using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Content.Server.Administration;
using Content.Server.Players.PlayTimeTracking;
using Content.Shared.Corvax.CCCVars;
using Microsoft.Extensions.Primitives;
using Robust.Server.ServerStatus;
using Robust.Shared.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Network;

namespace Content.Server.API;

internal sealed class PlaytimeApi : IPostInjectInit
{
    [Dependency] private readonly IStatusHost _statusHost = default!;
    [Dependency] private readonly PlayTimeTrackingManager _playTimeTracking = default!;
    [Dependency] private readonly IPlayerLocator _playerLocator = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private ISawmill _log = default!;
    private string _secret = string.Empty;
    private string _allowedIP = string.Empty;

    void IPostInjectInit.PostInject()
    {
        _log = Logger.GetSawmill("playtimeApi");

        _statusHost.AddHandler(PlaytimeHandler);

        _cfg.OnValueChanged(CCCVars.PlaytimeApiToken, v => _secret = v, true);
        _cfg.OnValueChanged(CCCVars.PlaytimeApiAllowedIP, v => _allowedIP = v, true);

        _log.Info("Playtime API initialized");
    }

    private async Task<bool> PlaytimeHandler(IStatusHandlerContext context)
    {
        if (_secret == string.Empty)
        {
            return false;
        }

        // Проверяем endpoint
        if (context.RequestMethod != HttpMethod.Post ||
            context.Url.AbsolutePath != "/api/playtime/add")
        {
            return false;
        }

        var ip = context.RemoteEndPoint.Address.ToString();

        _log.Info($"Incoming request from {ip}");

        // IP whitelist
        if (!_allowedIP.Contains(ip))
        {
            _log.Warning($"Blocked request from unauthorized IP: {ip}");
            await context.RespondErrorAsync(HttpStatusCode.Forbidden);
            return true;
        }

        // Проверка токена
        if (!context.RequestHeaders.TryGetValue("Authorization", out StringValues token) ||
            token.Count == 0 ||
            token[0] != _secret)
        {
            _log.Warning($"Unauthorized request from {ip}");
            await context.RespondErrorAsync(HttpStatusCode.Unauthorized);
            return true;
        }

        PlaytimeRequest? request;

        try
        {
            request = await context.RequestBodyJsonAsync<PlaytimeRequest>();
        }
        catch (Exception e)
        {
            _log.Error($"JSON parse error: {e}");
            await context.RespondErrorAsync(HttpStatusCode.BadRequest);
            return true;
        }

        if (request == null || request.Players.Length == 0)
        {
            _log.Warning("Empty playtime request");
            await context.RespondErrorAsync(HttpStatusCode.BadRequest);
            return true;
        }

        int operations = 0;

        foreach (var player in request.Players)
        {
            if (string.IsNullOrWhiteSpace(player.Ckey))
            {
                _log.Warning("Player with empty ckey skipped");
                continue;
            }

            if (player.Roles.Count == 0)
            {
                _log.Warning($"Player {player.Ckey} has no roles");
                continue;
            }

            foreach (var role in player.Roles)
            {
                var roleName = role.Key;
                var minutes = role.Value;

                if (minutes <= 0)
                {
                    _log.Warning($"Invalid minutes for {player.Ckey} role {roleName}");
                    continue;
                }

                _log.Info($"Playtime add request: {player.Ckey} | {roleName} | {minutes} minutes");

                // TODO: реальное начисление playtime
                // AddPlaytime(player.Ckey, roleName, minutes);
                NetUserId userId;
                if (Guid.TryParse(player.Ckey, out var guid))
                {
                    userId = new NetUserId(guid);
                }
                else
                {
                    var dbGuid = await _playerLocator.LookupIdByNameAsync(player.Ckey);
                    if (dbGuid == null)
                    {
                        _log.Error(Loc.GetString("parse-session-fail", ("username", player.Ckey)));
                        return false;
                    }
                    userId = dbGuid.UserId;
                }
                _playTimeTracking.AddTimeToTrackerById(userId, roleName, TimeSpan.FromMinutes(minutes));

                operations++;
            }
        }

        var response = new ApiResponse
        {
            Success = true,
            ProcessedOperations = operations
        };

        await context.RespondJsonAsync(response, HttpStatusCode.OK);

        _log.Info($"Processed {operations} playtime operations");

        return true;
    }

    // =========================
    // DATA MODELS
    // =========================

    private sealed class PlaytimeRequest
    {
        [JsonPropertyName("players")]
        public PlayerData[] Players { get; set; } = Array.Empty<PlayerData>();
    }

    private sealed class PlayerData
    {
        [JsonPropertyName("ckey")]
        public string Ckey { get; set; } = "";

        [JsonPropertyName("roles")]
        public Dictionary<string, int> Roles { get; set; } = new();
    }

    private sealed class ApiResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("processed_operations")]
        public int ProcessedOperations { get; set; }
    }
}
