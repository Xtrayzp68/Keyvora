namespace Keyvora.Desktop.Services;

using System;
using System.Collections.Concurrent;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public sealed class DiscordService : IDisposable
{
    private NamedPipeClientStream? _pipe;
    private CancellationTokenSource? _cts;
    private Task? _receiveLoop;
    private int _nonce;
    private readonly ConcurrentDictionary<string, TaskCompletionSource<JObject>> _pending = new();
    private bool _muted;
    private bool _deafened;

    public string? ClientId { get; set; }
    public bool IsConnected => _pipe?.IsConnected ?? false;
    public bool IsMuted => _muted;
    public bool IsDeafened => _deafened;

    public event Action? Connected;
    public event Action? Disconnected;
    public event Action<bool>? MuteStateChanged;
    public event Action<bool>? DeafenStateChanged;

    public async Task ConnectAsync()
    {
        await DisconnectAsync();

        if (string.IsNullOrEmpty(ClientId))
            throw new InvalidOperationException("Discord Client ID not configured");

        _cts = new CancellationTokenSource();
        _pipe = TryConnectPipe();

        var handshake = new JObject { ["v"] = 1, ["client_id"] = ClientId };
        await SendFrameAsync(0, handshake.ToString(Formatting.None));

        var (opcode, raw) = await ReceiveFrameAsync();
        System.Diagnostics.Debug.WriteLine($"[Discord] Handshake response: opcode={opcode}, data={raw}");

        JObject ready;
        try { ready = JObject.Parse(raw); }
        catch { throw new InvalidOperationException($"Discord handshake failed: invalid JSON (opcode={opcode})"); }

        var evt = ready["evt"]?.Value<string>();
        if (evt != "READY")
            throw new InvalidOperationException($"Discord handshake failed: expected READY, got '{evt}' (opcode={opcode})");

        _receiveLoop = ReceiveLoopAsync();

        _ = SubscribeAsync();
        _ = QueryVoiceSettingsAsync();

        Connected?.Invoke();
    }

    private static NamedPipeClientStream TryConnectPipe()
    {
        Exception? lastEx = null;
        for (int i = 0; i <= 9; i++)
        {
            try
            {
                var pipe = new NamedPipeClientStream(".", $"discord-ipc-{i}", PipeDirection.InOut, PipeOptions.Asynchronous);
                pipe.Connect(2000);
                return pipe;
            }
            catch (Exception ex) { lastEx = ex; }
        }
        throw new InvalidOperationException($"Cannot connect to Discord. Is Discord running? ({lastEx?.Message})");
    }

    public async Task DisconnectAsync()
    {
        _cts?.Cancel();

        if (_pipe?.IsConnected == true)
        {
            try { await SendFrameAsync(2, "{}"); }
            catch { }
            _pipe?.Dispose();
        }

        _pipe = null;

        foreach (var tcs in _pending.Values)
            tcs.TrySetCanceled();
        _pending.Clear();

        Disconnected?.Invoke();
    }

    public async Task SetMuteAsync(bool mute)
    {
        await SetVoiceSettingsAsync(mute, null);
    }

    public async Task ToggleMuteAsync()
    {
        await SetVoiceSettingsAsync(!_muted, null);
    }

    public async Task SetDeafenAsync(bool deafen)
    {
        await SetVoiceSettingsAsync(null, deafen);
    }

    public async Task ToggleDeafenAsync()
    {
        await SetVoiceSettingsAsync(null, !_deafened);
    }

    public async Task LeaveVoiceChannelAsync()
    {
        var cmd = new JObject
        {
            ["cmd"] = "SELECT_VOICE_CHANNEL",
            ["args"] = new JObject { ["channel_id"] = null!, ["guild_id"] = null! },
            ["nonce"] = NextNonce(),
            ["evt"] = null
        };
        await SendFrameAsync(1, cmd.ToString(Formatting.None));
    }

    private async Task SetVoiceSettingsAsync(bool? mute, bool? deafen)
    {
        var args = new JObject();
        if (mute.HasValue) args["mute"] = mute.Value;
        if (deafen.HasValue) args["deaf"] = deafen.Value;

        var cmd = new JObject
        {
            ["cmd"] = "SET_VOICE_SETTINGS",
            ["args"] = args,
            ["nonce"] = NextNonce(),
            ["evt"] = null
        };
        await SendFrameAsync(1, cmd.ToString(Formatting.None));
    }

    private async Task QueryVoiceSettingsAsync()
    {
        var cmd = new JObject
        {
            ["cmd"] = "GET_VOICE_SETTINGS",
            ["args"] = new JObject(),
            ["nonce"] = NextNonce(),
            ["evt"] = null
        };
        await SendFrameAsync(1, cmd.ToString(Formatting.None));
    }

    private async Task SubscribeAsync()
    {
        foreach (var evt in new[] { "VOICE_SETTINGS_UPDATE", "VOICE_CHANNEL_SELECT" })
        {
            var sub = new JObject
            {
                ["cmd"] = "SUBSCRIBE",
                ["args"] = new JObject(),
                ["evt"] = evt,
                ["nonce"] = NextNonce()
            };
            await SendFrameAsync(1, sub.ToString(Formatting.None));
        }
    }

    private string NextNonce()
    {
        var n = Interlocked.Increment(ref _nonce);
        return $"sd-{n}-{Guid.NewGuid():n}";
    }

    private async Task ReceiveLoopAsync()
    {
        try
        {
            while (!_cts!.IsCancellationRequested && _pipe?.IsConnected == true)
            {
                var (opcode, raw) = await ReceiveFrameAsync();
                if (opcode == 2) break;

                if (opcode != 1) continue;

                var msg = JObject.Parse(raw);
                var nonce = msg["nonce"]?.Value<string>();

                if (nonce != null && _pending.TryRemove(nonce, out var tcs))
                {
                    tcs.TrySetResult(msg);
                    continue;
                }

                HandleDispatch(msg);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Discord] Receive error: {ex.Message}");
        }

        if (!_cts!.IsCancellationRequested)
            _ = DisconnectAsync();
    }

    private void HandleDispatch(JObject msg)
    {
        var evt = msg["evt"]?.Value<string>();
        var data = msg["data"] as JObject;
        if (evt == null || data == null) return;

        switch (evt)
        {
            case "VOICE_SETTINGS_UPDATE":
                _muted = data["mute"]?.Value<bool>() ?? _muted;
                _deafened = data["deaf"]?.Value<bool>() ?? _deafened;
                MuteStateChanged?.Invoke(_muted);
                DeafenStateChanged?.Invoke(_deafened);
                break;
        }
    }

    private async Task SendFrameAsync(uint opcode, string json)
    {
        if (_pipe?.IsConnected != true)
            throw new InvalidOperationException("Not connected to Discord");

        var jsonBytes = Encoding.UTF8.GetBytes(json);
        var header = new byte[8];
        BitConverter.GetBytes(opcode).CopyTo(header, 0);
        BitConverter.GetBytes((uint)jsonBytes.Length).CopyTo(header, 4);

        await _pipe.WriteAsync(header.AsMemory(0, 8), _cts?.Token ?? CancellationToken.None);
        await _pipe.WriteAsync(jsonBytes.AsMemory(0, jsonBytes.Length), _cts?.Token ?? CancellationToken.None);
        await _pipe.FlushAsync(_cts?.Token ?? CancellationToken.None);
    }

    private async Task<(uint opcode, string json)> ReceiveFrameAsync()
    {
        var header = new byte[8];
        int read = 0;
        while (read < 8)
        {
            var n = await _pipe!.ReadAsync(header.AsMemory(read, 8 - read), _cts?.Token ?? CancellationToken.None);
            if (n == 0) throw new EndOfStreamException("Discord pipe closed");
            read += n;
        }

        var opcode = BitConverter.ToUInt32(header, 0);
        var length = BitConverter.ToUInt32(header, 4);

        if (length == 0) return (opcode, "{}");

        var jsonBytes = new byte[length];
        read = 0;
        while (read < length)
        {
            var n = await _pipe!.ReadAsync(jsonBytes.AsMemory(read, (int)(length - read)), _cts?.Token ?? CancellationToken.None);
            if (n == 0) throw new EndOfStreamException("Discord pipe closed");
            read += n;
        }

        return (opcode, Encoding.UTF8.GetString(jsonBytes));
    }

    private static string ConfigPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Keyvora", "discord.json");

    public void SaveConfig()
    {
        try
        {
            var dir = Path.GetDirectoryName(ConfigPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var config = new JObject { ["clientId"] = ClientId ?? "" };
            File.WriteAllText(ConfigPath, config.ToString(Formatting.Indented));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Discord] Failed to save config: {ex.Message}");
        }
    }

    public void LoadConfig()
    {
        try
        {
            if (!File.Exists(ConfigPath)) return;
            var json = File.ReadAllText(ConfigPath);
            var config = JObject.Parse(json);
            ClientId = config["clientId"]?.Value<string>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Discord] Failed to load config: {ex.Message}");
        }
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _pipe?.Dispose();
    }
}
