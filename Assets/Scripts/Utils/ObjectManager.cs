using UnityEngine;

public class ObjectManager : MonoBehaviour
{
    public static ObjectManager Instance;

    public ObjectSO[] allConfigurations; // This stores all ObjectSOs for both world objects and players

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
}
