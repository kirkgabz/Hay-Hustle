using System.Collections.Generic;
using UnityEngine;

namespace HayHustle
{
    /// <summary>Anything the farmer can use with the E key when standing close.</summary>
    public abstract class Interactable : MonoBehaviour
    {
        public static readonly List<Interactable> All = new List<Interactable>();

        public float Range = 3f;

        protected virtual void OnEnable() => All.Add(this);
        protected virtual void OnDisable() => All.Remove(this);

        public abstract string Prompt { get; }
        public virtual bool CanInteract => true;
        public virtual bool Selectable => true;
        public abstract void Interact(PlayerController player);

        public static Interactable Nearest(Vector3 pos)
        {
            Interactable best = null;
            float bestScore = float.MaxValue;
            foreach (var i in All)
            {
                if (!i.Selectable) continue;
                float dist = GameManager.FlatDistance(i.transform.position, pos);
                if (dist > i.Range) continue;
                float score = dist / i.Range;
                if (score < bestScore) { bestScore = score; best = i; }
            }
            return best;
        }
    }
}
