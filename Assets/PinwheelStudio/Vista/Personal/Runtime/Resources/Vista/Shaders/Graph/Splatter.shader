Shader "Hidden/Vista/Graph/Splatter"
{
	SubShader
	{
		Tags { "RenderType" = "Opaque" }
		LOD 100
		ColorMask R
		Blend SrcColor OneMinusSrcColor

		Pass
		{
			CGPROGRAM

			#pragma vertex vert
			#pragma fragment frag

			struct v2f
			{
				float2 uv: TEXCOORD0;
				float4 vertex: SV_POSITION;
				float4 localPos: TEXCOORD1;
				float intensity : TEXCOORD2;
			};

			StructuredBuffer<float> _Vertices;
			StructuredBuffer<float> _Texcoords;

			float2 LoadPackedFloat2(StructuredBuffer<float> buffer, uint index)
			{
				uint offset = index * 2;
				return float2(buffer[offset], buffer[offset + 1]);
			}

			int _HasMaskMap;
			sampler2D _Mask;

			float _IntensityMultiplier;
			int _HasIntensityBuffer;
			StructuredBuffer<float> _Intensity;

			v2f vert(uint id: SV_VERTEXID)
			{
				v2f o;
				float4 vertex = float4(LoadPackedFloat2(_Vertices, id), 0, 1);
				o.vertex = UnityObjectToClipPos(vertex);
				o.uv = LoadPackedFloat2(_Texcoords, id);
				o.localPos = vertex;
				o.intensity = _IntensityMultiplier;
				if (_HasIntensityBuffer)
				{
					o.intensity *= _Intensity[id / 6];
				}
				return o;
			}

			float frag(v2f input) : SV_Target
			{
				float value = input.intensity;
				if (_HasMaskMap)
				{
					value *= tex2D(_Mask, input.uv).r;
				}
				return value;
			}
			ENDCG
		}
	}
}
