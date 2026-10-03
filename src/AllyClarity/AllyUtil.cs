using RoR2;
using UnityEngine;

namespace AllyClarity
{
    /// <summary>
    /// Who is an ally and whose, for the outlines and the pings (the Genesis Loop only needs to know who is an ally).
    ///
    /// An ally is a body on the player team: the players, their summons (drones, turrets, Empathy Cores, Col.
    /// Droneman, ...) and ally bodies that nobody owns. A body is "own" for a viewer when it is the viewer's body, or a
    /// summon of the viewer's master (minionOwnership.ownerMaster); every other ally is a teammate's.
    ///
    /// A summon of a summon belongs to the original owner, so the owner chain is never followed:
    /// <see cref="MasterSummon.propogateMinionOwnership"/> is on by default, and the game's own summons (Engineer's
    /// turrets, bought drones, the Empathy Cores and Col. Droneman through the director's spawn request) all go through
    /// <see cref="MasterSummon"/> with the owner's body as the summoner.
    /// </summary>
    internal static class AllyUtil
    {
        public static bool IsAlly(CharacterBody body)
        {
            return body.teamComponent.teamIndex == TeamIndex.Player;
        }

        /// <summary>
        /// The color an ally has for a viewer: <see cref="PluginConfig.OwnColor"/> or
        /// <see cref="PluginConfig.TeammateColor"/>.
        /// </summary>
        public static Color GetColor(CharacterBody body, CharacterMaster viewerMaster)
        {
            return IsOwn(body, viewerMaster) ? PluginConfig.OwnColor.Value : PluginConfig.TeammateColor.Value;
        }

        private static bool IsOwn(CharacterBody body, CharacterMaster viewerMaster)
        {
            // Some bodies have no master, and a local player has none before their master has spawned.
            CharacterMaster master = body.master;
            if (!master || !viewerMaster)
            {
                return false;
            }
            return master == viewerMaster || master.minionOwnership.ownerMaster == viewerMaster;
        }
    }
}
