using System.Collections.Generic;
using UnityEngine;
using ChoNoi.Application;
using ChoNoi.Domain;
using ChoNoi.Presentation.NPC;
using ChoNoiMienTay.Presentation;
using ChoNoiMienTay.Infrastructure;

namespace ChoNoi.Systems
{
    public class CustomerSpawnManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TimeManager timeManager;
        [SerializeField] private BambooPoleManager bambooPoleManager;

        [Header("Spawn Settings")]
        [SerializeField] private float minSpawnInterval = 2f;
        [SerializeField] private float maxSpawnInterval = 6f; // Spawns very fast to create a bustling market!
        [SerializeField] private int maxActiveCustomers = 12; // Allow up to 12 active boats on the river

        private GameObject templateBoat;
        private List<GameObject> activeCustomers = new List<GameObject>();
        private float spawnTimer;
        private bool isSpawningActive;

        // Group points at the edges of the map (out of sight)
        private readonly Vector3[] spawnPoints = new Vector3[]
        {
            new Vector3(120f, 3.1f, 10f),   // Spawn 0: Downstream start
            new Vector3(36f, 3.1f, 215f),   // Spawn 1: Left Fork End
            new Vector3(204f, 3.1f, 215f)   // Spawn 2: Right Fork End
        };

        private readonly string[] customerNames = new string[]
        {
            "Bác Ba Khóm", "Chị Tư Chợ Nổi", "Anh Năm Lục Bình", "Mỹ Duyên", 
            "Văn Hải", "Hoàng Nam", "Út Nhỏ", "Dì Bảy Cam", "Chú Chín Xoài", 
            "Thím Hai Dưa Hấu", "Anh Ba Bí Đao", "Chị Sáu Miệt Vườn"
        };

        private readonly string[] touristNames = new string[]
        {
            "Khách Du Lịch Sài Gòn", "Tây Balo John", "Gia Đình Phương Xa", "Nhà Khảo Sát Văn Hóa"
        };

        private readonly string[] vendorNames = new string[]
        {
            "Ghe Hủ Tiếu Lắc", "Ghe Cà Phê Vợt Cô Năm", "Ghe Bánh Mì Nóng", "Ghe Nước Sâm Mát Lạnh"
        };

        private void Start()
        {
            if (timeManager == null) timeManager = FindAnyObjectByType<TimeManager>();
            if (bambooPoleManager == null)
            {
                var boat = GameObject.Find("PlayerBoat");
                if (boat != null)
                {
                    bambooPoleManager = boat.GetComponent<BambooPoleManager>();
                }
            }

            if (timeManager != null)
            {
                timeManager.OnPhaseChanged += HandlePhaseChanged;
                // Initialize based on current phase
                HandlePhaseChanged(timeManager.CurrentPhase);
            }

            // Find template boat to clone
            templateBoat = GameObject.Find("PaddlingNpcBoat");
            if (templateBoat == null)
            {
                Debug.LogWarning("[CustomerSpawnManager] PaddlingNpcBoat template not found in scene. Looking for NpcBoats children.");
                var npcBoats = GameObject.Find("NpcBoats");
                if (npcBoats != null && npcBoats.transform.childCount > 0)
                {
                    templateBoat = npcBoats.transform.GetChild(0).gameObject;
                }
            }
            
            ResetSpawnTimer();
        }

        private void OnDestroy()
        {
            if (timeManager != null)
            {
                timeManager.OnPhaseChanged -= HandlePhaseChanged;
            }
        }

        private void Update()
        {
            if (!isSpawningActive || templateBoat == null) return;

            // Check cleanup of destroyed customers
            activeCustomers.RemoveAll(item => item == null);

            if (activeCustomers.Count >= maxActiveCustomers) return;

            // Only spawn if player is inside the Morning Market Zone (Z = [55f, 95f])
            GameObject playerBoat = GameObject.Find("PlayerBoat");
            if (playerBoat == null) return;

            float playerZ = playerBoat.transform.position.z;
            if (playerZ < 55f || playerZ > 95f)
            {
                // Player is not in the market zone! Stop spawning.
                return;
            }

            spawnTimer -= Time.deltaTime;
            if (spawnTimer <= 0f)
            {
                SpawnRandomNpc();
                ResetSpawnTimer();
            }
        }

        private void HandlePhaseChanged(GamePhase phase)
        {
            if (phase == GamePhase.Dawn)
            {
                isSpawningActive = true;
                ResetSpawnTimer();
                Debug.Log("[CustomerSpawnManager] Market phase started. Spawn manager is ACTIVE.");
            }
            else
            {
                isSpawningActive = false;
                DismissAllSpawnedCustomers();
                Debug.Log("[CustomerSpawnManager] Market phase ended. Dismissing spawned customers.");
            }
        }

        private void ResetSpawnTimer()
        {
            float attractMultiplier = 1f;
            if (bambooPoleManager != null && bambooPoleManager.DisplayedItems.Count > 0)
            {
                attractMultiplier = 1f + bambooPoleManager.DisplayedItems.Count * 0.7f; // slightly stronger attraction
            }

            float baseInterval = Random.Range(minSpawnInterval, maxSpawnInterval);
            spawnTimer = baseInterval / attractMultiplier;
        }

        private void SpawnRandomNpc()
        {
            if (templateBoat == null) return;

            // Select a random spawn point
            int spawnIdx = Random.Range(0, spawnPoints.Length);
            Vector3 spawnPos = spawnPoints[spawnIdx];
            
            // Randomize position slightly to avoid perfect overlap
            spawnPos += new Vector3(Random.Range(-3f, 3f), 0f, Random.Range(-3f, 3f));
            Quaternion spawnRot = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            // Clone the template boat
            GameObject clone = Instantiate(templateBoat, spawnPos, spawnRot);
            clone.SetActive(true);

            // Setup NpcTradeTarget
            var tradeTarget = clone.GetComponentInChildren<NpcTradeTarget>();
            if (tradeTarget == null)
            {
                tradeTarget = clone.AddComponent<NpcTradeTarget>();
            }

            // Decide NPC type
            float rand = Random.value;
            bool isBuyer = rand < 0.70f; // 70% Buyer Customer, 15% Tourist, 15% Food Vendor

            if (isBuyer)
            {
                // 1. Buyer Customer - approaches player to buy Cây Bẹo items
                clone.name = $"SpawnedCustomer_{Random.Range(1000, 9999)}";
                string displayName = customerNames[Random.Range(0, customerNames.Length)];
                tradeTarget.Configure(displayName, 4.5f, InteractionTargetType.Bargain);
                tradeTarget.HasTraded = false;

                // Pick a random desired item matching Cây Bẹo displayed items
                if (bambooPoleManager != null && bambooPoleManager.DisplayedItems.Count > 0)
                {
                    ItemData desired = bambooPoleManager.DisplayedItems[Random.Range(0, bambooPoleManager.DisplayedItems.Count)];
                    tradeTarget.DesiredItem = desired;
                    tradeTarget.DesiredQuantity = Random.Range(2, 6);
                }
                else
                {
                    // Fallback if Cây Bẹo has nothing (should not happen due to Update check)
                    Destroy(clone);
                    return;
                }

                // Buyer customer does NOT patrol: remove NpcBoatPatrol so NpcCustomerBehavior controls it
                var patrol = clone.GetComponent<NpcBoatPatrol>();
                if (patrol != null) Destroy(patrol);

                // Setup NpcCustomerBehavior
                var behavior = clone.GetComponentInChildren<NpcCustomerBehavior>();
                if (behavior == null) behavior = clone.AddComponent<NpcCustomerBehavior>();
                behavior.isSpawnedCustomer = true;
                behavior.SetReturnPoint(spawnPos, spawnRot);

                Debug.Log($"[CustomerSpawnManager] Spawned Customer: {displayName} wanting {tradeTarget.DesiredQuantity}x {tradeTarget.DesiredItem.itemName} at {spawnPos}");
            }
            else
            {
                // 2. Tourist or Food Vendor - moves along patrol waypoints, doesn't cluster on player
                bool isTourist = rand < 0.85f;
                string displayName = "";
                
                if (isTourist)
                {
                    clone.name = $"SpawnedTourist_{Random.Range(1000, 9999)}";
                    displayName = touristNames[Random.Range(0, touristNames.Length)];
                    tradeTarget.Configure(displayName, 4.5f, InteractionTargetType.Trade); // gardener conversation path
                }
                else
                {
                    clone.name = $"SpawnedFoodVendor_{Random.Range(1000, 9999)}";
                    displayName = vendorNames[Random.Range(0, vendorNames.Length)];
                    tradeTarget.Configure(displayName, 4.5f, InteractionTargetType.Trade); // vendor noodle/stamina purchase path
                }
                tradeTarget.HasTraded = false;

                // Ensure NpcCustomerBehavior is NOT on it so it doesn't approach player boat automatically
                var behavior = clone.GetComponentInChildren<NpcCustomerBehavior>();
                if (behavior != null) Destroy(behavior);

                // Define traversing waypoints so they travel through the market area
                Vector3[] path;
                if (spawnIdx == 0) // Spawned Downstream (120, 3.1, 10) -> goes Upstream
                {
                    bool exitLeft = Random.value < 0.5f;
                    Vector3 exitFork = exitLeft ? new Vector3(36f, 3.1f, 215f) : new Vector3(204f, 3.1f, 215f);
                    path = new Vector3[]
                    {
                        new Vector3(120f, 3.1f, 55f),
                        new Vector3(120f, 3.1f, 90f),
                        new Vector3(120f, 3.1f, 115f),
                        exitFork
                    };
                }
                else if (spawnIdx == 1) // Spawned Left Fork (36, 3.1, 215) -> goes Downstream
                {
                    path = new Vector3[]
                    {
                        new Vector3(108f, 3.1f, 115f),
                        new Vector3(120f, 3.1f, 90f),
                        new Vector3(120f, 3.1f, 55f),
                        new Vector3(120f, 3.1f, 10f)
                    };
                }
                else // Spawned Right Fork (204, 3.1, 215) -> goes Downstream
                {
                    path = new Vector3[]
                    {
                        new Vector3(132f, 3.1f, 115f),
                        new Vector3(120f, 3.1f, 90f),
                        new Vector3(120f, 3.1f, 55f),
                        new Vector3(120f, 3.1f, 10f)
                    };
                }

                // Configure NpcBoatPatrol to traverse this path and self-destruct when finished
                var patrol = clone.GetComponent<NpcBoatPatrol>();
                if (patrol == null) patrol = clone.AddComponent<NpcBoatPatrol>();
                
                float patrolSpeed = Random.Range(1.8f, 2.5f);
                Animator animator = clone.GetComponentInChildren<Animator>();
                patrol.Configure(path, patrolSpeed, animator);
                patrol.destroyOnLastWaypoint = true;

                Debug.Log($"[CustomerSpawnManager] Spawned traversing boat: {displayName} at {spawnPos}");
            }

            activeCustomers.Add(clone);
        }

        private void DismissAllSpawnedCustomers()
        {
            activeCustomers.RemoveAll(item => item == null);
            foreach (var customer in activeCustomers)
            {
                if (customer == null) continue;
                
                var behavior = customer.GetComponentInChildren<NpcCustomerBehavior>();
                if (behavior != null)
                {
                    // Transition them to Returning, which will auto-destroy them when they arrive
                    behavior.SetReturnPoint(customer.transform.position, customer.transform.rotation); // immediately return
                    behavior.GetType().GetField("state", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(behavior, 4); // state = Returning
                }
                else
                {
                    Destroy(customer);
                }
            }
            activeCustomers.Clear();
        }
    }
}
