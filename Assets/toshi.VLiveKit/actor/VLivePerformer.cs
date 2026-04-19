using UnityEngine;

public class VLivePerformer : MonoBehaviour
{
    [Header("Performer Core")]
    [SerializeField] private Animator performerAnimator;

    [Header("Display")]
    [SerializeField] private string performerName = "Performer";

    public Animator PerformerAnimator => performerAnimator;

    public string PerformerName
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(performerName))
                return performerName;

            return gameObject.name;
        }
    }

    private void Reset()
    {
        if (performerAnimator == null)
        {
            performerAnimator = GetComponentInChildren<Animator>();
        }

        if (string.IsNullOrWhiteSpace(performerName))
        {
            performerName = gameObject.name;
        }
    }
}