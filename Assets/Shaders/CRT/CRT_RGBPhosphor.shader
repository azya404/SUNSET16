Shader "Custom/CRT/RGBPhosphor"
{
    // -----------------------------------------------------------------------
    // CRT RGB Phosphor Stripes — vertical R/G/B subpixel column banding.
    // Simulates the phosphor dot layout of an aged CRT monitor.
    // Add as a RawImage material on CRTOverlay_RGBPhosphor inside OverlayBackground.
    // -----------------------------------------------------------------------
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}

        _Intensity      ("Intensity",       Range(0.0, 1.0)) = 0.15
        _StripeWidth    ("Stripe Width (px)", Range(1.0, 8.0)) = 2.0

        // Required UI stencil properties
        _StencilComp      ("Stencil Comparison",  Float) = 8
        _Stencil          ("Stencil ID",          Float) = 0
        _StencilOp        ("Stencil Operation",   Float) = 0
        _StencilWriteMask ("Stencil Write Mask",  Float) = 255
        _StencilReadMask  ("Stencil Read Mask",   Float) = 255
        _ColorMask        ("Color Mask",          Float) = 15
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

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float _Intensity;
            float _StripeWidth;

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
                float2 screenUV = i.screenPos.xy / i.screenPos.w;
                float pixelX = screenUV.x * _ScreenParams.x;

                // Which stripe column are we in — cycles R / G / B
                float stripe = fmod(floor(pixelX / _StripeWidth), 3.0);

                float r = step(abs(stripe - 0.0), 0.5);
                float g = step(abs(stripe - 1.0), 0.5);
                float b = step(abs(stripe - 2.0), 0.5);

                return fixed4(r, g, b, _Intensity);
            }
            ENDCG
        }
    }
}
