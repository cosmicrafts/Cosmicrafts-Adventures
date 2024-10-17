using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Networking.Transport;
using TMPro; // Import for TextMeshPro support

public class PingClientBehaviour : MonoBehaviour
{
    public TMP_Text pingText; // TextMeshPro Text to display the ping (RTT)
    
    // Frequency (in seconds) at which to send ping messages.
    private const float PingFrequency = 1.0f;

    private NetworkDriver m_ClientDriver;
    private NativeReference<NetworkConnection> m_ClientConnection;
    private NativeReference<int> m_PingLastRTT;
    private int m_PingCount;
    private float m_LastSendTime;
    private JobHandle m_ClientJobHandle;
    
    public string ServerAddress = "127.0.0.1";
    public ushort ServerPort = 7777;

    private void Start()
    {
        // Create a NetworkDriver for sending/receiving data
        m_ClientDriver = NetworkDriver.Create();

        // Initialize the connection and RTT as NativeReferences
        m_ClientConnection = new NativeReference<NetworkConnection>(Allocator.Persistent);
        m_PingLastRTT = new NativeReference<int>(Allocator.Persistent);

        // Start connection to the server
        ConnectToServer(ServerAddress, ServerPort);
    }

    private void OnDestroy()
    {
        // Complete all pending jobs before disposing of resources
        m_ClientJobHandle.Complete();

        m_ClientDriver.Dispose();
        m_ClientConnection.Dispose();
        m_PingLastRTT.Dispose();
    }

    private void ConnectToServer(string ipAddress, ushort port)
    {
        // Parse the endpoint
        var endpoint = NetworkEndpoint.Parse(ipAddress, port);

        // Establish a connection to the server
        m_ClientConnection.Value = m_ClientDriver.Connect(endpoint);
        Debug.Log($"Connecting to server at {ipAddress}:{port}");
    }

    // Job for sending ping messages to the server
    [BurstCompile]
    private struct PingSendJob : IJob
    {
        public NetworkDriver.Concurrent Driver;
        public NetworkConnection Connection;
        public float CurrentTime;

        public void Execute()
        {
            var result = Driver.BeginSend(Connection, out var writer);
            if (result < 0)
            {
                Debug.LogError($"Failed to send ping (error code {result}).");
                return;
            }

            // Write the current timestamp in the ping message for RTT calculation
            writer.WriteFloat(CurrentTime);

            result = Driver.EndSend(writer);
            if (result < 0)
            {
                Debug.LogError($"Failed to send ping (error code {result}).");
                return;
            }
        }
    }

    // Job for receiving ping responses from the server and calculating RTT
    [BurstCompile]
    private struct PingUpdateJob : IJob
    {
        public NetworkDriver Driver;
        public NativeReference<NetworkConnection> Connection;
        public NativeReference<int> PingLastRTT;
        public float CurrentTime;

        public void Execute()
        {
            NetworkEvent.Type eventType;
            while ((eventType = Connection.Value.PopEvent(Driver, out var reader)) != NetworkEvent.Type.Empty)
            {
                switch (eventType)
                {
                    case NetworkEvent.Type.Connect:
                        Debug.Log("Connected to server.");
                        break;

                    case NetworkEvent.Type.Disconnect:
                        Debug.Log("Disconnected from server.");
                        Connection.Value = default;
                        break;

                    case NetworkEvent.Type.Data:
                        // Calculate RTT as the time difference between send and receive
                        PingLastRTT.Value = (int)((CurrentTime - reader.ReadFloat()) * 1000); // Convert to ms
                        break;
                }
            }
        }
    }

    private void Update()
    {
        // Complete previous job chain before starting new jobs
        m_ClientJobHandle.Complete();

        if (!m_ClientConnection.Value.IsCreated)
        {
            // Retry connection if it's not established
            ConnectToServer(ServerAddress, ServerPort);
        }

        // Update current time for sending pings
        var currentTime = Time.realtimeSinceStartup;

        // Schedule the ping update job to handle responses
        var updateJob = new PingUpdateJob
        {
            Driver = m_ClientDriver,
            Connection = m_ClientConnection,
            PingLastRTT = m_PingLastRTT,
            CurrentTime = currentTime
        };

        // If connected, send a ping every PingFrequency seconds
        if (m_ClientDriver.GetConnectionState(m_ClientConnection.Value) == NetworkConnection.State.Connected &&
            currentTime - m_LastSendTime >= PingFrequency)
        {
            m_LastSendTime = currentTime;
            m_PingCount++;

            // Schedule the job to send a ping
            m_ClientJobHandle = new PingSendJob
            {
                Driver = m_ClientDriver.ToConcurrent(),
                Connection = m_ClientConnection.Value,
                CurrentTime = currentTime
            }.Schedule();
        }

        // Schedule the job chain (driver update and ping update)
        m_ClientJobHandle = m_ClientDriver.ScheduleUpdate(m_ClientJobHandle);
        m_ClientJobHandle = updateJob.Schedule(m_ClientJobHandle);

        // Make sure to complete the jobs before reading RTT
        m_ClientJobHandle.Complete();

        // Update the pingText with the latest RTT (Round Trip Time)
        pingText.text = $"Ping: {m_PingLastRTT.Value} ms";
    }
}
