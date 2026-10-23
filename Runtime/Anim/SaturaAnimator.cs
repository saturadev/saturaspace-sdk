using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace SaturaSpace.Anim
{
    [RequireComponent(typeof(Animator))]
    [DisallowMultipleComponent]
    [AddComponentMenu("Satura/Satura Animator")]
    public sealed class SaturaAnimator : MonoBehaviour
    {
        [SerializeField] SaturaAnimatorController controller;
        [SerializeField] bool playOnEnable = true;
        [SerializeField] float speed = 1f;

        Animator _animator;
        PlayableGraph _graph;
        AnimationPlayableOutput _output;
        AnimationClipPlayable _clip;
        AnimationMixerPlayable _blend;
        AnimationClipPlayable _from;
        float _fade;
        float _fadeLen;
        bool _manual;
        AnimationClipPlayable _ikClip;
        AnimationScriptPlayable _poseNode;
        AnimationLayerMixerPlayable _mixer;
        AnimatorControllerPlayable _ik;
        SaturaIKPose _pose;
        SaturaIK _poseFor;
        GameObject _ikSampler;
        SaturaIK _ikSampled;
        readonly Transform[] _ikSampledGoals = new Transform[4];
        readonly Transform[] _ikSampledFor = new Transform[4];
        SaturaAnimState _state;
        bool _paused;
        bool _done;

        public static event Action<SaturaAnimator> GraphCreated;
        public static event Action<SaturaAnimator> BeforeAnimate;
        public static event Action<SaturaAnimator> GraphDestroying;

        public event Action<SaturaAnimator> Finished;

        public bool ManualTick
        {
            get => _manual;
            set
            {
                _manual = value;
                if (_graph.IsValid()) _graph.SetTimeUpdateMode(value ? DirectorUpdateMode.Manual : DirectorUpdateMode.GameTime);
            }
        }

        public void Tick(float deltaTime)
        {
            if (!_graph.IsValid()) return;
            SyncLayers();
            _graph.Evaluate(deltaTime);
        }

        public SaturaAnimatorController Controller
        {
            get => controller;
            set
            {
                if (controller == value) return;
                controller = value;
                if (!_graph.IsValid()) return;
                StopGraph();
                if (Application.isPlaying && isActiveAndEnabled && playOnEnable) PlayDefault();
            }
        }

        public bool PlayOnEnable
        {
            get => playOnEnable;
            set => playOnEnable = value;
        }

        public Animator Animator
        {
            get
            {
                if (_animator == null) _animator = GetComponent<Animator>();
                return _animator;
            }
        }

        public PlayableGraph Graph => _graph;

        public bool HasIK => _ik.IsValid();

        public AnimationClip CurrentIkClip => _ikClip.IsValid() && _state != null ? _state.ikClip : null;

        public double IkTime => _ikClip.IsValid() ? _ikClip.GetTime() : 0;

        public SaturaAnimState CurrentState => _state;

        public string CurrentStateName => _state != null ? _state.name : null;

        public AnimationClip CurrentClip => _state != null ? _state.clip : null;

        public bool IsPlaying => _clip.IsValid() && !_paused && !_done;

        public bool IsPaused => _paused;

        public bool IsDone => _done;

        public float Speed
        {
            get => speed;
            set
            {
                speed = value;
                ApplySpeed();
            }
        }

        public float Length => CurrentClip != null ? CurrentClip.length : 0f;

        public double Time
        {
            get
            {
                if (!_clip.IsValid()) return 0;
                double t = _clip.GetTime();
                float len = Length;
                if (len <= 0f) return t;
                if (_state != null && _state.loop) return t % len;
                return t < len ? t : len;
            }
            set
            {
                if (!_clip.IsValid()) return;
                _clip.SetTime(value);
                _clip.SetTime(value);
                _done = false;
                ApplySpeed();
                SyncIkTime(0.0);
            }
        }

        public float NormalizedTime => Length > 0f ? (float)(Time / Length) : 0f;

        public bool PlayDefault()
        {
            var st = controller != null ? controller.DefaultOrFirst : null;
            return st != null && Play(st);
        }

        public bool Play(string stateName, float normalizedTime = 0f)
        {
            var st = controller != null ? controller.FindState(stateName) : null;
            return st != null && Play(st, normalizedTime);
        }

        public bool Play(SaturaAnimState state, float normalizedTime = 0f)
        {
            return Enter(state, normalizedTime, 0f);
        }

        public bool CrossFade(string stateName, float duration, float normalizedTime = 0f)
        {
            var st = controller != null ? controller.FindState(stateName) : null;
            return st != null && Enter(st, normalizedTime, duration);
        }

        public bool CrossFade(SaturaAnimState state, float duration, float normalizedTime = 0f)
        {
            return Enter(state, normalizedTime, duration);
        }

        bool Enter(SaturaAnimState state, float normalizedTime, float fade)
        {
            if (state == null || state.clip == null) return false;
            EnsureGraph();
            if (_poseNode.IsValid()) _poseNode.DisconnectInput(0);
            if (_from.IsValid())
            {
                _blend.DisconnectInput(1);
                _graph.DestroyPlayable(_from);
                _from = default;
            }
            if (_clip.IsValid())
            {
                _blend.DisconnectInput(0);
                if (fade > 0f)
                {
                    _from = _clip;
                    _blend.ConnectInput(1, _from, 0, 1f);
                }
                else _graph.DestroyPlayable(_clip);
            }
            _fade = 0f;
            _fadeLen = _from.IsValid() ? fade : 0f;
            if (_ikClip.IsValid())
            {
                if (_mixer.IsValid()) _mixer.DisconnectInput(1);
                _graph.DestroyPlayable(_ikClip);
                _ikClip = default;
            }
            _state = state;
            _clip = AnimationClipPlayable.Create(_graph, state.clip);
            _clip.SetApplyFootIK(false);
            _blend.ConnectInput(0, _clip, 0, 1f);
            ApplyFade();
            ConnectIK();
            double t = Mathf.Max(0f, normalizedTime) * state.clip.length;
            _clip.SetTime(t);
            _clip.SetTime(t);
            _paused = false;
            _done = false;
            ApplySpeed();
            SyncIkTime(0.0);
            if (!_graph.IsPlaying()) _graph.Play();
            return true;
        }

        public void Pause()
        {
            _paused = true;
            ApplySpeed();
        }

        public void Resume()
        {
            _paused = false;
            ApplySpeed();
        }

        public void Stop()
        {
            StopGraph();
        }

        public void Evaluate()
        {
            if (!_graph.IsValid()) return;
            SyncLayers();
            _graph.Evaluate(0f);
        }

        public void SyncLayers()
        {
            if (_graph.IsValid()) BeforeAnimate?.Invoke(this);
        }

        void EnsureGraph()
        {
            if (_graph.IsValid()) return;
            _graph = PlayableGraph.Create(name + " (Satura Animator)");
            _graph.SetTimeUpdateMode(_manual ? DirectorUpdateMode.Manual : DirectorUpdateMode.GameTime);
            _output = AnimationPlayableOutput.Create(_graph, "Satura Animator", Animator);
            _blend = AnimationMixerPlayable.Create(_graph, 2);
            GraphCreated?.Invoke(this);
        }

        void ConnectIK()
        {
            bool want = WantsIK();
            if (want && !_ik.IsValid())
            {
                _ik = AnimatorControllerPlayable.Create(_graph, SaturaIK.PassController);
                _mixer = AnimationLayerMixerPlayable.Create(_graph, 3);
                _mixer.ConnectInput(2, _ik, 0, 1f);
                if (_pose == null) _pose = new SaturaIKPose();
                _poseNode = _pose.CreatePlayable(_graph);
                _mixer.ConnectInput(0, _poseNode, 0, 1f);
                _poseFor = GetComponent<SaturaIK>();
                if (_poseFor != null) _poseFor.Pose = _pose;
            }
            else if (!want && _ik.IsValid())
            {
                DropIK();
            }
            if (_mixer.IsValid())
            {
                _poseNode.ConnectInput(0, _blend, 0, 1f);
                if (_state != null && _state.ikClip != null)
                {
                    _ikClip = AnimationClipPlayable.Create(_graph, _state.ikClip);
                    _ikClip.SetApplyFootIK(false);
                    _mixer.ConnectInput(1, _ikClip, 0, 1f);
                    _mixer.SetLayerMaskFromAvatarMask(1, SaturaIK.LayerMask);
                }
                _output.SetSourcePlayable(_mixer);
            }
            else _output.SetSourcePlayable(_blend);
        }

        void ApplyFade()
        {
            if (!_blend.IsValid()) return;
            float w = _from.IsValid() && _fadeLen > 0f ? Mathf.Clamp01(1f - _fade / _fadeLen) : 0f;
            _blend.SetInputWeight(0, 1f - w);
            _blend.SetInputWeight(1, w);
        }

        void StepFade(float dt)
        {
            if (!_from.IsValid()) return;
            _fade += dt;
            if (_fade >= _fadeLen)
            {
                _blend.DisconnectInput(1);
                _graph.DestroyPlayable(_from);
                _from = default;
            }
            ApplyFade();
        }

        void DropIK()
        {
            if (_ikClip.IsValid()) _graph.DestroyPlayable(_ikClip);
            if (_poseNode.IsValid()) _graph.DestroyPlayable(_poseNode);
            if (_mixer.IsValid()) _graph.DestroyPlayable(_mixer);
            if (_ik.IsValid()) _graph.DestroyPlayable(_ik);
            _ikClip = default;
            _poseNode = default;
            _mixer = default;
            _ik = default;
            ReleasePose();
        }

        void ReleasePose()
        {
            if (_poseFor != null && _poseFor.Pose == _pose) _poseFor.Pose = null;
            _poseFor = null;
            _pose?.Dispose();
            _pose = null;
        }

        void SyncIkTime(double ahead)
        {
            if (!_ikClip.IsValid() || !_clip.IsValid() || _state == null || _state.clip == null) return;
            double t = _clip.GetTime() + ahead * _clip.GetSpeed();
            float len = _state.clip.length;
            if (len > 0f && _state.clip.isLooping) t -= Math.Floor(t / len) * len;
            _ikClip.SetTime(t);
            _ikClip.SetTime(t);
            _ikClip.SetSpeed(0.0);
            SampleIkFields((float)t);
        }

        void SampleIkFields(float t)
        {
            var target = _poseFor;
            if (target == null || _state == null || _state.ikClip == null) return;
            if (_ikSampler == null)
            {
                _ikSampler = new GameObject(name + " IK sampler") { hideFlags = HideFlags.HideAndDontSave };
                _ikSampled = _ikSampler.AddComponent<SaturaIK>();
                _ikSampled.enabled = false;
                HideSampler(_ikSampler.transform);
            }
            for (int i = 0; i < 4; i++)
            {
                var g = target[i];
                var real = g.target;
                var mirror = SampledGoal(i, real, target.transform);
                if (mirror != null)
                {
                    mirror.localPosition = real.localPosition;
                    mirror.localRotation = real.localRotation;
                }
                g.target = mirror;
                _ikSampled[i] = g;
            }
            _state.ikClip.SampleAnimation(_ikSampler, t);
            for (int i = 0; i < 4; i++)
            {
                var s = _ikSampled[i];
                var g = target[i];
                var real = g.target;
                if (real != null && s.target != null)
                {
                    if (real.localPosition != s.target.localPosition) real.localPosition = s.target.localPosition;
                    if (real.localRotation != s.target.localRotation) real.localRotation = s.target.localRotation;
                }
                g.positionWeight = s.positionWeight;
                g.rotationWeight = s.rotationWeight;
                target[i] = g;
            }
        }

        Transform SampledGoal(int goal, Transform real, Transform root)
        {
            if (real == null) return null;
            if (_ikSampledFor[goal] == real && _ikSampledGoals[goal] != null) return _ikSampledGoals[goal];
            _ikSampledFor[goal] = real;
            _ikSampledGoals[goal] = null;
            var names = new List<string>();
            var n = real;
            for (; n != null && n != root; n = n.parent) names.Add(n.name);
            if (n == null) return null;
            var at = _ikSampler.transform;
            for (int i = names.Count - 1; i >= 0; i--)
            {
                var next = at.Find(names[i]);
                if (next == null)
                {
                    next = new GameObject(names[i]) { hideFlags = HideFlags.HideAndDontSave }.transform;
                    next.SetParent(at, false);
                }
                at = next;
            }
            _ikSampledGoals[goal] = at;
            return at;
        }

        static void HideSampler(Transform t)
        {
            t.gameObject.hideFlags = HideFlags.HideAndDontSave;
            for (int i = 0; i < t.childCount; i++) HideSampler(t.GetChild(i));
        }

        bool WantsIK()
        {
            var a = Animator;
            return a != null && a.avatar != null && a.avatar.isHuman && GetComponent<SaturaIK>() != null && SaturaIK.PassController != null;
        }

        void StopGraph()
        {
            if (_graph.IsValid())
            {
                GraphDestroying?.Invoke(this);
                _graph.Destroy();
            }
            ReleasePose();
            if (_ikSampler != null)
            {
                if (Application.isPlaying) Destroy(_ikSampler);
                else DestroyImmediate(_ikSampler);
            }
            _ikSampler = null;
            _ikSampled = null;
            for (int i = 0; i < 4; i++)
            {
                _ikSampledGoals[i] = null;
                _ikSampledFor[i] = null;
            }
            _clip = default;
            _blend = default;
            _from = default;
            _ikClip = default;
            _poseNode = default;
            _mixer = default;
            _ik = default;
            _output = default;
            _state = null;
            _paused = false;
            _done = false;
        }

        void ApplySpeed()
        {
            if (!_clip.IsValid()) return;
            float s = _state != null ? _state.speed : 1f;
            _clip.SetSpeed(_paused || _done ? 0.0 : s * speed);
        }

        void OnEnable()
        {
            if (Application.isPlaying && playOnEnable && !_clip.IsValid() && GetComponent<SaturaCutscene>() == null) PlayDefault();
        }

        void OnDisable()
        {
            StopGraph();
        }

        void OnDestroy()
        {
            StopGraph();
        }

        void Update()
        {
            SyncLayers();
            StepFade(_graph.IsValid() && _graph.GetTimeUpdateMode() == DirectorUpdateMode.UnscaledGameTime ? UnityEngine.Time.unscaledDeltaTime : UnityEngine.Time.deltaTime);
            if (!_clip.IsValid() || _state == null || _state.clip == null || _paused || _done)
            {
                SyncIkTime(0.0);
                return;
            }
            float len = _state.clip.length;
            if (len <= 0f) return;
            double t = _clip.GetTime();
            bool jumped = false;
            if (_state.loop)
            {
                if (!_state.clip.isLooping && t >= len)
                {
                    _clip.SetTime(t % len);
                    _clip.SetTime(t % len);
                    jumped = true;
                }
            }
            else if (t >= len)
            {
                _clip.SetTime(len);
                _clip.SetTime(len);
                _done = true;
                ApplySpeed();
                jumped = true;
                Finished?.Invoke(this);
            }
            SyncIkTime(jumped ? 0.0 : _graph.IsValid() && _graph.GetTimeUpdateMode() == DirectorUpdateMode.UnscaledGameTime ? UnityEngine.Time.unscaledDeltaTime : UnityEngine.Time.deltaTime);
        }
    }
}
