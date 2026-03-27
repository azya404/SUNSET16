Shader "UI/DigitalGlitch"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Intensity ("Intensity", Range(0,1)) = 0
        _BlockSize ("Block Size", Float) = 20
        _DispStrength ("Displacement", Float) = 0.05
        _ColorSplit ("Color Split", Float) = 0.01
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float _Intensity;
            float _BlockSize;
            float _DispStrength;
            float _ColorSplit;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
            };

            float rand(float2 co)
            {
                return frac(sin(dot(co,float2(12.9898,78.233))) * 43758.5453);
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;

                // Time-based randomness
                float t = floor(_Time.y * 10);

                // Create block grid
                float2 blockUV = floor(uv * _BlockSize) / _BlockSize;

                // Random value per block
                float noise = rand(blockUV + t);

                // Only glitch some blocks
                float glitch = step(0.85, noise) * _Intensity;

                // Block displacement
                float2 offset = float2(
                    rand(blockUV + t * 1.3) - 0.5,
                    rand(blockUV + t * 1.7) - 0.5
                ) * _DispStrength * glitch;

                uv += offset;

                // Sample base
                fixed4 col;

                // RGB split (like KinoGlitch)
                float r = tex2D(_MainTex, uv + float2(_ColorSplit * glitch, 0)).r;
                float g = tex2D(_MainTex, uv).g;
                float b = tex2D(_MainTex, uv - float2(_ColorSplit * glitch, 0)).b;

                col = float4(r, g, b, tex2D(_MainTex, uv).a);

                return col * i.color;
            }
            ENDCG
        }
    }
}