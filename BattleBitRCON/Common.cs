using BattleBitAPI;
using BattleBitAPI.Common;

namespace BattleBitRCON.Common
{
    class PlayerInfo
    {
        public bool InVehicle { get; set; }
        public string Name { get; set; }
        public string IP { get; set; }
        public GameRole Role { get; set; }
        public Team Team { get; set; }
        public Squads Squad { get; set; }
        public string SteamID { get; set; }
        public float[] Position { get; set; }
        public bool IsDead { get; set; }
        public bool InSquad { get; set; }
        public int PingMs { get; set; }
        public bool IsSquadLeader { get; set; }
        public float HP { get; set; }

        public static PlayerInfo GetInfo<TPlayer>(Player<TPlayer> player)
            where TPlayer : Player<TPlayer>
        {
            var pos = new float[3] { player.Position.X, player.Position.Y, player.Position.Z };

            return new PlayerInfo
            {
                InVehicle = player.InVehicle,
                Name = player.Name,
                IP = player.IP.ToString(),
                Role = player.Role,
                Team = player.Team,
                Squad = player.SquadName,
                SteamID = player.SteamID.ToString(),
                Position = pos,
                IsDead = player.IsDead,
                InSquad = player.InSquad,
                PingMs = player.PingMs,
                IsSquadLeader = player.IsSquadLeader,
                HP = player.HP,
            };
        }
    }
}
