using System.Reflection;
using UnityEditor;
using UnityEngine;

[CanEditMultipleObjects]
[CustomEditor(typeof(MonoBehaviour), true)]
public class EditorButtonDrawer : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        DrawButtons(target, targets);
    }

    public static void DrawButtons(Object target, Object[] targets)
    {
        var targetType = target.GetType();
        var methods = targetType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        foreach (var method in methods)
        {
            var attr = method.GetCustomAttribute<EditorButtonAttribute>();
            if (attr != null)
            {
                string buttonName = string.IsNullOrEmpty(attr.ButtonName) ? method.Name : attr.ButtonName;
                if (GUILayout.Button(buttonName))
                {
                    foreach (var t in targets)
                    {
                        method.Invoke(t, null);
                    }
                }
            }
        }
    }
}

[CanEditMultipleObjects]
[CustomEditor(typeof(ScriptableObject), true)]
public class ScriptableObjectButtonDrawer : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        EditorButtonDrawer.DrawButtons(target, targets);
    }
}
