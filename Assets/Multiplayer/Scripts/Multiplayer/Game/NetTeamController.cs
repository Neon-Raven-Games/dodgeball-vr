using FishNet.Object;
using FishNet.Object.Synchronizing;

namespace Unity.Template.VR.Multiplayer
{
    public class NetTeamController : NetworkBehaviour
    {
        private readonly SyncDictionary<Team, int> _teamScores = new();
        private readonly SyncDictionary<int, Team> _teamPlayers = new();

        private static NetTeamController _instance;
        public void Awake() => _instance = this;
        
        public static void AddPointToTeam(Team team) =>
            _instance.AddScore(team);

        public static void ResetScores() =>
            _instance.ResetNetScores();
        
        public static void AddPlayerToTeam(NetworkObject player)
        {
            var team = GetTeamForPlayer();
            _instance.AddPlayerToTeam(player, team);
        }

        public static Team GetTeamForPlayer()
        {
            var playerCount = _instance.ServerManager.Clients.Count;
            return playerCount % 2 == 0 ? Team.TeamOne : Team.TeamTwo;
        }
        
        [ServerRpc(RequireOwnership = false)]
        private void ResetNetScores()
        {
            foreach (var team in _teamScores.Keys)
            {
                _teamScores[team] = 0;
                _teamScores.Dirty(team);
            }
        }
        
        [ServerRpc(RequireOwnership = false)]
        private void AddPlayerToTeam(NetworkObject player, Team team)
        {
            _teamPlayers[player.OwnerId] = team;
            _teamPlayers.Dirty(player.OwnerId);
            player.GetComponent<NetworkPlayer>().SetTeam(team);
        }
        
        [ServerRpc(RequireOwnership = false)]
        private void AddScore(Team team)
        {
            if (_teamScores.ContainsKey(team)) _teamScores[team]++;
            else _teamScores[team] = 1;

            _teamScores.Dirty(team);
        }
    }
}