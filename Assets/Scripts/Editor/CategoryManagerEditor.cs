using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CategoryManager))]
public class CategoryManagerEditor : Editor
{
    SerializedProperty spawnOnStartProp;
    SerializedProperty shouldBlastAtStartProp;
    SerializedProperty gameplayTypeProp;
    SerializedProperty levelDataProp;
    SerializedProperty liquidLevelDataProp;
    SerializedProperty horizontalAlignmentProp;

    private void OnEnable()
    {
        spawnOnStartProp = serializedObject.FindProperty("spawnOnStart");
        shouldBlastAtStartProp = serializedObject.FindProperty("shouldBlastAtStart");
        gameplayTypeProp = serializedObject.FindProperty("gameplayType");
        levelDataProp = serializedObject.FindProperty("levelData");
        liquidLevelDataProp = serializedObject.FindProperty("liquidLevelData");
        horizontalAlignmentProp = serializedObject.FindProperty("horizontalAlignment");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(spawnOnStartProp);
        EditorGUILayout.PropertyField(shouldBlastAtStartProp);
        EditorGUILayout.PropertyField(gameplayTypeProp);

        CategoryManager.GameplayType gameplayType = (CategoryManager.GameplayType)gameplayTypeProp.enumValueIndex;

        if (gameplayType == CategoryManager.GameplayType.MERGE)
        {
            EditorGUILayout.PropertyField(levelDataProp);
        }
        else if (gameplayType == CategoryManager.GameplayType.FILL)
        {
            EditorGUILayout.PropertyField(liquidLevelDataProp);
        }

        EditorGUILayout.PropertyField(horizontalAlignmentProp);

        serializedObject.ApplyModifiedProperties();
    }
}
