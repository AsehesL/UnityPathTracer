Shader "Hidden/TimeFilter"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _PreviousFrame("Cache", 2D) = "black" {}
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

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
            sampler2D _PreviousFrame;

            float _Frame;

            float4 frag (v2f i) : SV_Target
            {
                float4 col = tex2D(_MainTex, i.uv);
                float4 cache = tex2D(_PreviousFrame, i.uv) * _Frame;
                col.rgb += cache.rgb;
                col.rgb /= _Frame + 1.0;
                col.a = 1.0;
                return col;
            }
            ENDCG
        }
    }
}
