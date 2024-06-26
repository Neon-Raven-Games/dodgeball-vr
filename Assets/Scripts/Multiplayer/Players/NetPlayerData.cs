using Fusion;

namespace Unity.Template.VR.Multiplayer.Players
{
    public struct NetPlayerData : INetworkStruct
    {
        public BallType leftBall;
        public BallType rightBall;
        public Team team;
    }

    public struct BallData : INetworkStruct
    {
        public BallType ballType;
        public Team team;
        public NetworkId owner;
    }
}