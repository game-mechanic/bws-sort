using Dreamteck.Splines.Editor;
using UnityEditor;
using UnityEngine;

namespace Dreamteck.Splines.Primitives
{
    public class RectangularSpiralEditor : PrimitiveEditor
    {
        public override string GetName()
        {
            return "Rectangular Spiral";
        }

        public override void Open(DreamteckSplinesEditor editor)
        {
            base.Open(editor);
            primitive = new RectangularSpiral();
            primitive.offset = origin;
        }

        protected override void OnGUI()
        {
            base.OnGUI();

            RectangularSpiral spiral = (RectangularSpiral)primitive;

            // ── Start size ────────────────────────────────────────────────────
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Start", EditorStyles.boldLabel);
            spiral.startWidth = EditorGUILayout.FloatField("Start Width", spiral.startWidth);
            spiral.startHeight = EditorGUILayout.FloatField("Start Height", spiral.startHeight);

            // ── End size ──────────────────────────────────────────────────────
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("End", EditorStyles.boldLabel);
            spiral.endWidth = EditorGUILayout.FloatField("End Width", spiral.endWidth);
            spiral.endHeight = EditorGUILayout.FloatField("End Height", spiral.endHeight);

            // ── Spiral ────────────────────────────────────────────────────────
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Spiral", EditorStyles.boldLabel);
            spiral.spacing = EditorGUILayout.FloatField("Spacing (per edge)", spiral.spacing);
            spiral.clockwise = EditorGUILayout.Toggle("Clockwise", spiral.clockwise);

            // Read-only derived info
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.IntField(
                    new GUIContent("Total Sides", "Total individual edges generated across all turns"),
                    spiral.TotalSides);
                EditorGUILayout.FloatField(
                    new GUIContent("Turns (approx)", "Total sides / 4"),
                    spiral.TurnsApprox);
            }

            // ── Corners ───────────────────────────────────────────────────────
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Corners", EditorStyles.boldLabel);
            spiral.cornerRadius = EditorGUILayout.FloatField("Corner Radius", spiral.cornerRadius);
            spiral.cornerSegments = EditorGUILayout.IntSlider("Corner Segments", spiral.cornerSegments, 1, 16);

            // ── Clamp / validate ──────────────────────────────────────────────
            spiral.startWidth = Mathf.Max(0.1f, spiral.startWidth);
            spiral.startHeight = Mathf.Max(0.1f, spiral.startHeight);
            spiral.endWidth = Mathf.Max(0.1f, spiral.endWidth);
            spiral.endHeight = Mathf.Max(0.1f, spiral.endHeight);
            spiral.spacing = Mathf.Max(0.01f, spiral.spacing);
            spiral.cornerRadius = Mathf.Max(0f, spiral.cornerRadius);
            spiral.cornerSegments = Mathf.Max(1, spiral.cornerSegments);
        }
    }
}