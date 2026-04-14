using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "GameSettings")]
public class GameSettings : ScriptableObject
{
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

    public float MaxBounceAmplitude { get => maxBounceAmplitude; }
    public float BounceTime { get => bounceTime; }
    public int MaxBounces { get => maxBounces; }
    public float LerpSpeeed { get => lerpSpeeed; }
    public Bubble[] Bubbles { get => bubbles; }
    public int DragSpeed { get => dragSpeed; }
    public float BreathingSpeed { get => breathingSpeed; }
    public float BreathingAplitude { get => breathingAplitude; }

    internal static IEnumerator Init()
    {
        var asyncOp = Resources.LoadAsync<GameSettings>("GameSettings");
        yield return asyncOp;
        instance = (GameSettings)asyncOp.asset;
    }
}