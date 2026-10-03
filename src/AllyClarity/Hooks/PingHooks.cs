using System;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using RoR2.UI;
using UnityEngine;

namespace AllyClarity.Hooks
{
    /// <summary>
    /// The setting AllyPings. PingIndicator.RebuildPing runs on every client when a player pings, and treats every
    /// pinged character as an enemy: the red combat icon, a red outline, red ping text and the chat line "wants to
    /// attack". For an ally it is changed to the default ping (no combat icon), with the outline and the text in the
    /// ally's color, and a chat line in that color that says an ally was pinged.
    ///
    /// Two hooks: the chat line is added in the middle of RebuildPing, so an IL hook swaps its text; the rest is
    /// changed afterwards by an On hook, which is simpler than finding each place in the method that sets it.
    /// </summary>
    internal static class PingHooks
    {
        public static void Init()
        {
            On.RoR2.UI.PingIndicator.RebuildPing += PingIndicator_RebuildPing;
            IL.RoR2.UI.PingIndicator.RebuildPing += PingIndicator_RebuildPing;
        }

        /// <summary>
        /// Turns an enemy ping on an ally into an ally ping: undoes what RebuildPing did for an enemy (the enemy icons
        /// on, the red outline and text) and does what it does for a default ping.
        /// </summary>
        private static void PingIndicator_RebuildPing(On.RoR2.UI.PingIndicator.orig_RebuildPing orig,
            PingIndicator self)
        {
            orig(self);
            if (self.pingType != PingIndicator.PingType.Enemy || !TryGetAllyColor(self, out Color color))
            {
                return;
            }
            foreach (GameObject enemyPingObject in self.enemyPingGameObjects)
            {
                enemyPingObject.SetActive(false);
            }
            foreach (GameObject defaultPingObject in self.defaultPingGameObjects)
            {
                defaultPingObject.SetActive(true);
            }
            // The outline of the target's renderers, which RebuildPing collected for the enemy ping.
            self.activeColor = color;
            // RebuildPing ends with pingText.color = textBaseColor * pingColor.
            self.pingColor = color;
            self.pingText.color = self.textBaseColor * color;
        }

        /// <summary>
        /// Replaces the chat line of an enemy ping, the token PLAYER_PING_ENEMY, when the target is an ally. RebuildPing
        /// passes the text to Language.GetString, which returns anything that isn't a token as it is, and then formats
        /// it with the pinger's and the target's names, so the replacement is the format itself. If the game changes
        /// that code, the hook leaves the method unchanged and the log says so; ally pings then keep "wants to attack".
        /// </summary>
        private static void PingIndicator_RebuildPing(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);
            if (!cursor.TryGotoNext(MoveType.After, x => x.MatchLdstr("PLAYER_PING_ENEMY")))
            {
                AllyClarityPlugin.Log.LogError($"IL hook {il.Method.Name} failed: PLAYER_PING_ENEMY not found. "
                    + "Pings on allies keep the chat line \"wants to attack\" until AllyClarity is updated.");
                return;
            }
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Func<string, PingIndicator, string>>(GetChatLine);
        }

        private static string GetChatLine(string enemyToken, PingIndicator ping)
        {
            if (!TryGetAllyColor(ping, out Color color))
            {
                return enemyToken;
            }
            return "<color=#" + ColorUtility.ToHtmlStringRGB(color) + ">{0} pinged an ally: {1}.</color>";
        }

        /// <summary>
        /// The color of the ping's target if it is an ally and AllyPings is on. Only for a ping the game made an enemy
        /// ping, whose target is always a character body. A ping is one object for every local player, so unlike the
        /// outlines it uses the first local player's view of whose ally it is.
        /// </summary>
        private static bool TryGetAllyColor(PingIndicator ping, out Color color)
        {
            CharacterBody body = ping.pingTarget.GetComponent<CharacterBody>();
            if (!PluginConfig.AllyPings.Value || !AllyUtil.IsAlly(body))
            {
                color = default;
                return false;
            }
            color = AllyUtil.GetColor(body, LocalUserManager.GetFirstLocalUser().cachedMaster);
            return true;
        }
    }
}
