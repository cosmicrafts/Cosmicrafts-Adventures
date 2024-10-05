using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "Sector", menuName = "ProceduralWorld/Sector", order = 1)]
public class Sector : ScriptableObject
{
    public string sectorName;
    public bool isGenerated = false;
    public List<TileData> tiles = new List<TileData>(); // Store the tiles' data here.

    [System.Serializable]
    public struct TileData
    {
        public Vector2 position;
        public Quaternion rotation;
        public Sprite sprite;
    }
}
