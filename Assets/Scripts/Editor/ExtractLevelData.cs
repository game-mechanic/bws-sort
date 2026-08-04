using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ExtractLevelData
{
    private const string OutputFolder = "Assets/LevelData";

    [MenuItem("Tools/BWS/Extract Level Data from Scenes")]
    public static void Extract()
    {
        // Ensure output folder exists
        if (!AssetDatabase.IsValidFolder(OutputFolder))
        {
            AssetDatabase.CreateFolder("Assets", "LevelData");
        }

        // Save the currently open scene so we can restore it later
        string currentScenePath = SceneManager.GetActiveScene().path;
        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

        string[] sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" });
        int totalCreated = 0;

        foreach (string guid in sceneGuids)
        {
            string scenePath = AssetDatabase.GUIDToAssetPath(guid);
            string sceneName = Path.GetFileNameWithoutExtension(scenePath);

            // Open the scene
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            // Find all CategoryManagers in the scene (including disabled GameObjects)
            CategoryManager[] managers = Object.FindObjectsByType<CategoryManager>(FindObjectsInactive.Include, FindObjectsSortMode.InstanceID);

            if (managers.Length == 0)
            {
                Debug.LogWarning($"[ExtractLevelData] No CategoryManager found in scene: {sceneName}");
                continue;
            }

            for (int i = 0; i < managers.Length; i++)
            {
                CategoryManager manager = managers[i];
                string assetName = managers.Length == 1
                    ? sceneName
                    : (i == 0 ? sceneName : $"{sceneName}_{i + 1}");

                // Sanitize filename (remove characters invalid in file paths)
                string safeAssetName = assetName
                    .Replace(":", "_")
                    .Replace("?", "_")
                    .Replace("*", "_")
                    .Replace("<", "_")
                    .Replace(">", "_")
                    .Replace("|", "_");

                string assetPath = $"{OutputFolder}/{safeAssetName}.asset";

                // Create the LevelData ScriptableObject
                LevelData levelData = ScriptableObject.CreateInstance<LevelData>();

                // Copy data using SerializedObject to access private/serialized fields
                SerializedObject serializedManager = new SerializedObject(manager);
                SerializedProperty datasProperty = serializedManager.FindProperty("datas");
                SerializedProperty initialSpawnsProperty = serializedManager.FindProperty("initialSpawns");
                SerializedProperty minMultiplierProperty = serializedManager.FindProperty("minMultiplier");
                SerializedProperty maxMultiplierProperty = serializedManager.FindProperty("maxMultiplier");

                // Copy scalar fields
                levelData.initialSpawns = initialSpawnsProperty.intValue;
                levelData.minMultiplier = minMultiplierProperty.floatValue;
                levelData.maxMultiplier = maxMultiplierProperty.floatValue;

                // Copy the datas list
                levelData.datas = new List<LevelData.Data>();
                for (int j = 0; j < datasProperty.arraySize; j++)
                {
                    SerializedProperty element = datasProperty.GetArrayElementAtIndex(j);

                    SerializedProperty nameProp = element.FindPropertyRelative("name");
                    SerializedProperty overrideColorProp = element.FindPropertyRelative("overrideColor");
                    SerializedProperty bubbleColorProp = element.FindPropertyRelative("bubbleColor");
                    SerializedProperty dataProp = element.FindPropertyRelative("newDatas"); // Now representing the List<Bubble.Data>

                    List<Bubble.Data> copiedBubbleData = new List<Bubble.Data>();
                    if (dataProp != null && dataProp.isArray)
                    {
                        for (int k = 0; k < dataProp.arraySize; k++)
                        {
                            SerializedProperty bDataElement = dataProp.GetArrayElementAtIndex(k);
                            SerializedProperty dataNameProp = bDataElement.FindPropertyRelative("name");
                            SerializedProperty dataIconProp = bDataElement.FindPropertyRelative("icon");
                            SerializedProperty dataAnimClipProp = bDataElement.FindPropertyRelative("animationClip");
                            SerializedProperty dataShowBothProp = bDataElement.FindPropertyRelative("showBothTxtAndImg");

                            copiedBubbleData.Add(new Bubble.Data
                            {
                                name = dataNameProp != null ? dataNameProp.stringValue : "",
                                icon = dataIconProp != null ? dataIconProp.objectReferenceValue as Sprite : null,
                                animationClip = dataAnimClipProp != null ? dataAnimClipProp.objectReferenceValue as AnimationClip : null,
                                showBothTxtAndImg = dataShowBothProp != null ? dataShowBothProp.boolValue : false
                            });
                        }
                    }

                    LevelData.Data entry = new LevelData.Data
                    {
                        name = nameProp.objectReferenceValue as BubbleType,
                        overrideColor = overrideColorProp.boolValue,
                        bubbleColor = bubbleColorProp.colorValue,
                        newDatas = copiedBubbleData
                    };

                    levelData.datas.Add(entry);
                }

                // Delete existing asset if it exists to avoid CreateAsset errors
                if (AssetDatabase.LoadAssetAtPath<LevelData>(assetPath) != null)
                {
                    AssetDatabase.DeleteAsset(assetPath);
                }

                // Save the asset
                AssetDatabase.CreateAsset(levelData, assetPath);
                totalCreated++;

                // Assign the LevelData asset back to the CategoryManager's levelData field
                SerializedProperty levelDataProperty = serializedManager.FindProperty("levelData");
                levelDataProperty.objectReferenceValue = levelData;
                serializedManager.ApplyModifiedProperties();
                EditorUtility.SetDirty(manager);

                bool isEnabled = manager.enabled;
                Debug.Log($"[ExtractLevelData] Created & Assigned: {assetPath} ({levelData.datas.Count} entries, enabled={isEnabled})");
            }

            // Mark the scene dirty and save it with the assigned levelData references
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Restore the original scene
        if (!string.IsNullOrEmpty(currentScenePath))
        {
            EditorSceneManager.OpenScene(currentScenePath, OpenSceneMode.Single);
        }

        Debug.Log($"[ExtractLevelData] Done! Created {totalCreated} LevelData assets in {OutputFolder}/");
        EditorUtility.DisplayDialog("Extract Level Data",
            $"Successfully created {totalCreated} LevelData assets in {OutputFolder}/",
            "OK");
    }
}
