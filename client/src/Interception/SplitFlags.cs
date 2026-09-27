using Game;
using Game.Context;
using Game.Level;
using RailRouteArchipelago.Patching;

namespace RailRouteArchipelago.Interception
{
    /// <summary>
    /// Split mode: a slot's Researched flag means "slot bought" and upgrade effects come from
    /// <see cref="EffectState"/>. Only in intercept mode, while playing an Endless (Economy) level;
    /// everywhere else the game behaves unmodded. Evaluated on every query, so leaving the level
    /// restores the game's own semantics immediately.
    /// </summary>
    internal static class SplitFlags
    {
        public static bool Active
        {
            get
            {
                if (!UpgradeInterception.InterceptEnabled || !PatchManager.Applied)
                {
                    return false;
                }
                var deps = Ctx.Deps;
                if (deps == null || deps.CurrentMode != GameMode.Play)
                {
                    return false;
                }
                var definition = deps.LevelController?.CurrentLevel?.LevelDefinition;
                return definition != null && definition.ScoringModel == ScoringModel.Economy;
            }
        }
    }
}
