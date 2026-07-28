using System.Collections;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

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

    public enum ColorAssignmentMode
    {
        RANDOMISE,
        CATEGORY_WISE
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
    //Bubble[] bubbles;
    [SerializeField] private int dragSpeed = 10;
    [SerializeField] float breathingSpeed = 2;
    [SerializeField] float breathingAplitude = .05f;
    [SerializeField] private float textBreathingSpeed;
    [SerializeField] ParticleSystem bubbleFXPrefab;
    [SerializeField] bool canChangeColor;
    [SerializeField] ColorAssignmentMode colorAssignmentMode;
    [SerializeField] Color[] bubbleColors;
    [SerializeField] private TableReference tableReference;
    [SerializeField] private Locale selectedLanguage;
    [SerializeField] private UnityEngine.Localization.Locale englishLocale;
    [SerializeField] private RuntimeAnimatorController animatorController;
    [SerializeField] bool canAnimateSprite;

    public float MaxBounceAmplitude { get => maxBounceAmplitude; }
    public float BounceTime { get => bounceTime; }
    public int MaxBounces { get => maxBounces; }
    public float LerpSpeeed { get => lerpSpeeed; }
    public Bubble[] Bubbles { get => bubbleProfile.Bubbles; }
    public int DragSpeed { get => dragSpeed; }
    public float BreathingSpeed { get => breathingSpeed; }
    public float BreathingAplitude { get => breathingAplitude; }
    public float TextBreathingSpeed { get => textBreathingSpeed; }
    public ParticleSystem BubbleFXPrefab { get => bubbleFXPrefab; internal set => bubbleFXPrefab = value; }
    public bool CanChangeColor { get => canChangeColor; }
    public ColorAssignmentMode ColorMode { get => colorAssignmentMode; }
    public Color[] BubbleColors { get => bubbleColors; }
    public Locale SelectedLanguage { get => selectedLanguage; }
    public TableReference TableReference { get => tableReference; }
    public UnityEngine.Localization.Locale EnglishLocale { get => englishLocale; }
    public bool CanAnimateSprite { get => canAnimateSprite; }
    public RuntimeAnimatorController AnimatorController { get => animatorController; }

    internal static IEnumerator Init()
    {
        var asyncOp = Resources.LoadAsync<GameSettings>("GameSettings");
        yield return asyncOp;
        instance = (GameSettings)asyncOp.asset;
    }
}