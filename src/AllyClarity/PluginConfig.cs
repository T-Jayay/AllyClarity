using BepInEx.Configuration;
using UnityEngine;

namespace AllyClarity
{
    /// <summary>
    /// The settings in BepInEx/config/revor.AllyClarity.cfg. Everything is per player (the mod only changes what this
    /// game instance shows) and applies immediately. Section and key names are stored in players' config files:
    /// renaming or moving one resets that setting for everyone, so they must never change.
    /// </summary>
    internal static class PluginConfig
    {
        private const string Section = "General";

        public static ConfigEntry<Color> OwnColor { get; private set; }
        public static ConfigEntry<Color> TeammateColor { get; private set; }
        public static ConfigEntry<bool> OutlineAllies { get; private set; }
        public static ConfigEntry<bool> AllyPings { get; private set; }
        public static ConfigEntry<bool> AllyGenesisLoop { get; private set; }

        public static void Init(ConfigFile config)
        {
            OwnColor = config.Bind(Section, "OwnColor", new Color(0.3f, 1f, 0.3f),
                "The color of you, your summons (drones, turrets, ...) and the pings on them, and of every ally's "
                + "Genesis Loop explosion.");
            TeammateColor = config.Bind(Section, "TeammateColor", new Color(0.45f, 0.8f, 1f),
                "The color of the other players, their summons and allies that nobody owns, and the pings on them. "
                + "Not used for Genesis Loop explosions: those are in OwnColor for every ally.");
            OutlineAllies = config.Bind(Section, "OutlineAllies", true,
                "Outline allied summons (yours, your teammates' and allies that nobody owns) in the colors above, so "
                + "they are easy to tell from enemies. Players aren't outlined.");
            AllyPings = config.Bind(Section, "AllyPings", true,
                "Pinging an ally marks it as an ally instead of an enemy: no combat icon, its outline and the ping "
                + "text in the colors above, and a chat line that says so instead of \"wants to attack\".");
            AllyGenesisLoop = config.Bind(Section, "AllyGenesisLoop", true,
                "Show the Genesis Loop of every ally (yours, your teammates' and allied summons') in OwnColor "
                + "instead of the Wandering Vagrant's blue, which the item reuses and which makes it look like an "
                + "enemy's: the charge sphere, the area indicator and the explosion. Enemies' Genesis Loop and the "
                + "Wandering Vagrant stay blue.");
        }
    }
}
