using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class SceneViewHotkeys
{
    static SceneViewHotkeys()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private static void OnSceneGUI(SceneView sceneView)
    {
        Event e = Event.current;

        if (e.type != EventType.KeyDown || !e.isKey)
            return;

        bool reverse = e.control; // Ctrl pressed

        switch (e.keyCode)
        {
            case KeyCode.Keypad1:
                SetView(sceneView, Vector3.forward, reverse, sceneView.orthographic);
                e.Use();
                break;

            case KeyCode.Keypad3:
                SetView(sceneView, Vector3.right, reverse, sceneView.orthographic);
                e.Use();
                break;

            case KeyCode.Keypad5:
                SetView(sceneView, sceneView.camera.transform.forward, true, !sceneView.orthographic);
                e.Use();
                break;

            case KeyCode.Keypad7:
                SetView(sceneView, Vector3.up, reverse, sceneView.orthographic);
                e.Use();
                break;
        }
    }

    private static void SetView(SceneView sceneView, Vector3 direction, bool reverse,bool ortho=false)
    {
        if (sceneView == null) return;

        if (reverse)
            direction = -direction;

        Quaternion rotation = Quaternion.LookRotation(-direction);

        sceneView.LookAt(
            sceneView.pivot,
            rotation,
            sceneView.size,
            ortho
        );

        sceneView.Repaint();
    }
}