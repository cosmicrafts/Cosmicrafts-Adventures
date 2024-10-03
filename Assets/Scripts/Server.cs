using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport;

public class Server : MonoBehaviour
{
    private NetworkDriver m_Driver;

    void Start()
    {
        // Set up secure server parameters and UnityTransport before anything else
        SetupSecureServer();

        // Set up NetworkDriver
        m_Driver = NetworkDriver.Create();

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
        if (Application.isBatchMode && NetworkManager.Singleton != null)
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

    void OnDestroy()
    {
        // Dispose of network driver safely
        if (m_Driver.IsCreated)
        {
            m_Driver.Dispose();
        }
    }
}
