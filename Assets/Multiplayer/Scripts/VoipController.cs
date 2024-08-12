using System;
using System.Collections.Generic;
using System.Linq;
using FishNet.Object;
using FishNet.Transporting;
using UnityEngine;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

public class VoipHandler : NetworkBehaviour
{
    private bool _isRecording = false;
    private AudioSource _audioSource;
    private int _sampleRate = 44100;
    private const string VOIP_HEADER = "VOIP";


    private void OnDisable() =>
        StopRecording();

    private void InitializeModel()
    {
        try
        {
            StartRecording();
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error initializing model: {ex.Message}");
        }
    }

    [Client]
    public override void OnStartClient()
    {
        base.OnStartClient();
        NetworkManager.TransportManager.Transport.OnClientReceivedData += OnDataReceived;
        InitializeModel();
    }

    [Server]
    public override void OnStartServer()
    {
        base.OnStartServer();
        NetworkManager.TransportManager.Transport.OnServerReceivedData += OnServerDataReceived;
    }

    private void WhenDataAvailable(float[] pcmData)
    {
        var encodedData = EncodeAudioData(pcmData);
        SendInChunks(encodedData);
    }

    private void SendInChunks(byte[] data)
    {
        int chunkSize = 1000; // Adjust chunk size as needed
        int totalChunks = Mathf.CeilToInt((float) data.Length / chunkSize);
        byte[] headerBytes = System.Text.Encoding.UTF8.GetBytes(VOIP_HEADER);

        for (int i = 0; i < totalChunks; i++)
        {
            int currentChunkSize = Mathf.Min(chunkSize, data.Length - (i * chunkSize));
            byte[] chunk = new byte[headerBytes.Length + currentChunkSize + 3];
            Buffer.BlockCopy(headerBytes, 0, chunk, 0, headerBytes.Length);
            chunk[headerBytes.Length] = (byte) i; // Add chunk index after the header
            chunk[headerBytes.Length + 1] = (byte) totalChunks; // Add total chunk count after the header
            Buffer.BlockCopy(data, i * chunkSize, chunk, headerBytes.Length + 3, currentChunkSize);

            SendToServer(chunk);
        }

        Debug.Log("Chunked and set audio data with " + totalChunks + " chunks, and a total of " + data.Length +
                  " bytes.");
    }

    private static byte[] EncodeAudioData(float[] pcmData)
    {
        var headerBytes = System.Text.Encoding.UTF8.GetBytes(VOIP_HEADER);
        var audioBytes = new byte[headerBytes.Length + pcmData.Length * 4];

        Buffer.BlockCopy(headerBytes, 0, audioBytes, 0, headerBytes.Length);
        Buffer.BlockCopy(pcmData, 0, audioBytes, headerBytes.Length, pcmData.Length * 4);

        return audioBytes;
    }

    private void SendToAllOtherClients(byte[] encodedData, int connectionId)
    {
        foreach (var conn in NetworkManager.ServerManager.Clients)
        {
            if (conn.Value.ClientId == connectionId) continue;
            NetworkManager.TransportManager.Transport.SendToClient((byte) Channel.Unreliable,
                new ArraySegment<byte>(encodedData), conn.Value.ClientId);
        }
    }

    private void SendToServer(byte[] encodedData) =>
        NetworkManager.TransportManager.Transport.SendToServer((byte) Channel.Unreliable,
            new ArraySegment<byte>(encodedData));

    private readonly Dictionary<int, SortedDictionary<int, byte[]>> _receivedChunks = new();
    private readonly Dictionary<int, int> _chunkCounts = new();

    private void OnServerDataReceived(ServerReceivedDataArgs obj)
    {
        byte[] data = obj.Data.ToArray();
        if (!IsVoiceData(data)) return;

        int senderId = obj.ConnectionId;

        // Create a new array to include the sender ID
        byte[] modifiedData = new byte[data.Length + 1];
        modifiedData[0] = (byte) senderId;
        Buffer.BlockCopy(data, 0, modifiedData, 1, data.Length);

        SendToAllOtherClients(modifiedData, senderId);
    }

    private void OnDataReceived(ClientReceivedDataArgs obj)
    {
        byte[] data = obj.Data.ToArray();
        if (!IsVoiceData(data)) return;

        // Extract the sender ID
        var headerBytes = System.Text.Encoding.UTF8.GetBytes(VOIP_HEADER);

        // Extract the sender ID
        int senderId = data[headerBytes.Length];

        // Extract chunk index and total chunks
        int chunkIndex = data[headerBytes.Length + 1];
        int totalChunks = data[headerBytes.Length + 2];


        // Initialize storage for chunks if necessary
        if (!_receivedChunks.ContainsKey(senderId))
        {
            _receivedChunks[senderId] = new SortedDictionary<int, byte[]>();
            _chunkCounts[senderId] = totalChunks;
        }

        _receivedChunks[senderId][chunkIndex] = data.Skip(headerBytes.Length + 3).ToArray();

        if (_receivedChunks[senderId].Count >= totalChunks)
        {
            byte[] completeData = ReassembleChunks(_receivedChunks[senderId]);
            _receivedChunks.Remove(senderId);
            _chunkCounts.Remove(senderId);

            float[] pcmData = DecodeAudioData(completeData);
            PlayAudio(pcmData);
        }
    }

    private byte[] ReassembleChunks(SortedDictionary<int, byte[]> chunks)
    {
        List<byte> completeData = new List<byte>();
        foreach (var chunk in chunks.Values)
        {
            completeData.AddRange(chunk);
        }

        return completeData.ToArray();
    }

    private bool IsVoiceData(byte[] data)
    {
        var headerBytes = System.Text.Encoding.UTF8.GetBytes(VOIP_HEADER);
        if (data.Length < headerBytes.Length)
        {
            return false;
        }

        for (int i = 0; i < headerBytes.Length; i++)
        {
            if (data[i] != headerBytes[i])
            {
                return false;
            }
        }

        return true;
    }

    private static float[] DecodeAudioData(byte[] byteArray)
    {
        var floatArray = new float[byteArray.Length / 2];
        Buffer.BlockCopy(byteArray, 0, floatArray, 0, byteArray.Length);
        return floatArray;
    }

    private void PlayAudio(float[] pcmData)
    {
        var clip = AudioClip.Create("VoipAudio", pcmData.Length, 1, _sampleRate, false);
        clip.SetData(pcmData, 0);
        _audioSource.PlayOneShot(clip);
    }

    private void StartRecording()
    {
#if UNITY_ANDROID
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            Permission.RequestUserPermission(Permission.Microphone);
        }
#endif
        if (Microphone.devices.Length > 0)
        {
            if (!_audioSource)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
                _audioSource.spatialBlend = 1.0f; // Enable 3D spatial sound
            }

            _isRecording = true;
            var micDevice = Microphone.devices[0];
            _audioSource.clip = Microphone.Start(micDevice, true, 1, _sampleRate);
            _audioSource.loop = false; // No need to loop the AudioSource

            while (Microphone.GetPosition(micDevice) <= 0)
            {
            }

            InvokeRepeating(nameof(CaptureMicData), 0.1f, 0.1f);
        }
        else
        {
            Debug.LogError("No microphone devices found.");
        }
    }

    private void CaptureMicData()
    {
        if (!_isRecording) return;

        var micPosition = Microphone.GetPosition(null);

        if (micPosition <= 0) return;

        float[] micData = new float[micPosition * _audioSource.clip.channels];
        _audioSource.clip.GetData(micData, 0);
        WhenDataAvailable(micData);
    }

    private void StopRecording()
    {
        if (!_isRecording) return;

        _isRecording = false;
        Microphone.End(null);
        CancelInvoke(nameof(CaptureMicData));
    }
}