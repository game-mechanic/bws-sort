using System.Collections;
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
    Bubble[] bubbles;
    [SerializeField] private int dragSpeed = 10;
    [SerializeField] float breathingSpeed = 2;
    [SerializeField] float breathingAplitude = .05f;
    [SerializeField] private ParticleSystem bubbleFXPrefab;
    [SerializeField] private float propSpawnPositionHieght = 1f;
    [SerializeField] private float textBreathingSpeed;
    [SerializeField] private TableReference tableReference;
    [SerializeField] private Locale selectedLanguage;
    [SerializeField] private UnityEngine.Localization.Locale englishLocale;
    public float MaxBounceAmplitude { get => maxBounceAmplitude; }
    public float BounceTime { get => bounceTime; }
    public int MaxBounces { get => maxBounces; }
    public float LerpSpeeed { get => lerpSpeeed; }
    public Bubble[] Bubbles { get => bubbles; }
    public int DragSpeed { get => dragSpeed; }
    public float BreathingSpeed { get => breathingSpeed; }
    public float BreathingAplitude { get => breathingAplitude; }
    public ParticleSystem BubbleFXPrefab { get => bubbleFXPrefab; }
    public float PropSpawnPositionHieght { get => propSpawnPositionHieght; }
    public float TextBreathingSpeed { get => textBreathingSpeed; }
    public Locale SelectedLanguage { get => selectedLanguage; }
    public TableReference TableReference { get => tableReference; }
    public UnityEngine.Localization.Locale EnglishLocale { get => englishLocale; }
    internal static IEnumerator Init()
    {
        var asyncOp = Resources.LoadAsync<GameSettings>("GameSettings");
        yield return asyncOp;
        instance = (GameSettings)asyncOp.asset;
    }
}