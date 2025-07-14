using UnityEngine;

public class TerrainManager : MonoBehaviour
{
    [Header("Terrain Settings")]
    public Terrain terrain;
    public bool autoFindTerrain = true;
    
    [Header("Terrain Textures from Asset")]
    public Texture2D[] terrainTextures; // Drag your asset's terrain textures here
    public Vector2 textureScale = new Vector2(1, 1); // How many times the texture repeats
    
    [Header("Height Settings")]
    public float heightmapScale = 1f; // How tall the terrain can be
    public bool generateHeightmap = true;
    public float noiseScale = 0.1f;
    
    [Header("Terrain Size Settings")]
    public bool preserveOriginalSize = true; // Keep the original terrain size
    public Vector3 customTerrainSize = new Vector3(1000, 600, 1000); // Custom size if not preserving
    public bool resetToOriginalSize = false; // Reset to Unity's default size
    
    [Header("Collision Settings")]
    public bool enableTerrainCollider = true;
    public PhysicsMaterial terrainPhysicMaterial; // Optional: drag a physic material here
    
    // Store original size
    private Vector3 originalTerrainSize;
    
    void Start()
    {
        SetupTerrain();
    }
    
    void SetupTerrain()
    {
        // Find terrain if not assigned
        if (terrain == null && autoFindTerrain)
        {
            terrain = GetComponent<Terrain>();
            if (terrain == null)
            {
                terrain = FindFirstObjectByType<Terrain>();
            }
        }
        
        if (terrain == null)
        {
            Debug.LogError("No Terrain found! Please create a Terrain object or assign one to the script.");
            return;
        }
        
        // Setup terrain data
        TerrainData terrainData = terrain.terrainData;
        
        // Store original size if not already stored
        if (originalTerrainSize == Vector3.zero)
        {
            originalTerrainSize = terrainData.size;
            Debug.Log($"Original terrain size stored: {originalTerrainSize}");
        }
        
        // Handle terrain size settings
        if (resetToOriginalSize)
        {
            ResetTerrainToOriginalSize();
            resetToOriginalSize = false; // Reset the flag
        }
        else if (preserveOriginalSize)
        {
            RestoreOriginalTerrainSize();
        }
        else
        {
            SetCustomTerrainSize();
        }
        
        // Apply textures from your asset
        if (terrainTextures != null && terrainTextures.Length > 0)
        {
            ApplyTerrainTextures(terrainData);
        }
        
        // Generate heightmap if enabled
        if (generateHeightmap)
        {
            GenerateHeightmap(terrainData);
        }
        
        // Setup terrain collision
        SetupTerrainCollision();
        
        // Store original size
        originalTerrainSize = terrainData.size;
        
        // Apply size settings
        ApplyTerrainSize(terrainData);
        
        // Setup collision if enabled
        if (enableTerrainCollider)
        {
            SetupTerrainCollision();
        }
        
        Debug.Log($"Terrain setup complete! Size: {terrainData.size}");
    }
    
    void ApplyTerrainTextures(TerrainData terrainData)
    {
        // Create terrain layers from your asset textures
        TerrainLayer[] terrainLayers = new TerrainLayer[terrainTextures.Length];
        
        for (int i = 0; i < terrainTextures.Length; i++)
        {
            if (terrainTextures[i] != null)
            {
                terrainLayers[i] = new TerrainLayer();
                terrainLayers[i].diffuseTexture = terrainTextures[i];
                terrainLayers[i].tileSize = textureScale;
                terrainLayers[i].tileOffset = Vector2.zero;
                
                Debug.Log($"Applied texture: {terrainTextures[i].name}");
            }
        }
        
        terrainData.terrainLayers = terrainLayers;
        
        // Paint the terrain with the first texture as base
        if (terrainLayers.Length > 0)
        {
            PaintBaseTerrain(terrainData);
        }
    }
    
    void PaintBaseTerrain(TerrainData terrainData)
    {
        int alphamapWidth = terrainData.alphamapWidth;
        int alphamapHeight = terrainData.alphamapHeight;
        int numTextures = terrainData.terrainLayers.Length;
        
        // Create alphamap array
        float[,,] alphamap = new float[alphamapWidth, alphamapHeight, numTextures];
        
        // Paint the terrain
        for (int x = 0; x < alphamapWidth; x++)
        {
            for (int y = 0; y < alphamapHeight; y++)
            {
                // Calculate normalized position
                float normalizedX = (float)x / alphamapWidth;
                float normalizedY = (float)y / alphamapHeight;
                
                // Simple texture blending based on height or noise
                float height = terrainData.GetHeight(
                    Mathf.RoundToInt(normalizedX * terrainData.heightmapResolution),
                    Mathf.RoundToInt(normalizedY * terrainData.heightmapResolution)
                );
                
                float normalizedHeight = height / terrainData.size.y;
                
                // Blend textures based on height
                for (int t = 0; t < numTextures; t++)
                {
                    if (t == 0) // Base texture (grass/low areas)
                    {
                        alphamap[x, y, t] = Mathf.Clamp01(1f - normalizedHeight * 2f);
                    }
                    else if (t == 1 && numTextures > 1) // Higher areas
                    {
                        alphamap[x, y, t] = Mathf.Clamp01(normalizedHeight);
                    }
                    else
                    {
                        alphamap[x, y, t] = 0f;
                    }
                }
            }
        }
        
        terrainData.SetAlphamaps(0, 0, alphamap);
    }
    
    void GenerateHeightmap(TerrainData terrainData)
    {
        int heightmapWidth = terrainData.heightmapResolution;
        int heightmapHeight = terrainData.heightmapResolution;
        
        float[,] heightmap = new float[heightmapWidth, heightmapHeight];
        
        for (int x = 0; x < heightmapWidth; x++)
        {
            for (int y = 0; y < heightmapHeight; y++)
            {
                float height = Mathf.PerlinNoise(x * noiseScale, y * noiseScale);
                heightmap[x, y] = height;
            }
        }
        
        terrainData.SetHeights(0, 0, heightmap);
    }
    
    void ApplyTerrainSize(TerrainData terrainData)
    {
        if (preserveOriginalSize)
        {
            // Preserve the original size
            terrainData.size = originalTerrainSize;
        }
        else
        {
            // Apply custom size
            terrainData.size = customTerrainSize;
        }
        
        // Reset to default size if enabled
        if (resetToOriginalSize)
        {
            terrainData.size = new Vector3(1000, 600, 1000);
        }
    }
    
    void RestoreOriginalTerrainSize()
    {
        if (terrain == null) return;
        
        TerrainData terrainData = terrain.terrainData;
        
        if (originalTerrainSize != Vector3.zero)
        {
            terrainData.size = originalTerrainSize;
            Debug.Log($"Restored original terrain size: {originalTerrainSize}");
        }
        else
        {
            Debug.LogWarning("No original terrain size stored. Using current size.");
        }
    }
    
    void SetCustomTerrainSize()
    {
        if (terrain == null) return;
        
        TerrainData terrainData = terrain.terrainData;
        terrainData.size = customTerrainSize;
        Debug.Log($"Set custom terrain size: {customTerrainSize}");
    }
    
    void ResetTerrainToOriginalSize()
    {
        if (terrain == null) return;
        
        TerrainData terrainData = terrain.terrainData;
        
        // Unity's default terrain size
        Vector3 unityDefaultSize = new Vector3(1000, 600, 1000);
        terrainData.size = unityDefaultSize;
        originalTerrainSize = unityDefaultSize;
        
        Debug.Log($"Reset terrain to Unity default size: {unityDefaultSize}");
    }
    
    void SetupTerrainCollision()
    {
        if (terrain == null) return;
        
        TerrainCollider terrainCollider = terrain.GetComponent<TerrainCollider>();
        
        if (enableTerrainCollider)
        {
            // Add TerrainCollider if it doesn't exist
            if (terrainCollider == null)
            {
                terrainCollider = terrain.gameObject.AddComponent<TerrainCollider>();
                Debug.Log("Added TerrainCollider to terrain");
            }
            
            // Set the terrain data
            terrainCollider.terrainData = terrain.terrainData;
            
            // Apply physics material if assigned
            if (terrainPhysicMaterial != null)
            {
                terrainCollider.material = terrainPhysicMaterial;
                Debug.Log("Applied physics material to terrain");
            }
            
            // Enable the collider
            terrainCollider.enabled = true;
            Debug.Log("Terrain collision enabled");
        }
        else
        {
            // Disable collision if not wanted
            if (terrainCollider != null)
            {
                terrainCollider.enabled = false;
                Debug.Log("Terrain collision disabled");
            }
        }
    }
    
    [ContextMenu("Refresh Terrain")]
    public void RefreshTerrain()
    {
        SetupTerrain();
    }
    
    [ContextMenu("Clear Terrain Heights")]
    public void ClearTerrainHeights()
    {
        if (terrain != null)
        {
            TerrainData terrainData = terrain.terrainData;
            int resolution = terrainData.heightmapResolution;
            float[,] heights = new float[resolution, resolution];
            terrainData.SetHeights(0, 0, heights);
            Debug.Log("Terrain heights cleared");
        }
    }
    
    [ContextMenu("Restore Original Terrain Size")]
    public void RestoreOriginalSize()
    {
        RestoreOriginalTerrainSize();
    }
    
    [ContextMenu("Set Custom Terrain Size")]
    public void SetCustomSize()
    {
        SetCustomTerrainSize();
    }
    
    [ContextMenu("Reset to Unity Default Size")]
    public void ResetToUnityDefault()
    {
        ResetTerrainToOriginalSize();
    }
    
    [ContextMenu("Print Current Terrain Size")]
    public void PrintTerrainSize()
    {
        if (terrain != null)
        {
            Vector3 currentSize = terrain.terrainData.size;
            Debug.Log($"Current terrain size: {currentSize}");
            Debug.Log($"Original terrain size: {originalTerrainSize}");
        }
    }
}
