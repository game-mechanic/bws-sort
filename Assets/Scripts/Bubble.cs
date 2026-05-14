using DG.Tweening;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization.Settings;
using UnityEngine.Rendering;
using static UnityEditor.Progress;


[SelectionBase]
public class Bubble : MonoBehaviour
{
    const float PhaseDiff = 90 * Mathf.Deg2Rad;
    [SerializeField] byte index;
    [SerializeField] Transform viusal;
    [SerializeField] GameObject highlightImage;
    [SerializeField] SpriteRenderer bg;
    [SerializeField] Color bgColor;
    [SerializeField] List<TextMeshPro> textUIs;

    [SerializeField] List<string> names;
    Rigidbody2D rb;
    CircleCollider2D col;
    SortingGroup sortingGroup;
    float bounceAmplitude;
    float bounceDuration;
    bool isBouncing = false;
    float randomPhaseDiff;
    float time = 0f;
    Vector3 startScale;
    private float randomTextPhaseDiff;
    public Vector3[] textPositions;
    [SerializeField] private BubbleType category;
    public UnityEvent OnBubbleBlasted = new();


    public RigidbodyType2D IsKinematic { get => rb.bodyType; set => rb.bodyType = value; }
    public float Radius => col.radius;

    public byte Index { get => index; }
    public BubbleType Category { get => category; set => category = value; }
    public List<string> Names { get => names; }

    private void Start()
    {
        if (Category != null)
            CategoryManager.Instance.RegisterCategory(Category);
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<CircleCollider2D>();
        sortingGroup = GetComponent<SortingGroup>();
        startScale = viusal.transform.localScale;
        randomTextPhaseDiff = Random.Range(0, 360) * Mathf.Deg2Rad;
        randomPhaseDiff = Random.Range(0, 90) * Mathf.Deg2Rad;
        textPositions = new Vector3[textUIs.Count];
        for (int i = 0; i < textUIs.Count; i++)
        {
            textPositions[i] = textUIs[i].transform.localPosition;
        }
        RedrawNames();
        foreach (var name in textUIs)
        {
#if UNITY_EDITOR
            SceneVisibilityManager.instance.Hide(name.gameObject, true);
#endif
        }
    }

    private void RedrawNames()
    {
        for (int i = 0; i < Names.Count; i++)
        {
            //textUIs[i].text = Names[i];
            if (GameSettings.Instance.SelectedLanguage.ToString() == "en")
            {
                textUIs[i].text = Names[i];
            }
            else
            {
                textUIs[i].text =
                    LocalizationSettings.StringDatabase.GetLocalizedString(
                       GameSettings.Instance.TableReference,
                        Names[i].ToLower()
                    );
            }

        }
    }
    private void OnValidate()
    {
        if (bg != null)
            bg.color = bgColor;
        RedrawNames();
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
            //viusal.transform.DOScale(startScale, 0.05f);
            OnBubbleBlasted?.Invoke();
        }
    }
    public void Bounce()
    {
        isBouncing = true;
        time = 0;
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
    private void OnDrawGizmos()
    {
        //Gizmos.color = Color.white;
        //Gizmos.DrawSphere(transform.position, radius: Radius);
        if (Names.Count == 1)
        {
            string name = LocalizationSettings.StringDatabase.GetLocalizedString(
                           GameSettings.Instance.TableReference,
                            Names[0],
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
}
