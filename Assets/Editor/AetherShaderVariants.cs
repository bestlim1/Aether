#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Aether creates opaque and fade-mode greenhouse Standard materials at runtime using
/// color, scalar metallic / smoothness and optional emission. Keep those variants while dropping features
/// that cannot occur in this project's generated materials or real-time lighting.
/// </summary>
public sealed class AetherShaderVariants : IPreprocessShaders
{
    static readonly HashSet<string> UnusedKeywords = new HashSet<string>(StringComparer.Ordinal)
    {
        "_NORMALMAP",
        "_METALLICGLOSSMAP",
        "_PARALLAXMAP",
        "_DETAIL_MULX2",
        "_ALPHATEST_ON",
        "_ALPHAPREMULTIPLY_ON",
        "_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A",
        "_SPECULARHIGHLIGHTS_OFF",
        "_GLOSSYREFLECTIONS_OFF",
        "LIGHTMAP_ON",
        "DYNAMICLIGHTMAP_ON",
        "DIRLIGHTMAP_COMBINED",
        "SHADOWS_SHADOWMASK",
        "LIGHTMAP_SHADOW_MIXING"
    };

    public int callbackOrder => -100;

    public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> variants)
    {
        if (shader == null || shader.name != "Standard") return;
        int before = variants.Count;
        for (int i = variants.Count - 1; i >= 0; i--)
        {
            // Inspect the actual keyword handles provided by Unity. This works for
            // both shader-local material features and global lightmap keywords,
            // without constructing a keyword in the wrong local/global scope.
            ShaderKeyword[] keywords = variants[i].shaderKeywordSet.GetShaderKeywords();
            for (int k = 0; k < keywords.Length; k++)
            {
                if (!UnusedKeywords.Contains(keywords[k].name)) continue;
                variants.RemoveAt(i);
                break;
            }
        }
        if (variants.Count != before)
            Debug.Log("AETHER_SHADER_VARIANTS: Standard / " + snippet.passName + " / " + snippet.shaderType + ": kept " + variants.Count + " of " + before);
        // Do not filter passes. Emission, fog, light probes, forward lights,
        // reflection probes, screen-space shadows and shadow caster passes stay.
    }
}
#endif

