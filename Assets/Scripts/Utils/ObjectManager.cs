using UnityEngine;

public class ObjectManager : MonoBehaviour
{
    public static ObjectManager Instance;

    public ObjectSO[] allConfigurations; // Store all ObjectSOs for both world objects and players

    private void Awake()
    {
        Instance = this;
    }

    public ObjectSO GetObjectSOByIndex(int index)
    {
        if (index >= 0 && index < allConfigurations.Length)
        {
            return allConfigurations[index];
        }
        return null;
    }

    // New method to get index of a given ObjectSO
    public int GetObjectSOIndex(ObjectSO configuration)
    {
        for (int i = 0; i < allConfigurations.Length; i++)
        {
            if (allConfigurations[i] == configuration)
            {
                return i;
            }
        }
        return -1;  // Return -1 if not found
    }
}
