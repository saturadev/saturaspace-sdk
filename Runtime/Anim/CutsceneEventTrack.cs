using System.Collections.Generic;
using UnityEngine;

namespace SaturaSpace.Anim
{
    [AddComponentMenu("Satura/Cutscene/Event Track")]
    public sealed class CutsceneEventTrack : CutsceneTrack
    {
        readonly List<CutsceneEvent> _events = new List<CutsceneEvent>();

        public List<CutsceneEvent> Events()
        {
            var list = Items(new List<CutsceneEvent>());
            list.Sort((x, y) => x.Time.CompareTo(y.Time));
            return list;
        }

        public override float End
        {
            get
            {
                float end = 0f;
                foreach (var e in Items(_events))
                    if (e.Time > end) end = e.Time;
                return end;
            }
        }

        public override void Evaluate(SaturaCutscene cutscene, double time)
        {
        }

        public void Fire(double after, double upTo)
        {
            foreach (var e in Events())
                if (e.Time > after && e.Time <= upTo) e.Fire();
        }
    }
}
