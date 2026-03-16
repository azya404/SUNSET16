Shader "Custom/CRT/BarrelWarp"
{
    // -----------------------------------------------------------------------
    // CRT Barrel Warp — fullscreen barrel/pincushion distortion.
    // Applied via URP Fullscreen Pass Renderer Feature, not a UI RawImage.
    // Enabled/disabled at runtime by CRTBarrelWarpController.cs
    // -----------------------------------------------------------------------
    Properties
    {
        _WarpStrength ("Warp Strength", Range(0.0, 0.3)) = 0.08
        _WarpZoom     ("Warp Zoom",     Range(0.8, 1.0)) = 0.95
        _EdgeDarkness ("Edge Darkness", Range(0.0, 1.0)) = 0.4
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }

        ZWrite Off
        Cull Off
        ZTest Always

        Pass
        {
            Name "CRTBarrelWarpPass"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _WarpStrength;
            float _WarpZoom;
            float _EdgeDarkness;

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;

                // Move origin to screen centre (-1 to +1)
                float2 centered = uv * 2.0 - 1.0;

                // Barrel distortion: push corners outward
                float r2 = dot(centered, centered);
                float2 distorted = centered * (1.0 + _WarpStrength * r2);

                // Slight zoom-in so the distorted edges don't show gaps
                distorted /= _WarpZoom;

                // Back to 0..1 UV space
                float2 warpedUV = distorted * 0.5 + 0.5;

                // Pure black outside screen bounds (the CRT bezel)
                if (warpedUV.x < 0.0 || warpedUV.x > 1.0 ||
                    warpedUV.y < 0.0 || warpedUV.y > 1.0)
                    return half4(0.0, 0.0, 0.0, 1.0);

                half4 col = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, warpedUV);

                // Vignette darkening toward warped edges
                float vignette = 1.0 - saturate(r2 * _EdgeDarkness);
                col.rgb *= vignette;

                return col;
            }
            ENDHLSL
        }
    }
}
