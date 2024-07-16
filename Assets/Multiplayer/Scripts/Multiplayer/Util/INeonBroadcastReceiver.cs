
/// <summary>
/// The interface to implement for receiving messages from the server. Implement this on any class that needs to receive messages.
/// The class is not limited to network behavior, so MonoBehaviours are OK!
/// </summary>
public interface INeonBroadcastReceiver
{
    /// <summary>
    /// The callback for whenever a message is received from the server.
    /// </summary>
    /// <param name="data">Full data packet from the sent message that was lazy loaded.</param>
    /// <param name="senderId">The sender of the data's client Id, relayed from the server.</param>
    /// <param name="dataIndex">
    /// The type of data you defined it as.
    /// Helpful for indexing the data type on interface implementations.
    /// </param>
    public void ReceiveLazyLoadedMessage(byte[] data, int senderId, RavenDataIndex dataIndex);
}
