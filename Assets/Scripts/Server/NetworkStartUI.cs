using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport;
using Unity.Networking.Transport.TLS; // Add this line

public class NetworkStartUI : MonoBehaviour
{
    private float deltaTime = 0.0f;
    private UnityTransport transport;

    void Start()
    {
        if (Application.isBatchMode)
        {
            // Set up secure server parameters and start the server
            SetupSecureServer();

            // Automatically start the server if running in batch mode (headless mode)
            Debug.Log("Running in batch mode - Starting server automatically.");
            NetworkManager.Singleton.StartServer();
        }

        // Cache the UnityTransport component for latency calculations
        if (NetworkManager.Singleton != null)
        {
            transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        }
    }

    private void Update()
    {
        // Update deltaTime for FPS calculation
        deltaTime += (Time.deltaTime - deltaTime) * 0.1f;
    }

    private void SetupSecureServer()
    {
        // Create a NetworkSettings object and configure secure server parameters
        var settings = new NetworkSettings();
        settings.WithSecureServerParameters(
            certificate: SecureParameters.MyGameServerCertificate,
            privateKey: SecureParameters.MyGameServerPrivateKey);

        // Get UnityTransport and configure it
        if (NetworkManager.Singleton != null)
        {
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            if (transport != null)
            {
                // Set the bind address to 0.0.0.0 to listen on all network interfaces
                transport.SetConnectionData("0.0.0.0", 7777);

                // Set secure server parameters for encrypted communication
                transport.SetServerSecrets(
                    SecureParameters.MyGameServerCertificate,
                    SecureParameters.MyGameServerPrivateKey);

                Debug.Log("Secure server configured and bind address set to 0.0.0.0.");
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

    private void SetupSecureClient()
    {
        // Create a NetworkSettings object and configure secure client parameters
        var settings = new NetworkSettings();
        settings.WithSecureClientParameters(
            serverName: SecureParameters.ServerCommonName,
            caCertificate: SecureParameters.MyGameClientCA);

        // Get UnityTransport and configure it
        if (NetworkManager.Singleton != null)
        {
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            if (transport != null)
            {
                // Set the client to connect to the server IP address
                transport.SetConnectionData("0.0.0.0", 7777);

                // Set client secure parameters for encrypted communication
                transport.SetClientSecrets(SecureParameters.ServerCommonName, SecureParameters.MyGameClientCA);

                Debug.Log("Client connection data set to 0.0.0.0:7777 with encryption.");
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

    void OnGUI()
    {
        // Display buttons for starting as Host or Client if not running in headless mode
        if (!Application.isBatchMode)
        {
            GUILayout.BeginArea(new Rect(10, 10, 300, 300));

            if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
            {
                if (GUILayout.Button("Start Host"))
                {
                    SetupSecureServer();
                    NetworkManager.Singleton.StartHost();
                }
                if (GUILayout.Button("Start Client"))
                {
                    SetupSecureClient(); // Set the client's secure connection data
                    NetworkManager.Singleton.StartClient();
                }
                if (GUILayout.Button("Start Server"))
                {
                    SetupSecureServer();
                    NetworkManager.Singleton.StartServer();
                }
            }

            GUILayout.EndArea();
        }

        // Display FPS
        int fps = Mathf.CeilToInt(1.0f / deltaTime);
        GUI.Label(new Rect(10, 320, 150, 30), $"FPS: {fps}");

        // Display network latency (ping)
        if (transport != null && NetworkManager.Singleton.IsClient)
        {
            var rtt = transport.GetCurrentRtt(NetworkManager.Singleton.LocalClientId);
            GUI.Label(new Rect(10, 350, 200, 30), $"Ping: {rtt} ms");
        }
    }
}
