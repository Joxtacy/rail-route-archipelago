using Game.Context;
using RailRouteArchipelago.Core;
using RailRouteArchipelago.Net;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RailRouteArchipelago.Interception
{
    /// <summary>
    /// Shift+F10 in split mode sends the goal on purpose, for an Endless-complete star the live watcher
    /// didn't see (earned offline in an earlier game run). Every check but "reached live" still applies.
    /// The goal can't be undone, hence the modifier; plain F10 does nothing.
    /// </summary>
    internal sealed class GoalSendKey : MonoBehaviour
    {
        public static void Install()
        {
            var go = new GameObject("RailRouteArchipelago.GoalSendKey");
            DontDestroyOnLoad(go);
            go.AddComponent<GoalSendKey>();
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.f10Key.wasPressedThisFrame
                || !(keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed) || !SplitFlags.Active)
            {
                return;
            }
            var map = GoalWatcher.CurrentMap;
            var session = ArchipelagoPump.Session;
            var connected = session != null && session.IsConnected;
            var decision = GoalState.OnManual(map, connected, connected && !session.ChecksBlocked,
                connected && session.GoalSupported, GoalWatcher.EndlessCompleteGranted(Ctx.Deps.ResearchController));
            if (decision.Send)
            {
                GoalWatcher.Send(session, map, " (manual)");
            }
            else
            {
                Log.Warn("Goal not sent manually: " + decision.Reason);
            }
        }
    }
}
