using UnityEngine;

public class AssetTerrainSetup : MonoBehaviour
{
    [Header("Asset Terrain")]
    public GameObject assetTerrainPrefab; // Drag your asset's terrain prefab here
    public bool replaceExistingTerrain = true;
    
    [Header("Positioning")]
    public Vector3 terrainPosition = Vector3.zero;
    public bool centerTerrain = true;
    
    void Start()
    {
        SetupAssetTerrain();
    }
    
    void SetupAssetTerrain()
    {
        if (assetTerrainPrefab == null)
        {
            Debug.LogError("No asset terrain prefab assigned!");
            return;
        }
        
        // Remove existing terrain if requested
        if (replaceExistingTerrain)
        {
            Terrain existingTerrain = FindObjectOfType<Terrain>();
            if (existingTerrain != null)
            {
                DestroyImmediate(existingTerrain.gameObject);
                Debug.Log("Removed existing terrain");
            }
        }
        
        // Instantiate the asset terrain
        GameObject terrainInstance = Instantiate(assetTerrainPrefab, terrainPosition, Quaternion.identity);
        terrainInstance.name = "Asset Terrain";
        var collider = terrainInstance.transform.GetChild(0).GetChild(3).gameObject.AddComponent<MeshCollider>();
        collider.convex = true; // Set to true if you want it to be used for physics
        
        // Center the terrain if requested
        if (centerTerrain)
        {
            Terrain terrain = terrainInstance.GetComponent<Terrain>();
            if (terrain != null)
            {
                Vector3 terrainSize = terrain.terrainData.size;
                terrainInstance.transform.position = new Vector3(-terrainSize.x / 2, 0, -terrainSize.z / 2);
            }
        }
        
        Debug.Log($"Asset terrain setup complete: {terrainInstance.name}");
    }
    
    [ContextMenu("Setup Asset Terrain")]
    public void SetupAssetTerrainManual()
    {
        SetupAssetTerrain();
    }
}
