using FS_Core;
using FS_ThirdPerson;
using FS_Util;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace FS_ThirdPerson
{
    public static partial class AnimatorParameters
    {
        public static int IsSwimming = Animator.StringToHash("IsSwimming");
        public static int SwimSpeed = Animator.StringToHash("SwimSpeed");
        public static int SwimIdle = Animator.StringToHash("SwimIdle");
        public static int DiveSpeed = Animator.StringToHash("DiveSpeed");
    }

    public static partial class AnimationNames
    {
        public static string SwimmingAction = "Swimming Action";
        public static string ClimbUpFromWater = "ClimbUpFromWater";
        public static string DiveStart = "Dive Start";
        public static string DiveLoop = "Dive Loop";
        public static string DiveLand = "Dive Land";
    }
}

namespace FS_Swimming
{
    public enum UnderwaterDamageLogic
    {
        None,
        Instant,
        Delayed,
    }

    public enum UnderWaterSwimType
    {
        None,
        Automatic,
        Manual
    }

    public class SwimmingController : SystemBase
    {
        [Header("General")]
        [SerializeField, Tooltip("Enables diving")]
        bool enableDive = true;

        [SerializeField, Tooltip("Enables downward movement to be controlled based on the camera's pitch angle while above water.")]
        bool movementBasedOnCameraAngle = false;

        [ShowIf("movementBasedOnCameraAngle", true)]
        [SerializeField, Tooltip("The camera angle threshold (in degrees) that determines when the character moves downward while above water.")]
        float cameraAngleThreshold = 60f;

        [SerializeField, Tooltip("Enables upward and downward movement to be controlled using input keys")]
        bool movementBasedOnKeys = true;

        [SerializeField, Tooltip("Keyboard key used to move downward.")]
        KeyCode moveDownKey = KeyCode.C;

        [SerializeField, Tooltip("Input button name used to move downward.")]
        string moveDownButton;

        [SerializeField, Tooltip("Keyboard key used to move upward.")]
        KeyCode moveUpKey = KeyCode.Space;

        [SerializeField, Tooltip("Input button name used to move upward.")]
        string moveUpButton;

        [Header("Swimming Movement Settings")]
        [SerializeField, Tooltip("Movement speed while swimming normally.")]
        float normalSpeed = 2f;

        [SerializeField, Tooltip("Movement speed while swimming in fast mode.")]
        float fastSwimSpeed = 4f;

        [SerializeField, Tooltip("Movement speed while swimming underwater.")]
        float underWaterSpeed = 3f;

        [SerializeField, Tooltip("Rotation speed while swimming.")]
        float rotationSpeed = 100f;

        [Header("Breath & Health Settings")]
        [SerializeField, Tooltip("Speed at which the breath value decreases while underwater.")]
        float breathDecreaseSpeed = 10f;

        [SerializeField, Tooltip("Speed at which the breath value restores when above water.")]
        float breathRestoreSpeed = 20f;

        [SerializeField, Tooltip("Speed at which the player's health decreases after breath reaches zero (used with Delayed damage logic).")]
        float healthDecreaseSpeed = 10f;

        [SerializeField, Tooltip(
            "Defines how damage is applied when the player runs out of breath underwater:\n" +
            "None    - No damage while underwater.\n" +
            "Instant - Player dies instantly when breath reaches zero.\n" +
            "Delayed - After breath reaches zero, health gradually decreases until it reaches zero, then the player dies.")]
        UnderwaterDamageLogic damageLogic = UnderwaterDamageLogic.Delayed;

        [Header("Effects")]
        [SerializeField, Tooltip("Visual effect applied when the player is underwater.")]
        GameObject underWaterEffectPrefab;


        [SerializeField, Tooltip("Looping audio clip that plays when the player is fully underwater.")]
        AudioClip underwaterLoopClip;

        // Runtime / internal
        LayerMask water;
        float currentMoveSpeed = 0f;
        Vector3 moveDir = Vector3.zero;
        Vector3 lastMoveDir = Vector3.zero;
        Vector3 velocity = Vector3.zero;


        GameObject underwaterEffectObject;
        AudioSource underWaterAudioSource;

        Animator animator;
        CharacterController characterController;
        PlayerController playerController;
        EnvironmentScanner environmentScanner;
        LocomotionInputManager inputManager;
        Damagable damagable;
        LocomotionICharacter player;
        Swimming swimming;

        public override SystemState State => SystemState.Swim;
        public override float Priority => -1;

#if inputsystem
        FSSystemsInputAction input;

        private void OnEnable()
        {
            input = new FSSystemsInputAction();
            input.Enable();
        }
#endif

        private void OnDisable()
        {
#if inputsystem
            input.Disable();
#endif
            if (swimming != null)
            {
                swimming.OnEnableRootMotion -= EnableRootMotion;
                swimming.OnResetRootMotion -= ResetRootMotion;
            }
        }

        bool MoveDownKeyHolding =>
#if inputsystem
            (input.Swim.MoveDownKey.IsInProgress());
#else
            (moveDownKey != KeyCode.None && Input.GetKey(moveDownKey)) ||
            (!string.IsNullOrEmpty(moveDownButton) && Input.GetButton(moveDownButton));
#endif
        bool MoveUpKeyHolding =>
#if inputsystem
           (input.Swim.MoveUpKey.IsInProgress());
#else
            (moveUpKey != KeyCode.None && Input.GetKey(moveUpKey)) ||
            (!string.IsNullOrEmpty(moveUpButton) && Input.GetButton(moveUpButton));
#endif


        private void Awake()
        {
            animator = GetComponent<Animator>();
            characterController = GetComponent<CharacterController>();
            playerController = GetComponent<PlayerController>();
            player = GetComponent<LocomotionICharacter>();
            inputManager = GetComponent<LocomotionInputManager>();
            environmentScanner = GetComponent<EnvironmentScanner>();
            swimming = GetComponent<Swimming>();
            damagable = GetComponent<Damagable>();

            water = LayerMask.GetMask("Water");

            if (underWaterEffectPrefab != null)
            {
                underwaterEffectObject = Instantiate(underWaterEffectPrefab, transform);
                underwaterEffectObject.SetActive(false);
            }

            if (underwaterLoopClip != null)
            {
                var soundObj = new GameObject("Underwater Sound Effect");
                soundObj.transform.SetParent(transform, false);
                underWaterAudioSource = soundObj.AddComponent<AudioSource>();
                underWaterAudioSource.clip = underwaterLoopClip;
                underWaterAudioSource.loop = true;
                underWaterAudioSource.playOnAwake = false;
                underWaterAudioSource.spatialBlend = 1;
                underWaterAudioSource.maxDistance = 20;
                underWaterAudioSource.minDistance = 1;
                underWaterAudioSource.Stop();
            }
        }

        private void Start()
        {
            if (swimming != null)
            {
                swimming.OnEnterWater.AddListener(() =>
                {
                    player.OnStartSystem(this, true);
                    playerController.IsInAir = false;
                    playerController.StopAlignCameraForward();
                    lastMoveDir = moveDir = transform.forward;
                });

                swimming.OnExitWater.AddListener(() =>
                {
                    player.OnEndSystem(this);
                    playerController.ResetAlignCameraForward();
                });

                swimming.OnInitialVelocity += (float entryHeight) =>
                {
                    playerController.OnStartCameraShake?.Invoke(Mathf.Min(.6f, entryHeight * .5f), .3f);
                };

                swimming.OnStartDive.AddListener(() =>
                {
                    player.OnStartSystem(this, true);
                });

                swimming.OnEnableRootMotion += EnableRootMotion;
                swimming.OnResetRootMotion += ResetRootMotion;
            }
        }

        private void Update()
        {
            if (underwaterEffectObject != null && playerController != null)
            {
                bool cameraHitsWater = Physics.CheckSphere(playerController.cameraGameObject.transform.position, .01f, water, QueryTriggerInteraction.Collide);
                underwaterEffectObject.SetActive(cameraHitsWater);
            }

            if (swimming != null && player != null)
                swimming.IsGrounded = player.CheckIsGrounded();

            if (damagable.IsDead)
            {
                if (animator != null)
                    animator.SetFloat(AnimatorParameters.SwimSpeed, 0);
                swimming.SetRagdollState(true);
            }
        }

        public override void HandleUpdate()
        {
            if (damagable.IsDead) return;

            HandleSwim();
        }

        public override void HandleFixedUpdate()
        {
            if (damagable.IsDead) return;

            HandleDive();
        }

        void HandleDive()
        {
            if (inputManager.JumpHold)
            {
                swimming.Dive();
            }
        }

        void HandleSwim()
        {
            var headUnderWater = false;

            if (IsInFocus && !swimming.InAction)
            {
                bool isInUpAndDownMovementByKey = false;
                // Input
                float inputX = inputManager.DirectionInput.x;
                float inputY = inputManager.DirectionInput.y;
                if (movementBasedOnKeys)
                {
                    if (MoveDownKeyHolding || MoveUpKeyHolding)
                    {
                        if (MoveUpKeyHolding && swimming.HeadUnderwaterLastFrame)
                        {
                            inputY = 1;
                            isInUpAndDownMovementByKey = true;
                        }
                    }
                }
                Vector3 inputVector = new Vector3(inputX, 0f, inputY);
                

                

                if (inputVector != Vector3.zero)
                    moveDir = playerController.CameraPlanarRotation * inputVector.normalized;

                if (lastMoveDir == Vector3.zero)
                    lastMoveDir = transform.forward;

                swimming.MoveAmount = Mathf.MoveTowards(swimming.MoveAmount, inputVector.magnitude * GetMoveSpeed(), Time.deltaTime * 5);

                // Flattened camera forward angle
                Vector3 flatForward = playerController.cameraGameObject.transform.forward;
                flatForward.y = 0f;
                float flatCameraAngle = Vector3.Angle(playerController.cameraGameObject.transform.forward, flatForward);

                // State checks
                headUnderWater = Physics.CheckSphere(transform.position + Vector3.up * swimming.SwimOffset, 0.1f, water, QueryTriggerInteraction.Collide);

                float animSwimSpeed = animator.GetFloat(AnimatorParameters.SwimSpeed);
                bool underwaterMovement = false;

                // Swim speed handling
                if (!headUnderWater || inputVector == Vector3.zero)
                {
                    float targetSpeed = inputManager.SprintKey
                        ? (swimming.MoveAmount / fastSwimSpeed) * 1.5f
                        : (swimming.MoveAmount / normalSpeed) * 0.5f;
                    var s = targetSpeed > animSwimSpeed ? 40f : 100f;
                    float smoothedSpeed = Mathf.MoveTowards(animSwimSpeed, targetSpeed, Time.deltaTime * s);
                    animator.SetFloat(AnimatorParameters.SwimSpeed, smoothedSpeed, 0.2f, Time.deltaTime);
                }
                else
                {
                    underwaterMovement = true;
                    swimming.MoveAmount = Mathf.MoveTowards(swimming.MoveAmount, inputVector.magnitude * underWaterSpeed, Time.deltaTime * 5);
                    float smoothedSpeed = Mathf.MoveTowards(animSwimSpeed, 1f, Time.deltaTime * 40f);
                    animator.SetFloat(AnimatorParameters.SwimSpeed, smoothedSpeed, 0.2f, Time.deltaTime);
                }
                
                // Direction correction
                if (inputVector != Vector3.zero)
                {
                    Vector3 camRelativeDir = (inputX * playerController.cameraGameObject.transform.right +
                                              inputY * playerController.cameraGameObject.transform.forward).normalized;

                    bool shouldApply = (movementBasedOnCameraAngle && flatCameraAngle > cameraAngleThreshold) || underwaterMovement;
                    if (shouldApply)
                        moveDir = camRelativeDir;

                    if (movementBasedOnKeys)
                    {
                        if (MoveDownKeyHolding)
                        {
                            moveDir = new Vector3(camRelativeDir.x, -1f, camRelativeDir.z);
                        }
                        else if (MoveUpKeyHolding)
                        {
                            moveDir = new Vector3(camRelativeDir.x, 1f, camRelativeDir.z);
                        }
                    }
                }

                // Preserve last move direction if idle
                if (inputVector == Vector3.zero)
                {
                    moveDir = new Vector3(lastMoveDir.x, 0f, lastMoveDir.z);
                }
                if (!movementBasedOnCameraAngle && !movementBasedOnKeys)
                {
                    if(headUnderWater)
                    {
                        velocity = Vector3.zero;
                        characterController.Move(Vector3.up * 3 * Time.deltaTime);
                    }
                }

                var moveDirLerpSpeed = isInUpAndDownMovementByKey ? 5f : 1f;
                HandleMovementAndRotation(moveDirLerpSpeed);

                ClimbUpFromWater();
                swimming.UpdateSwimmingState();

                // Verify if head is fully under water (used for effects & health)
                headUnderWater = Physics.CheckSphere(transform.position + Vector3.up * (swimming.SwimOffset + swimming.HeadOffset), 0.1f, water, QueryTriggerInteraction.Collide);
                UpdateHealth(headUnderWater);
            }

            if (!headUnderWater)
            {
                damagable.UpdateBreath(Time.deltaTime * breathRestoreSpeed);
            }
        }

        void HandleMovementAndRotation(float moveDirLerpSpeed = 1)
        {
            Vector3 predictedPos = transform.position + moveDir * swimming.MoveAmount * Time.deltaTime;

            moveDir = Vector3.Slerp(lastMoveDir, moveDir, Time.deltaTime * moveDirLerpSpeed);
            velocity = moveDir * swimming.MoveAmount * Time.deltaTime;

            // Water boundary check
            bool predictedPosInWater = Physics.CheckSphere(predictedPos + Vector3.up * swimming.SwimOffset, 0.2f, water, QueryTriggerInteraction.Collide);
            if ((!predictedPosInWater && moveDir.y > 0) || (!movementBasedOnKeys && !movementBasedOnCameraAngle && moveDir.y < 0))
            {
                moveDir.y = 0f;
                velocity = moveDir * swimming.MoveAmount * Time.deltaTime;
            }

            // Rotation
            if (moveDir != Vector3.zero)
            {
                lastMoveDir = moveDir;
                transform.localRotation = Quaternion.RotateTowards(
                    transform.localRotation,
                    Quaternion.LookRotation(lastMoveDir),
                    rotationSpeed * Time.deltaTime
                );
            }

            // Apply movement
            if (!swimming.IsInInitailVelocity)
                characterController.Move(velocity);
        }

        void UpdateHealth(bool headUnderWater)
        {
            if (!headUnderWater) return;

            switch (damageLogic)
            {
                case UnderwaterDamageLogic.None:
                    break;

                case UnderwaterDamageLogic.Instant:
                    damagable.UpdateBreath(Time.deltaTime * -breathDecreaseSpeed);
                    if (damagable.CurrentBreath <= 0)
                    {
                        damagable.TakeDamage(damagable.CurrentHealth, HitType.Any);
                    }
                    break;

                case UnderwaterDamageLogic.Delayed:
                    damagable.UpdateBreath(Time.deltaTime * -breathDecreaseSpeed);
                    if (damagable.CurrentBreath <= 0)
                    {
                        var reduction = healthDecreaseSpeed * Time.deltaTime;
                        damagable.TakeDamage(reduction, HitType.Any);
                    }
                    break;
            }
        }

        void ClimbUpFromWater()
        {
            if (inputManager.JumpKeyDown)
            {
                var hitData = environmentScanner.ObstacleCheck();
                if (!swimming.HeadUnderwaterLastFrame && hitData.forwardHitFound &&
                hitData.heightHitFound &&
                hitData.hasSpace &&
                Vector3.Angle(Vector3.up, hitData.forwardHit.normal) > 45f &&
                Vector3.Angle(Vector3.up, hitData.heightHit.normal) <= 45f)
                {
                    StartCoroutine(swimming.ClimbUpFromWater(hitData));
                }
            }
        }

        float GetMoveSpeed()
        {
            var moveSpeed = inputManager.SprintKey && (swimming.RotationValue < .3f && swimming.RotationValue > -.3f) ? fastSwimSpeed : normalSpeed;
            currentMoveSpeed = Mathf.MoveTowards(currentMoveSpeed, moveSpeed, Time.deltaTime * 2);
            return currentMoveSpeed;
        }

        #region Rootmotion
        bool prevRootMotionVal;
        void EnableRootMotion()
        {
            prevRootMotionVal = player.UseRootMotion;
            player.UseRootMotion = true;
        }

        void ResetRootMotion()
        {
            player.UseRootMotion = prevRootMotionVal;
        }
        #endregion
    }
}
