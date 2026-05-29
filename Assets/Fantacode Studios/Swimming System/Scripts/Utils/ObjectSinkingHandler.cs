using FS_Core;
using System.Collections.Generic;
using UnityEngine;

namespace FS_Swimming {
    public class ObjectSinkingHandler : MonoBehaviour
    {
        public enum LimitMode { HardClamp, SmoothLerp, ApplyUpwardForce }

        [Header("Limiter settings")]
        [Tooltip("Maximum allowed downward speed (positive value). e.g. 2 = -2 m/s is the slowest downward velocity")]
        public float maxDownwardSpeed = .2f;
        [Tooltip("Which method to use to limit downward velocity.")]
        public LimitMode mode = LimitMode.SmoothLerp;
        [Tooltip("Used by SmoothLerp mode. Larger = faster smoothing.")]
        public float smoothRate = 15f;
        [Tooltip("Used by ApplyUpwardForce mode. Multiplier for computed impulse.")]
        public float forceMultiplier = 1f;
        [Tooltip("If true, only affect rigidbodies that are NOT kinematic.")]
        public bool ignoreKinematic = true;
        [Tooltip("Optional: only affect rigidbodies on this LayerMask. Leave empty to affect all.")]
        public LayerMask layerMask = ~0;

        readonly Dictionary<Rigidbody, int> insideCount = new Dictionary<Rigidbody, int>();

        [Header("Effects")]
        [SerializeField, Tooltip("Prefab for ripple effect when interacting with the water surface.")]
        GameObject rippleEffectPrefab;

        [Header("Ripple suppression (global history)")]
        [Tooltip("Minimum distance between successive ripples (in world units). XZ-plane only.")]
        [SerializeField] float rippleMinDistance = 1f;
        [Tooltip("Minimum time between successive ripples (seconds).")]
        [SerializeField] float rippleMinDelay = 0.75f;
        int rippleHistoryCount = 10;
        [Tooltip("How long to keep spawned ripple before Destroy")]
        [SerializeField] float rippleLifetime = 2f;
        [SerializeField] AudioClip rippleAudioEffect;
        LayerMask waterLayerMask = 0;

        Collider waterCollider;

        class RippleInfo
        {
            public Vector3 pos;
            public float time;
        }

        private void Start()
        {
            waterCollider = GetComponent<Collider>();
            waterLayerMask = LayerMask.GetMask("Water");
        }

        readonly Queue<RippleInfo> recentRipples = new Queue<RippleInfo>();

        void OnTriggerEnter(Collider other)
        {
            if ((layerMask.value & (1 << other.gameObject.layer)) == 0) return;

            Rigidbody rb = other.attachedRigidbody;
            if (rb == null) return;
            if (ignoreKinematic && rb.isKinematic) return;

            var hittingThroughObjects = Physics.Raycast(other.transform.position + Vector3.up * .2f, Vector3.down, .5f, FSSettings.i.GroundLayer);
            if (hittingThroughObjects) return;

            if (!insideCount.ContainsKey(rb)) insideCount[rb] = 0;
            insideCount[rb] += 1;

            Vector3 spawnPos = waterCollider.ClosestPoint(other.transform.position);
            var obejctIsInUnderwater = !Physics.Raycast(spawnPos + Vector3.up * Mathf.Abs(rb.linearVelocity.y * Time.deltaTime), Vector3.down * Mathf.Abs(rb.linearVelocity.y * Time.deltaTime) * 2, Mathf.Abs(rb.linearVelocity.y * Time.deltaTime) * 2, waterLayerMask, QueryTriggerInteraction.Collide);
            //Debug.DrawRay(spawnPos + Vector3.up * Mathf.Abs(rb.velocity.y * Time.deltaTime), Vector3.down * Mathf.Abs(rb.velocity.y * Time.deltaTime) * 2, Color.red, 1f);
            if (!obejctIsInUnderwater)
            {
                SpawnRippleIfAllowedGlobalHistory(spawnPos);
                PlayAudio(rippleAudioEffect, spawnPos, "Ripple Audio Source");
            }
        }


        void OnTriggerExit(Collider other)
        {
            Rigidbody rb = other.attachedRigidbody;
            if (rb == null) return;
            if (!insideCount.ContainsKey(rb)) return;

            insideCount[rb] -= 1;
            if (insideCount[rb] <= 0) insideCount.Remove(rb);
        }

        void FixedUpdate()
        {
            if (insideCount.Count == 0) return;

            var bodies = new List<Rigidbody>(insideCount.Keys);
            var toRemove = new List<Rigidbody>();

            foreach (var rb in bodies)
            {
                if (rb == null)
                {
                    toRemove.Add(rb);
                    continue;
                }
                LimitDownwardVelocity(rb);
            }

            foreach (var r in toRemove)
            {
                insideCount.Remove(r);
            }
        }

        void LimitDownwardVelocity(Rigidbody rb)
        {
            float currentVy = rb.linearVelocity.y;
            float allowedVy = -Mathf.Abs(maxDownwardSpeed);

            if (currentVy < allowedVy)
            {
                switch (mode)
                {
                    case LimitMode.HardClamp:
                        Vector3 vClamp = rb.linearVelocity;
                        vClamp.y = allowedVy;
                        rb.linearVelocity = vClamp;
                        break;

                    case LimitMode.SmoothLerp:
                        float newVy = Mathf.Lerp(currentVy, allowedVy, 1f - Mathf.Exp(-smoothRate * Time.fixedDeltaTime));
                        Vector3 vSmooth = rb.linearVelocity;
                        vSmooth.y = newVy;
                        rb.linearVelocity = vSmooth;
                        break;

                    case LimitMode.ApplyUpwardForce:
                        float desiredVy = allowedVy;
                        float deltaV = desiredVy - currentVy; // positive upwards
                        float requiredForce = rb.mass * deltaV / Time.fixedDeltaTime;
                        rb.AddForce(Vector3.up * requiredForce * forceMultiplier, ForceMode.Force);
                        break;
                }
            }
        }

        void SpawnRippleIfAllowedGlobalHistory(Vector3 spawnPos)
        {
            if (rippleEffectPrefab == null) return;

            float now = Time.time;
            float minDistSqr = rippleMinDistance * rippleMinDistance;

            foreach (var info in recentRipples)
            {
                // XZ-plane distance only (ignore Y)
                Vector2 a = new Vector2(info.pos.x, info.pos.z);
                Vector2 b = new Vector2(spawnPos.x, spawnPos.z);
                float distSqr = (a - b).sqrMagnitude;
                float dt = now - info.time;

                if (distSqr <= minDistSqr && dt <= rippleMinDelay)
                {
                    return;
                }
            }

            var ripple = Instantiate(rippleEffectPrefab, spawnPos, Quaternion.identity);
            Destroy(ripple, rippleLifetime);

            recentRipples.Enqueue(new RippleInfo { pos = spawnPos, time = now });
            // maintain at most rippleHistoryCount entries
            while (recentRipples.Count > rippleHistoryCount) recentRipples.Dequeue();
        }
        void PlayAudio(AudioClip clip, Vector3 position, string name = "Audio Source", float pitch = 1f)
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
        void OnDisable()
        {
            insideCount.Clear();
            recentRipples.Clear();
        }
    }
}