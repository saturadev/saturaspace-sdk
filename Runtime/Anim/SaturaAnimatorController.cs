using System;
using System.Collections.Generic;
using UnityEngine;

namespace SaturaSpace.Anim
{
    [Serializable]
    public sealed class SaturaAnimState
    {
        public string name = "State";
        public AnimationClip clip;
        [Tooltip("IK goals for this state. They live in their own clip, play on top of the state's clip and follow its time, so the clip itself is never changed.")]
        public AnimationClip ikClip;
        public float speed = 1f;
        public bool loop = true;
    }

    [Serializable]
    public sealed class SaturaAnimLayer
    {
        public string name = "Base";
        public string defaultState = "";
        public List<SaturaAnimState> states = new List<SaturaAnimState>();
    }

    [CreateAssetMenu(menuName = "Satura/Animator Controller", fileName = "New Satura Animator Controller", order = 400)]
    public sealed class SaturaAnimatorController : ScriptableObject
    {
        [SerializeField] List<SaturaAnimLayer> layers = new List<SaturaAnimLayer> { new SaturaAnimLayer() };

        [NonSerialized] int _version;

        public int Version => _version;

        public IReadOnlyList<SaturaAnimLayer> Layers
        {
            get
            {
                EnsureBase();
                return layers;
            }
        }

        public SaturaAnimLayer BaseLayer
        {
            get
            {
                EnsureBase();
                return layers[0];
            }
        }

        public IReadOnlyList<SaturaAnimState> States => BaseLayer.states;

        public string DefaultState
        {
            get => BaseLayer.defaultState;
            set
            {
                BaseLayer.defaultState = value ?? "";
                MarkChanged();
            }
        }

        public SaturaAnimState DefaultOrFirst
        {
            get
            {
                var s = FindState(BaseLayer.defaultState);
                if (s != null) return s;
                foreach (var st in BaseLayer.states)
                    if (st != null) return st;
                return null;
            }
        }

        public SaturaAnimState FindState(string stateName)
        {
            if (string.IsNullOrEmpty(stateName)) return null;
            foreach (var st in BaseLayer.states)
                if (st != null && st.name == stateName) return st;
            return null;
        }

        public SaturaAnimState FindStateByClip(AnimationClip clip)
        {
            if (clip == null) return null;
            foreach (var st in BaseLayer.states)
                if (st != null && st.clip == clip) return st;
            return null;
        }

        public SaturaAnimState FindStateByIkClip(AnimationClip ikClip)
        {
            if (ikClip == null) return null;
            foreach (var layer in Layers)
                foreach (var st in layer.states)
                    if (st != null && st.ikClip == ikClip) return st;
            return null;
        }

        public List<AnimationClip> IkClips
        {
            get
            {
                var list = new List<AnimationClip>();
                foreach (var st in BaseLayer.states)
                    if (st != null && st.ikClip != null && !list.Contains(st.ikClip)) list.Add(st.ikClip);
                return list;
            }
        }

        public List<AnimationClip> Clips
        {
            get
            {
                var list = new List<AnimationClip>();
                foreach (var st in BaseLayer.states)
                    if (st != null && st.clip != null && !list.Contains(st.clip)) list.Add(st.clip);
                return list;
            }
        }

        public string UniqueStateName(string wanted)
        {
            var root = string.IsNullOrWhiteSpace(wanted) ? "State" : wanted.Trim();
            if (FindState(root) == null) return root;
            for (int i = 1; ; i++)
            {
                var n = root + " " + i;
                if (FindState(n) == null) return n;
            }
        }

        public SaturaAnimState AddState(string stateName, AnimationClip clip, float speed = 1f, bool loop = true)
        {
            var st = new SaturaAnimState { name = UniqueStateName(stateName), clip = clip, speed = speed, loop = loop };
            BaseLayer.states.Add(st);
            if (string.IsNullOrEmpty(BaseLayer.defaultState)) BaseLayer.defaultState = st.name;
            MarkChanged();
            return st;
        }

        public bool RemoveState(string stateName)
        {
            var st = FindState(stateName);
            if (st == null) return false;
            BaseLayer.states.Remove(st);
            if (BaseLayer.defaultState == stateName) BaseLayer.defaultState = BaseLayer.states.Count > 0 && BaseLayer.states[0] != null ? BaseLayer.states[0].name : "";
            MarkChanged();
            return true;
        }

        public int ReplaceClip(AnimationClip from, AnimationClip to)
        {
            int n = 0;
            foreach (var layer in layers)
                foreach (var st in layer.states)
                    if (st != null && st.clip == from)
                    {
                        st.clip = to;
                        n++;
                    }
            if (n > 0) MarkChanged();
            return n;
        }

        public void MarkChanged() => _version++;

        void EnsureBase()
        {
            if (layers == null) layers = new List<SaturaAnimLayer>();
            if (layers.Count == 0) layers.Add(new SaturaAnimLayer());
            if (layers[0].states == null) layers[0].states = new List<SaturaAnimState>();
        }

        void OnValidate() => MarkChanged();
    }
}
