// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

//Copyright (c) 2014 Tilman Schmidt (@KeyMaster_)

//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:

//The above copyright notice and this permission notice shall be included in
//all copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//THE SOFTWARE.
Shader "UI/DigitalGlitch_Overlay"
{
    Properties
    {
        _Intensity ("Intensity", Range(0,1)) = 0
        _BlockSize ("Block Size", Float) = 40
        _DispStrength ("Displacement", Float) = 0.02
    }

    SubShader
    {
        Tags 
        { 
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv     : TEXCOORD0;
            };

            float _Intensity;
            float _BlockSize;
            float _DispStrength;

            float rand(float2 co)
            {
                return frac(sin(dot(co, float2(12.9898,78.233))) * 43758.5453);
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
			{
				float2 uv = i.uv;

				float t = floor(_Time.y * 4);

				float2 blockUV = float2(
					floor(uv.x * _BlockSize) / _BlockSize,
					floor(uv.y * (_BlockSize * 0.3)) / (_BlockSize * 0.3)
				);

				float noise = rand(blockUV + t);

				float glitch = step(0.92, noise) * _Intensity;

				float2 offset = float2(
					rand(blockUV + t * 1.3) - 0.5,
					rand(blockUV + t * 1.7) - 0.5
				) * _DispStrength * glitch;

				uv += offset;

				float r = rand(blockUV + t * 2.0);
				float g = rand(blockUV + t * 3.0);
				float b = rand(blockUV + t * 4.0);

				float3 col = float3(r, g, b);

				float gray = dot(col, float3(0.3, 0.59, 0.11));
				col = lerp(col, float3(gray, gray, gray), 0.7);

				col *= 0.6;

				return float4(col, glitch);
			}
            ENDCG
        }
    }
}