Shader "Hidden/Vista/Graph/CrackTrail"
{
	SubShader
	{
		Tags { "RenderType" = "Opaque" }
		LOD 100
		Cull Off

		Pass
		{
			CGPROGRAM

			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"
			#include "../Includes/Math.hlsl"
			#include "../Includes/CommonDataTypes.hlsl"

			struct v2f
			{
				float4 vertex: SV_POSITION;
				float4 localPos: TEXCOORD1;
				float intensity: TEXCOORD2;
			};

			StructuredBuffer<float> _Vertices;

			float3 LoadCrackVertex(uint index)
			{
				uint offset = index * 3;
				return float3(_Vertices[offset], _Vertices[offset + 1], _Vertices[offset + 2]);
			}

			v2f vert(uint id: SV_VERTEXID)
			{
				float3 v = LoadCrackVertex(id.x);
				float4 vertex = float4(v.xy, 0, 1);

				v2f o;
				o.vertex = UnityObjectToClipPos(vertex);
				o.localPos = vertex;
				o.intensity = v.z;
				return o;
			}

			float frag(v2f input): SV_Target
			{
				return input.intensity;
			}
			ENDCG

		}
	}
}
