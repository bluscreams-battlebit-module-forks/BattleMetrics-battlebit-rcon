using BattleBitAPI;
using BattleBitAPI.Common;
using BattleBitAPI.Server;
using BattleBitRCON.Common;

namespace BattleBitRCON.Messages {
    public class OnPlayerConnected<TPlayer>
        where TPlayer : Player<TPlayer> {
        public string Type { get; } = "OnPlayerConnected";
        public PlayerInfo Player { get; set; }

        public OnPlayerConnected(TPlayer player) {
            Player = PlayerInfo.GetInfo(player);
        }
    }

    public class OnPlayerDisconnected<TPlayer>
        where TPlayer : Player<TPlayer> {
        public string Type { get; } = "OnPlayerDisconnected";
        public PlayerInfo Player { get; set; }

        public OnPlayerDisconnected(TPlayer player) {
            Player = PlayerInfo.GetInfo(player);
        }
    }

    public class OnPlayerTypedMessage<TPlayer>
        where TPlayer : Player<TPlayer> {
        public string Type { get; } = "OnPlayerTypedMessage";
        public string SteamID { get; set; }
        public ChatChannel Channel { get; set; }
        public Team Team { get; set; }

        public string Message { get; set; }

        public OnPlayerTypedMessage(TPlayer player, ChatChannel channel, string msg) {
            SteamID = player.SteamID.ToString();
            Channel = channel;
            Message = msg;
            Team = player.Team;
        }
    }

    public class OnPlayerChangedRole<TPlayer>
        where TPlayer : Player<TPlayer> {
        public string Type { get; } = "OnPlayerChangedRole";
        public string SteamID { get; set; }
        public GameRole Role { get; set; }

        public OnPlayerChangedRole(TPlayer player, GameRole role) {
            SteamID = player.SteamID.ToString();
            Role = role;
        }
    }

    public class OnPlayerJoinedSquad<TPlayer>
        where TPlayer : Player<TPlayer> {
        public string Type { get; } = "OnPlayerJoinedSquad";
        public string SteamID { get; set; }
        public Squads Squad { get; set; }
        public Team Team { get; set; }
        public bool AsLeader { get; set; }

        public OnPlayerJoinedSquad(TPlayer player, Squad<TPlayer> squad) {
            SteamID = player.SteamID.ToString();
            Squad = squad.Name;
            Team = squad.Team;
            AsLeader = squad.Leader == player;
        }
    }

    public class OnSquadLeaderChanged<TPlayer>
        where TPlayer : Player<TPlayer> {
        public string Type { get; } = "OnSquadLeaderChanged";
        public string SteamID { get; set; }
        public Squads Squad { get; set; }
        public Team Team { get; set; }

        public OnSquadLeaderChanged(Squad<TPlayer> squad, TPlayer player) {
            SteamID = player.SteamID.ToString();
            Squad = squad.Name;
            Team = squad.Team;
        }
    }

    public class OnPlayerLeftSquad<TPlayer>
        where TPlayer : Player<TPlayer> {
        public string Type { get; } = "OnPlayerLeftSquad";
        public string SteamID { get; set; }
        public Squads Squad { get; set; }

        public OnPlayerLeftSquad(TPlayer player, Squad<TPlayer> squad) {
            SteamID = player.SteamID.ToString();
            Squad = squad.Name;
        }
    }

    public class OnPlayerChangeTeam<TPlayer>
        where TPlayer : Player<TPlayer> {
        public string Type { get; } = "OnPlayerChangeTeam";
        public string steamID { get; set; }
        public Team Team { get; set; }

        public OnPlayerChangeTeam(TPlayer player, Team team) {
            steamID = player.SteamID.ToString();
            Team = team;
        }
    }

    public class OnSquadPointsChanged<TPlayer>
        where TPlayer : Player<TPlayer> {
        public string Type { get; } = "OnSquadPointsChanged";
        public Squads Squad { get; set; }
        public int NewPoints { get; set; }

        public OnSquadPointsChanged(Squad<TPlayer> squad, int newPoints) {
            Squad = squad.Name;
            NewPoints = newPoints;
        }
    }

    public class OnPlayerSpawned<TPlayer>
        where TPlayer : Player<TPlayer> {
        public string Type { get; } = "OnPlayerSpawned";
        public string SteamID { get; set; }

        public OnPlayerSpawned(TPlayer player) {
            SteamID = player.SteamID.ToString();
        }
    }

    public class OnPlayerDied<TPlayer>
        where TPlayer : Player<TPlayer> {
        public string Type { get; } = "OnPlayerDied";
        public string SteamID { get; set; }

        public OnPlayerDied(TPlayer player) {
            SteamID = player.SteamID.ToString();
        }
    }

    public class OnPlayerGivenUp<TPlayer>
        where TPlayer : Player<TPlayer> {
        public string Type { get; } = "OnPlayerGivenUp";
        public string SteamID { get; set; }

        public OnPlayerGivenUp(TPlayer player) {
            SteamID = player.SteamID.ToString();
        }
    }

    public class OnAPlayerDownedAnotherPlayer<TPlayer>
        where TPlayer : Player<TPlayer> {
        public string Type { get; } = "OnAPlayerDownedAnotherPlayer";
        public string KillerSteamID { get; set; }
        public float[] KillerPosition { get; set; }
        public string VictimSteamID { get; set; }
        public float[] VictimPosition { get; set; }
        public string KillerTool { get; set; }
        public PlayerBody BodyPart { get; set; }
        public ReasonOfDamage SourceOfDamage { get; set; }

        public OnAPlayerDownedAnotherPlayer(OnPlayerKillArguments<TPlayer> args) {
            KillerSteamID = args.Killer.SteamID.ToString();
            KillerPosition = new float[3]
            {
                args.KillerPosition.X,
                args.KillerPosition.Y,
                args.KillerPosition.Z
            };

            VictimSteamID = args.Victim.SteamID.ToString();
            VictimPosition = new float[3]
            {
                args.VictimPosition.X,
                args.VictimPosition.Y,
                args.VictimPosition.Z
            };

            KillerTool = args.KillerTool;
            BodyPart = args.BodyPart;
            SourceOfDamage = args.SourceOfDamage;
        }
    }

    public class OnAPlayerRevivedAnotherPlayer<TPlayer>
        where TPlayer : Player<TPlayer> {
        public string Type { get; } = "OnAPlayerRevivedAnotherPlayer";
        public string FromSteamID { get; set; }
        public string ToSteamID { get; set; }

        public OnAPlayerRevivedAnotherPlayer(TPlayer from, TPlayer to) {
            FromSteamID = from.SteamID.ToString();
            ToSteamID = to.SteamID.ToString();
        }
    }

    public class OnPlayerReported<TPlayer>
        where TPlayer : Player<TPlayer> {
        public string Type { get; } = "OnPlayerReported";
        public string fromSteamID { get; set; }
        public string toSteamID { get; set; }

        public ReportReason Reason { get; set; }
        public string Additional { get; set; }

        public OnPlayerReported(TPlayer from, TPlayer to, ReportReason reason, string additional) {
            fromSteamID = from.SteamID.ToString();
            toSteamID = to.SteamID.ToString();
            Reason = reason;
            Additional = additional;
        }
    }

    public class OnGameStateChanged {
        public string Type { get; } = "OnGameStateChanged";
        public GameState OldState { get; set; }
        public GameState NewState { get; set; }

        public OnGameStateChanged(GameState oldState, GameState newState) {
            OldState = oldState;
            NewState = newState;
        }
    }

    public class OnRoundStarted {
        public string Type { get; } = "OnRoundStarted";
    }

    public class OnRoundEnded {
        public string Type { get; } = "OnRoundEnded";
    }
}
