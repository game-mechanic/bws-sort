using DG.Tweening;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

public class Bubble : MonoBehaviour
{
    const float PhaseDiff = 90 * Mathf.Deg2Rad;
    [SerializeField] byte index;
    [SerializeField] Transform viusal;
    [SerializeField] SpriteRenderer bg;
    [SerializeField] Color bgColor = Color.white;
    [SerializeField] GameObject highlightImage;
    [SerializeField] List<TextMeshPro> textUIs;
    [SerializeField] TextMeshPro categoryText;
    [SerializeField] List<string> names;
    Rigidbody2D rb;
    CircleCollider2D col;
    SortingGroup sortingGroup;
    float bounceAmplitude;
    float bounceDuration;
    bool isBouncing = false;
    float randomPhaseDiff;
    float randomTextPhaseDiff;
    float time = 0f;
    Vector3 startScale;
    [SerializeField] private BubbleType category;

    public RigidbodyType2D IsKinematic { get => rb.bodyType; set => rb.bodyType = value; }
    public float Radius => col.radius;

    public byte Index { get => index; }
    public BubbleType Category { get => category; set => category = value; }
    public List<string> Names { get => names; }
    public Vector3[] textPositions;

    private void Start()
    {
        CategoryManager.Instance.RegisterCategory(Category);
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<CircleCollider2D>();
        sortingGroup = GetComponent<SortingGroup>();
        startScale = viusal.transform.localScale;
        randomPhaseDiff = Random.Range(0, 90) * Mathf.Deg2Rad;
        randomTextPhaseDiff = Random.Range(0, 360) * Mathf.Deg2Rad;
        textPositions = new Vector3[textUIs.Count];
        for (int i = 0; i < textUIs.Count; i++)
        {
            textPositions[i] = textUIs[i].transform.localPosition;
        }
        Redraw();
    }
    private void OnValidate()
    {
        if (bg != null)
            bg.color = bgColor;
        Redraw();
    }

    private void Redraw()
    {
        for (int i = 0; i < Names.Count; i++)
        {
            textUIs[i].text = Names[i];
        }
    }

    private void Update()
    {
        if (!isBouncing)
        {
            float x = (Mathf.Sin((Time.time + randomPhaseDiff) * GameSettings.Instance.BreathingSpeed) + 0.5f) * GameSettings.Instance.BreathingAplitude;
            float y = (Mathf.Cos((Time.time + PhaseDiff + randomPhaseDiff) * GameSettings.Instance.BreathingSpeed) + 0.5f) * GameSettings.Instance.BreathingAplitude;
            Vector3 t = startScale + new Vector3(x, y: y, 0);

            viusal.transform.localScale = Vector3.Lerp(viusal.transform.localScale, t, GameSettings.Instance.LerpSpeeed * Time.deltaTime);
            TextBreathing();
            return;
        }

        time += Time.deltaTime;
        float tt = (time / bounceDuration) * GameSettings.Instance.MaxBounces;
        float bounceIntensity = 1 - tt;
        float rad = tt * Mathf.PI * 2;


        float sin = (Mathf.Sin(rad) + 0.5f) * bounceAmplitude;
        float cos = (Mathf.Cos(rad + PhaseDiff) + 0.5f) * bounceAmplitude;
        Vector3 targetScale = startScale + new Vector3(cos, y: sin, 0) * bounceIntensity;
        viusal.transform.localScale = Vector3.Lerp(viusal.transform.localScale, targetScale, GameSettings.Instance.LerpSpeeed * Time.deltaTime);
        if (tt >= 1)
        {
            isBouncing = false;
            time = 0;
            viusal.transform.DOKill();
            viusal.transform.DOScale(startScale, 0.05f);
        }
    }
    public void Bounce()
    {
        isBouncing = true;
        time = 0;
        this.bounceAmplitude = GameSettings.Instance.MaxBounceAmplitude;
        this.bounceDuration = GameSettings.Instance.BounceTime;
    }
    public void Bounce(float bounceAmplitude, float duration)
    {
        isBouncing = true;
        time = 0;
        this.bounceAmplitude = bounceAmplitude;
        this.bounceDuration = duration;
    }
    public void SetCollider(bool active)
    {
        col.enabled = active;
    }

    public void SetName(List<string> name)
    {
        Names.Clear();
        for (int i = 0; i < name.Count; i++)
        {
            Names.Add(name[i]);
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
    private void TextBreathing()
    {
        for (int i = 0; i < textUIs.Count; i++)
        {
            var x = Mathf.Sin((Time.time * GameSettings.Instance.TextBreathingSpeed) + randomTextPhaseDiff) * 0.05f;
            var y = Mathf.Sin((Time.time * .5f * GameSettings.Instance.TextBreathingSpeed) + randomTextPhaseDiff) * 0.1f;
            textUIs[i].transform.localPosition = textPositions[i] + new Vector3(x, y, 0);
        }
    }
    public void Highlight(bool v)
    {
        highlightImage.SetActive(v);
    }
    private void OnDrawGizmosSelected()
    {
        if (col == null)
            col = GetComponent<CircleCollider2D>();
        Gizmos.DrawSphere(transform.position, radius: Radius);
    }

    public void Blast()
    {
        if (categoryText != null)
            categoryText.text = Category.name;
        Sequence blastSequence = DOTween.Sequence();
        foreach (var text in textUIs)
        {
            text.transform.DOKill();
            blastSequence.Join(text.transform.DOScale(0, 0.3f).SetEase(Ease.InBack));
        }
        blastSequence.AppendCallback(() =>
        {
            if (categoryText != null)
            {
                categoryText.gameObject.SetActive(true);
                categoryText.transform.localScale = Vector3.zero;
            }
        });
        if (categoryText != null)
        {
            blastSequence.Append(categoryText.transform.DOScale(.1f, 0.2f).SetEase(Ease.OutBounce));
            blastSequence.AppendInterval(0.5f);
            blastSequence.Append(categoryText.transform.DOScale(0, 0.2f).SetEase(Ease.InBounce));
        }
        blastSequence.AppendCallback(() =>
        {
            //TODO: particle effect
            Destroy(gameObject);
        });
    }
}
