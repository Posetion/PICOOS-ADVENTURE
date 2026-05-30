using Supercyan.AnimalPeopleSample;
using UnityEngine;

namespace LonelyWolf
{
    /// <summary>
    /// Hides the default human mesh and turns off the wolf's own movement parts
    /// so only ThirdPersonController on the parent moves the character.
    /// </summary>
    [DisallowMultipleComponent]
    public class WolfPlayerSetup : MonoBehaviour
    {
        [SerializeField] private string humanMeshChildName = "Armature_Mesh";
        [SerializeField] private bool disableWolfMovementOnChildren = true;

        private void Awake()
        {
            Transform humanMesh = transform.Find(humanMeshChildName);
            if (humanMesh != null)
            {
                humanMesh.gameObject.SetActive(false);
            }

            if (!disableWolfMovementOnChildren)
            {
                return;
            }

            foreach (Rigidbody rb in GetComponentsInChildren<Rigidbody>(true))
            {
                rb.isKinematic = true;
                rb.detectCollisions = false;
            }

            foreach (Collider col in GetComponentsInChildren<Collider>(true))
            {
                if (col is CharacterController)
                {
                    continue;
                }

                col.enabled = false;
            }

            foreach (SimpleSampleCharacterControl wolfMove in GetComponentsInChildren<SimpleSampleCharacterControl>(true))
            {
                wolfMove.enabled = false;
            }

            foreach (Animator childAnimator in GetComponentsInChildren<Animator>(true))
            {
                if (childAnimator.gameObject == gameObject)
                {
                    continue;
                }

                childAnimator.enabled = false;
            }
        }
    }
}
