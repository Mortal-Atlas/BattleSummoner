Shader "Hidden/Vista/TerrainGraphEditor/PositionVisualize"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" { }
	}
	SubShader
	{
		Tags { "RenderType" = "TransparentCutout" }
		//Blend SrcAlpha OneMinusSrcAlpha
		LOD 100

		Pass
		{
			CGPROGRAM

			#pragma vertex vert
			#pragma fragment frag
			#pragma shader_feature_local DATA_TYPE_INSTANCE_SAMPLE

			#include "UnityCG.cginc"
			#include "../../../../Runtime/Resources/Vista/Shaders/Includes/CommonDataTypes.hlsl"

			struct v2f
			{
				float4 vertex: SV_POSITION;
				float4 color: COLOR;
			};

			#if DATA_TYPE_INSTANCE_SAMPLE
				#define SAMPLE InstanceSample
				#define LOAD_SAMPLE LoadInstanceSample
			#else
				#define SAMPLE PositionSample
				#define LOAD_SAMPLE LoadPositionSample
			#endif

			StructuredBuffer<float> _PositionSamples;
			sampler2D _HeightMap;
			float3 _TerrainSize;
			float3 _TerrainPos;

			v2f vert(uint id: SV_VERTEXID)
			{
				SAMPLE sample = LOAD_SAMPLE(_PositionSamples, id / 2);
				float posY = tex2Dlod(_HeightMap, float4(sample.position.xz, 0, 0)).r;
				float offsetY = (id % 2 == 0) ? 20: 0;
				float posX = sample.position.x;
				float posZ = sample.position.z;
				float3 vertexPos = float3(posX * _TerrainSize.x + _TerrainPos.x, posY * _TerrainSize.y + offsetY, posZ * _TerrainSize.z + _TerrainPos.z);
				float isValid = sample.isValid * (sample.position.x >= 0) * (sample.position.x <= 1) * (sample.position.z >= 0) * (sample.position.z <= 1);

				v2f o;
				o.vertex = UnityObjectToClipPos(vertexPos);
				o.color = isValid * float4(0, 0.4, 0, 1) + (1 - isValid) * float4(0, 0, 0, 0);
				return o;
			}

			fixed4 frag(v2f i): SV_Target
			{
				if (i.color.a <= 0)
					discard;
				return i.color;
			}
			ENDCG

		}
	}
}
