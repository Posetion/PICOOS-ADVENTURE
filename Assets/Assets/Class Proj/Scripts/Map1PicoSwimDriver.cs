using System.Reflection;
using FS_Core;
using FS_Swimming;
using FS_ThirdPerson;
using StarterAssets;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Map1(Forest), Map2 (Cave), and Map3(Desert) only. Keeps StarterAssets locomotion on
/// land and drives Fantacode swim while in water. Self-disables outside those maps.
/// </summary>
[DisallowMultipleComponent]
public sealed class Map1PicoSwimDriver : MonoBehaviour
{
    const string SwimAnimatorPath =
        "Assets/Assets/ImportedAssets/Fantacode Studios/Swimming System/Animator/Swimming Controller.controller";

    [Header("Swim movement (matches FS SwimmingController defaults)")]
    [SerializeField] RuntimeAnimatorController swimAnimatorAsset;
    [SerializeField] public float normalSpeed = 2f;
    [SerializeField] public float fastSwimSpeed = 4f;
    [SerializeField] public float underWaterSpeed = 3f;
    [SerializeField] public float rotationSpeed = 100f;
    [SerializeField] public KeyCode moveDownKey = KeyCode.C;
    [SerializeField] public KeyCode moveUpKey = KeyCode.Space;

    [Header("Exit water transition")]
    [SerializeField] public float exitCoastDuration = 0.6f;
    [SerializeField] public float exitAnimatorCrossfade = 0.45f;
    [SerializeField] public float exitCapsuleBlendDuration = 0.75f;
    [SerializeField] public float exitMomentumRetain = 0.7f;
    [SerializeField] public float exitLiftSpeed = 0.85f;
    [SerializeField] public float exitMinCoastSpeed = 0.35f;

    [Header("Enter water behavior")]
    [SerializeField] public float enterSurfaceSnapDuration = 0.45f;
    [SerializeField] public float enterSurfaceRiseSpeed = 4.5f;

    ThirdPersonController _locomotion;
    StarterAssetsInputs _input;
    CharacterController _controller;
    Animator _animator;
    Swimming _swimming;
    EnvironmentScanner _scanner;
    Damagable _damagable;

    RuntimeAnimatorController _landAnimator;
    RuntimeAnimatorController _swimAnimator;

    Transform _cameraTransform;
    LayerMask _waterMask;
    Vector3 _moveDir;
    Vector3 _lastMoveDir;
    Vector3 _velocity;
    float _currentMoveSpeed;

    float _landCapsuleHeight;
    float _landCapsuleRadius;
    Vector3 _landCapsuleCenter;
    float _swimCapsuleHeight;
    float _swimCapsuleRadius;
    Vector3 _swimCapsuleCenter;
    Coroutine _exitTransitionRoutine;
    bool _isExitingWater;
    bool _exitAnimatorBlended;
    bool _surfaceSnapActive;
    float _surfaceSnapEndTime;
    Vector3 _exitMoveDir;
    float _exitMoveSpeed;
    MethodInfo _cameraRotationMethod;

    static readonly int LandIdleStateHash = Animator.StringToHash("Idle Walk Run Blend");
    static readonly int AnimSpeedHash = Animator.StringToHash("Speed");
    static readonly int AnimGroundedHash = Animator.StringToHash("Grounded");
    static readonly int AnimMotionSpeedHash = Animator.StringToHash("MotionSpeed");

    bool _swimMapActive;

    void Awake()
    {
        _locomotion = GetComponent<ThirdPersonController>();
        _input = GetComponent<StarterAssetsInputs>();
        _controller = GetComponent<CharacterController>();
        _animator = GetComponent<Animator>();
        _swimming = GetComponent<Swimming>();
        _scanner = GetComponent<EnvironmentScanner>();
        _damagable = GetComponent<Damagable>();
        _waterMask = LayerMask.GetMask("Water");
        _landAnimator = _animator != null ? _animator.runtimeAnimatorController : null;
        _swimAnimator = swimAnimatorAsset != null ? swimAnimatorAsset : LoadSwimAnimatorFallback();

        if (_controller != null)
        {
            _landCapsuleHeight = _controller.height;
            _landCapsuleRadius = _controller.radius;
            _landCapsuleCenter = _controller.center;
        }

        CacheCameraTransform();
    }

    void Start()
    {
        if (!Map1SceneGuard.IsMap1OrMap2OrMap3Scene(gameObject.scene))
        {
            enabled = false;
            return;
        }

        _swimMapActive = true;
        RegisterWaterListeners();
    }

    void RegisterWaterListeners()
    {
        if (_swimming == null)
        {
            return;
        }

        _swimming.OnEnterWater.RemoveListener(OnEnterWater);
        _swimming.OnExitWater.RemoveListener(OnExitWater);
        _swimming.OnEnterWater.AddListener(OnEnterWater);
        _swimming.OnExitWater.AddListener(OnExitWater);
    }

    bool IsSwimMapActive()
    {
        return _swimMapActive;
    }

    void CacheCameraTransform()
    {
        if (_locomotion != null && _locomotion.CinemachineCameraTarget != null)
        {
            _cameraTransform = _locomotion.CinemachineCameraTarget.transform;
            return;
        }

        if (_cameraTransform == null && Camera.main != null)
        {
            _cameraTransform = Camera.main.transform;
        }
    }

    void EnsureCameraRotationMethod()
    {
        if (_cameraRotationMethod != null || _locomotion == null)
        {
            return;
        }

        _cameraRotationMethod = typeof(ThirdPersonController).GetMethod(
            "CameraRotation",
            BindingFlags.NonPublic | BindingFlags.Instance);
    }

    void OnEnable()
    {
        if (!IsSwimMapActive() || _swimming == null)
        {
            return;
        }

        RegisterWaterListeners();
    }

    void OnDisable()
    {
        if (_swimming == null)
        {
            return;
        }

        _swimming.OnEnterWater.RemoveListener(OnEnterWater);
        _swimming.OnExitWater.RemoveListener(OnExitWater);
    }

    void Update()
    {
        if (!IsSwimMapActive() || _swimming == null || _damagable == null)
        {
            return;
        }

        _swimming.IsGrounded = _locomotion != null && _locomotion.Grounded;

        if (!_swimming.IsSwimming || _swimming.InAction || _isExitingWater)
        {
            return;
        }

        CacheSwimCapsuleDimensions();
        HandleSwim();
    }

    void LateUpdate()
    {
        if (!IsSwimMapActive())
        {
            return;
        }

        // ThirdPersonController is disabled during swim so land movement stops,
        // but its CameraRotation() must keep running for mouse look.
        if (ShouldDriveCameraLook())
        {
            DriveCameraLook();
        }
    }

    bool ShouldDriveCameraLook()
    {
        if (_locomotion == null || _swimming == null)
        {
            return false;
        }

        return _swimming.IsSwimming || _isExitingWater;
    }

    void DriveCameraLook()
    {
        EnsureCameraRotationMethod();

        if (_cameraRotationMethod != null)
        {
            _cameraRotationMethod.Invoke(_locomotion, null);
            return;
        }

        ApplyFallbackCameraLook();
    }

    void ApplyFallbackCameraLook()
    {
        if (_input == null || _locomotion == null || _locomotion.CinemachineCameraTarget == null)
        {
            return;
        }

        const float threshold = 0.01f;
        if (_input.look.sqrMagnitude < threshold || _locomotion.LockCameraPosition)
        {
            return;
        }

        Transform cameraTarget = _locomotion.CinemachineCameraTarget.transform;
        Vector3 euler = cameraTarget.rotation.eulerAngles;
        float pitch = euler.x;
        if (pitch > 180f)
        {
            pitch -= 360f;
        }

        pitch += _input.look.y;
        pitch = Mathf.Clamp(pitch, _locomotion.BottomClamp, _locomotion.TopClamp);

        float yaw = euler.y + _input.look.x;
        cameraTarget.rotation = Quaternion.Euler(
            pitch + _locomotion.CameraAngleOverride,
            yaw,
            0f);
    }

    void CacheSwimCapsuleDimensions()
    {
        if (_controller == null)
        {
            return;
        }

        _swimCapsuleHeight = _controller.height;
        _swimCapsuleRadius = _controller.radius;
        _swimCapsuleCenter = _controller.center;
    }

    void OnEnterWater()
    {
        if (_exitTransitionRoutine != null)
        {
            StopCoroutine(_exitTransitionRoutine);
            _exitTransitionRoutine = null;
        }

        _isExitingWater = false;
        _exitAnimatorBlended = false;
        _surfaceSnapActive = true;
        _surfaceSnapEndTime = Time.time + enterSurfaceSnapDuration;
        _velocity = Vector3.zero;
        _currentMoveSpeed = 0f;

        if (_locomotion != null)
        {
            _locomotion.enabled = false;
        }

        if (_swimAnimator != null && _animator != null)
        {
            _animator.runtimeAnimatorController = _swimAnimator;
            _animator.SetBool(AnimatorParameters.IsSwimming, true);
            _animator.CrossFadeInFixedTime(AnimationNames.SwimmingAction, 0.2f);
        }

        _lastMoveDir = _moveDir = transform.forward;
    }

    void OnExitWater()
    {
        if (!IsSwimMapActive())
        {
            return;
        }

        if (_exitTransitionRoutine != null)
        {
            StopCoroutine(_exitTransitionRoutine);
        }

        _surfaceSnapActive = false;
        CacheSwimCapsuleDimensions();
        RestoreSwimCapsuleDimensions();

        _exitTransitionRoutine = StartCoroutine(SmoothExitWater());
    }

    void RestoreSwimCapsuleDimensions()
    {
        if (_controller == null)
        {
            return;
        }

        _controller.height = _swimCapsuleHeight;
        _controller.radius = _swimCapsuleRadius;
        _controller.center = _swimCapsuleCenter;
    }

    System.Collections.IEnumerator SmoothExitWater()
    {
        _isExitingWater = true;
        _exitAnimatorBlended = false;

        _exitMoveDir = _lastMoveDir.sqrMagnitude > 0.01f ? _lastMoveDir : transform.forward;
        _exitMoveDir.y = 0f;
        if (_exitMoveDir.sqrMagnitude > 0.01f)
        {
            _exitMoveDir.Normalize();
        }
        else
        {
            _exitMoveDir = transform.forward;
        }

        float capturedSpeed = Mathf.Max(_swimming.MoveAmount, normalSpeed * exitMinCoastSpeed);
        _exitMoveSpeed = capturedSpeed * exitMomentumRetain;

        if (_input != null)
        {
            _input.jump = false;
        }

        // Swimming.ExitFromSwim may resize the capsule — restore swim dimensions before blending up.
        if (_controller != null)
        {
            _controller.height = _swimCapsuleHeight;
            _controller.radius = _swimCapsuleRadius;
            _controller.center = _swimCapsuleCenter;
        }

        float animatorBlendStart = exitCoastDuration * 0.75f;
        float totalDuration = exitCoastDuration + exitAnimatorCrossfade + 0.1f;
        float elapsed = 0f;

        while (elapsed < totalDuration)
        {
            elapsed += Time.deltaTime;

            float coastT = exitCoastDuration > 0f
                ? Mathf.Clamp01(elapsed / exitCoastDuration)
                : 1f;
            float coastEase = Mathf.SmoothStep(0f, 1f, coastT);
            float currentSpeed = Mathf.Lerp(_exitMoveSpeed, 0f, coastEase);
            _swimming.MoveAmount = currentSpeed;

            if (_controller != null)
            {
                Vector3 move = _exitMoveDir * currentSpeed * Time.deltaTime;
                move += Vector3.up * (exitLiftSpeed * (1f - coastEase) * Time.deltaTime);
                _controller.Move(move);
            }

            if (!_exitAnimatorBlended && _animator != null && _animator.runtimeAnimatorController == _swimAnimator)
            {
                float targetAnimSpeed = currentSpeed > 0.01f
                    ? Mathf.Clamp01(currentSpeed / normalSpeed) * 0.45f
                    : 0f;
                float swimSpeed = _animator.GetFloat(AnimatorParameters.SwimSpeed);
                _animator.SetFloat(
                    AnimatorParameters.SwimSpeed,
                    Mathf.MoveTowards(swimSpeed, targetAnimSpeed, Time.deltaTime * 3.5f));
            }

            if (!_exitAnimatorBlended && elapsed >= animatorBlendStart)
            {
                BlendToLandAnimator(currentSpeed);
                _exitAnimatorBlended = true;
            }

            float capsuleT = exitCapsuleBlendDuration > 0f
                ? Mathf.Clamp01(elapsed / exitCapsuleBlendDuration)
                : 1f;
            LerpCapsuleTowardsLand(capsuleT);

            yield return null;
        }

        _swimming.MoveAmount = 0f;
        ApplyLandCapsuleDimensions();
        ResetLocomotionForWaterExit();

        if (_locomotion != null)
        {
            _locomotion.enabled = true;
        }

        _isExitingWater = false;
        _exitAnimatorBlended = false;
        _exitTransitionRoutine = null;
    }

    void BlendToLandAnimator(float coastSpeed)
    {
        if (_animator == null || _landAnimator == null)
        {
            return;
        }

        float landSpeed = Mathf.Clamp(coastSpeed * 0.35f, 0f, _locomotion != null ? _locomotion.MoveSpeed * 0.4f : 0.5f);

        _animator.runtimeAnimatorController = _landAnimator;
        _animator.SetBool(AnimatorParameters.IsSwimming, false);
        _animator.SetBool(AnimGroundedHash, true);
        _animator.SetFloat(AnimSpeedHash, landSpeed);
        _animator.SetFloat(AnimMotionSpeedHash, landSpeed > 0.01f ? 0.5f : 0f);
        _animator.CrossFadeInFixedTime(LandIdleStateHash, exitAnimatorCrossfade, 0, 0f);
    }

    void LerpCapsuleTowardsLand(float t)
    {
        if (_controller == null)
        {
            return;
        }

        float smoothT = Mathf.SmoothStep(0f, 1f, t);
        _controller.height = Mathf.Lerp(_swimCapsuleHeight, _landCapsuleHeight, smoothT);
        _controller.radius = Mathf.Lerp(_swimCapsuleRadius, _landCapsuleRadius, smoothT);
        _controller.center = Vector3.Lerp(_swimCapsuleCenter, _landCapsuleCenter, smoothT);
    }

    void ApplyLandCapsuleDimensions()
    {
        if (_controller == null)
        {
            return;
        }

        _controller.height = _landCapsuleHeight;
        _controller.radius = _landCapsuleRadius;
        _controller.center = _landCapsuleCenter;
    }

    void ResetLocomotionForWaterExit()
    {
        if (_locomotion == null)
        {
            return;
        }

        SetPrivateField(_locomotion, "_verticalVelocity", -2f);
        SetPrivateField(_locomotion, "_animationBlend", 0f);
        SetPrivateField(_locomotion, "_speed", 0f);
        SetPrivateField(_locomotion, "_jumpCount", 0);
        SetPrivateField(_locomotion, "_fallTimeoutDelta", 0.15f);
        _locomotion.Grounded = true;
    }

    static void SetPrivateField<T>(object instance, string fieldName, T value)
    {
        var field = instance.GetType().GetField(
            fieldName,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(instance, value);
    }

    void HandleSwim()
    {
        Vector2 moveInput = _input != null ? _input.move : Vector2.zero;
        bool sprint = _input != null && _input.sprint;
        float inputX = moveInput.x;
        float inputY = moveInput.y;

        if (Input.GetKey(moveUpKey) && _swimming.HeadUnderwaterLastFrame)
        {
            inputY = 1f;
        }

        Vector3 inputVector = new Vector3(inputX, 0f, inputY);
        if (inputVector != Vector3.zero && _cameraTransform != null)
        {
            Vector3 flatCamForward = _cameraTransform.forward;
            flatCamForward.y = 0f;
            flatCamForward.Normalize();
            _moveDir = Quaternion.LookRotation(flatCamForward) * inputVector.normalized;
        }

        if (_lastMoveDir == Vector3.zero)
        {
            _lastMoveDir = transform.forward;
        }

        _swimming.MoveAmount = Mathf.MoveTowards(
            _swimming.MoveAmount,
            inputVector.magnitude * GetMoveSpeed(sprint),
            Time.deltaTime * 5f);

        bool headUnderWater = Physics.CheckSphere(
            transform.position + Vector3.up * _swimming.SwimOffset,
            0.1f,
            _waterMask,
            QueryTriggerInteraction.Collide);

        if (_surfaceSnapActive)
        {
            bool snapExpired = Time.time >= _surfaceSnapEndTime;
            if (!headUnderWater || snapExpired)
            {
                _surfaceSnapActive = false;
            }
            else
            {
                _swimming.MoveAmount = 0f;
                _velocity = Vector3.zero;
                _controller.Move(Vector3.up * enterSurfaceRiseSpeed * Time.deltaTime);
                _swimming.UpdateSwimmingState();
                return;
            }
        }

        float animSwimSpeed = _animator.GetFloat(AnimatorParameters.SwimSpeed);
        if (!headUnderWater || inputVector == Vector3.zero)
        {
            float targetSpeed = sprint
                ? (_swimming.MoveAmount / fastSwimSpeed) * 1.5f
                : (_swimming.MoveAmount / normalSpeed) * 0.5f;
            float smoothSpeed = Mathf.MoveTowards(animSwimSpeed, targetSpeed, Time.deltaTime * 40f);
            _animator.SetFloat(AnimatorParameters.SwimSpeed, smoothSpeed, 0.2f, Time.deltaTime);
        }
        else
        {
            _swimming.MoveAmount = Mathf.MoveTowards(
                _swimming.MoveAmount,
                inputVector.magnitude * underWaterSpeed,
                Time.deltaTime * 5f);
            float smoothSpeed = Mathf.MoveTowards(animSwimSpeed, 1f, Time.deltaTime * 40f);
            _animator.SetFloat(AnimatorParameters.SwimSpeed, smoothSpeed, 0.2f, Time.deltaTime);
        }

        if (inputVector != Vector3.zero && _cameraTransform != null)
        {
            Vector3 camRelativeDir = (
                inputX * _cameraTransform.right +
                inputY * _cameraTransform.forward).normalized;

            if (headUnderWater)
            {
                _moveDir = camRelativeDir;
            }

            if (Input.GetKey(moveDownKey))
            {
                _moveDir = new Vector3(camRelativeDir.x, -1f, camRelativeDir.z);
            }
            else if (Input.GetKey(moveUpKey))
            {
                _moveDir = new Vector3(camRelativeDir.x, 1f, camRelativeDir.z);
            }
        }

        if (inputVector == Vector3.zero)
        {
            _moveDir = new Vector3(_lastMoveDir.x, 0f, _lastMoveDir.z);
        }

        if (headUnderWater && inputVector == Vector3.zero)
        {
            _velocity = Vector3.zero;
            _controller.Move(Vector3.up * 3f * Time.deltaTime);
        }

        ApplyMovementAndRotation();

        if (_input != null && _input.jump && _scanner != null)
        {
            TryClimbOutOfWater();
        }

        _swimming.UpdateSwimmingState();
    }

    void ApplyMovementAndRotation()
    {
        _moveDir = Vector3.Slerp(_lastMoveDir, _moveDir, Time.deltaTime);
        _velocity = _moveDir * _swimming.MoveAmount * Time.deltaTime;

        Vector3 predictedPos = transform.position + _moveDir * _swimming.MoveAmount * Time.deltaTime;
        bool predictedInWater = Physics.CheckSphere(
            predictedPos + Vector3.up * _swimming.SwimOffset,
            0.2f,
            _waterMask,
            QueryTriggerInteraction.Collide);

        if ((!predictedInWater && _moveDir.y > 0f) || _moveDir.y < 0f)
        {
            _moveDir.y = 0f;
            _velocity = _moveDir * _swimming.MoveAmount * Time.deltaTime;
        }

        if (_moveDir != Vector3.zero)
        {
            _lastMoveDir = _moveDir;
            transform.localRotation = Quaternion.RotateTowards(
                transform.localRotation,
                Quaternion.LookRotation(_lastMoveDir),
                rotationSpeed * Time.deltaTime);
        }

        if (!_swimming.IsInInitailVelocity)
        {
            _controller.Move(_velocity);
        }
    }

    void TryClimbOutOfWater()
    {
        ObstacleHitData hitData = _scanner.ObstacleCheck();
        if (_swimming.HeadUnderwaterLastFrame
            || !hitData.forwardHitFound
            || !hitData.heightHitFound
            || !hitData.hasSpace)
        {
            return;
        }

        if (Vector3.Angle(Vector3.up, hitData.forwardHit.normal) <= 45f
            || Vector3.Angle(Vector3.up, hitData.heightHit.normal) > 45f)
        {
            return;
        }

        StartCoroutine(_swimming.ClimbUpFromWater(hitData));
    }

    float GetMoveSpeed(bool sprint)
    {
        float target = sprint ? fastSwimSpeed : normalSpeed;
        _currentMoveSpeed = Mathf.MoveTowards(_currentMoveSpeed, target, Time.deltaTime * 2f);
        return _currentMoveSpeed;
    }

    static RuntimeAnimatorController LoadSwimAnimatorFallback()
    {
#if UNITY_EDITOR
        RuntimeAnimatorController controller =
            AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(SwimAnimatorPath);
        if (controller != null)
        {
            return controller;
        }
#endif
        GameObject template = Resources.Load<GameObject>("Swimming Controller");
        if (template == null)
        {
            return null;
        }

        Animator source = template.GetComponentInChildren<Animator>();
        return source != null ? source.runtimeAnimatorController : null;
    }
}
