using UnityEngine;

namespace SaturaSpace.Anim
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Satura/Cutscene/Animation Clip")]
    public sealed class CutsceneAnimationClip : MonoBehaviour
    {
        [Tooltip("The animation this clip plays on the track's actor.")]
        [SerializeField] AnimationClip clip;
        [Tooltip("When the clip starts, in seconds from the start of the cutscene.")]
        [SerializeField, Min(0f)] float start;
        [Tooltip("How long the clip plays, in seconds. Longer than the animation loops it (or holds its last frame if it doesn't loop).")]
        [SerializeField, Min(0.0167f)] float duration = 1f;
        [Tooltip("Where in the animation the clip starts, in seconds.")]
        [SerializeField, Min(0f)] float clipIn;
        [Tooltip("How fast the animation plays. 1 is normal speed.")]
        [SerializeField] float speed = 1f;

        public AnimationClip Clip
        {
            get => clip;
            set => clip = value;
        }

        public float StartTime
        {
            get => start;
            set => start = Mathf.Max(0f, value);
        }

        public float Duration
        {
            get => duration;
            set => duration = Mathf.Max(1f / 60f, value);
        }

        public float ClipIn
        {
            get => clipIn;
            set => clipIn = Mathf.Max(0f, value);
        }

        public float Speed
        {
            get => speed;
            set => speed = value;
        }

        public float End => start + duration;

        public double LocalTime(double time)
        {
            double t = time < start ? start : time > End ? End : time;
            return clipIn + (t - start) * speed;
        }

        void OnValidate()
        {
            start = Mathf.Max(0f, start);
            duration = Mathf.Max(1f / 60f, duration);
            clipIn = Mathf.Max(0f, clipIn);
            if (Mathf.Abs(speed) < 0.01f) speed = speed < 0f ? -0.01f : 0.01f;
        }
    }
}
