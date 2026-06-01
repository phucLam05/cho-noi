#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using ChoNoi.Domain;
using ChoNoi.Infrastructure;
using ChoNoi.Presentation;

namespace ChoNoiMienTay.Editor
{
    public class PrototypeSceneBuilder
    {
        [MenuItem("Cho Noi/Tạo Môi Trường Thử Nghiệm")]
        public static void CreatePrototypeEnvironment()
        {
            // 1. Tạo Scene mới và lưu vào thư mục Sandbox
            string scenePath = "Assets/_Project/Scenes/Sandbox/Prototype_RiverJunction.unity";
            Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            
            // Đảm bảo thư mục tồn tại
            System.IO.Directory.CreateDirectory("Assets/_Project/Scenes/Sandbox");

            // 2. Tạo hoặc load file BoatStats mặc định
            string statsPath = "Assets/_Project/DefaultBoatStats.asset";
            BoatStats statsAsset = AssetDatabase.LoadAssetAtPath<BoatStats>(statsPath);
            if (statsAsset == null)
            {
                statsAsset = ScriptableObject.CreateInstance<BoatStats>();
                
                // Thiết lập chỉ số vật lý qua Reflection (vì các trường là private [SerializeField])
                SetPrivateField(statsAsset, "thrustForce", 15f);
                SetPrivateField(statsAsset, "waterDrag", 1.5f);
                SetPrivateField(statsAsset, "sidewaysDrag", 6.0f);
                SetPrivateField(statsAsset, "turnTorque", 4.0f);
                
                AssetDatabase.CreateAsset(statsAsset, statsPath);
                AssetDatabase.SaveAssets();
                Debug.Log($"[PrototypeSceneBuilder] Đã tạo file BoatStats mặc định tại: {statsPath}");
            }

            // 3. Xây dựng địa hình Terrain
            TerrainData terrainData = new TerrainData();
            string terrainDataPath = "Assets/_Project/Scenes/Sandbox/Prototype_RiverData.asset";
            AssetDatabase.CreateAsset(terrainData, terrainDataPath);

            GameObject terrainGO = Terrain.CreateTerrainGameObject(terrainData);
            terrainGO.name = "RiverTerrain";
            Terrain terrain = terrainGO.GetComponent<Terrain>();
            
            // Cấu hình kích thước Terrain
            terrainData.heightmapResolution = 513;
            terrainData.size = new Vector3(150f, 40f, 150f);

            // Sinh địa hình lòng sông hình chữ Y
            int res = terrainData.heightmapResolution;
            float[,] heights = new float[res, res];

            float baseHeight = 0.15f; // 6m
            float waterLevel = 0.10f; // 4m
            float riverBed = 0.02f;   // 0.8m

            Vector2 mainStart = new Vector2(75f, 0f);
            Vector2 junction = new Vector2(75f, 65f);
            Vector2 leftForkEnd = new Vector2(20f, 150f);
            Vector2 rightForkEnd = new Vector2(130f, 150f);

            float mainHalfWidth = 16f; // Sông lớn rộng 32m
            float forkHalfWidth = 8f;  // Rạch nhỏ rộng 16m

            for (int row = 0; row < res; row++)
            {
                for (int col = 0; col < res; col++)
                {
                    float x = (float)col / (res - 1) * 150f;
                    float z = (float)row / (res - 1) * 150f;
                    Vector2 p = new Vector2(x, z);

                    float dMain = DistToSegment(p, mainStart, junction);
                    float dLeft = DistToSegment(p, junction, leftForkEnd);
                    float dRight = DistToSegment(p, junction, rightForkEnd);

                    float carve = 0f;

                    if (dMain < mainHalfWidth)
                    {
                        float factor = 1f - (dMain / mainHalfWidth);
                        carve = Mathf.Max(carve, factor * (baseHeight - riverBed));
                    }
                    if (dLeft < forkHalfWidth)
                    {
                        float factor = 1f - (dLeft / forkHalfWidth);
                        carve = Mathf.Max(carve, factor * (baseHeight - riverBed));
                    }
                    if (dRight < forkHalfWidth)
                    {
                        float factor = 1f - (dRight / forkHalfWidth);
                        carve = Mathf.Max(carve, factor * (baseHeight - riverBed));
                    }

                    heights[row, col] = baseHeight - carve;
                }
            }
            terrainData.SetHeights(0, 0, heights);

            // 4. Tạo mặt nước (Water Plane)
            GameObject waterGO = GameObject.CreatePrimitive(PrimitiveType.Plane);
            waterGO.name = "WaterPlane";
            waterGO.transform.position = new Vector3(75f, 4.0f, 75f); // Cao độ nước Y = 4.0m
            waterGO.transform.localScale = new Vector3(15f, 1f, 15f); // 150m x 150m
            
            // Gán vật liệu nước trong suốt
            Renderer waterRenderer = waterGO.GetComponent<Renderer>();
            Shader waterShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Diffuse");
            Material waterMat = new Material(waterShader);
            waterMat.color = new Color(0.1f, 0.4f, 0.6f, 0.6f);
            
            // Thiết lập chế độ transparent dựa trên shader
            if (waterShader.name.Contains("Universal Render Pipeline"))
            {
                waterMat.SetFloat("_Surface", 1.0f); // Transparent
                waterMat.SetFloat("_Blend", 0.0f);
                waterMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                waterMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                waterMat.SetInt("_ZWrite", 0);
                waterMat.DisableKeyword("_ALPHATEST_ON");
                waterMat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            }
            else
            {
                // Standard shader transparent setup
                waterMat.SetFloat("_Mode", 3f); // Transparent mode
                waterMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                waterMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                waterMat.SetInt("_ZWrite", 0);
                waterMat.DisableKeyword("_ALPHATEST_ON");
                waterMat.EnableKeyword("_ALPHABLEND_ON");
                waterMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            }
            
            waterMat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            waterRenderer.material = waterMat;

            // Xóa collider cũ của plane để thay bằng BoxCollider phẳng hỗ trợ ghe trượt mượt mà
            Collider oldCollider = waterGO.GetComponent<Collider>();
            if (oldCollider != null)
            {
                Object.DestroyImmediate(oldCollider);
            }
            BoxCollider waterCollider = waterGO.AddComponent<BoxCollider>();
            waterCollider.center = new Vector3(0, -0.05f, 0);
            waterCollider.size = new Vector3(10f, 0.1f, 10f); // default plane size is 10x10

            // Tạo vật liệu vật lý không ma sát (in-memory) để ghe trượt mượt trên nước
            PhysicsMaterial frictionlessMat = new PhysicsMaterial("FrictionlessWater");
            frictionlessMat.staticFriction = 0f;
            frictionlessMat.dynamicFriction = 0f;
            frictionlessMat.frictionCombine = PhysicsMaterialCombine.Minimum;
            waterCollider.sharedMaterial = frictionlessMat;

            // 5. Tạo ranh giới di chuyển bằng các Box Colliders chạy dọc mép bờ sông (Boundary Colliders)
            GameObject boundariesRoot = new GameObject("BoundaryColliders");
            
            // Danh sách các đoạn biên sông (A -> B)
            Vector2[] points = new Vector2[] {
                // Bottom Cap
                new Vector2(59f, 0f), new Vector2(91f, 0f),
                // Left Fork End Cap
                new Vector2(12f, 150f), new Vector2(28f, 150f),
                // Right Fork End Cap
                new Vector2(122f, 150f), new Vector2(138f, 150f),
                
                // Mép sông chính bên trái
                new Vector2(59f, 0f), new Vector2(59f, 65f),
                // Mép sông chính bên phải
                new Vector2(91f, 0f), new Vector2(91f, 65f),
                
                // Mép rạch trái phía ngoài
                new Vector2(59f, 65f), new Vector2(12f, 150f),
                // Mép rạch phải phía ngoài
                new Vector2(91f, 65f), new Vector2(138f, 150f),
                
                // Mép rạch trái phía trong (cạnh đảo)
                new Vector2(75f, 75f), new Vector2(28f, 150f),
                // Mép rạch phải phía trong (cạnh đảo)
                new Vector2(75f, 75f), new Vector2(122f, 150f)
            };

            for (int i = 0; i < points.Length; i += 2)
            {
                Vector2 a = points[i];
                Vector2 b = points[i + 1];
                Vector2 dir = b - a;
                float len = dir.magnitude;
                
                GameObject wall = new GameObject($"Wall_{i/2}");
                wall.transform.SetParent(boundariesRoot.transform);
                wall.transform.position = new Vector3((a.x + b.x) / 2f, 5.0f, (a.y + b.y) / 2f);
                if (len > 0.001f)
                {
                    wall.transform.rotation = Quaternion.LookRotation(new Vector3(dir.x, 0f, dir.y));
                }
                
                BoxCollider bc = wall.AddComponent<BoxCollider>();
                bc.size = new Vector3(2.0f, 10.0f, len); // Dày 2m, cao 10m
            }

            // 6. Tạo Ghe nháp (Draft Boat)
            GameObject boatGO = new GameObject("DraftBoat");
            boatGO.transform.position = new Vector3(75f, 4.4f, 20f); // Xuất phát ở giữa sông chính
            
            // Thiết lập Rigidbody
            Rigidbody rb = boatGO.AddComponent<Rigidbody>();
            rb.mass = 1.0f;
            rb.linearDamping = 0.5f;
            rb.angularDamping = 0.8f;
            rb.useGravity = true;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

            // BoxCollider cho thân ghe
            BoxCollider boatCollider = boatGO.AddComponent<BoxCollider>();
            boatCollider.center = new Vector3(0, 0.4f, 0);
            boatCollider.size = new Vector3(1.5f, 0.8f, 4.0f); // Dài 4m, rộng 1.5m, cao 0.8m

            // Visuals: Thân ghe nháp
            GameObject hull = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hull.name = "HullVisual";
            hull.transform.SetParent(boatGO.transform, false);
            hull.transform.localPosition = new Vector3(0, 0.4f, 0);
            hull.transform.localScale = new Vector3(1.5f, 0.8f, 4.0f);
            Object.DestroyImmediate(hull.GetComponent<Collider>());

            // Visuals: Buồng cabin nháp
            GameObject cabin = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cabin.name = "CabinVisual";
            cabin.transform.SetParent(boatGO.transform, false);
            cabin.transform.localPosition = new Vector3(0f, 1.0f, -0.8f);
            cabin.transform.localScale = new Vector3(1.2f, 0.6f, 1.6f);
            Object.DestroyImmediate(cabin.GetComponent<Collider>());
            // Màu sắc cabin khác biệt (dùng sharedMaterial để tránh warning trong Edit Mode)
            Material cabinMat = new Material(Shader.Find("Standard") ?? Shader.Find("Diffuse"));
            cabinMat.color = Color.gray;
            cabin.GetComponent<Renderer>().sharedMaterial = cabinMat;

            // Visuals: Đánh dấu mũi ghe (đầu đỏ để dễ phân biệt hướng)
            GameObject bowIndicator = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bowIndicator.name = "BowIndicator";
            bowIndicator.transform.SetParent(boatGO.transform, false);
            bowIndicator.transform.localPosition = new Vector3(0f, 0.9f, 1.8f);
            bowIndicator.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
            Object.DestroyImmediate(bowIndicator.GetComponent<Collider>());
            
            Material bowMat = new Material(Shader.Find("Standard") ?? Shader.Find("Diffuse"));
            bowMat.color = Color.red;
            bowIndicator.GetComponent<Renderer>().sharedMaterial = bowMat;

            // Gán các Scripts điều khiển ghe
            PCBoatInput pcInput = boatGO.AddComponent<PCBoatInput>();
            BoatController controller = boatGO.AddComponent<BoatController>();

            // Gán InputActionAsset thông qua tìm kiếm tự động và Reflection
            string[] inputActionGUIDs = AssetDatabase.FindAssets("InputSystem_Actions t:InputActionAsset");
            if (inputActionGUIDs.Length > 0)
            {
                string inputActionsPath = AssetDatabase.GUIDToAssetPath(inputActionGUIDs[0]);
                InputActionAsset inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(inputActionsPath);
                SetPrivateField(pcInput, "inputActions", inputActions);
            }
            else
            {
                Debug.LogWarning("[PrototypeSceneBuilder] Không tìm thấy file InputSystem_Actions.inputactions trong dự án.");
            }

            // Gán BoatStats mặc định
            SetPrivateField(controller, "boatStats", statsAsset);

            // 7. Thiết lập Camera bám theo ghe sử dụng Cinemachine (hỗ trợ cả CM 2.x và 3.x qua Reflection)
            GameObject vcamGO = new GameObject("CinemachineVirtualCamera");
            vcamGO.transform.position = new Vector3(75f, 10f, 10f); // Đặt vị trí ban đầu cho vcam gần ghe
            
            System.Type vcamType = System.Type.GetType("Unity.Cinemachine.CinemachineCamera, Unity.Cinemachine") 
                                ?? System.Type.GetType("Cinemachine.CinemachineVirtualCamera, Cinemachine");

            // Setup camera chính ở góc quay hướng về ghe để làm fallback
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                mainCam.transform.position = new Vector3(75f, 10f, 10f);
                mainCam.transform.rotation = Quaternion.Euler(30f, 0f, 0f);
            }

            if (vcamType != null)
            {
                Component vcam = vcamGO.AddComponent(vcamType);
                
                // Liên kết Follow & LookAt
                var followProp = vcamType.GetProperty("Follow") ?? vcamType.GetProperty("m_Follow");
                var lookAtProp = vcamType.GetProperty("LookAt") ?? vcamType.GetProperty("m_LookAt");
                
                if (followProp != null) followProp.SetValue(vcam, boatGO.transform);
                if (lookAtProp != null) lookAtProp.SetValue(vcam, boatGO.transform);

                // Thêm thành phần di chuyển (Body) để camera thực sự di chuyển đi theo
                System.Type bodyType = System.Type.GetType("Unity.Cinemachine.Cinemachine3rdPersonFollow, Unity.Cinemachine") 
                                    ?? System.Type.GetType("Cinemachine.CinemachineTransposer, Cinemachine");
                if (bodyType != null)
                {
                    Component bodyComp = vcamGO.AddComponent(bodyType);
                    // Đặt khoảng cách camera
                    var distField = bodyType.GetField("CameraDistance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
                                 ?? bodyType.GetField("m_CameraDistance", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (distField != null)
                    {
                        distField.SetValue(bodyComp, 12f); // CM 3.x distance
                    }
                    else
                    {
                        var offsetField = bodyType.GetField("m_FollowOffset", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (offsetField != null)
                        {
                            offsetField.SetValue(bodyComp, new Vector3(0f, 6f, -12f)); // CM 2.x offset
                        }
                    }
                }

                // Thêm CinemachineBrain vào MainCamera nếu chưa có
                if (mainCam != null)
                {
                    System.Type brainType = System.Type.GetType("Unity.Cinemachine.CinemachineBrain, Unity.Cinemachine")
                                         ?? System.Type.GetType("Cinemachine.CinemachineBrain, Cinemachine");
                    if (brainType != null && mainCam.GetComponent(brainType) == null)
                    {
                        mainCam.gameObject.AddComponent(brainType);
                    }
                }
                Debug.Log("[PrototypeSceneBuilder] Thiết lập Cinemachine Camera bám theo ghe thành công.");
            }
            else
            {
                // Fallback: Nếu chưa biên dịch xong Cinemachine, viết thông báo nhắc nhở
                Debug.LogWarning("[PrototypeSceneBuilder] Không tìm thấy Cinemachine trong Project. Vui lòng kiểm tra quá trình cài đặt Package.");
            }

            // Lưu và mở Scene
            EditorSceneManager.SaveScene(newScene, scenePath);
            EditorSceneManager.OpenScene(scenePath);
            
            Debug.Log($"\n==================================================" +
                      $"\n  [SUCCESS] Đã tạo thành công Scene thử nghiệm!" +
                      $"\n  Đường dẫn: {scenePath}" +
                      $"\n==================================================\n");
        }

        // --- CÁC HÀM TRỢ GIÚP NỘI BỘ ---

        private static float DistToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            float l2 = (b - a).sqrMagnitude;
            if (l2 == 0) return (p - a).magnitude;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, b - a) / l2);
            Vector2 projection = a + t * (b - a);
            return (p - projection).magnitude;
        }

        private static void SetPrivateField(object obj, string fieldName, object value)
        {
            if (obj == null) return;
            var field = obj.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(obj, value);
            }
            else
            {
                Debug.LogError($"[PrototypeSceneBuilder] Không tìm thấy trường {fieldName} trong class {obj.GetType().Name}");
            }
        }
    }
}
#endif
