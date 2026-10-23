using System.Collections.Generic;
using UnityEngine;

namespace SaturaSpace.Anim
{
    [AddComponentMenu("Satura/Cutscene/Camera Track")]
    public sealed class CutsceneCameraTrack : CutsceneTrack
    {
        [Tooltip("The camera these shots move.")]
        [SerializeField] Camera camera;

        readonly List<CutsceneShot> _shots = new List<CutsceneShot>();
        readonly float[] _v = new float[8];

        public Camera Camera
        {
            get => camera;
            set => camera = value;
        }

        public List<CutsceneShot> Shots()
        {
            var list = Items(new List<CutsceneShot>());
            list.Sort((x, y) => x.Time.CompareTo(y.Time));
            return list;
        }

        public override float End
        {
            get
            {
                float end = 0f;
                foreach (var s in Items(_shots))
                    if (s.Time > end) end = s.Time;
                return end;
            }
        }

        public override void Evaluate(SaturaCutscene cutscene, double time)
        {
            if (camera == null) return;
            var list = Items(_shots);
            if (list.Count == 0) return;
            list.Sort((x, y) => x.Time.CompareTo(y.Time));
            Pose(list, time, out var pos, out var rot, out var fov);
            camera.transform.SetPositionAndRotation(pos, rot);
            if (!camera.orthographic && Mathf.Abs(camera.fieldOfView - fov) > 1e-4f) camera.fieldOfView = fov;
        }

        public void Pose(List<CutsceneShot> list, double time, out Vector3 pos, out Quaternion rot, out float fov)
        {
            int i = 0;
            while (i + 1 < list.Count && list[i + 1].Time <= time + 1e-4) i++;
            var a = list[i];
            if (time <= a.Time + 1e-4 || i + 1 >= list.Count || a.ToNext == CutsceneShot.Next.Cut)
            {
                pos = a.transform.position;
                rot = a.transform.rotation;
                fov = a.FieldOfView;
                return;
            }
            var b = list[i + 1];
            float t0 = a.Time, t1 = b.Time, dt = Mathf.Max(1e-4f, t1 - t0);
            float s = Mathf.Clamp01((float)((time - t0) / dt));
            var pa = Values(a, a.transform.rotation);
            var pb = Values(b, a.transform.rotation);
            float[] pl = i > 0 ? Values(list[i - 1], a.transform.rotation) : null;
            float[] pr = i + 2 < list.Count ? Values(list[i + 2], a.transform.rotation) : null;
            float tl = pl != null ? list[i - 1].Time : 0f;
            float tr = pr != null ? list[i + 2].Time : 0f;
            float h00 = 2 * s * s * s - 3 * s * s + 1, h10 = s * s * s - 2 * s * s + s, h01 = -2 * s * s * s + 3 * s * s, h11 = s * s * s - s * s;
            for (int c = 0; c < 8; c++)
            {
                float m0 = pl != null ? Slope(pl[c], pa[c], pb[c], tl, t0, t1) : 0f;
                float m1 = pr != null ? Slope(pa[c], pb[c], pr[c], t0, t1, tr) : 0f;
                _v[c] = h00 * pa[c] + h10 * dt * m0 + h01 * pb[c] + h11 * dt * m1;
            }
            pos = new Vector3(_v[0], _v[1], _v[2]);
            var q = new Quaternion(_v[3], _v[4], _v[5], _v[6]);
            float len = Mathf.Sqrt(q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w);
            rot = len > 1e-6f ? new Quaternion(q.x / len, q.y / len, q.z / len, q.w / len) : a.transform.rotation;
            fov = _v[7];
        }

        static float Slope(float p0, float p1, float p2, float t0, float t1, float t2)
        {
            float m1 = (p1 - p0) / Mathf.Max(1e-4f, t1 - t0), m2 = (p2 - p1) / Mathf.Max(1e-4f, t2 - t1);
            if (m1 * m2 <= 0f) return 0f;
            float m = (p2 - p0) / Mathf.Max(1e-4f, t2 - t0);
            float cap = 3f * Mathf.Min(Mathf.Abs(m1), Mathf.Abs(m2));
            return Mathf.Clamp(m, -cap, cap);
        }

        static float[] Values(CutsceneShot s, Quaternion like)
        {
            var p = s.transform.position;
            var q = s.transform.rotation;
            if (Quaternion.Dot(q, like) < 0f) q = new Quaternion(-q.x, -q.y, -q.z, -q.w);
            return new[] { p.x, p.y, p.z, q.x, q.y, q.z, q.w, s.FieldOfView };
        }
    }
}
