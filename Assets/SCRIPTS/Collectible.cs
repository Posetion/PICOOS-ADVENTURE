using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering.Universal;

public class Collectible : MonoBehaviour
{
    private bool collected = false; // Prevent double collection


    public GameManager GameManager;
    [Header("Visual Feedback")]
    public float destroyDelay = 0.5f; // Delay before destroying the collectible after collection45=-
    public GameObject[] collectibleModels;
    public ParticleSystem collectedVFX;


    public UnityEvent OnCollectedEvent;

    private void Start()
    {
        // Make sure the collider is a trigger so the wolf passes through it
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }

        if (collectibleModels != null && collectibleModels.Length > 0)
        {
            int index = Random.Range(0, collectibleModels.Length);

            foreach (GameObject model in collectibleModels)
            {
                model.SetActive(false);
            }

            collectibleModels[index].gameObject.SetActive(true);
        }
    }

    private void OnEnable()
    {
        StartCoroutine(CheckPlayerOverlapNextFrame());
    }

    private IEnumerator CheckPlayerOverlapNextFrame()
    {
        yield return null;

        if (collected) yield break;

        Collider col = GetComponent<Collider>();
        if (col == null || !col.enabled || !col.isTrigger) yield break;

        Bounds bounds = col.bounds;
        Collider[] overlaps = Physics.OverlapBox(
            bounds.center,
            bounds.extents,
            transform.rotation,
            ~0,
            QueryTriggerInteraction.Collide);

        for (int i = 0; i < overlaps.Length; i++)
        {
            if (IsPlayer(overlaps[i]))
            {
                Collect();
                yield break;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        TryCollect(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryCollect(other);
    }

    private void TryCollect(Collider other)
    {
        if (!collected && IsPlayer(other))
            Collect();
    }

    private static bool IsPlayer(Collider other)
    {
        return other.CompareTag("Player") || other.transform.root.CompareTag("Player");
    }

    private void Collect()
    {
        Debug.Log("Collect() called on " + gameObject.name);
        if (AudioManager.Instance == null)
        {
            Debug.LogError("AudioManager Instance is NULL!");
        }
        else
        {
            AudioManager.Instance.PlaySFX("Collect");
        }
        collected = true;

        if (GameManager.instance != null)
        {
            GameManager.instance.AddCollectible();
        }
        Debug.Log("1 " + gameObject.name);

        if (collectedVFX != null)
        {
            Instantiate(collectedVFX, transform.position, Quaternion.identity);
        }
        Debug.Log("2" + gameObject.name);

        OnCollectedEvent.Invoke();

        AudioManager.Instance.PlaySFX("Collect");
        Debug.Log("Destroying " + gameObject.name);
        Destroy(gameObject);
    }

}
