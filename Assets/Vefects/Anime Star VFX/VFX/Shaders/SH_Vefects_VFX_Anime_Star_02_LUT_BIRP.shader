// Made with Amplify Shader Editor v1.9.9.12
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Vefects/SH_Vefects_VFX_Anime_Star_02_LUT_BIRP"
{
	Properties
	{
		_EmissiveMult( "Emissive Mult", Float ) = 1
		_ErosionSmoothness( "Erosion Smoothness", Float ) = 1
		[Space(33)][Header(Ellipse)][Space(13)] _EllipseScale( "Ellipse Scale", Float ) = 1
		_EllipseSize( "Ellipse Size", Vector ) = ( 1, 1, 0, 0 )
		_EllipseCurvature( "Ellipse Curvature", Float ) = 1
		_EllipseRotation( "Ellipse Rotation", Float ) = 0
		_EllipseRotationSpeed( "Ellipse Rotation Speed", Float ) = 0
		[Toggle( _USEHUESHIFT_ON )] _UseHueShift( "Use Hue Shift", Float ) = 0
		_HueShift( "Hue Shift", Float ) = 0
		_HueShiftSpeed( "Hue Shift Speed", Float ) = 0
		_Desaturate( "Desaturate", Float ) = 0
		[Space(13)][Header(LUT)][Space(33)] _LUTTexture( "LUT Texture", 2D ) = "white" {}
		_LUTAmplitude( "LUT Amplitude", Float ) = 1
		_LUTSpeed( "LUT Speed", Float ) = 0
		_LUTOffset( "LUT Offset", Float ) = 0
		_LUTIntensity( "LUT Intensity", Float ) = 1
		_LUTErosionSmoothness( "LUT Erosion Smoothness", Float ) = 1
		[Space(33)][Header(Flicker)][Space(13)] _FlickerSpeedOverall( "Flicker Speed Overall", Float ) = 3.333
		_FlickerIntensity01( "Flicker Intensity 01", Float ) = 1
		_FlickerSpeed01( "Flicker Speed 01", Float ) = 7.731
		_FlickerIntensity02( "Flicker Intensity 02", Float ) = 1
		_FlickerSpeed02( "Flicker Speed 02", Float ) = 3.137
		_FlickerIntensity03( "Flicker Intensity 03", Float ) = 1
		_FlickerSpeed03( "Flicker Speed 03", Float ) = 0.373
		_FlickerMin( "Flicker Min", Float ) = 0.5
		_FlickerMax( "Flicker Max", Float ) = 1
		[Space(33)][Header(Depth Fade)][Space(13)] _DepthFade( "Depth Fade", Float ) = 0.01
		[Space(33)][Header(Camera Push)][Space(13)] _CameraPush( "Camera Push", Float ) = 0
		[Space(13)][Header(AR)][Space(13)] _Cull1( "Cull", Float ) = 2
		_Src1( "Src", Float ) = 1
		_Dst1( "Dst", Float ) = 1
		_ZWrite1( "ZWrite", Float ) = 0
		_ZTest1( "ZTest", Float ) = 2
		[Toggle( _ISADDITIVE_ON )] _IsAdditive( "IsAdditive", Float ) = 1

	}

	SubShader
	{
		

		

		Tags { "RenderType"="Transparent" "Queue"="Transparent" }

	LOD 0

		ZWrite On
		Cull Back
		AlphaToMask Off
		ColorMask RGBA
		Blend One Zero, One Zero
		BlendOp Add, Add

		

		Blend One Zero, One Zero
		BlendOp Add, Add
		

		CGINCLUDE
			#pragma target 3.5
			// ensure rendering platforms toggle list is visible

			float4 ComputeClipSpacePosition( float2 screenPosNorm, float deviceDepth )
			{
				float4 positionCS = float4( screenPosNorm * 2.0 - 1.0, deviceDepth, 1.0 );
			#if UNITY_UV_STARTS_AT_TOP
				positionCS.y = -positionCS.y;
			#endif
				return positionCS;
			}
		ENDCG

		
		Pass
		{
			
			Name "Unlit"
			Tags { "LightMode"="ForwardBase" }

			Cull [_Cull1]
			ZWrite [_ZWrite1]
			ZTest [_ZTest1]
			Offset 0 , 0
			ColorMask RGBA
			Blend [_Src1] [_Dst1], One OneMinusSrcAlpha
			BlendOp Add, Add

			

			CGPROGRAM
				#define ASE_SURFACE_TRANSPARENT
				#define ASE_VERSION 19912

				#pragma vertex vert
				#pragma fragment frag
				#pragma multi_compile_instancing
				#include "UnityCG.cginc"

				#include "UnityStandardBRDF.cginc"
				#include "UnityShaderVariables.cginc"
				#define ASE_NEEDS_VERT_POSITION
				#define ASE_NEEDS_FRAG_COLOR
				#define ASE_NEEDS_TEXTURE_COORDINATES0
				#define ASE_NEEDS_FRAG_TEXTURE_COORDINATES0
				#define ASE_NEEDS_TEXTURE_COORDINATES1
				#define ASE_NEEDS_FRAG_TEXTURE_COORDINATES1
				#define ASE_NEEDS_FRAG_SCREEN_POSITION_NORMALIZED
				#pragma shader_feature_local _ISADDITIVE_ON
				#pragma shader_feature_local _USEHUESHIFT_ON


				#if defined(ASE_WRITE_DEPTH_CONSERVATIVE) && (SHADER_TARGET >= 45)
					#define ASE_SV_DEPTH SV_DepthLessEqual
					#define ASE_SV_POSITION_QUALIFIERS linear noperspective centroid
				#else
					#define ASE_SV_DEPTH SV_Depth
					#define ASE_SV_POSITION_QUALIFIERS
				#endif

				struct appdata
				{
					float4 vertex : POSITION;
					float3 normal : NORMAL;
					float4 tangent : TANGENT;
					float4 ase_color : COLOR;
					float4 ase_texcoord : TEXCOORD0;
					float4 ase_texcoord1 : TEXCOORD1;
					UNITY_VERTEX_INPUT_INSTANCE_ID
				};

				struct v2f
				{
					ASE_SV_POSITION_QUALIFIERS float4 pos : SV_POSITION;
					float4 ase_color : COLOR;
					float4 ase_texcoord : TEXCOORD0;
					float4 ase_texcoord1 : TEXCOORD1;
					UNITY_VERTEX_INPUT_INSTANCE_ID
					UNITY_VERTEX_OUTPUT_STEREO
				};

				uniform float _Cull1;
				uniform float _ZTest1;
				uniform float _ZWrite1;
				uniform float _Src1;
				uniform float _Dst1;
				uniform float _CameraPush;
				uniform sampler2D _LUTTexture;
				uniform float _LUTSpeed;
				uniform float _LUTErosionSmoothness;
				uniform float _EllipseScale;
				uniform float _EllipseRotation;
				uniform float _EllipseRotationSpeed;
				uniform float2 _EllipseSize;
				uniform float _EllipseCurvature;
				uniform float _LUTAmplitude;
				uniform float _LUTOffset;
				uniform float _HueShift;
				uniform float _HueShiftSpeed;
				uniform float _Desaturate;
				uniform float _LUTIntensity;
				uniform float _ErosionSmoothness;
				UNITY_DECLARE_DEPTH_TEXTURE( _CameraDepthTexture );
				uniform float4 _CameraDepthTexture_TexelSize;
				uniform float _DepthFade;
				uniform float _EmissiveMult;
				uniform float _FlickerMin;
				uniform float _FlickerMax;
				uniform float _FlickerSpeed01;
				uniform float _FlickerSpeedOverall;
				uniform float _FlickerIntensity01;
				uniform float _FlickerSpeed02;
				uniform float _FlickerIntensity02;
				uniform float _FlickerSpeed03;
				uniform float _FlickerIntensity03;


				float3 ASESafeNormalize(float3 inVec)
				{
					float dp3 = max(1.175494351e-38, dot(inVec, inVec));
					return inVec* rsqrt(dp3);
				}
				
				float3 HSVToRGB( float3 c )
				{
					float4 K = float4( 1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0 );
					float3 p = abs( frac( c.xxx + K.xyz ) * 6.0 - K.www );
					return c.z * lerp( K.xxx, saturate( p - K.xxx ), c.y );
				}
				
				float3 RGBToHSV(float3 c)
				{
					float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
					float4 p = lerp( float4( c.bg, K.wz ), float4( c.gb, K.xy ), step( c.b, c.g ) );
					float4 q = lerp( float4( p.xyw, c.r ), float4( c.r, p.yzx ), step( p.x, c.r ) );
					float d = q.x - min( q.w, q.y );
					float e = 1.0e-10;
					return float3( abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
				}

				v2f vert( appdata v  )
				{
					UNITY_SETUP_INSTANCE_ID(v);
					v2f o;
					UNITY_INITIALIZE_OUTPUT(v2f,o);
					UNITY_TRANSFER_INSTANCE_ID(v,o);
					UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

					float3 ase_positionWS = mul( unity_ObjectToWorld, float4( ( v.vertex ).xyz, 1 ) ).xyz;
					float3 ase_viewVectorOS = mul( ( float3x3 )unity_WorldToObject, ( ( unity_OrthoParams.w == 0 ) ? _WorldSpaceCameraPos - ase_positionWS : UNITY_MATRIX_V[ 2 ].xyz ) );
					float3 ase_viewDirSafeOS = Unity_SafeNormalize( ase_viewVectorOS );
					float3 normalizeResult168 = ASESafeNormalize( ase_viewDirSafeOS );
					float3 vertexPos171 = v.vertex.xyz;
					float4 ase_positionCS171 = UnityObjectToClipPos( vertexPos171 );
					float depthLinearEye171 = LinearEyeDepth( ase_positionCS171.z / ase_positionCS171.w );
					float3 WPO174 = ( normalizeResult168 * min( ( depthLinearEye171 - 0.05 ), _CameraPush ) );
					
					o.ase_color = v.ase_color;
					o.ase_texcoord = v.ase_texcoord;
					o.ase_texcoord1 = v.ase_texcoord1;

					#ifdef ASE_ABSOLUTE_VERTEX_POS
						float3 defaultVertexValue = v.vertex.xyz;
					#else
						float3 defaultVertexValue = float3(0, 0, 0);
					#endif
					float3 vertexValue = WPO174;
					#ifdef ASE_ABSOLUTE_VERTEX_POS
						v.vertex.xyz = vertexValue;
					#else
						v.vertex.xyz += vertexValue;
					#endif
					v.vertex.w = 1;
					v.normal = v.normal;
					v.tangent = v.tangent;

					o.pos = UnityObjectToClipPos( v.vertex );

					#if defined( ASE_SHADOWS )
						UNITY_TRANSFER_SHADOW( o, v.texcoord );
					#endif
					return o;
				}

				half4 frag( v2f IN 
							#if defined( ASE_WRITE_DEPTH )
								, out float outputDepth : SV_Depth
							#endif
				) : SV_Target
				{
					UNITY_SETUP_INSTANCE_ID( IN );
					UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( IN );

					float4 ScreenPosNorm = float4( IN.pos.xy * ( _ScreenParams.zw - 1.0 ), IN.pos.zw );
					float4 ClipPos = ComputeClipSpacePosition( ScreenPosNorm.xy, IN.pos.z ) * IN.pos.w;
					float4 ScreenPos = ComputeScreenPos( ClipPos );

					float2 temp_cast_0 = (_LUTSpeed).xx;
					float VTC_Eros72 = IN.ase_texcoord.z;
					float2 texCoord111 = IN.ase_texcoord.xy * float2( 1,1 ) + float2( 0,0 );
					float mulTime116 = _Time.y * _EllipseRotationSpeed;
					float cos112 = cos( ( _EllipseRotation + mulTime116 ) );
					float sin112 = sin( ( _EllipseRotation + mulTime116 ) );
					float2 rotator112 = mul( texCoord111 - float2( 0.5,0.5 ) , float2x2( cos112 , -sin112 , sin112 , cos112 )) + float2( 0.5,0.5 );
					float2 appendResult128 = (float2( (-1.0 + ( (rotator112).x - 0.0 ) * ( 1.0 - -1.0 ) / ( 1.0 - 0.0 ) ) ,  (-1.0 + ( (rotator112).y - 0.0 ) * ( 1.0 - -1.0 ) / ( 1.0 - 0.0 ) )));
					float VTC_Curvature155 = IN.ase_texcoord1.x;
					float temp_output_153_0 = ( ( VTC_Curvature155 + _EllipseCurvature ) + 1.0 );
					float ifLocalVar140 = 0;
					if( _EllipseScale <= 0.0 )
					ifLocalVar140 = 0.0;
					else
					ifLocalVar140 = saturate( ( 1.0 - ( ( pow( abs( ( (appendResult128).x / _EllipseSize.x ) ) , temp_output_153_0 ) + pow( abs( ( (appendResult128).y / _EllipseSize.y ) ) , temp_output_153_0 ) ) * ( 1.0 / _EllipseScale ) ) ) );
					float Out_Ellipse141 = ifLocalVar140;
					float smoothstepResult194 = smoothstep( VTC_Eros72 , ( VTC_Eros72 + _LUTErosionSmoothness ) , Out_Ellipse141);
					float VTC_Random_Hue_Offset103 = IN.ase_texcoord1.z;
					float2 temp_cast_1 = (( ( saturate( smoothstepResult194 ) * _LUTAmplitude ) + ( _LUTOffset + VTC_Random_Hue_Offset103 ) )).xx;
					float2 panner182 = ( 1.0 * _Time.y * temp_cast_0 + temp_cast_1);
					float3 hsvTorgb85 = RGBToHSV( tex2D( _LUTTexture, panner182 ).rgb );
					float2 temp_cast_3 = (hsvTorgb85.x).xx;
					float mulTime95 = _Time.y * _HueShiftSpeed;
					float2 texCoord104 = IN.ase_texcoord.xy * float2( 1,1 ) + float2( 0,0 );
					float VTC_Emission107 = IN.ase_texcoord1.y;
					#ifdef _USEHUESHIFT_ON
					float2 staticSwitch110 = ( ( hsvTorgb85.x + _HueShift ) + ( mulTime95 + ( VTC_Random_Hue_Offset103 + ( texCoord104 * VTC_Emission107 ) ) ) );
					#else
					float2 staticSwitch110 = temp_cast_3;
					#endif
					float3 hsvTorgb88 = HSVToRGB( float3(staticSwitch110.x,( hsvTorgb85.y * ( 1.0 - saturate( _Desaturate ) ) ),hsvTorgb85.z) );
					float4 lerpResult51 = lerp( IN.ase_color , ( IN.ase_color * float4( hsvTorgb88 , 0.0 ) ) , _LUTIntensity);
					float smoothstepResult77 = smoothstep( VTC_Eros72 , ( VTC_Eros72 + _ErosionSmoothness ) , Out_Ellipse141);
					float screenDepth41 = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE( _CameraDepthTexture, ScreenPosNorm.xy ));
					float distanceDepth41 = saturate( ( screenDepth41 - LinearEyeDepth( ScreenPosNorm.z ) ) / ( _DepthFade ) );
					float OP65 = saturate( ( saturate( ( IN.ase_color.a * saturate( smoothstepResult77 ) ) ) * saturate( distanceDepth41 ) ) );
					#ifdef _ISADDITIVE_ON
					float4 staticSwitch69 = ( lerpResult51 * OP65 );
					#else
					float4 staticSwitch69 = lerpResult51;
					#endif
					float mulTime202 = _Time.y * ( _FlickerSpeed01 * _FlickerSpeedOverall );
					float VTC_Random_Offset73 = IN.ase_texcoord.w;
					float mulTime203 = _Time.y * ( _FlickerSpeed02 * _FlickerSpeedOverall );
					float mulTime223 = _Time.y * ( _FlickerSpeed03 * _FlickerSpeedOverall );
					float lerpResult216 = lerp( _FlickerMin , _FlickerMax , saturate( ( ( ( sin( ( mulTime202 + VTC_Random_Offset73 ) ) * _FlickerIntensity01 ) * ( cos( ( mulTime203 + VTC_Random_Offset73 ) ) * _FlickerIntensity02 ) ) + ( sin( ( mulTime223 + VTC_Random_Offset73 ) ) * _FlickerIntensity03 ) ) ));
					float Flick219 = lerpResult216;
					float4 Col67 = ( staticSwitch69 * ( ( _EmissiveMult * VTC_Emission107 ) * Flick219 ) );
					

					float3 Color = Col67.rgb;
					float Alpha = OP65;
					half AlphaClipThreshold = 0.5;
					half AlphaClipThresholdShadow = 0.5;

					#if defined( ASE_WRITE_DEPTH )
						outputDepth = IN.pos.z;
					#endif

					#ifdef _ALPHATEST_ON
						clip( Alpha - AlphaClipThreshold );
					#endif

				#if defined( ASE_SURFACE_TRANSPARENT ) || defined( ASE_OPAQUE_KEEP_ALPHA )
					return half4( Color, Alpha );
				#else
					return half4( Color, 1.0 );
				#endif
				}
			ENDCG
		}

		
		Pass
		{
			
			Name "ShadowCaster"
			Tags { "LightMode"="ShadowCaster" }

			ZWrite On
			ZTest LEqual
			AlphaToMask Off

			CGPROGRAM
				#define ASE_SURFACE_TRANSPARENT
				#define ASE_VERSION 19912

				#pragma vertex vert
				#pragma fragment frag
				#pragma multi_compile_shadowcaster
				#ifndef UNITY_PASS_SHADOWCASTER
					#define UNITY_PASS_SHADOWCASTER
				#endif
				#include "UnityCG.cginc"

				#include "UnityStandardBRDF.cginc"
				#include "UnityShaderVariables.cginc"
				#define ASE_NEEDS_VERT_POSITION
				#define ASE_NEEDS_TEXTURE_COORDINATES0
				#define ASE_NEEDS_FRAG_TEXTURE_COORDINATES0
				#define ASE_NEEDS_TEXTURE_COORDINATES1
				#define ASE_NEEDS_FRAG_TEXTURE_COORDINATES1


				#if defined(ASE_WRITE_DEPTH_CONSERVATIVE) && (SHADER_TARGET >= 45)
					#define ASE_SV_DEPTH SV_DepthLessEqual
					#define ASE_SV_POSITION_QUALIFIERS linear noperspective centroid
				#else
					#define ASE_SV_DEPTH SV_Depth
					#define ASE_SV_POSITION_QUALIFIERS
				#endif

				struct appdata
				{
					float4 vertex : POSITION;
					float3 normal : NORMAL;
					float4 tangent : TANGENT;
					float4 ase_color : COLOR;
					float4 ase_texcoord : TEXCOORD0;
					float4 ase_texcoord1 : TEXCOORD1;
					UNITY_VERTEX_INPUT_INSTANCE_ID
				};

				struct v2f
				{
					ASE_SV_POSITION_QUALIFIERS UNITY_POSITION( pos );
					V2F_SHADOW_CASTER_NOPOS
					float4 ase_color : COLOR;
					float4 ase_texcoord1 : TEXCOORD1;
					float4 ase_texcoord2 : TEXCOORD2;
					float4 ase_texcoord3 : TEXCOORD3;
					UNITY_VERTEX_INPUT_INSTANCE_ID
					UNITY_VERTEX_OUTPUT_STEREO
				};

				#ifdef UNITY_STANDARD_USE_DITHER_MASK
					sampler3D _DitherMaskLOD;
				#endif

				uniform float _Cull1;
				uniform float _ZTest1;
				uniform float _ZWrite1;
				uniform float _Src1;
				uniform float _Dst1;
				uniform float _CameraPush;
				uniform float _ErosionSmoothness;
				uniform float _EllipseScale;
				uniform float _EllipseRotation;
				uniform float _EllipseRotationSpeed;
				uniform float2 _EllipseSize;
				uniform float _EllipseCurvature;
				UNITY_DECLARE_DEPTH_TEXTURE( _CameraDepthTexture );
				uniform float4 _CameraDepthTexture_TexelSize;
				uniform float _DepthFade;


				float3 ASESafeNormalize(float3 inVec)
				{
					float dp3 = max(1.175494351e-38, dot(inVec, inVec));
					return inVec* rsqrt(dp3);
				}
				

				v2f vert( appdata v  )
				{
					UNITY_SETUP_INSTANCE_ID( v );
					v2f o;
					UNITY_INITIALIZE_OUTPUT( v2f, o );
					UNITY_TRANSFER_INSTANCE_ID( v, o );
					UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO( o );

					float3 ase_positionWS = mul( unity_ObjectToWorld, float4( ( v.vertex ).xyz, 1 ) ).xyz;
					float3 ase_viewVectorOS = mul( ( float3x3 )unity_WorldToObject, ( ( unity_OrthoParams.w == 0 ) ? _WorldSpaceCameraPos - ase_positionWS : UNITY_MATRIX_V[ 2 ].xyz ) );
					float3 ase_viewDirSafeOS = Unity_SafeNormalize( ase_viewVectorOS );
					float3 normalizeResult168 = ASESafeNormalize( ase_viewDirSafeOS );
					float3 vertexPos171 = v.vertex.xyz;
					float4 ase_positionCS171 = UnityObjectToClipPos( vertexPos171 );
					float depthLinearEye171 = LinearEyeDepth( ase_positionCS171.z / ase_positionCS171.w );
					float3 WPO174 = ( normalizeResult168 * min( ( depthLinearEye171 - 0.05 ), _CameraPush ) );
					
					float4 ase_positionCS = UnityObjectToClipPos( v.vertex );
					float4 screenPos = ComputeScreenPos( ase_positionCS );
					o.ase_texcoord3 = screenPos;
					
					o.ase_color = v.ase_color;
					o.ase_texcoord1 = v.ase_texcoord;
					o.ase_texcoord2 = v.ase_texcoord1;

					#ifdef ASE_ABSOLUTE_VERTEX_POS
						float3 defaultVertexValue = v.vertex.xyz;
					#else
						float3 defaultVertexValue = float3(0, 0, 0);
					#endif
					float3 vertexValue = WPO174;
					#ifdef ASE_ABSOLUTE_VERTEX_POS
						v.vertex.xyz = vertexValue;
					#else
						v.vertex.xyz += vertexValue;
					#endif
					v.vertex.w = 1;
					v.normal = v.normal;
					v.tangent = v.tangent;

					TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
					return o;
				}

				half4 frag( v2f IN 
							#if defined( ASE_WRITE_DEPTH )
								, out float outputDepth : SV_Depth
							#endif
							) : SV_Target
				{
					UNITY_SETUP_INSTANCE_ID(IN);

					#ifdef LOD_FADE_CROSSFADE
						UNITY_APPLY_DITHER_CROSSFADE(IN.pos.xy);
					#endif

					float VTC_Eros72 = IN.ase_texcoord1.z;
					float2 texCoord111 = IN.ase_texcoord1.xy * float2( 1,1 ) + float2( 0,0 );
					float mulTime116 = _Time.y * _EllipseRotationSpeed;
					float cos112 = cos( ( _EllipseRotation + mulTime116 ) );
					float sin112 = sin( ( _EllipseRotation + mulTime116 ) );
					float2 rotator112 = mul( texCoord111 - float2( 0.5,0.5 ) , float2x2( cos112 , -sin112 , sin112 , cos112 )) + float2( 0.5,0.5 );
					float2 appendResult128 = (float2( (-1.0 + ( (rotator112).x - 0.0 ) * ( 1.0 - -1.0 ) / ( 1.0 - 0.0 ) ) ,  (-1.0 + ( (rotator112).y - 0.0 ) * ( 1.0 - -1.0 ) / ( 1.0 - 0.0 ) )));
					float VTC_Curvature155 = IN.ase_texcoord2.x;
					float temp_output_153_0 = ( ( VTC_Curvature155 + _EllipseCurvature ) + 1.0 );
					float ifLocalVar140 = 0;
					if( _EllipseScale <= 0.0 )
					ifLocalVar140 = 0.0;
					else
					ifLocalVar140 = saturate( ( 1.0 - ( ( pow( abs( ( (appendResult128).x / _EllipseSize.x ) ) , temp_output_153_0 ) + pow( abs( ( (appendResult128).y / _EllipseSize.y ) ) , temp_output_153_0 ) ) * ( 1.0 / _EllipseScale ) ) ) );
					float Out_Ellipse141 = ifLocalVar140;
					float smoothstepResult77 = smoothstep( VTC_Eros72 , ( VTC_Eros72 + _ErosionSmoothness ) , Out_Ellipse141);
					float4 screenPos = IN.ase_texcoord3;
					float4 ase_positionSSNorm = screenPos / screenPos.w;
					ase_positionSSNorm.z = ( UNITY_NEAR_CLIP_VALUE >= 0 ) ? ase_positionSSNorm.z : ase_positionSSNorm.z * 0.5 + 0.5;
					float screenDepth41 = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE( _CameraDepthTexture, ase_positionSSNorm.xy ));
					float distanceDepth41 = saturate( ( screenDepth41 - LinearEyeDepth( ase_positionSSNorm.z ) ) / ( _DepthFade ) );
					float OP65 = saturate( ( saturate( ( IN.ase_color.a * saturate( smoothstepResult77 ) ) ) * saturate( distanceDepth41 ) ) );
					

					float Alpha = OP65;
					half AlphaClipThreshold = 0.5;
					half AlphaClipThresholdShadow = 0.5;

					#if defined( ASE_WRITE_DEPTH )
						outputDepth = IN.pos.z;
					#endif

					#ifdef _ALPHATEST_SHADOW_ON
						if (unity_LightShadowBias.z != 0.0)
							clip(Alpha - AlphaClipThresholdShadow);
						#ifdef _ALPHATEST_ON
						else
							clip(Alpha - AlphaClipThreshold);
						#endif
					#else
						#ifdef _ALPHATEST_ON
							clip(Alpha - AlphaClipThreshold);
						#endif
					#endif

					#ifdef UNITY_STANDARD_USE_DITHER_MASK
						half alphaRef = tex3D(_DitherMaskLOD, float3(IN.pos.xy*0.25,Alpha*0.9375)).a;
						clip(alphaRef - 0.01);
					#endif

					SHADOW_CASTER_FRAGMENT(IN)
				}
			ENDCG
		}
		
	}
	CustomEditor "AmplifyShaderEditor.MaterialInspector"
	
	Fallback Off
}
/*ASEBEGIN
Version=19912
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":114,"pos":[-8576,-3584],"params":["Inherit","False","Property","_EllipseRotationSpeed","Ellipse Rotation Speed","6","0","Create","True","0","0","0","False","0","False","Object","-1","","0","0","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":113,"pos":[-8576,-3712],"params":["Inherit","False","Property","_EllipseRotation","Ellipse Rotation","5","0","Create","True","0","0","0","False","0","False","Object","-1","","0","0","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleTimeNode, AmplifyShaderEditor","id":116,"pos":[-8192,-3584],"params":["Inherit","False","1","0","FLOAT","1","False","5","FLOAT","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor","id":117,"pos":[-7936,-3712],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.TextureCoordinatesNode, AmplifyShaderEditor","id":111,"pos":[-8576,-3968],"params":["Inherit","False","0","-1","2","3","2","SAMPLER2D","","False","0","FLOAT2","1,1","False","1","FLOAT2","0,0","False","5","FLOAT2","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.RotatorNode, AmplifyShaderEditor","id":112,"pos":[-7808,-3968],"params":["Inherit","False","3","0","FLOAT2","0,0","False","1","FLOAT2","0.5,0.5","False","2","FLOAT","1","False","1","FLOAT2","0"]}
{"type":"AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor","id":99,"pos":[-2226,-1202],"params":["Inherit","False","684","578.0001","VTC","7","26","72","73","102","103","107","155","VTC","0,0,0,1","0","0"]}
{"type":"AmplifyShaderEditor.ComponentMaskNode, AmplifyShaderEditor","id":121,"pos":[-7552,-3968],"params":["Inherit","False","True","False","False","False","1","0","FLOAT2","0,0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.ComponentMaskNode, AmplifyShaderEditor","id":122,"pos":[-7552,-3840],"params":["Inherit","False","False","True","False","False","1","0","FLOAT2","0,0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":124,"pos":[-7296,-4352],"params":["Inherit","False","Constant","_Float0","Float 0","23","0","Create","True","0","0","0","False","0","False","Object","-1","","0","0","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":125,"pos":[-7296,-4224],"params":["Inherit","False","Constant","_Float0","Float 0","23","0","Create","True","0","0","0","False","0","False","Object","-1","","1","0","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":126,"pos":[-7296,-4096],"params":["Inherit","False","Constant","_Float0","Float 0","23","0","Create","True","0","0","0","False","0","False","Object","-1","","-1","0","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.TexCoordVertexDataNode, AmplifyShaderEditor","id":102,"pos":[-2176,-896],"params":["Inherit","False","1","4","0","5","FLOAT4","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.TFHCRemapNode, AmplifyShaderEditor","id":123,"pos":[-7296,-3968],"params":["Inherit","False","5","0","FLOAT","0","False","1","FLOAT","0","False","2","FLOAT","1","False","3","FLOAT","0","False","4","FLOAT","1","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.TFHCRemapNode, AmplifyShaderEditor","id":127,"pos":[-7296,-3712],"params":["Inherit","False","5","0","FLOAT","0","False","1","FLOAT","0","False","2","FLOAT","1","False","3","FLOAT","0","False","4","FLOAT","1","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.DynamicAppendNode, AmplifyShaderEditor","id":128,"pos":[-7040,-3968],"params":["Inherit","False","FLOAT2","4","0","FLOAT","0","False","1","FLOAT","0","False","2","FLOAT","0","False","3","FLOAT","0","False","1","FLOAT2","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":155,"pos":[-1920,-896],"params":["Inherit","False","VTC Curvature","-1","True","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.ComponentMaskNode, AmplifyShaderEditor","id":129,"pos":[-6784,-3968],"params":["Inherit","False","True","False","False","False","1","0","FLOAT2","0,0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.ComponentMaskNode, AmplifyShaderEditor","id":130,"pos":[-6784,-3840],"params":["Inherit","False","False","True","False","False","1","0","FLOAT2","0,0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":156,"pos":[-6016,-4352],"params":["Inherit","False","155","VTC Curvature","1","0","OBJECT","","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":159,"pos":[-6016,-4224],"params":["Inherit","False","Property","_EllipseCurvature","Ellipse Curvature","4","0","Create","True","0","0","0","False","0","False","Object","-1","","1","1","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.Vector2Node, AmplifyShaderEditor","id":160,"pos":[-6016,-3456],"params":["Inherit","False","Property","_EllipseSize","Ellipse Size","3","0","Create","True","0","0","0","False","0","False","Object","-1","","1,1","1,1","0","3","FLOAT2","0","FLOAT","1","FLOAT","2"]}
{"type":"AmplifyShaderEditor.SimpleDivideOpNode, AmplifyShaderEditor","id":149,"pos":[-4992,-3968],"params":["Inherit","False","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleDivideOpNode, AmplifyShaderEditor","id":151,"pos":[-4992,-3712],"params":["Inherit","False","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":157,"pos":[-5504,-4224],"params":["Inherit","False","Constant","_Float0","Float 0","26","0","Create","True","0","0","0","False","0","False","Object","-1","","1","0","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor","id":154,"pos":[-5760,-4352],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.AbsOpNode, AmplifyShaderEditor","id":152,"pos":[-4864,-3712],"params":["Inherit","False","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.AbsOpNode, AmplifyShaderEditor","id":150,"pos":[-4864,-3968],"params":["Inherit","False","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor","id":153,"pos":[-5504,-4352],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":135,"pos":[-3712,-3840],"params":["Inherit","False","Constant","_Float0","Float 0","24","0","Create","True","0","0","0","False","0","False","Object","-1","","1","0","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.PowerNode, AmplifyShaderEditor","id":147,"pos":[-4608,-3968],"params":["Inherit","False","False","2","0","FLOAT","0","False","1","FLOAT","1","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.PowerNode, AmplifyShaderEditor","id":148,"pos":[-4608,-3712],"params":["Inherit","False","False","2","0","FLOAT","0","False","1","FLOAT","1","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleDivideOpNode, AmplifyShaderEditor","id":134,"pos":[-3456,-3840],"params":["Inherit","False","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor","id":146,"pos":[-3968,-3968],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":136,"pos":[-3456,-3968],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.OneMinusNode, AmplifyShaderEditor","id":189,"pos":[-2688,-3968],"params":["Inherit","False","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":131,"pos":[-3712,-3200],"params":["Inherit","False","Property","_EllipseScale","Ellipse Scale","2","0","Create","True","0","0","0","False","3","Space(33)","Header(Ellipse)","Space(13)","False","Object","-1","","1","1","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor","id":190,"pos":[-2432,-3968],"params":["Inherit","False","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":143,"pos":[-2048,-3712],"params":["Inherit","False","Constant","_Float0","Float 0","26","0","Create","True","0","0","0","False","0","False","Object","-1","","0","0","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.ConditionalIfNode, AmplifyShaderEditor","id":140,"pos":[-2048,-3968],"params":["Inherit","False","False","5","0","FLOAT","0","False","1","FLOAT","0","False","2","FLOAT","0","False","3","FLOAT","0","False","4","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.TexCoordVertexDataNode, AmplifyShaderEditor","id":26,"pos":[-2176,-1152],"params":["Inherit","False","0","4","0","5","FLOAT4","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":141,"pos":[-1536,-3968],"params":["Inherit","False","Out Ellipse","-1","True","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":72,"pos":[-1920,-1152],"params":["Inherit","False","VTC Eros","-1","True","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":28,"pos":[-5632,1024],"params":["Inherit","False","Property","_ErosionSmoothness","Erosion Smoothness","1","0","Create","True","0","0","0","False","0","False","Object","-1","","1","1","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":142,"pos":[-5632,640],"params":["Inherit","False","141","Out Ellipse","1","0","OBJECT","","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor","id":80,"pos":[-5376,896],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":79,"pos":[-5632,896],"params":["Inherit","False","72","VTC Eros","1","0","OBJECT","","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor","id":176,"pos":[-2610,1870],"params":["Inherit","False","1572","547","Camera Push","9","167","168","170","171","172","173","169","174","175","Camera Push","0,0,0,1","0","0"]}
{"type":"AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor","id":1,"pos":[-3728,640],"params":["Inherit","False","916.5662","697.3394","Depth Fade","5","45","44","41","38","64","","0,0,0,1","0","0"]}
{"type":"AmplifyShaderEditor.SmoothstepOpNode, AmplifyShaderEditor","id":77,"pos":[-5248,768],"params":["Inherit","False","3","0","FLOAT","0","False","1","FLOAT","0","False","2","FLOAT","1","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.VertexColorNode, AmplifyShaderEditor","id":35,"pos":[-6016,0],"params":["Inherit","False","0","5","COLOR","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":38,"pos":[-3584,1152],"params":["Inherit","False","Property","_DepthFade","Depth Fade","30","0","Create","True","0","0","0","False","3","Space(33)","Header(Depth Fade)","Space(13)","False","Object","-1","","0.01","0.01","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.PosVertexDataNode, AmplifyShaderEditor","id":170,"pos":[-2560,2176],"params":["Inherit","False","0","0","5","FLOAT3","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor","id":34,"pos":[-4992,768],"params":["Inherit","False","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.DepthFade, AmplifyShaderEditor","id":41,"pos":[-3584,1024],"params":["Inherit","False","True","True","False","2","1","FLOAT3","0,0,0","False","0","FLOAT","1","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":39,"pos":[-4736,768],"params":["Inherit","True","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SurfaceDepthNode, AmplifyShaderEditor","id":171,"pos":[-2304,2176],"params":["Inherit","False","0","1","0","FLOAT3","0,0,0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor","id":78,"pos":[-4480,768],"params":["Inherit","False","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor","id":64,"pos":[-3328,1024],"params":["Inherit","False","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.ViewDirInputsCoordNode, AmplifyShaderEditor","id":167,"pos":[-2560,1920],"params":["Inherit","False","Object","True","0","4","FLOAT3","0","FLOAT","1","FLOAT","2","FLOAT","3"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":175,"pos":[-1792,2304],"params":["Inherit","False","Property","_CameraPush","Camera Push","31","0","Create","True","0","0","0","False","3","Space(33)","Header(Camera Push)","Space(13)","False","Object","-1","","0","0","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleSubtractOpNode, AmplifyShaderEditor","id":172,"pos":[-2048,2176],"params":["Inherit","False","2","0","FLOAT","0","False","1","FLOAT","0.05","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":44,"pos":[-3328,768],"params":["Inherit","True","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.NormalizeNode, AmplifyShaderEditor","id":168,"pos":[-2304,1920],"params":["Inherit","False","True","1","0","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.SimpleMinOpNode, AmplifyShaderEditor","id":173,"pos":[-1792,2176],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor","id":45,"pos":[-3072,768],"params":["Inherit","True","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":169,"pos":[-1536,1920],"params":["Inherit","False","2","2","0","FLOAT3","0,0,0","False","1","FLOAT","0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor","id":2,"pos":[640,0],"params":["Inherit","False","1238","166","Auto Register Variables","5","7","6","5","4","3","Ge Lush was here! <3","0,0,0,1","0","0"]}
{"type":"AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor","id":196,"pos":[-3888,2768],"params":["Inherit","False","3537.494","1537.313","Flicker","31","228","227","226","225","224","223","222","219","218","217","216","215","214","213","212","211","210","209","208","207","206","205","204","203","202","201","200","199","198","197","229","Flicker","0,0,0,1","0","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":174,"pos":[-1280,1920],"params":["Inherit","False","WPO","-1","True","1","0","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":65,"pos":[-2688,768],"params":["Inherit","False","OP","-1","True","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.TextureCoordinatesNode, AmplifyShaderEditor","id":8,"pos":[-8960,1024],"params":["Inherit","False","0","-1","2","3","2","SAMPLER2D","","False","0","FLOAT2","1,1","False","1","FLOAT2","0,0","False","5","FLOAT2","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.Vector2Node, AmplifyShaderEditor","id":9,"pos":[-8704,1152],"params":["Inherit","False","Property","_UVDScale","UVD Scale","19","0","Create","True","0","0","0","False","0","False","Object","-1","","1,1","1,1","0","3","FLOAT2","0","FLOAT","1","FLOAT","2"]}
{"type":"AmplifyShaderEditor.Vector2Node, AmplifyShaderEditor","id":11,"pos":[-8448,1152],"params":["Inherit","False","Property","_UVDPan","UVD Pan","18","0","Create","True","0","0","0","False","0","False","Object","-1","","0,0","1,1","0","3","FLOAT2","0","FLOAT","1","FLOAT","2"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":10,"pos":[-8704,1024],"params":["Inherit","False","2","2","0","FLOAT2","0,0","False","1","FLOAT2","0,0","False","1","FLOAT2","0"]}
{"type":"AmplifyShaderEditor.PannerNode, AmplifyShaderEditor","id":12,"pos":[-8448,1024],"params":["Inherit","False","3","0","FLOAT2","0,0","False","2","FLOAT2","0,0","False","1","FLOAT","1","False","1","FLOAT2","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":75,"pos":[-7936,1152],"params":["Inherit","False","73","VTC Random Offset","1","0","OBJECT","","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor","id":74,"pos":[-7936,1024],"params":["Inherit","False","2","2","0","FLOAT2","0,0","False","1","FLOAT","0","False","1","FLOAT2","0"]}
{"type":"AmplifyShaderEditor.SamplerNode, AmplifyShaderEditor","id":13,"pos":[-7552,1024],"params":["Inherit","True","Property","_MainTexture1","UVD Tex","17","0","Create","True","0","0","0","False","3","Space(33)","Header(Distortion Texture)","Space(33)","False","","-1","ad3d33bcd25b0b6409e4f8c40928b06d","ad3d33bcd25b0b6409e4f8c40928b06d","True","0","False","white","Auto","False","Object","-1","Auto","Texture2D","False","8","0","SAMPLER2D","","False","1","FLOAT2","0,0","False","2","FLOAT","0","False","3","FLOAT2","0,0","False","4","FLOAT2","0,0","False","5","FLOAT","1","False","6","FLOAT","0","False","7","SAMPLERSTATE","","False","6","COLOR","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4","FLOAT3","5"]}
{"type":"AmplifyShaderEditor.ComponentMaskNode, AmplifyShaderEditor","id":14,"pos":[-7168,1024],"params":["Inherit","False","True","True","False","False","1","0","COLOR","0,0,0,0","False","1","FLOAT2","0"]}
{"type":"AmplifyShaderEditor.Vector2Node, AmplifyShaderEditor","id":17,"pos":[-6656,896],"params":["Inherit","False","Constant","_Vector0","Vector 0","8","0","Create","True","0","0","0","False","0","False","Object","-1","","0,0","0,0","0","3","FLOAT2","0","FLOAT","1","FLOAT","2"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":20,"pos":[-6400,1024],"params":["Inherit","False","Property","_UVDLerp","UVD Lerp","20","0","Create","True","0","0","0","False","0","False","Object","-1","","0","0.05","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.FunctionNode, AmplifyShaderEditor","id":19,"pos":[-6912,1024],"params":["Inherit","False","ConstantBiasScale","-1","","1","63208df05c83e8e49a48ffbdce2e43a0","0","3","3","FLOAT2","0,0","False","1","FLOAT","-0.5","False","2","FLOAT","2","False","1","FLOAT2","0"]}
{"type":"AmplifyShaderEditor.LerpOp, AmplifyShaderEditor","id":22,"pos":[-6400,896],"params":["Inherit","False","3","0","FLOAT2","0,0","False","1","FLOAT2","0,0","False","2","FLOAT","0","False","1","FLOAT2","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":7,"pos":[688,48],"params":["Inherit","False","Property","_Cull1","Cull","32","0","Create","True","0","0","0","True","3","Space(13)","Header(AR)","Space(13)","False","Object","-1","","2","2","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":3,"pos":[1712,48],"params":["Inherit","False","Property","_ZTest1","ZTest","36","0","Create","True","0","0","0","True","0","False","Object","-1","","2","2","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":6,"pos":[1456,48],"params":["Inherit","False","Property","_ZWrite1","ZWrite","35","0","Create","True","0","0","0","True","0","False","Object","-1","","0","0","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":68,"pos":[-384,0],"params":["Inherit","False","67","Col","1","0","OBJECT","","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":5,"pos":[944,48],"params":["Inherit","False","Property","_Src1","Src","33","0","Create","True","0","0","0","True","0","False","Object","-1","","1","1","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":4,"pos":[1200,48],"params":["Inherit","False","Property","_Dst1","Dst","34","0","Create","True","0","0","0","True","0","False","Object","-1","","1","1","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":49,"pos":[-4224,128],"params":["Inherit","False","2","2","0","COLOR","0,0,0,0","False","1","FLOAT3","0,0,0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.SimpleTimeNode, AmplifyShaderEditor","id":95,"pos":[-5376,-512],"params":["Inherit","False","1","0","FLOAT","1","False","5","FLOAT","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":96,"pos":[-5632,-512],"params":["Inherit","False","Property","_HueShiftSpeed","Hue Shift Speed","9","0","Create","True","0","0","0","False","0","False","Object","-1","","0","0","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RGBToHSVNode, AmplifyShaderEditor","id":85,"pos":[-5632,-896],"params":["Inherit","False","1","0","FLOAT3","0,0,0","False","4","FLOAT3","0","FLOAT","1","FLOAT","2","FLOAT","3"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":98,"pos":[-5632,-640],"params":["Inherit","False","Property","_HueShift","Hue Shift","8","0","Create","True","0","0","0","False","0","False","Object","-1","","0","0","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor","id":97,"pos":[-5376,-896],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.HSVToRGBNode, AmplifyShaderEditor","id":88,"pos":[-4352,-896],"params":["Inherit","False","3","0","FLOAT","0","False","1","FLOAT","0","False","2","FLOAT","0","False","4","FLOAT3","0","FLOAT","1","FLOAT","2","FLOAT","3"]}
{"type":"AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor","id":94,"pos":[-4616,-384],"params":["Inherit","False","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":91,"pos":[-4616,-256],"params":["Inherit","False","Property","_Desaturate","Desaturate","10","0","Create","True","0","0","0","False","0","False","Object","-1","","0","0","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.OneMinusNode, AmplifyShaderEditor","id":92,"pos":[-4616,-512],"params":["Inherit","False","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":90,"pos":[-4616,-640],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor","id":100,"pos":[-5120,-512],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT2","0,0","False","1","FLOAT2","0"]}
{"type":"AmplifyShaderEditor.TextureCoordinatesNode, AmplifyShaderEditor","id":104,"pos":[-5120,-256],"params":["Inherit","False","0","-1","2","3","2","SAMPLER2D","","False","0","FLOAT2","1,1","False","1","FLOAT2","0,0","False","5","FLOAT2","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor","id":105,"pos":[-4848,-384],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT2","0,0","False","1","FLOAT2","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":106,"pos":[-4864,-256],"params":["Inherit","False","2","2","0","FLOAT2","0,0","False","1","FLOAT","0","False","1","FLOAT2","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":108,"pos":[-5120,-128],"params":["Inherit","False","107","VTC Emission","1","0","OBJECT","","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":73,"pos":[-1920,-1024],"params":["Inherit","False","VTC Random Offset","-1","True","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":103,"pos":[-1920,-736],"params":["Inherit","False","VTC Random Hue Offset","-1","True","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":107,"pos":[-1920,-816],"params":["Inherit","False","VTC Emission","-1","True","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.LerpOp, AmplifyShaderEditor","id":51,"pos":[-3200,-256],"params":["Inherit","False","3","0","COLOR","0,0,0,0","False","1","COLOR","0,0,0,0","False","2","FLOAT","0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.StaticSwitch, AmplifyShaderEditor","id":69,"pos":[-2304,0],"params":["Inherit","False","Property","_IsAdditive","IsAdditive","37","0","Create","True","0","0","0","False","0","False","","0","1","1","True","","Toggle","2","Key0","Key1","Create","True","True","All","9","1","COLOR","0,0,0,0","False","0","COLOR","0,0,0,0","False","2","COLOR","0,0,0,0","False","3","COLOR","0,0,0,0","False","4","COLOR","0,0,0,0","False","5","COLOR","0,0,0,0","False","6","COLOR","0,0,0,0","False","7","COLOR","0,0,0,0","False","8","COLOR","0,0,0,0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":70,"pos":[-2304,128],"params":["Inherit","False","2","2","0","COLOR","0,0,0,0","False","1","FLOAT","0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":52,"pos":[-1792,0],"params":["Inherit","False","2","2","0","COLOR","0,0,0,0","False","1","FLOAT","0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":177,"pos":[-384,256],"params":["Inherit","False","174","WPO","1","0","OBJECT","","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.SamplerNode, AmplifyShaderEditor","id":25,"pos":[-6016,640],"params":["Inherit","True","Property","_LUTTexture","LUT Texture","11","0","Create","True","0","0","0","False","3","Space(13)","Header(LUT)","Space(33)","False","","-1","0714bd23a673ddd48af24f353559553a","0714bd23a673ddd48af24f353559553a","True","0","False","white","Auto","False","Object","-1","Auto","Texture2D","False","8","0","SAMPLER2D","","False","1","FLOAT2","0,0","False","2","FLOAT","0","False","3","FLOAT2","0,0","False","4","FLOAT2","0,0","False","5","FLOAT","1","False","6","FLOAT","0","False","7","SAMPLERSTATE","","False","6","COLOR","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4","FLOAT3","5"]}
{"type":"AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor","id":180,"pos":[-7424,512],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.PannerNode, AmplifyShaderEditor","id":182,"pos":[-7040,512],"params":["Inherit","False","3","0","FLOAT2","0,0","False","2","FLOAT2","0,0","False","1","FLOAT","1","False","1","FLOAT2","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":183,"pos":[-7040,640],"params":["Inherit","False","Property","_LUTSpeed","LUT Speed","13","0","Create","True","0","0","0","False","0","False","Object","-1","","0","0","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":181,"pos":[-7424,640],"params":["Inherit","False","Property","_LUTOffset","LUT Offset","14","0","Create","True","0","0","0","False","0","False","Object","-1","","0","0","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":184,"pos":[-7808,512],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":185,"pos":[-7808,640],"params":["Inherit","False","Property","_LUTAmplitude","LUT Amplitude","12","0","Create","True","0","0","0","False","0","False","Object","-1","","1","1","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":50,"pos":[-3200,-128],"params":["Inherit","False","Property","_LUTIntensity","LUT Intensity","15","0","Create","True","0","0","0","False","0","False","Object","-1","","1","1","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor","id":89,"pos":[-4992,-896],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT2","0,0","False","1","FLOAT2","0"]}
{"type":"AmplifyShaderEditor.StaticSwitch, AmplifyShaderEditor","id":110,"pos":[-4736,-896],"params":["Inherit","False","Property","_UseHueShift","Use Hue Shift","7","0","Create","True","0","0","0","False","0","False","","0","0","0","True","","Toggle","2","Key0","Key1","Create","True","True","All","9","1","FLOAT2","0,0","False","0","FLOAT2","0,0","False","2","FLOAT2","0,0","False","3","FLOAT2","0,0","False","4","FLOAT2","0,0","False","5","FLOAT2","0,0","False","6","FLOAT2","0,0","False","7","FLOAT2","0,0","False","8","FLOAT2","0,0","False","1","FLOAT2","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":71,"pos":[-2304,256],"params":["Inherit","False","65","OP","1","0","OBJECT","","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":66,"pos":[-384,128],"params":["Inherit","False","65","OP","1","0","OBJECT","","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":179,"pos":[-8576,512],"params":["Inherit","False","141","Out Ellipse","1","0","OBJECT","","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":67,"pos":[-1024,0],"params":["Inherit","False","Col","-1","True","1","0","COLOR","0,0,0,0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor","id":24,"pos":[-6272,768],"params":["Inherit","False","2","2","0","FLOAT2","0,0","False","1","FLOAT2","0,0","False","1","FLOAT2","0"]}
{"type":"AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor","id":191,"pos":[-7936,0],"params":["Inherit","False","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor","id":193,"pos":[-8320,128],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SmoothstepOpNode, AmplifyShaderEditor","id":194,"pos":[-8192,0],"params":["Inherit","False","3","0","FLOAT","0","False","1","FLOAT","0","False","2","FLOAT","1","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":192,"pos":[-8576,256],"params":["Inherit","False","Property","_LUTErosionSmoothness","LUT Erosion Smoothness","16","0","Create","True","0","0","0","False","0","False","Object","-1","","1","1","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":195,"pos":[-8576,128],"params":["Inherit","False","72","VTC Eros","1","0","OBJECT","","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":197,"pos":[-3840,3328],"params":["Inherit","False","Property","_FlickerSpeed02","Flicker Speed 02","25","0","Create","True","0","0","0","False","0","False","Object","-1","","3.137","1","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":198,"pos":[-3840,2944],"params":["Inherit","False","Property","_FlickerSpeed01","Flicker Speed 01","23","0","Create","True","0","0","0","False","0","False","Object","-1","","7.731","1","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":199,"pos":[-3840,2816],"params":["Inherit","False","Property","_FlickerSpeedOverall","Flicker Speed Overall","21","0","Create","True","0","0","0","False","3","Space(33)","Header(Flicker)","Space(13)","False","Object","-1","","3.333","3.333","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":200,"pos":[-3456,2944],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":201,"pos":[-3456,3328],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleTimeNode, AmplifyShaderEditor","id":202,"pos":[-3200,2944],"params":["Inherit","False","1","0","FLOAT","1","False","5","FLOAT","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.SimpleTimeNode, AmplifyShaderEditor","id":203,"pos":[-3200,3328],"params":["Inherit","False","1","0","FLOAT","1","False","5","FLOAT","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor","id":204,"pos":[-2944,2944],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor","id":205,"pos":[-2944,3200],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SinOpNode, AmplifyShaderEditor","id":206,"pos":[-2816,2944],"params":["Inherit","False","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":207,"pos":[-2816,3072],"params":["Inherit","False","Property","_FlickerIntensity01","Flicker Intensity 01","22","0","Create","True","0","0","0","False","0","False","Object","-1","","1","1","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.CosOpNode, AmplifyShaderEditor","id":208,"pos":[-2816,3200],"params":["Inherit","False","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":209,"pos":[-2816,3456],"params":["Inherit","False","Property","_FlickerIntensity02","Flicker Intensity 02","24","0","Create","True","0","0","0","False","0","False","Object","-1","","1","1","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":210,"pos":[-2560,2944],"params":["Inherit","True","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":211,"pos":[-2560,3328],"params":["Inherit","True","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":212,"pos":[-2176,2944],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SinOpNode, AmplifyShaderEditor","id":217,"pos":[-2816,3328],"params":["Inherit","False","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":218,"pos":[-3200,3144],"params":["Inherit","False","73","VTC Random Offset","1","0","OBJECT","","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":53,"pos":[-1792,-384],"params":["Inherit","False","Property","_EmissiveMult","Emissive Mult","0","0","Create","True","0","0","0","False","0","False","Object","-1","","1","1","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":163,"pos":[-1536,-384],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":164,"pos":[-1792,-256],"params":["Inherit","False","107","VTC Emission","1","0","OBJECT","","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":220,"pos":[-1536,-256],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":221,"pos":[-1792,-128],"params":["Inherit","False","219","Flick","1","0","OBJECT","","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":222,"pos":[-3456,3712],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleTimeNode, AmplifyShaderEditor","id":223,"pos":[-3200,3712],"params":["Inherit","False","1","0","FLOAT","1","False","5","FLOAT","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":224,"pos":[-2560,3712],"params":["Inherit","True","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SinOpNode, AmplifyShaderEditor","id":225,"pos":[-2816,3712],"params":["Inherit","False","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":226,"pos":[-3840,3712],"params":["Inherit","False","Property","_FlickerSpeed03","Flicker Speed 03","27","0","Create","True","0","0","0","False","0","False","Object","-1","","0.373","1","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":227,"pos":[-2816,3840],"params":["Inherit","False","Property","_FlickerIntensity03","Flicker Intensity 03","26","0","Create","True","0","0","0","False","0","False","Object","-1","","1","1","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor","id":228,"pos":[-2944,3712],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor","id":213,"pos":[-1280,2944],"params":["Inherit","True","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":214,"pos":[-1024,3200],"params":["Inherit","False","Property","_FlickerMin","Flicker Min","28","0","Create","True","0","0","0","False","0","False","Object","-1","","0.5","0.5","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":215,"pos":[-1024,3328],"params":["Inherit","False","Property","_FlickerMax","Flicker Max","29","0","Create","True","0","0","0","False","0","False","Object","-1","","1","1","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.LerpOp, AmplifyShaderEditor","id":216,"pos":[-1024,2944],"params":["Inherit","True","3","0","FLOAT","0","False","1","FLOAT","0","False","2","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":219,"pos":[-640,2944],"params":["Inherit","False","Flick","-1","True","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor","id":229,"pos":[-1792,2944],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor","id":230,"pos":[-7168,640],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":101,"pos":[-5120,-384],"params":["Inherit","False","103","VTC Random Hue Offset","1","0","OBJECT","","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":231,"pos":[-7424,768],"params":["Inherit","False","103","VTC Random Hue Offset","1","0","OBJECT","","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.TemplateMultiPassMasterNode, AmplifyShaderEditor","id":233,"pos":[0,0],"params":["Float","False","False","-1","3","AmplifyShaderEditor.MaterialInspector","0","1","New Amplify Shader","0770190933193b94aaa3065e307002fa","True","ExtraPrePass","0","0","ExtraPrePass","6","False","True","1","1","False","","0","False","","1","1","False","","0","False","","True","1","False","","1","False","","False","False","False","False","False","False","False","False","False","True","0","False","","False","True","0","False","","False","True","True","True","True","True","0","False","","False","False","False","False","False","False","False","True","False","0","False","","255","False","","255","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","False","True","1","False","","False","False","False","True","1","RenderType=Opaque=RenderType","True","3","True","12","all","0","False","True","1","1","False","","0","False","","0","1","False","","0","False","","False","False","False","False","False","False","False","False","False","False","False","False","True","0","False","","False","True","True","True","True","True","0","False","","False","False","False","False","False","False","False","True","False","0","False","","255","False","","255","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","False","True","1","False","","True","3","False","","True","True","0","False","","0","False","","False","True","1","LightMode=ForwardBase","False","False","0","","0","0","Standard","0","False","0"]}
{"type":"AmplifyShaderEditor.TemplateMultiPassMasterNode, AmplifyShaderEditor","id":234,"pos":[0,0],"params":["Float","False","True","-1","3","AmplifyShaderEditor.MaterialInspector","0","7","Vefects/SH_Vefects_VFX_Anime_Star_02_LUT_BIRP","0770190933193b94aaa3065e307002fa","True","Unlit","0","1","Unlit","8","False","True","1","1","False","","0","False","","1","1","False","","0","False","","True","1","False","","1","False","","False","False","False","False","False","False","False","False","False","True","0","False","","False","True","0","False","","False","True","True","True","True","True","0","False","","False","False","False","False","False","False","False","True","False","0","False","","255","False","","255","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","False","True","1","False","","False","False","False","True","2","RenderType=Transparent=RenderType","Queue=Transparent=Queue=0","True","3","True","12","all","0","True","True","1","5","True","_Src1","10","True","_Dst1","1","1","False","","10","False","","True","1","False","","1","False","","False","False","False","False","False","False","False","False","False","False","True","True","0","True","_Cull1","False","True","True","True","True","True","0","False","","False","False","False","False","False","False","False","True","False","0","False","","255","False","","255","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","True","True","2","True","_ZWrite1","True","3","True","_ZTest1","True","True","0","False","","0","False","","False","True","1","LightMode=ForwardBase","False","False","0","","0","0","Standard","10","Surface","1","639239548079838619","  Keep Alpha","0","0","  Blend","0","0","Alpha Clipping","0","0","  Use Shadow Threshold","0","0","Cast Shadows","1","0","Write Depth","0","0","  Conservative","0","0","Extra Pre Pass","0","0","Vertex Position","1","0","0","3","False","True","True","False","","False","0"]}
{"type":"AmplifyShaderEditor.TemplateMultiPassMasterNode, AmplifyShaderEditor","id":235,"pos":[0,0],"params":["Float","False","False","-1","3","AmplifyShaderEditor.MaterialInspector","0","1","New Amplify Shader","0770190933193b94aaa3065e307002fa","True","ShadowCaster","0","2","ShadowCaster","0","False","True","1","1","False","","0","False","","1","1","False","","0","False","","True","1","False","","1","False","","False","False","False","False","False","False","False","False","False","True","0","False","","False","True","0","False","","False","True","True","True","True","True","0","False","","False","False","False","False","False","False","False","True","False","0","False","","255","False","","255","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","False","True","1","False","","False","False","False","True","1","RenderType=Opaque=RenderType","True","3","True","12","all","0","False","False","False","False","False","False","False","False","False","False","False","False","True","0","False","","False","False","False","False","False","False","False","False","False","False","False","False","False","True","1","False","","True","3","False","","False","False","True","1","LightMode=ShadowCaster","False","False","0","","0","0","Standard","0","False","0"]}
{"type":"AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor","id":118,"pos":[-8576,-4864],"params":["Inherit","False","896.4395","130.1563","Original Super Ellipse done by Bruno Afonseca in Substance Designer: https://www.artstation.com/bruno","0","Original Super Ellipse done by Bruno Afonseca in Substance Designer: https://www.artstation.com/bruno","0,0,0,1","0","0"]}
{"type":"AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor","id":119,"pos":[-8576,-4608],"params":["Inherit","False","896.4395","130.1563","Adapted to shader by Ge Lush, September 2026.","0","Adapted to shader by Ge Lush, September 2026.","0,0,0,1","0","0"]}
{"wire":[116,0,114,0]}
{"wire":[117,0,113,0]}
{"wire":[117,1,116,0]}
{"wire":[112,0,111,0]}
{"wire":[112,2,117,0]}
{"wire":[121,0,112,0]}
{"wire":[122,0,112,0]}
{"wire":[123,0,121,0]}
{"wire":[123,1,124,0]}
{"wire":[123,2,125,0]}
{"wire":[123,3,126,0]}
{"wire":[123,4,125,0]}
{"wire":[127,0,122,0]}
{"wire":[127,1,124,0]}
{"wire":[127,2,125,0]}
{"wire":[127,3,126,0]}
{"wire":[127,4,125,0]}
{"wire":[128,0,123,0]}
{"wire":[128,1,127,0]}
{"wire":[155,0,102,1]}
{"wire":[129,0,128,0]}
{"wire":[130,0,128,0]}
{"wire":[149,0,129,0]}
{"wire":[149,1,160,1]}
{"wire":[151,0,130,0]}
{"wire":[151,1,160,2]}
{"wire":[154,0,156,0]}
{"wire":[154,1,159,0]}
{"wire":[152,0,151,0]}
{"wire":[150,0,149,0]}
{"wire":[153,0,154,0]}
{"wire":[153,1,157,0]}
{"wire":[147,0,150,0]}
{"wire":[147,1,153,0]}
{"wire":[148,0,152,0]}
{"wire":[148,1,153,0]}
{"wire":[134,0,135,0]}
{"wire":[134,1,131,0]}
{"wire":[146,0,147,0]}
{"wire":[146,1,148,0]}
{"wire":[136,0,146,0]}
{"wire":[136,1,134,0]}
{"wire":[189,0,136,0]}
{"wire":[190,0,189,0]}
{"wire":[140,0,131,0]}
{"wire":[140,1,143,0]}
{"wire":[140,2,190,0]}
{"wire":[140,3,143,0]}
{"wire":[140,4,143,0]}
{"wire":[141,0,140,0]}
{"wire":[72,0,26,3]}
{"wire":[80,0,79,0]}
{"wire":[80,1,28,0]}
{"wire":[77,0,142,0]}
{"wire":[77,1,79,0]}
{"wire":[77,2,80,0]}
{"wire":[34,0,77,0]}
{"wire":[41,0,38,0]}
{"wire":[39,0,35,4]}
{"wire":[39,1,34,0]}
{"wire":[171,0,170,0]}
{"wire":[78,0,39,0]}
{"wire":[64,0,41,0]}
{"wire":[172,0,171,0]}
{"wire":[44,0,78,0]}
{"wire":[44,1,64,0]}
{"wire":[168,0,167,0]}
{"wire":[173,0,172,0]}
{"wire":[173,1,175,0]}
{"wire":[45,0,44,0]}
{"wire":[169,0,168,0]}
{"wire":[169,1,173,0]}
{"wire":[174,0,169,0]}
{"wire":[65,0,45,0]}
{"wire":[10,0,8,0]}
{"wire":[10,1,9,0]}
{"wire":[12,0,10,0]}
{"wire":[12,2,11,0]}
{"wire":[74,0,12,0]}
{"wire":[74,1,75,0]}
{"wire":[13,1,74,0]}
{"wire":[14,0,13,0]}
{"wire":[19,3,14,0]}
{"wire":[22,0,17,0]}
{"wire":[22,1,19,0]}
{"wire":[22,2,20,0]}
{"wire":[49,0,35,0]}
{"wire":[49,1,88,0]}
{"wire":[95,0,96,0]}
{"wire":[85,0,25,0]}
{"wire":[97,0,85,1]}
{"wire":[97,1,98,0]}
{"wire":[88,0,110,0]}
{"wire":[88,1,90,0]}
{"wire":[88,2,85,3]}
{"wire":[94,0,91,0]}
{"wire":[92,0,94,0]}
{"wire":[90,0,85,2]}
{"wire":[90,1,92,0]}
{"wire":[100,0,95,0]}
{"wire":[100,1,105,0]}
{"wire":[105,0,101,0]}
{"wire":[105,1,106,0]}
{"wire":[106,0,104,0]}
{"wire":[106,1,108,0]}
{"wire":[73,0,26,4]}
{"wire":[103,0,102,3]}
{"wire":[107,0,102,2]}
{"wire":[51,0,35,0]}
{"wire":[51,1,49,0]}
{"wire":[51,2,50,0]}
{"wire":[69,1,51,0]}
{"wire":[69,0,70,0]}
{"wire":[70,0,51,0]}
{"wire":[70,1,71,0]}
{"wire":[52,0,69,0]}
{"wire":[52,1,220,0]}
{"wire":[25,1,182,0]}
{"wire":[180,0,184,0]}
{"wire":[180,1,230,0]}
{"wire":[182,0,180,0]}
{"wire":[182,2,183,0]}
{"wire":[184,0,191,0]}
{"wire":[184,1,185,0]}
{"wire":[89,0,97,0]}
{"wire":[89,1,100,0]}
{"wire":[110,1,85,1]}
{"wire":[110,0,89,0]}
{"wire":[67,0,52,0]}
{"wire":[24,0,182,0]}
{"wire":[24,1,22,0]}
{"wire":[191,0,194,0]}
{"wire":[193,0,195,0]}
{"wire":[193,1,192,0]}
{"wire":[194,0,179,0]}
{"wire":[194,1,195,0]}
{"wire":[194,2,193,0]}
{"wire":[200,0,198,0]}
{"wire":[200,1,199,0]}
{"wire":[201,0,197,0]}
{"wire":[201,1,199,0]}
{"wire":[202,0,200,0]}
{"wire":[203,0,201,0]}
{"wire":[204,0,202,0]}
{"wire":[204,1,218,0]}
{"wire":[205,0,203,0]}
{"wire":[205,1,218,0]}
{"wire":[206,0,204,0]}
{"wire":[208,0,205,0]}
{"wire":[210,0,206,0]}
{"wire":[210,1,207,0]}
{"wire":[211,0,208,0]}
{"wire":[211,1,209,0]}
{"wire":[212,0,210,0]}
{"wire":[212,1,211,0]}
{"wire":[217,0,203,0]}
{"wire":[163,0,53,0]}
{"wire":[163,1,164,0]}
{"wire":[220,0,163,0]}
{"wire":[220,1,221,0]}
{"wire":[222,0,226,0]}
{"wire":[222,1,199,0]}
{"wire":[223,0,222,0]}
{"wire":[224,0,225,0]}
{"wire":[224,1,227,0]}
{"wire":[225,0,228,0]}
{"wire":[228,0,223,0]}
{"wire":[228,1,218,0]}
{"wire":[213,0,229,0]}
{"wire":[216,0,214,0]}
{"wire":[216,1,215,0]}
{"wire":[216,2,213,0]}
{"wire":[219,0,216,0]}
{"wire":[229,0,212,0]}
{"wire":[229,1,224,0]}
{"wire":[230,0,181,0]}
{"wire":[230,1,231,0]}
{"wire":[234,0,68,0]}
{"wire":[234,7,66,0]}
{"wire":[234,15,177,0]}
ASEEND*/
//CHKSM=E64A39B762A17038AAD1AD237A1DAE37374FE0D5