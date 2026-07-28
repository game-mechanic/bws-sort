using DG.Tweening;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization.Settings;
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
        public AnimationClip animationClip;
        public bool showBothTxtAndImg = false;
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
    [SerializeField] bool canChangeColor = true;
    [SerializeField] GameObject ghost;
    Rigidbody2D rb;
    Collider2D col;
    [SerializeField] float radius = 0.5f;
    SortingGroup sortingGroup;
    float bounceAmplitude;
    float bounceDuration;
    bool isBouncing = false;
    float randomPhaseDiff;
    float randomTextPhaseDiff;
    float time = 0f;
    Vector3 startScale;
    [SerializeField] private BubbleType category;
    public UnityEvent OnBounce = new();
    public RigidbodyType2D IsKinematic { get => rb.bodyType; set => rb.bodyType = value; }
    public float Radius => radius;

    public byte Index { get => index; }
    public BubbleType Category { get => category; set => category = value; }
    public List<Data> Names { get => names; }
    public bool CanChangeColor { get => canChangeColor; }

    Vector3[] textPositions;
    private GameObject ghostInstance;
    public int SortingOrder
    {
        get
        {
            if (sortingGroup == null)
                sortingGroup = GetComponent<SortingGroup>();
            return sortingGroup.sortingOrder;
        }

        set
        {
            if (sortingGroup == null)
                sortingGroup = GetComponent<SortingGroup>();
            sortingGroup.sortingOrder = value;
        }
    }

    private void Awake()
    {
        startScale = viusal.localScale;
    }

    private void Start()
    {
        RestorePositions();
        //yield return null;
        if (category != null)
            CategoryManager.Instance.RegisterCategory(Category);
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        sortingGroup = GetComponent<SortingGroup>();

        randomPhaseDiff = Random.Range(0, 90) * Mathf.Deg2Rad;
        randomTextPhaseDiff = Random.Range(0, 360) * Mathf.Deg2Rad;
        Redraw();
        //yield return null;
        //        foreach (var name in textUIs)
        //        {
        //#if UNITY_EDITOR
        //            SceneVisibilityManager.instance.Hide(name.textUIs.gameObject, true);
        //#endif
        //        }
    }
    private void OnDisable()
    {
        viusal.DOKill();
    }
    public void RestorePositions()
    {
        if (names.Count == 0) return;
        textPositions = new Vector3[textUIs.Count];
        for (int i = 0; i < textUIs.Count; i++)
        {
            textPositions[i] = names[i].icon == null ? textUIs[i].textUIs.transform.localPosition : textUIs[i].bg.transform.localPosition;
        }
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
            // 1. Always evaluate and set the text value first so it's ready if needed
            if (GameSettings.Instance.SelectedLanguage.ToString() == "en")
            {
                textUIs[i].textUIs.text = Names[i].name;
            }
            else
            {
                textUIs[i].textUIs.text =
                    LocalizationSettings.StringDatabase.GetLocalizedString(
                       GameSettings.Instance.TableReference,
                        Names[i].name
                    );
            }

            // 2. Handle the visibility logic based on the new boolean and icon existence
            if (Names[i].showBothTxtAndImg && Names[i].icon != null)
            {
                // Show both
                textUIs[i].bg.sprite = Names[i].icon;
                if (GameSettings.Instance.CanAnimateSprite && Names[i].animationClip != null)
                {
                    Animator animator = textUIs[i].bg.GetComponent<Animator>();
                    animator.runtimeAnimatorController = GameSettings.Instance.AnimatorController;
                    animator.Play(Names[i].animationClip.name);
                }
                textUIs[i].bg.gameObject.SetActive(true);
                textUIs[i].textUIs.gameObject.SetActive(true);
            }
            else if (Names[i].animationClip != null)
            {
                // Show animation only
                if (GameSettings.Instance.CanAnimateSprite)
                {
                    Animator animator = textUIs[i].bg.GetComponent<Animator>();
                    animator.runtimeAnimatorController = GameSettings.Instance.AnimatorController;
                    animator.Play(Names[i].animationClip.name);
                }
                textUIs[i].textUIs.gameObject.SetActive(false);
                textUIs[i].bg.gameObject.SetActive(true);
            }
            else if (Names[i].icon != null)
            {
                // Show image only
                textUIs[i].bg.sprite = Names[i].icon;
                if (GameSettings.Instance.CanAnimateSprite && Names[i].animationClip != null)
                {
                    Animator animator = textUIs[i].bg.GetComponent<Animator>();
                    animator.runtimeAnimatorController = GameSettings.Instance.AnimatorController;
                    animator.Play(Names[i].animationClip.name);
                }
                textUIs[i].textUIs.gameObject.SetActive(false);
                textUIs[i].bg.gameObject.SetActive(true);
            }
            else
            {
                // Show text only
                textUIs[i].bg.gameObject.SetActive(false);
                textUIs[i].textUIs.gameObject.SetActive(true);
            }
        }
    }

    bool isScalingExternally = false;

    private void Update()
    {
        if (isScalingExternally) return;

        if (!isBouncing)
        {
            float x = (Mathf.Sin((Time.time + randomPhaseDiff) * GameSettings.Instance.BreathingSpeed) + 0.5f) * GameSettings.Instance.BreathingAplitude;
            float y = (Mathf.Cos((Time.time + PhaseDiff + randomPhaseDiff) * GameSettings.Instance.BreathingSpeed) + 0.5f) * GameSettings.Instance.BreathingAplitude;
            Vector3 t = startScale + new Vector3(x, y: y, 0);

            viusal.localScale = Vector3.Lerp(viusal.localScale, t, GameSettings.Instance.LerpSpeeed * Time.deltaTime);
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
        viusal.transform.localScale = Vector3.Lerp(viusal.localScale, targetScale, GameSettings.Instance.LerpSpeeed * Time.deltaTime);
        if (tt >= 1)
        {
            isBouncing = false;
            time = 0;
            OnBounce?.Invoke();
            viusal.DOKill();
            viusal.DOScale(startScale, 0.05f).SetTarget(viusal).SetLink(gameObject);
        }
    }

    public Tween AnimateScaleFrom(Vector3 fromScale, float duration)
    {
        isBouncing = false;
        isScalingExternally = true;
        viusal.DOKill();
        viusal.localScale = fromScale;
        return viusal.DOScale(startScale, duration).SetEase(Ease.OutBack).SetLink(gameObject).OnComplete(() => {
            isScalingExternally = false;
        });
    }

    public void Bounce()
    {
        isBouncing = true;
        time = 0;
        this.bounceAmplitude = GameSettings.Instance.MaxBounceAmplitude;
        this.bounceDuration = GameSettings.Instance.BounceTime;
    }
    public void Bounce(float bounceAmplitude)
    {
        isBouncing = true;
        time = 0;
        this.bounceAmplitude = bounceAmplitude;
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
        this.ghostInstance = Instantiate(ghost, transform.position, Quaternion.identity);
    }
    public void EndDrag()
    {
        IsKinematic = RigidbodyType2D.Dynamic;
        SetCollider(true);
        sortingGroup.sortingOrder = 2;
    }
    private void TextBreathing()
    {
        if (textPositions == null || textPositions.Length <= 0) return;

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
        Gizmos.DrawSphere(transform.position, radius: Radius);
    }

    public void Blast(System.Action OnBlastComplete = null)
    {
        if (categoryText != null)
        {
            categoryText.text = Category.name;
            if (GameSettings.Instance.SelectedLanguage.ToString() == "en")
            {
                categoryText.text = Category.name;
            }
            else
            {
                categoryText.text =
                    LocalizationSettings.StringDatabase.GetLocalizedString(
                       GameSettings.Instance.TableReference,
                        Category.name
                    );
            }
        }

        Sequence blastSequence = DOTween.Sequence();
        float delayStep = 0.08f;
        int index = 0;

        foreach (var text in textUIs)
        {
            Transform bg = text.bg.transform;
            Transform txt = text.textUIs.transform;

            bg.DOKill();
            txt.DOKill();

            // Store initial scale
            Vector3 bgStartScale = bg.localScale;
            Vector3 txtStartScale = txt.localScale;

            float delay = index * delayStep;

            Sequence textSeq = DOTween.Sequence();

            textSeq.AppendInterval(delay);

            // Optional tiny anticipation (feels nicer than instant shrink)
            textSeq.Append(
                bg.DOScale(bgStartScale * 1.05f, 0.1f).SetEase(Ease.OutSine).SetLink(gameObject)
            );

            textSeq.Join(
                txt.DOScale(txtStartScale * 1.05f, 0.1f).SetEase(Ease.OutSine).SetLink(gameObject)
            );

            // Main disappear (shrink)
            textSeq.Append(
                bg.DOScale(Vector3.zero, 0.25f).SetEase(Ease.InBack).SetLink(gameObject)
            );

            textSeq.Join(
                txt.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack).SetLink(gameObject)
            );

            blastSequence.Join(textSeq);

            index++;
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
            Transform t = categoryText.transform;

            t.DOKill();

            // Store original scale
            Vector3 startScale = t.localScale;

            Sequence seq = DOTween.Sequence();

            // Start slightly smaller for pop-in
            t.localScale = startScale * 0.7f;

            seq.AppendInterval(0.15f);

            // Pop in with overshoot
            seq.Append(
                t.DOScale(startScale * 1.1f, 0.35f)
                .SetEase(Ease.OutBack).SetLink(gameObject)
            );

            // Settle to normal
            seq.Append(
                t.DOScale(startScale, 0.15f)
                .SetEase(Ease.OutSine).SetLink(gameObject)
            );

            // Short, snappy pause (not too long)
            seq.AppendInterval(0.4f);

            // Exit with slight anticipation
            seq.Append(
                t.DOScale(startScale * 1.05f, 0.1f)
                .SetEase(Ease.OutSine).SetLink(gameObject)
            );

            seq.Append(
                t.DOScale(Vector3.zero, 0.25f)
                .SetEase(Ease.InBack).SetLink(gameObject)
            );

            blastSequence.Append(seq);
        }
        blastSequence.AppendCallback(() =>
        {
            ParticlePool.PlayRevealFx(transform.position);
            //CategoryManager.Instance.SpawnNewCategories();
            Destroy(gameObject);
            OnBlastComplete?.Invoke();
        });
    }

    internal void SetColor(Color bubbleColor)
    {
        bgColor = bubbleColor;
        if (bg != null)
            bg.color = bgColor;
    }
    private void OnDrawGizmos()
    {
        //Gizmos.color = Color.white;
        //Gizmos.DrawSphere(transform.position, radius: Radius);
        if (Names.Count == 1)
        {
            string name = LocalizationSettings.StringDatabase.GetLocalizedString(
                           GameSettings.Instance.TableReference,
                            Names[0].name,
                            GameSettings.Instance.EnglishLocale
                        );

            GUIStyle style = null;
#if UNITY_EDITOR
            if (style == null)
            {
                style = new GUIStyle();
                style.normal.textColor = Color.white;
                style.fontSize = 10;
                style.fontStyle = FontStyle.Bold;
                style.alignment = TextAnchor.MiddleCenter;
            }

            Handles.Label(
                transform.position + Vector3.back,
                name,
                style
            );
#endif
        }
    }

    internal void BlastGhost()
    {
        Destroy(ghostInstance);
        ParticlePool.PlayRevealFx(ghostInstance.transform.position);
    }

    internal void ReturnBack()
    {
        transform.DOMove(ghostInstance.transform.position, 0.2f).SetLink(gameObject).OnComplete(() =>
        {
            EndDrag();
            Destroy(ghostInstance);
        });
    }
}
