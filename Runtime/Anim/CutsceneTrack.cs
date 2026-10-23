using System.Collections.Generic;
using UnityEngine;

namespace SaturaSpace.Anim
{
    [DisallowMultipleComponent]
    public abstract class CutsceneTrack : MonoBehaviour
    {
        public abstract float End { get; }

        public abstract void Evaluate(SaturaCutscene cutscene, double time);

        public virtual void Release()
        {
        }

        protected List<T> Items<T>(List<T> into) where T : Component
        {
            into.Clear();
            for (int i = 0; i < transform.childCount; i++)
            {
                var c = transform.GetChild(i).GetComponent<T>();
                if (c != null && c.gameObject.activeSelf) into.Add(c);
            }
            return into;
        }
    }
}
