using UnityEngine;
using Unity.Netcode;
using System.Collections;
using Unity.Netcode.Transports.UTP;

public class Portal : MonoBehaviour
{
    // Port for the next zone
    public int nextZonePort = 7778;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check if the collider is the player
        if (other.CompareTag("Player"))
        {
            Debug.Log("Player entered portal, transitioning to next zone.");
            // Trigger the transition to the next zone
            StartCoroutine(TransitionToNextZone());
        }
    }

private IEnumerator TransitionToNextZone()
{
    // Step 1: Disconnect from the current server
    if (NetworkManager.Singleton != null)
    {
        Debug.Log("Shutting down the network manager...");
        NetworkManager.Singleton.Shutdown();
        Debug.Log("Disconnected from the current server.");
    }

    yield return new WaitForSeconds(1f);  // Small delay to ensure disconnect

    // Step 2: Set up the connection to the new server with the new port
    SetupClientForNextZone(nextZonePort);

    // Step 3: Connect to the next zone server
    if (NetworkManager.Singleton != null)
    {
        NetworkManager.Singleton.StartClient();
        Debug.Log($"Connecting to the next zone on port {nextZonePort}.");
    }

    yield return new WaitUntil(() => NetworkManager.Singleton.IsConnectedClient);

    // Step 4: After connecting, request procedural world data from the new server
    RequestWorldDataFromServer();
}

private void RequestWorldDataFromServer()
{
    // This function sends a request to the server to get procedural world data (seed, object data, etc.)
    // The server should respond with the data, and then the client can use that to rebuild the world.
    Debug.Log("Requesting world data from the new server...");
    // Implement logic for requesting and receiving world data here
}


    private void SetupClientForNextZone(int port)
    {
        // Get UnityTransport component and configure it for the next zone
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (transport != null)
        {
            // Assuming the IP remains the same, only the port changes
            transport.SetConnectionData("127.0.0.1", (ushort)port);
            Debug.Log($"Client configured to connect to server on port {port}.");
        }
        else
        {
            Debug.LogWarning("UnityTransport component is missing on the NetworkManager!");
        }
    }
}
