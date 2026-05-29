using FS_Core;
using FS_ThirdPerson;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace FS_Swimming
{
    public class Swimming : MonoBehaviour
    {
        [SerializeField, Tooltip("Enables diving")]
        bool enableDive = true;

        [Header("Swimming Detection Settings")]
        [SerializeField, Tooltip("Vertical offset used for checking if the player is inside the water.")]
        float swimOffset = 1.33f;

        [SerializeField, Tooltip("Vertical offset used to determine if the player's head is above or below the water surface.")]
        float headOffset = 0.15f;

        [SerializeField, Tooltip("Maximum height the player can dive.")]
        float maxDiveHeight = 50f;

        [Header("Effects")]
        [SerializeField, Tooltip("Prefab for ripple effect when interacting with the water surface.")]
        GameObject rippleEffectPrefab;

        [SerializeField, Tooltip("Prefab for foam effect when interacting with water edges.")]
        GameObject foamEffectPrefab;

        [SerializeField, Tooltip("Prefab for bubble effect when the player is underwater.")]
        GameObject bubbleEffectPrefab;

        [Header("Sound Effects")]
        [SerializeField, Tooltip("Audio clip for a small splash (played when the character falls from a low height).")]
        AudioClip smallSplashAudioClip;

        [SerializeField, Tooltip("Audio clip for a big splash (played when the character falls from a greater height).")]
        AudioClip bigSplashAudioClip;

        [SerializeField, Tooltip("The minimum fall height that triggers a big splash sound.")]
        float bigSplashThreshold = 5f;

        [SerializeField, Tooltip("Audio clip that plays when the player climbs up and exits from the water onto a surface.")]
        AudioClip climbUpFromWaterAudioClip;

        [SerializeField, Tooltip("Audio clip for a hand hitting the water surface and creating a splash effect.")]
        List<AudioClip> handSplashAudioClip;

        [SerializeField, Tooltip("Audio clip that plays when the player is swimming underwater (moving through water).")]
        List<AudioClip> underwaterSwimAudioClip;

        [Header("Events")]
        public UnityEvent OnEnterWater;
        public UnityEvent OnExitWater;

        public UnityEvent OnEnterUnderWater;
        public UnityEvent OnExitUnderWater;

        public UnityEvent OnStartDive;
        public UnityEvent OnEndDive;
        public UnityEvent OnClimbupFromWater;
        public UnityEvent ClimbupFromWaterCompleted;

        public Action<float> OnInitialVelocity;
        public Action OnEnableRootMotion;
        public Action OnResetRootMotion;
        public Action OnDiveCancelled;

        // runtime fields
        Collider waterCollider;
        float maxYPosWhileInAir = 0f;

        float defaultCharacterControllerRadius = .2f;
        float characterControllerRadiusSwim = .5f;

        float defaultCharacterControllerHeight = 1.7f;
        float characterControllerHeightSwim = .7f;

        float deafultCharacterControllerCenterY = 0.87f;
        float characterControllerCenterYSwim = 1f;


        bool applyInitialUpwardGravity = true;

        List<Rigidbody> ragdollRigidBodies;
        Coroutine modelAdjustmentCoroutine;
        Transform hipsTransform;
        Transform headTransform;
        Transform rightHandTransform;
        Transform leftHandTransform;
        float waterSurfaceY = 0f;
        bool canEnterWater = true;
        GameObject bubbleEffectObject;
        bool prevFootStepEffectState;
        float foamDelay = 1f;
        float foamDelayTimer = 0f;
        float prevYAngle;

        // components
        Animator animator;
        CharacterController characterController;
        EnvironmentScanner environmentScanner;
        Damagable damagable;
        FootStepEffects footStepEffects;

        // properties
        public LayerMask Water { get; private set; }
        public bool HeadUnderwaterLastFrame  { get; private set; } = false;
        public float MoveAmount { get; set; } = 0f;
        public float RotationValue { get; private set; }
        public bool IsSwimming { get; private set; } = false;
        public bool IsGrounded { get; set; } = false;
        public bool InAction { get; private set; }
        public bool IsInInitailVelocity { get; private set; } // keep original name to avoid breaking usage
        public float SwimOffset => swimOffset;
        public float HeadOffset => headOffset;
        public float MaxDiveHeight => maxDiveHeight;


        AudioClip UnderwaterSwimAudioClip => underwaterSwimAudioClip != null && underwaterSwimAudioClip.Count > 0 ? underwaterSwimAudioClip[UnityEngine.Random.Range(0, underwaterSwimAudioClip.Count)] : null;
        AudioClip HandSplashAudioClip => handSplashAudioClip != null && handSplashAudioClip.Count > 0 ? handSplashAudioClip[UnityEngine.Random.Range(0, handSplashAudioClip.Count)] : null;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            characterController = GetComponent<CharacterController>();
            environmentScanner = GetComponent<EnvironmentScanner>();
            footStepEffects = GetComponent<FootStepEffects>();
            damagable = GetComponent<Damagable>();

            Water = LayerMask.GetMask("Water");
            if (characterController != null)
            {
                defaultCharacterControllerRadius = characterController.radius;
                defaultCharacterControllerHeight = characterController.height;
                deafultCharacterControllerCenterY = characterController.center.y;
            }

            if (animator != null)
            {
                hipsTransform = animator.GetBoneTransform(HumanBodyBones.Hips);
                headTransform = animator.GetBoneTransform(HumanBodyBones.Head);
                rightHandTransform = animator.GetBoneTransform(HumanBodyBones.RightHand);
                leftHandTransform = animator.GetBoneTransform(HumanBodyBones.LeftHand);
            }

            ragdollRigidBodies = GetRagdollRigidbodies();
            SetRagdollState(false);

            if (bubbleEffectPrefab != null && headTransform != null)
            {
                bubbleEffectObject = Instantiate(bubbleEffectPrefab, headTransform);
                bubbleEffectObject.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                bubbleEffectObject.transform.forward = Vector3.up;
                bubbleEffectObject.SetActive(false);
            }

            if (damagable != null)
            {
                damagable.OnDead += () =>
                {
                    if (bubbleEffectObject != null)
                        bubbleEffectObject.SetActive(false);
                };
            }
        }

        private void FixedUpdate()
        {
            //if (damagable != null && damagable.IsDead && IsSwimming && ragdollRigidBodies != null)
            //{
            //    foreach (var rb in ragdollRigidBodies)
            //    {
            //        if (rb == null) continue;
            //        rb.useGravity = false;
            //        rb.AddForce(Physics.gravity * Time.fixedDeltaTime, ForceMode.Acceleration);
            //    }
            //}
        }

        private void Update()
        {
            if (!damagable.IsDead)
            {
                if (!InAction)
                {
                    bool inWater = Physics.CheckSphere(transform.position + Vector3.up * swimOffset, 0.25f, Water, QueryTriggerInteraction.Collide);
                    if (inWater && canEnterWater)
                    {
                        if (!IsSwimming)
                            EnterWater();
                    }
                    else if (IsSwimming)
                    {
                        ExitFromSwim();
                    }
                }

                if (!IsSwimming)
                {
                    if ((transform.position.y > maxYPosWhileInAir) || IsGrounded)
                    {
                        maxYPosWhileInAir = transform.position.y;
                    }
                }
            }
        }

        void EnterWater()
        {
            float entryHeight = Mathf.Clamp(maxYPosWhileInAir - transform.position.y, 2, maxDiveHeight);
            var waterCheckPos = transform.position + Vector3.up * entryHeight;

            if (Physics.Raycast(waterCheckPos, Vector3.down, out var hit, entryHeight, Water, QueryTriggerInteraction.Collide))
            {
                waterSurfaceY = hit.point.y;
                waterCollider = hit.collider;
            }
            else
            {
                waterSurfaceY = waterCheckPos.y;
            }
            
            if (animator != null)
            {
                animator.SetBool(AnimatorParameters.IsSwimming, true);
                animator.CrossFade(AnimationNames.SwimmingAction, .2f);
            }

            StartCoroutine(InitialVelocity());
            OnEnterWater?.Invoke();

            if (bubbleEffectObject != null)
            {
                var ps = bubbleEffectObject.GetComponent<ParticleSystem>();
                if (ps != null && waterCollider != null)
                {
                    var trigger = ps.trigger;
                    trigger.enabled = true;
                    trigger.SetCollider(0, waterCollider);
                    trigger.inside = ParticleSystemOverlapAction.Ignore;
                    trigger.outside = ParticleSystemOverlapAction.Kill;
                }
            }

            if (footStepEffects != null)
            {
                prevFootStepEffectState = footStepEffects.IsEnabled;
                footStepEffects.EnableFootStepEffects(false);
            }

            IsSwimming = true;
            //characterController.height = characterControllerHeightSwim;
            //characterController.center = new Vector3(characterController.center.x, characterControllerCenterYSwim, characterController.center.z);
        }

        public void ExitFromSwim()
        {
            //characterController.height = characterControllerHeightSwim;
            //characterController.center = new Vector3(characterController.center.x, characterControllerCenterYSwim, characterController.center.z);
            applyInitialUpwardGravity = true;

            if (animator != null)
                animator.SetBool(AnimatorParameters.IsSwimming, false);

            MoveAmount = 0;
            StartCoroutine(SmoothAnimatorFloatValue(AnimatorParameters.SwimSpeed, 0f, 1));
            maxYPosWhileInAir = float.NegativeInfinity;
            SetColliderRadius(true, true);
            OnExitWater?.Invoke();
            StartCoroutine(ControlEntryDelay());
            waterCollider = null;

            if (footStepEffects != null)
                footStepEffects.EnableFootStepEffects(prevFootStepEffectState);
            animator.SetFloat(AnimatorParameters.rotation, 0);
            IsSwimming = false;

        }
        Vector3 jumpPoint;
        public void Dive()
        {
            if (!enableDive || InAction || IsSwimming || !IsGrounded)
            {
                OnDiveCancelled?.Invoke();
                return;
            }
            Vector3 halfExtents = new Vector3(0.2f, 0.5f, 0.01f);
            Vector3 origin = transform.position + Vector3.up; 
            Vector3 direction = transform.forward.normalized;

            // Forward hit check
            if (!Physics.BoxCast(origin, halfExtents, direction, out var hit, transform.rotation, 2f, environmentScanner.ObstacleLayer, QueryTriggerInteraction.Ignore))
            {
                // Height hit
                origin = transform.position + transform.forward * 2f + Vector3.up * 0.5f;
                if (Physics.BoxCast(origin, new Vector3(0.2f, 0.6f, 0.01f), Vector3.down, out hit, Quaternion.LookRotation(Vector3.down), maxDiveHeight, ~0, QueryTriggerInteraction.Collide))
                {
                    if (hit.transform != null && hit.transform.gameObject.layer == LayerMask.NameToLayer("Water"))
                    {
                        hit.point = hit.point + direction.normalized;
                        var hasSufficientWater = Physics.CheckSphere(hit.point + Vector3.down * swimOffset, 0.05f, Water, QueryTriggerInteraction.Collide) &&
                            !Physics.BoxCast(hit.point, new Vector3(0.2f, 0.2f, 0.01f), Vector3.down, out var spaceCheck, Quaternion.LookRotation(Vector3.down), swimOffset * 2, ~Water, QueryTriggerInteraction.Collide);
                        jumpPoint = hit.point;
                        if (hasSufficientWater)
                        {
                            StartCoroutine(StartDive());
                            if (animator != null)
                                animator.SetFloat(AnimatorParameters.SwimSpeed, 1);
                           
                            return;
                        }
                    }
                }
                //BoxCastDebug.DrawBoxCastBox(transform.position + transform.forward * 2f + Vector3.up * 0.5f, new Vector3(0.2f, 0.6f, 0.01f), Quaternion.LookRotation(Vector3.down), Vector3.down, maxDiveHeight, Color.cyan);
                //BoxCastDebug.DrawBoxCastBox(transform.position + Vector3.up, new Vector3(0.2f, 0.5f, 0.01f), transform.rotation, transform.forward, 2f, Color.yellow);
                //BoxCastDebug.DrawBoxCastBox(hit.point, new Vector3(0.2f, 0.2f, 0.01f), Quaternion.LookRotation(Vector3.down), Vector3.down, swimOffset * 2, Color.red);
            }
            OnDiveCancelled?.Invoke();
        }

        public void UpdateSwimmingState()
        {
            if (!IsSwimming || InAction) return;

            var headUnderWater = Physics.CheckSphere(transform.position + Vector3.up * swimOffset, 0.1f, Water, QueryTriggerInteraction.Collide); 

            AdjustCollider();
            AdjustSwimIdle(headUnderWater);
            HandleTurning();

            headUnderWater = Physics.CheckSphere(transform.position + Vector3.up * (swimOffset + headOffset), 0.1f, Water, QueryTriggerInteraction.Collide);
            StartCoroutine(HandleFoam(headUnderWater));
            HandleUnderWaterTransition(headUnderWater);
        }

        IEnumerator StartDive(Vector2 directionInput = default, Vector3 _moveDir = default)
        {
            if (animator != null)
                animator.SetFloat(AnimatorParameters.DiveSpeed, animator.GetFloat(AnimatorParameters.moveAmount));

            InAction = true;
            OnStartDive?.Invoke();
            applyInitialUpwardGravity = false;

            const int LayerIndex = 0;
            const float CrossfadeDuration = 0.1f;
            const float WaitStartTimeout = 1.5f;

            EnableRootMotion();

            if (animator != null)
                animator.CrossFade(AnimationNames.DiveStart, CrossfadeDuration);

            // Wait for state enter (or timeout)
            float startWaitBegin = Time.time;
            while (animator != null && !animator.GetCurrentAnimatorStateInfo(LayerIndex).IsName(AnimationNames.DiveStart))
            {
                if (Time.time - startWaitBegin > WaitStartTimeout) break;
                yield return null;
            }

            // Wait until DiveStart finishes
            while (animator != null && animator.GetCurrentAnimatorStateInfo(LayerIndex).IsName(AnimationNames.DiveStart) &&
                   animator.GetCurrentAnimatorStateInfo(LayerIndex).normalizedTime < 1f)
            {
                yield return null;
            }
            ResetRootMotion();


            float maxYSpeed = -30;
            float ySpeed = Physics.gravity.y;
            // Wait until foot enters water
            bool headUnderWater = false;
            while (!headUnderWater)
            {
                var foot = animator?.GetBoneTransform(HumanBodyBones.RightFoot);
                if (foot != null)
                {
                    ySpeed -= Time.deltaTime * 10;
                    Mathf.Clamp(ySpeed, maxYSpeed, Physics.gravity.y);
                    var v = new Vector3(0, ySpeed * Time.deltaTime, 0);
                    characterController.Move(v);
                    headUnderWater = Physics.CheckSphere(foot.position, 0.51f, Water, QueryTriggerInteraction.Collide);
                }
                yield return null;
            }

            InAction = false;
            

            float entryHeight = maxYPosWhileInAir - transform.position.y;
            OnInitialVelocity?.Invoke(entryHeight);

            Vector3 inputVector = new Vector3(directionInput.x, 0f, directionInput.y);
            if (inputVector != Vector3.zero)
            {
                if (animator != null)
                {
                    animator.SetFloat(AnimatorParameters.SwimSpeed, 1f);
                    animator.CrossFade(AnimationNames.SwimmingAction, CrossfadeDuration);
                }
            }
            else
            {
                EnableRootMotion();
                if (animator != null)
                    animator.CrossFade(AnimationNames.DiveLand, CrossfadeDuration);

                while (animator != null && animator.GetCurrentAnimatorStateInfo(LayerIndex).IsName(AnimationNames.DiveLand) &&
                       animator.GetCurrentAnimatorStateInfo(LayerIndex).normalizedTime < 1f)
                {
                    yield return null;
                }
                ResetRootMotion();
            }
            OnEndDive?.Invoke();
            InAction = false;
        }

        public IEnumerator ClimbUpFromWater(ObstacleHitData hitData)
        {
            float height = hitData.heightHit.point.y - transform.position.y;
            if (height > 2f)
                yield break;

            var matchParams = new TargetMatchParams()
            {
                pos = hitData.heightHit.point,
                startTime = .01f,
                endTime = .46f,
                target = AvatarTarget.RightFoot,
                posWeight = Vector3.one
            };

            InAction = true;
            var dir = hitData.heightHit.point - transform.position;
            dir.y = 0;
            var targetRotation = Quaternion.LookRotation(dir);

            OnClimbupFromWater?.Invoke();
            PlayAudio(climbUpFromWaterAudioClip, transform.position);
            yield return DoAction(AnimationNames.ClimbUpFromWater, true, targetRotation, matchParams);
            ExitFromSwim();
            ClimbupFromWaterCompleted?.Invoke();
            InAction = false;
        }

        void AdjustCollider()
        {
            float animSwimSpeed = animator != null ? animator.GetFloat(AnimatorParameters.SwimSpeed) : 0f;
            SetColliderRadius(animSwimSpeed < 0.1f);
        }

        void HandleTurning()
        {
            float currentYAngle = transform.localEulerAngles.y;
            float angleDiff = Mathf.DeltaAngle(prevYAngle, currentYAngle);

            if (Mathf.Abs(angleDiff) < 0.01f)
            {
                RotationValue = 0f;
            }
            else
            {
                RotationValue = Mathf.Clamp(angleDiff / Time.deltaTime * 0.01f, -0.5f, 0.5f);
            }

            if (animator != null)
                animator.SetFloat(AnimatorParameters.rotation, RotationValue, 0.1f, Time.deltaTime);

            prevYAngle = currentYAngle;
        }

        void AdjustSwimIdle(bool headUnderWater)
        {
            if (animator != null)
            {
                float targetSwimHeight = headUnderWater ? 1f : 0f;
                animator.SetFloat(AnimatorParameters.SwimIdle, targetSwimHeight, .2f, Time.deltaTime);
            }
        }

        IEnumerator HandleFoam(bool headUnderWater)
        {
            if (!headUnderWater)
            {
                foamDelay = Mathf.Lerp(1f, .1f, MoveAmount / 3f);
                if (foamDelayTimer < foamDelay)
                {
                    foamDelayTimer += Time.deltaTime;
                    yield break;
                }
                foamDelayTimer = 0f;

                var foamPos = transform.position;
                foamPos.y = waterSurfaceY;
                SpawnFoam(foamPos, headTransform);
            }
        }

        void SpawnFoam(Vector3 foamPos, Transform bone)
        {
            if (foamEffectPrefab == null || bone == null) return;
            var effect = Instantiate(foamEffectPrefab);
            foamPos.x = bone.position.x;
            foamPos.z = bone.position.z;
            effect.transform.position = foamPos;
            Destroy(effect, 2f);
        }

        void HandleUnderWaterTransition(bool headUnderWater)
        {
            if (headUnderWater && !HeadUnderwaterLastFrame)
            {
                if (bubbleEffectObject != null) bubbleEffectObject.SetActive(true);
                OnEnterUnderWater?.Invoke();
            }
            else if (!headUnderWater && HeadUnderwaterLastFrame)
            {
                if (bubbleEffectObject != null) bubbleEffectObject.SetActive(false);
                OnExitUnderWater?.Invoke();
            }
            HeadUnderwaterLastFrame = headUnderWater;
        }

        public void SetRagdollState(bool state)
        {
            if (ragdollRigidBodies == null || ragdollRigidBodies.Count == 0) return;

            if (state)
            {
                if (modelAdjustmentCoroutine == null)
                    modelAdjustmentCoroutine = StartCoroutine(AdjustModelPositionCoroutine());
            }
            else
            {
                if (animator != null) animator.enabled = true;

                foreach (var rb in ragdollRigidBodies)
                {
                    if (rb == null) continue;
                    rb.isKinematic = true;
                    var col = rb.GetComponent<Collider>();
                    if (col != null) col.isTrigger = false;
                }

                if (modelAdjustmentCoroutine != null)
                {
                    StopCoroutine(modelAdjustmentCoroutine);
                    modelAdjustmentCoroutine = null;
                }

                if (ragdollRigidBodies.Count > 10 && hipsTransform != null)
                {
                    transform.position = hipsTransform.position;
                    hipsTransform.localPosition = Vector3.zero;
                }
            }
        }

        IEnumerator InitialVelocity(bool reduceUpwardVelocity = true)
        {
            IsInInitailVelocity = true;

            float g = Mathf.Abs(Physics.gravity.y);
            float entryHeight = maxYPosWhileInAir - waterSurfaceY;
            if (entryHeight <= 0.05f)
            {
                IsInInitailVelocity = false;
                yield break;
            }

            if (applyInitialUpwardGravity)
                OnInitialVelocity?.Invoke(entryHeight);
            
            PlayRippleEffect();
            float downwardVelocity = -Mathf.Sqrt(2f * g * Mathf.Min(10f, entryHeight * .75f));
            float entryDuration = 0.2f;
            float t = 0f;
            while (t < entryDuration)
            {
                downwardVelocity -= g * Time.deltaTime;
                characterController?.Move(Vector3.up * downwardVelocity * Time.deltaTime);

                t += Time.deltaTime;
                yield return null;
            }

            float upwardVelocity = Mathf.Abs(downwardVelocity) * (reduceUpwardVelocity ? 0.6f : 1.2f);
            entryDuration = .2f;
            float drag = entryHeight > 1f ? 5f : 2f;
            t = 0f;
            while ((t < entryDuration && Physics.CheckSphere(transform.position + Vector3.up * swimOffset, 0.2f, Water, QueryTriggerInteraction.Collide)) && applyInitialUpwardGravity)
            {
                upwardVelocity = Mathf.Lerp(upwardVelocity, 0f, drag * Time.deltaTime);
                transform.position += Vector3.up * upwardVelocity * Time.deltaTime;

                if (MoveAmount > 0) break;
                t += Time.deltaTime;
                yield return null;
            }

            IsInInitailVelocity = false;
        }

        #region Utilities
        void SetColliderRadius(bool reset = true, bool snap = false)
        {
            if (characterController == null) return;
            var radius = reset ? defaultCharacterControllerRadius : characterControllerRadiusSwim;

            if (!Mathf.Approximately(characterController.radius, radius))
            {
                if (snap)
                    characterController.radius = radius;
                else
                    characterController.radius = Mathf.MoveTowards(characterController.radius, radius, Time.deltaTime);
            }
            var height = reset ? defaultCharacterControllerHeight : characterControllerHeightSwim;

            if (!Mathf.Approximately(characterController.height, height))
            {
                if (snap)
                    characterController.height = height;
                else
                    characterController.height = Mathf.MoveTowards(characterController.height, height, Time.deltaTime);
            }
            var centerY = reset ? deafultCharacterControllerCenterY : characterControllerCenterYSwim;
            if (!Mathf.Approximately(characterController.center.y, centerY))
            {
                if (snap)
                    characterController.center = new Vector3(characterController.center.x, centerY, characterController.center.z);
                else
                    characterController.center = new Vector3(characterController.center.x, Mathf.MoveTowards(characterController.center.y, centerY, Time.deltaTime), characterController.center.z);
            }
        }

        IEnumerator SmoothAnimatorFloatValue(int parameter, float val, float duration = .5f)
        {
            if (animator == null) yield break;
            var startVal = animator.GetFloat(parameter);
            float timer = 0f;

            while (timer < duration)
            {
                timer += Time.deltaTime;
                var percent = Mathf.Clamp01(timer / duration);
                var lerpVal = Mathf.Lerp(startVal, val, percent);
                animator.SetFloat(parameter, lerpVal);
                yield return null;
            }
            animator.SetFloat(parameter, val);
        }

        List<Rigidbody> GetRagdollRigidbodies()
        {
            var list = new List<Rigidbody>();
            if (animator == null || !animator.isHuman) return list;

            foreach (HumanBodyBones bone in Enum.GetValues(typeof(HumanBodyBones)))
            {
                if (bone == HumanBodyBones.LastBone) continue;
                var boneTransform = animator.GetBoneTransform(bone);
                if (boneTransform == null) continue;
                var rb = boneTransform.GetComponent<Rigidbody>();
                if (rb != null && !list.Contains(rb)) list.Add(rb);
            }

            return list;
        }

        IEnumerator AdjustModelPositionCoroutine()
        {
            yield return new WaitForEndOfFrame();

            if (ragdollRigidBodies != null)
            {
                foreach (var rb in ragdollRigidBodies)
                {
                    if (rb == null) continue;
                    rb.isKinematic = false;
                    var col = rb.GetComponent<Collider>();
                    if (col != null) col.isTrigger = false;
                }
            }

            yield return null;
            if (animator != null) animator.enabled = false;

            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            while (true)
            {
                if (hipsTransform != null)
                {
                    transform.position = hipsTransform.position;
                    hipsTransform.localPosition = Vector3.zero;
                }
                yield return null;
            }
        }

        IEnumerator ControlEntryDelay()
        {
            var delay = (characterController != null && IsGrounded) ? .5f : .1f;
            float t = 0f;
            canEnterWater = false;
            while (t < delay)
            {
                t += Time.deltaTime;
                yield return null;
            }
            canEnterWater = true;
        }
        #endregion

        #region Effects
        void PlayRippleEffect()
        {
            if (rippleEffectPrefab == null) return;

            var waterPos = transform.position + Vector3.up * 2f;
            waterPos.y = waterSurfaceY;
            var ripple = Instantiate(rippleEffectPrefab, waterPos, Quaternion.Euler(90f, 0f, 0f));
            Destroy(ripple, 4f);

            var fallHeight = maxYPosWhileInAir - waterSurfaceY;
            var audioClip = fallHeight > bigSplashThreshold ? bigSplashAudioClip : smallSplashAudioClip;
            PlayAudio(audioClip, waterPos, "SplashAudio");
        }

        /// <summary>
        /// Plays splash effects when hand interacts with the water.
        /// Input string example: "0,0.5" (handIndex,speedMarker)
        /// </summary>
        public void PlaySplashEffect(string data)
        {
            if (IsInInitailVelocity) return;
            if (string.IsNullOrEmpty(data)) return;
            if (!IsSwimming) return;

            var parts = data.Split(',');
            if (parts.Length < 2) return;

            if (!int.TryParse(parts[0], out int handIndex)) return;
            if (!float.TryParse(parts[1], out float speedMarker)) return;

            var waterPos = transform.position + Vector3.up * 2f;
            waterPos.y = waterSurfaceY;
            float swimSpeed = animator != null ? animator.GetFloat(AnimatorParameters.SwimSpeed) : 0f;

            if (!HeadUnderwaterLastFrame)
            {
                var foamPos = transform.position;
                foamPos.y = waterSurfaceY;
                SpawnFoam(foamPos, handIndex == 0 ? leftHandTransform : rightHandTransform);
            }

            if (speedMarker == 0f && swimSpeed < 0.1f)
                PlayAudio(HandSplashAudioClip, waterPos, "SplashAudio");
            else if (Mathf.Approximately(speedMarker, 0.5f) && swimSpeed >= 0.3f && swimSpeed <= 0.8f)
                PlayAudio(HandSplashAudioClip, waterPos, "SplashAudio");
            else if (Mathf.Approximately(speedMarker, 1f) && swimSpeed > 0.8f && swimSpeed <= 1.25f)
                PlayAudio(UnderwaterSwimAudioClip, waterPos, "SplashAudio");
            else if (Mathf.Approximately(speedMarker, 1.5f) && swimSpeed > 1.25f)
                PlayAudio(HandSplashAudioClip, waterPos, "SplashAudio");
        }

        public void PlayAudio(AudioClip clip, Vector3 position, string name = "Audio Source", float pitch = 1f)
        {
            if (clip == null) return;
            var audioSourceGO = new GameObject(name);
            audioSourceGO.transform.position = position;
            var audio = audioSourceGO.AddComponent<AudioSource>();
            audio.pitch = pitch;
            audio.clip = clip;
            audio.Play();
            Destroy(audioSourceGO, clip.length + .1f);
        }
        #endregion

        #region TargetMatching
        public IEnumerator DoAction(string anim, bool rotate = false, Quaternion targetRot = default,
            TargetMatchParams matchParams = null, float postDelay = 0f, bool mirror = false, Action onComplete = null, float crossFadeTime = .2f)
        {
            InAction = true;
            EnableRootMotion();

            if (animator != null)
            {
                if (matchParams != null) Mathf.Min(crossFadeTime, matchParams.startTime + 0.02f);
                animator.CrossFade(anim, crossFadeTime);
            }

            yield return null;

            var animState = animator != null ? animator.GetNextAnimatorStateInfo(0) : default;
            float timer = 0f;
            while (timer <= animState.length)
            {
                timer += Time.deltaTime;
                float normalTime = animState.length > 0f ? timer / animState.length : 0f;

                if (matchParams != null)
                {
                    MatchTarget(matchParams.pos, matchParams.rot, matchParams.target, new MatchTargetWeightMask(matchParams.posWeight, 0),
                        matchParams.startTime, matchParams.endTime);

                    if (rotate && normalTime >= matchParams.startTime)
                        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, 100f * Time.deltaTime);
                }
                else if (rotate)
                {
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, 100f * Time.deltaTime);
                }

                if (animator != null && animator.IsInTransition(0) && timer > 0.5f) break;
                yield return null;
            }

            if (postDelay > 0f) yield return new WaitForSeconds(postDelay);
            InAction = false;
            ResetRootMotion();
            onComplete?.Invoke();
        }

        void MatchTarget(Vector3 matchPos, Quaternion rotation, AvatarTarget target, MatchTargetWeightMask weightMask, float startTime, float endTime)
        {
            if (animator == null) return;
            if (animator.isMatchingTarget || animator.IsInTransition(0)) return;
            animator.MatchTarget(matchPos, rotation, target, weightMask, startTime, endTime);
        }
        #endregion

        #region Rootmotion
        public void EnableRootMotion() => OnEnableRootMotion?.Invoke();
        public void ResetRootMotion() => OnResetRootMotion?.Invoke();
        #endregion

        private void OnDrawGizmos()
        {
            bool isInWater = Physics.CheckSphere(transform.position + Vector3.up * swimOffset, 0.25f, Water, QueryTriggerInteraction.Collide);
            Gizmos.color = isInWater ? new Color(1f, 0f, 0f, 0.5f) : new Color(0f, 0f, 1f, 0.5f);
            Gizmos.DrawSphere(transform.position + Vector3.up * swimOffset, 0.25f);

            var headUnderWater = Physics.CheckSphere(transform.position + Vector3.up * (swimOffset + headOffset), 0.1f, Water, QueryTriggerInteraction.Collide);
            Gizmos.color = headUnderWater ? new Color(1f, 0f, 0f, 0.5f) : new Color(0f, 0f, 1f, 0.5f);
            Gizmos.DrawSphere(transform.position + Vector3.up * (swimOffset + headOffset), 0.1f);

            //var maxY = new Vector3(transform.position.x, maxYPosWhileInAir, transform.position.z);
            //Gizmos.color = new Color(1f, 0f, 0f, 1f);
            //Gizmos.DrawSphere(maxY, 0.3f);
            //Gizmos.color = new Color(0f, 0f, 1f, 1f);
            //Gizmos.DrawSphere(jumpPoint, 0.3f);
        }
    }
}
