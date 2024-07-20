using System;
using UnityEngine;

public enum RavenDataIndex
{
    BallState = 0,
    BallReset = 1,
}

/// <summary>
/// Helper class to split and recombine a byte array into chunks
/// </summary>
public class RavenMessage
{
    public bool isAllocated = false;
    public int poolIndex = -1;

    public byte[] FullArray { get; private set; }
    public int BytesLeft { get; private set; }
    
    public bool HasBytesLeft() => BytesLeft > 0;

    private static int _maxSize => NeonRavenBroadcast.MAX_SIZE;
    private int _byteIndex;

    public int totalSize = 0, senderID = 0, currentSlowSenderIndex = 0, targetClientId = 0;
    public RavenDataIndex dataIndex;
    public bool sendingToServer = false;

    public RavenMessage()
    {
        currentSlowSenderIndex = 0;
        _byteIndex = 0;
        totalSize = _maxSize;
    }

    public void Initialize(int initialSize, int sendSize) =>
        Initialize(new byte[initialSize], sendSize);
    
    public void Initialize(byte[] array, int sendSize)
    {
        FullArray = array;
        totalSize = sendSize;
        currentSlowSenderIndex = 0;
        _byteIndex = 0;
        BytesLeft = FullArray.Length;
    }

    public bool ReadChunk(out byte[] buffer)
    {
        buffer = null;
        if (!HasBytesLeft()) return false;

        var bytesToSend = Mathf.Min(BytesLeft, _maxSize);
        buffer = new byte[bytesToSend];
        
        Buffer.BlockCopy(FullArray, _byteIndex, buffer, 0, bytesToSend);
        // Array.Copy(FullArray, _byteIndex, buffer, 0, bytesToSend);

        _byteIndex += bytesToSend;
        BytesLeft -= bytesToSend;

        return true;
    }

    public void WriteChunk(byte[] buffer, int index, int desiredtotalsize)
    {
        var offset = index * _maxSize;

        if (FullArray.Length < desiredtotalsize) FullArray = new byte[desiredtotalsize];
        // Array.Copy(buffer, 0, FullArray, offset, buffer.Length);
        Buffer.BlockCopy(buffer, 0, FullArray, offset, buffer.Length);
    }
}