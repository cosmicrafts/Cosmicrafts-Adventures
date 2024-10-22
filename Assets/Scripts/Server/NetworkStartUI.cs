using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport;
using Unity.Networking.Transport.TLS; // For TLS
using TMPro; // For TextMeshPro support

public class NetworkStartUI : MonoBehaviour
{
    public TMP_Text fpsText;
    private float deltaTime = 0.0f;
    private UnityTransport transport;

    void Start()
    {
        if (Application.isBatchMode)
        {
            // Get port from command line arguments or environment variable
            int port = GetPortFromArgsOrEnv();

            // Set up secure server parameters and start the server
            SetupSecureServer(port);

            // Automatically start the server if running in batch mode (headless mode)
            Debug.Log($"Running in batch mode - Starting secure server automatically on port {port}.");
            NetworkManager.Singleton.StartServer();
        }
    }

    private void Update()
    {
        // Update deltaTime for FPS calculation
        deltaTime += (Time.deltaTime - deltaTime) * 0.1f;

        // Display FPS
        int fps = Mathf.CeilToInt(1.0f / deltaTime);
        fpsText.text = $"{fps}";
    }

    public void StartHost()
    {
        SetupSecureServer(7777);  // Default port for host
        NetworkManager.Singleton.StartHost();
    }

    public void StartClient()
    {
        SetupSecureClient();
        NetworkManager.Singleton.StartClient();
    }

    public void StartServer()
    {
        SetupSecureServer(7777);  // Default port for server
        NetworkManager.Singleton.StartServer();
    }

    private void SetupSecureServer(int port)
    {
        // Get UnityTransport and configure it
        if (NetworkManager.Singleton != null)
        {
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            if (transport != null)
            {
                // Set the bind address to 0.0.0.0 to listen on all network interfaces
                transport.SetConnectionData("0.0.0.0", (ushort)port);

                // Set secure server parameters for encrypted communication
                transport.SetServerSecrets(
                    SecureParameters.MyGameServerCertificate,  // Your SSL certificate
                    SecureParameters.MyGameServerPrivateKey);  // Your private key

                Debug.Log($"Secure server configured and bind address set to 0.0.0.0 on port {port}.");
            }
        }
    }

    private void SetupSecureClient()
    {
        // Create a NetworkSettings object and configure secure client parameters
        var settings = new NetworkSettings();
        settings.WithSecureClientParameters(
            serverName: SecureParameters.ServerCommonName, // Your server common name
            caCertificate: SecureParameters.MyGameClientCA); // Your CA certificate

        // Get UnityTransport and configure it
        if (NetworkManager.Singleton != null)
        {
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            if (transport != null)
            {
                // Set client secure parameters for encrypted communication
                transport.SetClientSecrets(SecureParameters.ServerCommonName, SecureParameters.MyGameClientCA);
            }
        }
    }

    // Function to get port from command line arguments or environment variable
    private int GetPortFromArgsOrEnv()
    {
        // Default port
        int port = 7777;

        // Check command line arguments
        string[] args = System.Environment.GetCommandLineArgs();
        foreach (string arg in args)
        {
            if (arg.StartsWith("-port="))
            {
                if (int.TryParse(arg.Substring(6), out int parsedPort))
                {
                    port = parsedPort;
                }
            }
        }

        // Check environment variable (overrides command line if set)
        string portEnv = System.Environment.GetEnvironmentVariable("GAME_SERVER_PORT");
        if (!string.IsNullOrEmpty(portEnv) && int.TryParse(portEnv, out int envPort))
        {
            port = envPort;
        }

        return port;
    }
}
