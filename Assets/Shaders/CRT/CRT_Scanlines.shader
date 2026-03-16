Shader "Custom/CRT/Scanlines"
{
    // -----------------------------------------------------------------------
    // CRT Scanlines — UI overlay shader
    // Apply as material on a RawImage that covers the ComputerCanvas Frame 2.
    // RaycastTarget must be OFF on that RawImage.
    // -----------------------------------------------------------------------
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}

        // --- Scanline controls (exposed in Material Inspector) ---
        _ScanlineSpacing  ("Line Spacing (pixels)", Float)       = 4.0
        _ScanlineOpacity  ("Line Opacity",  Range(0.0, 1.0))    = 0.25
        _ScanlineWidth    ("Line Width (0=thin 1=thick)", Range(0.0, 1.0)) = 0.4

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
            float _ScanlineSpacing;
            float _ScanlineOpacity;
            float _ScanlineWidth;

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
                // Convert to actual screen pixel Y coordinate
                float2 screenUV = i.screenPos.xy / i.screenPos.w;
                float  pixelY   = screenUV.y * _ScreenParams.y;

                // Every _ScanlineSpacing pixels, draw a dark band
                // _ScanlineWidth controls how thick the dark band is (0=hairline, 1=fills the gap)
                float pattern = fmod(floor(pixelY), _ScanlineSpacing) / _ScanlineSpacing;
                float isLine  = step(1.0 - _ScanlineWidth, pattern);

                // Output: black with variable opacity only where scanlines fall
                // Transparent everywhere else — does not affect underlying UI colours
                return fixed4(0.0, 0.0, 0.0, isLine * _ScanlineOpacity);
            }
            ENDCG
        }
    }
}
