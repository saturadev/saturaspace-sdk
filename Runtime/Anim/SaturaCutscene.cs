using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace SaturaSpace.Anim
{
    [RequireComponent(typeof(SaturaAnimator))]
    [DisallowMultipleComponent]
    [AddComponentMenu("Satura/Satura Cutscene")]
    public sealed class SaturaCutscene : MonoBehaviour
    {
        public enum EndMode
        {
            Hold = 0,
            Release = 1
        }

        [Serializable]
        public sealed class SignalHook
        {
            public string name = "";
            public UnityEvent action = new UnityEvent();
        }

        [SerializeField] string state = "";
        [SerializeField] bool playOnStart;
        [Tooltip("How long the cutscene lasts, in seconds. 0 ends it with its last clip, shot, event or key.")]
        [SerializeField, Min(0f)] float length;
        [Tooltip("What happens on the last frame. Hold leaves everything where the cutscene put it, like Timeline's Hold. Release gives the characters and objects back to their own animation, so they jump back to it.")]
        [FormerlySerializedAs("releaseAtEnd")]
        [SerializeField] EndMode atEnd = EndMode.Hold;
        [SerializeField] AudioSource audioSource;
        [SerializeField] SignalHook[] signals = new SignalHook[0];
        [SerializeField] UnityEvent onFinished = new UnityEvent();

        SaturaAnimator _animator;
        readonly List<CutsceneTrack> _tracks = new List<CutsceneTrack>();
        Animator[] _cast = new Animator[0];
        AnimatorCullingMode[] _culling = new AnimatorCullingMode[0];
        bool _playing;
        double _elapsed;

        public event Action<SaturaCutscene> Finished;
        public event Action<string> Signaled;

        public SaturaAnimator Animator
        {
            get
            {
                if (_animator == null) _animator = GetComponent<SaturaAnimator>();
                return _animator;
            }
        }

        public string State
        {
            get => state;
            set => state = value ?? "";
        }

        public EndMode AtEnd
        {
            get => atEnd;
            set => atEnd = value;
        }

        public bool IsPlaying => _playing;

        public float Length
        {
            get
            {
                if (length > 0f) return length;
                var clip = DirectorClip;
                float len = clip != null ? clip.length : 0f;
                foreach (var t in Tracks())
                    if (t.End > len) len = t.End;
                return len;
            }
        }

        public AnimationClip DirectorClip
        {
            get
            {
                var a = Animator;
                if (a == null || a.Controller == null) return null;
                var st = string.IsNullOrEmpty(state) ? a.Controller.DefaultOrFirst : a.Controller.FindState(state);
                return st != null ? st.clip : null;
            }
        }

        public List<CutsceneTrack> Tracks()
        {
            GetComponentsInChildren(true, _tracks);
            return _tracks;
        }

        public void Evaluate(double time)
        {
            foreach (var t in Tracks().ToArray())
                if (t != null && t.isActiveAndEnabled) t.Evaluate(this, time);
        }

        public void ReleaseTracks()
        {
            foreach (var t in Tracks().ToArray())
                if (t != null) t.Release();
        }

        public double Time => _playing ? Math.Min(_elapsed, Length) : 0;

        void Start()
        {
            if (playOnStart) Play();
        }

        public bool Play()
        {
            var a = Animator;
            a.ManualTick = true;
            RestoreCulling();
            var cast = new List<Animator>();
            foreach (var t in Tracks())
                if (t is CutsceneAnimationTrack at && at.Actor != null && !cast.Contains(at.Actor)) cast.Add(at.Actor);
            _cast = cast.ToArray();
            _culling = new AnimatorCullingMode[_cast.Length];
            for (int i = 0; i < _cast.Length; i++)
            {
                _culling[i] = _cast[i].cullingMode;
                _cast[i].cullingMode = AnimatorCullingMode.AlwaysAnimate;
            }
            bool ok = a.Controller == null || (string.IsNullOrEmpty(state) ? a.PlayDefault() : a.Play(state));
            if (!ok && Length <= 0f) a.ManualTick = false;
            _playing = ok || Length > 0f;
            _elapsed = 0;
            if (_playing)
            {
                Drive(0f);
                FireEvents(-1, 0);
            }
            return _playing;
        }

        public IEnumerator PlayAndWait()
        {
            if (!Play()) yield break;
            while (_playing) yield return null;
        }

        public void Stop()
        {
            if (!_playing) return;
            _playing = false;
            Release();
        }

        public void Skip()
        {
            if (!_playing) return;
            End();
        }

        public void Signal(string signal)
        {
            Signaled?.Invoke(signal);
            if (signals == null) return;
            foreach (var s in signals)
                if (s != null && s.name == signal) s.action?.Invoke();
        }

        public void PlaySound(AudioClip clip)
        {
            if (clip == null) return;
            if (audioSource != null)
            {
                audioSource.PlayOneShot(clip);
                return;
            }
            var cam = Camera.main;
            AudioSource.PlayClipAtPoint(clip, cam != null ? cam.transform.position : transform.position);
        }

        public void PlayParticles(string path)
        {
            var t = Find(path);
            if (t == null) return;
            if (!t.gameObject.activeSelf) t.gameObject.SetActive(true);
            var ps = t.GetComponentInChildren<ParticleSystem>(true);
            if (ps != null) ps.Play(true);
        }

        public void Show(string path)
        {
            var t = Find(path);
            if (t != null) t.gameObject.SetActive(true);
        }

        public void Hide(string path)
        {
            var t = Find(path);
            if (t != null) t.gameObject.SetActive(false);
        }

        Transform Find(string path)
        {
            var clean = (path ?? "").Trim().Trim('/');
            if (clean.Length == 0) return null;
            var t = transform.Find(clean);
            if (t != null) return t;
            int slash = clean.IndexOf('/');
            var head = slash < 0 ? clean : clean.Substring(0, slash);
            foreach (var root in gameObject.scene.GetRootGameObjects())
            {
                if (root.name != head) continue;
                var hit = slash < 0 ? root.transform : root.transform.Find(clean.Substring(slash + 1));
                if (hit != null) return hit;
            }
            Debug.LogWarning("SaturaCutscene " + name + ": no object '" + clean + "' under the cutscene or in its scene", this);
            return null;
        }

        void Update()
        {
            if (!_playing) return;
            var a = Animator;
            float dt = UnityEngine.Time.deltaTime;
            double before = _elapsed;
            _elapsed += dt * a.Speed;
            Drive(dt);
            FireEvents(before, _elapsed);
            if (_elapsed >= Length) End();
        }

        void Drive(float dt)
        {
            var a = Animator;
            a.Tick(dt);
            Evaluate(Math.Min(_elapsed, Length));
        }

        void FireEvents(double after, double upTo)
        {
            foreach (var t in Tracks().ToArray())
                if (t is CutsceneEventTrack et && et.isActiveAndEnabled) et.Fire(after, upTo);
        }

        void End()
        {
            _playing = false;
            var a = Animator;
            if (a.CurrentState != null && !a.CurrentState.loop)
            {
                a.Time = a.Length;
                a.Evaluate();
            }
            Evaluate(Length);
            if (atEnd == EndMode.Release) Release();
            Finished?.Invoke(this);
            onFinished?.Invoke();
        }

        void RestoreCulling()
        {
            for (int i = 0; i < _cast.Length && i < _culling.Length; i++)
                if (_cast[i] != null) _cast[i].cullingMode = _culling[i];
            _cast = new Animator[0];
            _culling = new AnimatorCullingMode[0];
        }

        void Release()
        {
            RestoreCulling();
            var a = Animator;
            a.Stop();
            a.ManualTick = false;
            ReleaseTracks();
        }

        void OnDisable()
        {
            _playing = false;
            RestoreCulling();
            ReleaseTracks();
        }
    }
}
