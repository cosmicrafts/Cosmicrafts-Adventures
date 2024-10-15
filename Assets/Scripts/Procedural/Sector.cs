// Sector Script
using UnityEngine;

[CreateAssetMenu(fileName = "Sector", menuName = "ProceduralWorld/Sector", order = 1)]
public class Sector : ScriptableObject
{
    public string sectorName;
    public bool isGenerated = false;

}
//