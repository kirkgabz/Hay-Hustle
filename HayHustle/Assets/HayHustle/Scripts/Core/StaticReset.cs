using UnityEngine;

namespace HayHustle
{
    /// <summary>
    /// This project has "Enter Play Mode Options" on (no domain reload), so static lists would keep
    /// destroyed objects from the previous Play session. Clear them every time Play starts.
    /// </summary>
    static class StaticReset
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Interactable.All.Clear();
            ProductPickup.All.Clear();
            Laborer.ClearClaims();
            Tutorial.Stop();
            Backend.ResetRuntime();
            LampPlacer.Cancel();
        }
    }
}
