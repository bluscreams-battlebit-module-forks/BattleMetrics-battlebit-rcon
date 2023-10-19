using BattleBitAPI;
using BattleBitAPI.Common;
using BattleBitAPI.Server;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;

namespace BattleBitRCON {

    public class RCONServer<TPlayer> : GameServer<TPlayer>, IDisposable
        where TPlayer : Player<TPlayer> {
        private WebSocketServer<TPlayer> wss;

        public RCONServer() {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build()
                .GetSection("BattleBitRCON");

            var serverConfig = config.GetSection($"{GameIP}:{GamePort}");
            var ip = serverConfig["ip"] ?? "0.0.0.0";

            int port;
            if (!int.TryParse(serverConfig["port"], out port)) {
                port = GamePort + 1;
            }

            var password = serverConfig["password"];
            if (password == null) {
                // This should probably just be a fatal error, but it's useful for testing.
                password = Guid.NewGuid().ToString();
                Console.WriteLine(
                    $"No RCON password found. Please set a secure password. Using: {password}"
                );
            }

            wss = new WebSocketServer<TPlayer>(this, ip, port, password);
        }

        public new void Dispose() {
            wss.Dispose();
            base.Dispose();
        }

        public override Task OnConnected() {
            wss.Start();
            return base.OnConnected();
        }

        public override Task OnDisconnected() {
            wss.Stop();
            return base.OnDisconnected();
        }

        public override async Task OnPlayerConnected(TPlayer player) {
            await wss.BroadcastMessage(new Messages.OnPlayerConnected<TPlayer>(player));
            await base.OnPlayerConnected(player);
        }

        public override async Task OnPlayerDisconnected(TPlayer player) {
            await wss.BroadcastMessage(new Messages.OnPlayerDisconnected<TPlayer>(player));
            await base.OnPlayerDisconnected(player);
        }

        public override async Task<bool> OnPlayerTypedMessage(
            TPlayer player,
            ChatChannel channel,
            string msg
        ) {
            await wss.BroadcastMessage(
                new Messages.OnPlayerTypedMessage<TPlayer>(player, channel, msg)
            );
            return await base.OnPlayerTypedMessage(player, channel, msg);
        }

        public override async Task OnPlayerChangedRole(TPlayer player, GameRole role) {
            await wss.BroadcastMessage(new Messages.OnPlayerChangedRole<TPlayer>(player, role));
            await base.OnPlayerChangedRole(player, role);
        }

        public override async Task OnPlayerJoinedSquad(TPlayer player, Squad<TPlayer> squad) {
            await wss.BroadcastMessage(new Messages.OnPlayerJoinedSquad<TPlayer>(player, squad));
            await base.OnPlayerJoinedSquad(player, squad);
        }

        public override async Task OnSquadLeaderChanged(Squad<TPlayer> squad, TPlayer newLeader) {
            await wss.BroadcastMessage(
                new Messages.OnSquadLeaderChanged<TPlayer>(squad, newLeader)
            );
            await base.OnSquadLeaderChanged(squad, newLeader);
        }

        public override async Task OnPlayerLeftSquad(TPlayer player, Squad<TPlayer> squad) {
            await wss.BroadcastMessage(new Messages.OnPlayerLeftSquad<TPlayer>(player, squad));
            await base.OnPlayerLeftSquad(player, squad);
        }

        public override async Task OnPlayerChangeTeam(TPlayer player, Team team) {
            await wss.BroadcastMessage(new Messages.OnPlayerChangeTeam<TPlayer>(player, team));
            await base.OnPlayerChangeTeam(player, team);
        }

        public override async Task OnSquadPointsChanged(Squad<TPlayer> squad, int newPoints) {
            await wss.BroadcastMessage(
                new Messages.OnSquadPointsChanged<TPlayer>(squad, newPoints)
            );
            await base.OnSquadPointsChanged(squad, newPoints);
        }

        public override async Task OnPlayerSpawned(TPlayer player) {
            await wss.BroadcastMessage(new Messages.OnPlayerSpawned<TPlayer>(player));
            await base.OnPlayerSpawned(player);
        }

        public override async Task OnPlayerDied(TPlayer player) {
            await wss.BroadcastMessage(new Messages.OnPlayerDied<TPlayer>(player));
            await base.OnPlayerDied(player);
        }

        public override async Task OnPlayerGivenUp(TPlayer player) {
            await wss.BroadcastMessage(new Messages.OnPlayerGivenUp<TPlayer>(player));
            await base.OnPlayerGivenUp(player);
        }

        public override async Task OnAPlayerDownedAnotherPlayer(OnPlayerKillArguments<TPlayer> args) {
            await wss.BroadcastMessage(new Messages.OnAPlayerDownedAnotherPlayer<TPlayer>(args));
            await base.OnAPlayerDownedAnotherPlayer(args);
        }

        public override async Task OnAPlayerRevivedAnotherPlayer(TPlayer from, TPlayer to) {
            await wss.BroadcastMessage(
                new Messages.OnAPlayerRevivedAnotherPlayer<TPlayer>(from, to)
            );
            await base.OnAPlayerRevivedAnotherPlayer(from, to);
        }

        public override async Task OnPlayerReported(
            TPlayer from,
            TPlayer to,
            ReportReason reason,
            string additional
        ) {
            await wss.BroadcastMessage(
                new Messages.OnPlayerReported<TPlayer>(from, to, reason, additional)
            );
            await base.OnPlayerReported(from, to, reason, additional);
        }

        public override async Task OnGameStateChanged(GameState oldState, GameState newState) {
            await wss.BroadcastMessage(new Messages.OnGameStateChanged(oldState, newState));
            await base.OnGameStateChanged(oldState, newState);
        }

        public override async Task OnRoundStarted() {
            await wss.BroadcastMessage(new Messages.OnRoundStarted());
            await base.OnRoundStarted();
        }

        public override async Task OnRoundEnded() {
            await wss.BroadcastMessage(new Messages.OnRoundEnded());
            await base.OnRoundEnded();
        }
    }
}