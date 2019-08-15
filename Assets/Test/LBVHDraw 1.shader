Shader "Unlit/LBVHDraw2"
{
    Properties
    {
    }
    SubShader
    {
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
			#pragma geometry geom
            #pragma fragment frag
			#pragma target 5.0

            #include "UnityCG.cginc"

			struct Triangle {
				float3 vertex0;
				float3 vertex1;
				float3 vertex2;
				float3 normal0;
				float3 normal1;
				float3 normal2;
			};

			StructuredBuffer<Triangle> _Result;

            struct v2g
            {
                float4 vertex0 : SV_POSITION;
                float3 vertex1 : TEXCOORD0;
                float3 vertex2 : TEXCOORD1;
            };

			struct g2f {
				float4 pos : SV_POSITION;
			};

			v2g vert(uint vertex_id : SV_VertexID, uint instance_id : SV_InstanceID)
			{
				v2g o;
				o.vertex0 = float4(_Result[instance_id].vertex0.xyz, 1.0);
				o.vertex1 = _Result[instance_id].vertex1.xyz;
				o.vertex2 = _Result[instance_id].vertex2.xyz;
				return o;
			}

			[maxvertexcount(24)]
			void geom(point v2g i[1], inout LineStream<g2f> os) 
			{
				g2f o;
				UNITY_INITIALIZE_OUTPUT(g2f, o);

				o.pos = mul(UNITY_MATRIX_VP, float4(i[0].vertex0.xyz, 1.0));
				os.Append(o);

				o.pos = mul(UNITY_MATRIX_VP, float4(i[0].vertex1.xyz, 1.0));
				os.Append(o);

				os.RestartStrip();

				o.pos = mul(UNITY_MATRIX_VP, float4(i[0].vertex0.xyz, 1.0));
				os.Append(o);

				o.pos = mul(UNITY_MATRIX_VP, float4(i[0].vertex2.xyz, 1.0));
				os.Append(o);

				os.RestartStrip();

				o.pos = mul(UNITY_MATRIX_VP, float4(i[0].vertex1.xyz, 1.0));
				os.Append(o);

				o.pos = mul(UNITY_MATRIX_VP, float4(i[0].vertex2.xyz, 1.0));
				os.Append(o);

				os.RestartStrip();
			}

            fixed4 frag (g2f i) : SV_Target
            {
                return fixed4(0,0,0, 1.0);
            }
            ENDCG
        }
    }
}
