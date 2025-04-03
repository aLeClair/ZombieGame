using UnityEngine;
using System.Collections.Generic;

public class LevelGenerator : MonoBehaviour
{
    // Singleton instance
    private static LevelGenerator _instance;
    public static LevelGenerator Instance { get { return _instance; } }
    
    void Awake()
    {
        // Singleton setup
        if (_instance == null)
        {
            _instance = this;
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    [System.Serializable]
    public class TerrainElement
    {
        public GameObject prefab;
        public float chance = 1.0f;
        public bool isObstacle = true;
    }
    
    [System.Serializable]
    public class LevelSettings
    {
        public int width = 40;
        public int height = 40;
        public int cellSize = 5;
        public float noiseScale = 0.1f;
        public float obstacleThreshold = 0.6f;
        public float pathWidth = 3f;
        public int seed = 0;
        public bool useRandomSeed = true;
        [Range(0,1)] public float mazeComplexity = 0.7f;
    }
    
    [Header("Level Generation Settings")]
    [SerializeField] private LevelSettings settings;
    
    [Header("Terrain Elements")]
    [SerializeField] private List<TerrainElement> obstacleElements;
    [SerializeField] private List<TerrainElement> decorationElements;
    [SerializeField] private GameObject groundPrefab;
    [SerializeField] private GameObject wallPrefab;
    
    [Header("Level Features")]
    [SerializeField] private GameObject waterPrefab;
    [SerializeField] private GameObject tallGrassPrefab;
    [SerializeField] private float waterChance = 0.05f;
    [SerializeField] private float tallGrassChance = 0.1f;
    
    [Header("Spawn Points")]
    [SerializeField] private int numSpawnPoints = 8;
    [SerializeField] private float spawnDistance = 25f;
    
    private int[,] mazeGrid;
    private GameObject levelContainer;
    private List<Vector3> spawnPositions = new List<Vector3>();
    private Vector3 centerPosition;
    
    public void GenerateLevel()
    {
        // Initialize seed for randomization
        if (settings.useRandomSeed)
        {
            settings.seed = System.DateTime.Now.Millisecond;
        }
        Random.InitState(settings.seed);
        
        // Create container for level objects
        if (levelContainer != null)
        {
            DestroyImmediate(levelContainer);
        }
        levelContainer = new GameObject("LevelContainer");
        
        // Set center position where tower will be placed
        centerPosition = Vector3.zero;
        
        // Generate maze grid
        GenerateMazeGrid();
        
        // Create ground
        CreateGround();
        
        // Create maze obstacles
        CreateMazeObstacles();
        
        // Add terrain features (water, tall grass)
        AddTerrainFeatures();
        
        // Create boundaries
        CreateBoundaries();
        
        // Create spawn points
        CreateSpawnPoints();
        
        Debug.Log("Level generation complete!");
    }
    
    private void GenerateMazeGrid()
    {
        mazeGrid = new int[settings.width, settings.height];
        
        // Initialize grid
        for (int x = 0; x < settings.width; x++)
        {
            for (int y = 0; y < settings.height; y++)
            {
                mazeGrid[x, y] = 1; // 1 = wall, 0 = path
            }
        }
        
        // Generate maze using recursive backtracking
        GenerateMaze(settings.width / 2, settings.height / 2);
        
        // Create clear area around center (for tower placement)
        int clearRadius = 3;
        int centerX = settings.width / 2;
        int centerY = settings.height / 2;
        
        for (int x = centerX - clearRadius; x <= centerX + clearRadius; x++)
        {
            for (int y = centerY - clearRadius; y <= centerY + clearRadius; y++)
            {
                if (x >= 0 && x < settings.width && y >= 0 && y < settings.height)
                {
                    mazeGrid[x, y] = 0; // Clear path
                }
            }
        }
    }
    
    private void GenerateMaze(int startX, int startY)
    {
        // Directions: 0 = north, 1 = east, 2 = south, 3 = west
        int[] dx = { 0, 1, 0, -1 };
        int[] dy = { -1, 0, 1, 0 };
        
        // Setup for random maze generation
        List<Vector2Int> stack = new List<Vector2Int>();
        mazeGrid[startX, startY] = 0; // Start point is a path
        stack.Add(new Vector2Int(startX, startY));
        
        while (stack.Count > 0)
        {
            // Get current cell
            Vector2Int current = stack[stack.Count - 1];
            
            // Check for unvisited neighbors
            List<int> unvisitedNeighbors = new List<int>();
            
            for (int i = 0; i < 4; i++)
            {
                int nx = current.x + dx[i] * 2;
                int ny = current.y + dy[i] * 2;
                
                if (nx >= 0 && nx < settings.width && ny >= 0 && ny < settings.height && mazeGrid[nx, ny] == 1)
                {
                    unvisitedNeighbors.Add(i);
                }
            }
            
            if (unvisitedNeighbors.Count > 0)
            {
                // Choose random direction
                int direction = unvisitedNeighbors[Random.Range(0, unvisitedNeighbors.Count)];
                
                // Get new cell
                int nx = current.x + dx[direction] * 2;
                int ny = current.y + dy[direction] * 2;
                
                // Carve path
                mazeGrid[current.x + dx[direction], current.y + dy[direction]] = 0;
                mazeGrid[nx, ny] = 0;
                
                // Add new cell to stack
                stack.Add(new Vector2Int(nx, ny));
            }
            else
            {
                // Backtrack
                stack.RemoveAt(stack.Count - 1);
            }
        }
        
        // Add additional paths to increase connectivity based on complexity setting
        int numExtraPaths = Mathf.RoundToInt((settings.width * settings.height) * settings.mazeComplexity * 0.05f);
        
        for (int i = 0; i < numExtraPaths; i++)
        {
            int x = Random.Range(1, settings.width - 1);
            int y = Random.Range(1, settings.height - 1);
            
            if (mazeGrid[x, y] == 1)
            {
                // Check if we'd be connecting two paths
                int pathNeighbors = 0;
                for (int d = 0; d < 4; d++)
                {
                    int nx = x + dx[d];
                    int ny = y + dy[d];
                    if (nx >= 0 && nx < settings.width && ny >= 0 && ny < settings.height && mazeGrid[nx, ny] == 0)
                    {
                        pathNeighbors++;
                    }
                }
                
                // Only create a new path if it connects existing paths
                if (pathNeighbors >= 2)
                {
                    mazeGrid[x, y] = 0;
                }
            }
        }
    }
    
    private void CreateGround()
    {
        // Create a single ground plane
        float width = settings.width * settings.cellSize;
        float height = settings.height * settings.cellSize;
        
        GameObject ground = Instantiate(groundPrefab, new Vector3(0, 0, 0), Quaternion.identity, levelContainer.transform);
        ground.name = "Ground";
        
        // Scale ground to match grid size
        ground.transform.localScale = new Vector3(width / 10, 1, height / 10);
    }
    
    private TerrainElement GetRandomElement(List<TerrainElement> elements)
    {
        if (elements == null || elements.Count == 0)
            return null;
            
        // Sum up total chances
        float totalChance = 0;
        foreach (TerrainElement element in elements)
        {
            totalChance += element.chance;
        }
        
        // Pick random value
        float random = Random.Range(0, totalChance);
        float currentChance = 0;
        
        // Find selected element
        foreach (TerrainElement element in elements)
        {
            currentChance += element.chance;
            if (random <= currentChance)
            {
                return element;
            }
        }
        
        // Default to first element if something went wrong
        return elements[0];
    }
    
    private void CreateMazeObstacles()
    {
        for (int x = 0; x < settings.width; x++)
        {
            for (int y = 0; y < settings.height; y++)
            {
                if (mazeGrid[x, y] == 1) // Wall
                {
                    float worldX = (x - settings.width / 2) * settings.cellSize;
                    float worldZ = (y - settings.height / 2) * settings.cellSize;
                    Vector3 position = new Vector3(worldX, 0, worldZ);
                    
                    // Use Perlin noise to determine wall type variety
                    float noiseValue = Mathf.PerlinNoise(x * settings.noiseScale, y * settings.noiseScale);
                    
                    if (noiseValue > settings.obstacleThreshold)
                    {
                        // Place a random obstacle
                        TerrainElement obstacle = GetRandomElement(obstacleElements);
                        if (obstacle != null && obstacle.prefab != null)
                        {
                            // Randomize rotation slightly
                            Quaternion rotation = Quaternion.Euler(0, Random.Range(0, 4) * 90, 0);
                            
                            // Randomize scale slightly
                            float scale = Random.Range(0.8f, 1.2f);
                            
                            GameObject obstacleObj = Instantiate(obstacle.prefab, position, rotation, levelContainer.transform);
                            obstacleObj.transform.localScale *= scale;
                            obstacleObj.name = "Obstacle_" + x + "_" + y;
                        }
                    }
                    else
                    {
                        // Place standard wall
                        Quaternion rotation = Quaternion.Euler(0, Random.Range(0, 4) * 90, 0);
                        GameObject wallObj = Instantiate(wallPrefab, position, rotation, levelContainer.transform);
                        wallObj.name = "Wall_" + x + "_" + y;
                    }
                }
                else // Path
                {
                    // Potentially place decorations on paths
                    float decorChance = 0.05f; // 5% chance for decoration
                    if (Random.value < decorChance)
                    {
                        float worldX = (x - settings.width / 2) * settings.cellSize;
                        float worldZ = (y - settings.height / 2) * settings.cellSize;
                        Vector3 position = new Vector3(worldX, 0, worldZ);
                        
                        // Don't place decorations in the center area (tower area)
                        float distanceFromCenter = Vector3.Distance(position, centerPosition);
                        if (distanceFromCenter > 5f)
                        {
                            TerrainElement decoration = GetRandomElement(decorationElements);
                            if (decoration != null && decoration.prefab != null)
                            {
                                Quaternion rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
                                GameObject decorObj = Instantiate(decoration.prefab, position, rotation, levelContainer.transform);
                                decorObj.name = "Decoration_" + x + "_" + y;
                            }
                        }
                    }
                }
            }
        }
    }
    
    private void AddTerrainFeatures()
    {
        // Create clusters of water and tall grass
        int numWaterClusters = Mathf.RoundToInt(settings.width * settings.height * waterChance / 5);
        int numGrassClusters = Mathf.RoundToInt(settings.width * settings.height * tallGrassChance / 5);
        
        // Add water clusters
        for (int i = 0; i < numWaterClusters; i++)
        {
            // Find a valid starting location (must be path)
            int startX, startY;
            int attempts = 0;
            
            do {
                startX = Random.Range(0, settings.width);
                startY = Random.Range(0, settings.height);
                attempts++;
                
                // Avoid placing water close to center
                float worldX = (startX - settings.width / 2) * settings.cellSize;
                float worldZ = (startY - settings.height / 2) * settings.cellSize;
                float distanceFromCenter = Vector2.Distance(new Vector2(worldX, worldZ), new Vector2(0, 0));
                
                if (distanceFromCenter < 15f) // Too close to center
                    continue;
                    
            } while ((mazeGrid[startX, startY] != 0 || IsNearSpawnPoint(startX, startY)) && attempts < 50);
            
            if (attempts < 50 && waterPrefab != null)
            {
                // Create water cluster
                int clusterSize = Random.Range(3, 8);
                CreateCluster(startX, startY, clusterSize, waterPrefab, "Water");
            }
        }
        
        // Add tall grass clusters
        for (int i = 0; i < numGrassClusters; i++)
        {
            // Find a valid starting location (must be path)
            int startX, startY;
            int attempts = 0;
            
            do {
                startX = Random.Range(0, settings.width);
                startY = Random.Range(0, settings.height);
                attempts++;
            } while ((mazeGrid[startX, startY] != 0 || IsNearSpawnPoint(startX, startY)) && attempts < 50);
            
            if (attempts < 50 && tallGrassPrefab != null)
            {
                // Create tall grass cluster
                int clusterSize = Random.Range(4, 10);
                CreateCluster(startX, startY, clusterSize, tallGrassPrefab, "TallGrass");
            }
        }
    }
    
    private void CreateCluster(int startX, int startY, int size, GameObject prefab, string name)
    {
        // Create a cluster of terrain features
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        List<Vector2Int> placed = new List<Vector2Int>();
        
        // Add start position
        queue.Enqueue(new Vector2Int(startX, startY));
        placed.Add(new Vector2Int(startX, startY));
        
        while (queue.Count > 0 && placed.Count < size)
        {
            Vector2Int current = queue.Dequeue();
            
            // Place feature at this location
            float worldX = (current.x - settings.width / 2) * settings.cellSize;
            float worldZ = (current.y - settings.height / 2) * settings.cellSize;
            Vector3 position = new Vector3(worldX, 0, worldZ);
            
            GameObject featureObj = Instantiate(prefab, position, Quaternion.identity, levelContainer.transform);
            featureObj.name = name + "_" + current.x + "_" + current.y;
            
            // Try to add neighboring cells
            int[] dx = { 0, 1, 0, -1 };
            int[] dy = { -1, 0, 1, 0 };
            
            for (int i = 0; i < 4; i++)
            {
                int nx = current.x + dx[i];
                int ny = current.y + dy[i];
                Vector2Int neighbor = new Vector2Int(nx, ny);
                
                // Check if valid cell and is a path
                if (nx >= 0 && nx < settings.width && ny >= 0 && ny < settings.height && 
                    mazeGrid[nx, ny] == 0 && !placed.Contains(neighbor) && Random.value < 0.7f)
                {
                    queue.Enqueue(neighbor);
                    placed.Add(neighbor);
                }
            }
        }
    }
    
    private void CreateBoundaries()
    {
        // Create a boundary wall around the level
        float halfWidth = settings.width * settings.cellSize * 0.5f;
        float halfHeight = settings.height * settings.cellSize * 0.5f;
        float wallHeight = 5f;
        
        // Create four walls
        GameObject northWall = CreateWallSection(new Vector3(0, wallHeight/2, -halfHeight - 2), new Vector3(halfWidth * 2, wallHeight, 4), "NorthWall");
        GameObject southWall = CreateWallSection(new Vector3(0, wallHeight/2, halfHeight + 2), new Vector3(halfWidth * 2, wallHeight, 4), "SouthWall");
        GameObject eastWall = CreateWallSection(new Vector3(halfWidth + 2, wallHeight/2, 0), new Vector3(4, wallHeight, halfHeight * 2), "EastWall");
        GameObject westWall = CreateWallSection(new Vector3(-halfWidth - 2, wallHeight/2, 0), new Vector3(4, wallHeight, halfHeight * 2), "WestWall");
    }
    
    private GameObject CreateWallSection(Vector3 position, Vector3 scale, string name)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.transform.position = position;
        wall.transform.localScale = scale;
        wall.transform.SetParent(levelContainer.transform);
        wall.name = name;
        
        // Set material if needed
        // wall.GetComponent<Renderer>().material = wallMaterial;
        
        return wall;
    }
    
    private void CreateSpawnPoints()
    {
        spawnPositions.Clear();
        
        // Create spawn points in a circle around the perimeter
        for (int i = 0; i < numSpawnPoints; i++)
        {
            float angle = i * (360f / numSpawnPoints) * Mathf.Deg2Rad;
            float x = Mathf.Sin(angle) * spawnDistance;
            float z = Mathf.Cos(angle) * spawnDistance;
            
            Vector3 spawnPos = new Vector3(x, 0, z);
            spawnPositions.Add(spawnPos);
            
            // Create spawn point marker
            GameObject spawnMarker = new GameObject("SpawnPoint_" + i);
            spawnMarker.transform.position = spawnPos;
            spawnMarker.transform.parent = levelContainer.transform;
        }
    }
    
    private bool IsNearSpawnPoint(int gridX, int gridY)
    {
        // Convert grid position to world position
        float worldX = (gridX - settings.width / 2) * settings.cellSize;
        float worldZ = (gridY - settings.height / 2) * settings.cellSize;
        Vector3 worldPos = new Vector3(worldX, 0, worldZ);
        
        // Check distance to all spawn points
        foreach (Vector3 spawnPos in spawnPositions)
        {
            if (Vector3.Distance(worldPos, spawnPos) < 5f)
            {
                return true;
            }
        }
        
        return false;
    }
    
    public List<Vector3> GetSpawnPoints()
    {
        return spawnPositions;
    }
}