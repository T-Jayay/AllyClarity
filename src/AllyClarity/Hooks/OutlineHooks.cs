using System;
using System.Collections.Generic;
using RoR2;
using UnityEngine;

namespace AllyClarity.Hooks
{
    /// <summary>
    /// The setting OutlineAllies: allied summons get an outline in the color of whose they are. The game draws outlines
    /// with <see cref="OutlineHighlight"/>, a screen-space effect on each camera: it calls
    /// <see cref="OutlineHighlight.onPreRenderOutlineHighlight"/> every time it renders, after it has added the
    /// <see cref="Highlight"/> components' outlines, and a handler adds the renderers it wants outlined (the last color
    /// added for a renderer wins). The pings use it the same way. The effect only renders while a Highlight is on or
    /// something has asked for it (<see cref="OutlineHighlight.AddPreRenderHighlightRequest"/>, which the game's
    /// InteractionDriver and ProximityHighlight do while they have something to outline), so the mod asks for as long
    /// as the setting is on.
    /// </summary>
    internal static class OutlineHooks
    {
        // The object that asks the game to render outlines: the plugin, which lives for the whole session.
        private static UnityEngine.Object requester;

        public static void Init(UnityEngine.Object requester)
        {
            OutlineHooks.requester = requester;
            UpdateRequest();
            PluginConfig.OutlineAllies.SettingChanged += OutlineAllies_SettingChanged;
            // Subscribed before any ping exists, so a ping's own outline of an ally (PingHooks) is added after ours and
            // wins.
            OutlineHighlight.onPreRenderOutlineHighlight += OutlineHighlight_onPreRenderOutlineHighlight;
        }

        private static void OutlineAllies_SettingChanged(object sender, EventArgs e)
        {
            UpdateRequest();
        }

        private static void UpdateRequest()
        {
            if (PluginConfig.OutlineAllies.Value)
            {
                OutlineHighlight.AddPreRenderHighlightRequest(requester);
            }
            else
            {
                OutlineHighlight.ReleasePreRenderHighlightRequest(requester);
            }
        }

        /// <summary>
        /// Outlines the allied summons for the camera that is rendering. The outline is the same for each camera except
        /// its color, which depends on who is looking: split-screen players each see their own summons as their own.
        /// Runs every frame for every camera, so it walks the game's own list of character models (each model knows its
        /// body) and does no component lookups until a model is known to be an ally to outline.
        /// </summary>
        private static void OutlineHighlight_onPreRenderOutlineHighlight(OutlineHighlight outlineHighlight)
        {
            if (!PluginConfig.OutlineAllies.Value)
            {
                // The game still renders outlines for its own reasons.
                return;
            }
            // Not every outline camera is a player's: like the game's own handlers, skip the ones without a camera
            // rig, and the rigs without a local player.
            CameraRigController cameraRig = outlineHighlight.sceneCamera.cameraRigController;
            if (!cameraRig || cameraRig.localUserViewer == null)
            {
                return;
            }
            CharacterMaster viewerMaster = cameraRig.localUserViewer.cachedMaster;

            List<CharacterModel> models = InstanceTracker.GetInstancesList<CharacterModel>();
            for (int i = 0; i < models.Count; i++)
            {
                CharacterModel model = models[i];
                CharacterBody body = model.body;
                // The display models in the menus have no body, and a corpse outlives its body. Players aren't
                // outlined: only their summons and ally bodies nobody controls.
                if (!body || !AllyUtil.IsAlly(body) || body.isPlayerControlled)
                {
                    continue;
                }
                // Hidden from this camera: invisible, or cloaked, which is how the other team sees a cloaked body (the
                // player team sees a cloaked ally as "revealed", and the outline stays).
                if (model.visibility is VisibilityLevel.Invisible or VisibilityLevel.Cloaked)
                {
                    continue;
                }
                // Already outlined by the game: Drone Tech's command highlight is a Highlight on the drone's model, in
                // the color of its drone type, which ours would replace on the renderer it covers.
                if (model.TryGetComponent(out Highlight highlight) && highlight.enabled && highlight.isOn)
                {
                    continue;
                }

                Color color = AllyUtil.GetColor(body, viewerMaster);
                // As PingIndicator.RebuildPing collects a character's renderers.
                CharacterModel.RendererInfo[] rendererInfos = model.baseRendererInfos;
                for (int j = 0; j < rendererInfos.Length; j++)
                {
                    CharacterModel.RendererInfo rendererInfo = rendererInfos[j];
                    Renderer renderer = rendererInfo.renderer;
                    if (!rendererInfo.ignoreOverlays && renderer && renderer.enabled
                        && renderer.gameObject.activeInHierarchy)
                    {
                        outlineHighlight.AddHighlight(renderer, color);
                    }
                }
            }
        }
    }
}
