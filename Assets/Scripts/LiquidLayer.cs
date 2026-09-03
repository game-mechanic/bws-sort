using System.Collections;
using UnityEngine;

public enum LiquidState
{
    FILLED,
    EMPTY
}

public class LiquidLayer : MonoBehaviour
{
    [SerializeField] private SpriteRenderer liquidRenderer;
    [SerializeField] private Transform liquidVol;
    [SerializeField] private float minLiqVol;
    [SerializeField] private float maxLiqVol;
    [SerializeField] private float liquidFlowSpeed = 5f;

    [HideInInspector]
    public GameSettings.LiquidColorType selectedColorType;

    public SpriteRenderer LiquidRenderer => liquidRenderer;

    private Coroutine activeRoutine;

    public LiquidState CurrentState
    {
        get
        {
            if (liquidVol != null && liquidVol.localScale.y >= maxLiqVol - 0.01f)
            {
                return LiquidState.FILLED;
            }
            return LiquidState.EMPTY;
        }
    }

    public void FillLiquid()
    {
        if (activeRoutine != null) StopCoroutine(activeRoutine);
        activeRoutine = StartCoroutine(FillLiquidRoutine());
    }

    public void EmptyLiquid()
    {
        if (activeRoutine != null) StopCoroutine(activeRoutine);
        activeRoutine = StartCoroutine(EmptyLiquidRoutine());
    }

    public void SetLiquidInstantly(LiquidState state, Color color, GameSettings.LiquidColorType colorType)
    {
        if (activeRoutine != null) StopCoroutine(activeRoutine);
        selectedColorType = colorType;
        if (liquidRenderer != null) liquidRenderer.color = color;

        Vector3 scale = liquidVol.localScale;
        scale.y = state == LiquidState.FILLED ? maxLiqVol : 0f;
        liquidVol.localScale = scale;
    }

    public void SetLiquidInstantly(LiquidState state)
    {
        if (activeRoutine != null) StopCoroutine(activeRoutine);
        Vector3 scale = liquidVol.localScale;
        scale.y = state == LiquidState.FILLED ? maxLiqVol : 0f;
        liquidVol.localScale = scale;
    }

    private IEnumerator FillLiquidRoutine()
    {
        Vector3 scale = liquidVol.localScale;
        while (Mathf.Abs(scale.y - maxLiqVol) > 0.001f)
        {
            scale.y = Mathf.Lerp(scale.y, maxLiqVol, liquidFlowSpeed * Time.deltaTime);
            liquidVol.localScale = scale;
            yield return null;
        }
        
        scale.y = maxLiqVol;
        liquidVol.localScale = scale;
    }

    private IEnumerator EmptyLiquidRoutine()
    {
        Vector3 scale = liquidVol.localScale;
        while (Mathf.Abs(scale.y - minLiqVol) > 0.001f)
        {
            scale.y = Mathf.Lerp(scale.y, minLiqVol, liquidFlowSpeed * Time.deltaTime);
            liquidVol.localScale = scale;
            yield return null;
        }
        
        // After reaching minLiqVol instantly change the y scale to 0
        scale.y = 0f;
        liquidVol.localScale = scale;
    }
}
