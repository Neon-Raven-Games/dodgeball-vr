using FishNet.Broadcast;

public readonly struct RavenMessageSegment : IBroadcast
{
    public readonly int targetClientId;
    public readonly int senderID;
    public readonly int slowSenderIndex;
    public readonly int totalSize;
    public readonly RavenDataIndex dataIndex;
    public readonly byte[] data;
    public readonly bool complete;

    public RavenMessageSegment(int senderID, int targetClientId, RavenDataIndex dataIndex, int slowSenderIndex, int totalSize,
        byte[] data, bool complete)
    {
        this.targetClientId = targetClientId;
        this.dataIndex = dataIndex;
        this.senderID = senderID;
        this.slowSenderIndex = slowSenderIndex;
        this.totalSize = totalSize;
        this.data = data;
        this.complete = complete;
    }
}