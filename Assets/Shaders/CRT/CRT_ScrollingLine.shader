Shader "Custom/CRT/ScrollingLine"
{
    // -----------------------------------------------------------------------
    // CRT Scrolling Line — single horizontal line controlled by CRTScrollingLineController.cs
    // The script passes _LineY and _LineActive each frame.
    // Do NOT animate this shader directly — use the script.
    // -----------------------------------------------------------------------
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}

        // Controlled by CRTScanLineController at runtime
        _LineY         ("Line Y Position (1=top, 0=bottom)", Range(0,1)) = 1.0
        _LineActive    ("Line Active (0=off, 1=on)",         Float)      = 0.0

        // Appearance — set these on the material, not at runtime
        _LineThickness ("Core Thickness",  Range(0.001, 0.02)) = 0.004
        _GlowWidth     ("Glow Falloff",    Range(0.0,   0.04)) = 0.012
        _LineColor     ("Line Color",      Color) = (1.0, 0.55, 0.1, 1.0)

        // --- Required by Unity UI stencil system (do not remove) ---
        _StencilComp      ("Stencil Comparison", Float) = 8
        _Stencil          ("Stencil ID",         Float) = 0
        _StencilOp        ("Stencil Operation",  Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask  ("Stencil Read Mask",  Float) = 255
        _ColorMask        ("Color Mask",         Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue"           = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType"      = "Transparent"
            "PreviewType"     = "Plane"
        }

        Stencil
        {
            Ref       [_Stencil]
            Comp      [_StencilComp]
            Pass      [_StencilOp]
            ReadMask  [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull     Off
        Lighting Off
        ZWrite   Off
        ZTest    [unity_GUIZTestMode]
        Blend    SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float  _LineY;
            float  _LineActive;
            float  _LineThickness;
            float  _GlowWidth;
            float4 _LineColor;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos       : SV_POSITION;
                float2 uv        : TEXCOORD0;
                float4 screenPos : TEXCOORD1;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos       = UnityObjectToClipPos(v.vertex);
                o.uv        = v.uv;
                o.screenPos = ComputeScreenPos(o.pos);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Script sets this to 0 when no line is active — early out
                if (_LineActive < 0.5)
                    return fixed4(0, 0, 0, 0);

                float2 screenUV = i.screenPos.xy / i.screenPos.w;
                float  dist     = abs(screenUV.y - _LineY);

                // Bright core line
                float core = 1.0 - smoothstep(0.0, _LineThickness, dist);

                // Soft orange glow fading out around the core
                float glow = (1.0 - smoothstep(_LineThickness, _LineThickness + _GlowWidth, dist)) * 0.35;

                float alpha = max(core, glow) * _LineColor.a;

                return fixed4(_LineColor.rgb, alpha);
            }
            ENDCG
        }
    }
}
