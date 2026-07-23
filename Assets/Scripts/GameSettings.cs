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
    [SerializeField] private bool checkRow;

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
    public bool CheckRow { get => checkRow; }

    internal static IEnumerator Init()
    {
        var asyncOp = Resources.LoadAsync<GameSettings>("GameSettings");
        yield return asyncOp;
        instance = (GameSettings)asyncOp.asset;
    }
    public static Vector3 GetStackedPosition(Vector3 position, int index, float slotOffset)
    {
        return GetStackedPosition(position, index, Vector3.up, Vector3.zero, slotOffset);
    }


    /// <summary>
    /// Calculates the position of an object stacked in a line, with an additional rotation applied to the stacking direction.
    /// </summary>
    /// <param name="index">The index of the object in the stack (0-based).</param>
    /// <param name="position">The starting position of the stack.</param>
    /// <param name="direction">The direction in which to stack objects (should be normalized).</param>
    /// <param name="eulerRotation">Euler angles (in degrees) to rotate the stacking direction.</param>
    /// <param name="slotOffset">The distance between each stacked object.</param>
    /// <returns>The calculated position for the object at the given index, with rotation applied.</returns>
    public static Vector3 GetStackedPosition(Vector3 position, int index, Vector3 direction, Vector3 eulerRotation, float slotOffset)
    {
        Vector3 offset = index * slotOffset * direction;
        Quaternion rotation = Quaternion.Euler(eulerRotation); // Convert Vector3 rotation to Quaternion
        return position + rotation * offset; // Apply rotation to the offset
    }
}
