using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(LiquidLayer))]
public class LiquidLayerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        LiquidLayer liquidLayer = (LiquidLayer)target;

        EditorGUI.BeginChangeCheck();
        
        GameSettings.LiquidColorType newColorType = (GameSettings.LiquidColorType)EditorGUILayout.EnumPopup("Liquid Color", liquidLayer.selectedColorType);

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(liquidLayer, "Change Liquid Color");
            liquidLayer.selectedColorType = newColorType;

            if (GameSettings.Instance != null && GameSettings.Instance.LiquidColors != null)
            {
                foreach (var mapping in GameSettings.Instance.LiquidColors)
                {
                    if (mapping.colorType == newColorType)
                    {
                        if (liquidLayer.LiquidRenderer != null)
                        {
                            Undo.RecordObject(liquidLayer.LiquidRenderer, "Change Renderer Color");
                            liquidLayer.LiquidRenderer.color = mapping.color;
                            EditorUtility.SetDirty(liquidLayer.LiquidRenderer);
                        }
                        break;
                    }
                }
            }
            EditorUtility.SetDirty(liquidLayer);
        }
    }
}
