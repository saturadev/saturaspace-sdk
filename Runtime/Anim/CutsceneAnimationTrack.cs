using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace SaturaSpace.Anim
{
    [AddComponentMenu("Satura/Cutscene/Animation Track")]
    public sealed class CutsceneAnimationTrack : CutsceneTrack
    {
        [Tooltip("The character or object these clips animate.")]
        [SerializeField] Animator actor;

        readonly List<CutsceneAnimationClip> _clips = new List<CutsceneAnimationClip>();
        PlayableGraph _graph;
        AnimationMixerPlayable _mixer;
        AnimationClipPlayable _a;
        AnimationClipPlayable _b;
        AnimationClip _aClip;
        AnimationClip _bClip;
        Animator _for;
        SaturaAnimator _paused;

        public Animator Actor
        {
            get => actor;
            set
            {
                if (actor == value) return;
                Release();
                actor = value;
            }
        }

        public bool IsDriving => _graph.IsValid();

        public List<CutsceneAnimationClip> Clips()
        {
            var list = Items(new List<CutsceneAnimationClip>());
            list.Sort((x, y) => x.StartTime.CompareTo(y.StartTime));
            return list;
        }

        public override float End
        {
            get
            {
                float end = 0f;
                foreach (var c in Items(_clips))
                    if (c.End > end) end = c.End;
                return end;
            }
        }

        public override void Evaluate(SaturaCutscene cutscene, double time)
        {
            if (actor == null)
            {
                Release();
                return;
            }
            var list = Items(_clips);
            list.Sort((x, y) => x.StartTime.CompareTo(y.StartTime));
            int at = -1;
            for (int i = 0; i < list.Count; i++)
                if (list[i].Clip != null && list[i].StartTime <= time + 1e-4) at = i;
            if (at < 0)
            {
                Release();
                return;
            }
            var cur = list[at];
            CutsceneAnimationClip prev = null;
            for (int i = at - 1; i >= 0 && prev == null; i--)
                if (list[i].Clip != null) prev = list[i];
            if (prev != null && prev.End > time && prev.End > cur.StartTime)
            {
                float mix = Mathf.Clamp01((float)((time - cur.StartTime) / (prev.End - cur.StartTime)));
                Drive(prev.Clip, prev.LocalTime(time), cur.Clip, cur.LocalTime(time), mix);
            }
            else Drive(cur.Clip, cur.LocalTime(time), null, 0, 0f);
        }

        void Drive(AnimationClip a, double ta, AnimationClip b, double tb, float mix)
        {
            if (_for != actor) Release();
            if (!_graph.IsValid())
            {
                if (Application.isPlaying)
                {
                    var sa = actor.GetComponent<SaturaAnimator>();
                    if (sa != null && sa.Graph.IsValid())
                    {
                        sa.Stop();
                        _paused = sa;
                    }
                }
                _graph = PlayableGraph.Create(name + " (Satura Cutscene)");
                _graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                var output = AnimationPlayableOutput.Create(_graph, "Cutscene", actor);
                _mixer = AnimationMixerPlayable.Create(_graph, 2);
                output.SetSourcePlayable(_mixer);
                _graph.Play();
                _for = actor;
            }
            _a = Slot(_a, ref _aClip, a, 0);
            _b = Slot(_b, ref _bClip, b, 1);
            if (_a.IsValid()) SetTime(_a, ta);
            if (_b.IsValid()) SetTime(_b, tb);
            float w = b == null ? 0f : mix;
            _mixer.SetInputWeight(0, 1f - w);
            _mixer.SetInputWeight(1, w);
            _graph.Evaluate(0f);
        }

        AnimationClipPlayable Slot(AnimationClipPlayable cur, ref AnimationClip curClip, AnimationClip want, int input)
        {
            if (cur.IsValid() && curClip == want) return cur;
            if (cur.IsValid())
            {
                _mixer.DisconnectInput(input);
                _graph.DestroyPlayable(cur);
            }
            curClip = want;
            if (want == null) return default;
            var p = AnimationClipPlayable.Create(_graph, want);
            p.SetApplyFootIK(false);
            p.SetSpeed(0.0);
            _mixer.ConnectInput(input, p, 0, 1f);
            return p;
        }

        static void SetTime(AnimationClipPlayable p, double t)
        {
            p.SetTime(t);
            p.SetTime(t);
        }

        public override void Release()
        {
            if (_graph.IsValid()) _graph.Destroy();
            _graph = default;
            _mixer = default;
            _a = _b = default;
            _aClip = _bClip = null;
            _for = null;
            var sa = _paused;
            _paused = null;
            if (sa != null && Application.isPlaying && sa.isActiveAndEnabled && sa.PlayOnEnable) sa.PlayDefault();
        }

        void OnDisable()
        {
            Release();
        }
    }
}
