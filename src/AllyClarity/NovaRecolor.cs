using System.Collections.Generic;
using UnityEngine;
using MinMaxGradient = UnityEngine.ParticleSystem.MinMaxGradient;

namespace AllyClarity
{
    /// <summary>
    /// Recolors one Genesis Loop effect (the charge sphere, the area indicator or the explosion; see
    /// <see cref="Hooks.GenesisLoopHooks"/>) from the Wandering Vagrant's blue to a color, and back. The component is
    /// added the first time an instance is recolored: the instance is still vanilla then, so it stores its vanilla
    /// values. Recoloring always starts from those, so recoloring twice, or for another color, never stacks, and
    /// restoring writes them back. Only the explosion needs that: it is pooled and shared with the boss and with
    /// enemies, so an instance that was recolored for an ally must be restored before it plays for anyone else. The
    /// charge instances are made for one use and destroyed, and are never restored.
    ///
    /// What carries the nova's color: every particle system's start color and color over lifetime, every light
    /// (including the inactive template light that a particle system's Lights module copies for each particle), and
    /// every renderer's materials. A particle renderer has two material slots, the particles' and the trail's, so
    /// going through all of the renderer's materials covers the trails. How a color is shifted is in
    /// <see cref="ColorShift"/>.
    ///
    /// The charge and the explosion also tint the screen near them cyan, the boss's color grade, with a post-processing
    /// volume. Unity's post-processing assembly isn't among the game's reference assemblies (RiskOfRain2.GameLibs), so
    /// the volume can't be recolored here; for an ally it is switched off instead, and it is found by its type's name
    /// for the same reason.
    /// </summary>
    internal sealed class NovaRecolor : MonoBehaviour
    {
        private (ParticleSystem system, MinMaxGradient startColor, MinMaxGradient lifetimeColor)[] particleSystems;
        private (Light light, Color color)[] lights;
        private (Renderer renderer, Material[] materials)[] renderers;
        private (Behaviour volume, bool enabled)[] screenTints;
        private bool recolored;

        private const string PostProcessVolumeType = "UnityEngine.Rendering.PostProcessing.PostProcessVolume";

        public static void Apply(GameObject effect, Color color)
        {
            if (!effect.TryGetComponent(out NovaRecolor recolor))
            {
                recolor = effect.AddComponent<NovaRecolor>();
                // Not in Awake: a pooled effect is inactive until after the game has reset it, and Awake runs when it
                // is activated.
                recolor.StoreVanillaValues();
            }
            recolor.SetColor(color);
        }

        /// <summary>
        /// Writes the vanilla values back, if the effect was ever recolored.
        /// </summary>
        public static void Restore(GameObject effect)
        {
            if (effect.TryGetComponent(out NovaRecolor recolor) && recolor.recolored)
            {
                recolor.SetVanillaValues();
            }
        }

        private void StoreVanillaValues()
        {
            ParticleSystem[] systems = GetComponentsInChildren<ParticleSystem>(true);
            particleSystems = new (ParticleSystem, MinMaxGradient, MinMaxGradient)[systems.Length];
            for (int i = 0; i < systems.Length; i++)
            {
                particleSystems[i] = (systems[i], systems[i].main.startColor, systems[i].colorOverLifetime.color);
            }

            Light[] allLights = GetComponentsInChildren<Light>(true);
            lights = new (Light, Color)[allLights.Length];
            for (int i = 0; i < allLights.Length; i++)
            {
                lights[i] = (allLights[i], allLights[i].color);
            }

            Renderer[] allRenderers = GetComponentsInChildren<Renderer>(true);
            renderers = new (Renderer, Material[])[allRenderers.Length];
            for (int i = 0; i < allRenderers.Length; i++)
            {
                renderers[i] = (allRenderers[i], allRenderers[i].sharedMaterials);
            }

            List<(Behaviour, bool)> volumes = new List<(Behaviour, bool)>();
            foreach (Behaviour behaviour in GetComponentsInChildren<Behaviour>(true))
            {
                if (behaviour.GetType().FullName == PostProcessVolumeType)
                {
                    volumes.Add((behaviour, behaviour.enabled));
                }
            }
            screenTints = volumes.ToArray();
        }

        private void SetColor(Color color)
        {
            ColorShift shift = new ColorShift(color);
            foreach (var (system, startColor, lifetimeColor) in particleSystems)
            {
                ParticleSystem.MainModule main = system.main;
                main.startColor = shift.Apply(startColor);
                ParticleSystem.ColorOverLifetimeModule lifetime = system.colorOverLifetime;
                lifetime.color = shift.Apply(lifetimeColor);
            }
            foreach (var (light, vanillaColor) in lights)
            {
                light.color = shift.Apply(vanillaColor);
            }
            foreach (var (renderer, vanillaMaterials) in renderers)
            {
                Material[] shiftedMaterials = new Material[vanillaMaterials.Length];
                for (int i = 0; i < vanillaMaterials.Length; i++)
                {
                    // A slot can be empty: a particle renderer's trail slot unless it has trails, and its particle slot
                    // when it only draws trails.
                    shiftedMaterials[i] = vanillaMaterials[i] ? shift.GetMaterial(vanillaMaterials[i]) : null;
                }
                renderer.sharedMaterials = shiftedMaterials;
            }
            foreach (var (volume, _) in screenTints)
            {
                volume.enabled = false;
            }
            recolored = true;
        }

        private void SetVanillaValues()
        {
            foreach (var (system, startColor, lifetimeColor) in particleSystems)
            {
                ParticleSystem.MainModule main = system.main;
                main.startColor = startColor;
                ParticleSystem.ColorOverLifetimeModule lifetime = system.colorOverLifetime;
                lifetime.color = lifetimeColor;
            }
            foreach (var (light, vanillaColor) in lights)
            {
                light.color = vanillaColor;
            }
            foreach (var (renderer, vanillaMaterials) in renderers)
            {
                renderer.sharedMaterials = vanillaMaterials;
            }
            foreach (var (volume, enabled) in screenTints)
            {
                volume.enabled = enabled;
            }
            recolored = false;
        }
    }
}
