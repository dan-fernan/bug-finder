using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace BugDefense
{
    /// <summary>
    /// Creates (and caches) materials with the right shader for the active render pipeline.
    /// Built-in "Standard" renders pink under URP, so we pick URP/Lit when a pipeline is active.
    /// </summary>
    public static class MaterialFactory
    {
        static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { cache.Clear(); }

        static bool UsingSRP => GraphicsSettings.currentRenderPipeline != null;

        static Shader LitShader()
        {
            Shader s = UsingSRP ? Shader.Find("Universal Render Pipeline/Lit") : Shader.Find("Standard");
            return s != null ? s : Shader.Find("Standard");
        }

        static Shader UnlitShader()
        {
            Shader s = UsingSRP ? Shader.Find("Universal Render Pipeline/Unlit") : Shader.Find("Unlit/Color");
            return s != null ? s : Shader.Find("Unlit/Color");
        }

        static void SetColor(Material m, Color c)
        {
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
        }

        /// <summary>Lit material, optionally emissive (emission = color * emission).</summary>
        public static Material Lit(Color color, float emission = 0f, float smoothness = 0.3f)
        {
            string key = $"lit_{ColorUtility.ToHtmlStringRGBA(color)}_{emission:0.00}_{smoothness:0.00}";
            if (cache.TryGetValue(key, out var cached) && cached != null) return cached;

            var m = new Material(LitShader()) { name = key };
            SetColor(m, color);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smoothness);
            if (emission > 0f && m.HasProperty("_EmissionColor"))
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", color * emission);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            cache[key] = m;
            return m;
        }

        /// <summary>Flat unlit color (good for glowing bits like projectiles and LEDs).</summary>
        public static Material Unlit(Color color)
        {
            string key = $"unlit_{ColorUtility.ToHtmlStringRGBA(color)}";
            if (cache.TryGetValue(key, out var cached) && cached != null) return cached;
            var m = new Material(UnlitShader()) { name = key };
            SetColor(m, color);
            cache[key] = m;
            return m;
        }

        public static Color Hex(string hex)
        {
            return ColorUtility.TryParseHtmlString(hex.StartsWith("#") ? hex : "#" + hex, out var c) ? c : Color.magenta;
        }
    }
}
