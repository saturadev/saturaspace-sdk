using UnityEngine;

namespace SaturaSpace.Anim
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Satura/Cutscene/Shot")]
    public sealed class CutsceneShot : MonoBehaviour
    {
        public enum Next
        {
            Cut = 0,
            Move = 1
        }

        [Tooltip("When the camera reaches this shot, in seconds from the start of the cutscene.")]
        [SerializeField, Min(0f)] float time;
        [Tooltip("The camera's field of view in this shot. The shot's position and rotation are this object's Transform.")]
        [SerializeField, Range(1f, 179f)] float fieldOfView = 50f;
        [Tooltip("What happens after this shot. Cut holds it and jumps to the next one. Move glides the camera to the next shot.")]
        [SerializeField] Next toNext = Next.Cut;

        public float Time
        {
            get => time;
            set => time = Mathf.Max(0f, value);
        }

        public float FieldOfView
        {
            get => fieldOfView;
            set => fieldOfView = Mathf.Clamp(value, 1f, 179f);
        }

        public Next ToNext
        {
            get => toNext;
            set => toNext = value;
        }

        void OnDrawGizmos()
        {
            Draw(0.35f);
        }

        void OnDrawGizmosSelected()
        {
            Draw(1f);
        }

        void Draw(float alpha)
        {
            var m = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(0.99f, 0.88f, 0.28f, alpha);
            Gizmos.DrawFrustum(Vector3.zero, fieldOfView, 1.2f, 0.05f, 16f / 9f);
            Gizmos.matrix = m;
        }
    }
}
