using BBRAPIModules;
using System.Threading.Tasks;
using BattleBitAPI.Common;
using BattleBitAPI.Server;
using System.Text;
using System;

namespace BattleBitRCON;

public class RCONConfiguration : ModuleConfiguration
{
    public string RCONIP { get; set; } = "0.0.0.0";
    public int RCONPort { get; set; }
    public string Password { get; set; }
}

public class BattleMetricsRCON : BattleBitModule
{
    public RCONConfiguration? BattleMetricsRCONConfiguration { get; set; }

    private WebSocketServer<RunnerPlayer>? wss;

    public override void OnModulesLoaded()
    {
        if (wss == null && BattleMetricsRCONConfiguration != null)
        {
            var changedConfig = false;
            if (BattleMetricsRCONConfiguration.RCONIP == "")
            {
                BattleMetricsRCONConfiguration.RCONIP = "+";
                changedConfig = true;
            }

            if (BattleMetricsRCONConfiguration.RCONPort == 0)
            {
                BattleMetricsRCONConfiguration.RCONPort = Server.GamePort + 1;
                changedConfig = true;
            }

            if (
                BattleMetricsRCONConfiguration.Password == null
                || BattleMetricsRCONConfiguration.Password == ""
            )
            {
                BattleMetricsRCONConfiguration.Password = CreatePassword(32);
                changedConfig = true;
            }

            if (changedConfig)
            {
                BattleMetricsRCONConfiguration.Save();
            }

            wss = new WebSocketServer<RunnerPlayer>(
                Server,
                BattleMetricsRCONConfiguration.RCONIP,
                BattleMetricsRCONConfiguration.RCONPort,
                BattleMetricsRCONConfiguration.Password ?? CreatePassword(20)
            );
        }
    }

    public void Dispose()
    {
        wss?.Dispose();
        wss = null;
    }

    public override Task OnConnected()
    {
        wss?.Start();
        return base.OnConnected();
    }

    public override Task OnDisconnected()
    {
        wss?.Stop();
        return base.OnDisconnected();
    }

    public override async Task OnPlayerConnected(RunnerPlayer player)
    {
        if (wss != null)
        {
            await wss.BroadcastMessage(new Messages.OnPlayerConnected<RunnerPlayer>(player));
        }
        await base.OnPlayerConnected(player);
    }

    public override async Task OnPlayerDisconnected(RunnerPlayer player)
    {
        if (wss != null)
        {
            await wss.BroadcastMessage(new Messages.OnPlayerDisconnected<RunnerPlayer>(player));
        }
        await base.OnPlayerDisconnected(player);
    }

    public override async Task<bool> OnPlayerTypedMessage(
        RunnerPlayer player,
        ChatChannel channel,
        string msg
    )
    {
        if (wss != null)
        {
            await wss.BroadcastMessage(
                new Messages.OnPlayerTypedMessage<RunnerPlayer>(player, channel, msg)
            );
        }
        return await base.OnPlayerTypedMessage(player, channel, msg);
    }

    public override async Task OnPlayerChangedRole(RunnerPlayer player, GameRole role)
    {
        if (wss != null)
        {
            await wss.BroadcastMessage(
                new Messages.OnPlayerChangedRole<RunnerPlayer>(player, role)
            );
        }
        await base.OnPlayerChangedRole(player, role);
    }

    public override async Task OnPlayerJoinedSquad(RunnerPlayer player, Squad<RunnerPlayer> squad)
    {
        if (wss != null)
        {
            await wss.BroadcastMessage(
                new Messages.OnPlayerJoinedSquad<RunnerPlayer>(player, squad)
            );
        }
        await base.OnPlayerJoinedSquad(player, squad);
    }

    public override async Task OnSquadLeaderChanged(
        Squad<RunnerPlayer> squad,
        RunnerPlayer newLeader
    )
    {
        if (wss != null)
        {
            await wss.BroadcastMessage(
                new Messages.OnSquadLeaderChanged<RunnerPlayer>(squad, newLeader)
            );
        }
        await base.OnSquadLeaderChanged(squad, newLeader);
    }

    public override async Task OnPlayerLeftSquad(RunnerPlayer player, Squad<RunnerPlayer> squad)
    {
        if (wss != null)
        {
            await wss.BroadcastMessage(new Messages.OnPlayerLeftSquad<RunnerPlayer>(player, squad));
        }
        await base.OnPlayerLeftSquad(player, squad);
    }

    public override async Task OnPlayerChangeTeam(RunnerPlayer player, Team team)
    {
        if (wss != null)
        {
            await wss.BroadcastMessage(new Messages.OnPlayerChangeTeam<RunnerPlayer>(player, team));
        }
        await base.OnPlayerChangeTeam(player, team);
    }

    public override async Task OnSquadPointsChanged(Squad<RunnerPlayer> squad, int newPoints)
    {
        if (wss != null)
        {
            await wss.BroadcastMessage(
                new Messages.OnSquadPointsChanged<RunnerPlayer>(squad, newPoints)
            );
        }
        await base.OnSquadPointsChanged(squad, newPoints);
    }

    public override async Task OnPlayerSpawned(RunnerPlayer player)
    {
        if (wss != null)
        {
            await wss.BroadcastMessage(new Messages.OnPlayerSpawned<RunnerPlayer>(player));
        }
        await base.OnPlayerSpawned(player);
    }

    public override async Task OnPlayerDied(RunnerPlayer player)
    {
        if (wss != null)
        {
            await wss.BroadcastMessage(new Messages.OnPlayerDied<RunnerPlayer>(player));
        }
        await base.OnPlayerDied(player);
    }

    public override async Task OnPlayerGivenUp(RunnerPlayer player)
    {
        if (wss != null)
        {
            await wss.BroadcastMessage(new Messages.OnPlayerGivenUp<RunnerPlayer>(player));
        }
        await base.OnPlayerGivenUp(player);
    }

    public override async Task OnAPlayerDownedAnotherPlayer(
        OnPlayerKillArguments<RunnerPlayer> args
    )
    {
        if (wss != null)
        {
            await wss.BroadcastMessage(
                new Messages.OnAPlayerDownedAnotherPlayer<RunnerPlayer>(args)
            );
        }
        await base.OnAPlayerDownedAnotherPlayer(args);
    }

    public override async Task OnAPlayerRevivedAnotherPlayer(RunnerPlayer from, RunnerPlayer to)
    {
        if (wss != null)
        {
            await wss.BroadcastMessage(
                new Messages.OnAPlayerRevivedAnotherPlayer<RunnerPlayer>(from, to)
            );
        }
        await base.OnAPlayerRevivedAnotherPlayer(from, to);
    }

    public override async Task OnPlayerReported(
        RunnerPlayer from,
        RunnerPlayer to,
        ReportReason reason,
        string additional
    )
    {
        if (wss != null)
        {
            await wss.BroadcastMessage(
                new Messages.OnPlayerReported<RunnerPlayer>(from, to, reason, additional)
            );
        }
        await base.OnPlayerReported(from, to, reason, additional);
    }

    public override async Task OnGameStateChanged(GameState oldState, GameState newState)
    {
        if (wss != null)
        {
            await wss.BroadcastMessage(new Messages.OnGameStateChanged(oldState, newState));
        }
        await base.OnGameStateChanged(oldState, newState);
    }

    public override async Task OnRoundStarted()
    {
        if (wss != null)
        {
            await wss.BroadcastMessage(new Messages.OnRoundStarted());
        }
        await base.OnRoundStarted();
    }

    public override async Task OnRoundEnded()
    {
        if (wss != null)
        {
            await wss.BroadcastMessage(new Messages.OnRoundEnded());
        }
        await base.OnRoundEnded();
    }

    // Taken from https://stackoverflow.com/a/54997.
    // Not the most secure, but seems good enough for this use.
    private string CreatePassword(int length)
    {
        const string valid = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890";
        StringBuilder res = new StringBuilder();
        Random rnd = new Random();
        while (0 < length--)
        {
            res.Append(valid[rnd.Next(valid.Length)]);
        }
        return res.ToString();
    }
}
