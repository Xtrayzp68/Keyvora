namespace Keyvora.Desktop.Services;

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public sealed class SpotifyService : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly string _redirectUri;
    private readonly string _tokensPath;

    private string? _accessToken;
    private string? _refreshToken;
    private DateTime _tokenExpiry = DateTime.MinValue;

    public bool IsAuthorized => !string.IsNullOrEmpty(_accessToken) && DateTime.UtcNow < _tokenExpiry;
    public bool HasValidCredentials =>
        !string.IsNullOrWhiteSpace(_clientId) && !_clientId.Contains("YOUR_") &&
        !string.IsNullOrWhiteSpace(_clientSecret) && !_clientSecret.Contains("YOUR_");

    public Action<string>? OnStatusMessage { get; set; }
    public string RedirectUri => _redirectUri;

    public SpotifyService(string clientId, string clientSecret, string redirectUri)
    {
        _clientId = clientId;
        _clientSecret = clientSecret;
        _redirectUri = redirectUri;
        _httpClient = new HttpClient();
        _tokensPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Keyvora", "spotify-tokens.json");

        LoadTokens();

        if (!HasValidCredentials)
        {
            Debug.WriteLine("[Spotify] Invalid credentials. Set your ClientId and ClientSecret in appsettings.json");
        }
    }

    private void LoadTokens()
    {
        try
        {
            if (!File.Exists(_tokensPath)) return;
            var json = File.ReadAllText(_tokensPath);
            var data = JObject.Parse(json);
            _accessToken = data["access_token"]?.ToString();
            _refreshToken = data["refresh_token"]?.ToString();
            if (data["token_expiry"]?.Value<long>() is long exp)
                _tokenExpiry = DateTime.UnixEpoch.AddSeconds(exp);
            Debug.WriteLine("[Spotify] Tokens loaded from disk");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Spotify] Failed to load tokens: {ex.Message}");
        }
    }

    private void SaveTokens()
    {
        try
        {
            var dir = Path.GetDirectoryName(_tokensPath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            var data = new JObject
            {
                ["access_token"] = _accessToken,
                ["refresh_token"] = _refreshToken,
                ["token_expiry"] = new DateTimeOffset(_tokenExpiry).ToUnixTimeSeconds()
            };
            File.WriteAllText(_tokensPath, data.ToString(Formatting.Indented));
            Debug.WriteLine("[Spotify] Tokens saved to disk");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Spotify] Failed to save tokens: {ex.Message}");
        }
    }

    public string GetAuthorizationUrl()
    {
        var scopes = Uri.EscapeDataString(
            "user-read-playback-state user-modify-playback-state user-read-currently-playing");
        return $"https://accounts.spotify.com/authorize" +
               $"?client_id={_clientId}" +
               $"&response_type=code" +
               $"&redirect_uri={Uri.EscapeDataString(_redirectUri)}" +
               $"&scope={scopes}";
    }

    public async Task<bool> AuthorizeAsync(string authorizationCode)
    {
        try
        {
            var authHeader = Convert.ToBase64String(
                System.Text.Encoding.UTF8.GetBytes($"{_clientId}:{_clientSecret}"));

            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "authorization_code"),
                new KeyValuePair<string, string>("code", authorizationCode),
                new KeyValuePair<string, string>("redirect_uri", _redirectUri)
            });

            var request = new HttpRequestMessage(HttpMethod.Post, "https://accounts.spotify.com/api/token")
            {
                Content = content
            };
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authHeader);

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var data = JObject.Parse(json);

            _accessToken = data["access_token"]?.ToString();
            _refreshToken = data["refresh_token"]?.ToString();
            _tokenExpiry = DateTime.UtcNow.AddSeconds(data["expires_in"]?.Value<int>() ?? 3600);

            SaveTokens();

            Debug.WriteLine("[Spotify] Authorization successful");
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Spotify] Authorization failed: {ex.Message}");
            return false;
        }
    }

    private async Task EnsureAuthorizedAsync()
    {
        if (!IsAuthorized && !string.IsNullOrEmpty(_refreshToken))
            await RefreshTokenAsync();
    }

    private async Task RefreshTokenAsync()
    {
        try
        {
            var authHeader = Convert.ToBase64String(
                System.Text.Encoding.UTF8.GetBytes($"{_clientId}:{_clientSecret}"));

            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "refresh_token"),
                new KeyValuePair<string, string>("refresh_token", _refreshToken!)
            });

            var request = new HttpRequestMessage(HttpMethod.Post, "https://accounts.spotify.com/api/token")
            {
                Content = content
            };
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authHeader);

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var data = JObject.Parse(json);

            _accessToken = data["access_token"]?.ToString();
            _tokenExpiry = DateTime.UtcNow.AddSeconds(data["expires_in"]?.Value<int>() ?? 3600);

            SaveTokens();

            Debug.WriteLine("[Spotify] Token refreshed");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Spotify] Token refresh failed: {ex.Message}");
        }
    }

    private async Task<JObject?> GetAsync(string endpoint)
    {
        await EnsureAuthorizedAsync();
        if (!IsAuthorized)
        {
            var msg = !HasValidCredentials
                ? "Spotify: configurer ClientId/Secret dans appsettings.json"
                : "Spotify: non autorisé. Cliquer sur ⚙ > Authorize Spotify";
            Debug.WriteLine("[Spotify] " + msg);
            OnStatusMessage?.Invoke(msg);
            return null;
        }

        var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.spotify.com/v1/{endpoint}");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);

        try
        {
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JObject.Parse(json);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Spotify] GET {endpoint} failed: {ex.Message}");
            OnStatusMessage?.Invoke($"Spotify erreur: {ex.Message}");
            return null;
        }
    }

    private async Task<bool> PostAsync(string endpoint)
    {
        await EnsureAuthorizedAsync();
        if (!IsAuthorized)
        {
            var msg = "Spotify: non autorisé";
            Debug.WriteLine("[Spotify] " + msg);
            OnStatusMessage?.Invoke(msg);
            return false;
        }

        var request = new HttpRequestMessage(HttpMethod.Post, $"https://api.spotify.com/v1/{endpoint}");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);

        try
        {
            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                var msg = $"Spotify: {endpoint} → {(int)response.StatusCode}";
                Debug.WriteLine($"[Spotify] POST {endpoint} returned {(int)response.StatusCode}: {body}");
                OnStatusMessage?.Invoke(msg);
            }
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            var msg = $"Spotify erreur: {ex.Message}";
            Debug.WriteLine($"[Spotify] POST {endpoint} failed: {ex.Message}");
            OnStatusMessage?.Invoke(msg);
            return false;
        }
    }

    private async Task<bool> PutAsync(string endpoint, HttpContent? content = null)
    {
        await EnsureAuthorizedAsync();
        if (!IsAuthorized)
        {
            var msg = "Spotify: non autorisé";
            Debug.WriteLine("[Spotify] " + msg);
            OnStatusMessage?.Invoke(msg);
            return false;
        }

        var request = new HttpRequestMessage(HttpMethod.Put, $"https://api.spotify.com/v1/{endpoint}")
        {
            Content = content
        };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);

        try
        {
            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                var msg = $"Spotify: {endpoint} → {(int)response.StatusCode}";
                Debug.WriteLine($"[Spotify] PUT {endpoint} returned {(int)response.StatusCode}: {body}");
                OnStatusMessage?.Invoke(msg);
            }
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            var msg = $"Spotify erreur: {ex.Message}";
            Debug.WriteLine($"[Spotify] PUT {endpoint} failed: {ex.Message}");
            OnStatusMessage?.Invoke(msg);
            return false;
        }
    }

    public async Task<string> ListDevicesAsync()
    {
        var devices = await GetAsync("me/player/devices");
        var list = devices?["devices"] as JArray;
        if (list == null || list.Count == 0)
            return "Aucun appareil Spotify trouvé. Joue d'abord une musique sur Spotify Desktop, puis réessaie.";
        var names = list
            .Select(d => $"{d["name"]} ({(d["is_active"]?.Value<bool>() == true ? "actif" : "inactif")})")
            .ToList();
        return "Appareils: " + string.Join(", ", names);
    }

    private async Task<bool> TryActivateDeviceAsync()
    {
        await EnsureAuthorizedAsync();
        if (!IsAuthorized) return false;

        try
        {
            var raw = await GetAsync("me/player/devices");
            var list = raw?["devices"] as JArray;
            if (list == null || list.Count == 0) return false;

            Debug.WriteLine($"[Spotify] Appareils: {string.Join(", ", list.Select(d => d["name"]))}");

            foreach (var device in list)
            {
                var deviceId = device["id"]?.ToString();
                var deviceName = device["name"]?.ToString() ?? "inconnu";
                if (string.IsNullOrEmpty(deviceId)) continue;

                if (device["is_active"]?.Value<bool>() == true)
                {
                    Debug.WriteLine($"[Spotify] Déjà actif: {deviceName}");
                    return true;
                }

                var payload = JsonConvert.SerializeObject(new { device_ids = new[] { deviceId }, play = false });
                var content = new StringContent(payload, Encoding.UTF8, "application/json");
                var ok = await PutAsync("me/player", content);
                if (ok)
                {
                    OnStatusMessage?.Invoke($"Spotify: connecté à \"{deviceName}\"");
                    await Task.Delay(500);
                    return true;
                }
            }
            return false;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Spotify] Device activation failed: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> EnsureConnectedAsync()
    {
        var state = await GetAsync("me/player");
        if (state != null) return true;
        return await TryActivateDeviceAsync();
    }

    public async Task PlayPauseAsync()
    {
        var state = await GetAsync("me/player");
        if (state == null)
        {
            if (!IsAuthorized) return;
            var activated = await TryActivateDeviceAsync();
            if (!activated)
            {
                var msg = "Spotify: aucun appareil. Ouvre Spotify, joue une musique, puis réessaie.";
                Debug.WriteLine("[Spotify] " + msg);
                OnStatusMessage?.Invoke(msg);
                return;
            }
            state = await GetAsync("me/player");
            if (state == null) return;
        }

        var isPlaying = state["is_playing"]?.Value<bool>() ?? false;

        if (isPlaying)
        {
            var ok = await PutAsync("me/player/pause");
            if (ok) { Debug.WriteLine("[Spotify] Paused"); OnStatusMessage?.Invoke("Spotify: pause"); }
        }
        else
        {
            var ok = await PutAsync("me/player/play");
            if (ok) { Debug.WriteLine("[Spotify] Resumed"); OnStatusMessage?.Invoke("Spotify: lecture"); }
        }
    }

    public async Task NextTrackAsync()
    {
        var ok = await PostAsync("me/player/next");
        if (ok) { Debug.WriteLine("[Spotify] Next track"); OnStatusMessage?.Invoke("Spotify: suivant"); }
    }

    public async Task PreviousTrackAsync()
    {
        var ok = await PostAsync("me/player/previous");
        if (ok) { Debug.WriteLine("[Spotify] Previous track"); OnStatusMessage?.Invoke("Spotify: précédent"); }
    }

    public async Task<int> GetVolumeAsync()
    {
        var state = await GetAsync("me/player");
        var vol = state?["device"]?["volume_percent"]?.Value<int>() ?? 50;
        return vol;
    }

    public async Task<bool> SetVolumeAsync(int volume)
    {
        volume = Math.Clamp(volume, 0, 100);
        var ok = await PutAsync($"me/player/volume?volume_percent={volume}");
        if (ok) { Debug.WriteLine($"[Spotify] Volume set to {volume}"); OnStatusMessage?.Invoke($"Spotify: volume {volume}%"); }
        return ok;
    }

    public async Task<string?> GetCurrentTrackAsync()
    {
        var state = await GetAsync("me/player/currently-playing");
        if (state == null)
        {
            await TryActivateDeviceAsync();
            state = await GetAsync("me/player/currently-playing");
            if (state == null) return null;
        }

        var item = state["item"];
        if (item == null) return "No track playing";

        var name = item["name"]?.ToString() ?? "Unknown";
        var artists = string.Join(", ", item["artists"]?.Select(a => a["name"]?.ToString()) ?? Array.Empty<string>());

        return $"{artists} - {name}";
    }

    public void Dispose() => _httpClient.Dispose();
}
