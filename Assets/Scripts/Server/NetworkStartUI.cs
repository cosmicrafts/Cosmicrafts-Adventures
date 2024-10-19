using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport;
using Unity.Networking.Transport.TLS; // Add this line
using TMPro; // Add this line for TextMeshPro support

public class NetworkStartUI : MonoBehaviour
{
    public TMP_Text fpsText;
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
        SetupSecureServer();
        NetworkManager.Singleton.StartHost();
    }

    public void StartClient()
    {
        SetupSecureClient();
        NetworkManager.Singleton.StartClient();
    }

    public void StartServer()
    {
        SetupSecureServer();
        NetworkManager.Singleton.StartServer();
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
                // Set client secure parameters for encrypted communication
                transport.SetClientSecrets(SecureParameters.ServerCommonName, SecureParameters.MyGameClientCA);
            }
        }
    }
}
