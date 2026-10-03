using AllyClarity.Hooks;
using BepInEx;
using BepInEx.Logging;

namespace AllyClarity
{
    /// <summary>
    /// Makes allies easy to tell from enemies: allied summons get an outline (<see cref="OutlineHooks"/>) and pinging
    /// an ally marks it as an ally instead of an enemy (<see cref="PingHooks"/>), in one color for the player and their
    /// own summons and one for their teammates (<see cref="AllyUtil"/>), and an ally's Genesis Loop is shown in the
    /// player's color instead of the Wandering Vagrant's blue (<see cref="GenesisLoopHooks"/>). Client-side:
    /// everything is local visuals and UI, so there are no network messages and the mod stays off the game's network
    /// mod list; every player can use it or not.
    /// </summary>
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(RiskOfOptionsCompat.Guid, BepInDependency.DependencyFlags.SoftDependency)]
    public sealed class AllyClarityPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = "revor.AllyClarity";
        public const string PluginName = "AllyClarity";
        public const string PluginVersion = "1.0.0"; // tools/package.py checks this against the manifest

        internal static ManualLogSource Log { get; private set; }

        private void Awake()
        {
            Log = Logger;
            PluginConfig.Init(Config);
            OutlineHooks.Init(this);
            PingHooks.Init();
            GenesisLoopHooks.Init();
            if (RiskOfOptionsCompat.IsInstalled)
            {
                RiskOfOptionsCompat.Init();
            }
        }
    }
}
