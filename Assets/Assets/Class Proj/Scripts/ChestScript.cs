using UnityEngine;

public class ChestController : MonoBehaviour
{
    public static bool hasKey = false;

    [SerializeField] private Animator animator;
    private bool opened = false;

    private void Awake()
    {
        // Fresh start every time the scene loads: chest closed, no key.
        hasKey = false;

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
    }

    public void OpenChest()
    {
        if (opened) return;

        if (!hasKey)
        {
            Debug.Log("You need the key!");
            return;
        }

        opened = true;
        animator.SetTrigger("Open");

        Debug.Log("Chest Opened!");
    }
}