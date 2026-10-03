using System.Collections.Generic;
using UnityEngine;

namespace AllyClarity
{
    /// <summary>
    /// Moves the blues of the Wandering Vagrant's nova effects to a target color, for <see cref="NovaRecolor"/>. A
    /// color's hue is rotated by the distance from the nova's blue to the target's hue, and its saturation and value
    /// are scaled by the target's. A blue becomes the target, white and grey (no saturation, so no hue to rotate) stay
    /// neutral, and a white, grey or dark target makes the whole effect neutral or darker. Alpha and HDR intensity
    /// are kept.
    ///
    /// The nova's color comes from its materials (a ramp texture, _RemapTex, that the shader looks the color up in, and
    /// a tint, _TintColor), the particles' colors and the lights. Materials and ramps are shared with the boss, other
    /// enemies and other lightning effects, so they are never changed: <see cref="GetMaterial"/> returns a clone with a
    /// rotated copy of the ramp. Clones and ramps are made the first time a color needs them and kept for the session
    /// (a few small textures; an effect on screen may still be using the ones of an earlier color).
    /// </summary>
    internal readonly struct ColorShift
    {
        // The hue of the vanilla nova: its ramps, tints, particles and lights are blues between 180 and 237 degrees,
        // 207 on average.
        private const float VanillaHue = 207f / 360f;

        private static readonly int RemapTexProperty = Shader.PropertyToID("_RemapTex");
        private static readonly int TintColorProperty = Shader.PropertyToID("_TintColor");

        private static readonly Dictionary<(Material, Color), Material> materials =
            new Dictionary<(Material, Color), Material>();
        private static readonly Dictionary<(Texture, Color), Texture2D> ramps =
            new Dictionary<(Texture, Color), Texture2D>();

        private readonly Color target;
        private readonly float hueOffset;
        private readonly float saturation;
        private readonly float value;

        public ColorShift(Color target)
        {
            this.target = target;
            Color.RGBToHSV(target, out float hue, out saturation, out value);
            hueOffset = hue - VanillaHue;
        }

        public Color Apply(Color color)
        {
            Color.RGBToHSV(color, out float hue, out float colorSaturation, out float colorValue);
            Color shifted = Color.HSVToRGB(Mathf.Repeat(hue + hueOffset, 1f), colorSaturation * saturation,
                colorValue * value);
            shifted.a = color.a;
            return shifted;
        }

        /// <summary>
        /// A new gradient: the one passed in is the game's, or a vanilla value that is kept to restore, and is left
        /// as it is.
        /// </summary>
        public Gradient Apply(Gradient gradient)
        {
            GradientColorKey[] colorKeys = gradient.colorKeys;
            for (int i = 0; i < colorKeys.Length; i++)
            {
                colorKeys[i].color = Apply(colorKeys[i].color);
            }
            Gradient shifted = new Gradient { mode = gradient.mode };
            shifted.SetKeys(colorKeys, gradient.alphaKeys);
            return shifted;
        }

        public ParticleSystem.MinMaxGradient Apply(ParticleSystem.MinMaxGradient gradient)
        {
            switch (gradient.mode)
            {
                case ParticleSystemGradientMode.Color:
                    return new ParticleSystem.MinMaxGradient(Apply(gradient.color));
                case ParticleSystemGradientMode.TwoColors:
                    return new ParticleSystem.MinMaxGradient(Apply(gradient.colorMin), Apply(gradient.colorMax));
                case ParticleSystemGradientMode.Gradient:
                    return new ParticleSystem.MinMaxGradient(Apply(gradient.gradient));
                case ParticleSystemGradientMode.TwoGradients:
                    return new ParticleSystem.MinMaxGradient(Apply(gradient.gradientMin), Apply(gradient.gradientMax));
                default: // RandomColor
                    return new ParticleSystem.MinMaxGradient(Apply(gradient.gradient))
                    {
                        mode = ParticleSystemGradientMode.RandomColor
                    };
            }
        }

        /// <summary>
        /// A clone of the material (the same shader, keywords and other properties) that has a rotated copy of its
        /// ramp, and its tint rotated. Cloned rather than given a MaterialPropertyBlock: it applies to every slot of
        /// every renderer that uses the material, a particle trail's included, and the area indicator's
        /// AnimateShaderAlpha sets a block of its own.
        /// </summary>
        public Material GetMaterial(Material material)
        {
            if (!materials.TryGetValue((material, target), out Material clone))
            {
                clone = new Material(material);
                // Nothing in a scene refers to the cached objects between effects, so Unity's unloading of unused
                // assets (it runs on scene changes) mustn't free them while the cache still holds them.
                clone.hideFlags = HideFlags.DontUnloadUnusedAsset;
                clone.SetTexture(RemapTexProperty, GetRamp(material.GetTexture(RemapTexProperty)));
                clone.SetColor(TintColorProperty, Apply(material.GetColor(TintColorProperty)));
                materials.Add((material, target), clone);
            }
            return clone;
        }

        private Texture2D GetRamp(Texture ramp)
        {
            if (!ramps.TryGetValue((ramp, target), out Texture2D shifted))
            {
                // The game's ramps aren't readable by scripts: draw the ramp into a render target and read that. Both
                // are sRGB, as the ramps are, so the pixels are the ramp's own.
                RenderTexture previous = RenderTexture.active;
                RenderTexture renderTexture = RenderTexture.GetTemporary(ramp.width, ramp.height, 0,
                    RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
                Graphics.Blit(ramp, renderTexture);
                RenderTexture.active = renderTexture;
                shifted = new Texture2D(ramp.width, ramp.height, TextureFormat.RGBA32, ramp.mipmapCount > 1,
                    linear: false);
                shifted.ReadPixels(new Rect(0f, 0f, ramp.width, ramp.height), 0, 0);
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(renderTexture);

                Color[] pixels = shifted.GetPixels();
                for (int i = 0; i < pixels.Length; i++)
                {
                    pixels[i] = Apply(pixels[i]);
                }
                shifted.SetPixels(pixels);
                shifted.Apply();
                shifted.wrapMode = ramp.wrapMode;
                shifted.filterMode = ramp.filterMode;
                shifted.hideFlags = HideFlags.DontUnloadUnusedAsset;
                ramps.Add((ramp, target), shifted);
            }
            return shifted;
        }
    }
}
