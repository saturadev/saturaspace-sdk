using UnityEditor;
using UnityEngine;

namespace SaturaSpace.Anim.Editor
{
    [CustomEditor(typeof(SaturaAnimator))]
    public sealed class SaturaAnimatorEditor : UnityEditor.Editor
    {
        SerializedProperty _controller;
        SerializedProperty _playOnEnable;
        SerializedProperty _speed;

        void OnEnable()
        {
            _controller = serializedObject.FindProperty("controller");
            _playOnEnable = serializedObject.FindProperty("playOnEnable");
            _speed = serializedObject.FindProperty("speed");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(_controller, new GUIContent("Controller", "The Satura Animator Controller that lists this object's states and their clips."));
            EditorGUILayout.PropertyField(_playOnEnable, new GUIContent("Play On Enable", "Start the default state when the object is enabled in Play Mode."));
            EditorGUILayout.PropertyField(_speed, new GUIContent("Speed", "Multiplies the speed of every state."));
            serializedObject.ApplyModifiedProperties();

            var sa = (SaturaAnimator)target;
            var anim = sa.GetComponent<Animator>();
            if (anim != null && anim.runtimeAnimatorController != null)
                EditorGUILayout.HelpBox("The Animator on this object also has a Controller. Satura Animator plays its own states, so clear the Animator's Controller field.", MessageType.Warning);
            var ctrl = sa.Controller;
            if (ctrl == null)
            {
                EditorGUILayout.HelpBox("Assign a Satura Animator Controller.", MessageType.Info);
                return;
            }
            if (ctrl.States.Count == 0)
            {
                EditorGUILayout.HelpBox("The controller has no states yet.", MessageType.Info);
                return;
            }
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("States", EditorStyles.boldLabel);
            foreach (var st in ctrl.States)
            {
                if (st == null) continue;
                var label = st.name + (st.name == ctrl.DefaultState ? " (default)" : "");
                EditorGUILayout.LabelField(label, st.clip != null ? st.clip.name + (st.loop ? ", loops" : ", once") : "no clip");
            }
            if (!Application.isPlaying) return;
            EditorGUILayout.Space();
            var cur = sa.CurrentStateName;
            EditorGUILayout.LabelField("Playing", cur == null ? "nothing" : cur + "  " + sa.Time.ToString("0.00") + " s" + (sa.IsPaused ? " (paused)" : sa.IsDone ? " (done)" : ""));
            Repaint();
        }
    }
}
