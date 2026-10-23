using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace SaturaSpace.Anim
{
    [Serializable]
    public struct SaturaIKGoal
    {
        [Tooltip("The object the hand or foot reaches for.")]
        public Transform target;
        [Tooltip("How far the hand or foot moves toward the goal position. 0.5 is halfway, 1 is all the way. At 0 the animation plays unchanged.")]
        [Range(0f, 1f)] public float positionWeight;
        [Tooltip("How far the hand or foot turns toward the goal rotation. 0.5 is halfway, 1 is all the way. At 0 the animation plays unchanged.")]
        [Range(0f, 1f)] public float rotationWeight;

        public bool Active => positionWeight > 0f || rotationWeight > 0f;
    }

    public struct SaturaIKPoseJob : IAnimationJob
    {
        public NativeArray<Vector3> position;
        public NativeArray<Quaternion> rotation;
        public NativeArray<int> count;

        public void ProcessRootMotion(AnimationStream stream) { }

        public void ProcessAnimation(AnimationStream stream)
        {
            if (!stream.isHumanStream) return;
            var h = stream.AsHuman();
            for (int i = 0; i < 4; i++)
            {
                var g = i == 0 ? AvatarIKGoal.LeftHand : i == 1 ? AvatarIKGoal.RightHand : i == 2 ? AvatarIKGoal.LeftFoot : AvatarIKGoal.RightFoot;
                position[i] = h.GetGoalPositionFromPose(g);
                rotation[i] = h.GetGoalRotationFromPose(g);
            }
            count[0] = count[0] + 1;
        }
    }

    public sealed class SaturaIKPose : IDisposable
    {
        NativeArray<Vector3> _position;
        NativeArray<Quaternion> _rotation;
        NativeArray<int> _count;

        public bool IsCreated => _count.IsCreated;

        public AnimationScriptPlayable CreatePlayable(PlayableGraph graph)
        {
            if (!_count.IsCreated)
            {
                _position = new NativeArray<Vector3>(4, Allocator.Persistent);
                _rotation = new NativeArray<Quaternion>(4, Allocator.Persistent);
                _count = new NativeArray<int>(1, Allocator.Persistent);
            }
            return AnimationScriptPlayable.Create(graph, new SaturaIKPoseJob { position = _position, rotation = _rotation, count = _count }, 1);
        }

        public bool TryGet(int goal, out Vector3 position, out Quaternion rotation)
        {
            position = Vector3.zero;
            rotation = Quaternion.identity;
            if (!_count.IsCreated || goal < 0 || goal > 3) return false;
            try
            {
                if (_count[0] == 0) return false;
                position = _position[goal];
                rotation = _rotation[goal];
                return true;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }

        public void Dispose()
        {
            if (_position.IsCreated) _position.Dispose();
            if (_rotation.IsCreated) _rotation.Dispose();
            if (_count.IsCreated) _count.Dispose();
        }
    }

    [ExecuteAlways]
    [RequireComponent(typeof(Animator))]
    [DisallowMultipleComponent]
    [AddComponentMenu("Satura/Satura IK")]
    public sealed class SaturaIK : MonoBehaviour
    {
        public const string PassControllerName = "SaturaIKPass";

        public static readonly AvatarIKGoal[] Goals = { AvatarIKGoal.LeftHand, AvatarIKGoal.RightHand, AvatarIKGoal.LeftFoot, AvatarIKGoal.RightFoot };
        public static readonly string[] FieldNames = { "leftHand", "rightHand", "leftFoot", "rightFoot" };
        public const string GoalsName = "IK goals";
        public static readonly string[] GoalNames = { "Left hand goal", "Right hand goal", "Left foot goal", "Right foot goal" };
        static readonly HumanBodyBones[] GoalBones = { HumanBodyBones.LeftHand, HumanBodyBones.RightHand, HumanBodyBones.LeftFoot, HumanBodyBones.RightFoot };

        public static string GoalPath(int goal) => GoalsName + "/" + GoalNames[goal];

        static readonly float[] PositionResponse = { 0f, 0.1476f, 0.2798f, 0.3936f, 0.4879f, 0.5704f, 0.6394f, 0.6967f, 0.7452f, 0.7846f, 0.8178f, 0.8456f, 0.8690f, 0.8895f, 0.9077f, 0.9244f, 0.9402f, 0.9553f, 0.9702f, 0.9851f, 1f };

        public SaturaIKGoal leftHand;
        public SaturaIKGoal rightHand;
        public SaturaIKGoal leftFoot;
        public SaturaIKGoal rightFoot;

        static RuntimeAnimatorController _pass;
        static AvatarMask _layerMask;

        public static AvatarMask LayerMask
        {
            get
            {
                if (_layerMask != null) return _layerMask;
                _layerMask = new AvatarMask { name = "Satura IK layer", hideFlags = HideFlags.HideAndDontSave };
                for (int i = 0; i < (int)AvatarMaskBodyPart.LastBodyPart; i++) _layerMask.SetHumanoidBodyPartActive((AvatarMaskBodyPart)i, false);
                _layerMask.transformCount = 0;
                return _layerMask;
            }
        }
        Animator _animator;

        public static RuntimeAnimatorController PassController
        {
            get
            {
                if (_pass == null) _pass = Resources.Load<RuntimeAnimatorController>(PassControllerName);
                return _pass;
            }
        }

        public Animator Animator
        {
            get
            {
                if (_animator == null) _animator = GetComponent<Animator>();
                return _animator;
            }
        }

        public int Passes { get; private set; }

        public int ExactPasses { get; private set; }

        public SaturaIKPose Pose { get; set; }

        readonly Vector3[] _fk = new Vector3[4];

        public Vector3 AnimatedPosition(int goal) => _fk[goal];

        public static float MecanimPositionWeight(float fraction)
        {
            float f = Mathf.Clamp01(fraction);
            if (f <= 0f) return 0f;
            if (f >= 1f) return 1f;
            int n = PositionResponse.Length - 1;
            for (int i = 1; i <= n; i++)
            {
                if (PositionResponse[i] < f) continue;
                float a = PositionResponse[i - 1];
                float b = PositionResponse[i];
                float s = b > a ? (f - a) / (b - a) : 0f;
                return (i - 1 + s) / n;
            }
            return 1f;
        }

        public bool AnyActive => leftHand.Active || rightHand.Active || leftFoot.Active || rightFoot.Active;

        public SaturaIKGoal this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0: return leftHand;
                    case 1: return rightHand;
                    case 2: return leftFoot;
                    case 3: return rightFoot;
                    default: throw new ArgumentOutOfRangeException(nameof(index));
                }
            }
            set
            {
                switch (index)
                {
                    case 0: leftHand = value; break;
                    case 1: rightHand = value; break;
                    case 2: leftFoot = value; break;
                    case 3: rightFoot = value; break;
                    default: throw new ArgumentOutOfRangeException(nameof(index));
                }
            }
        }

        public static Vector3 ToWorld(Vector3 rootPosition, Quaternion rootRotation, Vector3 scale, Vector3 local) => rootPosition + rootRotation * Vector3.Scale(scale, local);

        public static Quaternion Unit(Quaternion q)
        {
            float m = Mathf.Sqrt(q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w);
            return m > 1e-6f ? new Quaternion(q.x / m, q.y / m, q.z / m, q.w / m) : Quaternion.identity;
        }

        public bool GoalPose(int goal, out Vector3 position, out Quaternion rotation)
        {
            position = Vector3.zero;
            rotation = Quaternion.identity;
            var t = this[goal].target;
            if (t == null) return false;
            var root = transform;
            var p = t.localPosition;
            var q = t.localRotation;
            var n = t.parent;
            while (n != null && n != root)
            {
                p = n.localPosition + n.localRotation * Vector3.Scale(n.localScale, p);
                q = n.localRotation * q;
                n = n.parent;
            }
            if (n == null)
            {
                p = root.InverseTransformPoint(t.position);
                q = Quaternion.Inverse(root.rotation) * t.rotation;
            }
            position = p;
            rotation = Unit(q);
            return true;
        }

        public bool EnsureGoals(List<GameObject> created = null)
        {
            bool changed = false;
            var box = transform.Find(GoalsName);
            for (int i = 0; i < 4; i++)
            {
                var g = this[i];
                if (g.target != null) continue;
                if (box == null)
                {
                    box = new GameObject(GoalsName).transform;
                    box.SetParent(transform, false);
                    created?.Add(box.gameObject);
                }
                var t = box.Find(GoalNames[i]);
                if (t == null)
                {
                    t = new GameObject(GoalNames[i]).transform;
                    t.SetParent(box, false);
                    var an = Animator;
                    var bone = an != null && an.isHuman ? an.GetBoneTransform(GoalBones[i]) : null;
                    if (bone != null) t.position = bone.position;
                    created?.Add(t.gameObject);
                }
                g.target = t;
                this[i] = g;
                changed = true;
            }
            return changed;
        }

        void Reset()
        {
            EnsureGoals();
        }

        void OnAnimatorIK(int layerIndex)
        {
            Apply(Animator);
        }

        public void Apply(Animator animator)
        {
            if (animator == null) return;
            Passes++;
            var rp = animator.rootPosition;
            var rr = animator.rootRotation;
            var s = transform.lossyScale;
            var pose = Pose;
            bool exact = false;
            Vector3 fp = Vector3.zero;
            Quaternion fq = Quaternion.identity;
            for (int i = 0; i < 4; i++)
            {
                var g = Goals[i];
                var v = this[i];
                bool has = GoalPose(i, out var lp, out var lq);
                float pw = has ? Mathf.Clamp01(v.positionWeight) : 0f;
                float rw = has ? Mathf.Clamp01(v.rotationWeight) : 0f;
                var goalP = ToWorld(rp, rr, s, lp);
                var goalQ = rr * lq;
                bool fresh = pose != null && pose.TryGet(i, out fp, out fq);
                if (fresh) _fk[i] = Vector3.Scale(new Vector3(1f / s.x, 1f / s.y, 1f / s.z), Quaternion.Inverse(rr) * (fp - rp));
                if ((pw > 0f || rw > 0f) && fresh)
                {
                    exact = true;
                    animator.SetIKPositionWeight(g, pw > 0f ? 1f : 0f);
                    animator.SetIKRotationWeight(g, rw > 0f ? 1f : 0f);
                    if (pw > 0f) animator.SetIKPosition(g, Vector3.LerpUnclamped(fp, goalP, pw));
                    if (rw > 0f) animator.SetIKRotation(g, Quaternion.SlerpUnclamped(fq, goalQ, rw));
                    continue;
                }
                animator.SetIKPositionWeight(g, MecanimPositionWeight(pw));
                animator.SetIKRotationWeight(g, rw);
                if (pw > 0f) animator.SetIKPosition(g, goalP);
                if (rw > 0f) animator.SetIKRotation(g, goalQ);
            }
            if (exact) ExactPasses++;
        }
    }
}
