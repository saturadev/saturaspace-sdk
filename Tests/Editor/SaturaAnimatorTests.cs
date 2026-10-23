using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace SaturaSpace.Anim.Tests
{
    public class SaturaAnimatorTests
    {
        GameObject _go;
        SaturaAnimatorController _ctrl;
        AnimationClip _rise;
        AnimationClip _spin;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("SaturaAnimatorTestCube");
            _ctrl = ScriptableObject.CreateInstance<SaturaAnimatorController>();
            _rise = new AnimationClip { name = "Rise" };
            AnimationUtility.SetEditorCurve(_rise, EditorCurveBinding.FloatCurve("", typeof(Transform), "m_LocalPosition.y"), AnimationCurve.Linear(0f, 0f, 1f, 2f));
            _spin = new AnimationClip { name = "Spin" };
            AnimationUtility.SetEditorCurve(_spin, EditorCurveBinding.FloatCurve("", typeof(Transform), "m_LocalScale.x"), AnimationCurve.Linear(0f, 1f, 2f, 3f));
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
            Object.DestroyImmediate(_ctrl);
            Object.DestroyImmediate(_rise);
            Object.DestroyImmediate(_spin);
        }

        [Test]
        public void SdkBehavioursHaveNoUnityMessageNamesWithParameters()
        {
            var messages = new[] { "Awake", "Start", "Update", "LateUpdate", "FixedUpdate", "OnEnable", "OnDisable", "OnDestroy" };
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.DeclaredOnly;
            foreach (var type in typeof(SaturaAnimator).Assembly.GetTypes())
            {
                if (!typeof(MonoBehaviour).IsAssignableFrom(type)) continue;
                foreach (var m in type.GetMethods(flags))
                    if (System.Array.IndexOf(messages, m.Name) >= 0)
                        Assert.AreEqual(0, m.GetParameters().Length, type.Name + "." + m.Name + " takes parameters, so Unity logs a script error for it");
            }
        }

        [Test]
        public void AddStateKeepsNamesUniqueAndSetsTheDefault()
        {
            var a = _ctrl.AddState("Bob", _rise);
            var b = _ctrl.AddState("Bob", _spin);
            Assert.AreEqual("Bob", a.name);
            Assert.AreEqual("Bob 1", b.name);
            Assert.AreEqual("Bob", _ctrl.DefaultState);
            Assert.AreSame(b, _ctrl.FindStateByClip(_spin));
            Assert.AreEqual(2, _ctrl.Clips.Count);
        }

        [Test]
        public void RemoveStateMovesTheDefaultToTheNextState()
        {
            _ctrl.AddState("Bob", _rise);
            _ctrl.AddState("Pulse", _spin);
            Assert.IsTrue(_ctrl.RemoveState("Bob"));
            Assert.AreEqual("Pulse", _ctrl.DefaultState);
            Assert.IsNull(_ctrl.FindState("Bob"));
        }

        [Test]
        public void ReplaceClipSwapsEveryStateThatUsesIt()
        {
            _ctrl.AddState("A", _rise);
            _ctrl.AddState("B", _rise);
            int v = _ctrl.Version;
            Assert.AreEqual(2, _ctrl.ReplaceClip(_rise, _spin));
            Assert.AreSame(_spin, _ctrl.FindState("B").clip);
            Assert.Greater(_ctrl.Version, v);
        }

        [Test]
        public void PlayPosesTheObjectThroughThePlayableGraph()
        {
            _ctrl.AddState("Rise", _rise, 1f, false);
            var sa = _go.AddComponent<SaturaAnimator>();
            sa.Controller = _ctrl;
            Assert.IsTrue(sa.Play("Rise", 0.5f));
            sa.Evaluate();
            Assert.AreEqual(1f, _go.transform.localPosition.y, 1e-3f);
            Assert.AreEqual("Rise", sa.CurrentStateName);
            Assert.AreEqual(0.5, sa.Time, 1e-4);
            sa.Time = 0.25;
            sa.Evaluate();
            Assert.AreEqual(0.5f, _go.transform.localPosition.y, 1e-3f);
            sa.Pause();
            Assert.IsTrue(sa.IsPaused);
            Assert.IsFalse(sa.IsPlaying);
            sa.Stop();
            Assert.IsNull(sa.CurrentStateName);
        }

        [Test]
        public void PlayUnknownStateFails()
        {
            var sa = _go.AddComponent<SaturaAnimator>();
            sa.Controller = _ctrl;
            Assert.IsFalse(sa.Play("Nope"));
            Assert.IsFalse(sa.PlayDefault());
        }
    }
}
