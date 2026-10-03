using System.Runtime.CompilerServices;
using BepInEx.Bootstrap;
using RiskOfOptions;
using RiskOfOptions.Options;

namespace AllyClarity
{
    /// <summary>
    /// In-game settings through Risk of Options, a soft dependency. Its types appear only inside the body of the
    /// non-inlined <see cref="Init"/>, so they are only loaded when it is installed.
    /// </summary>
    internal static class RiskOfOptionsCompat
    {
        public const string Guid = "com.rune580.riskofoptions";

        // The store icon, embedded from thunderstore/AllyClarity/icon.png by Directory.Build.targets.
        private const string IconResource = "AllyClarity.icon.png";

        public static bool IsInstalled => Chainloader.PluginInfos.ContainsKey(Guid);

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static void Init()
        {
            ModSettingsManager.SetModDescription("Tells your allies from enemies: outlines allied summons and marks "
                + "pinged allies as allies, in one color for you and your summons and one for your teammates, and "
                + "shows allies' Genesis Loop in your color instead of the Wandering Vagrant's blue. "
                + "Everything is local: only you see it, and nobody else needs the mod.");
            ModSettingsManager.SetModIcon(EmbeddedSprites.Load(IconResource));

            ModSettingsManager.AddOption(new ColorOption(PluginConfig.OwnColor));
            ModSettingsManager.AddOption(new ColorOption(PluginConfig.TeammateColor));
            ModSettingsManager.AddOption(new CheckBoxOption(PluginConfig.OutlineAllies));
            ModSettingsManager.AddOption(new CheckBoxOption(PluginConfig.AllyPings));
            ModSettingsManager.AddOption(new CheckBoxOption(PluginConfig.AllyGenesisLoop));
        }
    }
}
