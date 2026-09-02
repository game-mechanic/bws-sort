using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

public class CategoryManager : Singleton<CategoryManager>
{
    [SerializeField] bool spawnOnStart = true;
    [SerializeField] bool shouldBlastAtStart = false;

    [SerializeField] LevelData levelData;
    [SerializeField] HorizontalAlignment horizontalAlignment;
    List<LevelData.Data> datas = new();
    int initialSpawns = 15;

    bool EnableRandomSize => GameSettings.Instance.EnableRandomBubbleSize;
    float minMultiplier = 1f;
    float maxMultiplier = 2f;
    [SerializeField] public Bubble destinationBubble;
    [SerializeField] float delayBeforeDestinationSetup = 1f;
    
    [Header("Tap Feedback Settings")]
    [SerializeField] public Color correctTapColor = Color.green;
    [SerializeField] public Color wrongTapColor = Color.red;
    [SerializeField] public float wrongShakeDuration = 0.4f;
    [SerializeField] public float wrongShakeStrength = 0.3f;
    
    [Header("Destination Floaty Settings")]
    [SerializeField] public float destBubbleFloatyInnerRadii = 0.5f;
    [SerializeField] public float destBubbleFloatyOuterRadii = 1.0f;
    [SerializeField] public float destBubbleFloatySpeed = 2f;
    [SerializeField] public float distBtwBubble = 0.5f;
    [SerializeField] public float arcBendness = 0f;

    [Header("Burst Sequence Settings")]
    [SerializeField] public float delayBurst = 0.1f;
    [SerializeField] public float initialDelayAfterComplete = 0.5f;

    int currentIndex = 0;
    int currentOrderIndex = 0;
    Dictionary<BubbleType, int> categoryCounts = new Dictionary<BubbleType, int>();
    List<Bubble> gatheredBubbles = new List<Bubble>();

    Dictionary<Bubble, Vector2> bubbleVelocities = new Dictionary<Bubble, Vector2>();
    Dictionary<Bubble, Vector2> bubbleFloatTargets = new Dictionary<Bubble, Vector2>();

    public LevelData LevelDataAsset => levelData;

    public int CurrentIndex { get => currentIndex; }

    IEnumerator Start()
    {
        datas = new(levelData.datas);
        initialSpawns = levelData.initialSpawns;
        minMultiplier = levelData.minMultiplier;
        maxMultiplier = levelData.maxMultiplier;


        if (!spawnOnStart)
        {
            yield break;
        }

        if (destinationBubble != null)
        {
            destinationBubble.SetAsDestination();
            StartCoroutine(DestinationSetupCoroutine());
        }

        yield return LoadLocalization(GameSettings.Instance.TableReference.TableCollectionName);
        Shuffle();

        List<Bubble> blastableBubble = new();

        WaitForSeconds _waitForSeconds0_1 = new(0.1f);
        for (int j = 0; j < Mathf.Min(initialSpawns, datas.Count); j++)
        {
            Bubble bubblePrefab = GameSettings.Instance.Bubbles[datas[j].newDatas.Count - 1];
            Vector3 pos = horizontalAlignment.GetSlotPosition(j % 4);
            BubbleType category = datas[j].name;
            List<Bubble.Data> data = new();
            if (datas[j].newDatas != null)
            {
                data.AddRange(datas[j].newDatas);
            }
            bool shouldBlastAtStart = datas[j].shouldBlastAtStart;

            Color bubbleColor = datas[j].overrideColor ?
                datas[j].bubbleColor :
                GameSettings.Instance.BubbleColors[j % GameSettings.Instance.BubbleColors.Length];

            Sprite sprite = GameSettings.Instance.BubbleSprites[j % GameSettings.Instance.BubbleSprites.Length];

            DOVirtual.DelayedCall(Random.Range(0.1f, 0.2f), () =>
            {
                var bubble = Instantiate(bubblePrefab, pos, Quaternion.Euler(new Vector3(0, 0, Random.Range(-GameSettings.Instance.RotationOffset, GameSettings.Instance.RotationOffset))));

                if (EnableRandomSize)
                {
                    float randomScale = Random.Range(minMultiplier, maxMultiplier);
                    bubble.transform.localScale = Vector3.one * randomScale;
                }

                bubble.Category = category;

                if (GameSettings.Instance.CanChangeColor)
                {
                    bubble.SetColor(bubbleColor);
                }

                if (GameSettings.Instance.CanUseDifferentSprites)
                {
                    bubble.SetBubbleSprite(sprite);
                }

                bubble.transform.DOScale(bubble.transform.localScale, 0.2f).From(0);

                bubble.SetName(data);

                if (shouldBlastAtStart)
                {
                    blastableBubble.Add(bubble);
                }

            });

            if (j % 4 == 0)
                yield return _waitForSeconds0_1;
        }

        currentIndex = initialSpawns;
        yield return new WaitForSeconds(2);
        for (int i = 0; i < blastableBubble.Count; i++)
        {
            // blastableBubble[i].Blast(() => InputHandler.Instance.OnSuccessfullMerge?.Invoke());
            blastableBubble[i].gameObject.SetActive(false);
            yield return new WaitForSeconds(.15f);
        }
        if (blastableBubble.Count > 0)
            SpawnNewCategories();
    }

    private IEnumerator LoadLocalization(string tableName)
    {
        // 1. Wait until the localization system is entirely initialized
        yield return LocalizationSettings.InitializationOperation;

        // 2. Load your specific String Table asynchronously
        var tableOperation = LocalizationSettings.StringDatabase.GetTableAsync(tableName);
        yield return tableOperation;

        if (tableOperation.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
        {
            StringTable stringTable = tableOperation.Result;

            // 3. Extract a value using your entry key
            string localizedValue = stringTable.GetEntry("YOUR_KEY_HERE")?.LocalizedValue;
            Debug.Log($"Loaded String: {localizedValue}");
        }
        else
        {
            Debug.LogError($"Failed to load localization table: {tableName}");
        }
        LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.Locales[(int)GameSettings.Instance.SelectedLanguage];
    }

    IEnumerator DestinationSetupCoroutine()
    {
        yield return new WaitForSeconds(delayBeforeDestinationSetup);
        SetupNextDestinationCategory();
    }

    public void SetupNextDestinationCategory()
    {
        if (currentOrderIndex < GameSettings.Instance.Order.Length)
        {
            BubbleType nextType = GameSettings.Instance.Order[currentOrderIndex];
            destinationBubble.Category = nextType;
            
            string localizedValue = nextType.name;
            if (GameSettings.Instance.SelectedLanguage.ToString() != "en")
            {
                localizedValue = LocalizationSettings.StringDatabase.GetLocalizedString(
                       GameSettings.Instance.TableReference,
                        nextType.name
                    );
            }
            destinationBubble.SetupDestinationCategory(localizedValue);
        }
    }

    private void Shuffle()
    {
        if (datas.Count == 0) return;
        for (int i = 0; i < 5; i++)
        {
            int a = Random.Range(0, initialSpawns);
            int b = Random.Range(0, initialSpawns);
            (datas[b], datas[a]) = (datas[a], datas[b]);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ChangeCategory();
        }

        if (gatheredBubbles.Count > 0)
        {
            foreach (var b in gatheredBubbles)
            {
                if (b == null || !b.isFloatingInBucket) continue;

                if (!bubbleFloatTargets.ContainsKey(b)) AssignNewFloatTarget(b);
                if (!bubbleVelocities.ContainsKey(b)) bubbleVelocities[b] = Vector2.zero;

                Vector2 target = bubbleFloatTargets[b];
                Vector2 pos = b.transform.position;

                Vector2 dir = target - pos;
                if (dir.magnitude < 0.1f)
                {
                    AssignNewFloatTarget(b);
                }

                // Acceleration towards target
                bubbleVelocities[b] += dir.normalized * destBubbleFloatySpeed * Time.deltaTime;
                
                // Repulsion
                Vector2 repulsion = Vector2.zero;
                foreach (var other in gatheredBubbles)
                {
                    if (other == b || other == null) continue;
                    float dist = Vector2.Distance(pos, other.transform.position);
                    if (dist < distBtwBubble && dist > 0.001f)
                    {
                        Vector2 repDir = (pos - (Vector2)other.transform.position).normalized;
                        repulsion += repDir * (distBtwBubble - dist) * 15f; 
                    }
                }

                if (repulsion != Vector2.zero)
                {
                    bubbleVelocities[b] += repulsion * Time.deltaTime;
                }

                // Damping
                bubbleVelocities[b] *= 0.95f; 

                pos += bubbleVelocities[b] * Time.deltaTime;

                if (destinationBubble != null)
                {
                    Vector2 center = destinationBubble.transform.position;
                    Vector2 offset = pos - center;
                    if (offset.magnitude > destBubbleFloatyOuterRadii)
                    {
                        pos = center + offset.normalized * destBubbleFloatyOuterRadii;
                        bubbleVelocities[b] *= -0.5f; // Soft bounce
                    }
                }

                b.transform.position = pos;
            }
        }
    }

    void AssignNewFloatTarget(Bubble bubble)
    {
        if (destinationBubble == null) return;
        float angle = Random.Range(0f, Mathf.PI * 2);
        float radius = Random.Range(0f, destBubbleFloatyOuterRadii); 
        Vector2 target = (Vector2)destinationBubble.transform.position + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        bubbleFloatTargets[bubble] = target;
    }
    public void RegisterCategory(BubbleType category)
    {
        if (categoryCounts.ContainsKey(category))
        {
            categoryCounts[category]++;
        }
        else
        {
            categoryCounts[category] = 1;
        }
    }

    public int ReduceCount(BubbleType category)
    {
        if (!categoryCounts.ContainsKey(category)) return 0;
        categoryCounts[category] -= 2;

        if (categoryCounts[category] <= 0)
        {
            categoryCounts.Remove(category);
            return 0;
        }
        else
        {
            return categoryCounts[category];
        }
    }


    public void SpawnNewCategories()
    {
        Bubble bubblePrefab = GameSettings.Instance.Bubbles[0];

        int end = Mathf.Min(currentIndex + 4, datas.Count);
        for (int j = currentIndex; j < end; j++)
        {
            Vector3 pos = horizontalAlignment.GetSlotPosition(j % 4);
            BubbleType category = datas[j].name;
            List<Bubble.Data> data = new();
            if (datas[j].newDatas != null)
            {
                data.AddRange(datas[j].newDatas);
            }
            Color bubbleColor = datas[j].overrideColor ?
                        datas[j].bubbleColor :
                        GameSettings.Instance.BubbleColors[j % GameSettings.Instance.BubbleColors.Length];

            Sprite sprite = GameSettings.Instance.BubbleSprites[j % GameSettings.Instance.BubbleSprites.Length];
            DOVirtual.DelayedCall(Random.Range(0.1f, 0.2f), () =>
            {
                var bubble = Instantiate(bubblePrefab, pos, Quaternion.identity);

                if (EnableRandomSize)
                {
                    float randomScale = Random.Range(minMultiplier, maxMultiplier);
                    bubble.transform.localScale = Vector3.one * randomScale;
                }

                bubble.Category = category;

                if (GameSettings.Instance.CanChangeColor)
                    bubble.SetColor(bubbleColor);

                if (GameSettings.Instance.CanUseDifferentSprites)
                    bubble.SetBubbleSprite(sprite);

                bubble.SetName(data);
            });
        }
        currentIndex += 4;
    }

    void ChangeCategory()
    {
        //currentIndex++;
        //Vector3 squishedScale = new Vector3(1.2f, 0.8f, 1);
        //Vector3 originalScale = new Vector3(.8f, 1.2f, 1);
        //Vector3 startPosition = transform.position;
        //Sequence moveOutSequence = DOTween.Sequence();

        //// Step 2: When lid reaches destination, squish the cart
        //moveOutSequence.Append(DOTween.Sequence()
        //        .Append(transform.DOScale(squishedScale, 0.2f))
        //        .Append(transform.DOScale(originalScale, 0.1f)));

        //// Step 3: Move the cart up and restore scale simultaneously
        //moveOutSequence.Append(transform.DOMove(startPosition + Vector3.up * 2, .5f).SetEase(Ease.OutExpo));

        //moveOutSequence.AppendCallback(() =>
        //{
        //    Redraw();
        //});
        //moveOutSequence.Append(transform.DOMove(startPosition, .5f).SetEase(Ease.InExpo));
        //moveOutSequence.Join(transform.DOScale(originalScale, 0.1f));
        //moveOutSequence.Append(transform.DOScale(squishedScale, 0.2f));
        //moveOutSequence.Append(transform.DOScale(Vector3.one, 0.1f));
    }

    public int GetCategoryCount(BubbleType category)
    {
        if (!categoryCounts.ContainsKey(category)) return -1;
        return categoryCounts[category];
    }

    public int DecrementCount(BubbleType category)
    {
        if (!categoryCounts.ContainsKey(category)) return 0;
        categoryCounts[category] -= 1;

        if (categoryCounts[category] <= 0)
        {
            categoryCounts.Remove(category);
            return 0;
        }
        return categoryCounts[category];
    }

    public Vector3 GetRandomAnnulusPosition()
    {
        if (destinationBubble == null) return Vector3.zero;

        Vector3 center = destinationBubble.transform.position;
        Vector3 bestPos = center;
        float maxMinDist = -1f;

        for (int i = 0; i < 15; i++)
        {
            float angle = Random.Range(0f, Mathf.PI * 2);
            float radius = Random.Range(destBubbleFloatyInnerRadii, destBubbleFloatyOuterRadii);
            Vector3 candidatePos = center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * radius;

            if (gatheredBubbles.Count == 0)
            {
                return candidatePos;
            }

            float minDist = float.MaxValue;
            foreach (var b in gatheredBubbles)
            {
                if (b == null) continue;
                float dist = Vector3.Distance(candidatePos, b.transform.position);
                if (dist < minDist)
                {
                    minDist = dist;
                }
            }

            if (minDist > maxMinDist)
            {
                maxMinDist = minDist;
                bestPos = candidatePos;
            }
        }
        return bestPos;
    }

    public void OnBubbleReachedDestination(Bubble bubble)
    {
        gatheredBubbles.Add(bubble);
        bubble.isFloatingInBucket = true;
        // FloatAround is now handled dynamically with repulsion in Update()
        
        int remaining = DecrementCount(bubble.Category);
        if (remaining <= 0)
        {
            StartCoroutine(HandleDestinationComplete());
        }
    }

    private IEnumerator HandleDestinationComplete()
    {
        yield return new WaitForSeconds(initialDelayAfterComplete);

        foreach (var b in gatheredBubbles)
        {
            if (b != null)
            {
                b.isFloatingInBucket = false;
                b.transform.DOKill();
                
                if (GameSettings.Instance.BubbleFXPrefab != null)
                {
                    ParticleSystem fx = Instantiate(GameSettings.Instance.BubbleFXPrefab);
                    fx.transform.SetPositionAndRotation(b.transform.position, Quaternion.Euler(-90, 0, 0));
                    fx.transform.localScale = b.transform.localScale / 2;
                    fx.Play();
                }

                b.gameObject.SetActive(false);
                Destroy(b.gameObject);
            }
            yield return new WaitForSeconds(delayBurst);
        }
        gatheredBubbles.Clear();
        bubbleVelocities.Clear();
        bubbleFloatTargets.Clear();

        if (destinationBubble != null)
        {
            destinationBubble.ScaleOutCategoryText(0.2f);
            yield return new WaitForSeconds(0.2f);

            if (GameSettings.Instance.BubbleFXPrefab != null)
            {
                ParticleSystem fx = Instantiate(GameSettings.Instance.BubbleFXPrefab);
                fx.transform.SetPositionAndRotation(destinationBubble.transform.position, Quaternion.Euler(-90, 0, 0));
                fx.transform.localScale = destinationBubble.transform.localScale / 2;
                fx.Play();
            }

            destinationBubble.gameObject.SetActive(false);
        }

        SpawnNewCategories();

        currentOrderIndex++;
        DOVirtual.DelayedCall(0.5f, () => 
        {
            if (currentOrderIndex < GameSettings.Instance.Order.Length)
            {
                destinationBubble.gameObject.SetActive(true);
                SetupNextDestinationCategory();
            }
        });
    }

    private void OnDrawGizmos()
    {
        if (destinationBubble != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(destinationBubble.transform.position, destBubbleFloatyOuterRadii);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(destinationBubble.transform.position, destBubbleFloatyInnerRadii);
        }
    }
}
