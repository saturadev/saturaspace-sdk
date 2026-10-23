using UnityEditor;
using UnityEngine;

namespace SaturaSpace.Anim.Editor
{
    [CustomEditor(typeof(CutsceneAnimationClip))]
    [CanEditMultipleObjects]
    public sealed class CutsceneAnimationClipEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, "m_Script");
            serializedObject.ApplyModifiedProperties();
            if (targets.Length != 1) return;
            var c = (CutsceneAnimationClip)target;
            if (c.Clip == null) return;
            float fit = c.Clip.length / Mathf.Max(0.01f, Mathf.Abs(c.Speed)) - c.ClipIn / Mathf.Max(0.01f, Mathf.Abs(c.Speed));
            float match = MatchSpeed(c);
            EditorGUILayout.Space(2);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                using (new EditorGUI.DisabledScope(fit <= 0f || Mathf.Abs(fit - c.Duration) < 1e-3f))
                    if (GUILayout.Button(new GUIContent("Fit to animation", "Make the clip exactly as long as one pass of its animation at this speed")))
                    {
                        Undo.RecordObject(c, "Fit to animation");
                        c.Duration = fit;
                        EditorUtility.SetDirty(c);
                    }
                using (new EditorGUI.DisabledScope(match <= 0f || Mathf.Abs(match - c.Speed) < 1e-3f))
                    if (GUILayout.Button(new GUIContent(match > 0f ? "Match the move (" + match.ToString("0.##") + "×)" : "Match the move", "Play the animation as fast as the character travels here, so the feet don't slide")))
                    {
                        Undo.RecordObject(c, "Match the move");
                        c.Speed = match;
                        EditorUtility.SetDirty(c);
                    }
            }
        }

        static float MatchSpeed(CutsceneAnimationClip c)
        {
            var track = c.GetComponentInParent<CutsceneAnimationTrack>();
            var cs = c.GetComponentInParent<SaturaCutscene>();
            if (track == null || cs == null || track.Actor == null) return 0f;
            var dir = cs.DirectorClip;
            var walk = new Vector2(c.Clip.averageSpeed.x, c.Clip.averageSpeed.z).magnitude;
            if (dir == null || walk < 0.05f || !track.Actor.transform.IsChildOf(cs.transform)) return 0f;
            var path = AnimationUtility.CalculateTransformPath(track.Actor.transform, cs.transform);
            var cx = AnimationUtility.GetEditorCurve(dir, EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalPosition.x"));
            var cz = AnimationUtility.GetEditorCurve(dir, EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalPosition.z"));
            if (cx == null && cz == null) return 0f;
            float dist = 0f;
            Vector2 prev = default;
            const int n = 32;
            for (int i = 0; i <= n; i++)
            {
                float t = c.StartTime + c.Duration * i / n;
                var p = new Vector2(cx != null ? cx.Evaluate(t) : 0f, cz != null ? cz.Evaluate(t) : 0f);
                if (i > 0) dist += (p - prev).magnitude;
                prev = p;
            }
            float move = dist / Mathf.Max(0.01f, c.Duration);
            if (move < 0.02f) return 0f;
            return Mathf.Round(move / walk * 100f) / 100f;
        }
    }
}
