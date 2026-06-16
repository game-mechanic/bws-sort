using DG.Tweening;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MathAnswers : MonoBehaviour
{
    [SerializeField] BubbleType category;
    [SerializeField] TextMeshPro text;


    static List<MathAnswers> answers = new();

    static GameObject trail;

    private void Start()
    {
        answers.Add(this);
    }
    private void OnDestroy()
    {
        answers.Remove(this);
    }

    public void SetCategory(BubbleType category)
    {
        this.category = category;
        Redraw();
    }
    public void Redraw()
    {
        if (category == null || text == null) return;

        text.text = category.name;
    }
    private void OnValidate()
    {
        Redraw();
    }

    public static void DestroyBubble(BubbleType category, Vector3 blastPosition)
    {
        foreach (var answer in answers)
        {
            if (answer.category == category)
            {
                Vector3 pos = answer.transform.position;

                if (trail == null)
                {
                    trail = Instantiate(GameSettings.Instance.TrailPrefab);
                }

                trail.transform.position = blastPosition;
                trail.SetActive(true);
                var d = answer;

                trail.transform.DOMove(answer.transform.position, 1f)
                    .SetEase(Ease.InBack)
                    .OnComplete(() =>
                    {
                        d.transform.DOScale(Vector3.zero, 0.35f).SetEase(Ease.InBack);
                        Destroy(d.gameObject, .5f);
                        DOVirtual.DelayedCall(0.5f, () =>
                        {
                            MathAnswers newAnswer = GameObject.Instantiate(GameSettings.Instance.MathAnswerPrefab, pos, Quaternion.identity);
                            newAnswer.SetCategory(GameSettings.Instance.GetNextCategory());
                            newAnswer.transform.DOScale(1, 0.2f).From(0);
                        });
                        trail.SetActive(false);
                    });


                return;
            }
        }
    }
}
