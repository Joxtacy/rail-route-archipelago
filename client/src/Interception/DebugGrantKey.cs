using Game;
using Game.Context;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RailRouteArchipelago.Interception
{
    /// <summary>
    /// F9 in play mode (intercept mode only) grants the most recently intercepted upgrade, exercising the
    /// path received Archipelago items will take. The game uses the new Input System, so poll Keyboard.current.
    /// </summary>
    internal sealed class DebugGrantKey : MonoBehaviour
    {
        public static void Install()
        {
            var go = new GameObject("RailRouteArchipelago.DebugGrantKey");
            DontDestroyOnLoad(go);
            go.AddComponent<DebugGrantKey>();
        }

        private void Update()
        {
            if (!UpgradeInterception.InterceptEnabled)
            {
                return;
            }
            var keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.f9Key.wasPressedThisFrame)
            {
                return;
            }
            if (Ctx.Deps?.CurrentMode != GameMode.Play || Ctx.Deps.LevelController?.CurrentLevel == null)
            {
                return;
            }
            UpgradeInterception.GrantMostRecentPending();
        }
    }
}
