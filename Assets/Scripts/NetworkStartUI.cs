using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

public class NetworkStartUI : MonoBehaviour
{
    void Start()
    {
        if (Application.isBatchMode)
        {
            // Set the server to bind to all interfaces
            SetServerBindAddress();

            // Automatically start the server if running in batch mode (headless mode)
            Debug.Log("Running in batch mode - Starting server automatically.");
            NetworkManager.Singleton.StartServer();
        }
    }

    private void SetServerBindAddress()
    {
        if (NetworkManager.Singleton != null)
        {
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            if (transport != null)
            {
                // Set the bind address to 0.0.0.0 to listen on all network interfaces
                transport.SetConnectionData("0.0.0.0", 7777);
                Debug.Log("Bind address set to 0.0.0.0 for all network interfaces.");
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
                    NetworkManager.Singleton.StartHost();
                }
                if (GUILayout.Button("Start Client"))
                {
                    SetClientConnectionData(); // Set the client's connection data to connect to the VPS IP
                    NetworkManager.Singleton.StartClient();
                }
                if (GUILayout.Button("Start Server"))
                {
                    SetServerBindAddress();
                    NetworkManager.Singleton.StartServer();
                }
            }

            GUILayout.EndArea();
        }
    }

    private void SetClientConnectionData()
    {
        if (NetworkManager.Singleton != null)
        {
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            if (transport != null)
            {
                // Set the client to connect to the VPS IP address
                transport.SetConnectionData("74.208.246.177", 7777);
                Debug.Log("Client connection data set to 74.208.246.177:7777.");
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
}
