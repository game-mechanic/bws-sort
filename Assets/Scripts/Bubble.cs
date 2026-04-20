using DG.Tweening;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

public class Bubble : MonoBehaviour
{
    [System.Serializable]
    public class visuals
    {
        public SpriteRenderer bg;
        public TextMeshPro textUIs;
    }
    [System.Serializable]
    public class Data
    {
        public string name;
        public Sprite icon;
    }
    const float PhaseDiff = 90 * Mathf.Deg2Rad;
    [SerializeField] byte index;
    [SerializeField] Transform viusal;
    [SerializeField] SpriteRenderer bg;
    [SerializeField] Color bgColor = Color.white;
    [SerializeField] GameObject highlightImage;
    [SerializeField] List<visuals> textUIs;
    [SerializeField] TextMeshPro categoryText;
    [SerializeField] List<Data> names;
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
    public List<Data> Names { get => names; }
    Vector3[] textPositions;

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
            textPositions[i] = names[i].icon == null ? textUIs[i].textUIs.transform.localPosition : textUIs[i].bg.transform.localPosition;
        }
        Redraw();
    }
    [EditorButton]
    public void Refresh()
    {
        if (bg != null)
            bg.color = bgColor;
        Redraw();
    }

    private void Redraw()
    {
        for (int i = 0; i < Names.Count; i++)
        {
            if (Names[i].icon == null)
            {
                textUIs[i].textUIs.text = Names[i].name;
                textUIs[i].bg.gameObject.SetActive(false);
                textUIs[i].textUIs.gameObject.SetActive(true);
            }
            else
            {
                textUIs[i].bg.sprite = Names[i].icon;
                textUIs[i].textUIs.gameObject.SetActive(false);
                textUIs[i].bg.gameObject.SetActive(true);
            }
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

    public void SetName(List<Data> name)
    {
        Names.Clear();
        for (int i = 0; i < name.Count; i++)
        {
            Names.Add(name[i]);
        }
        Redraw();
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
        Vector3 scaleModifier = viusal.localScale;

        for (int i = 0; i < textUIs.Count; i++)
        {
            if (textUIs[i] == null) continue;

            var x = (Mathf.Sin((Time.time * GameSettings.Instance.TextBreathingSpeed) + randomTextPhaseDiff)) * 0.05f;
            var y = (Mathf.Sin((Time.time * .5f * GameSettings.Instance.TextBreathingSpeed) + randomTextPhaseDiff)) * 0.1f;


            Vector3 offset = new Vector3(x / scaleModifier.x, y / scaleModifier.y, 0);

            if (textUIs[i].bg != null)
                textUIs[i].bg.transform.localPosition = Vector3.Lerp(textUIs[i].bg.transform.localPosition, textPositions[i] + offset, GameSettings.Instance.LerpSpeeed * Time.deltaTime);

            if (textUIs[i].textUIs != null)
                textUIs[i].textUIs.transform.localPosition = Vector3.Lerp(textUIs[i].textUIs.transform.localPosition, textPositions[i] + offset, GameSettings.Instance.LerpSpeeed * Time.deltaTime);
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
        //Gizmos.DrawSphere(transform.position, radius: Radius);
        if (textPositions == null) return;
        for (int i = 0; i < textPositions.Length; i++)
        {
            Gizmos.DrawWireSphere(textPositions[i], radius: 0.1f);
        }
    }

    public void Blast()
    {
        if (categoryText != null)
            categoryText.text = Category.name;
        CategoryManager.Instance.hide();
        Sequence blastSequence = DOTween.Sequence();
        foreach (var text in textUIs)
        {
            text.bg.transform.DOKill();
            text.textUIs.transform.DOKill();
            blastSequence.Join(text.bg.transform.DOScale(0, 0.5f).SetEase(Ease.OutSine));
            blastSequence.Join(text.textUIs.transform.DOScale(0, 0.5f).SetEase(Ease.OutSine));
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
            blastSequence.AppendInterval(0.2f);
            blastSequence.Append(categoryText.transform.DOScale(.1f, 0.5f).SetEase(Ease.OutBounce));
            blastSequence.AppendInterval(1);
            blastSequence.Append(categoryText.transform.DOScale(0, 0.3f).SetEase(Ease.InSine));
        }
        blastSequence.AppendCallback(() =>
        {
            ParticlePool.PlayRevealFx(transform.position);
            Destroy(gameObject);
        });
    }
}
