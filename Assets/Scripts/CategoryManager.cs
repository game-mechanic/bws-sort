using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization.Settings;

public class CategoryManager : Singleton<CategoryManager>
{
    [System.Serializable]
    public class Data
    {
        public BubbleType name;
        public bool overrideColor = false;
        [ColorUsage(false)] public Color bubbleColor = Color.white;
        public Bubble.Data data;
    }
    [SerializeField] HorizontalAlignment horizontalAlignment;
    [SerializeField] int pipeCount = 3;
    [SerializeField] List<Data> datas = new();
    [SerializeField] int initialSpawns = 15;
    [SerializeField] ParticleSystem[] particleSystems;
    int currentIndex = 0;
    Dictionary<BubbleType, int> categoryCounts = new Dictionary<BubbleType, int>();

    IEnumerator Start()
    {
        LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.Locales[(int)GameSettings.Instance.SelectedLanguage];
        Shuffle();

        var fx = particleSystems[1];
        fx.Play();
        WaitForSeconds _waitForSeconds0_1 = new(0.1f);
        Bubble bubblePrefab = GameSettings.Instance.Bubbles[0];

        for (int j = 0; j < Mathf.Min(initialSpawns, datas.Count); j += 3)
        {
            //CreateBubble(bubblePrefab, j, 1);
            int endIndex = Mathf.Min(j + 3, datas.Count);
            CreateDummyBubbles(datas.GetRange(j, endIndex - j), 1);
            yield return _waitForSeconds0_1;
        }

        fx.Stop();

        currentIndex = initialSpawns;
    }
    public void CreateDummyBubbles(List<Data> datas, int entry)
    {
        Vector3 position = horizontalAlignment.GetSlotPosition(entry % pipeCount);
        DummyBubble dummyBubble = Instantiate(GameSettings.Instance.DummyBubble, position, Quaternion.identity);
        dummyBubble.SetData(datas);
    }


    private void CreateBubble(Bubble bubblePrefab, int j, int entry)
    {
        Vector3 pos = horizontalAlignment.GetSlotPosition(entry % pipeCount);
        pos += Vector3.right * Random.Range(-0.005f, 0.005f);

        BubbleType category = datas[j].name;

        Bubble.Data data = datas[j].data;

        Color bubbleColor = datas[j].overrideColor ?
            datas[j].bubbleColor :
            GameSettings.Instance.BubbleColors[j % GameSettings.Instance.BubbleColors.Length];

        var bubble = Instantiate(bubblePrefab, pos, Quaternion.identity);
        bubble.Category = category;
        if (GameSettings.Instance.CanChangeColor)
            bubble.SetColor(bubbleColor);
        bubble.SetName(new() { data });
    }

    private void Shuffle()
    {
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

        StartCoroutine(SpawnNewBubbles(bubblePrefab));
    }
    IEnumerator SpawnNewBubbles(Bubble bubblePrefab)
    {
        int end = Mathf.Min(currentIndex + 4, datas.Count);
        int pipeIndex = Random.Range(0, pipeCount);
        var fx = particleSystems[pipeIndex];
        fx.Play();

        WaitForSeconds waitForSeconds = new(0.1f);
        for (int j = currentIndex; j < end; j += 3)
        {
            //CreateBubble(bubblePrefab, j, pipeIndex);
            int endIndex = Mathf.Min(j + 3, datas.Count);
            CreateDummyBubbles(datas.GetRange(j, endIndex - j), 1);
            yield return waitForSeconds;
        }
        fx.Stop();
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
}
