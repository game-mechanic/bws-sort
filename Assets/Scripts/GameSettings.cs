using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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
    [SerializeField] Color[] bubbleColors;
    [SerializeField] private TableReference tableReference;
    [SerializeField] private Locale selectedLanguage;
    [SerializeField] private UnityEngine.Localization.Locale englishLocale;
    [SerializeField] bool canCreateGhost;
    [SerializeField] private bool canMerge;
    [SerializeField] private BubbleType[] order;
    [SerializeField] private bool canTextBreathe;
    [SerializeField] private bool enableRandomBubbleSize;
    [SerializeField] MathAnswers mathAnswerPrefab;
    [SerializeField] List<BubbleType> types;
    [SerializeField] private GameObject trailPrefab;
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
    public Color[] BubbleColors { get => bubbleColors; }
    public Locale SelectedLanguage { get => selectedLanguage; }
    public TableReference TableReference { get => tableReference; }
    public UnityEngine.Localization.Locale EnglishLocale { get => englishLocale; }
    public bool CanCreateGhost { get => canCreateGhost; }
    public bool CanMerge { get => canMerge; internal set => canMerge = value; }
    public BubbleType[] Order { get => order; set => order = value; }
    public bool CanTextBreathe { get => canTextBreathe; internal set => canTextBreathe = value; }
    public bool EnableRandomBubbleSize { get => enableRandomBubbleSize; }
    public MathAnswers MathAnswerPrefab { get => mathAnswerPrefab; }
    public GameObject TrailPrefab { get => trailPrefab; }

    int currentCategory = 0;

    private void Awake()
    {
        currentCategory = 0;
    }

    internal static IEnumerator Init()
    {
        var asyncOp = Resources.LoadAsync<GameSettings>("GameSettings");
        yield return asyncOp;
        instance = (GameSettings)asyncOp.asset;
    }

    internal BubbleType GetNextCategory()
    {
        return types[currentCategory++ % types.Count];
    }
}