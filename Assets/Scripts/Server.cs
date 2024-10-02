using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport;
using Unity.Networking.Transport.TLS;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;

public class Server : MonoBehaviour
{
    private NetworkDriver m_Driver;
    private NativeList<NetworkConnection> m_Connections;
    private JobHandle serverJobHandle;

    void Start()
    {
        // Set up secure server parameters and UnityTransport before anything else
        SetupSecureServer();

        // Set up NetworkDriver
        m_Driver = NetworkDriver.Create();
        m_Connections = new NativeList<NetworkConnection>(16, Allocator.Persistent);

        // Configure the endpoint to bind to all network interfaces on port 7777
        var endpoint = NetworkEndpoint.AnyIpv4;
        endpoint.Port = 7777;

        // Bind the server
        if (m_Driver.Bind(endpoint) != 0)
        {
            Debug.LogError("Failed to bind to port 7777");
            return;
        }

        Debug.Log("Server bound to port 7777, listening for connections.");
        m_Driver.Listen();

        // Start Unity Netcode Server (NetworkManager) if in batch mode
        if (Application.isBatchMode)
        {
            Debug.Log("Running in batch mode - Starting Unity Netcode server automatically.");
            NetworkManager.Singleton.StartServer();
        }
    }

    private void SetupSecureServer()
    {
        // Get UnityTransport and configure it
        if (NetworkManager.Singleton != null)
        {
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            if (transport != null)
            {
                Debug.Log("Setting up secure server...");

                // Set secure server parameters for encrypted communication
                transport.SetConnectionData("0.0.0.0", 7777); // Bind address
                transport.SetServerSecrets(
                    SecureParameters.MyGameServerCertificate,
                    SecureParameters.MyGameServerPrivateKey);

                Debug.Log("Secure server configured with certificate and private key.");
            }
            else
            {
                Debug.LogWarning("UnityTransport component not found on NetworkManager.");
            }
        }
        else
        {
            Debug.LogWarning("NetworkManager not found in the scene.");
        }
    }

    void Update()
    {
        // Complete jobs from the previous frame
        serverJobHandle.Complete();

        if (!m_Driver.IsCreated)
        {
            Debug.LogWarning("NetworkDriver is not created properly.");
            return;
        }

        // Schedule jobs for handling connections and server updates
        var connectionJob = new ServerUpdateConnectionsJob
        {
            driver = m_Driver,
            connections = m_Connections
        };

        var serverUpdateJob = new ServerUpdateJob
        {
            driver = m_Driver.ToConcurrent(),
            connections = m_Connections.AsDeferredJobArray()
        };

        // Chain and schedule the jobs
        serverJobHandle = m_Driver.ScheduleUpdate();
        serverJobHandle = connectionJob.Schedule(serverJobHandle);
        serverJobHandle = serverUpdateJob.Schedule(m_Connections, 1, serverJobHandle);
    }

    void OnDestroy()
    {
        // Complete jobs before cleaning up
        if (serverJobHandle.IsCompleted == false)
        {
            serverJobHandle.Complete();
        }

        // Dispose of native arrays and network driver safely
        if (m_Connections.IsCreated)
        {
            for (int i = 0; i < m_Connections.Length; i++)
            {
                if (m_Connections[i].IsCreated)
                {
                    m_Connections[i].Disconnect(m_Driver);
                    m_Connections[i] = default;
                }
            }

            m_Connections.Dispose();
        }

        if (m_Driver.IsCreated)
        {
            m_Driver.Dispose();
        }
    }

    [BurstCompile]
    struct ServerUpdateConnectionsJob : IJob
    {
        public NetworkDriver driver;
        public NativeList<NetworkConnection> connections;

        public void Execute()
        {
            // Clean up any closed connections
            for (int i = 0; i < connections.Length; i++)
            {
                if (!connections[i].IsCreated)
                {
                    connections.RemoveAtSwapBack(i);
                    --i;
                }
            }

            // Accept new connections
            NetworkConnection connection;
            while ((connection = driver.Accept()) != default(NetworkConnection))
            {
                connections.Add(connection);
                Debug.Log("Accepted a new connection");
            }
        }
    }

    [BurstCompile]
    struct ServerUpdateJob : IJobParallelForDefer
    {
        public NetworkDriver.Concurrent driver;
        public NativeArray<NetworkConnection> connections;

        public void Execute(int index)
        {
            NetworkConnection connection = connections[index];
            DataStreamReader stream;

            Unity.Networking.Transport.NetworkEvent.Type cmd;
            while ((cmd = driver.PopEventForConnection(connection, out stream)) != Unity.Networking.Transport.NetworkEvent.Type.Empty)
            {
                if (cmd == Unity.Networking.Transport.NetworkEvent.Type.Data)
                {
                    // Handle incoming data (For demonstration, echoing received data)
                    uint receivedData = stream.ReadUInt();
                    Debug.Log($"Received {receivedData} from Client, processing...");

                    // Modify data and send it back to the client
                    driver.BeginSend(connection, out var writer);
                    writer.WriteUInt(receivedData + 2);
                    driver.EndSend(writer);
                }
                else if (cmd == Unity.Networking.Transport.NetworkEvent.Type.Disconnect)
                {
                    Debug.Log("Client disconnected");
                    connections[index] = default(NetworkConnection);
                }
            }
        }
    }
}
