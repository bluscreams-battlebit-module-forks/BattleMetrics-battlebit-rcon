using BattleBitAPI;
using BattleBitAPI.Common;
using BattleBitAPI.Server;
using BattleBitRCON.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.Json;

namespace BattleBitRCON.Commands {

    internal class CommandType {
        public string? Command { get; set; }

        public uint? Identifier { get; set; }
    }

    internal abstract class BaseCommand {

        public static readonly JsonSerializerOptions JsonSerializationOptions =
            new JsonSerializerOptions {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                IgnoreReadOnlyFields = false,
            };

        public string? Command {
            get {
                return JsonNamingPolicy.CamelCase.ConvertName(
                    (GetType().Namespace ?? "").Split(".").Last()
                );
            }
        }

        public uint? Identifier { get; set; }
    }

    internal class InvalidCommand : Exception {
        public const string Type = "error";

        public new string Message;

        public InvalidCommand(string? type) {
            Message = $"Invalid command: {type}";
        }
    }

    namespace Ping {

        internal class Request<TPlayer> : BaseCommand
            where TPlayer : Player<TPlayer> {

            public static Request<TPlayer>? Parse(ArraySegment<byte> json) {
                return JsonSerializer.Deserialize<Request<TPlayer>>(json, JsonSerializationOptions);
            }

            public static Response Execute(GameServer<TPlayer> gameServer, Request<TPlayer> cmd) {
                return new Response(DateTime.Now) { Identifier = cmd.Identifier };
            }
        }

        internal class Response : BaseCommand {
            public string Message { get; set; } = "pong";
            public DateTime Timestamp { get; set; }

            public Response(DateTime timestamp) {
                Timestamp = timestamp;
            }
        }
    }

    namespace PlayerList {

        internal class Request<TPlayer> : BaseCommand
            where TPlayer : Player<TPlayer> {

            public static Request<TPlayer>? Parse(ArraySegment<byte> json) {
                return JsonSerializer.Deserialize<Request<TPlayer>>(json, JsonSerializationOptions);
            }

            public static Response<TPlayer> Execute(
                GameServer<TPlayer> gameServer,
                Request<TPlayer> cmd
            ) {
                return new Response<TPlayer>(gameServer.AllPlayers) { Identifier = cmd.Identifier };
            }
        }

        internal class Response<TPlayer> : Request<TPlayer>
            where TPlayer : Player<TPlayer> {
            public List<PlayerInfo> Players { get; set; }

            public Response(IEnumerable<Player<TPlayer>> players) {
                Players = new List<PlayerInfo>();

                foreach (var player in players) {
                    Players.Add(PlayerInfo.GetInfo(player));
                }
            }
        }
    }

    namespace State {

        internal class Request<TPlayer> : BaseCommand
            where TPlayer : Player<TPlayer> {

            public static Request<TPlayer>? Parse(ArraySegment<byte> json) {
                return JsonSerializer.Deserialize<Request<TPlayer>>(json, JsonSerializationOptions);
            }

            public static Response<TPlayer> Execute(
                GameServer<TPlayer> gameServer,
                Request<TPlayer> cmd
            ) {
                return new Response<TPlayer>(
                    gameServer.ServerName,
                    gameServer.Map,
                    gameServer.MapSize,
                    gameServer.Gamemode,
                    gameServer.DayNight,
                    gameServer.MaxPlayerCount,
                    gameServer.AllPlayers
                ) {
                    Identifier = cmd.Identifier
                };
            }
        }

        internal class Response<TPlayer> : Request<TPlayer>
            where TPlayer : Player<TPlayer> {
            public string ServerName { get; set; }
            public string MapName { get; set; }
            public MapSize MapSize { get; set; }
            public string GameMode { get; set; }
            public bool IsDay { get; set; }
            public int MaxPlayers { get; set; }
            public List<PlayerInfo> Players { get; set; }

            public Response(
                string serverName,
                string mapName,
                MapSize mapSize,
                string gameMode,
                MapDayNight dayNight,
                int maxPlayers,
                IEnumerable<Player<TPlayer>> players
            ) {
                ServerName = serverName;
                MapName = mapName;
                GameMode = gameMode;
                IsDay = dayNight == MapDayNight.Day;
                MapSize = mapSize;
                MaxPlayers = maxPlayers;
                Players = new List<PlayerInfo>();

                foreach (var player in players) {
                    Players.Add(PlayerInfo.GetInfo(player));
                }
            }
        }
    }

    namespace Kick {

        internal class Request<TPlayer> : BaseCommand
            where TPlayer : Player<TPlayer> {

            public static Request<TPlayer>? Parse(ArraySegment<byte> json) {
                return JsonSerializer.Deserialize<Request<TPlayer>>(json, JsonSerializationOptions);
            }

            public string SteamID { get; set; }
            public string Reason { get; set; }

            public static Response Execute(GameServer<TPlayer> gameServer, Request<TPlayer> cmd) {
                gameServer.Kick(Convert.ToUInt64(cmd.SteamID), cmd.Reason);

                return new Response { Identifier = cmd.Identifier };
            }

            public Request(string SteamID, string Reason) {
                this.SteamID = SteamID;
                this.Reason = Reason;
            }
        }

        internal class Response : BaseCommand { }
    }

    namespace MessagePlayer {

        internal class Request<TPlayer> : BaseCommand
            where TPlayer : Player<TPlayer> {

            public static Request<TPlayer>? Parse(ArraySegment<byte> json) {
                return JsonSerializer.Deserialize<Request<TPlayer>>(json, JsonSerializationOptions);
            }

            public string SteamID { get; set; }
            public string Message { get; set; }
            public float FadeOutTime { get; set; }

            public static Response Execute(GameServer<TPlayer> gameServer, Request<TPlayer> cmd) {
                gameServer.MessageToPlayer(
                    Convert.ToUInt64(cmd.SteamID),
                    cmd.Message,
                    cmd.FadeOutTime
                );

                return new Response { Identifier = cmd.Identifier };
            }

            public Request(string SteamID, string Message, float FadeOutTime) {
                this.SteamID = SteamID;
                this.Message = Message;
                this.FadeOutTime = FadeOutTime;
            }
        }

        internal class Response : BaseCommand { }
    }

    namespace SetNewPassword {

        internal class Request<TPlayer> : BaseCommand
            where TPlayer : Player<TPlayer> {

            public static Request<TPlayer>? Parse(ArraySegment<byte> json) {
                return JsonSerializer.Deserialize<Request<TPlayer>>(json, JsonSerializationOptions);
            }

            public string NewPassword { get; set; }

            public static Response Execute(GameServer<TPlayer> gameServer, Request<TPlayer> cmd) {
                gameServer.SetNewPassword(cmd.NewPassword);

                return new Response { Identifier = cmd.Identifier };
            }

            public Request(string newPassword) {
                NewPassword = newPassword;
            }
        }

        internal class Response : BaseCommand { }
    }

    namespace SetPingLimit {

        internal class Request<TPlayer> : BaseCommand
            where TPlayer : Player<TPlayer> {

            public static Request<TPlayer>? Parse(ArraySegment<byte> json) {
                return JsonSerializer.Deserialize<Request<TPlayer>>(json, JsonSerializationOptions);
            }

            public int NewPing { get; set; }

            public static Response Execute(GameServer<TPlayer> gameServer, Request<TPlayer> cmd) {
                gameServer.SetPingLimit(cmd.NewPing);
                return new Response { Identifier = cmd.Identifier };
            }

            public Request(int newPing) {
                NewPing = newPing;
            }
        }

        internal class Response : BaseCommand { }
    }

    namespace AnnounceShort {

        internal class Request<TPlayer> : BaseCommand
            where TPlayer : Player<TPlayer> {

            public static Request<TPlayer>? Parse(ArraySegment<byte> json) {
                return JsonSerializer.Deserialize<Request<TPlayer>>(json, JsonSerializationOptions);
            }

            public string Message { get; set; }

            public static Response Execute(GameServer<TPlayer> gameServer, Request<TPlayer> cmd) {
                gameServer.AnnounceShort(cmd.Message);
                return new Response { Identifier = cmd.Identifier };
            }

            public Request(string message) {
                Message = message;
            }
        }

        internal class Response : BaseCommand { }
    }

    namespace AnnounceLong {

        internal class Request<TPlayer> : BaseCommand
            where TPlayer : Player<TPlayer> {

            public static Request<TPlayer>? Parse(ArraySegment<byte> json) {
                return JsonSerializer.Deserialize<Request<TPlayer>>(json, JsonSerializationOptions);
            }

            public string Message { get; set; }

            public static Response Execute(GameServer<TPlayer> gameServer, Request<TPlayer> cmd) {
                gameServer.AnnounceLong(cmd.Message);
                return new Response { Identifier = cmd.Identifier };
            }

            public Request(string message) {
                Message = message;
            }
        }

        internal class Response : BaseCommand { }
    }

    namespace UILogOnServer {

        internal class Request<TPlayer> : BaseCommand
            where TPlayer : Player<TPlayer> {

            public static Request<TPlayer>? Parse(ArraySegment<byte> json) {
                return JsonSerializer.Deserialize<Request<TPlayer>>(json, JsonSerializationOptions);
            }

            public string Message { get; set; }
            public float MessageLifetime { get; set; }

            public static Response Execute(GameServer<TPlayer> gameServer, Request<TPlayer> cmd) {
                gameServer.UILogOnServer(cmd.Message, cmd.MessageLifetime);
                return new Response { Identifier = cmd.Identifier };
            }

            public Request(string message, float messageLifetime) {
                Message = message;
                MessageLifetime = messageLifetime;
            }
        }

        internal class Response : BaseCommand { }
    }

    namespace ForceStartGame {

        internal class Request<TPlayer> : BaseCommand
            where TPlayer : Player<TPlayer> {

            public static Request<TPlayer>? Parse(ArraySegment<byte> json) {
                return JsonSerializer.Deserialize<Request<TPlayer>>(json, JsonSerializationOptions);
            }

            public static Response Execute(GameServer<TPlayer> gameServer, Request<TPlayer> cmd) {
                gameServer.ForceStartGame();
                return new Response { Identifier = cmd.Identifier };
            }
        }

        internal class Response : BaseCommand { }
    }

    namespace ForceEndGame {

        internal class Request<TPlayer> : BaseCommand
            where TPlayer : Player<TPlayer> {

            public static Request<TPlayer>? Parse(ArraySegment<byte> json) {
                return JsonSerializer.Deserialize<Request<TPlayer>>(json, JsonSerializationOptions);
            }

            public static Response Execute(GameServer<TPlayer> gameServer, Request<TPlayer> cmd) {
                gameServer.ForceEndGame();
                return new Response { Identifier = cmd.Identifier };
            }
        }

        internal class Response : BaseCommand { }
    }

    namespace SayToAllChat {

        internal class Request<TPlayer> : BaseCommand
            where TPlayer : Player<TPlayer> {

            public static Request<TPlayer>? Parse(ArraySegment<byte> json) {
                return JsonSerializer.Deserialize<Request<TPlayer>>(json, JsonSerializationOptions);
            }

            public string Message { get; set; }

            public static Response Execute(GameServer<TPlayer> gameServer, Request<TPlayer> cmd) {
                gameServer.SayToAllChat(cmd.Message);
                return new Response { Identifier = cmd.Identifier };
            }

            public Request(string message) {
                Message = message;
            }
        }

        internal class Response : BaseCommand { }
    }

    namespace SayToChat {

        internal class Request<TPlayer> : BaseCommand
            where TPlayer : Player<TPlayer> {

            public static Request<TPlayer>? Parse(ArraySegment<byte> json) {
                return JsonSerializer.Deserialize<Request<TPlayer>>(json, JsonSerializationOptions);
            }

            public string Message { get; set; }

            public string SteamID { get; set; }

            public static Response Execute(GameServer<TPlayer> gameServer, Request<TPlayer> cmd) {
                gameServer.SayToChat(cmd.Message, Convert.ToUInt64(cmd.SteamID));
                return new Response { Identifier = cmd.Identifier };
            }

            public Request(string message, string steamID) {
                Message = message;
                SteamID = steamID;
            }
        }

        internal class Response : BaseCommand { }
    }

    namespace SetLoadingScreenText {

        internal class Request<TPlayer> : BaseCommand
            where TPlayer : Player<TPlayer> {

            public static Request<TPlayer>? Parse(ArraySegment<byte> json) {
                return JsonSerializer.Deserialize<Request<TPlayer>>(json, JsonSerializationOptions);
            }

            public string Message { get; set; }

            public static Response Execute(GameServer<TPlayer> gameServer, Request<TPlayer> cmd) {
                gameServer.SetLoadingScreenText(cmd.Message);
                return new Response { Identifier = cmd.Identifier };
            }

            public Request(string message) {
                Message = message;
            }
        }

        internal class Response : BaseCommand { }
    }

    namespace SetRulesScreenText {

        internal class Request<TPlayer> : BaseCommand
            where TPlayer : Player<TPlayer> {

            public static Request<TPlayer>? Parse(ArraySegment<byte> json) {
                return JsonSerializer.Deserialize<Request<TPlayer>>(json, JsonSerializationOptions);
            }

            public string Message { get; set; }

            public static Response Execute(GameServer<TPlayer> gameServer, Request<TPlayer> cmd) {
                gameServer.SetRulesScreenText(cmd.Message);
                return new Response { Identifier = cmd.Identifier };
            }

            public Request(string message) {
                Message = message;
            }
        }

        internal class Response : BaseCommand { }
    }

    namespace StopServer {

        internal class Request<TPlayer> : BaseCommand
            where TPlayer : Player<TPlayer> {

            public static Request<TPlayer>? Parse(ArraySegment<byte> json) {
                return JsonSerializer.Deserialize<Request<TPlayer>>(json, JsonSerializationOptions);
            }

            public static Response Execute(GameServer<TPlayer> gameServer, Request<TPlayer> cmd) {
                gameServer.StopServer();
                return new Response { Identifier = cmd.Identifier };
            }

            public Request() {
            }
        }

        internal class Response : BaseCommand { }
    }

    namespace CloseServer {

        internal class Request<TPlayer> : BaseCommand
            where TPlayer : Player<TPlayer> {

            public static Request<TPlayer>? Parse(ArraySegment<byte> json) {
                return JsonSerializer.Deserialize<Request<TPlayer>>(json, JsonSerializationOptions);
            }

            public static Response Execute(GameServer<TPlayer> gameServer, Request<TPlayer> cmd) {
                gameServer.CloseServer();
                return new Response { Identifier = cmd.Identifier };
            }

            public Request() {
            }
        }

        internal class Response : BaseCommand { }
    }

    namespace KickAllPlayers {

        internal class Request<TPlayer> : BaseCommand
            where TPlayer : Player<TPlayer> {

            public static Request<TPlayer>? Parse(ArraySegment<byte> json) {
                return JsonSerializer.Deserialize<Request<TPlayer>>(json, JsonSerializationOptions);
            }

            public static Response Execute(GameServer<TPlayer> gameServer, Request<TPlayer> cmd) {
                gameServer.KickAllPlayers();
                return new Response { Identifier = cmd.Identifier };
            }

            public Request() {
            }
        }

        internal class Response : BaseCommand { }
    }

    namespace Kill {

        internal class Request<TPlayer> : BaseCommand
            where TPlayer : Player<TPlayer> {

            public static Request<TPlayer>? Parse(ArraySegment<byte> json) {
                return JsonSerializer.Deserialize<Request<TPlayer>>(json, JsonSerializationOptions);
            }

            public string SteamID { get; set; }

            public static Response Execute(GameServer<TPlayer> gameServer, Request<TPlayer> cmd) {
                gameServer.Kill(Convert.ToUInt64(cmd.SteamID));
                return new Response { Identifier = cmd.Identifier };
            }

            public Request(string steamID) {
                SteamID = steamID;
            }
        }

        internal class Response : BaseCommand { }
    }

    namespace ChangeTeam {

        internal class Request<TPlayer> : BaseCommand
            where TPlayer : Player<TPlayer> {

            public static Request<TPlayer>? Parse(ArraySegment<byte> json) {
                return JsonSerializer.Deserialize<Request<TPlayer>>(json, JsonSerializationOptions);
            }

            public string SteamID { get; set; }
            public int Team { get; set; }

            public static Response Execute(GameServer<TPlayer> gameServer, Request<TPlayer> cmd) {
                gameServer.ChangeTeam(Convert.ToUInt64(cmd.SteamID), (Team)cmd.Team);
                return new Response { Identifier = cmd.Identifier };
            }

            public Request(string steamID, int team) {
                SteamID = steamID;
                Team = team;
            }
        }

        internal class Response : BaseCommand { }
    }

    namespace KickFromSquad {

        internal class Request<TPlayer> : BaseCommand
            where TPlayer : Player<TPlayer> {

            public static Request<TPlayer>? Parse(ArraySegment<byte> json) {
                return JsonSerializer.Deserialize<Request<TPlayer>>(json, JsonSerializationOptions);
            }

            public string SteamID { get; set; }

            public static Response Execute(GameServer<TPlayer> gameServer, Request<TPlayer> cmd) {
                gameServer.KickFromSquad(Convert.ToUInt64(cmd.SteamID));
                return new Response { Identifier = cmd.Identifier };
            }

            public Request(string steamID) {
                SteamID = steamID;
            }
        }

        internal class Response : BaseCommand { }
    }

    namespace JoinSquad {

        internal class Request<TPlayer> : BaseCommand
            where TPlayer : Player<TPlayer> {

            public static Request<TPlayer>? Parse(ArraySegment<byte> json) {
                return JsonSerializer.Deserialize<Request<TPlayer>>(json, JsonSerializationOptions);
            }

            public string SteamID { get; set; }
            public int TargetSquad { get; set; }

            public static Response Execute(GameServer<TPlayer> gameServer, Request<TPlayer> cmd) {
                gameServer.JoinSquad(Convert.ToUInt64(cmd.SteamID), (Squads)cmd.TargetSquad);
                return new Response { Identifier = cmd.Identifier };
            }

            public Request(string steamID, int targetSquad) {
                SteamID = steamID;
                TargetSquad = targetSquad;
            }
        }

        internal class Response : BaseCommand { }
    }

    namespace DisbandPlayerSquad {

        internal class Request<TPlayer> : BaseCommand
            where TPlayer : Player<TPlayer> {

            public static Request<TPlayer>? Parse(ArraySegment<byte> json) {
                return JsonSerializer.Deserialize<Request<TPlayer>>(json, JsonSerializationOptions);
            }

            public string SteamID { get; set; }

            public static Response Execute(GameServer<TPlayer> gameServer, Request<TPlayer> cmd) {
                gameServer.DisbandPlayerSquad(Convert.ToUInt64(cmd.SteamID));
                return new Response { Identifier = cmd.Identifier };
            }

            public Request(string steamID) {
                SteamID = steamID;
            }
        }

        internal class Response : BaseCommand { }
    }

    namespace PromoteSquadLeader {

        internal class Request<TPlayer> : BaseCommand
            where TPlayer : Player<TPlayer> {

            public static Request<TPlayer>? Parse(ArraySegment<byte> json) {
                return JsonSerializer.Deserialize<Request<TPlayer>>(json, JsonSerializationOptions);
            }

            public string SteamID { get; set; }

            public static Response Execute(GameServer<TPlayer> gameServer, Request<TPlayer> cmd) {
                gameServer.PromoteSquadLeader(Convert.ToUInt64(cmd.SteamID));
                return new Response { Identifier = cmd.Identifier };
            }

            public Request(string steamID) {
                SteamID = steamID;
            }
        }

        internal class Response : BaseCommand { }
    }

    namespace Teleport {

        internal class Request<TPlayer> : BaseCommand
            where TPlayer : Player<TPlayer> {

            public static Request<TPlayer>? Parse(ArraySegment<byte> json) {
                return JsonSerializer.Deserialize<Request<TPlayer>>(json, JsonSerializationOptions);
            }

            public string SteamID { get; set; }
            public float X { get; set; }
            public float Y { get; set; }
            public float Z { get; set; }

            public static Response Execute(GameServer<TPlayer> gameServer, Request<TPlayer> cmd) {
                gameServer.Teleport(
                    Convert.ToUInt64(cmd.SteamID),
                    new Vector3 {
                        X = cmd.X,
                        Y = cmd.Y,
                        Z = cmd.Z,
                    }
                );
                return new Response { Identifier = cmd.Identifier };
            }

            public Request(string steamID, float x, float y, float z) {
                SteamID = steamID;
                X = x;
                Y = y;
                Z = z;
            }
        }

        internal class Response : BaseCommand { }
    }

    namespace WarnPlayer {

        internal class Request<TPlayer> : BaseCommand
            where TPlayer : Player<TPlayer> {

            public static Request<TPlayer>? Parse(ArraySegment<byte> json) {
                return JsonSerializer.Deserialize<Request<TPlayer>>(json, JsonSerializationOptions);
            }

            public string SteamID { get; set; }
            public string Message { get; set; }

            public static Response Execute(GameServer<TPlayer> gameServer, Request<TPlayer> cmd) {
                gameServer.WarnPlayer(Convert.ToUInt64(cmd.SteamID), cmd.Message);
                return new Response { Identifier = cmd.Identifier };
            }

            public Request(string steamID, string message) {
                SteamID = steamID;
                Message = message;
            }
        }

        internal class Response : BaseCommand { }
    }

    namespace SetRoleTo {

        internal class Request<TPlayer> : BaseCommand
            where TPlayer : Player<TPlayer> {

            public static Request<TPlayer>? Parse(ArraySegment<byte> json) {
                return JsonSerializer.Deserialize<Request<TPlayer>>(json, JsonSerializationOptions);
            }

            public string SteamID { get; set; }
            public string Role { get; set; }

            public static Response Execute(GameServer<TPlayer> gameServer, Request<TPlayer> cmd) {
                gameServer.SetRoleTo(
                    Convert.ToUInt64(cmd.SteamID),
                    (GameRole)Enum.Parse(typeof(GameRole), cmd.Role)
                );
                return new Response { Identifier = cmd.Identifier };
            }

            public Request(string steamID, string role) {
                SteamID = steamID;
                Role = role;
            }
        }

        internal class Response : BaseCommand { }
    }

    namespace SetHP {

        internal class Request<TPlayer> : BaseCommand
            where TPlayer : Player<TPlayer> {

            public static Request<TPlayer>? Parse(ArraySegment<byte> json) {
                return JsonSerializer.Deserialize<Request<TPlayer>>(json, JsonSerializationOptions);
            }

            public string SteamID { get; set; }
            public float HP { get; set; }

            public static Response Execute(GameServer<TPlayer> gameServer, Request<TPlayer> cmd) {
                gameServer.SetHP(Convert.ToUInt64(cmd.SteamID), cmd.HP);
                return new Response { Identifier = cmd.Identifier };
            }

            public Request(string steamID, float hp) {
                SteamID = steamID;
                HP = hp;
            }
        }

        internal class Response : BaseCommand { }
    }

    namespace GiveDamage {

        internal class Request<TPlayer> : BaseCommand
            where TPlayer : Player<TPlayer> {

            public static Request<TPlayer>? Parse(ArraySegment<byte> json) {
                return JsonSerializer.Deserialize<Request<TPlayer>>(json, JsonSerializationOptions);
            }

            public string SteamID { get; set; }
            public float Damage { get; set; }

            public static Response Execute(GameServer<TPlayer> gameServer, Request<TPlayer> cmd) {
                gameServer.GiveDamage(Convert.ToUInt64(cmd.SteamID), cmd.Damage);
                return new Response { Identifier = cmd.Identifier };
            }

            public Request(string steamID, float damage) {
                SteamID = steamID;
                Damage = damage;
            }
        }

        internal class Response : BaseCommand { }
    }

    namespace Heal {

        internal class Request<TPlayer> : BaseCommand
            where TPlayer : Player<TPlayer> {

            public static Request<TPlayer>? Parse(ArraySegment<byte> json) {
                return JsonSerializer.Deserialize<Request<TPlayer>>(json, JsonSerializationOptions);
            }

            public string SteamID { get; set; }
            public float Heal { get; set; }

            public static Response Execute(GameServer<TPlayer> gameServer, Request<TPlayer> cmd) {
                gameServer.Heal(Convert.ToUInt64(cmd.SteamID), cmd.Heal);
                return new Response { Identifier = cmd.Identifier };
            }

            public Request(string steamID, float heal) {
                SteamID = steamID;
                Heal = heal;
            }
        }

        internal class Response : BaseCommand { }
    }

    namespace SetSquadPointsOf {

        internal class Request<TPlayer> : BaseCommand
            where TPlayer : Player<TPlayer> {

            public static Request<TPlayer>? Parse(ArraySegment<byte> json) {
                return JsonSerializer.Deserialize<Request<TPlayer>>(json, JsonSerializationOptions);
            }

            public int Team { get; set; }
            public int Squad { get; set; }
            public int Points { get; set; }

            public static Response Execute(GameServer<TPlayer> gameServer, Request<TPlayer> cmd) {
                gameServer.SetSquadPointsOf((Team)cmd.Team, (Squads)cmd.Squad, cmd.Points);
                return new Response { Identifier = cmd.Identifier };
            }

            public Request(int team, int squad, int points) {
                Team = team;
                Squad = squad;
                Points = points;
            }
        }

        internal class Response : BaseCommand { }
    }
}