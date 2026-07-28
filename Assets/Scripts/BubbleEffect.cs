using System.Collections;
using UnityEngine;

public class BubbleEffect : MonoBehaviour
{
    [Header("Bubble Effect Settings")]
    [Tooltip("Enable or disable the bubble effect feature.")]
    public bool enableBubbleEffect = true;

    [Tooltip("Assign the Bubble Effect GameObject here.")]
    public GameObject bubbleEffectObject;

    [Tooltip("How long the bubble effect stays active.")]
    public float activeDuration = 3f;

    private Coroutine bubbleRoutine;

    private void Start()
    {
        if (bubbleEffectObject != null)
            bubbleEffectObject.SetActive(false);
    }

    public void OnBubblePop()
    {
        if (!enableBubbleEffect)
            return;

        if (bubbleRoutine != null)
            StopCoroutine(bubbleRoutine);

        bubbleRoutine = StartCoroutine(ActivateBubbleEffect());
    }

    private IEnumerator ActivateBubbleEffect()
    {
        if (bubbleEffectObject == null)
            yield break;

        bubbleEffectObject.SetActive(true);

        yield return new WaitForSeconds(activeDuration);

        bubbleEffectObject.SetActive(false);
        bubbleRoutine = null;
    }
}