// Made with Amplify Shader Editor v1.9.9.12
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Vefects/SH_Vefects_VFX_Zoom_Blur_Radial_Noise_01_URP"
{
	Properties
	{
		_Strength( "Blur Strength", Range( -1, 1 ) ) = 0.12
		_Samples( "Samples", Range( 2, 64 ) ) = 8
		_Opacity( "Opacity", Range( 0, 1 ) ) = 1
		_EdgeSoftness( "Edge Softness", Range( 0.001, 1 ) ) = 0.3
		_UseCustomStrength( "Use Custom Strength", Range( 0, 1 ) ) = 0
		_UseRadialNoise( "Use Radial Noise", Range( 0, 1 ) ) = 1
		_NoiseAmount( "Noise Amount", Range( 0, 1 ) ) = 1
		_RadialNoiseTex( "Radial Noise Texture", 2D ) = "white" {}
		_NoiseAngularTiling( "Noise Angular Tiling", Range( 1, 128 ) ) = 8
		_NoiseRadialTiling( "Noise Radial Tiling", Range( 0, 8 ) ) = 0.25
		_NoiseScrollSpeed( "Noise Scroll Speed", Range( -4, 4 ) ) = 0
		_NoiseContrast( "Noise Contrast", Range( 0, 2.67 ) ) = 1
		_NoiseInnerRadius( "Noise Inner Radius", Range( 0, 0.95 ) ) = 0.25
		_NoiseLineDarkness( "Noise Line Darkness", Range( 0, 1 ) ) = 0.45
		_NoiseBaseOpacity( "Noise Base Opacity", Range( 0, 1 ) ) = 0.8
		[Space(13)][Header(AR)][Space(13)] _Cull1( "Cull", Float ) = 0
		_Src1( "Src", Float ) = 5
		_Dst1( "Dst", Float ) = 10
		_ZWrite1( "ZWrite", Float ) = 0
		_ZTest1( "ZTest", Float ) = 8


		//_TessPhongStrength( "Tess Phong Strength", Range( 0, 1 ) ) = 0.5
		//_TessValue( "Tess Max Tessellation", Range( 1, 32 ) ) = 16
		//_TessMin( "Tess Min Distance", Float ) = 10
		//_TessMax( "Tess Max Distance", Float ) = 25
		//_TessEdgeLength ( "Tess Edge length", Range( 2, 50 ) ) = 16
		//_TessMaxDisp( "Tess Max Displacement", Float ) = 25

		[HideInInspector] _QueueOffset("_QueueOffset", Float) = 0
        [HideInInspector] _QueueControl("_QueueControl", Float) = -1

        [HideInInspector][NoScaleOffset] unity_Lightmaps("unity_Lightmaps", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset] unity_LightmapsInd("unity_LightmapsInd", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset] unity_ShadowMasks("unity_ShadowMasks", 2DArray) = "" {}

		[HideInInspector][ToggleUI] _ReceiveShadows("Receive Shadows", Float) = 1.0

		[HideInInspector][ToggleUI] _AddPrecomputedVelocity("Add Precomputed Velocity", Float) = 1
		[HideInInspector] _XRMotionVectorsPass("_XRMotionVectorsPass", Float) = 1

		//[HideInInspector] _AlphaClip("__clip", Float) = 0.0
	}

	SubShader
	{
		PackageRequirements
		{
			"com.unity.render-pipelines.universal": "[17.0,18.0]"
		}

		

		

		Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" "UniversalMaterialType"="Unlit" }

	LOD 0

		ZWrite [_ZWrite1]
		Cull [_Cull1]
		AlphaToMask Off
		ColorMask RGB
		Blend [_Src1] [_Dst1], One Zero
		BlendOp Add, Add

		

		HLSLINCLUDE
		#pragma target 3.0
		#pragma prefer_hlslcc gles
		// ensure rendering platforms toggle list is visible

		#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
		#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Filtering.hlsl"

		#define ASE_ADJUST_CLIP_POSITION( x ) x

		#ifndef ASE_TESS_FUNCS
		#define ASE_TESS_FUNCS
		float4 FixedTess( float tessValue )
		{
			return tessValue;
		}

		float CalcDistanceTessFactor (float4 vertex, float minDist, float maxDist, float tess, float4x4 o2w, float3 cameraPos )
		{
			float3 wpos = mul(o2w,vertex).xyz;
			float dist = distance (wpos, cameraPos);
			float f = clamp(1.0 - (dist - minDist) / (maxDist - minDist), 0.01, 1.0) * tess;
			return f;
		}

		float4 CalcTriEdgeTessFactors (float3 triVertexFactors)
		{
			float4 tess;
			tess.x = 0.5 * (triVertexFactors.y + triVertexFactors.z);
			tess.y = 0.5 * (triVertexFactors.x + triVertexFactors.z);
			tess.z = 0.5 * (triVertexFactors.x + triVertexFactors.y);
			tess.w = (triVertexFactors.x + triVertexFactors.y + triVertexFactors.z) / 3.0f;
			return tess;
		}

		float CalcEdgeTessFactor (float3 wpos0, float3 wpos1, float edgeLen, float3 cameraPos, float4 scParams )
		{
			float dist = distance (0.5 * (wpos0+wpos1), cameraPos);
			float len = distance(wpos0, wpos1);
			float f = max(len * scParams.y / (edgeLen * dist), 1.0);
			return f;
		}

		float DistanceFromPlane (float3 pos, float4 plane)
		{
			float d = dot (float4(pos,1.0f), plane);
			return d;
		}

		bool WorldViewFrustumCull (float3 wpos0, float3 wpos1, float3 wpos2, float cullEps, float4 planes[6] )
		{
			float4 planeTest;
			planeTest.x = (( DistanceFromPlane(wpos0, planes[0]) > -cullEps) ? 1.0f : 0.0f ) +
							(( DistanceFromPlane(wpos1, planes[0]) > -cullEps) ? 1.0f : 0.0f ) +
							(( DistanceFromPlane(wpos2, planes[0]) > -cullEps) ? 1.0f : 0.0f );
			planeTest.y = (( DistanceFromPlane(wpos0, planes[1]) > -cullEps) ? 1.0f : 0.0f ) +
							(( DistanceFromPlane(wpos1, planes[1]) > -cullEps) ? 1.0f : 0.0f ) +
							(( DistanceFromPlane(wpos2, planes[1]) > -cullEps) ? 1.0f : 0.0f );
			planeTest.z = (( DistanceFromPlane(wpos0, planes[2]) > -cullEps) ? 1.0f : 0.0f ) +
							(( DistanceFromPlane(wpos1, planes[2]) > -cullEps) ? 1.0f : 0.0f ) +
							(( DistanceFromPlane(wpos2, planes[2]) > -cullEps) ? 1.0f : 0.0f );
			planeTest.w = (( DistanceFromPlane(wpos0, planes[3]) > -cullEps) ? 1.0f : 0.0f ) +
							(( DistanceFromPlane(wpos1, planes[3]) > -cullEps) ? 1.0f : 0.0f ) +
							(( DistanceFromPlane(wpos2, planes[3]) > -cullEps) ? 1.0f : 0.0f );
			return !all (planeTest);
		}

		float4 DistanceBasedTess( float4 v0, float4 v1, float4 v2, float tess, float minDist, float maxDist, float4x4 o2w, float3 cameraPos )
		{
			float3 f;
			f.x = CalcDistanceTessFactor (v0,minDist,maxDist,tess,o2w,cameraPos);
			f.y = CalcDistanceTessFactor (v1,minDist,maxDist,tess,o2w,cameraPos);
			f.z = CalcDistanceTessFactor (v2,minDist,maxDist,tess,o2w,cameraPos);

			return CalcTriEdgeTessFactors (f);
		}

		float4 EdgeLengthBasedTess( float4 v0, float4 v1, float4 v2, float edgeLength, float4x4 o2w, float3 cameraPos, float4 scParams )
		{
			float3 pos0 = mul(o2w,v0).xyz;
			float3 pos1 = mul(o2w,v1).xyz;
			float3 pos2 = mul(o2w,v2).xyz;
			float4 tess;
			tess.x = CalcEdgeTessFactor (pos1, pos2, edgeLength, cameraPos, scParams);
			tess.y = CalcEdgeTessFactor (pos2, pos0, edgeLength, cameraPos, scParams);
			tess.z = CalcEdgeTessFactor (pos0, pos1, edgeLength, cameraPos, scParams);
			tess.w = (tess.x + tess.y + tess.z) / 3.0f;
			return tess;
		}

		float4 EdgeLengthBasedTessCull( float4 v0, float4 v1, float4 v2, float edgeLength, float maxDisplacement, float4x4 o2w, float3 cameraPos, float4 scParams, float4 planes[6] )
		{
			float3 pos0 = mul(o2w,v0).xyz;
			float3 pos1 = mul(o2w,v1).xyz;
			float3 pos2 = mul(o2w,v2).xyz;
			float4 tess;

			if (WorldViewFrustumCull(pos0, pos1, pos2, maxDisplacement, planes))
			{
				tess = 0.0f;
			}
			else
			{
				tess.x = CalcEdgeTessFactor (pos1, pos2, edgeLength, cameraPos, scParams);
				tess.y = CalcEdgeTessFactor (pos2, pos0, edgeLength, cameraPos, scParams);
				tess.z = CalcEdgeTessFactor (pos0, pos1, edgeLength, cameraPos, scParams);
				tess.w = (tess.x + tess.y + tess.z) / 3.0f;
			}
			return tess;
		}
		#endif //ASE_TESS_FUNCS
		ENDHLSL

		
		Pass
		{
			
			Name "Forward"
			Tags { "LightMode"="UniversalForwardOnly" }

			ZWrite [_ZWrite1]
			ZTest [_ZTest1]
			Offset 0 , 0
			ColorMask RGB
			Blend [_Src1] [_Dst1], One OneMinusSrcAlpha
			BlendOp Add, Add

			

			HLSLPROGRAM

			#define _SURFACE_TYPE_TRANSPARENT 1
			#define _NORMAL_DROPOFF_TS 1
			#pragma shader_feature_local_fragment _RECEIVE_SHADOWS_OFF
			#pragma multi_compile_instancing
			#pragma instancing_options renderinglayer
			#define ASE_VERSION 19912
			#define ASE_SRP_VERSION 170003


			#pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3

			#pragma multi_compile_fragment _ DEBUG_DISPLAY

			#pragma vertex vert
			#pragma fragment frag

			// Option "Keep Lighting Variants" @david please keep this note for future changes in Unlit  lighting
			//#define UNLIT_REALTIME_LIGHTING 1
			//#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
			//#pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
			//#pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
			//#pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
			//#pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
			//#pragma multi_compile_fragment _ _REFLECTION_PROBE_ATLAS
			//#pragma multi_compile _ REFLECTION_PROBE_ROTATION
			//#pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
			//#pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
			//#pragma multi_compile _ SHADOWS_SHADOWMASK
			//#pragma multi_compile_fragment _ _LIGHT_LAYERS
			//#pragma multi_compile_fragment _ _LIGHT_COOKIES
			//#pragma multi_compile _ _CLUSTER_LIGHT_LOOP

			#define SHADERPASS SHADERPASS_UNLIT

			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
			#if ( UNITY_VERSION >= 60010000 )
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"
			#else
			#pragma multi_compile_fog
			#endif
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
			#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Debug/Debugging3D.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceData.hlsl"

			#if defined(LOD_FADE_CROSSFADE)
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
            #endif

			#include "HLSL/Vefects_Radial_Zoom_Blur_URP.hlsl"
			#define ASE_NEEDS_FRAG_SCREEN_POSITION_NORMALIZED
			#define ASE_NEEDS_TEXTURE_COORDINATES1
			#define ASE_NEEDS_FRAG_TEXTURE_COORDINATES1
			#define ASE_NEEDS_TEXTURE_COORDINATES0
			#define ASE_NEEDS_FRAG_TEXTURE_COORDINATES0
			#define ASE_NEEDS_FRAG_COLOR


			#if defined(ASE_WRITE_DEPTH_CONSERVATIVE) && (SHADER_TARGET >= 45)
				#define ASE_SV_DEPTH SV_DepthLessEqual
				#define ASE_SV_POSITION_QUALIFIERS linear noperspective centroid
			#else
				#define ASE_SV_DEPTH SV_Depth
				#define ASE_SV_POSITION_QUALIFIERS
			#endif

			struct Attributes
			{
				float4 positionOS : POSITION;
				half3 normalOS : NORMAL;
				half4 tangentOS : TANGENT;
				float4 ase_texcoord1 : TEXCOORD1;
				float4 ase_texcoord : TEXCOORD0;
				float4 ase_color : COLOR;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct PackedVaryings
			{
				ASE_SV_POSITION_QUALIFIERS float4 positionCS : SV_POSITION;
				float4 positionWSAndFogFactor : TEXCOORD0;
				half3 normalWS : TEXCOORD1;
				half4 tangentWS : TEXCOORD2;
				float4 ase_texcoord3 : TEXCOORD3;
				float4 ase_texcoord4 : TEXCOORD4;
				float4 ase_color : COLOR;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			CBUFFER_START(UnityPerMaterial)
			float _ZTest1;
			float _Opacity;
			float _NoiseLineDarkness;
			float _Samples;
			float _NoiseAmount;
			float _UseRadialNoise;
			float _NoiseInnerRadius;
			float _NoiseContrast;
			float _EdgeSoftness;
			float _NoiseScrollSpeed;
			float _NoiseAngularTiling;
			float _UseCustomStrength;
			float _Strength;
			float _Cull1;
			float _Dst1;
			float _Src1;
			float _ZWrite1;
			float _NoiseRadialTiling;
			float _NoiseBaseOpacity;
			float _AlphaClip;
			float _Cutoff;
			#ifdef ASE_TESSELLATION
				float _TessPhongStrength;
				float _TessValue;
				float _TessMin;
				float _TessMax;
				float _TessEdgeLength;
				float _TessMaxDisp;
			#endif
			CBUFFER_END

			sampler2D _RadialNoiseTex;


			float ParticleStrength125( float Strength, float CustomStrength, float UseCustom )
			{
				return Strength * lerp(1.0, CustomStrength, saturate(UseCustom));
			}
			
			float2 Vefects_RadialNoiseUV140( float2 ParticleUV, float AngularTiling, float RadialTiling, float Time, float ScrollSpeed )
			{
				float2 p = (ParticleUV - 0.5) * 2.0;
				float angle = atan2(p.y, p.x) * 0.1591549431 + 0.5;
				return float2(angle * max(1.0, floor(AngularTiling + 0.5)), length(p) * RadialTiling + Time * ScrollSpeed);
			}
			
			float Vefects_RadialNoiseMask142( float2 ParticleUV, float Noise, float Contrast, float InnerRadius, float UseNoise, float Amount )
			{
				float r = length((ParticleUV - 0.5) * 2.0);
				float n = saturate((Noise - 0.5) * Contrast + 0.5);
				float inner = (InnerRadius > 0.0001) ? smoothstep(InnerRadius, min(1.0, InnerRadius + 0.2), r) : 1.0;
				return lerp(1.0, n * inner, saturate(UseNoise) * saturate(Amount));
			}
			
			float Vefects_NoiseBlurStrength143( float Strength, float NoiseMask )
			{
				return Strength * lerp(0.25, 1.0, saturate(NoiseMask));
			}
			
			float4 Vefects_RadialNoiseColor145( float4 Blur, float UseNoise, float Amount, float Darkness )
			{
				float darken = saturate(UseNoise) * saturate(Amount) * saturate(Darkness);
				return float4(Blur.rgb * (1.0 - darken), Blur.a);
			}
			
			float ParticleMask127( float2 ParticleUV, float VertexAlpha, float Opacity, float4 Blur, float EdgeSoftness )
			{
				float r = length((ParticleUV - 0.5) * 2.0);
				float mask = 1.0 - smoothstep(1.0 - clamp(EdgeSoftness, 0.001, 1.0), 1.0, r);
				return saturate(mask * VertexAlpha * Opacity * Blur.a);
			}
			
			float Vefects_NoiseOpacityCoverage147( float NoiseMask, float BaseOpacity )
			{
				return lerp(saturate(BaseOpacity), 1.0, saturate(NoiseMask));
			}
			

			PackedVaryings VertexFunction( Attributes input  )
			{
				PackedVaryings output = (PackedVaryings)0;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

				output.ase_texcoord3 = input.ase_texcoord1;
				output.ase_texcoord4.xy = input.ase_texcoord.xy;
				output.ase_color = input.ase_color;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				output.ase_texcoord4.zw = 0;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = input.positionOS.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif

				float3 vertexValue = defaultVertexValue;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					input.positionOS.xyz = vertexValue;
				#else
					input.positionOS.xyz += vertexValue;
				#endif

				input.normalOS = input.normalOS;
				input.tangentOS = input.tangentOS;

				#ifdef ASE_CUSTOM_MOTION_VECTOR
					// Declared so the Motion Vector output port surfaces on the master node; only consumed by the motion vector passes.
					float3 aseCustomMotionVector = float3(0, 0, 0);
				#endif

				VertexPositionInputs vertexInput = GetVertexPositionInputs( input.positionOS.xyz );
				VertexNormalInputs normalInput = GetVertexNormalInputs( input.normalOS, input.tangentOS );

				float fogFactor = 0;
				#if defined(ASE_FOG) && !defined(_FOG_FRAGMENT)
					fogFactor = ComputeFogFactor(vertexInput.positionCS.z);
				#endif

				output.positionCS = ASE_ADJUST_CLIP_POSITION( vertexInput.positionCS );
				output.positionWSAndFogFactor = float4( vertexInput.positionWS, fogFactor );
				output.normalWS = normalInput.normalWS;
				output.tangentWS = half4( normalInput.tangentWS, ( input.tangentOS.w > 0.0 ? 1.0 : -1.0 ) * GetOddNegativeScale() );;
				return output;
			}

			#if defined(ASE_TESSELLATION)
			struct VertexControl
			{
				float4 positionOS : INTERNALTESSPOS;
				half3 normalOS : NORMAL;
				half4 tangentOS : TANGENT;
				float4 ase_texcoord1 : TEXCOORD1;
				float4 ase_texcoord : TEXCOORD0;
				float4 ase_color : COLOR;

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct TessellationFactors
			{
				float edge[3] : SV_TessFactor;
				float inside : SV_InsideTessFactor;
			};

			VertexControl vert ( Attributes input )
			{
				VertexControl output;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				output.positionOS = input.positionOS;
				output.normalOS = input.normalOS;
				output.tangentOS = input.tangentOS;
				output.ase_texcoord1 = input.ase_texcoord1;
				output.ase_texcoord = input.ase_texcoord;
				output.ase_color = input.ase_color;
				return output;
			}

			TessellationFactors TessellationFunction (InputPatch<VertexControl,3> input)
			{
				TessellationFactors output;
				float4 tf = 1;
				float tessValue = _TessValue; float tessMin = _TessMin; float tessMax = _TessMax;
				float edgeLength = _TessEdgeLength; float tessMaxDisp = _TessMaxDisp;
				#if defined(ASE_FIXED_TESSELLATION)
				tf = FixedTess( tessValue );
				#elif defined(ASE_DISTANCE_TESSELLATION)
				tf = DistanceBasedTess(input[0].positionOS, input[1].positionOS, input[2].positionOS, tessValue, tessMin, tessMax, GetObjectToWorldMatrix(), _WorldSpaceCameraPos );
				#elif defined(ASE_LENGTH_TESSELLATION)
				tf = EdgeLengthBasedTess(input[0].positionOS, input[1].positionOS, input[2].positionOS, edgeLength, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams );
				#elif defined(ASE_LENGTH_CULL_TESSELLATION)
				tf = EdgeLengthBasedTessCull(input[0].positionOS, input[1].positionOS, input[2].positionOS, edgeLength, tessMaxDisp, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams, unity_CameraWorldClipPlanes );
				#endif
				output.edge[0] = tf.x; output.edge[1] = tf.y; output.edge[2] = tf.z; output.inside = tf.w;
				return output;
			}

			[domain("tri")]
			[partitioning("fractional_odd")]
			[outputtopology("triangle_cw")]
			[patchconstantfunc("TessellationFunction")]
			[outputcontrolpoints(3)]
			VertexControl HullFunction(InputPatch<VertexControl, 3> patch, uint id : SV_OutputControlPointID)
			{
				return patch[id];
			}

			[domain("tri")]
			PackedVaryings DomainFunction(TessellationFactors factors, OutputPatch<VertexControl, 3> patch, float3 bary : SV_DomainLocation)
			{
				Attributes output = (Attributes) 0;
				output.positionOS = patch[0].positionOS * bary.x + patch[1].positionOS * bary.y + patch[2].positionOS * bary.z;
				output.normalOS = patch[0].normalOS * bary.x + patch[1].normalOS * bary.y + patch[2].normalOS * bary.z;
				output.tangentOS = patch[0].tangentOS * bary.x + patch[1].tangentOS * bary.y + patch[2].tangentOS * bary.z;
				output.ase_texcoord1 = patch[0].ase_texcoord1 * bary.x + patch[1].ase_texcoord1 * bary.y + patch[2].ase_texcoord1 * bary.z;
				output.ase_texcoord = patch[0].ase_texcoord * bary.x + patch[1].ase_texcoord * bary.y + patch[2].ase_texcoord * bary.z;
				output.ase_color = patch[0].ase_color * bary.x + patch[1].ase_color * bary.y + patch[2].ase_color * bary.z;
				#if defined(ASE_PHONG_TESSELLATION)
				float3 pp[3];
				for (int i = 0; i < 3; ++i)
					pp[i] = output.positionOS.xyz - patch[i].normalOS * (dot(output.positionOS.xyz, patch[i].normalOS) - dot(patch[i].positionOS.xyz, patch[i].normalOS));
				float phongStrength = _TessPhongStrength;
				output.positionOS.xyz = phongStrength * (pp[0]*bary.x + pp[1]*bary.y + pp[2]*bary.z) + (1.0f-phongStrength) * output.positionOS.xyz;
				#endif
				UNITY_TRANSFER_INSTANCE_ID(patch[0], output);
				return VertexFunction(output);
			}
			#else
			PackedVaryings vert ( Attributes input )
			{
				return VertexFunction( input );
			}
			#endif

			half4 frag ( PackedVaryings input
						#if defined( ASE_WRITE_DEPTH )
						,out float outputDepth : ASE_SV_DEPTH
						#endif
						#ifdef _WRITE_RENDERING_LAYERS
						#if ( UNITY_VERSION >= 60020000 )
						, out uint outRenderingLayers : SV_Target1
						#else
						, out float4 outRenderingLayers : SV_Target1
						#endif
						#endif
						 ) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

				#if defined( _SURFACE_TYPE_TRANSPARENT )
					const bool isTransparent = true;
				#else
					const bool isTransparent = false;
				#endif

				#if defined(LOD_FADE_CROSSFADE)
					LODFadeCrossFade( input.positionCS );
				#endif

				#if defined(MAIN_LIGHT_CALCULATE_SHADOWS)
					float4 shadowCoord = TransformWorldToShadowCoord( input.positionWSAndFogFactor.xyz );
				#else
					float4 shadowCoord = float4(0, 0, 0, 0);
				#endif

				// @diogo: mikktspace compliant
				float renormFactor = 1.0 / max( FLT_MIN, length( input.normalWS ) );

				float3 PositionWS = input.positionWSAndFogFactor.xyz;
				float3 PositionRWS = GetCameraRelativePositionWS( PositionWS );
				half3 ViewDirWS = GetWorldSpaceNormalizeViewDir( PositionWS );
				float4 ShadowCoord = shadowCoord;
				float4 ScreenPosNorm = float4( GetNormalizedScreenSpaceUV( input.positionCS ), input.positionCS.zw );
				float4 ClipPos = ComputeClipSpacePosition( ScreenPosNorm.xy, input.positionCS.z ) * input.positionCS.w;
				float4 ScreenPos = ComputeScreenPos( ClipPos );
				float3 TangentWS = input.tangentWS.xyz * renormFactor;
				float3 BitangentWS = cross( input.normalWS, input.tangentWS.xyz ) * input.tangentWS.w * renormFactor;
				float3 NormalWS = input.normalWS * renormFactor;

				float2 ScreenUV126 = ScreenPosNorm.xy;
				float3 CenterWS126 = input.ase_texcoord3.xyz;
				float Strength125 = _Strength;
				float CustomStrength125 = input.ase_texcoord3.w;
				float UseCustom125 = _UseCustomStrength;
				float localParticleStrength125 = ParticleStrength125( Strength125 , CustomStrength125 , UseCustom125 );
				float Strength143 = localParticleStrength125;
				float2 VTC_UV159 = input.ase_texcoord4.xy;
				float2 ParticleUV142 = VTC_UV159;
				float2 ParticleUV140 = VTC_UV159;
				float AngularTiling140 = _NoiseAngularTiling;
				float RadialTiling140 = _NoiseRadialTiling;
				float Time140 = _TimeParameters.x;
				float ScrollSpeed140 = _NoiseScrollSpeed;
				float2 localVefects_RadialNoiseUV140 = Vefects_RadialNoiseUV140( ParticleUV140 , AngularTiling140 , RadialTiling140 , Time140 , ScrollSpeed140 );
				float Noise142 = tex2D( _RadialNoiseTex, localVefects_RadialNoiseUV140 ).r;
				float Contrast142 = _NoiseContrast;
				float InnerRadius142 = _NoiseInnerRadius;
				float useRN149 = _UseRadialNoise;
				float UseNoise142 = useRN149;
				float noiseAm148 = _NoiseAmount;
				float Amount142 = noiseAm148;
				float localVefects_RadialNoiseMask142 = Vefects_RadialNoiseMask142( ParticleUV142 , Noise142 , Contrast142 , InnerRadius142 , UseNoise142 , Amount142 );
				float noiseMask163 = localVefects_RadialNoiseMask142;
				float NoiseMask143 = noiseMask163;
				float localVefects_NoiseBlurStrength143 = Vefects_NoiseBlurStrength143( Strength143 , NoiseMask143 );
				float Strength126 = localVefects_NoiseBlurStrength143;
				float Samples126 = _Samples;
				float4 localVefects_Radial_Zoom_Blur_URP126 = Vefects_Radial_Zoom_Blur_URP( ScreenUV126 , CenterWS126 , Strength126 , Samples126 );
				float4 Blur145 = ( localVefects_Radial_Zoom_Blur_URP126 * float4( (input.ase_color).rgb , 0.0 ) );
				float UseNoise145 = useRN149;
				float Amount145 = noiseAm148;
				float Darkness145 = _NoiseLineDarkness;
				float4 localVefects_RadialNoiseColor145 = Vefects_RadialNoiseColor145( Blur145 , UseNoise145 , Amount145 , Darkness145 );
				float4 outCOL154 = localVefects_RadialNoiseColor145;
				
				float2 ParticleUV127 = VTC_UV159;
				float VertexAlpha127 = input.ase_color.a;
				float Opacity127 = _Opacity;
				float4 Blur127 = localVefects_Radial_Zoom_Blur_URP126;
				float EdgeSoftness127 = _EdgeSoftness;
				float localParticleMask127 = ParticleMask127( ParticleUV127 , VertexAlpha127 , Opacity127 , Blur127 , EdgeSoftness127 );
				float NoiseMask147 = noiseMask163;
				float BaseOpacity147 = _NoiseBaseOpacity;
				float localVefects_NoiseOpacityCoverage147 = Vefects_NoiseOpacityCoverage147( NoiseMask147 , BaseOpacity147 );
				float outOP156 = saturate( ( localParticleMask127 * localVefects_NoiseOpacityCoverage147 ) );
				
				float3 BakedAlbedo = 0;
				float3 BakedEmission = 0;
				float3 Color = outCOL154.xyz;
				float3 Normal = float3(0, 0, 1);
				float Alpha = outOP156;
				#if defined( _ALPHATEST_ON )
					float AlphaClipThreshold = _Cutoff;
					float AlphaClipThresholdShadow = 0.5;
				#endif


				#if defined( ASE_WRITE_DEPTH )
					input.positionCS.z = input.positionCS.z;
				#endif

				#if defined( _ALPHATEST_ON )
					AlphaDiscard( Alpha, AlphaClipThreshold );
				#endif

				#if defined(MAIN_LIGHT_CALCULATE_SHADOWS) && defined(ASE_CHANGES_WORLD_POS)
					ShadowCoord = TransformWorldToShadowCoord( PositionWS );
				#endif

				InputData inputData = (InputData)0;
				inputData.positionWS = PositionWS;
				inputData.positionCS = input.positionCS;
				inputData.normalizedScreenSpaceUV = ScreenPosNorm.xy;
				inputData.normalWS = NormalWS;
				inputData.viewDirectionWS = ViewDirWS;

				#if defined(_SCREEN_SPACE_OCCLUSION) && !defined(_SURFACE_TYPE_TRANSPARENT) && defined(UNLIT_DEFAULT_SSAO)
					float2 normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
					AmbientOcclusionFactor aoFactor = GetScreenSpaceAmbientOcclusion(normalizedScreenSpaceUV);
					Color.rgb *= aoFactor.directAmbientOcclusion;
				#endif

				#ifdef ASE_FOG
					inputData.fogCoord = InitializeInputDataFog(float4(inputData.positionWS, 1.0), input.positionWSAndFogFactor.w);
				#endif

				#if defined(_DBUFFER) && defined(UNLIT_DEFAULT_DECAL_BLENDING)
					ApplyDecalToBaseColor(input.positionCS, Color);
				#endif

				#ifdef ASE_FOG
					#ifdef TERRAIN_SPLAT_ADDPASS
						Color.rgb = MixFogColor(Color.rgb, half3(0,0,0), inputData.fogCoord);
					#else
						Color.rgb = MixFog(Color.rgb, inputData.fogCoord);
					#endif
				#endif

				#if defined( ASE_WRITE_DEPTH )
					outputDepth = input.positionCS.z;
				#endif

				#ifdef _WRITE_RENDERING_LAYERS
					#if ( UNITY_VERSION >= 60020000 )
					outRenderingLayers = EncodeMeshRenderingLayer();
					#else
					uint renderingLayers = GetMeshRenderingLayer();
					outRenderingLayers = float4( EncodeMeshRenderingLayer( renderingLayers ), 0, 0, 0 );
					#endif
				#endif

				#if defined( ASE_OPAQUE_KEEP_ALPHA )
					return half4( Color, Alpha );
				#else
					return half4( Color, OutputAlpha( Alpha, isTransparent ) );
				#endif
			}
			ENDHLSL
		}

		
		Pass
		{
			
			Name "DepthOnly"
			Tags { "LightMode"="DepthOnly" }

			ZWrite On
			ColorMask 0
			AlphaToMask Off

			HLSLPROGRAM

			#define _SURFACE_TYPE_TRANSPARENT 1
			#define _NORMAL_DROPOFF_TS 1
			#pragma multi_compile_instancing
			#define ASE_VERSION 19912
			#define ASE_SRP_VERSION 170003


			#pragma vertex vert
			#pragma fragment frag

			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

			#if defined(LOD_FADE_CROSSFADE)
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
            #endif

			#include "HLSL/Vefects_Radial_Zoom_Blur_URP.hlsl"
			#define ASE_NEEDS_TEXTURE_COORDINATES0
			#define ASE_NEEDS_FRAG_TEXTURE_COORDINATES0
			#define ASE_NEEDS_FRAG_SCREEN_POSITION_NORMALIZED
			#define ASE_NEEDS_TEXTURE_COORDINATES1
			#define ASE_NEEDS_FRAG_TEXTURE_COORDINATES1


			#if defined(ASE_WRITE_DEPTH_CONSERVATIVE) && (SHADER_TARGET >= 45)
				#define ASE_SV_DEPTH SV_DepthLessEqual
				#define ASE_SV_POSITION_QUALIFIERS linear noperspective centroid
			#else
				#define ASE_SV_DEPTH SV_Depth
				#define ASE_SV_POSITION_QUALIFIERS
			#endif

			struct Attributes
			{
				float4 positionOS : POSITION;
				half3 normalOS : NORMAL;
				half4 tangentOS : TANGENT;
				float4 ase_texcoord : TEXCOORD0;
				float4 ase_color : COLOR;
				float4 ase_texcoord1 : TEXCOORD1;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct PackedVaryings
			{
				ASE_SV_POSITION_QUALIFIERS float4 positionCS : SV_POSITION;
				float4 ase_texcoord : TEXCOORD0;
				float4 ase_color : COLOR;
				float4 ase_texcoord1 : TEXCOORD1;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			CBUFFER_START(UnityPerMaterial)
			float _ZTest1;
			float _Opacity;
			float _NoiseLineDarkness;
			float _Samples;
			float _NoiseAmount;
			float _UseRadialNoise;
			float _NoiseInnerRadius;
			float _NoiseContrast;
			float _EdgeSoftness;
			float _NoiseScrollSpeed;
			float _NoiseAngularTiling;
			float _UseCustomStrength;
			float _Strength;
			float _Cull1;
			float _Dst1;
			float _Src1;
			float _ZWrite1;
			float _NoiseRadialTiling;
			float _NoiseBaseOpacity;
			float _AlphaClip;
			float _Cutoff;
			#ifdef ASE_TESSELLATION
				float _TessPhongStrength;
				float _TessValue;
				float _TessMin;
				float _TessMax;
				float _TessEdgeLength;
				float _TessMaxDisp;
			#endif
			CBUFFER_END

			sampler2D _RadialNoiseTex;


			float3 _LightDirection;
			float3 _LightPosition;

			float ParticleStrength125( float Strength, float CustomStrength, float UseCustom )
			{
				return Strength * lerp(1.0, CustomStrength, saturate(UseCustom));
			}
			
			float2 Vefects_RadialNoiseUV140( float2 ParticleUV, float AngularTiling, float RadialTiling, float Time, float ScrollSpeed )
			{
				float2 p = (ParticleUV - 0.5) * 2.0;
				float angle = atan2(p.y, p.x) * 0.1591549431 + 0.5;
				return float2(angle * max(1.0, floor(AngularTiling + 0.5)), length(p) * RadialTiling + Time * ScrollSpeed);
			}
			
			float Vefects_RadialNoiseMask142( float2 ParticleUV, float Noise, float Contrast, float InnerRadius, float UseNoise, float Amount )
			{
				float r = length((ParticleUV - 0.5) * 2.0);
				float n = saturate((Noise - 0.5) * Contrast + 0.5);
				float inner = (InnerRadius > 0.0001) ? smoothstep(InnerRadius, min(1.0, InnerRadius + 0.2), r) : 1.0;
				return lerp(1.0, n * inner, saturate(UseNoise) * saturate(Amount));
			}
			
			float Vefects_NoiseBlurStrength143( float Strength, float NoiseMask )
			{
				return Strength * lerp(0.25, 1.0, saturate(NoiseMask));
			}
			
			float ParticleMask127( float2 ParticleUV, float VertexAlpha, float Opacity, float4 Blur, float EdgeSoftness )
			{
				float r = length((ParticleUV - 0.5) * 2.0);
				float mask = 1.0 - smoothstep(1.0 - clamp(EdgeSoftness, 0.001, 1.0), 1.0, r);
				return saturate(mask * VertexAlpha * Opacity * Blur.a);
			}
			
			float Vefects_NoiseOpacityCoverage147( float NoiseMask, float BaseOpacity )
			{
				return lerp(saturate(BaseOpacity), 1.0, saturate(NoiseMask));
			}
			

			PackedVaryings VertexFunction( Attributes input  )
			{
				PackedVaryings output = (PackedVaryings)0;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

				output.ase_texcoord.xy = input.ase_texcoord.xy;
				output.ase_color = input.ase_color;
				output.ase_texcoord1 = input.ase_texcoord1;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				output.ase_texcoord.zw = 0;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = input.positionOS.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif

				float3 vertexValue = defaultVertexValue;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					input.positionOS.xyz = vertexValue;
				#else
					input.positionOS.xyz += vertexValue;
				#endif

				VertexPositionInputs vertexInput = GetVertexPositionInputs( input.positionOS.xyz );

				output.positionCS = ASE_ADJUST_CLIP_POSITION( vertexInput.positionCS );
				return output;
			}

			#if defined(ASE_TESSELLATION)
			struct VertexControl
			{
				float4 positionOS : INTERNALTESSPOS;
				half3 normalOS : NORMAL;
				float4 ase_texcoord : TEXCOORD0;
				float4 ase_color : COLOR;
				float4 ase_texcoord1 : TEXCOORD1;

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct TessellationFactors
			{
				float edge[3] : SV_TessFactor;
				float inside : SV_InsideTessFactor;
			};

			VertexControl vert ( Attributes input )
			{
				VertexControl output;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				output.positionOS = input.positionOS;
				output.normalOS = input.normalOS;
				output.ase_texcoord = input.ase_texcoord;
				output.ase_color = input.ase_color;
				output.ase_texcoord1 = input.ase_texcoord1;
				return output;
			}

			TessellationFactors TessellationFunction (InputPatch<VertexControl,3> input)
			{
				TessellationFactors output;
				float4 tf = 1;
				float tessValue = _TessValue; float tessMin = _TessMin; float tessMax = _TessMax;
				float edgeLength = _TessEdgeLength; float tessMaxDisp = _TessMaxDisp;
				#if defined(ASE_FIXED_TESSELLATION)
				tf = FixedTess( tessValue );
				#elif defined(ASE_DISTANCE_TESSELLATION)
				tf = DistanceBasedTess(input[0].positionOS, input[1].positionOS, input[2].positionOS, tessValue, tessMin, tessMax, GetObjectToWorldMatrix(), _WorldSpaceCameraPos );
				#elif defined(ASE_LENGTH_TESSELLATION)
				tf = EdgeLengthBasedTess(input[0].positionOS, input[1].positionOS, input[2].positionOS, edgeLength, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams );
				#elif defined(ASE_LENGTH_CULL_TESSELLATION)
				tf = EdgeLengthBasedTessCull(input[0].positionOS, input[1].positionOS, input[2].positionOS, edgeLength, tessMaxDisp, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams, unity_CameraWorldClipPlanes );
				#endif
				output.edge[0] = tf.x; output.edge[1] = tf.y; output.edge[2] = tf.z; output.inside = tf.w;
				return output;
			}

			[domain("tri")]
			[partitioning("fractional_odd")]
			[outputtopology("triangle_cw")]
			[patchconstantfunc("TessellationFunction")]
			[outputcontrolpoints(3)]
			VertexControl HullFunction(InputPatch<VertexControl, 3> patch, uint id : SV_OutputControlPointID)
			{
				return patch[id];
			}

			[domain("tri")]
			PackedVaryings DomainFunction(TessellationFactors factors, OutputPatch<VertexControl, 3> patch, float3 bary : SV_DomainLocation)
			{
				Attributes output = (Attributes) 0;
				output.positionOS = patch[0].positionOS * bary.x + patch[1].positionOS * bary.y + patch[2].positionOS * bary.z;
				output.normalOS = patch[0].normalOS * bary.x + patch[1].normalOS * bary.y + patch[2].normalOS * bary.z;
				output.ase_texcoord = patch[0].ase_texcoord * bary.x + patch[1].ase_texcoord * bary.y + patch[2].ase_texcoord * bary.z;
				output.ase_color = patch[0].ase_color * bary.x + patch[1].ase_color * bary.y + patch[2].ase_color * bary.z;
				output.ase_texcoord1 = patch[0].ase_texcoord1 * bary.x + patch[1].ase_texcoord1 * bary.y + patch[2].ase_texcoord1 * bary.z;
				#if defined(ASE_PHONG_TESSELLATION)
				float3 pp[3];
				for (int i = 0; i < 3; ++i)
					pp[i] = output.positionOS.xyz - patch[i].normalOS * (dot(output.positionOS.xyz, patch[i].normalOS) - dot(patch[i].positionOS.xyz, patch[i].normalOS));
				float phongStrength = _TessPhongStrength;
				output.positionOS.xyz = phongStrength * (pp[0]*bary.x + pp[1]*bary.y + pp[2]*bary.z) + (1.0f-phongStrength) * output.positionOS.xyz;
				#endif
				UNITY_TRANSFER_INSTANCE_ID(patch[0], output);
				return VertexFunction(output);
			}
			#else
			PackedVaryings vert ( Attributes input )
			{
				return VertexFunction( input );
			}
			#endif

			half4 frag(PackedVaryings input
						#if defined( ASE_WRITE_DEPTH )
						,out float outputDepth : ASE_SV_DEPTH
						#endif
						 ) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( input );

				float4 ScreenPosNorm = float4( GetNormalizedScreenSpaceUV( input.positionCS ), input.positionCS.zw );
				float4 ClipPos = ComputeClipSpacePosition( ScreenPosNorm.xy, input.positionCS.z ) * input.positionCS.w;
				float4 ScreenPos = ComputeScreenPos( ClipPos );

				float2 VTC_UV159 = input.ase_texcoord.xy;
				float2 ParticleUV127 = VTC_UV159;
				float VertexAlpha127 = input.ase_color.a;
				float Opacity127 = _Opacity;
				float2 ScreenUV126 = ScreenPosNorm.xy;
				float3 CenterWS126 = input.ase_texcoord1.xyz;
				float Strength125 = _Strength;
				float CustomStrength125 = input.ase_texcoord1.w;
				float UseCustom125 = _UseCustomStrength;
				float localParticleStrength125 = ParticleStrength125( Strength125 , CustomStrength125 , UseCustom125 );
				float Strength143 = localParticleStrength125;
				float2 ParticleUV142 = VTC_UV159;
				float2 ParticleUV140 = VTC_UV159;
				float AngularTiling140 = _NoiseAngularTiling;
				float RadialTiling140 = _NoiseRadialTiling;
				float Time140 = _TimeParameters.x;
				float ScrollSpeed140 = _NoiseScrollSpeed;
				float2 localVefects_RadialNoiseUV140 = Vefects_RadialNoiseUV140( ParticleUV140 , AngularTiling140 , RadialTiling140 , Time140 , ScrollSpeed140 );
				float Noise142 = tex2D( _RadialNoiseTex, localVefects_RadialNoiseUV140 ).r;
				float Contrast142 = _NoiseContrast;
				float InnerRadius142 = _NoiseInnerRadius;
				float useRN149 = _UseRadialNoise;
				float UseNoise142 = useRN149;
				float noiseAm148 = _NoiseAmount;
				float Amount142 = noiseAm148;
				float localVefects_RadialNoiseMask142 = Vefects_RadialNoiseMask142( ParticleUV142 , Noise142 , Contrast142 , InnerRadius142 , UseNoise142 , Amount142 );
				float noiseMask163 = localVefects_RadialNoiseMask142;
				float NoiseMask143 = noiseMask163;
				float localVefects_NoiseBlurStrength143 = Vefects_NoiseBlurStrength143( Strength143 , NoiseMask143 );
				float Strength126 = localVefects_NoiseBlurStrength143;
				float Samples126 = _Samples;
				float4 localVefects_Radial_Zoom_Blur_URP126 = Vefects_Radial_Zoom_Blur_URP( ScreenUV126 , CenterWS126 , Strength126 , Samples126 );
				float4 Blur127 = localVefects_Radial_Zoom_Blur_URP126;
				float EdgeSoftness127 = _EdgeSoftness;
				float localParticleMask127 = ParticleMask127( ParticleUV127 , VertexAlpha127 , Opacity127 , Blur127 , EdgeSoftness127 );
				float NoiseMask147 = noiseMask163;
				float BaseOpacity147 = _NoiseBaseOpacity;
				float localVefects_NoiseOpacityCoverage147 = Vefects_NoiseOpacityCoverage147( NoiseMask147 , BaseOpacity147 );
				float outOP156 = saturate( ( localParticleMask127 * localVefects_NoiseOpacityCoverage147 ) );
				

				float Alpha = outOP156;
				#if defined( _ALPHATEST_ON )
					float AlphaClipThreshold = _Cutoff;
				#endif

				#if defined( ASE_WRITE_DEPTH )
					input.positionCS.z = input.positionCS.z;
				#endif

				#if defined( _ALPHATEST_ON )
					AlphaDiscard( Alpha, AlphaClipThreshold );
				#endif

				#if defined(LOD_FADE_CROSSFADE)
					LODFadeCrossFade( input.positionCS );
				#endif

				#if defined( ASE_WRITE_DEPTH )
					outputDepth = input.positionCS.z;
				#endif

				return 0;
			}
			ENDHLSL
		}

		
		Pass
		{
			
			Name "SceneSelectionPass"
			Tags { "LightMode"="SceneSelectionPass" }

			Cull Off
			AlphaToMask Off

			HLSLPROGRAM

			#define _SURFACE_TYPE_TRANSPARENT 1
			#define _NORMAL_DROPOFF_TS 1
			#define ASE_VERSION 19912
			#define ASE_SRP_VERSION 170003


			#pragma vertex vert
			#pragma fragment frag

			#define ATTRIBUTES_NEED_NORMAL
			#define ATTRIBUTES_NEED_TANGENT
			#define SHADERPASS SHADERPASS_DEPTHONLY

			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

			#include "HLSL/Vefects_Radial_Zoom_Blur_URP.hlsl"
			#define ASE_NEEDS_TEXTURE_COORDINATES0
			#define ASE_NEEDS_FRAG_TEXTURE_COORDINATES0
			#define ASE_NEEDS_TEXTURE_COORDINATES1
			#define ASE_NEEDS_FRAG_TEXTURE_COORDINATES1


			struct Attributes
			{
				float4 positionOS : POSITION;
				half3 normalOS : NORMAL;
				half4 tangentOS : TANGENT;
				float4 ase_texcoord : TEXCOORD0;
				float4 ase_color : COLOR;
				float4 ase_texcoord1 : TEXCOORD1;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct PackedVaryings
			{
				float4 positionCS : SV_POSITION;
				float4 ase_texcoord : TEXCOORD0;
				float4 ase_color : COLOR;
				float4 ase_texcoord1 : TEXCOORD1;
				float4 ase_texcoord2 : TEXCOORD2;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			CBUFFER_START(UnityPerMaterial)
			float _ZTest1;
			float _Opacity;
			float _NoiseLineDarkness;
			float _Samples;
			float _NoiseAmount;
			float _UseRadialNoise;
			float _NoiseInnerRadius;
			float _NoiseContrast;
			float _EdgeSoftness;
			float _NoiseScrollSpeed;
			float _NoiseAngularTiling;
			float _UseCustomStrength;
			float _Strength;
			float _Cull1;
			float _Dst1;
			float _Src1;
			float _ZWrite1;
			float _NoiseRadialTiling;
			float _NoiseBaseOpacity;
			float _AlphaClip;
			float _Cutoff;
			#ifdef ASE_TESSELLATION
				float _TessPhongStrength;
				float _TessValue;
				float _TessMin;
				float _TessMax;
				float _TessEdgeLength;
				float _TessMaxDisp;
			#endif
			CBUFFER_END

			sampler2D _RadialNoiseTex;


			float ParticleStrength125( float Strength, float CustomStrength, float UseCustom )
			{
				return Strength * lerp(1.0, CustomStrength, saturate(UseCustom));
			}
			
			float2 Vefects_RadialNoiseUV140( float2 ParticleUV, float AngularTiling, float RadialTiling, float Time, float ScrollSpeed )
			{
				float2 p = (ParticleUV - 0.5) * 2.0;
				float angle = atan2(p.y, p.x) * 0.1591549431 + 0.5;
				return float2(angle * max(1.0, floor(AngularTiling + 0.5)), length(p) * RadialTiling + Time * ScrollSpeed);
			}
			
			float Vefects_RadialNoiseMask142( float2 ParticleUV, float Noise, float Contrast, float InnerRadius, float UseNoise, float Amount )
			{
				float r = length((ParticleUV - 0.5) * 2.0);
				float n = saturate((Noise - 0.5) * Contrast + 0.5);
				float inner = (InnerRadius > 0.0001) ? smoothstep(InnerRadius, min(1.0, InnerRadius + 0.2), r) : 1.0;
				return lerp(1.0, n * inner, saturate(UseNoise) * saturate(Amount));
			}
			
			float Vefects_NoiseBlurStrength143( float Strength, float NoiseMask )
			{
				return Strength * lerp(0.25, 1.0, saturate(NoiseMask));
			}
			
			float ParticleMask127( float2 ParticleUV, float VertexAlpha, float Opacity, float4 Blur, float EdgeSoftness )
			{
				float r = length((ParticleUV - 0.5) * 2.0);
				float mask = 1.0 - smoothstep(1.0 - clamp(EdgeSoftness, 0.001, 1.0), 1.0, r);
				return saturate(mask * VertexAlpha * Opacity * Blur.a);
			}
			
			float Vefects_NoiseOpacityCoverage147( float NoiseMask, float BaseOpacity )
			{
				return lerp(saturate(BaseOpacity), 1.0, saturate(NoiseMask));
			}
			

			int _ObjectId;
			int _PassValue;

			struct SurfaceDescription
			{
				float Alpha;
				float AlphaClipThreshold;
			};

			PackedVaryings VertexFunction(Attributes input  )
			{
				PackedVaryings output;
				ZERO_INITIALIZE(PackedVaryings, output);

				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

				float4 ase_positionCS = TransformObjectToHClip( ( input.positionOS ).xyz );
				float4 screenPos = ComputeScreenPos( ase_positionCS );
				output.ase_texcoord1 = screenPos;
				
				output.ase_texcoord.xy = input.ase_texcoord.xy;
				output.ase_color = input.ase_color;
				output.ase_texcoord2 = input.ase_texcoord1;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				output.ase_texcoord.zw = 0;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = input.positionOS.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif

				float3 vertexValue = defaultVertexValue;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					input.positionOS.xyz = vertexValue;
				#else
					input.positionOS.xyz += vertexValue;
				#endif

				VertexPositionInputs vertexInput = GetVertexPositionInputs( input.positionOS.xyz );

				output.positionCS = ASE_ADJUST_CLIP_POSITION( vertexInput.positionCS );
				return output;
			}

			#if defined(ASE_TESSELLATION)
			struct VertexControl
			{
				float4 positionOS : INTERNALTESSPOS;
				half3 normalOS : NORMAL;
				float4 ase_texcoord : TEXCOORD0;
				float4 ase_color : COLOR;
				float4 ase_texcoord1 : TEXCOORD1;

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct TessellationFactors
			{
				float edge[3] : SV_TessFactor;
				float inside : SV_InsideTessFactor;
			};

			VertexControl vert ( Attributes input )
			{
				VertexControl output;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				output.positionOS = input.positionOS;
				output.normalOS = input.normalOS;
				output.ase_texcoord = input.ase_texcoord;
				output.ase_color = input.ase_color;
				output.ase_texcoord1 = input.ase_texcoord1;
				return output;
			}

			TessellationFactors TessellationFunction (InputPatch<VertexControl,3> input)
			{
				TessellationFactors output;
				float4 tf = 1;
				float tessValue = _TessValue; float tessMin = _TessMin; float tessMax = _TessMax;
				float edgeLength = _TessEdgeLength; float tessMaxDisp = _TessMaxDisp;
				#if defined(ASE_FIXED_TESSELLATION)
				tf = FixedTess( tessValue );
				#elif defined(ASE_DISTANCE_TESSELLATION)
				tf = DistanceBasedTess(input[0].positionOS, input[1].positionOS, input[2].positionOS, tessValue, tessMin, tessMax, GetObjectToWorldMatrix(), _WorldSpaceCameraPos );
				#elif defined(ASE_LENGTH_TESSELLATION)
				tf = EdgeLengthBasedTess(input[0].positionOS, input[1].positionOS, input[2].positionOS, edgeLength, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams );
				#elif defined(ASE_LENGTH_CULL_TESSELLATION)
				tf = EdgeLengthBasedTessCull(input[0].positionOS, input[1].positionOS, input[2].positionOS, edgeLength, tessMaxDisp, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams, unity_CameraWorldClipPlanes );
				#endif
				output.edge[0] = tf.x; output.edge[1] = tf.y; output.edge[2] = tf.z; output.inside = tf.w;
				return output;
			}

			[domain("tri")]
			[partitioning("fractional_odd")]
			[outputtopology("triangle_cw")]
			[patchconstantfunc("TessellationFunction")]
			[outputcontrolpoints(3)]
			VertexControl HullFunction(InputPatch<VertexControl, 3> patch, uint id : SV_OutputControlPointID)
			{
				return patch[id];
			}

			[domain("tri")]
			PackedVaryings DomainFunction(TessellationFactors factors, OutputPatch<VertexControl, 3> patch, float3 bary : SV_DomainLocation)
			{
				Attributes output = (Attributes) 0;
				output.positionOS = patch[0].positionOS * bary.x + patch[1].positionOS * bary.y + patch[2].positionOS * bary.z;
				output.normalOS = patch[0].normalOS * bary.x + patch[1].normalOS * bary.y + patch[2].normalOS * bary.z;
				output.ase_texcoord = patch[0].ase_texcoord * bary.x + patch[1].ase_texcoord * bary.y + patch[2].ase_texcoord * bary.z;
				output.ase_color = patch[0].ase_color * bary.x + patch[1].ase_color * bary.y + patch[2].ase_color * bary.z;
				output.ase_texcoord1 = patch[0].ase_texcoord1 * bary.x + patch[1].ase_texcoord1 * bary.y + patch[2].ase_texcoord1 * bary.z;
				#if defined(ASE_PHONG_TESSELLATION)
				float3 pp[3];
				for (int i = 0; i < 3; ++i)
					pp[i] = output.positionOS.xyz - patch[i].normalOS * (dot(output.positionOS.xyz, patch[i].normalOS) - dot(patch[i].positionOS.xyz, patch[i].normalOS));
				float phongStrength = _TessPhongStrength;
				output.positionOS.xyz = phongStrength * (pp[0]*bary.x + pp[1]*bary.y + pp[2]*bary.z) + (1.0f-phongStrength) * output.positionOS.xyz;
				#endif
				UNITY_TRANSFER_INSTANCE_ID(patch[0], output);
				return VertexFunction(output);
			}
			#else
			PackedVaryings vert ( Attributes input )
			{
				return VertexFunction( input );
			}
			#endif

			half4 frag(PackedVaryings input ) : SV_Target
			{
				SurfaceDescription surfaceDescription = (SurfaceDescription)0;

				float2 VTC_UV159 = input.ase_texcoord.xy;
				float2 ParticleUV127 = VTC_UV159;
				float VertexAlpha127 = input.ase_color.a;
				float Opacity127 = _Opacity;
				float4 screenPos = input.ase_texcoord1;
				float4 ase_positionSSNorm = screenPos / screenPos.w;
				ase_positionSSNorm.z = ( UNITY_NEAR_CLIP_VALUE >= 0 ) ? ase_positionSSNorm.z : ase_positionSSNorm.z * 0.5 + 0.5;
				float2 ScreenUV126 = ase_positionSSNorm.xy;
				float3 CenterWS126 = input.ase_texcoord2.xyz;
				float Strength125 = _Strength;
				float CustomStrength125 = input.ase_texcoord2.w;
				float UseCustom125 = _UseCustomStrength;
				float localParticleStrength125 = ParticleStrength125( Strength125 , CustomStrength125 , UseCustom125 );
				float Strength143 = localParticleStrength125;
				float2 ParticleUV142 = VTC_UV159;
				float2 ParticleUV140 = VTC_UV159;
				float AngularTiling140 = _NoiseAngularTiling;
				float RadialTiling140 = _NoiseRadialTiling;
				float Time140 = _TimeParameters.x;
				float ScrollSpeed140 = _NoiseScrollSpeed;
				float2 localVefects_RadialNoiseUV140 = Vefects_RadialNoiseUV140( ParticleUV140 , AngularTiling140 , RadialTiling140 , Time140 , ScrollSpeed140 );
				float Noise142 = tex2D( _RadialNoiseTex, localVefects_RadialNoiseUV140 ).r;
				float Contrast142 = _NoiseContrast;
				float InnerRadius142 = _NoiseInnerRadius;
				float useRN149 = _UseRadialNoise;
				float UseNoise142 = useRN149;
				float noiseAm148 = _NoiseAmount;
				float Amount142 = noiseAm148;
				float localVefects_RadialNoiseMask142 = Vefects_RadialNoiseMask142( ParticleUV142 , Noise142 , Contrast142 , InnerRadius142 , UseNoise142 , Amount142 );
				float noiseMask163 = localVefects_RadialNoiseMask142;
				float NoiseMask143 = noiseMask163;
				float localVefects_NoiseBlurStrength143 = Vefects_NoiseBlurStrength143( Strength143 , NoiseMask143 );
				float Strength126 = localVefects_NoiseBlurStrength143;
				float Samples126 = _Samples;
				float4 localVefects_Radial_Zoom_Blur_URP126 = Vefects_Radial_Zoom_Blur_URP( ScreenUV126 , CenterWS126 , Strength126 , Samples126 );
				float4 Blur127 = localVefects_Radial_Zoom_Blur_URP126;
				float EdgeSoftness127 = _EdgeSoftness;
				float localParticleMask127 = ParticleMask127( ParticleUV127 , VertexAlpha127 , Opacity127 , Blur127 , EdgeSoftness127 );
				float NoiseMask147 = noiseMask163;
				float BaseOpacity147 = _NoiseBaseOpacity;
				float localVefects_NoiseOpacityCoverage147 = Vefects_NoiseOpacityCoverage147( NoiseMask147 , BaseOpacity147 );
				float outOP156 = saturate( ( localParticleMask127 * localVefects_NoiseOpacityCoverage147 ) );
				

				surfaceDescription.Alpha = outOP156;
				#if defined( _ALPHATEST_ON )
					surfaceDescription.AlphaClipThreshold = _Cutoff;
				#endif

				#ifdef _ALPHATEST_ON
					clip(surfaceDescription.Alpha - surfaceDescription.AlphaClipThreshold);
				#endif

				half4 outColor = half4(_ObjectId, _PassValue, 1.0, 1.0);
				return outColor;
			}
			ENDHLSL
		}

		
		Pass
		{
			
			Name "ScenePickingPass"
			Tags { "LightMode"="Picking" }

			AlphaToMask Off

			HLSLPROGRAM

			#define _SURFACE_TYPE_TRANSPARENT 1
			#define _NORMAL_DROPOFF_TS 1
			#define ASE_VERSION 19912
			#define ASE_SRP_VERSION 170003


			#pragma vertex vert
			#pragma fragment frag

			#define ATTRIBUTES_NEED_NORMAL
			#define ATTRIBUTES_NEED_TANGENT

			#define SHADERPASS SHADERPASS_DEPTHONLY

			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

			#if defined(LOD_FADE_CROSSFADE)
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
            #endif

			#include "HLSL/Vefects_Radial_Zoom_Blur_URP.hlsl"
			#define ASE_NEEDS_TEXTURE_COORDINATES0
			#define ASE_NEEDS_FRAG_TEXTURE_COORDINATES0
			#define ASE_NEEDS_TEXTURE_COORDINATES1
			#define ASE_NEEDS_FRAG_TEXTURE_COORDINATES1


			struct Attributes
			{
				float4 positionOS : POSITION;
				half3 normalOS : NORMAL;
				half4 tangentOS : TANGENT;
				float4 ase_texcoord : TEXCOORD0;
				float4 ase_color : COLOR;
				float4 ase_texcoord1 : TEXCOORD1;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct PackedVaryings
			{
				float4 positionCS : SV_POSITION;
				float4 ase_texcoord : TEXCOORD0;
				float4 ase_color : COLOR;
				float4 ase_texcoord1 : TEXCOORD1;
				float4 ase_texcoord2 : TEXCOORD2;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			CBUFFER_START(UnityPerMaterial)
			float _ZTest1;
			float _Opacity;
			float _NoiseLineDarkness;
			float _Samples;
			float _NoiseAmount;
			float _UseRadialNoise;
			float _NoiseInnerRadius;
			float _NoiseContrast;
			float _EdgeSoftness;
			float _NoiseScrollSpeed;
			float _NoiseAngularTiling;
			float _UseCustomStrength;
			float _Strength;
			float _Cull1;
			float _Dst1;
			float _Src1;
			float _ZWrite1;
			float _NoiseRadialTiling;
			float _NoiseBaseOpacity;
			float _AlphaClip;
			float _Cutoff;
			#ifdef ASE_TESSELLATION
				float _TessPhongStrength;
				float _TessValue;
				float _TessMin;
				float _TessMax;
				float _TessEdgeLength;
				float _TessMaxDisp;
			#endif
			CBUFFER_END

			sampler2D _RadialNoiseTex;


			float ParticleStrength125( float Strength, float CustomStrength, float UseCustom )
			{
				return Strength * lerp(1.0, CustomStrength, saturate(UseCustom));
			}
			
			float2 Vefects_RadialNoiseUV140( float2 ParticleUV, float AngularTiling, float RadialTiling, float Time, float ScrollSpeed )
			{
				float2 p = (ParticleUV - 0.5) * 2.0;
				float angle = atan2(p.y, p.x) * 0.1591549431 + 0.5;
				return float2(angle * max(1.0, floor(AngularTiling + 0.5)), length(p) * RadialTiling + Time * ScrollSpeed);
			}
			
			float Vefects_RadialNoiseMask142( float2 ParticleUV, float Noise, float Contrast, float InnerRadius, float UseNoise, float Amount )
			{
				float r = length((ParticleUV - 0.5) * 2.0);
				float n = saturate((Noise - 0.5) * Contrast + 0.5);
				float inner = (InnerRadius > 0.0001) ? smoothstep(InnerRadius, min(1.0, InnerRadius + 0.2), r) : 1.0;
				return lerp(1.0, n * inner, saturate(UseNoise) * saturate(Amount));
			}
			
			float Vefects_NoiseBlurStrength143( float Strength, float NoiseMask )
			{
				return Strength * lerp(0.25, 1.0, saturate(NoiseMask));
			}
			
			float ParticleMask127( float2 ParticleUV, float VertexAlpha, float Opacity, float4 Blur, float EdgeSoftness )
			{
				float r = length((ParticleUV - 0.5) * 2.0);
				float mask = 1.0 - smoothstep(1.0 - clamp(EdgeSoftness, 0.001, 1.0), 1.0, r);
				return saturate(mask * VertexAlpha * Opacity * Blur.a);
			}
			
			float Vefects_NoiseOpacityCoverage147( float NoiseMask, float BaseOpacity )
			{
				return lerp(saturate(BaseOpacity), 1.0, saturate(NoiseMask));
			}
			

			float4 _SelectionID;

			struct SurfaceDescription
			{
				float Alpha;
				float AlphaClipThreshold;
			};

			PackedVaryings VertexFunction(Attributes input  )
			{
				PackedVaryings output;
				ZERO_INITIALIZE(PackedVaryings, output);

				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

				float4 ase_positionCS = TransformObjectToHClip( ( input.positionOS ).xyz );
				float4 screenPos = ComputeScreenPos( ase_positionCS );
				output.ase_texcoord1 = screenPos;
				
				output.ase_texcoord.xy = input.ase_texcoord.xy;
				output.ase_color = input.ase_color;
				output.ase_texcoord2 = input.ase_texcoord1;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				output.ase_texcoord.zw = 0;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = input.positionOS.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif

				float3 vertexValue = defaultVertexValue;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					input.positionOS.xyz = vertexValue;
				#else
					input.positionOS.xyz += vertexValue;
				#endif

				VertexPositionInputs vertexInput = GetVertexPositionInputs( input.positionOS.xyz );

				output.positionCS = ASE_ADJUST_CLIP_POSITION( vertexInput.positionCS );
				return output;
			}

			#if defined(ASE_TESSELLATION)
			struct VertexControl
			{
				float4 positionOS : INTERNALTESSPOS;
				half3 normalOS : NORMAL;
				float4 ase_texcoord : TEXCOORD0;
				float4 ase_color : COLOR;
				float4 ase_texcoord1 : TEXCOORD1;

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct TessellationFactors
			{
				float edge[3] : SV_TessFactor;
				float inside : SV_InsideTessFactor;
			};

			VertexControl vert ( Attributes input )
			{
				VertexControl output;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				output.positionOS = input.positionOS;
				output.normalOS = input.normalOS;
				output.ase_texcoord = input.ase_texcoord;
				output.ase_color = input.ase_color;
				output.ase_texcoord1 = input.ase_texcoord1;
				return output;
			}

			TessellationFactors TessellationFunction (InputPatch<VertexControl,3> input)
			{
				TessellationFactors output;
				float4 tf = 1;
				float tessValue = _TessValue; float tessMin = _TessMin; float tessMax = _TessMax;
				float edgeLength = _TessEdgeLength; float tessMaxDisp = _TessMaxDisp;
				#if defined(ASE_FIXED_TESSELLATION)
				tf = FixedTess( tessValue );
				#elif defined(ASE_DISTANCE_TESSELLATION)
				tf = DistanceBasedTess(input[0].positionOS, input[1].positionOS, input[2].positionOS, tessValue, tessMin, tessMax, GetObjectToWorldMatrix(), _WorldSpaceCameraPos );
				#elif defined(ASE_LENGTH_TESSELLATION)
				tf = EdgeLengthBasedTess(input[0].positionOS, input[1].positionOS, input[2].positionOS, edgeLength, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams );
				#elif defined(ASE_LENGTH_CULL_TESSELLATION)
				tf = EdgeLengthBasedTessCull(input[0].positionOS, input[1].positionOS, input[2].positionOS, edgeLength, tessMaxDisp, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams, unity_CameraWorldClipPlanes );
				#endif
				output.edge[0] = tf.x; output.edge[1] = tf.y; output.edge[2] = tf.z; output.inside = tf.w;
				return output;
			}

			[domain("tri")]
			[partitioning("fractional_odd")]
			[outputtopology("triangle_cw")]
			[patchconstantfunc("TessellationFunction")]
			[outputcontrolpoints(3)]
			VertexControl HullFunction(InputPatch<VertexControl, 3> patch, uint id : SV_OutputControlPointID)
			{
				return patch[id];
			}

			[domain("tri")]
			PackedVaryings DomainFunction(TessellationFactors factors, OutputPatch<VertexControl, 3> patch, float3 bary : SV_DomainLocation)
			{
				Attributes output = (Attributes) 0;
				output.positionOS = patch[0].positionOS * bary.x + patch[1].positionOS * bary.y + patch[2].positionOS * bary.z;
				output.normalOS = patch[0].normalOS * bary.x + patch[1].normalOS * bary.y + patch[2].normalOS * bary.z;
				output.ase_texcoord = patch[0].ase_texcoord * bary.x + patch[1].ase_texcoord * bary.y + patch[2].ase_texcoord * bary.z;
				output.ase_color = patch[0].ase_color * bary.x + patch[1].ase_color * bary.y + patch[2].ase_color * bary.z;
				output.ase_texcoord1 = patch[0].ase_texcoord1 * bary.x + patch[1].ase_texcoord1 * bary.y + patch[2].ase_texcoord1 * bary.z;
				#if defined(ASE_PHONG_TESSELLATION)
				float3 pp[3];
				for (int i = 0; i < 3; ++i)
					pp[i] = output.positionOS.xyz - patch[i].normalOS * (dot(output.positionOS.xyz, patch[i].normalOS) - dot(patch[i].positionOS.xyz, patch[i].normalOS));
				float phongStrength = _TessPhongStrength;
				output.positionOS.xyz = phongStrength * (pp[0]*bary.x + pp[1]*bary.y + pp[2]*bary.z) + (1.0f-phongStrength) * output.positionOS.xyz;
				#endif
				UNITY_TRANSFER_INSTANCE_ID(patch[0], output);
				return VertexFunction(output);
			}
			#else
			PackedVaryings vert ( Attributes input )
			{
				return VertexFunction( input );
			}
			#endif

			half4 frag(PackedVaryings input ) : SV_Target
			{
				SurfaceDescription surfaceDescription = (SurfaceDescription)0;

				float2 VTC_UV159 = input.ase_texcoord.xy;
				float2 ParticleUV127 = VTC_UV159;
				float VertexAlpha127 = input.ase_color.a;
				float Opacity127 = _Opacity;
				float4 screenPos = input.ase_texcoord1;
				float4 ase_positionSSNorm = screenPos / screenPos.w;
				ase_positionSSNorm.z = ( UNITY_NEAR_CLIP_VALUE >= 0 ) ? ase_positionSSNorm.z : ase_positionSSNorm.z * 0.5 + 0.5;
				float2 ScreenUV126 = ase_positionSSNorm.xy;
				float3 CenterWS126 = input.ase_texcoord2.xyz;
				float Strength125 = _Strength;
				float CustomStrength125 = input.ase_texcoord2.w;
				float UseCustom125 = _UseCustomStrength;
				float localParticleStrength125 = ParticleStrength125( Strength125 , CustomStrength125 , UseCustom125 );
				float Strength143 = localParticleStrength125;
				float2 ParticleUV142 = VTC_UV159;
				float2 ParticleUV140 = VTC_UV159;
				float AngularTiling140 = _NoiseAngularTiling;
				float RadialTiling140 = _NoiseRadialTiling;
				float Time140 = _TimeParameters.x;
				float ScrollSpeed140 = _NoiseScrollSpeed;
				float2 localVefects_RadialNoiseUV140 = Vefects_RadialNoiseUV140( ParticleUV140 , AngularTiling140 , RadialTiling140 , Time140 , ScrollSpeed140 );
				float Noise142 = tex2D( _RadialNoiseTex, localVefects_RadialNoiseUV140 ).r;
				float Contrast142 = _NoiseContrast;
				float InnerRadius142 = _NoiseInnerRadius;
				float useRN149 = _UseRadialNoise;
				float UseNoise142 = useRN149;
				float noiseAm148 = _NoiseAmount;
				float Amount142 = noiseAm148;
				float localVefects_RadialNoiseMask142 = Vefects_RadialNoiseMask142( ParticleUV142 , Noise142 , Contrast142 , InnerRadius142 , UseNoise142 , Amount142 );
				float noiseMask163 = localVefects_RadialNoiseMask142;
				float NoiseMask143 = noiseMask163;
				float localVefects_NoiseBlurStrength143 = Vefects_NoiseBlurStrength143( Strength143 , NoiseMask143 );
				float Strength126 = localVefects_NoiseBlurStrength143;
				float Samples126 = _Samples;
				float4 localVefects_Radial_Zoom_Blur_URP126 = Vefects_Radial_Zoom_Blur_URP( ScreenUV126 , CenterWS126 , Strength126 , Samples126 );
				float4 Blur127 = localVefects_Radial_Zoom_Blur_URP126;
				float EdgeSoftness127 = _EdgeSoftness;
				float localParticleMask127 = ParticleMask127( ParticleUV127 , VertexAlpha127 , Opacity127 , Blur127 , EdgeSoftness127 );
				float NoiseMask147 = noiseMask163;
				float BaseOpacity147 = _NoiseBaseOpacity;
				float localVefects_NoiseOpacityCoverage147 = Vefects_NoiseOpacityCoverage147( NoiseMask147 , BaseOpacity147 );
				float outOP156 = saturate( ( localParticleMask127 * localVefects_NoiseOpacityCoverage147 ) );
				

				surfaceDescription.Alpha = outOP156;
				#if defined( _ALPHATEST_ON )
					surfaceDescription.AlphaClipThreshold = _Cutoff;
				#endif

				#ifdef _ALPHATEST_ON
					clip(surfaceDescription.Alpha - surfaceDescription.AlphaClipThreshold);
				#endif

				half4 outColor = 0;
				outColor = unity_SelectionID;

				return outColor;
			}

			ENDHLSL
		}

		
		Pass
		{
			
			Name "DepthNormals"
			Tags { "LightMode"="DepthNormalsOnly" }

			ZTest LEqual
			ZWrite On

			HLSLPROGRAM

        	#define _SURFACE_TYPE_TRANSPARENT 1
        	#define _NORMAL_DROPOFF_TS 1
        	#pragma multi_compile_instancing
        	#define ASE_VERSION 19912
        	#define ASE_SRP_VERSION 170003


        	#pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT

			#pragma vertex vert
			#pragma fragment frag

			#define ATTRIBUTES_NEED_NORMAL
			#define ATTRIBUTES_NEED_TANGENT
			#define VARYINGS_NEED_NORMAL_WS
			#define VARYINGS_NEED_TANGENT_WS

			#define SHADERPASS SHADERPASS_DEPTHNORMALSONLY

			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

            #if defined(LOD_FADE_CROSSFADE)
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
            #endif

			#include "HLSL/Vefects_Radial_Zoom_Blur_URP.hlsl"
			#define ASE_NEEDS_TEXTURE_COORDINATES0
			#define ASE_NEEDS_FRAG_TEXTURE_COORDINATES0
			#define ASE_NEEDS_FRAG_SCREEN_POSITION_NORMALIZED
			#define ASE_NEEDS_TEXTURE_COORDINATES1
			#define ASE_NEEDS_FRAG_TEXTURE_COORDINATES1


			#if defined(ASE_WRITE_DEPTH_CONSERVATIVE) && (SHADER_TARGET >= 45)
				#define ASE_SV_DEPTH SV_DepthLessEqual
				#define ASE_SV_POSITION_QUALIFIERS linear noperspective centroid
			#else
				#define ASE_SV_DEPTH SV_Depth
				#define ASE_SV_POSITION_QUALIFIERS
			#endif

			struct Attributes
			{
				float4 positionOS : POSITION;
				half3 normalOS : NORMAL;
				half4 tangentOS : TANGENT;
				float4 ase_texcoord : TEXCOORD0;
				float4 ase_color : COLOR;
				float4 ase_texcoord1 : TEXCOORD1;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct PackedVaryings
			{
				ASE_SV_POSITION_QUALIFIERS float4 positionCS : SV_POSITION;
				half3 normalWS : TEXCOORD0;
				float4 tangentWS : TEXCOORD1;
				float4 ase_texcoord2 : TEXCOORD2;
				float4 ase_color : COLOR;
				float4 ase_texcoord3 : TEXCOORD3;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			CBUFFER_START(UnityPerMaterial)
			float _ZTest1;
			float _Opacity;
			float _NoiseLineDarkness;
			float _Samples;
			float _NoiseAmount;
			float _UseRadialNoise;
			float _NoiseInnerRadius;
			float _NoiseContrast;
			float _EdgeSoftness;
			float _NoiseScrollSpeed;
			float _NoiseAngularTiling;
			float _UseCustomStrength;
			float _Strength;
			float _Cull1;
			float _Dst1;
			float _Src1;
			float _ZWrite1;
			float _NoiseRadialTiling;
			float _NoiseBaseOpacity;
			float _AlphaClip;
			float _Cutoff;
			#ifdef ASE_TESSELLATION
				float _TessPhongStrength;
				float _TessValue;
				float _TessMin;
				float _TessMax;
				float _TessEdgeLength;
				float _TessMaxDisp;
			#endif
			CBUFFER_END

			sampler2D _RadialNoiseTex;


			float ParticleStrength125( float Strength, float CustomStrength, float UseCustom )
			{
				return Strength * lerp(1.0, CustomStrength, saturate(UseCustom));
			}
			
			float2 Vefects_RadialNoiseUV140( float2 ParticleUV, float AngularTiling, float RadialTiling, float Time, float ScrollSpeed )
			{
				float2 p = (ParticleUV - 0.5) * 2.0;
				float angle = atan2(p.y, p.x) * 0.1591549431 + 0.5;
				return float2(angle * max(1.0, floor(AngularTiling + 0.5)), length(p) * RadialTiling + Time * ScrollSpeed);
			}
			
			float Vefects_RadialNoiseMask142( float2 ParticleUV, float Noise, float Contrast, float InnerRadius, float UseNoise, float Amount )
			{
				float r = length((ParticleUV - 0.5) * 2.0);
				float n = saturate((Noise - 0.5) * Contrast + 0.5);
				float inner = (InnerRadius > 0.0001) ? smoothstep(InnerRadius, min(1.0, InnerRadius + 0.2), r) : 1.0;
				return lerp(1.0, n * inner, saturate(UseNoise) * saturate(Amount));
			}
			
			float Vefects_NoiseBlurStrength143( float Strength, float NoiseMask )
			{
				return Strength * lerp(0.25, 1.0, saturate(NoiseMask));
			}
			
			float ParticleMask127( float2 ParticleUV, float VertexAlpha, float Opacity, float4 Blur, float EdgeSoftness )
			{
				float r = length((ParticleUV - 0.5) * 2.0);
				float mask = 1.0 - smoothstep(1.0 - clamp(EdgeSoftness, 0.001, 1.0), 1.0, r);
				return saturate(mask * VertexAlpha * Opacity * Blur.a);
			}
			
			float Vefects_NoiseOpacityCoverage147( float NoiseMask, float BaseOpacity )
			{
				return lerp(saturate(BaseOpacity), 1.0, saturate(NoiseMask));
			}
			

			struct SurfaceDescription
			{
				float Alpha;
				float AlphaClipThreshold;
			};

			PackedVaryings VertexFunction( Attributes input  )
			{
				PackedVaryings output;
				ZERO_INITIALIZE(PackedVaryings, output);

				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

				output.ase_texcoord2.xy = input.ase_texcoord.xy;
				output.ase_color = input.ase_color;
				output.ase_texcoord3 = input.ase_texcoord1;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				output.ase_texcoord2.zw = 0;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = input.positionOS.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif

				float3 vertexValue = defaultVertexValue;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					input.positionOS.xyz = vertexValue;
				#else
					input.positionOS.xyz += vertexValue;
				#endif

				input.normalOS = input.normalOS;
				input.tangentOS = input.tangentOS;

				VertexPositionInputs vertexInput = GetVertexPositionInputs( input.positionOS.xyz );
				VertexNormalInputs normalInput = GetVertexNormalInputs( input.normalOS, input.tangentOS );

				output.positionCS = ASE_ADJUST_CLIP_POSITION( vertexInput.positionCS );
				output.normalWS = normalInput.normalWS;
				output.tangentWS = float4( normalInput.tangentWS, ( input.tangentOS.w > 0.0 ? 1.0 : -1.0 ) * GetOddNegativeScale() );
				return output;
			}

			#if defined(ASE_TESSELLATION)
			struct VertexControl
			{
				float4 positionOS : INTERNALTESSPOS;
				half3 normalOS : NORMAL;
				half4 tangentOS : TANGENT;
				float4 ase_texcoord : TEXCOORD0;
				float4 ase_color : COLOR;
				float4 ase_texcoord1 : TEXCOORD1;

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct TessellationFactors
			{
				float edge[3] : SV_TessFactor;
				float inside : SV_InsideTessFactor;
			};

			VertexControl vert ( Attributes input )
			{
				VertexControl output;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				output.positionOS = input.positionOS;
				output.normalOS = input.normalOS;
				output.tangentOS = input.tangentOS;
				output.ase_texcoord = input.ase_texcoord;
				output.ase_color = input.ase_color;
				output.ase_texcoord1 = input.ase_texcoord1;
				return output;
			}

			TessellationFactors TessellationFunction (InputPatch<VertexControl,3> input)
			{
				TessellationFactors output;
				float4 tf = 1;
				float tessValue = _TessValue; float tessMin = _TessMin; float tessMax = _TessMax;
				float edgeLength = _TessEdgeLength; float tessMaxDisp = _TessMaxDisp;
				#if defined(ASE_FIXED_TESSELLATION)
				tf = FixedTess( tessValue );
				#elif defined(ASE_DISTANCE_TESSELLATION)
				tf = DistanceBasedTess(input[0].positionOS, input[1].positionOS, input[2].positionOS, tessValue, tessMin, tessMax, GetObjectToWorldMatrix(), _WorldSpaceCameraPos );
				#elif defined(ASE_LENGTH_TESSELLATION)
				tf = EdgeLengthBasedTess(input[0].positionOS, input[1].positionOS, input[2].positionOS, edgeLength, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams );
				#elif defined(ASE_LENGTH_CULL_TESSELLATION)
				tf = EdgeLengthBasedTessCull(input[0].positionOS, input[1].positionOS, input[2].positionOS, edgeLength, tessMaxDisp, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams, unity_CameraWorldClipPlanes );
				#endif
				output.edge[0] = tf.x; output.edge[1] = tf.y; output.edge[2] = tf.z; output.inside = tf.w;
				return output;
			}

			[domain("tri")]
			[partitioning("fractional_odd")]
			[outputtopology("triangle_cw")]
			[patchconstantfunc("TessellationFunction")]
			[outputcontrolpoints(3)]
			VertexControl HullFunction(InputPatch<VertexControl, 3> patch, uint id : SV_OutputControlPointID)
			{
				return patch[id];
			}

			[domain("tri")]
			PackedVaryings DomainFunction(TessellationFactors factors, OutputPatch<VertexControl, 3> patch, float3 bary : SV_DomainLocation)
			{
				Attributes output = (Attributes) 0;
				output.positionOS = patch[0].positionOS * bary.x + patch[1].positionOS * bary.y + patch[2].positionOS * bary.z;
				output.normalOS = patch[0].normalOS * bary.x + patch[1].normalOS * bary.y + patch[2].normalOS * bary.z;
				output.tangentOS = patch[0].tangentOS * bary.x + patch[1].tangentOS * bary.y + patch[2].tangentOS * bary.z;
				output.ase_texcoord = patch[0].ase_texcoord * bary.x + patch[1].ase_texcoord * bary.y + patch[2].ase_texcoord * bary.z;
				output.ase_color = patch[0].ase_color * bary.x + patch[1].ase_color * bary.y + patch[2].ase_color * bary.z;
				output.ase_texcoord1 = patch[0].ase_texcoord1 * bary.x + patch[1].ase_texcoord1 * bary.y + patch[2].ase_texcoord1 * bary.z;
				#if defined(ASE_PHONG_TESSELLATION)
				float3 pp[3];
				for (int i = 0; i < 3; ++i)
					pp[i] = output.positionOS.xyz - patch[i].normalOS * (dot(output.positionOS.xyz, patch[i].normalOS) - dot(patch[i].positionOS.xyz, patch[i].normalOS));
				float phongStrength = _TessPhongStrength;
				output.positionOS.xyz = phongStrength * (pp[0]*bary.x + pp[1]*bary.y + pp[2]*bary.z) + (1.0f-phongStrength) * output.positionOS.xyz;
				#endif
				UNITY_TRANSFER_INSTANCE_ID(patch[0], output);
				return VertexFunction(output);
			}
			#else
			PackedVaryings vert ( Attributes input )
			{
				return VertexFunction( input );
			}
			#endif

			void frag(PackedVaryings input
						, out half4 outNormalWS : SV_Target0
						#if defined( ASE_WRITE_DEPTH )
						,out float outputDepth : ASE_SV_DEPTH
						#endif
						#ifdef _WRITE_RENDERING_LAYERS
						#if ( UNITY_VERSION >= 60020000 )
						, out uint outRenderingLayers : SV_Target1
						#else
						, out float4 outRenderingLayers : SV_Target1
						#endif
						#endif
						 )
			{
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( input );

				// @diogo: mikktspace compliant
				float renormFactor = 1.0 / max( FLT_MIN, length( input.normalWS ) );

				float3 NormalWS = input.normalWS * renormFactor;
				float3 TangentWS = input.tangentWS.xyz * renormFactor;
				float3 BitangentWS = cross( input.normalWS, input.tangentWS.xyz ) * input.tangentWS.w * renormFactor;
				float4 ScreenPosNorm = float4( GetNormalizedScreenSpaceUV( input.positionCS ), input.positionCS.zw );
				float4 ClipPos = ComputeClipSpacePosition( ScreenPosNorm.xy, input.positionCS.z ) * input.positionCS.w;
				float4 ScreenPos = ComputeScreenPos( ClipPos );

				float2 VTC_UV159 = input.ase_texcoord2.xy;
				float2 ParticleUV127 = VTC_UV159;
				float VertexAlpha127 = input.ase_color.a;
				float Opacity127 = _Opacity;
				float2 ScreenUV126 = ScreenPosNorm.xy;
				float3 CenterWS126 = input.ase_texcoord3.xyz;
				float Strength125 = _Strength;
				float CustomStrength125 = input.ase_texcoord3.w;
				float UseCustom125 = _UseCustomStrength;
				float localParticleStrength125 = ParticleStrength125( Strength125 , CustomStrength125 , UseCustom125 );
				float Strength143 = localParticleStrength125;
				float2 ParticleUV142 = VTC_UV159;
				float2 ParticleUV140 = VTC_UV159;
				float AngularTiling140 = _NoiseAngularTiling;
				float RadialTiling140 = _NoiseRadialTiling;
				float Time140 = _TimeParameters.x;
				float ScrollSpeed140 = _NoiseScrollSpeed;
				float2 localVefects_RadialNoiseUV140 = Vefects_RadialNoiseUV140( ParticleUV140 , AngularTiling140 , RadialTiling140 , Time140 , ScrollSpeed140 );
				float Noise142 = tex2D( _RadialNoiseTex, localVefects_RadialNoiseUV140 ).r;
				float Contrast142 = _NoiseContrast;
				float InnerRadius142 = _NoiseInnerRadius;
				float useRN149 = _UseRadialNoise;
				float UseNoise142 = useRN149;
				float noiseAm148 = _NoiseAmount;
				float Amount142 = noiseAm148;
				float localVefects_RadialNoiseMask142 = Vefects_RadialNoiseMask142( ParticleUV142 , Noise142 , Contrast142 , InnerRadius142 , UseNoise142 , Amount142 );
				float noiseMask163 = localVefects_RadialNoiseMask142;
				float NoiseMask143 = noiseMask163;
				float localVefects_NoiseBlurStrength143 = Vefects_NoiseBlurStrength143( Strength143 , NoiseMask143 );
				float Strength126 = localVefects_NoiseBlurStrength143;
				float Samples126 = _Samples;
				float4 localVefects_Radial_Zoom_Blur_URP126 = Vefects_Radial_Zoom_Blur_URP( ScreenUV126 , CenterWS126 , Strength126 , Samples126 );
				float4 Blur127 = localVefects_Radial_Zoom_Blur_URP126;
				float EdgeSoftness127 = _EdgeSoftness;
				float localParticleMask127 = ParticleMask127( ParticleUV127 , VertexAlpha127 , Opacity127 , Blur127 , EdgeSoftness127 );
				float NoiseMask147 = noiseMask163;
				float BaseOpacity147 = _NoiseBaseOpacity;
				float localVefects_NoiseOpacityCoverage147 = Vefects_NoiseOpacityCoverage147( NoiseMask147 , BaseOpacity147 );
				float outOP156 = saturate( ( localParticleMask127 * localVefects_NoiseOpacityCoverage147 ) );
				

				float3 Normal = float3(0, 0, 1);
				float Alpha = outOP156;
				#if defined( _ALPHATEST_ON )
					float AlphaClipThreshold = _Cutoff;
				#endif

				#if defined( ASE_WRITE_DEPTH )
					input.positionCS.z = input.positionCS.z;
				#endif

				#ifdef _ALPHATEST_ON
					clip(Alpha - AlphaClipThreshold);
				#endif

				#if defined(LOD_FADE_CROSSFADE)
					LODFadeCrossFade( input.positionCS );
				#endif

				#if defined( ASE_WRITE_DEPTH )
					outputDepth = input.positionCS.z;
				#endif

				#if defined(_GBUFFER_NORMALS_OCT)
					float2 octNormalWS = PackNormalOctQuadEncode(NormalWS);
					float2 remappedOctNormalWS = saturate(octNormalWS * 0.5 + 0.5);
					half3 packedNormalWS = PackFloat2To888(remappedOctNormalWS);
					outNormalWS = half4(packedNormalWS, 0.0);
				#else
					#if defined(_NORMALMAP)
						#if _NORMAL_DROPOFF_TS
							float3 normalWS = TransformTangentToWorld(Normal, half3x3(TangentWS, BitangentWS, NormalWS));
						#elif _NORMAL_DROPOFF_OS
							float3 normalWS = TransformObjectToWorldNormal(Normal);
						#elif _NORMAL_DROPOFF_WS
							float3 normalWS = Normal;
						#endif
					#else
						float3 normalWS = NormalWS;
					#endif
					outNormalWS = half4(NormalizeNormalPerPixel(normalWS), 0.0);
				#endif

				#ifdef _WRITE_RENDERING_LAYERS
					#if ( UNITY_VERSION >= 60020000 )
					outRenderingLayers = EncodeMeshRenderingLayer();
					#else
					uint renderingLayers = GetMeshRenderingLayer();
					outRenderingLayers = float4( EncodeMeshRenderingLayer( renderingLayers ), 0, 0, 0 );
					#endif
				#endif
			}
			ENDHLSL
		}

		
		Pass
		{			
			
			Name "GBuffer"
			Tags { "LightMode"="UniversalGBuffer" }

			ZWrite [_ZWrite1]
			ZTest [_ZTest1]
			Offset 0 , 0
			ColorMask RGB

			

			HLSLPROGRAM

			#define _SURFACE_TYPE_TRANSPARENT 1
			#define _NORMAL_DROPOFF_TS 1
			#pragma shader_feature_local_fragment _RECEIVE_SHADOWS_OFF
			#define ASE_VERSION 19912
			#define ASE_SRP_VERSION 170003


			// Deferred Rendering Path does not support the OpenGL-based graphics API:
			// Desktop OpenGL, OpenGL ES 3.0, WebGL 2.0.
			#pragma exclude_renderers glcore gles3 

			#pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
			#pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
			#pragma multi_compile_fragment _ _RENDER_PASS_ENABLED

			#pragma vertex vert
			#pragma fragment frag

			#define SHADERPASS SHADERPASS_GBUFFER

			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ProbeVolumeVariants.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

			#if ( UNITY_VERSION >= 60030016 && UNITY_VERSION < 60040000 ) || ( UNITY_VERSION >= 60040010 )
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/GBufferOutputFormat.hlsl"
			#endif

			#if defined(LOD_FADE_CROSSFADE)
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
            #endif

			#if defined(UNITY_INSTANCING_ENABLED) && defined(_TERRAIN_INSTANCED_PERPIXEL_NORMAL)
				#define ENABLE_TERRAIN_PERPIXEL_NORMAL
			#endif

			#include "HLSL/Vefects_Radial_Zoom_Blur_URP.hlsl"
			#define ASE_NEEDS_FRAG_SCREEN_POSITION_NORMALIZED
			#define ASE_NEEDS_TEXTURE_COORDINATES1
			#define ASE_NEEDS_FRAG_TEXTURE_COORDINATES1
			#define ASE_NEEDS_TEXTURE_COORDINATES0
			#define ASE_NEEDS_FRAG_TEXTURE_COORDINATES0
			#define ASE_NEEDS_FRAG_COLOR


			#if defined(ASE_WRITE_DEPTH_CONSERVATIVE) && (SHADER_TARGET >= 45)
				#define ASE_SV_DEPTH SV_DepthLessEqual
				#define ASE_SV_POSITION_QUALIFIERS linear noperspective centroid
			#else
				#define ASE_SV_DEPTH SV_Depth
				#define ASE_SV_POSITION_QUALIFIERS
			#endif

			struct Attributes
			{
				float4 positionOS : POSITION;
				half3 normalOS : NORMAL;
				half4 tangentOS : TANGENT;
				float4 ase_texcoord1 : TEXCOORD1;
				float4 ase_texcoord : TEXCOORD0;
				float4 ase_color : COLOR;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct PackedVaryings
			{
				ASE_SV_POSITION_QUALIFIERS float4 positionCS : SV_POSITION;
				float3 positionWS : TEXCOORD0;
				half3 normalWS : TEXCOORD1;
				half4 tangentWS : TEXCOORD2;
				float4 ase_texcoord3 : TEXCOORD3;
				float4 ase_texcoord4 : TEXCOORD4;
				float4 ase_color : COLOR;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			CBUFFER_START(UnityPerMaterial)
			float _ZTest1;
			float _Opacity;
			float _NoiseLineDarkness;
			float _Samples;
			float _NoiseAmount;
			float _UseRadialNoise;
			float _NoiseInnerRadius;
			float _NoiseContrast;
			float _EdgeSoftness;
			float _NoiseScrollSpeed;
			float _NoiseAngularTiling;
			float _UseCustomStrength;
			float _Strength;
			float _Cull1;
			float _Dst1;
			float _Src1;
			float _ZWrite1;
			float _NoiseRadialTiling;
			float _NoiseBaseOpacity;
			float _AlphaClip;
			float _Cutoff;
			#ifdef ASE_TESSELLATION
				float _TessPhongStrength;
				float _TessValue;
				float _TessMin;
				float _TessMax;
				float _TessEdgeLength;
				float _TessMaxDisp;
			#endif
			CBUFFER_END

			#ifdef SCENEPICKINGPASS
				float4 _SelectionID;
			#endif

			#ifdef SCENESELECTIONPASS
				int _ObjectId;
				int _PassValue;
			#endif

			sampler2D _RadialNoiseTex;


			#if ( UNITY_VERSION >= 60010000 )
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/GBufferOutput.hlsl"
			#else
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UnityGBuffer.hlsl"
			#endif

			float ParticleStrength125( float Strength, float CustomStrength, float UseCustom )
			{
				return Strength * lerp(1.0, CustomStrength, saturate(UseCustom));
			}
			
			float2 Vefects_RadialNoiseUV140( float2 ParticleUV, float AngularTiling, float RadialTiling, float Time, float ScrollSpeed )
			{
				float2 p = (ParticleUV - 0.5) * 2.0;
				float angle = atan2(p.y, p.x) * 0.1591549431 + 0.5;
				return float2(angle * max(1.0, floor(AngularTiling + 0.5)), length(p) * RadialTiling + Time * ScrollSpeed);
			}
			
			float Vefects_RadialNoiseMask142( float2 ParticleUV, float Noise, float Contrast, float InnerRadius, float UseNoise, float Amount )
			{
				float r = length((ParticleUV - 0.5) * 2.0);
				float n = saturate((Noise - 0.5) * Contrast + 0.5);
				float inner = (InnerRadius > 0.0001) ? smoothstep(InnerRadius, min(1.0, InnerRadius + 0.2), r) : 1.0;
				return lerp(1.0, n * inner, saturate(UseNoise) * saturate(Amount));
			}
			
			float Vefects_NoiseBlurStrength143( float Strength, float NoiseMask )
			{
				return Strength * lerp(0.25, 1.0, saturate(NoiseMask));
			}
			
			float4 Vefects_RadialNoiseColor145( float4 Blur, float UseNoise, float Amount, float Darkness )
			{
				float darken = saturate(UseNoise) * saturate(Amount) * saturate(Darkness);
				return float4(Blur.rgb * (1.0 - darken), Blur.a);
			}
			
			float ParticleMask127( float2 ParticleUV, float VertexAlpha, float Opacity, float4 Blur, float EdgeSoftness )
			{
				float r = length((ParticleUV - 0.5) * 2.0);
				float mask = 1.0 - smoothstep(1.0 - clamp(EdgeSoftness, 0.001, 1.0), 1.0, r);
				return saturate(mask * VertexAlpha * Opacity * Blur.a);
			}
			
			float Vefects_NoiseOpacityCoverage147( float NoiseMask, float BaseOpacity )
			{
				return lerp(saturate(BaseOpacity), 1.0, saturate(NoiseMask));
			}
			

			PackedVaryings VertexFunction( Attributes input  )
			{
				PackedVaryings output = (PackedVaryings)0;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

				output.ase_texcoord3 = input.ase_texcoord1;
				output.ase_texcoord4.xy = input.ase_texcoord.xy;
				output.ase_color = input.ase_color;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				output.ase_texcoord4.zw = 0;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = input.positionOS.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif

				float3 vertexValue = defaultVertexValue;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					input.positionOS.xyz = vertexValue;
				#else
					input.positionOS.xyz += vertexValue;
				#endif

				input.normalOS = input.normalOS;
				input.tangentOS = input.tangentOS;

				VertexPositionInputs vertexInput = GetVertexPositionInputs( input.positionOS.xyz );
				VertexNormalInputs normalInput = GetVertexNormalInputs( input.normalOS, input.tangentOS );

				output.positionCS = ASE_ADJUST_CLIP_POSITION(vertexInput.positionCS );
				output.positionWS = vertexInput.positionWS;
				output.normalWS = normalInput.normalWS;
				output.tangentWS = half4( normalInput.tangentWS, ( input.tangentOS.w > 0.0 ? 1.0 : -1.0 ) * GetOddNegativeScale() );;
				return output;
			}

			#if defined(ASE_TESSELLATION)
			struct VertexControl
			{
				float4 positionOS : INTERNALTESSPOS;
				half3 normalOS : NORMAL;
				half4 tangentOS : TANGENT;
				float4 ase_texcoord1 : TEXCOORD1;
				float4 ase_texcoord : TEXCOORD0;
				float4 ase_color : COLOR;

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct TessellationFactors
			{
				float edge[3] : SV_TessFactor;
				float inside : SV_InsideTessFactor;
			};

			VertexControl vert ( Attributes input )
			{
				VertexControl output;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				output.positionOS = input.positionOS;
				output.normalOS = input.normalOS;
				output.tangentOS = input.tangentOS;
				output.ase_texcoord1 = input.ase_texcoord1;
				output.ase_texcoord = input.ase_texcoord;
				output.ase_color = input.ase_color;
				return output;
			}

			TessellationFactors TessellationFunction (InputPatch<VertexControl,3> input)
			{
				TessellationFactors output;
				float4 tf = 1;
				float tessValue = _TessValue; float tessMin = _TessMin; float tessMax = _TessMax;
				float edgeLength = _TessEdgeLength; float tessMaxDisp = _TessMaxDisp;
				#if defined(ASE_FIXED_TESSELLATION)
				tf = FixedTess( tessValue );
				#elif defined(ASE_DISTANCE_TESSELLATION)
				tf = DistanceBasedTess(input[0].positionOS, input[1].positionOS, input[2].positionOS, tessValue, tessMin, tessMax, GetObjectToWorldMatrix(), _WorldSpaceCameraPos );
				#elif defined(ASE_LENGTH_TESSELLATION)
				tf = EdgeLengthBasedTess(input[0].positionOS, input[1].positionOS, input[2].positionOS, edgeLength, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams );
				#elif defined(ASE_LENGTH_CULL_TESSELLATION)
				tf = EdgeLengthBasedTessCull(input[0].positionOS, input[1].positionOS, input[2].positionOS, edgeLength, tessMaxDisp, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams, unity_CameraWorldClipPlanes );
				#endif
				output.edge[0] = tf.x; output.edge[1] = tf.y; output.edge[2] = tf.z; output.inside = tf.w;
				return output;
			}

			[domain("tri")]
			[partitioning("fractional_odd")]
			[outputtopology("triangle_cw")]
			[patchconstantfunc("TessellationFunction")]
			[outputcontrolpoints(3)]
			VertexControl HullFunction(InputPatch<VertexControl, 3> patch, uint id : SV_OutputControlPointID)
			{
				return patch[id];
			}

			[domain("tri")]
			PackedVaryings DomainFunction(TessellationFactors factors, OutputPatch<VertexControl, 3> patch, float3 bary : SV_DomainLocation)
			{
				Attributes output = (Attributes) 0;
				output.positionOS = patch[0].positionOS * bary.x + patch[1].positionOS * bary.y + patch[2].positionOS * bary.z;
				output.normalOS = patch[0].normalOS * bary.x + patch[1].normalOS * bary.y + patch[2].normalOS * bary.z;
				output.tangentOS = patch[0].tangentOS * bary.x + patch[1].tangentOS * bary.y + patch[2].tangentOS * bary.z;
				output.ase_texcoord1 = patch[0].ase_texcoord1 * bary.x + patch[1].ase_texcoord1 * bary.y + patch[2].ase_texcoord1 * bary.z;
				output.ase_texcoord = patch[0].ase_texcoord * bary.x + patch[1].ase_texcoord * bary.y + patch[2].ase_texcoord * bary.z;
				output.ase_color = patch[0].ase_color * bary.x + patch[1].ase_color * bary.y + patch[2].ase_color * bary.z;
				#if defined(ASE_PHONG_TESSELLATION)
				float3 pp[3];
				for (int i = 0; i < 3; ++i)
					pp[i] = output.positionOS.xyz - patch[i].normalOS * (dot(output.positionOS.xyz, patch[i].normalOS) - dot(patch[i].positionOS.xyz, patch[i].normalOS));
				float phongStrength = _TessPhongStrength;
				output.positionOS.xyz = phongStrength * (pp[0]*bary.x + pp[1]*bary.y + pp[2]*bary.z) + (1.0f-phongStrength) * output.positionOS.xyz;
				#endif
				UNITY_TRANSFER_INSTANCE_ID(patch[0], output);
				return VertexFunction(output);
			}
			#else
			PackedVaryings vert ( Attributes input )
			{
				return VertexFunction( input );
			}
			#endif

		#if ( UNITY_VERSION >= 60010000 )
			GBufferFragOutput frag ( PackedVaryings input
		#else
			FragmentOutput frag ( PackedVaryings input
		#endif
								#if defined( ASE_WRITE_DEPTH )
								,out float outputDepth : ASE_SV_DEPTH
								#endif
								 )
			{
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

				#if defined(LOD_FADE_CROSSFADE)
					LODFadeCrossFade( input.positionCS );
				#endif

				// @diogo: mikktspace compliant
				float renormFactor = 1.0 / max( FLT_MIN, length( input.normalWS ) );

				float3 PositionWS = input.positionWS;
				float3 ViewDirWS = GetWorldSpaceNormalizeViewDir( PositionWS );
				float4 ScreenPosNorm = float4( GetNormalizedScreenSpaceUV( input.positionCS ), input.positionCS.zw );
				float4 ClipPos = ComputeClipSpacePosition( ScreenPosNorm.xy, input.positionCS.z ) * input.positionCS.w;
				float4 ScreenPos = ComputeScreenPos( ClipPos );
				float3 TangentWS = input.tangentWS.xyz * renormFactor;
				float3 BitangentWS = cross( input.normalWS, input.tangentWS.xyz ) * input.tangentWS.w * renormFactor;
				float3 NormalWS = input.normalWS * renormFactor;

				float2 ScreenUV126 = ScreenPosNorm.xy;
				float3 CenterWS126 = input.ase_texcoord3.xyz;
				float Strength125 = _Strength;
				float CustomStrength125 = input.ase_texcoord3.w;
				float UseCustom125 = _UseCustomStrength;
				float localParticleStrength125 = ParticleStrength125( Strength125 , CustomStrength125 , UseCustom125 );
				float Strength143 = localParticleStrength125;
				float2 VTC_UV159 = input.ase_texcoord4.xy;
				float2 ParticleUV142 = VTC_UV159;
				float2 ParticleUV140 = VTC_UV159;
				float AngularTiling140 = _NoiseAngularTiling;
				float RadialTiling140 = _NoiseRadialTiling;
				float Time140 = _TimeParameters.x;
				float ScrollSpeed140 = _NoiseScrollSpeed;
				float2 localVefects_RadialNoiseUV140 = Vefects_RadialNoiseUV140( ParticleUV140 , AngularTiling140 , RadialTiling140 , Time140 , ScrollSpeed140 );
				float Noise142 = tex2D( _RadialNoiseTex, localVefects_RadialNoiseUV140 ).r;
				float Contrast142 = _NoiseContrast;
				float InnerRadius142 = _NoiseInnerRadius;
				float useRN149 = _UseRadialNoise;
				float UseNoise142 = useRN149;
				float noiseAm148 = _NoiseAmount;
				float Amount142 = noiseAm148;
				float localVefects_RadialNoiseMask142 = Vefects_RadialNoiseMask142( ParticleUV142 , Noise142 , Contrast142 , InnerRadius142 , UseNoise142 , Amount142 );
				float noiseMask163 = localVefects_RadialNoiseMask142;
				float NoiseMask143 = noiseMask163;
				float localVefects_NoiseBlurStrength143 = Vefects_NoiseBlurStrength143( Strength143 , NoiseMask143 );
				float Strength126 = localVefects_NoiseBlurStrength143;
				float Samples126 = _Samples;
				float4 localVefects_Radial_Zoom_Blur_URP126 = Vefects_Radial_Zoom_Blur_URP( ScreenUV126 , CenterWS126 , Strength126 , Samples126 );
				float4 Blur145 = ( localVefects_Radial_Zoom_Blur_URP126 * float4( (input.ase_color).rgb , 0.0 ) );
				float UseNoise145 = useRN149;
				float Amount145 = noiseAm148;
				float Darkness145 = _NoiseLineDarkness;
				float4 localVefects_RadialNoiseColor145 = Vefects_RadialNoiseColor145( Blur145 , UseNoise145 , Amount145 , Darkness145 );
				float4 outCOL154 = localVefects_RadialNoiseColor145;
				
				float2 ParticleUV127 = VTC_UV159;
				float VertexAlpha127 = input.ase_color.a;
				float Opacity127 = _Opacity;
				float4 Blur127 = localVefects_Radial_Zoom_Blur_URP126;
				float EdgeSoftness127 = _EdgeSoftness;
				float localParticleMask127 = ParticleMask127( ParticleUV127 , VertexAlpha127 , Opacity127 , Blur127 , EdgeSoftness127 );
				float NoiseMask147 = noiseMask163;
				float BaseOpacity147 = _NoiseBaseOpacity;
				float localVefects_NoiseOpacityCoverage147 = Vefects_NoiseOpacityCoverage147( NoiseMask147 , BaseOpacity147 );
				float outOP156 = saturate( ( localParticleMask127 * localVefects_NoiseOpacityCoverage147 ) );
				

				float3 Color = outCOL154.xyz;
				float3 Normal = float3(0, 0, 1);
				float Alpha = outOP156;
				#if defined( _ALPHATEST_ON )
					float AlphaClipThreshold = _Cutoff;
					float AlphaClipThresholdShadow = 0.5;
				#endif

				#if defined( ASE_WRITE_DEPTH )
					input.positionCS.z = input.positionCS.z;
				#endif

				#ifdef _ALPHATEST_ON
					clip(Alpha - AlphaClipThreshold);
				#endif

				InputData inputData = (InputData)0;
				inputData.positionWS = PositionWS;
				inputData.positionCS = input.positionCS;
				inputData.normalizedScreenSpaceUV = ScreenPosNorm.xy;

				#ifdef _NORMALMAP
					#if _NORMAL_DROPOFF_TS
						inputData.normalWS = TransformTangentToWorld(Normal, half3x3( TangentWS, BitangentWS, NormalWS ));
					#elif _NORMAL_DROPOFF_OS
						inputData.normalWS = TransformObjectToWorldNormal(Normal);
					#elif _NORMAL_DROPOFF_WS
						inputData.normalWS = Normal;
					#endif
					inputData.normalWS = NormalizeNormalPerPixel(inputData.normalWS);
				#else
					inputData.normalWS = NormalWS;
				#endif

				inputData.viewDirectionWS = ViewDirWS;

				#if defined(_DBUFFER) && defined(UNLIT_DEFAULT_DECAL_BLENDING)
					ApplyDecalToBaseColor(input.positionCS, Color);
				#endif

				#if defined( ASE_WRITE_DEPTH )
					outputDepth = input.positionCS.z;
				#endif

				SurfaceData surfaceData = (SurfaceData)0;
				surfaceData.albedo = Color;
				surfaceData.alpha = Alpha;

				#if defined(_SCREEN_SPACE_OCCLUSION) && !defined(_SURFACE_TYPE_TRANSPARENT)
					float2 normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV( unpacked.positionCS );
					AmbientOcclusionFactor aoFactor = GetScreenSpaceAmbientOcclusion( normalizedScreenSpaceUV );
					surfaceData.albedo.rgb *= aoFactor.directAmbientOcclusion;
				#else
					surfaceData.occlusion = 1;
				#endif

			#if ( UNITY_VERSION >= 60010000 )
				return PackGBuffersSurfaceData( surfaceData, inputData, float3( 0, 0, 0 ) );
			#else
				return SurfaceDataToGbuffer( surfaceData, inputData, float3( 0, 0, 0 ), kLightingInvalid );
			#endif
			}

			ENDHLSL
		}
		
	}
	

	

	CustomEditor "UnityEditor.ShaderGraphUnlitGUI"
	FallBack "Hidden/Shader Graph/FallbackError"
	
	Fallback Off
}
/*ASEBEGIN
Version=19912
{"type":"AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor","id":168,"pos":[-2744,720],"params":["Inherit","False","676","291","Reg","4","131","148","149","132","Reg","0,0,0,1","0","0"]}
{"type":"AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor","id":167,"pos":[-2744,464],"params":["Inherit","False","548","211","VTC","2","117","159","VTC","0,0,0,1","0","0"]}
{"type":"AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor","id":166,"pos":[-2736,-1584],"params":["Inherit","False","2212","771","Noise Mask","14","142","150","136","137","151","160","140","141","133","134","135","161","139","163","Noise Mask","0,0,0,1","0","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":134,"pos":[-2688,-1280],"params":["Inherit","False","Property","_NoiseRadialTiling","Noise Radial Tiling","9","0","Create","True","0","0","0","False","0","False","Object","-1","","0.25","0.25","0","8","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":161,"pos":[-2688,-1536],"params":["Inherit","False","159","VTC UV","1","0","OBJECT","","False","1","FLOAT2","0"]}
{"type":"AmplifyShaderEditor.SimpleTimeNode, AmplifyShaderEditor","id":139,"pos":[-2688,-1024],"params":["Inherit","False","1","0","FLOAT","1","False","5","FLOAT","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":133,"pos":[-2688,-1408],"params":["Inherit","False","Property","_NoiseAngularTiling","Noise Angular Tiling","8","0","Create","True","0","0","0","False","0","False","Object","-1","","8","8","1","128","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":135,"pos":[-2688,-1152],"params":["Inherit","False","Property","_NoiseScrollSpeed","Noise Scroll Speed","10","0","Create","True","0","0","0","False","0","False","Object","-1","","0","0","-4","4","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.TexCoordVertexDataNode, AmplifyShaderEditor","id":117,"pos":[-2696,512],"params":["Inherit","False","0","2","0","5","FLOAT2","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":131,"pos":[-2696,896],"params":["Inherit","False","Property","_UseRadialNoise","Use Radial Noise","5","0","Create","True","0","0","0","False","0","False","Object","-1","","1","1","0","1","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":132,"pos":[-2696,768],"params":["Inherit","False","Property","_NoiseAmount","Noise Amount","6","0","Create","True","0","0","0","False","0","False","Object","-1","","1","1","0","1","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.CustomExpressionNode, AmplifyShaderEditor","id":140,"pos":[-2304,-1536],"params":["Float","False","float2 p = (ParticleUV - 0.5) * 2.0;\nfloat angle = atan2(p.y, p.x) * 0.1591549431 + 0.5;\nreturn float2(angle * max(1.0, floor(AngularTiling + 0.5)), length(p) * RadialTiling + Time * ScrollSpeed);","2","Create","5","True","ParticleUV","FLOAT2","0,0","In","","Inherit","False","True","AngularTiling","FLOAT","0","In","","Inherit","False","True","RadialTiling","FLOAT","0","In","","Inherit","False","True","Time","FLOAT","0","In","","Inherit","False","True","ScrollSpeed","FLOAT","0","In","","Inherit","False","Vefects_RadialNoiseUV","True","False","0","","False","5","0","FLOAT2","0,0","False","1","FLOAT","0","False","2","FLOAT","0","False","3","FLOAT","0","False","4","FLOAT","0","False","1","FLOAT2","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":148,"pos":[-2312,768],"params":["Inherit","False","noiseAm","-1","True","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":149,"pos":[-2312,896],"params":["Inherit","False","useRN","-1","True","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":159,"pos":[-2400,512],"params":["Inherit","False","VTC UV","-1","True","1","0","FLOAT2","0,0","False","1","FLOAT2","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":150,"pos":[-1536,-1024],"params":["Inherit","False","148","noiseAm","1","0","OBJECT","","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":137,"pos":[-1536,-1280],"params":["Inherit","False","Property","_NoiseInnerRadius","Noise Inner Radius","12","0","Create","True","0","0","0","False","0","False","Object","-1","","0.25","0.25","0","0.95","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":151,"pos":[-1536,-1152],"params":["Inherit","False","149","useRN","1","0","OBJECT","","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":160,"pos":[-1536,-1536],"params":["Inherit","False","159","VTC UV","1","0","OBJECT","","False","1","FLOAT2","0"]}
{"type":"AmplifyShaderEditor.SamplerNode, AmplifyShaderEditor","id":141,"pos":[-2048,-1536],"params":["Inherit","True","Property","_RadialNoiseTex","Radial Noise Texture","7","0","Create","True","0","0","0","False","0","False","","-1","0cecb8f6ca69d534cb0c1eeeb84d65dd","0cecb8f6ca69d534cb0c1eeeb84d65dd","True","0","False","white","Auto","False","Object","-1","Auto","Texture2D","False","8","0","SAMPLER2D","","False","1","FLOAT2","0,0","False","2","FLOAT","0","False","3","FLOAT2","0,0","False","4","FLOAT2","0,0","False","5","FLOAT","1","False","6","FLOAT","0","False","7","SAMPLERSTATE","","False","6","COLOR","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4","FLOAT3","5"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":136,"pos":[-1536,-1408],"params":["Inherit","False","Property","_NoiseContrast","Noise Contrast","11","0","Create","True","0","0","0","False","0","False","Object","-1","","1","4","0","2.67","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.CustomExpressionNode, AmplifyShaderEditor","id":142,"pos":[-1024,-1536],"params":["Float","False","float r = length((ParticleUV - 0.5) * 2.0);\nfloat n = saturate((Noise - 0.5) * Contrast + 0.5);\nfloat inner = (InnerRadius > 0.0001) ? smoothstep(InnerRadius, min(1.0, InnerRadius + 0.2), r) : 1.0;\nreturn lerp(1.0, n * inner, saturate(UseNoise) * saturate(Amount));","1","Create","6","True","ParticleUV","FLOAT2","0,0","In","","Inherit","False","True","Noise","FLOAT","0","In","","Inherit","False","True","Contrast","FLOAT","0","In","","Inherit","False","True","InnerRadius","FLOAT","0","In","","Inherit","False","True","UseNoise","FLOAT","0","In","","Inherit","False","True","Amount","FLOAT","0","In","","Inherit","False","Vefects_RadialNoiseMask","True","False","0","","False","6","0","FLOAT2","0,0","False","1","FLOAT","0","False","2","FLOAT","0","False","3","FLOAT","0","False","4","FLOAT","0","False","5","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":123,"pos":[-2688,0],"params":["Inherit","False","Property","_UseCustomStrength","Use Custom Strength","4","0","Create","True","0","0","0","False","0","False","Object","-1","","0","0","0","1","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":119,"pos":[-2688,-128],"params":["Inherit","False","Property","_Strength","Blur Strength","0","0","Create","True","0","0","0","False","0","False","Object","-1","","0.12","0.12","-1","1","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":163,"pos":[-768,-1536],"params":["Inherit","False","noiseMask","-1","True","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor","id":169,"pos":[-1586,462],"params":["Inherit","False","1316","803","Opacity","10","165","121","122","146","127","147","144","157","156","162","Opacity","0,0,0,1","0","0"]}
{"type":"AmplifyShaderEditor.CustomExpressionNode, AmplifyShaderEditor","id":125,"pos":[-2304,-128],"params":["Float","False","return Strength * lerp(1.0, CustomStrength, saturate(UseCustom));","1","Create","3","True","Strength","FLOAT","0","In","","Inherit","False","True","CustomStrength","FLOAT","0","In","","Inherit","False","True","UseCustom","FLOAT","0","In","","Inherit","False","ParticleStrength","True","False","0","","False","3","0","FLOAT","0","False","1","FLOAT","0","False","2","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":164,"pos":[-2048,0],"params":["Inherit","False","163","noiseMask","1","0","OBJECT","","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":120,"pos":[-1792,-384],"params":["Inherit","False","Property","_Samples","Samples","1","0","Create","True","0","0","0","False","0","False","Object","-1","","8","8","2","64","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.VertexColorNode, AmplifyShaderEditor","id":118,"pos":[-1664,-256],"params":["Inherit","False","0","5","COLOR","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.TexCoordVertexDataNode, AmplifyShaderEditor","id":116,"pos":[-2688,-384],"params":["Inherit","False","1","4","0","5","FLOAT4","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.CustomExpressionNode, AmplifyShaderEditor","id":143,"pos":[-2048,-128],"params":["Float","False","return Strength * lerp(0.25, 1.0, saturate(NoiseMask));","1","Create","2","True","Strength","FLOAT","0","In","","Inherit","False","True","NoiseMask","FLOAT","0","In","","Inherit","False","Vefects_NoiseBlurStrength","True","False","0","","False","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.ScreenPosInputsNode, AmplifyShaderEditor","id":115,"pos":[-2688,-640],"params":["Float","False","0","False","0","5","FLOAT4","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":165,"pos":[-1536,1024],"params":["Inherit","False","163","noiseMask","1","0","OBJECT","","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":121,"pos":[-1536,768],"params":["Inherit","False","Property","_Opacity","Opacity","2","0","Create","True","0","0","0","False","0","False","Object","-1","","1","1","0","1","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":122,"pos":[-1536,896],"params":["Inherit","False","Property","_EdgeSoftness","Edge Softness","3","0","Create","True","0","0","0","False","0","False","Object","-1","","0.3","0.3","0.001","1","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":146,"pos":[-1536,1152],"params":["Inherit","False","Property","_NoiseBaseOpacity","Noise Base Opacity","14","0","Create","True","0","0","0","False","0","False","Object","-1","","0.8","0.8","0","1","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":162,"pos":[-1536,512],"params":["Inherit","False","159","VTC UV","1","0","OBJECT","","False","1","FLOAT2","0"]}
{"type":"AmplifyShaderEditor.ComponentMaskNode, AmplifyShaderEditor","id":130,"pos":[-1280,-512],"params":["Inherit","False","True","True","True","False","1","0","COLOR","0,0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.CustomExpressionNode, AmplifyShaderEditor","id":126,"pos":[-1664,-640],"params":["Float","False"," ","4","File","4","True","ScreenUV","FLOAT2","0,0","In","","Inherit","False","True","CenterWS","FLOAT3","0,0,0","In","","Inherit","False","True","Strength","FLOAT","0","In","","Inherit","False","True","Samples","FLOAT","0","In","","Inherit","False","Vefects_Radial_Zoom_Blur_URP","False","False","0","a5eaaedcbecdf3148bd17c9cb450424c","False","4","0","FLOAT2","0,0","False","1","FLOAT3","0,0,0","False","2","FLOAT","0","False","3","FLOAT","0","False","1","FLOAT4","0"]}
{"type":"AmplifyShaderEditor.CustomExpressionNode, AmplifyShaderEditor","id":127,"pos":[-1280,512],"params":["Float","False","float r = length((ParticleUV - 0.5) * 2.0);\nfloat mask = 1.0 - smoothstep(1.0 - clamp(EdgeSoftness, 0.001, 1.0), 1.0, r);\nreturn saturate(mask * VertexAlpha * Opacity * Blur.a);","1","Create","5","True","ParticleUV","FLOAT2","0,0","In","","Inherit","False","True","VertexAlpha","FLOAT","0","In","","Inherit","False","True","Opacity","FLOAT","0","In","","Inherit","False","True","Blur","FLOAT4","0,0,0,0","In","","Inherit","False","True","EdgeSoftness","FLOAT","0","In","","Inherit","False","ParticleMask","True","False","0","","False","5","0","FLOAT2","0,0","False","1","FLOAT","0","False","2","FLOAT","0","False","3","FLOAT4","0,0,0,0","False","4","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.CustomExpressionNode, AmplifyShaderEditor","id":147,"pos":[-1280,1024],"params":["Inherit","False","return lerp(saturate(BaseOpacity), 1.0, saturate(NoiseMask));","1","Create","2","True","NoiseMask","FLOAT","0","In","","Inherit","False","True","BaseOpacity","FLOAT","0","In","","Inherit","False","Vefects_NoiseOpacityCoverage","True","False","0","","False","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":129,"pos":[-1024,-640],"params":["Inherit","False","2","2","0","FLOAT4","0,0,0,0","False","1","FLOAT3","0,0,0","False","1","FLOAT4","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":152,"pos":[-1024,-384],"params":["Inherit","False","149","useRN","1","0","OBJECT","","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":153,"pos":[-1024,-256],"params":["Inherit","False","148","noiseAm","1","0","OBJECT","","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":138,"pos":[-1024,-128],"params":["Inherit","False","Property","_NoiseLineDarkness","Noise Line Darkness","13","0","Create","True","0","0","0","False","0","False","Object","-1","","0.45","0.45","0","1","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":144,"pos":[-1024,768],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.CustomExpressionNode, AmplifyShaderEditor","id":145,"pos":[-768,-640],"params":["Float","False","float darken = saturate(UseNoise) * saturate(Amount) * saturate(Darkness);\nreturn float4(Blur.rgb * (1.0 - darken), Blur.a);","4","Create","4","True","Blur","FLOAT4","0,0,0,0","In","","Inherit","False","True","UseNoise","FLOAT","0","In","","Inherit","False","True","Amount","FLOAT","0","In","","Inherit","False","True","Darkness","FLOAT","0","In","","Inherit","False","Vefects_RadialNoiseColor","True","False","0","","False","4","0","FLOAT4","0,0,0,0","False","1","FLOAT","0","False","2","FLOAT","0","False","3","FLOAT","0","False","1","FLOAT4","0"]}
{"type":"AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor","id":157,"pos":[-896,768],"params":["Inherit","False","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":154,"pos":[-512,-640],"params":["Inherit","False","outCOL","-1","True","1","0","FLOAT4","0,0,0,0","False","1","FLOAT4","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":156,"pos":[-512,768],"params":["Inherit","False","outOP","-1","True","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":3,"pos":[1664,0],"params":["Inherit","False","Property","_ZTest1","ZTest","19","0","Create","True","0","0","0","True","0","False","Object","-1","","8","2","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":6,"pos":[1408,0],"params":["Inherit","False","Property","_ZWrite1","ZWrite","18","0","Create","True","0","0","0","True","0","False","Object","-1","","0","0","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":5,"pos":[896,0],"params":["Inherit","False","Property","_Src1","Src","16","0","Create","True","0","0","0","True","0","False","Object","-1","","5","1","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":4,"pos":[1152,0],"params":["Inherit","False","Property","_Dst1","Dst","17","0","Create","True","0","0","0","True","0","False","Object","-1","","10","1","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":7,"pos":[640,0],"params":["Inherit","False","Property","_Cull1","Cull","15","0","Create","True","0","0","0","True","3","Space(13)","Header(AR)","Space(13)","False","Object","-1","","0","2","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":155,"pos":[-256,0],"params":["Inherit","False","154","outCOL","1","0","OBJECT","","False","1","FLOAT4","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":158,"pos":[-256,128],"params":["Inherit","False","156","outOP","1","0","OBJECT","","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.TemplateMultiPassMasterNode, AmplifyShaderEditor","id":54,"pos":[0,0],"params":["Float","False","False","-1","3","UnityEditor.ShaderGraphUnlitGUI","0","1","New Amplify Shader","2992e84f91cbeb14eab234972e07ea9d","True","ExtraPrePass","0","0","ExtraPrePass","6","False","True","1","1","False","","0","False","","1","1","False","","0","False","","True","1","False","","1","False","","False","False","False","False","False","False","False","False","False","True","0","False","","False","True","0","False","","False","True","True","True","True","True","0","False","","False","False","False","False","False","False","False","True","False","0","False","","255","False","","255","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","False","True","1","False","","False","False","False","True","4","RenderPipeline=UniversalPipeline","RenderType=Opaque=RenderType","Queue=Geometry=Queue=0","UniversalMaterialType=Unlit","True","5","True","14","all","0","False","False","False","False","False","False","False","False","False","False","False","False","False","False","True","0","False","","False","True","True","True","True","True","0","False","","False","False","False","False","False","False","False","True","False","0","False","","255","False","","255","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","False","True","1","False","","True","3","False","","True","True","0","False","","0","False","","False","True","0","False","False","0","","0","0","Standard","0","False","0"]}
{"type":"AmplifyShaderEditor.TemplateMultiPassMasterNode, AmplifyShaderEditor","id":55,"pos":[0,0],"params":["Float","False","True","-1","3","UnityEditor.ShaderGraphUnlitGUI","0","17","Vefects/SH_Vefects_VFX_Zoom_Blur_Radial_Noise_01_URP","2992e84f91cbeb14eab234972e07ea9d","True","Forward","0","1","Forward","12","True","True","1","1","True","_Src1","0","True","_Dst1","1","1","False","","0","False","","True","1","False","","1","False","","False","False","False","False","False","False","False","False","False","True","0","False","","True","True","0","True","_Cull1","True","True","True","True","True","False","0","False","","False","False","False","False","False","False","False","True","False","0","False","","255","False","","255","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","True","True","1","True","_ZWrite1","False","False","False","True","4","RenderPipeline=UniversalPipeline","RenderType=Transparent=RenderType","Queue=Transparent=Queue=0","UniversalMaterialType=Unlit","True","2","True","14","all","0","True","True","1","5","True","_Src1","10","True","_Dst1","1","1","False","","10","False","","True","1","False","","1","False","","False","False","False","False","False","False","False","False","False","False","False","False","True","True","True","True","True","False","0","False","","False","False","False","False","False","False","False","True","False","0","False","","255","False","","255","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","True","True","2","True","_ZWrite1","True","3","True","_ZTest1","True","True","0","False","","0","False","","False","True","1","LightMode=UniversalForwardOnly","False","False","0","","0","0","Standard","33","Surface","1","639209391155893547","  Keep Alpha","0","0","  Blend","0","0","Two Sided","1","0","Alpha Clipping","0","0","  Use Shadow Threshold","0","0","Fragment Normal Space","0","0","Forward Only","0","0","Cast Shadows","0","639237959225533410","Receive Shadows","2","0","Receive SSAO","0","639237959230253499","Default Decal Blending","0","639269779283187381","Motion Vectors","0","639269779289691527","  Additional Motion Vectors","1","0","  Alembic Motion Vectors","0","0","  XR Motion Vectors","0","0","GPU Instancing","1","0","LOD CrossFade","0","639237959239848067","Built-in Fog","0","639237959243979640","Meta Pass","0","0","Extra Pre Pass","0","0","Tessellation","0","0","  Phong","0","0","  Strength","0.5,False,","0","  Type","0","0","  Tess","16,False,","0","  Min","10,False,","0","  Max","25,False,","0","  Edge Length","16,False,","0","  Max Displacement","25,False,","0","Write Depth","0","0","  Conservative","0","0","Vertex Position","1","0","0","13","False","True","False","True","False","False","True","True","True","False","False","False","True","False","","False","0"]}
{"type":"AmplifyShaderEditor.TemplateMultiPassMasterNode, AmplifyShaderEditor","id":56,"pos":[0,0],"params":["Float","False","False","-1","3","UnityEditor.ShaderGraphUnlitGUI","0","1","New Amplify Shader","2992e84f91cbeb14eab234972e07ea9d","True","ShadowCaster","0","2","ShadowCaster","0","False","True","1","1","False","","0","False","","1","1","False","","0","False","","True","1","False","","1","False","","False","False","False","False","False","False","False","False","False","True","0","False","","False","True","0","False","","False","True","True","True","True","True","0","False","","False","False","False","False","False","False","False","True","False","0","False","","255","False","","255","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","False","True","1","False","","False","False","False","True","4","RenderPipeline=UniversalPipeline","RenderType=Opaque=RenderType","Queue=Geometry=Queue=0","UniversalMaterialType=Unlit","True","5","True","14","all","0","False","False","False","False","False","False","False","False","False","False","False","False","True","0","False","","False","False","False","True","False","False","False","False","0","False","","False","False","False","False","False","False","False","False","False","True","1","False","","True","3","False","","False","False","True","1","LightMode=ShadowCaster","False","False","0","","0","0","Standard","0","False","0"]}
{"type":"AmplifyShaderEditor.TemplateMultiPassMasterNode, AmplifyShaderEditor","id":57,"pos":[0,0],"params":["Float","False","False","-1","3","UnityEditor.ShaderGraphUnlitGUI","0","1","New Amplify Shader","2992e84f91cbeb14eab234972e07ea9d","True","DepthOnly","0","3","DepthOnly","0","False","True","1","1","False","","0","False","","1","1","False","","0","False","","True","1","False","","1","False","","False","False","False","False","False","False","False","False","False","True","0","False","","False","True","0","False","","False","True","True","True","True","True","0","False","","False","False","False","False","False","False","False","True","False","0","False","","255","False","","255","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","False","True","1","False","","False","False","False","True","4","RenderPipeline=UniversalPipeline","RenderType=Opaque=RenderType","Queue=Geometry=Queue=0","UniversalMaterialType=Unlit","True","5","True","14","all","0","False","False","False","False","False","False","False","False","False","False","False","False","True","0","False","","False","False","False","True","False","False","False","False","0","False","","False","False","False","False","False","False","False","False","False","True","1","False","","False","False","False","True","1","LightMode=DepthOnly","False","False","0","","0","0","Standard","0","False","0"]}
{"type":"AmplifyShaderEditor.TemplateMultiPassMasterNode, AmplifyShaderEditor","id":58,"pos":[0,0],"params":["Float","False","False","-1","3","UnityEditor.ShaderGraphUnlitGUI","0","1","New Amplify Shader","2992e84f91cbeb14eab234972e07ea9d","True","Meta","0","4","Meta","0","False","True","1","1","False","","0","False","","1","1","False","","0","False","","True","1","False","","1","False","","False","False","False","False","False","False","False","False","False","True","0","False","","False","True","0","False","","False","True","True","True","True","True","0","False","","False","False","False","False","False","False","False","True","False","0","False","","255","False","","255","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","False","True","1","False","","False","False","False","True","4","RenderPipeline=UniversalPipeline","RenderType=Opaque=RenderType","Queue=Geometry=Queue=0","UniversalMaterialType=Unlit","True","5","True","14","all","0","False","False","False","False","False","False","False","False","False","False","False","False","False","False","True","2","False","","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","True","1","LightMode=Meta","False","False","0","","0","0","Standard","0","False","0"]}
{"type":"AmplifyShaderEditor.TemplateMultiPassMasterNode, AmplifyShaderEditor","id":59,"pos":[0,0],"params":["Float","False","False","-1","3","UnityEditor.ShaderGraphUnlitGUI","0","1","New Amplify Shader","2992e84f91cbeb14eab234972e07ea9d","True","Universal2D","0","5","Universal2D","0","False","True","1","1","False","","0","False","","1","1","False","","0","False","","True","1","False","","1","False","","False","False","False","False","False","False","False","False","False","True","0","False","","False","True","0","False","","False","True","True","True","True","True","0","False","","False","False","False","False","False","False","False","True","False","0","False","","255","False","","255","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","False","True","1","False","","False","False","False","True","4","RenderPipeline=UniversalPipeline","RenderType=Opaque=RenderType","Queue=Geometry=Queue=0","UniversalMaterialType=Unlit","True","5","True","14","all","0","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","True","True","True","True","True","0","False","","False","False","False","False","False","False","False","True","False","0","False","","255","False","","255","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","False","True","1","False","","True","3","False","","True","True","0","False","","0","False","","False","True","1","LightMode=Universal2D","False","False","0","","0","0","Standard","0","False","0"]}
{"type":"AmplifyShaderEditor.TemplateMultiPassMasterNode, AmplifyShaderEditor","id":60,"pos":[0,0],"params":["Float","False","False","-1","3","UnityEditor.ShaderGraphUnlitGUI","0","1","New Amplify Shader","2992e84f91cbeb14eab234972e07ea9d","True","SceneSelectionPass","0","6","SceneSelectionPass","0","False","True","1","1","False","","0","False","","1","1","False","","0","False","","True","1","False","","1","False","","False","False","False","False","False","False","False","False","False","True","0","False","","False","True","0","False","","False","True","True","True","True","True","0","False","","False","False","False","False","False","False","False","True","False","0","False","","255","False","","255","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","False","True","1","False","","False","False","False","True","4","RenderPipeline=UniversalPipeline","RenderType=Opaque=RenderType","Queue=Geometry=Queue=0","UniversalMaterialType=Unlit","True","5","True","14","all","0","False","False","False","False","False","False","False","False","False","False","False","False","True","0","False","","False","True","2","False","","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","True","1","LightMode=SceneSelectionPass","False","False","0","","0","0","Standard","0","False","0"]}
{"type":"AmplifyShaderEditor.TemplateMultiPassMasterNode, AmplifyShaderEditor","id":61,"pos":[0,0],"params":["Float","False","False","-1","3","UnityEditor.ShaderGraphUnlitGUI","0","1","New Amplify Shader","2992e84f91cbeb14eab234972e07ea9d","True","ScenePickingPass","0","7","ScenePickingPass","0","False","True","1","1","False","","0","False","","1","1","False","","0","False","","True","1","False","","1","False","","False","False","False","False","False","False","False","False","False","True","0","False","","False","True","0","False","","False","True","True","True","True","True","0","False","","False","False","False","False","False","False","False","True","False","0","False","","255","False","","255","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","False","True","1","False","","False","False","False","True","4","RenderPipeline=UniversalPipeline","RenderType=Opaque=RenderType","Queue=Geometry=Queue=0","UniversalMaterialType=Unlit","True","5","True","14","all","0","False","False","False","False","False","False","False","False","False","False","False","False","True","0","False","","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","True","1","LightMode=Picking","False","False","0","","0","0","Standard","0","False","0"]}
{"type":"AmplifyShaderEditor.TemplateMultiPassMasterNode, AmplifyShaderEditor","id":62,"pos":[0,0],"params":["Float","False","False","-1","3","UnityEditor.ShaderGraphUnlitGUI","0","1","New Amplify Shader","2992e84f91cbeb14eab234972e07ea9d","True","DepthNormals","0","8","DepthNormals","0","False","True","1","1","False","","0","False","","1","1","False","","0","False","","True","1","False","","1","False","","False","False","False","False","False","False","False","False","False","True","0","False","","False","True","0","False","","False","True","True","True","True","True","0","False","","False","False","False","False","False","False","False","True","False","0","False","","255","False","","255","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","False","True","1","False","","False","False","False","True","4","RenderPipeline=UniversalPipeline","RenderType=Opaque=RenderType","Queue=Geometry=Queue=0","UniversalMaterialType=Unlit","True","5","True","14","all","0","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","True","1","False","","True","3","False","","False","False","True","1","LightMode=DepthNormalsOnly","False","False","0","","0","0","Standard","0","False","0"]}
{"type":"AmplifyShaderEditor.TemplateMultiPassMasterNode, AmplifyShaderEditor","id":63,"pos":[0,0],"params":["Float","False","False","-1","3","UnityEditor.ShaderGraphUnlitGUI","0","1","New Amplify Shader","2992e84f91cbeb14eab234972e07ea9d","True","DepthNormalsOnly","0","9","DepthNormalsOnly","0","False","True","1","1","False","","0","False","","1","1","False","","0","False","","True","1","False","","1","False","","False","False","False","False","False","False","False","False","False","True","0","False","","False","True","0","False","","False","True","True","True","True","True","0","False","","False","False","False","False","False","False","False","True","False","0","False","","255","False","","255","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","False","True","1","False","","False","False","False","True","4","RenderPipeline=UniversalPipeline","RenderType=Opaque=RenderType","Queue=Geometry=Queue=0","UniversalMaterialType=Unlit","True","5","True","14","all","0","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","True","1","False","","True","3","False","","False","False","True","1","LightMode=DepthNormalsOnly","False","True","9","d3d11","metal","vulkan","xboxone","xboxseries","playstation","ps4","ps5","switch","0","","0","0","Standard","0","False","0"]}
{"type":"AmplifyShaderEditor.TemplateMultiPassMasterNode, AmplifyShaderEditor","id":111,"pos":[0,283],"params":["Float","False","False","-1","3","UnityEditor.ShaderGraphUnlitGUI","0","1","New Amplify Shader","2992e84f91cbeb14eab234972e07ea9d","True","MotionVectors","0","10","MotionVectors","0","False","True","1","1","False","","0","False","","1","1","False","","0","False","","True","1","False","","1","False","","False","False","False","False","False","False","False","False","False","True","0","False","","False","True","0","False","","False","True","True","True","True","True","0","False","","False","False","False","False","False","False","False","True","False","0","False","","255","False","","255","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","False","True","1","False","","False","False","False","True","4","RenderPipeline=UniversalPipeline","RenderType=Opaque=RenderType","Queue=Geometry=Queue=0","UniversalMaterialType=Unlit","True","5","True","14","all","0","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","True","True","True","False","False","0","False","","False","False","False","False","False","False","False","False","False","False","False","False","False","True","1","LightMode=MotionVectors","False","False","0","","0","0","Standard","0","False","0"]}
{"type":"AmplifyShaderEditor.TemplateMultiPassMasterNode, AmplifyShaderEditor","id":112,"pos":[0,283],"params":["Float","False","False","-1","3","UnityEditor.ShaderGraphUnlitGUI","0","1","New Amplify Shader","2992e84f91cbeb14eab234972e07ea9d","True","XRMotionVectors","0","11","XRMotionVectors","0","False","True","1","1","False","","0","False","","1","1","False","","0","False","","True","1","False","","1","False","","False","False","False","False","False","False","False","False","False","True","0","False","","False","True","0","False","","False","True","True","True","True","True","0","False","","False","False","False","False","False","False","False","True","False","0","False","","255","False","","255","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","False","True","1","False","","False","False","False","True","4","RenderPipeline=UniversalPipeline","RenderType=Opaque=RenderType","Queue=Geometry=Queue=0","UniversalMaterialType=Unlit","True","5","True","14","all","0","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","True","True","True","True","True","0","False","","False","False","False","False","False","False","False","True","True","1","False","","255","False","","1","False","","7","False","","3","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","False","False","False","False","False","True","1","LightMode=XRMotionVectors","False","False","0","","0","0","Standard","0","False","0"]}
{"type":"AmplifyShaderEditor.TemplateMultiPassMasterNode, AmplifyShaderEditor","id":113,"pos":[0,283],"params":["Float","False","False","-1","3","UnityEditor.ShaderGraphUnlitGUI","0","1","New Amplify Shader","2992e84f91cbeb14eab234972e07ea9d","True","GBuffer","0","12","GBuffer","0","False","True","1","1","False","","0","False","","1","1","False","","0","False","","True","1","False","","1","False","","False","False","False","False","False","False","False","False","False","True","0","False","","False","True","0","False","","False","True","True","True","True","True","0","False","","False","False","False","False","False","False","False","True","False","0","False","","255","False","","255","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","False","True","1","False","","False","False","False","True","4","RenderPipeline=UniversalPipeline","RenderType=Opaque=RenderType","Queue=Geometry=Queue=0","UniversalMaterialType=Unlit","True","5","True","14","all","0","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","True","True","True","True","False","0","False","","False","False","False","False","False","False","False","True","False","0","False","","255","False","","255","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","False","True","2","True","","True","3","True","","True","True","0","False","","0","False","","False","True","1","LightMode=UniversalGBuffer","False","True","12","d3d11","gles","metal","vulkan","xboxone","xboxseries","playstation","ps4","ps5","switch","switch2","webgpu","0","","0","0","Standard","0","False","0"]}
{"type":"AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor","id":128,"pos":[616,-56],"params":["Inherit","False","1238","166","Auto Register Variables","0","Ge Lush was here! <3","0,0,0,1","0","0"]}
{"wire":[140,0,161,0]}
{"wire":[140,1,133,0]}
{"wire":[140,2,134,0]}
{"wire":[140,3,139,0]}
{"wire":[140,4,135,0]}
{"wire":[148,0,132,0]}
{"wire":[149,0,131,0]}
{"wire":[159,0,117,0]}
{"wire":[141,1,140,0]}
{"wire":[142,0,160,0]}
{"wire":[142,1,141,1]}
{"wire":[142,2,136,0]}
{"wire":[142,3,137,0]}
{"wire":[142,4,151,0]}
{"wire":[142,5,150,0]}
{"wire":[163,0,142,0]}
{"wire":[125,0,119,0]}
{"wire":[125,1,116,4]}
{"wire":[125,2,123,0]}
{"wire":[143,0,125,0]}
{"wire":[143,1,164,0]}
{"wire":[130,0,118,0]}
{"wire":[126,0,115,0]}
{"wire":[126,1,116,0]}
{"wire":[126,2,143,0]}
{"wire":[126,3,120,0]}
{"wire":[127,0,162,0]}
{"wire":[127,1,118,4]}
{"wire":[127,2,121,0]}
{"wire":[127,3,126,0]}
{"wire":[127,4,122,0]}
{"wire":[147,0,165,0]}
{"wire":[147,1,146,0]}
{"wire":[129,0,126,0]}
{"wire":[129,1,130,0]}
{"wire":[144,0,127,0]}
{"wire":[144,1,147,0]}
{"wire":[145,0,129,0]}
{"wire":[145,1,152,0]}
{"wire":[145,2,153,0]}
{"wire":[145,3,138,0]}
{"wire":[157,0,144,0]}
{"wire":[154,0,145,0]}
{"wire":[156,0,157,0]}
{"wire":[55,2,155,0]}
{"wire":[55,3,158,0]}
ASEEND*/
//CHKSM=F49EE3278B26218884F41340304A825EB544FB19