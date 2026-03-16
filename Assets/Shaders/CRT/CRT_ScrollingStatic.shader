Shader "Custom/CRT/ScrollingStatic"
{
    // -----------------------------------------------------------------------
    // CRT Scrolling Static — UI overlay shader
    // Animated noise bands that drift from top to bottom, like TV interference.
    // Apply as material on a RawImage inside ComputerCanvas > OverlayBackground.
    // RaycastTarget must be OFF on that RawImage.
    // -----------------------------------------------------------------------
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}

        // --- Effect controls ---
        _ScrollSpeed   ("Scroll Speed",   Float)      = 0.3
        _BandFrequency ("Band Frequency", Float)      = 3.0
        _BandOpacity   ("Band Opacity",   Range(0,1)) = 0.15
        _NoiseScale    ("Noise Scale",    Float)      = 50.0

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
            float _ScrollSpeed;
            float _BandFrequency;
            float _BandOpacity;
            float _NoiseScale;

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

            // Simple hash — produces pseudo-random value from a 2D coordinate
            float hash(float2 p)
            {
                p  = frac(p * float2(127.1, 311.7));
                p += dot(p, p + 19.19);
                return frac(p.x * p.y);
            }

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
                float2 screenUV = i.screenPos.xy / i.screenPos.w;

                // Scrolling UV — bands drift downward over time
                float scrolledY = screenUV.y + _Time.y * _ScrollSpeed;

                // Sine wave creates smooth rolling bands
                float band = sin(scrolledY * _BandFrequency * 6.2832) * 0.5 + 0.5;

                // Per-pixel noise roughens the band edges so they look like interference
                float noise = hash(floor(screenUV * _NoiseScale + float2(0, _Time.y * _ScrollSpeed * _NoiseScale)));

                float alpha = band * noise * _BandOpacity;

                // Bright white interference bands
                return fixed4(1.0, 1.0, 1.0, alpha);
            }
            ENDCG
        }
    }
}
