using Game.Context;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RailRouteArchipelago.Interception
{
    /// <summary>
    /// F9 in split mode receives the item of the upgrade selected in the upgrade panel, as though
    /// Archipelago had sent it. Needs no network and no bought slot. The game uses the new Input System,
    /// so poll Keyboard.current.
    /// </summary>
    internal sealed class DebugReceiveKey : MonoBehaviour
    {
        public static void Install()
        {
            var go = new GameObject("RailRouteArchipelago.DebugReceiveKey");
            DontDestroyOnLoad(go);
            go.AddComponent<DebugReceiveKey>();
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.f9Key.wasPressedThisFrame || !SplitFlags.Active)
            {
                return;
            }
            var menu = Ctx.Deps.MenuController;
            var panel = menu == null || menu.SystemUpgradesMenu == null ? null : menu.SystemUpgradesMenu.SystemUpgradeContextPanelView;
            var selected = panel == null || !panel.gameObject.activeInHierarchy ? null : panel.SystemUpgradeContextPanelModel?.ResearchItem;
            if (selected == null)
            {
                Log.Info("Debug receive: no upgrade selected");
                return;
            }
            UpgradeReceiver.Receive(selected);
        }
    }
}
