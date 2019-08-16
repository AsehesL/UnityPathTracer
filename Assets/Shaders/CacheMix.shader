Shader "Hidden/CacheMix"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
		_Cache ("Cache", 2D) = "black" {}
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
			sampler2D _Cache;

			float _Frame;

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
				i.uv.y = 1.0 - i.uv.y;
                fixed4 cache = tex2D(_Cache, i.uv) * _Frame;
				//col.rgb = lerp(cache.rgb, col.rgb, 0.01);
				//fixed l = Luminance(col.rgb);
				//col.rgb = lerp(cache.rgb, col.rgb, l);
				col.rgb += cache.rgb;
				col.rgb /= _Frame + 1.0;
				col.a = 1.0;
                return col;
            }
            ENDCG
        }
    }
}
