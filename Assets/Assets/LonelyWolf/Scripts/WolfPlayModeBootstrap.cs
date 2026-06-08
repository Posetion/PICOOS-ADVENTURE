using StarterAssets;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace LonelyWolf
{
    /// <summary>
    /// Wolf scene: lake (visible in Editor + Play) and Pico swim using Fantacode swim animations.
    /// </summary>
    [ExecuteAlways]
    [DefaultExecutionOrder(-200)]
    public sealed class WolfPlayModeBootstrap : MonoBehaviour
    {
        const string WaterBlockPrefabPath =
            "Assets/Assets/ImportedAssets/Enviroment/IgniteCoders/Simple Water Shader/Prefabs/WaterBlock_50m.prefab";
        const string LakeVisualName = "WolfLake_Visual";
        const string LakeVolumeName = "WolfLake_WaterVolume";

        [SerializeField] private float lakeSizePercent = 0.38f;
        [SerializeField] private float lakeDepth = 10f;
        [SerializeField] private float waterSurfaceOffset = 0.15f;
        [SerializeField] private bool placeLakeAtPlayer = true;

        Transform _lakeRoot;

#if DEVELOPMENT_BUILD || UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void DisableRenderingDebuggerBeforeSceneLoad()
        {
            if (!IsWolfScene())
            {
                return;
            }

            DebugManager.instance.enableRuntimeUI = false;
        }
#endif

        void OnEnable()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                EditorApplication.delayCall += EditorEnsureLake;
            }
#endif
        }

        void OnDisable()
        {
#if UNITY_EDITOR
            EditorApplication.delayCall -= EditorEnsureLake;
#endif
        }

        void Awake()
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            DebugManager.instance.enableRuntimeUI = false;
#endif
        }

        void Start()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            SetupWolfScene();
        }

#if UNITY_EDITOR
        void EditorEnsureLake()
        {
            if (this == null || Application.isPlaying)
            {
                return;
            }

            if (!IsWolfScene())
            {
                return;
            }

            SetupWolfScene();
            EditorUtility.SetDirty(this);
            if (!Application.isPlaying)
            {
                EditorSceneManager.MarkSceneDirty(gameObject.scene);
            }
        }

        [ContextMenu("Create / Refresh Wolf Lake")]
        void ContextCreateLake()
        {
            ClearLakeChildren();
            SetupWolfScene();
            EditorUtility.SetDirty(this);
            EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }
#endif

        void SetupWolfScene()
        {
            Terrain terrain = Terrain.activeTerrain ?? FindFirstObjectByType<Terrain>();
            if (terrain == null)
            {
                Debug.LogWarning("[WolfPlayModeBootstrap] No terrain found.");
                return;
            }

            Transform player = FindPicoPlayerRoot();
            float surfaceY = EnsureLake(terrain, player);

            if (!Application.isPlaying)
            {
                return;
            }

            if (player == null)
            {
                Debug.LogWarning("[WolfPlayModeBootstrap] Lake created — no Player tag found for swim.");
                return;
            }

            EnsurePicoVisible(player);
            var swim = player.GetComponent<PicoFantacodeSwim>();
            if (swim == null)
            {
                swim = player.gameObject.AddComponent<PicoFantacodeSwim>();
            }

            swim.Configure(surfaceY);
        }

        float EnsureLake(Terrain terrain, Transform player)
        {
            _lakeRoot = transform.Find("WolfLake");
            if (_lakeRoot == null)
            {
                var rootGo = new GameObject("WolfLake");
                rootGo.transform.SetParent(transform, false);
                _lakeRoot = rootGo.transform;
            }

            Transform existingVisual = _lakeRoot.Find(LakeVisualName);
            Transform existingVolume = _lakeRoot.Find(LakeVolumeName);
            if (existingVisual != null && existingVolume != null)
            {
                return ReadSurfaceYFromLake(existingVisual, terrain);
            }

            ClearLakeChildren();

            TerrainData data = terrain.terrainData;
            Vector3 terrainPos = terrain.transform.position;
            Vector3 size = data.size;

            Vector3 lakeCenter = terrainPos + new Vector3(size.x * 0.5f, 0f, size.z * 0.5f);
            if (placeLakeAtPlayer && player != null)
            {
                lakeCenter = new Vector3(player.position.x, 0f, player.position.z);
            }

            float surfaceY = terrain.SampleHeight(lakeCenter) + terrainPos.y + waterSurfaceOffset;
            float lakeWidth = Mathf.Max(28f, size.x * lakeSizePercent);
            float lakeLength = Mathf.Max(28f, size.z * lakeSizePercent);

            GameObject waterVisual = CreateWaterVisual(lakeCenter, surfaceY, lakeWidth, lakeLength);
            waterVisual.name = LakeVisualName;
            waterVisual.transform.SetParent(_lakeRoot, false);

            int waterLayer = LayerMask.NameToLayer("Water");
            if (waterLayer < 0)
            {
                Debug.LogWarning("[WolfPlayModeBootstrap] Add a 'Water' layer in Tags & Layers for swimming.");
                waterLayer = 0;
            }

            var volume = new GameObject(LakeVolumeName);
            volume.transform.SetParent(_lakeRoot, false);
            volume.layer = waterLayer;
            volume.transform.position = new Vector3(lakeCenter.x, surfaceY + lakeDepth * 0.45f, lakeCenter.z);
            var trigger = volume.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(lakeWidth, lakeDepth + 6f, lakeLength);

            Debug.Log(
                $"[WolfPlayModeBootstrap] Lake at ({lakeCenter.x:F0}, {surfaceY:F1}, {lakeCenter.z:F0}), " +
                $"size {lakeWidth:F0}x{lakeLength:F0}. Look for 'WolfLake' under PlayModeBootstrap.");

            return surfaceY;
        }

        static float ReadSurfaceYFromLake(Transform visual, Terrain terrain)
        {
            float y = visual.position.y + 0.75f;
            Vector3 p = visual.position;
            p.y = 0f;
            return terrain.SampleHeight(p) + terrain.transform.position.y + 0.15f;
        }

        void ClearLakeChildren()
        {
            Transform lake = transform.Find("WolfLake");
            if (lake == null)
            {
                return;
            }

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                DestroyImmediate(lake.gameObject);
                return;
            }
#endif
            Destroy(lake.gameObject);
        }

        GameObject CreateWaterVisual(Vector3 center, float surfaceY, float width, float length)
        {
            float scaleX = width / 50f;
            float scaleZ = length / 50f;
            Vector3 position = new Vector3(center.x, surfaceY - 0.75f, center.z);
            Vector3 scale = new Vector3(scaleX, 1f, scaleZ);

#if UNITY_EDITOR
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(WaterBlockPrefabPath);
            if (prefab != null)
            {
                GameObject water = Application.isPlaying
                    ? Instantiate(prefab, position, Quaternion.identity)
                    : (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                water.transform.position = position;
                water.transform.localScale = scale;
                return water;
            }

            Debug.LogWarning(
                $"[WolfPlayModeBootstrap] Water prefab not found at:\n{WaterBlockPrefabPath}\nUsing blue fallback cube.");
#endif

            var fallback = GameObject.CreatePrimitive(PrimitiveType.Cube);
            DestroyCollider(fallback);
            fallback.transform.position = new Vector3(center.x, surfaceY - 0.35f, center.z);
            fallback.transform.localScale = new Vector3(width, 1.5f, length);
            var renderer = fallback.GetComponent<Renderer>();
            if (renderer != null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit")
                    ?? Shader.Find("Universal Render Pipeline/Simple Lit");
                if (shader != null)
                {
                    var mat = new Material(shader);
                    mat.SetFloat("_Surface", 1f);
                    mat.color = new Color(0.12f, 0.5f, 0.85f, 0.85f);
                    renderer.sharedMaterial = mat;
                }
            }

            return fallback;
        }

        static void DestroyCollider(GameObject go)
        {
            var col = go.GetComponent<Collider>();
            if (col != null)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    DestroyImmediate(col);
                    return;
                }
#endif
                Destroy(col);
            }
        }

        static Transform FindPicoPlayerRoot()
        {
            GameObject tagged = GameObject.FindGameObjectWithTag("Player");
            if (tagged != null)
            {
                return tagged.transform;
            }

            var tpc = FindFirstObjectByType<ThirdPersonController>();
            return tpc != null ? tpc.transform : null;
        }

        static void EnsurePicoVisible(Transform playerRoot)
        {
            Transform pico = playerRoot.Find("PicoChan") ?? FindChildByName(playerRoot, "PicoChan");
            if (pico == null)
            {
                return;
            }

            pico.gameObject.SetActive(true);
            foreach (var renderer in pico.GetComponentsInChildren<Renderer>(true))
            {
                renderer.enabled = true;
            }

            foreach (var animator in playerRoot.GetComponentsInChildren<Animator>(true))
            {
                animator.enabled = true;
            }
        }

        static Transform FindChildByName(Transform root, string childName)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == childName)
                {
                    return child;
                }
            }

            return null;
        }

        static bool IsWolfScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.name.Equals("wolf", System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

#if UNITY_EDITOR
            string path = scene.path;
            return path.EndsWith("/wolf.unity", System.StringComparison.OrdinalIgnoreCase)
                || path.EndsWith("\\wolf.unity", System.StringComparison.OrdinalIgnoreCase);
#else
            return false;
#endif
        }
    }

    /// <summary>
    /// Pico uses Fantacode swim animator in water; StarterAssets locomotion on land.
    /// </summary>
    public sealed class PicoFantacodeSwim : MonoBehaviour
    {
        const string SwimAnimatorPath =
            "Assets/Assets/ImportedAssets/Fantacode Studios/Swimming System/Animator/Swimming Controller.controller";
        const string SwimmingActionState = "Swimming Action";

        static readonly int IsSwimmingHash = Animator.StringToHash("IsSwimming");
        static readonly int SwimSpeedHash = Animator.StringToHash("SwimSpeed");

        const float SwimOffset = 1.33f;
        const float NormalSwimSpeed = 2.2f;
        const float FastSwimSpeed = 4.2f;
        const float SurfaceBob = 0.35f;

        ThirdPersonController _locomotion;
        CharacterController _controller;
        StarterAssetsInputs _input;
        Animator _animator;
        RuntimeAnimatorController _landController;
        RuntimeAnimatorController _swimController;
        LayerMask _waterMask;
        Transform _cameraTransform;
        float _waterSurfaceY;
        bool _isSwimming;
        float _smoothedSwimSpeed;

        public void Configure(float waterSurfaceY)
        {
            _waterSurfaceY = waterSurfaceY;
        }

        void Awake()
        {
            _locomotion = GetComponent<ThirdPersonController>();
            _controller = GetComponent<CharacterController>();
            _input = GetComponent<StarterAssetsInputs>();
            _waterMask = LayerMask.GetMask("Water");
            _animator = GetComponentInChildren<Animator>();
            _landController = _animator != null ? _animator.runtimeAnimatorController : null;
            _swimController = LoadSwimAnimatorController();

            if (_locomotion != null && _locomotion.CinemachineCameraTarget != null)
            {
                _cameraTransform = _locomotion.CinemachineCameraTarget.transform;
            }

            if (_cameraTransform == null && Camera.main != null)
            {
                _cameraTransform = Camera.main.transform;
            }
        }

        void Update()
        {
            if (_animator == null || _controller == null || _waterMask.value == 0)
            {
                return;
            }

            bool inWater = Physics.CheckSphere(
                transform.position + Vector3.up * SwimOffset,
                0.28f,
                _waterMask,
                QueryTriggerInteraction.Collide);

            if (inWater && !_isSwimming)
            {
                EnterWater();
            }
            else if (!inWater && _isSwimming)
            {
                ExitWater();
            }

            if (_isSwimming)
            {
                SwimMovement();
            }
        }

        void EnterWater()
        {
            _isSwimming = true;
            if (_locomotion != null)
            {
                _locomotion.enabled = false;
            }

            if (_swimController != null)
            {
                _animator.runtimeAnimatorController = _swimController;
            }

            _animator.SetBool(IsSwimmingHash, true);
            _animator.CrossFadeInFixedTime(SwimmingActionState, 0.2f);
        }

        void ExitWater()
        {
            _isSwimming = false;
            _smoothedSwimSpeed = 0f;

            if (_landController != null)
            {
                _animator.runtimeAnimatorController = _landController;
            }

            _animator.SetBool(IsSwimmingHash, false);
            _animator.SetFloat(SwimSpeedHash, 0f);

            if (_locomotion != null)
            {
                _locomotion.enabled = true;
            }
        }

        void SwimMovement()
        {
            Vector2 moveInput = _input != null ? _input.move : Vector2.zero;
            bool sprint = _input != null && _input.sprint;
            float moveSpeed = sprint ? FastSwimSpeed : NormalSwimSpeed;

            Vector3 move = Vector3.zero;
            if (_cameraTransform != null && moveInput.sqrMagnitude > 0.01f)
            {
                Vector3 camForward = _cameraTransform.forward;
                camForward.y = 0f;
                camForward.Normalize();
                Vector3 camRight = _cameraTransform.right;
                camRight.y = 0f;
                camRight.Normalize();
                move = camForward * moveInput.y + camRight * moveInput.x;
                move = Vector3.ClampMagnitude(move, 1f) * moveSpeed;

                if (move.sqrMagnitude > 0.001f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(move.normalized);
                    transform.rotation = Quaternion.Slerp(
                        transform.rotation,
                        targetRot,
                        8f * Time.deltaTime);
                }
            }

            float targetY = _waterSurfaceY + SurfaceBob;
            float verticalDelta = (targetY - transform.position.y) * 4f;
            Vector3 velocity = move + Vector3.up * verticalDelta;
            _controller.Move(velocity * Time.deltaTime);

            float targetAnimSpeed = move.magnitude > 0.1f
                ? (sprint ? 1.35f : 0.55f)
                : 0.15f;
            _smoothedSwimSpeed = Mathf.MoveTowards(_smoothedSwimSpeed, targetAnimSpeed, Time.deltaTime * 3f);
            _animator.SetFloat(SwimSpeedHash, _smoothedSwimSpeed);
        }

        static RuntimeAnimatorController LoadSwimAnimatorController()
        {
#if UNITY_EDITOR
            return AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(SwimAnimatorPath);
#else
            GameObject swimPrefab = Resources.Load<GameObject>("Swimming Controller");
            if (swimPrefab == null)
            {
                return null;
            }

            var temp = Instantiate(swimPrefab);
            temp.hideFlags = HideFlags.HideAndDontSave;
            Animator sourceAnimator = temp.GetComponent<Animator>();
            RuntimeAnimatorController controller = sourceAnimator != null
                ? sourceAnimator.runtimeAnimatorController
                : null;
            Destroy(temp);
            return controller;
#endif
        }
    }
}
