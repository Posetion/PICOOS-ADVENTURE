using FS_Core;
using FS_ThirdPerson;
using FS_Util;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace FS_Swimming
{
    public class SwimmingAI : MonoBehaviour
    {
        public bool followTarget;
        [ShowIf("followTarget", true)]
        public Transform target;
        [ShowIf("followTarget", true)]
        public float stoppingDistance = 2f;
        [ShowIf("followTarget", true)]
        public float agentMinSpeedWhileSwimming = 2f;
        public float agentMaxSpeedWhileSwimming = 3.5f;

        [Header("Ground Check Settings")]
        [Tooltip("Radius of ground detection sphere")]
        [SerializeField] float groundCheckRadius = 0.2f;

        [Tooltip("Offset between the player's root position and the ground detection sphere")]
        [SerializeField] Vector3 groundCheckOffset = new(0f, 0.15f, 0.07f);

        CharacterController characterController;
        Animator animator;
        NavMeshAgent agent;
        Damagable damagable;
        IAICharacter aICharacter;
        Swimming swimming;
        bool useRootMotion;
        bool inAction;
        Transform headTransform;
        float defaultAgentSpeed;
        float currentSwimSpeed;
        float ySpeed;



        private void Awake()
        {
            animator = GetComponent<Animator>();
            agent = GetComponent<NavMeshAgent>();
            aICharacter = GetComponent<IAICharacter>();
            swimming = GetComponent<Swimming>();
            characterController = GetComponent<CharacterController>();
            damagable = GetComponent<Damagable>();
            headTransform = animator.GetBoneTransform(HumanBodyBones.Head);
            agent.autoTraverseOffMeshLink = false;
        }
        private void Start()
        {
            swimming.OnEnterWater.AddListener(() =>
            {
                defaultAgentSpeed = agent.speed;
                agent.speed = agentMinSpeedWhileSwimming;
                aICharacter.OnStartAction(!aICharacter.IsBusy, false);

            });
            swimming.OnExitWater.AddListener(() =>
            {
                agent.speed = defaultAgentSpeed;
                aICharacter.OnEndAction();
            });

            swimming.OnEnableRootMotion += () => useRootMotion = true;
            swimming.OnResetRootMotion += () => useRootMotion = false;
            swimming.OnStartDive.AddListener(() =>
            {
                aICharacter.OnStartAction(true, true);
            });

            swimming.OnEndDive.AddListener(() =>
            {
                if (agent.enabled)
                    agent.CompleteOffMeshLink();
            });
            swimming.OnDiveCancelled += () =>
            {
                StartCoroutine(JumpDown(agent.currentOffMeshLinkData.endPos));
            };
        }
        private void Update()
        {
            if (damagable.IsDead)
            {
                if (!aICharacter.IsBusy)
                {
                    if (animator != null)
                        animator.SetFloat(AnimatorParameters.SwimSpeed, 0);
                    swimming.SetRagdollState(true);
                }
                return;
            }

            swimming.IsGrounded = Physics.CheckSphere(transform.TransformPoint(groundCheckOffset), groundCheckRadius, FSSettings.i.GroundLayer);
            swimming.MoveAmount = animator.GetFloat(AnimatorParameters.SwimSpeed);

            if (agent.enabled && (followTarget || swimming.IsSwimming) && target != null)
            {
                Vector3 posA = transform.position;
                Vector3 posB = target.position;
                posA.y = 0f;
                posB.y = 0f;
                float distance = Vector3.Distance(posA, posB);

                if (distance > stoppingDistance)
                {
                    agent.stoppingDistance = Mathf.MoveTowards(agent.stoppingDistance, 0, Time.deltaTime);
                    if (distance > stoppingDistance * 2 && swimming.IsSwimming)
                        agent.speed = Mathf.MoveTowards(agent.speed, agent.speed + distance * Time.deltaTime, Time.deltaTime);
                    else if (swimming.IsSwimming)
                        agent.speed = Mathf.MoveTowards(agent.speed, agentMinSpeedWhileSwimming, Time.deltaTime * 5);
                }
                else
                {
                    agent.stoppingDistance = Mathf.MoveTowards(agent.stoppingDistance, stoppingDistance + 1, Time.deltaTime * 10);
                    if (swimming.IsSwimming)
                        agent.speed = Mathf.MoveTowards(agent.speed, agentMinSpeedWhileSwimming, Time.deltaTime * 5);
                }
                if (swimming.IsSwimming)
                {
                    agent.speed = Mathf.Clamp(agent.speed, agentMinSpeedWhileSwimming, agentMaxSpeedWhileSwimming);
                }
                agent.SetDestination(target.position);
            }
            HandleMovementUpdate();
            swimming.UpdateSwimmingState();

            if (agent.isOnOffMeshLink && !swimming.IsSwimming && !swimming.InAction && !inAction)
            {
                NavMesh.SamplePosition(agent.currentOffMeshLinkData.endPos, out var hit, 1f, NavMesh.AllAreas);
                if (Physics.CheckSphere(agent.currentOffMeshLinkData.endPos, .1f, swimming.Water, QueryTriggerInteraction.Collide) && hit.mask == (1 << NavMesh.GetAreaFromName("Water")))
                {
                    agent.updateRotation = false;
                    var rotPos = agent.currentOffMeshLinkData.endPos;
                    rotPos.y = transform.position.y;
                    Vector3 direction = (rotPos - transform.position).normalized;
                    Quaternion lookRotation = Quaternion.LookRotation(direction);
                    transform.rotation = lookRotation;
                    swimming.Dive();
                }
                else if (agent.currentOffMeshLinkData.linkType == OffMeshLinkType.LinkTypeDropDown || agent.currentOffMeshLinkData.linkType == OffMeshLinkType.LinkTypeJumpAcross)
                    StartCoroutine(JumpDown(agent.currentOffMeshLinkData.endPos));
            }
            if (agent.isOnOffMeshLink && swimming.IsSwimming && !swimming.InAction && !inAction)
            {
                if (agent.currentOffMeshLinkData.linkType == OffMeshLinkType.LinkTypeDropDown)
                {
                    StartCoroutine(ClimbUp(agent.currentOffMeshLinkData.endPos));
                }
            }
        }

        IEnumerator HandleFalling(string animName, Vector3 landPos, Action onComplete = null)
        {
            var dis = landPos - transform.position;

            if (dis.y > -0.2f)
                yield break;
            //if (NavMesh.SamplePosition(landPos, out NavMeshHit hit, 1f, NavMesh.AllAreas))
            //    landPos = hit.position;
            var matchParams = new FS_ThirdPerson.TargetMatchParams()
            {
                pos = landPos,
                startTime = 0.4f,
                endTime = 0.7f,
                target = AvatarTarget.Root,
                posWeight = new Vector3(0, 0, 1)
            };
            var dir = landPos - transform.position;
            dir.y = 0;
            Quaternion targetRot;
            if (dir != Vector3.zero)
                targetRot = Quaternion.LookRotation(dir);
            else
                targetRot = Quaternion.LookRotation(transform.forward);

            animator.SetBool("IsGrounded", false);
            yield return swimming.DoAction(animName, rotate: true, targetRot: targetRot, matchParams, onComplete: () =>
            {
                onComplete?.Invoke();
            });
            ySpeed = Physics.gravity.y / 4;
            while (transform.position.y > landPos.y)
            {
                ySpeed += Physics.gravity.y * Time.deltaTime;
                transform.position += Vector3.up * ySpeed * Time.deltaTime;
                if ((transform.position.y - landPos.y) < 0.2f)
                {
                    if (ySpeed < Physics.gravity.y / 2)
                        animator.SetFloat("fallAmount", Mathf.Clamp(Mathf.Abs(ySpeed) * 0.06f, 0.6f, 1f));
                    else
                        animator.SetFloat("fallAmount", 0);
                    animator.SetBool("IsGrounded", true);
                }
                yield return null;
            }
            if (ySpeed < Physics.gravity.y / 2)
                animator.SetFloat("fallAmount", Mathf.Clamp(Mathf.Abs(ySpeed) * 0.06f, 0.6f, 1f));
            else
                animator.SetFloat("fallAmount", 0);
            animator.SetBool("IsGrounded", true);
            transform.position = new Vector3(transform.position.x, landPos.y, transform.position.z);
        }
        IEnumerator ClimbUp(Vector3 linkEndPos)
        {

            agent.enabled = false;
            aICharacter.OnStartAction(false, true);
            //agent.updateRotation = false;
            var rotPos = agent.currentOffMeshLinkData.endPos;
            rotPos.y = transform.position.y;
            Vector3 direction = (rotPos - transform.position).normalized;
            Quaternion lookRotation = Quaternion.LookRotation(-direction);
            transform.rotation = lookRotation;

            ObstacleHitData hitData = new ObstacleHitData();
            hitData.heightHit.point = linkEndPos;
            yield return swimming.ClimbUpFromWater(hitData);
            agent.CompleteOffMeshLink();
            aICharacter.OnEndAction();
            agent.enabled = true;
            //agent.updateRotation = true;
        }
        IEnumerator JumpDown(Vector3 linkEndPos)
        {
            if (agent.isOnOffMeshLink)
            {
                NavMesh.SamplePosition(agent.currentOffMeshLinkData.endPos, out var hit, 1f, NavMesh.AllAreas);
                if (followTarget || hit.mask == (1 << NavMesh.GetAreaFromName("Water")))
                {
                    inAction = true;
                    yield return HandleFalling("Jump Down", linkEndPos);
                    inAction = false;
                    agent.updateRotation = true;
                    agent.CompleteOffMeshLink();
                }
            }
        }
        private void HandleMovementUpdate()
        {
            var characterVelocity = agent.velocity;
            characterVelocity.y = 0;


            float forwardSpeed = Vector3.Dot(characterVelocity, transform.forward);
            animator.SetFloat(AnimatorParameters.moveAmount, forwardSpeed / 3, 0.2f, Time.deltaTime);

            float strafeSpeed = Vector3.Dot(characterVelocity, transform.right);
            animator.SetFloat(AnimatorParameters.strafeAmount, strafeSpeed / 3, 0.2f, Time.deltaTime);


            if (swimming.IsSwimming)
            {
                float curSpeed = animator.GetFloat(AnimatorParameters.SwimSpeed);
                float targetSpeed = (agent.speed / agentMaxSpeedWhileSwimming) * 1.5f;
                float distance = Vector3.Distance(transform.position, target.position);

                // Smooth speed transitions
                currentSwimSpeed = Mathf.MoveTowards(currentSwimSpeed, targetSpeed, Time.deltaTime * 10f);
                float speed = Mathf.MoveTowards(curSpeed, targetSpeed, Time.deltaTime * 10f);

                // Adjust speed based on distance
                if (distance > stoppingDistance)
                {
                    if (currentSwimSpeed is > 0.7f and < 1f)
                        speed = Mathf.MoveTowards(speed, 0.5f, Time.deltaTime * 10f);
                    else if (currentSwimSpeed > 1.2f)
                        speed = Mathf.MoveTowards(speed, 1.5f, Time.deltaTime * 10f);
                }
                else
                {
                    speed = 0f;
                }
                if (!agent.hasPath || agent.velocity.magnitude == 0) speed = 0;
                // Apply smoothed animation speed
                animator.SetFloat(AnimatorParameters.SwimSpeed, speed, 0.2f, Time.deltaTime);
            }

            var headUnderWater = Physics.CheckSphere(headTransform.position + Vector3.up * .1f, .01f, swimming.Water, QueryTriggerInteraction.Collide);
            if (headUnderWater)
            {
                agent.enabled = false;
                transform.position += Vector3.up * Time.deltaTime * 3;
            }
            else
                agent.enabled = true;
        }
        private void OnAnimatorMove()
        {
            if (useRootMotion)
            {
                transform.position += animator.deltaPosition;
                transform.rotation *= animator.deltaRotation;
            }
        }
    }
}