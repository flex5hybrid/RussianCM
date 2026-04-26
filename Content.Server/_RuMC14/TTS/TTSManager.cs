using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Content.Shared.Corvax.CCCVars;
using Prometheus;
using Robust.Shared.Configuration;

namespace Content.Server.Corvax.TTS;

// ReSharper disable once InconsistentNaming
public sealed class TTSManager
{
    private static readonly Histogram RequestTimings = Metrics.CreateHistogram(
        "tts_req_timings",
        "Timings of TTS API requests",
        new HistogramConfiguration()
        {
            LabelNames = new[] { "type" },
            Buckets = Histogram.ExponentialBuckets(.1, 1.5, 10),
        });

    private static readonly Counter WantedCount = Metrics.CreateCounter(
        "tts_wanted_count",
        "Amount of wanted TTS audio.");

    private static readonly Counter ReusedCount = Metrics.CreateCounter(
        "tts_reused_count",
        "Amount of reused TTS audio from cache.");

    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private readonly HttpClient _httpClient = new();

    private ISawmill _sawmill = default!;
    private readonly Dictionary<string, byte[]> _cache = new();
    private readonly List<string> _cacheKeysSeq = new();
    private readonly object _cacheLock = new();
    private int _maxCachedCount = 200;
    private string _apiUrl = string.Empty;
    private string _apiToken = string.Empty;

    public void Initialize()
    {
        _sawmill = Logger.GetSawmill("tts");
        _cfg.OnValueChanged(CCCVars.TTSMaxCache, val =>
        {
            _maxCachedCount = val;
            ResetCache();
        }, true);
        _cfg.OnValueChanged(CCCVars.TTSApiUrl, v => _apiUrl = v, true);
        _cfg.OnValueChanged(CCCVars.TTSApiToken, v => _apiToken = v, true);
    }

    /// <summary>
    /// Generates audio with passed text by API
    /// </summary>
    /// <param name="speaker">Identifier of speaker</param>
    /// <param name="text">SSML formatted text</param>
    /// <returns>OGG audio bytes or null if failed</returns>
    public async Task<byte[]?> ConvertTextToSpeech(string speaker, string text)
    {
        WantedCount.Inc();

        var cacheKey = GenerateCacheKey(speaker, text);

        lock (_cacheLock)
        {
            if (_cache.TryGetValue(cacheKey, out var cached))
            {
                ReusedCount.Inc();
                _sawmill.Verbose($"Use cached TTS for '{text}' by '{speaker}'");
                return cached;
            }
        }

        _sawmill.Verbose($"Generate TTS for '{text}' by '{speaker}'");

        var queryParams = new Dictionary<string, string>
        {
            ["speaker"] = speaker,
            ["text"] = text,
            ["ext"] = "wav"
        };

        if (!Uri.TryCreate(_apiUrl, UriKind.Absolute, out var baseUri))
        {
            _sawmill.Error($"Invalid TTS API URL: '{_apiUrl}'");
            return null;
        }

        var query = string.Join("&",
            queryParams.Select(kv =>
                $"{kv.Key}={Uri.EscapeDataString(kv.Value)}"));

        var requestUri = new UriBuilder(baseUri)
        {
            Query = query
        }.Uri;

        var startTime = DateTime.UtcNow;

        try
        {
            var timeout = _cfg.GetCVar(CCCVars.TTSApiTimeout);
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeout));
            using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            request.Headers.Authorization = new("Bearer", _apiToken);

            var response = await _httpClient.SendAsync(request, cts.Token);

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    _sawmill.Warning("TTS rate limited");
                    return null;
                }

                _sawmill.Error($"TTS API error: {response.StatusCode}");
                return null;
            }

            var soundData = await response.Content.ReadAsByteArrayAsync(cts.Token);

            lock (_cacheLock)
            {
                if (_cache.TryGetValue(cacheKey, out var cached))
                {
                    ReusedCount.Inc();
                    return cached;
                }

                _cache[cacheKey] = soundData;
                _cacheKeysSeq.Add(cacheKey);

                if (_cache.Count > _maxCachedCount)
                {
                    var first = _cacheKeysSeq[0];
                    _cache.Remove(first);
                    _cacheKeysSeq.RemoveAt(0);
                }
            }

            _sawmill.Debug(
                $"TTS generated ({soundData.Length} bytes) for '{text}' by '{speaker}'");

            RequestTimings
                .WithLabels("Success")
                .Observe((DateTime.UtcNow - startTime).TotalSeconds);

            return soundData;
        }
        catch (TaskCanceledException)
        {
            RequestTimings
                .WithLabels("Timeout")
                .Observe((DateTime.UtcNow - startTime).TotalSeconds);

            _sawmill.Error($"TTS timeout for '{text}'");
            return null;
        }
        catch (Exception e)
        {
            RequestTimings
                .WithLabels("Error")
                .Observe((DateTime.UtcNow - startTime).TotalSeconds);

            _sawmill.Error($"TTS failed for '{text}'\n{e}");
            return null;
        }
    }

    public void ResetCache()
    {
        lock (_cacheLock)
        {
            _cache.Clear();
            _cacheKeysSeq.Clear();
        }
    }

    private string GenerateCacheKey(string speaker, string text)
    {
        var key = $"{speaker}/{text}";
        byte[] keyData = Encoding.UTF8.GetBytes(key);
        var sha256 = System.Security.Cryptography.SHA256.Create();
        var bytes = sha256.ComputeHash(keyData);
        return Convert.ToHexString(bytes);
    }

    private struct GenerateVoiceRequest
    {
        public GenerateVoiceRequest()
        {
        }

        [JsonPropertyName("api_token")]
        public string ApiToken { get; set; } = "";

        [JsonPropertyName("text")]
        public string Text { get; set; } = "";

        [JsonPropertyName("speaker")]
        public string Speaker { get; set; } = "";

        [JsonPropertyName("ssml")]
        public bool SSML { get; private set; } = true;

        [JsonPropertyName("word_ts")]
        public bool WordTS { get; private set; } = false;

        [JsonPropertyName("put_accent")]
        public bool PutAccent { get; private set; } = true;

        [JsonPropertyName("put_yo")]
        public bool PutYo { get; private set; } = false;

        [JsonPropertyName("sample_rate")]
        public int SampleRate { get; private set; } = 24000;

        [JsonPropertyName("format")]
        public string Format { get; private set; } = "ogg";
    }

    private struct GenerateVoiceResponse
    {
        [JsonPropertyName("results")]
        public List<VoiceResult> Results { get; set; }

        [JsonPropertyName("original_sha1")]
        public string Hash { get; set; }
    }

    private struct VoiceResult
    {
        [JsonPropertyName("audio")]
        public string Audio { get; set; }
    }
}
