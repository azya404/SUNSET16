Shader "Custom/CRT/Static"
{
    // -----------------------------------------------------------------------
    // CRT Static — per-frame positionally random noise grain overlay.
    // Simulates TV snow / film grain on an aged monitor.
    // Add as a RawImage material on CRTOverlay_Static inside OverlayBackground.
    // -----------------------------------------------------------------------
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}

        _Intensity  ("Intensity",   Range(0.0, 1.0)) = 0.08
        _Speed      ("Speed",       Range(0.0, 10.0)) = 6.0
        _GrainSize  ("Grain Size",  Range(0.5, 5.0))  = 1.0

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
            float _Speed;
            float _GrainSize;

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

            // Hash function — returns pseudo-random float in 0..1
            float hash(float2 p)
            {
                p = frac(p * float2(443.897, 441.423));
                p += dot(p, p.yx + 19.19);
                return frac((p.x + p.y) * p.x);
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

                // Scale screen position by grain size and seed with time
                float2 grainUV = floor(screenUV * (_ScreenParams.xy / _GrainSize));
                float  timeSeed = floor(_Time.y * _Speed);

                float noise = hash(grainUV + timeSeed);

                // Output: dark or bright grain, fully transparent on average
                float alpha = noise * _Intensity;
                return fixed4(noise, noise, noise, alpha);
            }
            ENDCG
        }
    }
}
