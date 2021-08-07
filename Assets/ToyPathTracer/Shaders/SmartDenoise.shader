Shader "Hidden/SmartDenoise"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            //
            //  https://michelemorrone.eu - https://BrutPitt.com
            //

            #define INV_SQRT_OF_2PI 0.39894228040143267793994605993439

            float _Sigma;
            float _KSigma;
            float _Threshold;

            float4 SmartDeNoise(sampler2D tex, float2 texSize, float2 uv, float sigma, float kSigma, float threshold)
            {
                float radius = round(kSigma * sigma);
                float radQ = radius * radius;

                float invSigmaQx2 = 0.5 / (sigma * sigma);
                float invSigmaQx2PI = UNITY_INV_PI * invSigmaQx2;

                float invThresholdSqx2 = 0.5 / (threshold * threshold);
                float invThresholdSqrt2PI = INV_SQRT_OF_2PI / threshold;

                float4 centrPx = tex2D(tex, uv);

                float zBuff = 0.0;
                float4 aBuff = 0.0;
                float2 size = texSize;

                float2 d;
                for (d.x = -radius; d.x <= radius; d.x++) {
                    float pt = sqrt(radQ - d.x * d.x);       // pt = yRadius: have circular trend
                    for (d.y = -pt; d.y <= pt; d.y++) {
                        float blurFactor = exp(-dot(d , d) * invSigmaQx2) * invSigmaQx2PI;

                        float4 walkPx = tex2D(tex, uv + d / size);
                        float4 dC = walkPx - centrPx;
                        float deltaFactor = exp(-dot(dC, dC) * invThresholdSqx2) * invThresholdSqrt2PI * blurFactor;

                        zBuff += deltaFactor;
                        aBuff += deltaFactor * walkPx;
                    }
                }
                return aBuff / zBuff;
            }

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;

            float4 frag (v2f i) : SV_Target
            {
                float4 col = SmartDeNoise(_MainTex, _MainTex_TexelSize.zw, i.uv, _Sigma, _KSigma, _Threshold);
                col.a = 1.0;
                return col;
            }
            ENDCG
        }
    }
}
