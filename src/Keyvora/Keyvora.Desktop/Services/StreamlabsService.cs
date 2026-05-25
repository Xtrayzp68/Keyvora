namespace Keyvora.Desktop.Services;

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public sealed class StreamlabsService : IDisposable
{
    private ClientWebSocket? _webSocket;
    private CancellationTokenSource? _cts;
    private Task? _receiveLoop;
    private int _requestId;
    private readonly ConcurrentDictionary<int, TaskCompletionSource<JToken>> _pendingRequests = new();

    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 59650;
    public string? Password { get; set; }

    public bool IsConnected => _webSocket?.State == WebSocketState.Open;

    public event Action? Connected;
    public event Action? Disconnected;
    public event Action<string>? SceneChanged;
    public event Action<bool>? StreamingStateChanged;
    public event Action<bool>? RecordingStateChanged;

    public async Task ConnectAsync()
    {
        await DisconnectAsync();

        _cts = new CancellationTokenSource();
        _webSocket = new ClientWebSocket();

        var uri = new Uri($"ws://{Host}:{Port}/api/websocket");
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, timeoutCts.Token);

        await _webSocket.ConnectAsync(uri, linkedCts.Token);

        var authId = Interlocked.Increment(ref _requestId);
        var authRequest = new JObject
        {
            ["jsonrpc"] = "2.0",
            ["id"] = authId,
            ["method"] = "auth",
            ["params"] = new JObject
            {
                ["resource"] = "TcpServerService",
                ["args"] = new JArray(Password ?? "")
            }
        };

        await SendMessageAsync(authRequest.ToString(Formatting.None), linkedCts.Token);

        timeoutCts.CancelAfter(TimeSpan.FromSeconds(5));
        var authResponseJson = await ReceiveMessageAsync(linkedCts.Token);
        var authResponse = JObject.Parse(authResponseJson);

        if (authResponse["error"] != null)
        {
            var errMsg = authResponse["error"]?["message"]?.Value<string>() ?? "unknown error";
            throw new InvalidOperationException($"Authentication failed: {errMsg}");
        }

        if (authResponse["result"]?.Value<bool>() != true)
            throw new InvalidOperationException("Authentication failed - check API token");

        _receiveLoop = ReceiveLoopAsync(_cts.Token);
        Connected?.Invoke();
    }

    public async Task DisconnectAsync()
    {
        _cts?.Cancel();

        if (_webSocket?.State == WebSocketState.Open)
        {
            try { await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None); }
            catch { }
        }

        _webSocket?.Dispose();
        _webSocket = null;

        foreach (var tcs in _pendingRequests.Values)
            tcs.TrySetException(new OperationCanceledException());
        _pendingRequests.Clear();

        Disconnected?.Invoke();
    }

    public async Task<string[]> GetSceneListAsync()
    {
        var scenes = await GetAllScenesAsync();
        return scenes.Select(s => s["name"]?.Value<string>() ?? "")
                     .Where(n => !string.IsNullOrEmpty(n)).ToArray();
    }

    public async Task<string> GetCurrentSceneAsync()
    {
        var result = await SendRequestAsync("activeScene", "ScenesService");
        return result["name"]?.Value<string>() ?? "";
    }

    public async Task SetCurrentSceneAsync(string sceneName)
    {
        var scenes = await GetAllScenesAsync();
        var scene = scenes.FirstOrDefault(s => s["name"]?.Value<string>() == sceneName);
        if (scene == null)
            throw new ArgumentException($"Scene '{sceneName}' not found");

        var sceneId = scene["id"]?.Value<string>();
        if (string.IsNullOrEmpty(sceneId))
            throw new InvalidOperationException($"Scene '{sceneName}' has no ID");

        await SendRequestAsync("makeSceneActive", "ScenesService", sceneId);
    }

    public async Task<string[]> GetSceneSourcesAsync(string sceneName)
    {
        var scenes = await GetAllScenesAsync();
        var scene = scenes.FirstOrDefault(s => s["name"]?.Value<string>() == sceneName);
        if (scene == null) return [];

        var items = scene["items"] as JArray;
        return items?.Select(i => i["name"]?.Value<string>() ?? "")
                     .Where(n => !string.IsNullOrEmpty(n)).ToArray() ?? [];
    }

    public async Task ToggleSourceAsync(string sceneName, string sourceName)
    {
        var scenes = await GetAllScenesAsync();
        var scene = scenes.FirstOrDefault(s => s["name"]?.Value<string>() == sceneName);
        if (scene == null) return;

        var sceneId = scene["id"]?.Value<string>();
        if (string.IsNullOrEmpty(sceneId)) return;

        var items = scene["items"] as JArray;
        var item = items?.FirstOrDefault(i => i["name"]?.Value<string>() == sourceName);
        if (item == null) return;

        var sceneItemId = item["sceneItemId"]?.Value<string>();
        if (string.IsNullOrEmpty(sceneItemId)) return;

        var visible = item["visible"]?.Value<bool>() ?? true;

        await SendRequestAsync("setVisibility", "ScenesService", sceneId, sceneItemId, !visible);
    }

    public async Task StartStreamAsync()
    {
        await SendRequestAsync("startStreaming", "StreamingService");
    }

    public async Task StopStreamAsync()
    {
        await SendRequestAsync("stopStreaming", "StreamingService");
    }

    public async Task ToggleStreamAsync()
    {
        await SendRequestAsync("toggleStreaming", "StreamingService");
    }

    public async Task StartRecordAsync()
    {
        await SendRequestAsync("startRecording", "StreamingService");
    }

    public async Task StopRecordAsync()
    {
        await SendRequestAsync("stopRecording", "StreamingService");
    }

    public async Task ToggleRecordAsync()
    {
        await SendRequestAsync("toggleRecording", "StreamingService");
    }

    private async Task<JArray> GetAllScenesAsync()
    {
        var result = await SendRequestAsync("getScenes", "ScenesService");
        if (result is JArray arr) return arr;
        if (result["result"] is JArray nestedArr) return nestedArr;
        throw new InvalidOperationException("Unexpected scene list response format");
    }

    private async Task<JToken> SendRequestAsync(string method, string resource, params object[] args)
    {
        var id = Interlocked.Increment(ref _requestId);
        var tcs = new TaskCompletionSource<JToken>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pendingRequests[id] = tcs;

        var request = new JObject
        {
            ["jsonrpc"] = "2.0",
            ["id"] = id,
            ["method"] = method,
            ["params"] = new JObject
            {
                ["resource"] = resource,
                ["args"] = new JArray(args)
            }
        };

        await SendMessageAsync(request.ToString(Formatting.None), _cts?.Token ?? CancellationToken.None);

        return await tcs.Task.WaitAsync(TimeSpan.FromSeconds(10));
    }

    private async Task ReceiveLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && _webSocket?.State == WebSocketState.Open)
        {
            try
            {
                var json = await ReceiveMessageAsync(ct);
                var msg = JObject.Parse(json);

                var id = msg["id"]?.Value<int>();
                if (id.HasValue)
                {
                    HandleResponse(id.Value, msg);
                }
                else
                {
                    HandleEvent(msg);
                }
            }
            catch (OperationCanceledException) { break; }
            catch (WebSocketException) { break; }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Streamlabs] Receive error: {ex.Message}");
                break;
            }
        }

        if (!ct.IsCancellationRequested)
            _ = DisconnectAsync();
    }

    private void HandleResponse(int id, JObject msg)
    {
        if (!_pendingRequests.TryRemove(id, out var tcs))
            return;

        if (msg["error"] != null)
        {
            var errMsg = msg["error"]?["message"]?.Value<string>() ?? "unknown error";
            tcs.TrySetException(new InvalidOperationException($"Streamlabs request failed: {errMsg}"));
            return;
        }

        var result = msg["result"];
        if (result != null)
        {
            tcs.TrySetResult(result);
        }
        else
        {
            tcs.TrySetResult(JValue.CreateNull());
        }
    }

    private void HandleEvent(JObject msg)
    {
        var method = msg["method"]?.Value<string>();
        var result = msg["result"] as JObject;
        var @params = msg["params"] as JObject;

        var resource = result?["resource"]?.Value<string>()
                       ?? @params?["resource"]?.Value<string>();
        var args = result?["args"] as JArray
                   ?? @params?["args"] as JArray;

        if (string.IsNullOrEmpty(method) || string.IsNullOrEmpty(resource))
            return;

        switch (resource)
        {
            case "ScenesService":
                HandleScenesEvent(method, args);
                break;
            case "StreamingService":
                HandleStreamingEvent(method, args);
                break;
        }
    }

    private void HandleScenesEvent(string method, JArray? args)
    {
        switch (method)
        {
            case "activeSceneChanged":
            case "sceneSwitched":
                var sceneId = args?.FirstOrDefault()?.Value<string>();
                if (sceneId != null)
                {
                    _ = FetchAndNotifySceneName(sceneId);
                }
                break;
        }
    }

    private async Task FetchAndNotifySceneName(string sceneId)
    {
        try
        {
            var scenes = await GetAllScenesAsync();
            var scene = scenes.FirstOrDefault(s => s["id"]?.Value<string>() == sceneId);
            if (scene != null)
            {
                var name = scene["name"]?.Value<string>() ?? "";
                SceneChanged?.Invoke(name);
            }
        }
        catch { }
    }

    private void HandleStreamingEvent(string method, JArray? args)
    {
        switch (method)
        {
            case "streamingStateChange":
            case "streamingStatusChange":
                var streaming = args?.FirstOrDefault()?.Value<bool>();
                if (streaming.HasValue)
                    StreamingStateChanged?.Invoke(streaming.Value);
                break;
            case "recordingStateChange":
            case "recordingStatusChange":
                var recording = args?.FirstOrDefault()?.Value<bool>();
                if (recording.HasValue)
                    RecordingStateChanged?.Invoke(recording.Value);
                break;
        }
    }

    private async Task SendMessageAsync(string message, CancellationToken ct)
    {
        if (_webSocket?.State != WebSocketState.Open)
            throw new InvalidOperationException("Not connected to Streamlabs");

        var bytes = Encoding.UTF8.GetBytes(message);
        await _webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, ct);
    }

    private async Task<string> ReceiveMessageAsync(CancellationToken ct)
    {
        var buffer = new byte[65536];
        var sb = new StringBuilder();

        while (true)
        {
            var result = await _webSocket!.ReceiveAsync(new ArraySegment<byte>(buffer), ct);
            sb.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
            if (result.EndOfMessage) break;
        }

        return sb.ToString();
    }

    private static string ConfigPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Keyvora", "streamlabs.json");

    public void SaveConnectionInfo()
    {
        try
        {
            var dir = Path.GetDirectoryName(ConfigPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var config = new JObject
            {
                ["host"] = Host,
                ["port"] = Port,
                ["password"] = Password ?? ""
            };

            File.WriteAllText(ConfigPath, config.ToString(Formatting.Indented));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Streamlabs] Failed to save config: {ex.Message}");
        }
    }

    public void LoadConnectionInfo()
    {
        try
        {
            if (!File.Exists(ConfigPath)) return;

            var json = File.ReadAllText(ConfigPath);
            var config = JObject.Parse(json);

            Host = config["host"]?.Value<string>() ?? "localhost";
            Port = config["port"]?.Value<int>() ?? 59650;
            Password = config["password"]?.Value<string>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Streamlabs] Failed to load config: {ex.Message}");
        }
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _webSocket?.Dispose();
        foreach (var tcs in _pendingRequests.Values)
            tcs.TrySetCanceled();
        _pendingRequests.Clear();
    }
}
