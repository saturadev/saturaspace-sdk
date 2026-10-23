using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace SaturaSpace.Anim.Editor
{
    [CustomEditor(typeof(SaturaAnimatorController))]
    public sealed class SaturaAnimatorControllerEditor : UnityEditor.Editor
    {
        ReorderableList _list;
        SerializedProperty _layer;

        void OnEnable()
        {
            var layers = serializedObject.FindProperty("layers");
            if (layers.arraySize == 0)
            {
                layers.arraySize = 1;
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }
            _layer = layers.GetArrayElementAtIndex(0);
            var states = _layer.FindPropertyRelative("states");
            _list = new ReorderableList(serializedObject, states, true, true, true, true);
            _list.drawHeaderCallback = r => EditorGUI.LabelField(r, new GUIContent("States", "Each state plays one clip. The default state starts when the object is enabled."));
            _list.elementHeight = EditorGUIUtility.singleLineHeight * 2 + 6;
            _list.drawElementCallback = (r, i, active, focused) =>
            {
                var el = states.GetArrayElementAtIndex(i);
                float h = EditorGUIUtility.singleLineHeight;
                var top = new Rect(r.x, r.y + 2, r.width, h);
                var bottom = new Rect(r.x, r.y + h + 4, r.width, h);
                float half = top.width * 0.45f;
                EditorGUI.PropertyField(new Rect(top.x, top.y, half - 4, h), el.FindPropertyRelative("name"), GUIContent.none);
                EditorGUI.PropertyField(new Rect(top.x + half, top.y, top.width - half, h), el.FindPropertyRelative("clip"), GUIContent.none);
                float lw = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = 44;
                EditorGUI.PropertyField(new Rect(bottom.x, bottom.y, half - 4, h), el.FindPropertyRelative("speed"), new GUIContent("Speed"));
                EditorGUI.PropertyField(new Rect(bottom.x + half, bottom.y, 80, h), el.FindPropertyRelative("loop"), new GUIContent("Loop"));
                EditorGUIUtility.labelWidth = lw;
            };
            _list.onAddCallback = l =>
            {
                var ctrl = (SaturaAnimatorController)target;
                Undo.RecordObject(ctrl, "Add State");
                ctrl.AddState("State", null);
                EditorUtility.SetDirty(ctrl);
                serializedObject.Update();
            };
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(_layer.FindPropertyRelative("name"), new GUIContent("Layer", "The layer name. One layer for now."));
            var states = _layer.FindPropertyRelative("states");
            var names = new List<string>();
            for (int i = 0; i < states.arraySize; i++) names.Add(states.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue);
            var def = _layer.FindPropertyRelative("defaultState");
            int at = Mathf.Max(0, names.IndexOf(def.stringValue));
            if (names.Count > 0)
            {
                int pick = EditorGUILayout.Popup(new GUIContent("Default State", "The state that plays when the object is enabled."), at, names.ToArray());
                if (pick != at) def.stringValue = names[pick];
            }
            EditorGUILayout.Space();
            _list.DoLayoutList();
            serializedObject.ApplyModifiedProperties();
        }
    }
}
