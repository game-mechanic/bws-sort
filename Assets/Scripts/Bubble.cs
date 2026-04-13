using DG.Tweening;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

public class Bubble : MonoBehaviour
{
    const float PhaseDiff = 90 * Mathf.Deg2Rad;
    [SerializeField] Transform viusal;
    [SerializeField] List<TextMeshProUGUI> textUIs;

    List<string> names;
    Rigidbody2D rb;
    Collider2D col;
    SortingGroup sortingGroup;
    bool isBouncing = false;
    float time = 0f;
    Vector3 startScale;
    public RigidbodyType2D IsKinematic { get => rb.bodyType; set => rb.bodyType = value; }
    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        sortingGroup = GetComponent<SortingGroup>();
        startScale = viusal.transform.localScale;
    }
    private void Update()
    {
        if (!isBouncing) return;

        time += Time.deltaTime;
        float tt = (time / GameSettings.Instance.BounceTime) * GameSettings.Instance.MaxBounces;
        float bounceIntensity = 1 - tt;
        float rad = tt * Mathf.PI * 2;


        float sin = (Mathf.Sin(rad) + 0.5f) * GameSettings.Instance.MaxBounceAmplitude;
        float cos = (Mathf.Cos(rad + PhaseDiff) + 0.5f) * GameSettings.Instance.MaxBounceAmplitude;
        Vector3 targetScale = startScale + new Vector3(cos, y: sin, 0) * bounceIntensity;
        viusal.transform.localScale = Vector3.Lerp(viusal.transform.localScale, targetScale, GameSettings.Instance.LerpSpeeed * Time.deltaTime);
        if (tt >= 1)
        {
            isBouncing = false;
            time = 0;
            viusal.transform.DOScale(startScale, 0.05f);
        }
    }
    [EditorButton]
    public void Bounce()
    {
        isBouncing = true;
        time = 0;
    }
    public void SetCollider(bool active)
    {
        col.enabled = active;
    }

    public void SetName(string[] name)
    {
        names.Clear();
        for (int i = 0; i < name.Length; i++)
        {
            names.Add(name[i]);
            textUIs[i].text = name[i];
        }
    }
    public void StartDrag()
    {
        IsKinematic = RigidbodyType2D.Kinematic;
        SetCollider(false);
        sortingGroup.sortingOrder = 100;
    }
    public void EndDrag()
    {
        IsKinematic = RigidbodyType2D.Dynamic;
        SetCollider(true);
        sortingGroup.sortingOrder = 2;
    }
}
