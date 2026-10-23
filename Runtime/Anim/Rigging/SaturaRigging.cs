using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.Playables;

namespace SaturaSpace.Anim
{
    public static class SaturaRigging
    {
        sealed class Built
        {
            public RigBuilder Builder;
            public PlayableGraph Graph;
            public readonly List<PlayableOutput> Outputs = new List<PlayableOutput>();
        }

        static readonly Dictionary<SaturaAnimator, Built> _built = new Dictionary<SaturaAnimator, Built>();

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#endif
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Init()
        {
            _built.Clear();
            SaturaAnimator.GraphCreated -= OnGraph;
            SaturaAnimator.GraphCreated += OnGraph;
            SaturaAnimator.BeforeAnimate -= OnSync;
            SaturaAnimator.BeforeAnimate += OnSync;
            SaturaAnimator.GraphDestroying -= OnStop;
            SaturaAnimator.GraphDestroying += OnStop;
            RigBuilder.onAddRigBuilder -= OnRigBuilder;
            RigBuilder.onAddRigBuilder += OnRigBuilder;
        }

        static bool Wants(SaturaAnimator a, out RigBuilder rb)
        {
            rb = a != null ? a.GetComponent<RigBuilder>() : null;
            return Application.isPlaying && rb != null && rb.isActiveAndEnabled && rb.layers.Count > 0 && a.Graph.IsValid();
        }

        static void OnGraph(SaturaAnimator a)
        {
            Drop(a, false);
            if (!Wants(a, out var rb)) return;
            var g = a.Graph;
            int before = g.GetOutputCount();
            rb.Clear();
            if (!rb.Build(g)) return;
            var b = new Built { Builder = rb, Graph = g };
            for (int i = before; i < g.GetOutputCount(); i++) b.Outputs.Add(g.GetOutput(i));
            _built[a] = b;
        }

        static void OnSync(SaturaAnimator a)
        {
            if (!_built.TryGetValue(a, out var b))
            {
                if (Wants(a, out _)) OnGraph(a);
                if (!_built.TryGetValue(a, out b)) return;
            }
            if (b.Builder == null || !b.Graph.IsValid() || b.Builder.graph.IsValid() || !b.Builder.isActiveAndEnabled)
            {
                OnGraph(a);
                if (!_built.TryGetValue(a, out b)) return;
            }
            b.Builder.SyncLayers();
        }

        static void OnStop(SaturaAnimator a)
        {
            Drop(a, true);
        }

        static void OnRigBuilder(RigBuilder rb)
        {
            if (!Application.isPlaying || rb == null) return;
            var a = rb.GetComponent<SaturaAnimator>();
            if (a != null && a.isActiveAndEnabled && a.Graph.IsValid()) OnGraph(a);
        }

        static void Drop(SaturaAnimator a, bool giveBack)
        {
            if (a == null || !_built.TryGetValue(a, out var b)) return;
            _built.Remove(a);
            if (b.Graph.IsValid())
                foreach (var o in b.Outputs)
                {
                    if (!o.IsOutputValid()) continue;
                    var src = o.GetSourcePlayable();
                    b.Graph.DestroyOutput(o);
                    if (src.IsValid()) b.Graph.DestroySubgraph(src);
                }
            if (b.Builder == null) return;
            if (!b.Builder.graph.IsValid()) b.Builder.Clear();
            if (giveBack && Application.isPlaying && b.Builder.isActiveAndEnabled && !b.Builder.graph.IsValid()) b.Builder.Build();
        }
    }
}
