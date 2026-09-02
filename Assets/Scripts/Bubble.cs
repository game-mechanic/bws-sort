using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.Rendering;

public class Bubble : MonoBehaviour
{
    // =========================================================
    // DATA
    // =========================================================

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

    // =========================================================
    // CONSTANTS
    // =========================================================

    const float PhaseDiff = 90 * Mathf.Deg2Rad;

    // =========================================================
    // BUBBLE REFERENCES
    // =========================================================

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

    // =========================================================
    // SAND
    // =========================================================

    [Header("Falling Sand")]
    [SerializeField] private FallingSandController fallingSandController;

    // =========================================================
    // PHYSICS
    // =========================================================

    Rigidbody2D rb;
    Collider2D col;

    [SerializeField] float radius = 0.5f;

    SortingGroup sortingGroup;

    // =========================================================
    // BOUNCE
    // =========================================================

    float bounceAmplitude;
    float bounceDuration;

    bool isBouncing = false;

    float randomPhaseDiff;
    float randomTextPhaseDiff;

    float time = 0f;

    Vector3 startScale;

    // =========================================================
    // CATEGORY
    // =========================================================

    [SerializeField] private BubbleType category;

    // =========================================================
    // PROPERTIES
    // =========================================================

    public RigidbodyType2D IsKinematic
    {
        get
        {
            return rb.bodyType;
        }

        set
        {
            rb.bodyType = value;
        }
    }

    public float Radius => radius;

    public byte Index => index;

    public BubbleType Category
    {
        get => category;
        set => category = value;
    }

    public List<Data> Names => names;

    public bool CanChangeColor => canChangeColor;

    // =========================================================
    // INTERNAL
    // =========================================================

    Vector3[] textPositions;

    private GameObject ghostInstance;

    // =========================================================
    // START
    // =========================================================

    private IEnumerator Start()
    {
        CategoryManager.Instance.RegisterCategory(Category);

        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        sortingGroup = GetComponent<SortingGroup>();

        startScale = viusal.localScale;

        randomPhaseDiff =
            Random.Range(0, 90) * Mathf.Deg2Rad;

        randomTextPhaseDiff =
            Random.Range(0, 360) * Mathf.Deg2Rad;

        RestorePositions();

        Redraw();

        // -----------------------------------------------------
        // Automatically find FallingSandController if not
        // assigned in Inspector.
        // -----------------------------------------------------

        if (fallingSandController == null)
        {
            fallingSandController =
                FindObjectOfType<FallingSandController>();
        }

        yield return null;

        foreach (var name in textUIs)
        {
#if UNITY_EDITOR
            SceneVisibilityManager.instance.Hide(
                name.textUIs.gameObject,
                true
            );
#endif
        }
    }

    // =========================================================
    // DISABLE
    // =========================================================

    private void OnDisable()
    {
        if (viusal != null)
            viusal.DOKill();
    }

    // =========================================================
    // RESTORE POSITIONS
    // =========================================================

    public void RestorePositions()
    {
        textPositions =
            new Vector3[textUIs.Count];

        for (int i = 0; i < textUIs.Count; i++)
        {
            textPositions[i] =
                names[i].icon == null
                    ? textUIs[i].textUIs.transform.localPosition
                    : textUIs[i].bg.transform.localPosition;
        }
    }

    // =========================================================
    // REFRESH
    // =========================================================

    [EditorButton]
    public void Refresh()
    {
        if (bg != null)
            bg.color = bgColor;

        Redraw();
    }

    // =========================================================
    // REDRAW
    // =========================================================

    private void Redraw()
    {
        for (int i = 0; i < Names.Count; i++)
        {
            // -------------------------------------------------
            // TEXT
            // -------------------------------------------------

            if (GameSettings.Instance.SelectedLanguage.ToString() == "en")
            {
                textUIs[i].textUIs.text =
                    Names[i].name;
            }
            else
            {
                textUIs[i].textUIs.text =
                    LocalizationSettings.StringDatabase.GetLocalizedString(
                        GameSettings.Instance.TableReference,
                        Names[i].name
                    );
            }

            // -------------------------------------------------
            // TEXT + IMAGE
            // -------------------------------------------------

            if (
                Names[i].showBothTxtAndImg &&
                Names[i].icon != null
            )
            {
                textUIs[i].bg.sprite =
                    Names[i].icon;

                if (
                    GameSettings.Instance.CanAnimateSprite &&
                    Names[i].animationClip != null
                )
                {
                    Animator animator =
                        textUIs[i].bg.GetComponent<Animator>();

                    animator.runtimeAnimatorController =
                        GameSettings.Instance.AnimatorController;

                    animator.Play(
                        Names[i].animationClip.name
                    );
                }

                textUIs[i].bg.gameObject.SetActive(true);
                textUIs[i].textUIs.gameObject.SetActive(true);
            }

            // -------------------------------------------------
            // ANIMATION ONLY
            // -------------------------------------------------

            else if (Names[i].animationClip != null)
            {
                if (GameSettings.Instance.CanAnimateSprite)
                {
                    Animator animator =
                        textUIs[i].bg.GetComponent<Animator>();

                    animator.runtimeAnimatorController =
                        GameSettings.Instance.AnimatorController;

                    animator.Play(
                        Names[i].animationClip.name
                    );
                }

                textUIs[i].textUIs.gameObject.SetActive(false);
                textUIs[i].bg.gameObject.SetActive(true);
            }

            // -------------------------------------------------
            // IMAGE ONLY
            // -------------------------------------------------

            else if (Names[i].icon != null)
            {
                textUIs[i].bg.sprite =
                    Names[i].icon;

                if (
                    GameSettings.Instance.CanAnimateSprite &&
                    Names[i].animationClip != null
                )
                {
                    Animator animator =
                        textUIs[i].bg.GetComponent<Animator>();

                    animator.runtimeAnimatorController =
                        GameSettings.Instance.AnimatorController;

                    animator.Play(
                        Names[i].animationClip.name
                    );
                }

                textUIs[i].textUIs.gameObject.SetActive(false);
                textUIs[i].bg.gameObject.SetActive(true);
            }

            // -------------------------------------------------
            // TEXT ONLY
            // -------------------------------------------------

            else
            {
                textUIs[i].bg.gameObject.SetActive(false);
                textUIs[i].textUIs.gameObject.SetActive(true);
            }
        }
    }

    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        if (!isBouncing)
        {
            float x =
                (
                    Mathf.Sin(
                        (Time.time + randomPhaseDiff) *
                        GameSettings.Instance.BreathingSpeed
                    )
                    + 0.5f
                )
                *
                GameSettings.Instance.BreathingAplitude;

            float y =
                (
                    Mathf.Cos(
                        (
                            Time.time +
                            PhaseDiff +
                            randomPhaseDiff
                        )
                        *
                        GameSettings.Instance.BreathingSpeed
                    )
                    + 0.5f
                )
                *
                GameSettings.Instance.BreathingAplitude;

            Vector3 target =
                startScale +
                new Vector3(x, y, 0);

            viusal.localScale =
                Vector3.Lerp(
                    viusal.localScale,
                    target,
                    GameSettings.Instance.LerpSpeeed *
                    Time.deltaTime
                );

            TextBreathing();

            return;
        }

        time += Time.deltaTime;

        float tt =
            time / bounceDuration;

        float bounceIntensity =
            1 - tt;

        float rad =
            tt *
            Mathf.PI *
            2 *
            GameSettings.Instance.MaxBounces;

        float sin =
            (
                Mathf.Sin(rad) +
                0.5f
            )
            *
            bounceAmplitude;

        float cos =
            (
                Mathf.Cos(rad + PhaseDiff) +
                0.5f
            )
            *
            bounceAmplitude;

        Vector3 targetScale =
            startScale +
            new Vector3(cos, sin, 0) *
            bounceIntensity;

        viusal.transform.localScale =
            Vector3.Lerp(
                viusal.transform.localScale,
                targetScale,
                GameSettings.Instance.LerpSpeeed *
                Time.deltaTime
            );

        if (tt >= 1)
        {
            isBouncing = false;

            time = 0;

            viusal.DOKill();

            viusal
                .DOScale(startScale, 0.05f)
                .SetTarget(viusal);
        }
    }

    // =========================================================
    // BOUNCE
    // =========================================================

    public void Bounce()
    {
        isBouncing = true;

        time = 0;

        bounceAmplitude =
            GameSettings.Instance.MaxBounceAmplitude;

        bounceDuration =
            GameSettings.Instance.BounceTime;
    }

    public void Bounce(
        float bounceAmplitude,
        float duration
    )
    {
        isBouncing = true;

        time = 0;

        this.bounceAmplitude =
            bounceAmplitude;

        this.bounceDuration =
            duration;
    }

    // =========================================================
    // COLLIDER
    // =========================================================

    public void SetCollider(bool active)
    {
        if (col != null)
            col.enabled = active;
    }

    // =========================================================
    // SET NAME
    // =========================================================

    public void SetName(List<Data> name)
    {
        Names.Clear();

        for (int i = 0; i < name.Count; i++)
        {
            Names.Add(name[i]);
        }

        Redraw();
    }

    // =========================================================
    // DRAG
    // =========================================================

    public void StartDrag()
    {
        IsKinematic =
            RigidbodyType2D.Kinematic;

        SetCollider(false);

        if (sortingGroup != null)
            sortingGroup.sortingOrder = 100;

        rb.linearVelocity =
            Vector2.zero;

        if (GameSettings.Instance.CanCreateGhost)
        {
            ghostInstance =
                Instantiate(
                    ghost,
                    transform.position,
                    Quaternion.identity
                );
        }
    }

    // =========================================================
    // END DRAG
    // =========================================================

    public void EndDrag()
    {
        IsKinematic =
            RigidbodyType2D.Dynamic;

        SetCollider(true);

        if (sortingGroup != null)
            sortingGroup.sortingOrder = -200;
    }

    // =========================================================
    // TEXT BREATHING
    // =========================================================

    private void TextBreathing()
    {
        if (!GameSettings.Instance.CanTextBreathe)
            return;

        Vector3 scaleModifier =
            viusal.localScale;

        for (int i = 0; i < textUIs.Count; i++)
        {
            if (textUIs[i] == null)
                continue;

            float x =
                Mathf.Sin(
                    Time.time *
                    GameSettings.Instance.TextBreathingSpeed +
                    randomTextPhaseDiff
                )
                *
                0.05f *
                0.5f;

            float y =
                Mathf.Sin(
                    Time.time *
                    0.5f *
                    GameSettings.Instance.TextBreathingSpeed +
                    randomTextPhaseDiff
                )
                *
                0.1f *
                0.5f;

            Vector3 offset =
                new Vector3(
                    x / scaleModifier.x,
                    y / scaleModifier.y,
                    0
                );

            if (textUIs[i].bg != null)
            {
                textUIs[i].bg.transform.localPosition =
                    Vector3.Lerp(
                        textUIs[i].bg.transform.localPosition,
                        textPositions[i] + offset,
                        GameSettings.Instance.LerpSpeeed *
                        Time.deltaTime
                    );
            }

            if (textUIs[i].textUIs != null)
            {
                textUIs[i].textUIs.transform.localPosition =
                    Vector3.Lerp(
                        textUIs[i].textUIs.transform.localPosition,
                        textPositions[i] + offset,
                        GameSettings.Instance.LerpSpeeed *
                        Time.deltaTime
                    );
            }
        }
    }

    // =========================================================
    // HIGHLIGHT
    // =========================================================

    public void Highlight(bool v)
    {
        if (highlightImage != null)
            highlightImage.SetActive(v);
    }

    // =========================================================
    // GIZMOS
    // =========================================================

    private void OnDrawGizmosSelected()
    {
        if (col == null)
            col = GetComponent<CircleCollider2D>();

        Gizmos.DrawSphere(
            transform.position,
            Radius
        );
    }

    // =========================================================
    // BLAST
    // =========================================================

    public void Blast(
        System.Action OnBlastComplete = null
    )
    {
        if (categoryText != null)
        {
            categoryText.text =
                Category.name;

            if (
                GameSettings.Instance.SelectedLanguage
                    .ToString() != "en"
            )
            {
                categoryText.text =
                    LocalizationSettings.StringDatabase.GetLocalizedString(
                        GameSettings.Instance.TableReference,
                        Category.name
                    );
            }
        }

        Sequence blastSequence =
            DOTween.Sequence();

        float delayStep = 0.08f;

        int index = 0;

        // -----------------------------------------------------
        // BUBBLE CONTENT DISAPPEAR
        // -----------------------------------------------------

        foreach (var text in textUIs)
        {
            Transform bg =
                text.bg.transform;

            Transform txt =
                text.textUIs.transform;

            bg.DOKill();
            txt.DOKill();

            Vector3 bgStartScale =
                bg.localScale;

            Vector3 txtStartScale =
                txt.localScale;

            float delay =
                index * delayStep;

            Sequence textSeq =
                DOTween.Sequence();

            textSeq.AppendInterval(delay);

            textSeq.Append(
                bg.DOScale(
                    bgStartScale * 1.05f,
                    0.1f
                )
                .SetEase(Ease.OutSine)
            );

            textSeq.Join(
                txt.DOScale(
                    txtStartScale * 1.05f,
                    0.1f
                )
                .SetEase(Ease.OutSine)
            );

            textSeq.Append(
                bg.DOScale(
                    Vector3.zero,
                    0.25f
                )
                .SetEase(Ease.InBack)
            );

            const float moveSpeed = 0.2f;

            textSeq.Join(
                bg.DOLocalMove(
                    Vector3.zero,
                    moveSpeed
                )
                .SetEase(Ease.InBack)
            );

            textSeq.Join(
                txt.DOLocalMove(
                    Vector3.zero,
                    moveSpeed
                )
                .SetEase(Ease.InBack)
            );

            textSeq.Join(
                txt.DOScale(
                    Vector3.zero,
                    0.2f
                )
                .SetEase(Ease.InBack)
            );

            blastSequence.Join(textSeq);

            index++;
        }

        // -----------------------------------------------------
        // CATEGORY TEXT
        // -----------------------------------------------------

        blastSequence.AppendCallback(() =>
        {
            if (categoryText != null)
            {
                categoryText.gameObject.SetActive(true);

                categoryText.transform.localScale =
                    Vector3.zero;
            }
        });

        if (categoryText != null)
        {
            Transform t =
                categoryText.transform;

            t.DOKill();

            Vector3 startScale =
                t.localScale;

            Sequence seq =
                DOTween.Sequence();

            t.localScale =
                startScale * 0.7f;

            seq.AppendInterval(0.15f);

            seq.Append(
                t.DOScale(
                    startScale * 1.1f,
                    0.35f
                )
                .SetEase(Ease.OutBack)
            );

            seq.Append(
                t.DOScale(
                    startScale,
                    0.15f
                )
                .SetEase(Ease.OutSine)
            );

            seq.AppendInterval(0.4f);

            seq.Append(
                t.DOScale(
                    startScale * 1.05f,
                    0.1f
                )
                .SetEase(Ease.OutSine)
            );

            seq.Append(
                t.DOScale(
                    Vector3.zero,
                    0.25f
                )
                .SetEase(Ease.InBack)
            );

            blastSequence.Append(seq);
        }

        // =====================================================
        // BUBBLE POP COMPLETE
        // =====================================================

        blastSequence.AppendCallback(() =>
        {
            ParticlePool.PlayRevealFx(transform.position);

            CategoryManager.Instance.SpawnNewCategories();

            Vector3 screenPosition =
                Camera.main.WorldToScreenPoint(
                    transform.position
                );

            FallingSandController sand =
                FindObjectOfType<FallingSandController>();

            if (sand != null)
            {
                sand.StartSand(
                    new Vector2(
                        screenPosition.x,
                        screenPosition.y
                    )
                );
            }

            InputHandler.Instance.OnSuccessfullMerge?.Invoke();

            BubbleEffect bubbleEffect =
                Object.FindObjectOfType<BubbleEffect>();

            if (bubbleEffect != null)
            {
                bubbleEffect.OnBubblePop();
            }

            Destroy(gameObject);

            OnBlastComplete?.Invoke();
        });
    }

    // =========================================================
    // SET SPRITE
    // =========================================================

    internal void SetBubbleSprite(Sprite sprite)
    {
        if (bg != null)
            bg.sprite = sprite;
    }

    // =========================================================
    // SET COLOR
    // =========================================================

    internal void SetColor(Color bubbleColor)
    {
        bgColor = bubbleColor;

        if (bg != null)
            bg.color = bgColor;
    }

    // =========================================================
    // GIZMOS
    // =========================================================

    private void OnDrawGizmos()
    {
        Color color =
            category.Color;

        color.a = 1f;

        Gizmos.color =
            color;

        Gizmos.DrawSphere(
            transform.position,
            Radius
        );
    }

    // =========================================================
    // BLAST GHOST
    // =========================================================

    internal void BlastGhost()
    {
        if (ghostInstance != null)
        {
            Destroy(ghostInstance);
        }
    }

    // =========================================================
    // RETURN BACK
    // =========================================================

    internal void ReturnBack()
    {
        if (GameSettings.Instance.CanCreateGhost)
        {
            if (ghostInstance != null)
            {
                transform
                    .DOMove(
                        ghostInstance.transform.position,
                        0.2f
                    )
                    .OnComplete(() =>
                    {
                        EndDrag();

                        Destroy(
                            ghostInstance
                        );
                    });
            }
            else
            {
                EndDrag();
            }
        }
        else
        {
            EndDrag();
        }
    }
}