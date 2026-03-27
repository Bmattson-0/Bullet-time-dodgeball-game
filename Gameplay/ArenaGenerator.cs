// v0.1
    // Initial prototype version
// v0.2
    // - Added navmesh using statements
    // - Added Navigation fields
    // - Adjusted GenerateArena() to include BuildNavMesh() function after geometry is created
    // - Added BuildNavMesh() method
    // - Replaced TeleportActor() method with updated version that makes the AI spawn cleanly onto the baked NavMesh after generation
using System.Collections.Generic;
using BulletTimeDodgeball.Gameplay;
using Unity.AI.Navigation;
using UnityEngine.AI;
using UnityEngine;

namespace BulletTimeDodgeball.Arena
{
    [DefaultExecutionOrder(-100)]
    public class ArenaGenerator : MonoBehaviour
    {
        [System.Serializable]
        public class WeightedPrefab
        {
            public GameObject prefab;
            [Min(1)] public int weight = 1;
        }

        [Header("Arena Size")]
        [SerializeField] private int gridWidth = 6;
        [SerializeField] private int gridDepth = 6;
        [SerializeField] private float cellSize = 4f;
        [SerializeField] private float floorThickness = 0.5f;
        [SerializeField] private float wallHeight = 3f;
        [SerializeField] private float wallThickness = 0.5f;

        [Header("Obstacle Generation")]
        [SerializeField] [Range(0f, 1f)] private float obstacleFillPercent = 0.35f;
        [SerializeField] private WeightedPrefab[] obstaclePrefabs;

        [Header("Spawn References")]
        [SerializeField] private Transform playerTransform;
        [SerializeField] private Transform aiTransform;
        [SerializeField] private Dodgeball dodgeballPrefab;
        [SerializeField] private int ballCount = 3;

        [Header("Spawn Settings")]
        [SerializeField] private float actorGroundOffset = 0.05f;
        [SerializeField] private float actorSpawnRayHeight = 12f;
        [SerializeField] private float ballSpawnHeight = 0.9f;
        [SerializeField] private LayerMask groundMask = ~0;
        [SerializeField] private bool randomizeBorderSpawnColumns = true;

        [Header("Generation Options")]
        [SerializeField] private bool generateOnStart = true;
        [SerializeField] private bool useRandomSeed = true;
        [SerializeField] private int fixedSeed = 12345;

        [Header("Debug")]
        [SerializeField] private bool drawCellGizmos = false;

        [Header("Navigation")]
        [SerializeField] private NavMeshSurface navMeshSurface;

        private Transform generatedRoot;
        private readonly List<Vector2Int> freeCells = new();

        private Vector2Int playerSpawnCell;
        private Vector2Int aiSpawnCell;

        private void Start()
        {
            if (generateOnStart)
            {
                GenerateArena();
            }
        }

        [ContextMenu("Generate Arena")]
        public void GenerateArena()
        {
            InitializeRandom();
            EnsureGeneratedRoot();
            ClearGeneratedChildren();

            ClampSettings();
            SetupSpawnCells();

            BuildFloor();
            BuildOuterWalls();
            BuildKillboxBounds();
            BuildObstacleLayout();
            BuildNavMesh();
            PositionActors();
            SpawnBalls();
        }

        private void InitializeRandom()
        {
            if (useRandomSeed)
            {
                Random.InitState(System.Environment.TickCount);
            }
            else
            {
                Random.InitState(fixedSeed);
            }
        }

        private void EnsureGeneratedRoot()
        {
            if (generatedRoot == null)
            {
                Transform existing = transform.Find("GeneratedArena");
                if (existing != null)
                {
                    generatedRoot = existing;
                }
                else
                {
                    GameObject root = new GameObject("GeneratedArena");
                    root.transform.SetParent(transform);
                    root.transform.localPosition = Vector3.zero;
                    root.transform.localRotation = Quaternion.identity;
                    generatedRoot = root.transform;
                }
            }
        }

        private void ClearGeneratedChildren()
        {
            if (generatedRoot == null)
            {
                return;
            }

            for (int i = generatedRoot.childCount - 1; i >= 0; i--)
            {
                Transform child = generatedRoot.GetChild(i);

                if (Application.isPlaying)
                {
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }
        }

        private void ClampSettings()
        {
            gridWidth = Mathf.Max(4, gridWidth);
            gridDepth = Mathf.Max(4, gridDepth);
            cellSize = Mathf.Max(1f, cellSize);
            floorThickness = Mathf.Max(0.1f, floorThickness);
            wallHeight = Mathf.Max(1f, wallHeight);
            wallThickness = Mathf.Max(0.1f, wallThickness);
            ballCount = Mathf.Max(1, ballCount);
        }

        private void SetupSpawnCells()
        {
            int playerX;
            int aiX;

            if (randomizeBorderSpawnColumns)
            {
                playerX = Random.Range(0, gridWidth);
                aiX = Random.Range(0, gridWidth);
            }
            else
            {
                int centerX = gridWidth / 2;
                playerX = gridWidth % 2 == 0 ? centerX - 1 : centerX;
                aiX = playerX;
            }

            // Outer border spawns: south and north edges
            playerSpawnCell = new Vector2Int(playerX, 0);
            aiSpawnCell = new Vector2Int(aiX, gridDepth - 1);
        }

        private void BuildFloor()
        {
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "ArenaFloor";
            floor.transform.SetParent(generatedRoot);

            float arenaWidth = gridWidth * cellSize;
            float arenaDepth = gridDepth * cellSize;

            floor.transform.position = new Vector3(0f, -floorThickness * 0.5f, 0f);
            floor.transform.localScale = new Vector3(arenaWidth, floorThickness, arenaDepth);
        }

        private void BuildOuterWalls()
        {
            float arenaWidth = gridWidth * cellSize;
            float arenaDepth = gridDepth * cellSize;

            CreateWall(
                "Wall_North",
                new Vector3(0f, wallHeight * 0.5f, arenaDepth * 0.5f + wallThickness * 0.5f),
                new Vector3(arenaWidth + wallThickness * 2f, wallHeight, wallThickness));

            CreateWall(
                "Wall_South",
                new Vector3(0f, wallHeight * 0.5f, -arenaDepth * 0.5f - wallThickness * 0.5f),
                new Vector3(arenaWidth + wallThickness * 2f, wallHeight, wallThickness));

            CreateWall(
                "Wall_East",
                new Vector3(arenaWidth * 0.5f + wallThickness * 0.5f, wallHeight * 0.5f, 0f),
                new Vector3(wallThickness, wallHeight, arenaDepth));

            CreateWall(
                "Wall_West",
                new Vector3(-arenaWidth * 0.5f - wallThickness * 0.5f, wallHeight * 0.5f, 0f),
                new Vector3(wallThickness, wallHeight, arenaDepth));
        }

        private void BuildKillboxBounds()
        {
            float arenaWidth = gridWidth * cellSize;
            float arenaDepth = gridDepth * cellSize;

            float sideWallHeight = 12f;
            float sideWallThickness = 1f;
            float sideWallY = 0f;
            float sideOffset = 4f;

            CreateKillboxWall(
                "Killbox_North",
                new Vector3(0f, sideWallY, arenaDepth * 0.5f + sideOffset),
                new Vector3(arenaWidth + 8f, sideWallHeight, sideWallThickness));

            CreateKillboxWall(
                "Killbox_South",
                new Vector3(0f, sideWallY, -arenaDepth * 0.5f - sideOffset),
                new Vector3(arenaWidth + 8f, sideWallHeight, sideWallThickness));

            CreateKillboxWall(
                "Killbox_East",
                new Vector3(arenaWidth * 0.5f + sideOffset, sideWallY, 0f),
                new Vector3(sideWallThickness, sideWallHeight, arenaDepth + 8f));

            CreateKillboxWall(
                "Killbox_West",
                new Vector3(-arenaWidth * 0.5f - sideOffset, sideWallY, 0f),
                new Vector3(sideWallThickness, sideWallHeight, arenaDepth + 8f));

            CreateKillboxWall(
                "Killbox_Floor",
                new Vector3(0f, -10f, 0f),
                new Vector3(arenaWidth + 8f, 2f, arenaDepth + 8f));
        }

        private void CreateKillboxWall(string wallName, Vector3 position, Vector3 scale)
        {
            GameObject wall = new GameObject(wallName);
            wall.transform.SetParent(generatedRoot);
            wall.transform.position = position;
            wall.transform.localScale = scale;

            BoxCollider collider = wall.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.size = Vector3.one;

            Killbox killbox = wall.AddComponent<Killbox>();
            killbox.hideFlags = HideFlags.None;
        }

        private void CreateWall(string wallName, Vector3 position, Vector3 scale)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = wallName;
            wall.transform.SetParent(generatedRoot);
            wall.transform.position = position;
            wall.transform.localScale = scale;
        }

        private void BuildObstacleLayout()
        {
            freeCells.Clear();

            for (int z = 0; z < gridDepth; z++)
            {
                for (int x = 0; x < gridWidth; x++)
                {
                    Vector2Int cell = new Vector2Int(x, z);

                    // Only block the exact player/AI spawn cells.
                    if (IsSpawnCell(cell))
                    {
                        continue;
                    }

                    freeCells.Add(cell);

                    if (obstaclePrefabs == null || obstaclePrefabs.Length == 0)
                    {
                        continue;
                    }

                    if (Random.value > obstacleFillPercent)
                    {
                        continue;
                    }

                    GameObject selectedPrefab = GetRandomObstaclePrefab();
                    if (selectedPrefab == null)
                    {
                        continue;
                    }

                    Vector3 worldPos = CellToWorld(cell);
                    Quaternion rotation = Quaternion.Euler(0f, 90f * Random.Range(0, 4), 0f);

                    GameObject instance = Instantiate(selectedPrefab, worldPos, rotation, generatedRoot);
                    instance.name = $"{selectedPrefab.name}_{x}_{z}";
                }
            }
        }

        private void BuildNavMesh()
        {
            if (navMeshSurface == null)
            {
                navMeshSurface = GetComponent<NavMeshSurface>();
            }
        
            if (navMeshSurface == null)
            {
                Debug.LogWarning("ArenaGenerator: No NavMeshSurface assigned/found on ArenaRoot.");
                return;
            }
        
            navMeshSurface.BuildNavMesh();
        }

        private bool IsSpawnCell(Vector2Int cell)
        {
            return cell == playerSpawnCell || cell == aiSpawnCell;
        }

        private GameObject GetRandomObstaclePrefab()
        {
            int totalWeight = 0;

            foreach (WeightedPrefab entry in obstaclePrefabs)
            {
                if (entry != null && entry.prefab != null && entry.weight > 0)
                {
                    totalWeight += entry.weight;
                }
            }

            if (totalWeight <= 0)
            {
                return null;
            }

            int roll = Random.Range(0, totalWeight);

            foreach (WeightedPrefab entry in obstaclePrefabs)
            {
                if (entry == null || entry.prefab == null || entry.weight <= 0)
                {
                    continue;
                }

                if (roll < entry.weight)
                {
                    return entry.prefab;
                }

                roll -= entry.weight;
            }

            return null;
        }

        private void PositionActors()
        {
            Vector3 playerPos = GetGroundedSpawnPosition(playerSpawnCell);
            Vector3 aiPos = GetGroundedSpawnPosition(aiSpawnCell);

            if (playerTransform != null)
            {
                TeleportActor(playerTransform, playerPos);
            }

            if (aiTransform != null)
            {
                TeleportActor(aiTransform, aiPos);
            }

            if (playerTransform != null && aiTransform != null)
            {
                FaceActorToward(playerTransform, aiTransform.position);
                FaceActorToward(aiTransform, playerTransform.position);
            }
        }

        private Vector3 GetGroundedSpawnPosition(Vector2Int cell)
        {
            Vector3 basePos = CellToWorld(cell);
            Vector3 rayOrigin = basePos + Vector3.up * actorSpawnRayHeight;

            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, actorSpawnRayHeight * 2f, groundMask, QueryTriggerInteraction.Ignore))
            {
                return hit.point + Vector3.up * actorGroundOffset;
            }

            return new Vector3(basePos.x, actorGroundOffset, basePos.z);
        }

        private void TeleportActor(Transform actorTransform, Vector3 worldPosition)
        {
            NavMeshAgent navAgent = actorTransform.GetComponent<NavMeshAgent>();
            if (navAgent != null)
            {
                if (navAgent.enabled)
                {
                    navAgent.Warp(worldPosition);
                    return;
                }
            }
        
            CharacterController controller = actorTransform.GetComponent<CharacterController>();
            if (controller != null)
            {
                bool wasEnabled = controller.enabled;
                controller.enabled = false;
                actorTransform.position = worldPosition;
                controller.enabled = wasEnabled;
            }
            else
            {
                actorTransform.position = worldPosition;
            }
        }

        private void FaceActorToward(Transform actorTransform, Vector3 targetPosition)
        {
            Vector3 lookDir = targetPosition - actorTransform.position;
            lookDir.y = 0f;

            if (lookDir.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            actorTransform.rotation = Quaternion.LookRotation(lookDir.normalized, Vector3.up);
        }

        private void SpawnBalls()
        {
            if (dodgeballPrefab == null)
            {
                return;
            }

            List<Vector2Int> validBallCells = new List<Vector2Int>();

            foreach (Vector2Int cell in freeCells)
            {
                if (IsSpawnCell(cell))
                {
                    continue;
                }

                validBallCells.Add(cell);
            }

            Shuffle(validBallCells);

            int spawnTotal = Mathf.Min(ballCount, validBallCells.Count);

            for (int i = 0; i < spawnTotal; i++)
            {
                Vector3 spawnPos = CellToWorld(validBallCells[i]);
                spawnPos.y = ballSpawnHeight;

                Dodgeball ball = Instantiate(dodgeballPrefab, spawnPos, Quaternion.identity, generatedRoot);
                ball.name = $"Dodgeball_{i + 1}";
            }
        }

        private void Shuffle(List<Vector2Int> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                int swapIndex = Random.Range(i, list.Count);
                (list[i], list[swapIndex]) = (list[swapIndex], list[i]);
            }
        }

        private Vector3 CellToWorld(Vector2Int cell)
        {
            float arenaWidth = gridWidth * cellSize;
            float arenaDepth = gridDepth * cellSize;

            float x = -arenaWidth * 0.5f + cellSize * 0.5f + cell.x * cellSize;
            float z = -arenaDepth * 0.5f + cellSize * 0.5f + cell.y * cellSize;

            return new Vector3(x, 0f, z);
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawCellGizmos)
            {
                return;
            }

            Gizmos.color = Color.yellow;

            for (int z = 0; z < gridDepth; z++)
            {
                for (int x = 0; x < gridWidth; x++)
                {
                    Vector3 center = CellToWorld(new Vector2Int(x, z));
                    Vector3 size = new Vector3(cellSize, 0.05f, cellSize);
                    Gizmos.DrawWireCube(center, size);
                }
            }

            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(CellToWorld(playerSpawnCell) + Vector3.up * 0.5f, 0.5f);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(CellToWorld(aiSpawnCell) + Vector3.up * 0.5f, 0.5f);
        }
    }
}
