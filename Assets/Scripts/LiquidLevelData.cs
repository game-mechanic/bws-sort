using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "LiquidLevelData", menuName = "ScriptableObjects/LiquidLevelData")]
public class LiquidLevelData : ScriptableObject
{
    [System.Serializable]
    public class LiquidBubbleData
    {
        [Tooltip("Max 4 colors. Each color corresponds to a LiquidLayer.")]
        public List<GameSettings.LiquidColorType> colors = new List<GameSettings.LiquidColorType>();
    }

    [Tooltip("Each element corresponds to a spawned bubble.")]
    public List<LiquidBubbleData> bubbleDatas = new List<LiquidBubbleData>();
}
