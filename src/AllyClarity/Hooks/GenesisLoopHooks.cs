using EntityStates.VagrantMonster;
using EntityStates.VagrantNovaItem;
using RoR2;
using UnityEngine;

namespace AllyClarity.Hooks
{
    /// <summary>
    /// The setting AllyGenesisLoop. The Genesis Loop item reuses the Wandering Vagrant's nova effects: its charge
    /// sphere, its area indicator and its explosion are the boss's, in the boss's blue, so an ally's Genesis Loop looks
    /// like an enemy's. Allies' are recolored with <see cref="NovaRecolor"/>; enemies' and the boss's stay as they are.
    ///
    /// The charge: ChargeState.OnEnter runs on every machine (the item's state machine is networked), and makes the
    /// sphere and the area indicator as plain instances of the boss's prefabs, for one use each. An On hook recolors
    /// them after the original has made them, before they have emitted anything.
    ///
    /// The explosion: DetonateState.OnEnter spawns it on the server, transmitted to the clients, with the holder's
    /// body as the effect data's root object (null for a client that doesn't know the body). The boss spawns the same
    /// prefab on each machine itself, with its own body as the root object, so the team of the root object tells whose
    /// explosion it is. The effect is pooled, one pool for the prefab, so an instance is reused for whoever explodes
    /// next. The game applies the effect data to an instance in EffectComponent.Reset, which runs on every machine for
    /// a new instance and for a reused one, before the instance is activated, so an On hook there recolors it for an
    /// ally and restores it for anyone else.
    /// </summary>
    internal static class GenesisLoopHooks
    {
        // The nova explosion's effect index, set once when the catalogs are ready. Reset runs for every effect in the
        // game, so the hook checks it first.
        private static EffectIndex novaEffectIndex = EffectIndex.Invalid;

        public static void Init()
        {
            RoR2Application.onLoad += RoR2Application_onLoad;
            On.EntityStates.VagrantNovaItem.ChargeState.OnEnter += ChargeState_OnEnter;
            On.RoR2.EffectComponent.Reset += EffectComponent_Reset;
        }

        /// <summary>
        /// FireMegaNova.novaEffectPrefab is a static field that the game fills from an entity state configuration, in
        /// EntityStateCatalog.Init, and the effect index is assigned to the prefab by the EffectCatalog, a
        /// SystemInitializer. The game's loading routine runs both before onLoad.
        /// </summary>
        private static void RoR2Application_onLoad()
        {
            novaEffectIndex = EffectCatalog.FindEffectIndexFromPrefab(FireMegaNova.novaEffectPrefab);
        }

        /// <summary>
        /// Every ally's Genesis Loop has the same color, OwnColor, the "friendly" one: whose ally it is doesn't matter
        /// for a Genesis Loop, only that it isn't an enemy's. The color of the outlines and pings of teammates,
        /// TeammateColor, isn't used.
        /// </summary>
        private static Color GetColor()
        {
            return PluginConfig.OwnColor.Value;
        }

        private static void ChargeState_OnEnter(On.EntityStates.VagrantNovaItem.ChargeState.orig_OnEnter orig,
            ChargeState self)
        {
            orig(self);
            // A client may not know the holder's body yet, and then the state makes no instances.
            if (!PluginConfig.AllyGenesisLoop.Value || !self.attachedBody || !AllyUtil.IsAlly(self.attachedBody))
            {
                return;
            }
            Color color = GetColor();
            NovaRecolor.Apply(self.chargeVfxInstance, color);
            NovaRecolor.Apply(self.areaIndicatorVfxInstance, color);
        }

        private static void EffectComponent_Reset(On.RoR2.EffectComponent.orig_Reset orig, EffectComponent self)
        {
            orig(self);
            if (self.effectIndex != novaEffectIndex)
            {
                return;
            }
            // The game's Reset does nothing without effect data. The root object is null if this client doesn't know
            // the exploding body (it has been destroyed, for one), and needn't be a body when another mod spawns the
            // effect: vanilla then.
            GameObject exploder = self.effectData?.rootObject;
            if (PluginConfig.AllyGenesisLoop.Value && exploder && exploder.TryGetComponent(out CharacterBody body)
                && AllyUtil.IsAlly(body))
            {
                NovaRecolor.Apply(self.gameObject, GetColor());
            }
            else
            {
                NovaRecolor.Restore(self.gameObject);
            }
        }
    }
}
