using System.Collections;
using BasicTutorialMessage;
using StarterAssets;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
#if UNITY_EDITOR
using UnityEditor;
#endif

[DefaultExecutionOrder(-50)]
public class PicoTutoMapTutorial : MonoBehaviour
{
    private const string TutorialPrefabPath = "Assets/BasicTutorialMessage/Prefab/TutorialMessage.prefab";
    private const string WrapperChildName = "Wrapper";

    [SerializeField] private GameObject tutorialMessagePrefab;
    [SerializeField] private RectTransform tutorialCanvas;

    [Header("Tutorial Targets")]
    [SerializeField] private Transform collectibleTarget;
    [SerializeField] private Transform timerTarget;
    [SerializeField] private Transform speedUpTarget;
    [SerializeField] private Transform slowDownTarget;
    [SerializeField] private Transform freezeTarget;

    [Header("Arrow Settings")]
    [SerializeField] private float bufferOffset = 28f;
    [SerializeField] private float screenMargin = 80f;

    [Header("Camera Timing")]
    [SerializeField] private float cameraFocusDuration = 1.1f;
    [SerializeField] private float focusPauseBeforePopup = 0.2f;
    [SerializeField] private float startDelay = 0.25f;

    private struct TutorialCameraProfile
    {
        public float Fov;
        public float Distance;
        public float Height;
        public float SideOffset;
        public float AimHeight;
        public bool UseBounds;
        public float BoundsPadding;
        public bool FlatZoneAim;
    }

    private Camera mainCamera;
    private MonoBehaviour cinemachineBrain;
    private MonoBehaviour followVirtualCamera;
    private float defaultFov;
    private bool cameraOverrideActive;
    private ThirdPersonController playerController;
    private StarterAssetsInputs playerInputs;
#if ENABLE_INPUT_SYSTEM
    private PlayerInput playerInput;
#endif
    private bool playerLocked;
    private int tutorialStep;

    private void Awake()
    {
        DisableExampleTutorialRunners();
        tutorialMessagePrefab = ResolveTutorialPrefab();
    }

    private IEnumerator Start()
    {
        if (tutorialMessagePrefab == null)
        {
            Debug.LogError("PicoTutoMapTutorial: TutorialMessage prefab is missing.");
            yield break;
        }

        if (tutorialCanvas == null)
        {
            var canvasObject = GameObject.Find("Tuto Map Canvas");
            if (canvasObject != null)
                tutorialCanvas = canvasObject.GetComponent<RectTransform>();
        }

        if (tutorialCanvas == null)
        {
            Debug.LogError("PicoTutoMapTutorial: Tuto Map Canvas not found.");
            yield break;
        }

        ResolveTargets();

        if (!HasAnyTarget())
        {
            Debug.LogError("PicoTutoMapTutorial: No tutorial targets assigned or found.");
            yield break;
        }

        if (startDelay > 0f)
            yield return new WaitForSeconds(startDelay);

        while (Camera.main == null)
            yield return null;

        mainCamera = Camera.main;
        CacheCameraRig();
        LockPlayer();
        StartCoroutine(ShowNextTutorialRoutine());
    }

    private void CacheCameraRig()
    {
        if (mainCamera == null)
            return;

        defaultFov = mainCamera.fieldOfView;
        cinemachineBrain = FindBehaviourByTypeName(mainCamera.gameObject, "CinemachineBrain");

        var followCameraObject = GameObject.Find("PlayerFollowCamera");
        if (followCameraObject != null)
            followVirtualCamera = FindBehaviourByTypeName(followCameraObject, "CinemachineCamera");
    }

    private static MonoBehaviour FindBehaviourByTypeName(GameObject gameObject, string typeName)
    {
        foreach (var behaviour in gameObject.GetComponents<MonoBehaviour>())
        {
            if (behaviour != null && behaviour.GetType().Name == typeName)
                return behaviour;
        }

        return null;
    }

    private static void DisableExampleTutorialRunners()
    {
        foreach (var example in FindObjectsByType<BasicTutorialMessage.Example.ExampleUse>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            example.enabled = false;
    }

    private GameObject ResolveTutorialPrefab()
    {
        if (tutorialMessagePrefab != null)
            return tutorialMessagePrefab;

#if UNITY_EDITOR
        return AssetDatabase.LoadAssetAtPath<GameObject>(TutorialPrefabPath);
#else
        return null;
#endif
    }

    private void ResolveTargets()
    {
        if (collectibleTarget == null)
            collectibleTarget = FindTarget("RowofCollectables", "RowofCollectables (1)", "Collectible");
        if (timerTarget == null)
            timerTarget = FindTarget("TimeRD Variant", "Timer");
        if (speedUpTarget == null)
            speedUpTarget = FindTarget("SpeedUp");
        if (slowDownTarget == null)
            slowDownTarget = FindTarget("SlowDown");
        if (freezeTarget == null)
            freezeTarget = FindTarget("Freeze");

        speedUpTarget = ResolveSpeedUpFocusTransform(speedUpTarget);
        slowDownTarget = ResolveZoneFocusTransform(slowDownTarget);
        freezeTarget = ResolveZoneFocusTransform(freezeTarget);
    }

    private static Transform ResolveSpeedUpFocusTransform(Transform root)
    {
        if (root == null)
            return null;

        foreach (var child in root.GetComponentsInChildren<Transform>())
        {
            if (child == root)
                continue;

            if (child.name.Contains("SpeedChev") || child.name.Contains("Chev"))
                return child;
        }

        return ResolveZoneFocusTransform(root);
    }

    private static Transform ResolveZoneFocusTransform(Transform root)
    {
        if (root == null)
            return null;

        if (root.GetComponent<Collider>() != null || root.GetComponent<Renderer>() != null)
            return root;

        Transform farthestChild = root;
        var farthestDistance = 0f;

        foreach (var renderer in root.GetComponentsInChildren<Renderer>())
        {
            var distance = (renderer.bounds.center - root.position).sqrMagnitude;
            if (distance > farthestDistance)
            {
                farthestDistance = distance;
                farthestChild = renderer.transform;
            }
        }

        if (farthestDistance > 0.01f)
            return farthestChild;

        foreach (var collider in root.GetComponentsInChildren<Collider>())
        {
            var distance = (collider.bounds.center - root.position).sqrMagnitude;
            if (distance > farthestDistance)
            {
                farthestDistance = distance;
                farthestChild = collider.transform;
            }
        }

        return farthestChild;
    }

    private bool HasAnyTarget()
    {
        return collectibleTarget != null
            || timerTarget != null
            || speedUpTarget != null
            || slowDownTarget != null
            || freezeTarget != null;
    }

    private IEnumerator ShowNextTutorialRoutine()
    {
        while (tutorialStep < 5)
        {
            Transform target = null;
            string text = null;
            TutorialCameraProfile cameraProfile = default;

            switch (tutorialStep)
            {
                case 0:
                    target = collectibleTarget;
                    text = "Collect these to win the game.";
                    cameraProfile = GetCameraProfileForStep(0);
                    break;
                case 1:
                    target = timerTarget;
                    text = "Adds extra time to your timer.";
                    cameraProfile = GetCameraProfileForStep(1);
                    break;
                case 2:
                    target = speedUpTarget;
                    text = "Increases movement speed temporarily.";
                    cameraProfile = GetCameraProfileForStep(2);
                    break;
                case 3:
                    target = slowDownTarget;
                    text = "Slows you down and reduces time.";
                    cameraProfile = GetCameraProfileForStep(3);
                    break;
                case 4:
                    target = freezeTarget;
                    text = "Freezes you temporarily.";
                    cameraProfile = GetCameraProfileForStep(4);
                    break;
            }

            tutorialStep++;

            if (target == null)
                continue;

            FacePlayerTowardTarget(target);
            yield return FocusCameraOnTargetRoutine(target, cameraProfile);

            if (focusPauseBeforePopup > 0f)
                yield return new WaitForSeconds(focusPauseBeforePopup);

            var message = SpawnTutorial(text, target, cameraProfile);
            if (message == null)
                continue;

            message.OnMessageClosed += (_, _) => StartCoroutine(ShowNextTutorialRoutine());
            yield break;
        }

        UnlockPlayer();
        RestoreGameplayCamera();
    }

    private static TutorialCameraProfile GetCameraProfileForStep(int step)
    {
        return step switch
        {
            // Row of crystals — wider shot to show the full row
            0 => new TutorialCameraProfile
            {
                Fov = 30f,
                Distance = 4f,
                Height = 2.2f,
                SideOffset = 1.15f,
                AimHeight = 1f,
                UseBounds = true,
                BoundsPadding = 0.85f
            },
            // Timer pickup — medium hero shot (skip bounds; clock VFX skews framing)
            1 => new TutorialCameraProfile
            {
                Fov = 28f,
                Distance = 3.5f,
                Height = 1.9f,
                SideOffset = 1f,
                AimHeight = 1.1f,
                UseBounds = false,
                BoundsPadding = 0f
            },
            // SpeedUp pad — frame the chevron visual; straight-on with slight height
            2 => new TutorialCameraProfile
            {
                Fov = 36f,
                Distance = 5.6f,
                Height = 4.1f,
                SideOffset = 0f,
                AimHeight = 0.8f,
                UseBounds = true,
                BoundsPadding = 0.65f,
                FlatZoneAim = true
            },
            // SlowDown pad — slight left offset so it doesn't mirror SpeedUp exactly
            3 => new TutorialCameraProfile
            {
                Fov = 38f,
                Distance = 5.2f,
                Height = 3.7f,
                SideOffset = -0.55f,
                AimHeight = 0.4f,
                UseBounds = true,
                BoundsPadding = 1f
            },
            // Freeze pad — slight right offset
            4 => new TutorialCameraProfile
            {
                Fov = 38f,
                Distance = 5.2f,
                Height = 3.7f,
                SideOffset = 0.55f,
                AimHeight = 0.4f,
                UseBounds = true,
                BoundsPadding = 1f
            },
            _ => default
        };
    }

    private static void FacePlayerTowardTarget(Transform target)
    {
        var player = GameObject.FindWithTag("Player");
        if (player == null || target == null)
            return;

        var direction = target.position - player.transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.01f)
            return;

        player.transform.rotation = Quaternion.LookRotation(direction.normalized);
    }

    private IEnumerator FocusCameraOnTargetRoutine(Transform target, TutorialCameraProfile profile)
    {
        if (mainCamera == null)
            yield break;

        EnableCameraOverride();

        var focusPoint = GetAimPoint(target, profile);
        var targetPosition = GetTutorialCameraPosition(focusPoint, target, profile);
        var targetRotation = Quaternion.LookRotation(focusPoint - targetPosition, Vector3.up);

        var startPosition = mainCamera.transform.position;
        var startRotation = mainCamera.transform.rotation;
        var startFov = mainCamera.fieldOfView;

        var elapsed = 0f;
        while (elapsed < cameraFocusDuration)
        {
            elapsed += Time.deltaTime;
            var t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / cameraFocusDuration));

            mainCamera.transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            mainCamera.transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
            mainCamera.fieldOfView = Mathf.Lerp(startFov, profile.Fov, t);
            yield return null;
        }

        mainCamera.transform.position = targetPosition;
        mainCamera.transform.rotation = targetRotation;
        mainCamera.fieldOfView = profile.Fov;
    }

    private Vector3 GetTutorialCameraPosition(Vector3 focusPoint, Transform target, TutorialCameraProfile profile)
    {
        var viewDirection = GetPresentationDirection(focusPoint);
        var distance = GetAdjustedDistance(target, profile);
        var sideOffset = Vector3.Cross(Vector3.up, viewDirection).normalized * profile.SideOffset;
        return focusPoint
            - viewDirection * distance
            + Vector3.up * profile.Height
            + sideOffset;
    }

    private Vector3 GetPresentationDirection(Vector3 focusPoint)
    {
        var player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            var fromPlayer = focusPoint - player.transform.position;
            fromPlayer.y = 0f;
            if (fromPlayer.sqrMagnitude > 0.25f)
                return fromPlayer.normalized;
        }

        var fromCamera = focusPoint - mainCamera.transform.position;
        fromCamera.y = 0f;
        if (fromCamera.sqrMagnitude > 0.01f)
            return fromCamera.normalized;

        return mainCamera.transform.forward;
    }

    private static float GetAdjustedDistance(Transform target, TutorialCameraProfile profile)
    {
        if (!profile.UseBounds || !TryGetWorldBounds(target, out var bounds))
            return profile.Distance;

        var horizontalSpan = Mathf.Max(bounds.extents.x, bounds.extents.z);
        return profile.Distance + horizontalSpan * profile.BoundsPadding;
    }

    private void EnableCameraOverride()
    {
        if (cameraOverrideActive)
            return;

        if (cinemachineBrain != null)
            cinemachineBrain.enabled = false;
        if (followVirtualCamera != null)
            followVirtualCamera.enabled = false;

        cameraOverrideActive = true;
    }

    private void RestoreGameplayCamera()
    {
        if (!cameraOverrideActive)
            return;

        if (followVirtualCamera != null)
            followVirtualCamera.enabled = true;
        if (cinemachineBrain != null)
            cinemachineBrain.enabled = true;

        if (mainCamera != null && defaultFov > 0f)
            mainCamera.fieldOfView = defaultFov;

        cameraOverrideActive = false;
    }

    private TutorialMessage SpawnTutorial(string text, Transform target, TutorialCameraProfile profile)
    {
        var aimPoint = GetAimPoint(target, profile);
        var direction = PickOnScreenDirection(aimPoint);
        var screenPoint = mainCamera.WorldToScreenPoint(aimPoint);
        screenPoint.x = Mathf.Clamp(screenPoint.x, screenMargin, Screen.width - screenMargin);
        screenPoint.y = Mathf.Clamp(screenPoint.y, screenMargin, Screen.height - screenMargin);
        return SpawnTutorial(text, screenPoint, direction, bufferOffset);
    }

    private TutorialMessage SpawnTutorial(string text, Vector3 screenPos, TutorialLocation location, float buffer)
    {
        var instanceObject = Instantiate(tutorialMessagePrefab);
        if (instanceObject is not GameObject instance)
        {
            Debug.LogError($"PicoTutoMapTutorial: Failed to spawn tutorial popup ({instanceObject?.GetType().Name ?? "null"}).");
            return null;
        }

        instance.transform.SetParent(tutorialCanvas, false);

        var message = instance.GetComponent<TutorialMessage>();
        if (message == null)
        {
            Debug.LogError("PicoTutoMapTutorial: TutorialMessage prefab is missing the TutorialMessage component.");
            Destroy(instance);
            return null;
        }

        message.InitializeMessageWithScreenPos(text, tutorialCanvas, screenPos, location, buffer);
        ClampTutorialOnScreen(instance.transform);
        return message;
    }

    private TutorialLocation PickOnScreenDirection(Vector3 worldPos)
    {
        var viewport = mainCamera.WorldToViewportPoint(worldPos);

        if (viewport.z < 0f)
            return TutorialLocation.POINT_TO_TOP;

        if (viewport.x > 0.62f)
            return TutorialLocation.POINT_TO_RIGHT;

        if (viewport.x < 0.38f)
            return TutorialLocation.POINT_TO_LEFT;

        if (viewport.y > 0.55f)
            return TutorialLocation.POINT_TO_TOP;

        return TutorialLocation.POINT_TO_BOTTOM;
    }

    private void ClampTutorialOnScreen(Transform tutorialRoot)
    {
        var wrapper = tutorialRoot.Find(WrapperChildName) as RectTransform;
        if (wrapper == null)
            return;

        Canvas.ForceUpdateCanvases();

        var canvasRect = tutorialCanvas.rect;
        var halfCanvasWidth = canvasRect.width * 0.5f;
        var halfCanvasHeight = canvasRect.height * 0.5f;
        var halfPopupWidth = wrapper.rect.width * 0.5f;
        var halfPopupHeight = wrapper.rect.height * 0.5f;

        var pos = wrapper.anchoredPosition;
        pos.x = Mathf.Clamp(
            pos.x,
            -halfCanvasWidth + halfPopupWidth + screenMargin,
            halfCanvasWidth - halfPopupWidth - screenMargin);
        pos.y = Mathf.Clamp(
            pos.y,
            -halfCanvasHeight + halfPopupHeight + screenMargin,
            halfCanvasHeight - halfPopupHeight - screenMargin);
        wrapper.anchoredPosition = pos;
    }

    private static Vector3 GetAimPoint(Transform target, TutorialCameraProfile profile)
    {
        if (profile.UseBounds && TryGetWorldBounds(target, out var bounds))
        {
            if (profile.FlatZoneAim)
            {
                return new Vector3(
                    bounds.center.x,
                    bounds.max.y - bounds.extents.y * 0.15f,
                    bounds.center.z);
            }

            return new Vector3(bounds.center.x, bounds.center.y + bounds.extents.y * 0.4f, bounds.center.z);
        }

        return target.position + Vector3.up * profile.AimHeight;
    }

    private static bool TryGetWorldBounds(Transform target, out Bounds bounds)
    {
        var hasBounds = false;
        bounds = new Bounds(target.position, Vector3.zero);

        foreach (var renderer in target.GetComponentsInChildren<Renderer>())
        {
            if (renderer is ParticleSystemRenderer)
                continue;

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        if (hasBounds)
            return true;

        foreach (var collider in target.GetComponentsInChildren<Collider>())
        {
            if (!hasBounds)
            {
                bounds = collider.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(collider.bounds);
            }
        }

        return hasBounds;
    }

    private void LockPlayer()
    {
        if (playerLocked)
            return;

        var player = GameObject.FindWithTag("Player");
        if (player == null)
            return;

        playerController = player.GetComponent<ThirdPersonController>();
        playerInputs = player.GetComponent<StarterAssetsInputs>();
#if ENABLE_INPUT_SYSTEM
        playerInput = player.GetComponent<PlayerInput>();
#endif

        if (playerController != null)
            playerController.enabled = false;
#if ENABLE_INPUT_SYSTEM
        if (playerInput != null)
            playerInput.enabled = false;
#endif

        if (playerInputs != null)
        {
            playerInputs.move = Vector2.zero;
            playerInputs.look = Vector2.zero;
            playerInputs.jump = false;
            playerInputs.sprint = false;
            playerInputs.cursorLocked = false;
            playerInputs.cursorInputForLook = false;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        playerLocked = true;
    }

    private void UnlockPlayer()
    {
        if (!playerLocked)
            return;

        if (playerController != null)
            playerController.enabled = true;
#if ENABLE_INPUT_SYSTEM
        if (playerInput != null)
            playerInput.enabled = true;
#endif

        if (playerInputs != null)
        {
            playerInputs.cursorLocked = true;
            playerInputs.cursorInputForLook = true;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        playerLocked = false;
    }

    private static Transform FindTarget(params string[] objectNames)
    {
        foreach (var objectName in objectNames)
        {
            var target = GameObject.Find(objectName);
            if (target != null)
                return target.transform;
        }

        Debug.LogWarning($"PicoTutoMapTutorial: Could not find any of [{string.Join(", ", objectNames)}] in the scene.");
        return null;
    }
}
