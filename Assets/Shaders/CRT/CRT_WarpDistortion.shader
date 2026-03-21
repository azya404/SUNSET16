Shader "Custom/CRT/WarpDistortion"
{
    // -----------------------------------------------------------------------
    // CRT Warp Distortion — continuous subtle horizontal band displacement
    // simulating signal instability on an aged monitor.
    // Add as a RawImage material on CRTOverlay_WarpDistortion inside OverlayBackground.
    // -----------------------------------------------------------------------
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}

        _WarpStrength  ("Warp Strength",  Range(0.0, 0.05)) = 0.008
        _WarpSpeed     ("Warp Speed",     Range(0.0, 5.0))  = 1.2
        _WarpFrequency ("Warp Frequency", Range(0.0, 20.0)) = 6.0
        _WarpOpacity   ("Opacity",        Range(0.0, 1.0))  = 0.12
        _WarpColor     ("Warp Color",     Color)            = (0.9, 0.45, 0.05, 1.0)

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
            "Queue"          = "Transparent"
            "IgnoreProjector"= "True"
            "RenderType"     = "Transparent"
            "PreviewType"    = "Plane"
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
            float _WarpStrength;
            float _WarpSpeed;
            float _WarpFrequency;
            float _WarpOpacity;
            float4 _WarpColor;

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
                float  t        = _Time.y * _WarpSpeed;

                // Two layered sine waves for organic signal instability
                float wave  = sin(screenUV.y * _WarpFrequency       + t)        * _WarpStrength;
                float wave2 = sin(screenUV.y * _WarpFrequency * 0.5 + t * 1.3) * _WarpStrength * 0.5;
                float displacement = wave + wave2;

                // Derive visible band alpha from displacement magnitude
                float bandIntensity = abs(displacement) / max(_WarpStrength, 0.0001);
                float alpha         = bandIntensity * _WarpOpacity * _WarpColor.a;

                return fixed4(_WarpColor.rgb, alpha);
            }
            ENDCG
        }
    }
}
