using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization.Tables;

[System.Serializable]
public struct BubbleTypeWaterColor
{
    public BubbleType bubbleType;
    public Color color;
}

[CreateAssetMenu(fileName = "GameSettings")]
public class GameSettings : ScriptableObject
{

    public enum Locale
    {
        en = 0,
        fr = 1,
        de = 2,
        pt = 3,
        ru = 4,
        es = 5,
    }

    public static GameSettings Instance
    {
        get
        {
            if (instance == null)
                instance = Resources.Load<GameSettings>("GameSettings");
            return instance;
        }
    }

    private static GameSettings instance;

    [SerializeField] float maxBounceAmplitude = .5f;
    [SerializeField] float bounceTime = 0.5f;
    [SerializeField] int maxBounces = 2;
    [SerializeField] private float lerpSpeeed;

    [Header("Prefabs")]

    [SerializeField]
    BubbleProfile bubbleProfile;
    [SerializeField, Range(2, 5)]
    [Tooltip("Maximum number of bubbles that can be merged at once. It is recommended to keep it a 4. 5 is no merge")]
    int mergeCount;
    //Bubble[] bubbles;
    [SerializeField] private int dragSpeed = 10;
    [SerializeField] float breathingSpeed = 2;
    [SerializeField] float breathingAplitude = .05f;
    [SerializeField] private float textBreathingSpeed;
    [SerializeField] ParticleSystem bubbleFXPrefab;
    [SerializeField] float rotationOffset;
    [SerializeField] private RuntimeAnimatorController animatorController;
    [SerializeField] bool canAnimateSprite;
    [SerializeField] bool canChangeColor;
    [SerializeField] ColorProfile colorProfile;
    [SerializeField] bool canUseDifferentSprites;
    [SerializeField] SpriteProfile spriteProfile;

    [SerializeField] private TableReference tableReference;
    [SerializeField] private Locale selectedLanguage;
    [SerializeField] private UnityEngine.Localization.Locale englishLocale;
    [SerializeField] bool canCreateGhost;
    [SerializeField] private bool canMerge;
    [SerializeField] private BubbleType[] order;
    [SerializeField] private bool canTextBreathe;
    [SerializeField] private bool enableRandomBubbleSize;

    [Header("Bubble Type Water Colors")]
    [SerializeField] private List<BubbleTypeWaterColor> bubbleTypeWaterColors = new List<BubbleTypeWaterColor>();

    public List<BubbleTypeWaterColor> BubbleTypeWaterColors => bubbleTypeWaterColors;

    public bool TryGetWaterColor(BubbleType type, out Color color)
    {
        color = Color.white;
        if (type == null || bubbleTypeWaterColors == null) return false;

        for (int i = 0; i < bubbleTypeWaterColors.Count; i++)
        {
            if (bubbleTypeWaterColors[i].bubbleType == type)
            {
                color = bubbleTypeWaterColors[i].color;
                return true;
            }
        }
        return false;
    }

    public float MaxBounceAmplitude { get => maxBounceAmplitude; }
    public float BounceTime { get => bounceTime; }
    public int MaxBounces { get => maxBounces; }
    public float LerpSpeeed { get => lerpSpeeed; }
    public Bubble[] Bubbles { get => bubbleProfile.Bubbles; }
    public int DragSpeed { get => dragSpeed; }
    public float BreathingSpeed { get => breathingSpeed; }
    public float BreathingAplitude { get => breathingAplitude; }
    public float TextBreathingSpeed { get => textBreathingSpeed; }
    public ParticleSystem BubbleFXPrefab { get => bubbleFXPrefab; }
    public bool CanChangeColor { get => canChangeColor; }
    public Color[] BubbleColors { get => colorProfile.bubbleColors; }
    public Locale SelectedLanguage { get => selectedLanguage; }
    public TableReference TableReference { get => tableReference; }
    public UnityEngine.Localization.Locale EnglishLocale { get => englishLocale; }
    public bool CanCreateGhost { get => canCreateGhost; }
    public bool CanMerge { get => canMerge; internal set => canMerge = value; }
    public BubbleType[] Order { get => order; set => order = value; }
    public bool CanTextBreathe { get => canTextBreathe; internal set => canTextBreathe = value; }
    public bool EnableRandomBubbleSize { get => enableRandomBubbleSize; }
    public bool CanUseDifferentSprites { get => canUseDifferentSprites; }
    public Sprite[] BubbleSprites { get => spriteProfile.BubbleSprites; }
    public int MergeCount { get => mergeCount; }
    public bool CanAnimateSprite { get => canAnimateSprite; }
    public RuntimeAnimatorController AnimatorController { get => animatorController; }
    public float RotationOffset { get => rotationOffset; }

    internal static IEnumerator Init()
    {
        var asyncOp = Resources.LoadAsync<GameSettings>("GameSettings");
        yield return asyncOp;
        instance = (GameSettings)asyncOp.asset;
    }
}