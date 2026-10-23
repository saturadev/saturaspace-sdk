using UnityEngine;
using UnityEngine.Events;

namespace SaturaSpace.Anim
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Satura/Cutscene/Event")]
    public sealed class CutsceneEvent : MonoBehaviour
    {
        [Tooltip("When the event fires, in seconds from the start of the cutscene.")]
        [SerializeField, Min(0f)] float time;
        [Tooltip("What happens at that moment, like playing a sound or particles.")]
        [SerializeField] UnityEvent action = new UnityEvent();

        public float Time
        {
            get => time;
            set => time = Mathf.Max(0f, value);
        }

        public UnityEvent Action => action;

        public void Fire()
        {
            action?.Invoke();
        }
    }
}
