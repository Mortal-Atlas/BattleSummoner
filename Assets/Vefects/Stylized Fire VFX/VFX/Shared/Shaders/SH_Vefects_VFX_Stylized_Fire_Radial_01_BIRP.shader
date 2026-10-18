// Made with Amplify Shader Editor v1.9.9.7
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Vefects/SH_Vefects_VFX_Stylized_Fire_Radial_01_BIRP"
{
	Properties
	{
		[Space(33)][Header(Clevender)][Space(13)] _ClevendercomLearnVFXNow( "- Clevender.com - Learn VFX Now!", Float ) = 777
		[Space(33)][Header(General)][Space(13)] _Emissive( "Emissive", Float ) = 1
		_OverallUVScale( "Overall UV Scale", Float ) = 1
		_OverallSpeed( "Overall Speed", Float ) = 1
		_NoisesTileVertical( "Noises Tile Vertical", Float ) = 1
		_NoisesTileHorizontal( "Noises Tile Horizontal", Float ) = 1
		_EnableRandomRotation( "Enable Random Rotation", Float ) = 0
		_Erosion( "Erosion", Float ) = 0
		_ErosionSmoothness( "Erosion Smoothness", Float ) = 1
		[Space(33)][Header(Noise)][Space(13)] _NoisesTexture( "Noises Texture", 2D ) = "white" {}
		_Noises01Selector( "Noises 01 Selector", Vector ) = ( 0, 1, 0, 0 )
		_Noises01Scale( "Noises 01 Scale", Vector ) = ( 1, 1, 0, 0 )
		_Noises01Speed( "Noises 01 Speed", Vector ) = ( -0.3, 0.01, 0, 0 )
		[Space(33)][Header(Noise Secondary)][Space(13)] _NoisesTextureSecondary( "Noises Texture Secondary", 2D ) = "white" {}
		_Noises02Selector( "Noises 02 Selector", Vector ) = ( 1, 0, 0, 0 )
		_Noises02Scale( "Noises 02 Scale", Vector ) = ( 1, 1, 0, 0 )
		_Noises02Speed( "Noises 02 Speed", Vector ) = ( -0.115, 0.01, 0, 0 )
		[Space(33)][Header(Noise Tertiary)][Space(13)] _NoisesTextureTertiary( "Noises Texture Tertiary", 2D ) = "white" {}
		_Noises03Scale( "Noises 03 Scale", Vector ) = ( 1, 1, 0, 0 )
		_Noises03Speed( "Noises 03 Speed", Vector ) = ( -0.2, -0.01, 0, 0 )
		_Noises03DistortIntensity( "Noises 03 Distort Intensity", Float ) = 1
		[Space(33)][Header(Radial Distort Mask)][Space(13)] _RadialDistortMaskSize( "Radial Distort Mask Size", Float ) = 0
		_RadialDistortMaskSharpness( "Radial Distort Mask Sharpness", Float ) = 1
		_RadialDistortionMaskEro( "Radial Distortion Mask Ero", Float ) = 0
		_RadialDistortionMaskEroSmo( "Radial Distortion Mask Ero Smo", Float ) = 0.5
		[Space(33)][Header(Twist)][Space(13)] _TwistIntensity( "Twist Intensity", Float ) = 0
		_PolarCoordinatesRadialScale( "Polar Coordinates Radial Scale", Float ) = 0.5
		_PolarCoordinatesLengthScale( "Polar Coordinates Length Scale", Float ) = 1
		_PolarCoordinatesPanSpeed( "Polar Coordinates Pan Speed", Vector ) = ( 0, 0, 0, 0 )
		[Space(33)][Header(Masks)][Space(13)] _MasksTexture( "Masks Texture", 2D ) = "white" {}
		_MaskErosion( "Mask Erosion", Float ) = 0.7
		_MaskErosionSmoothness( "Mask Erosion Smoothness", Float ) = 0.3
		[Space(33)][Header(Distortion)][Space(13)] _DistortionIntensity( "Distortion Intensity", Float ) = 1
		[Space(33)][Header(Camera Depth Fade)][Space(13)] _CameraDepthFadeLength( "Camera Depth Fade Length", Float ) = 0.333
		_CameraDepthFadeOffset( "Camera Depth Fade Offset", Float ) = 0
		[Space(33)][Header(LUT)][Space(13)] _LUT( "LUT", 2D ) = "white" {}
		_LUTOffset( "LUT Offset", Float ) = 0
		_LUTSpeed( "LUT Speed", Float ) = 0
		_LUTAmplitude( "LUT Amplitude", Float ) = 1
		_LUTErosion( "LUT Erosion", Float ) = 0
		_LUTErosionSmoothness( "LUT Erosion Smoothness", Float ) = 1
		_HueShift( "Hue Shift", Float ) = 0
		_Desaturate( "Desaturate", Float ) = 0
		[Space(33)][Header(Depth Fade)][Space(13)] _DepthFade( "Depth Fade", Float ) = 0.1
		[Space(33)][Header(Fresnel)][Space(13)] _FresnelIntensity( "Fresnel Intensity", Float ) = 1
		_FresnelBias( "Fresnel Bias", Float ) = 0
		_FresnelScale( "Fresnel Scale", Float ) = 1
		_FresnelPower( "Fresnel Power", Float ) = 5
		[Space(33)][Header(Camera Offset)][Space(13)] _CameraOffset( "Camera Offset", Float ) = 0
		[Space(33)][Header(AR)][Space(13)] _Cull( "Cull", Float ) = 2
		_Src( "Src", Float ) = 5
		_Dst( "Dst", Float ) = 10
		_ZWrite( "ZWrite", Float ) = 0
		_ZTest( "ZTest", Float ) = 2
		_DistortionInversion( "Distortion Inversion", Float ) = -1
		[HideInInspector] _texcoord2( "", 2D ) = "white" {}
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Transparent"  "Queue" = "Transparent+0" "IsEmissive" = "true"  }
		Cull [_Cull]
		ZWrite [_ZWrite]
		ZTest [_ZTest]
		Blend [_Src] [_Dst]
		CGPROGRAM
		#include "UnityShaderVariables.cginc"
		#include "UnityCG.cginc"
		#pragma target 3.5
		#define ASE_VERSION 19907
		#pragma surface surf Unlit keepalpha noshadow vertex:vertexDataFunc 
		#undef TRANSFORM_TEX
		#define TRANSFORM_TEX(tex,name) float4(tex.xy * name##_ST.xy + name##_ST.zw, tex.z, tex.w)
		struct Input
		{
			float3 worldPos;
			float4 uv_texcoord;
			float4 screenPos;
			float3 worldNormal;
			float4 uv2_texcoord2;
			float4 vertexColor : COLOR;
			float eyeDepth;
		};

		uniform float _Dst;
		uniform float _Src;
		uniform float _ZWrite;
		uniform float _ZTest;
		uniform float _Cull;
		uniform float _CameraOffset;
		uniform sampler2D _LUT;
		uniform float _LUTSpeed;
		uniform float _LUTErosion;
		UNITY_DECLARE_DEPTH_TEXTURE( _CameraDepthTexture );
		uniform float4 _CameraDepthTexture_TexelSize;
		uniform float _DepthFade;
		uniform float _FresnelBias;
		uniform float _FresnelScale;
		uniform float _FresnelPower;
		uniform float _FresnelIntensity;
		uniform float _LUTErosionSmoothness;
		uniform float _MaskErosion;
		uniform float _MaskErosionSmoothness;
		uniform sampler2D _MasksTexture;
		uniform sampler2D _NoisesTexture;
		uniform float2 _Noises01Scale;
		uniform float _PolarCoordinatesRadialScale;
		uniform float _PolarCoordinatesLengthScale;
		uniform float2 _PolarCoordinatesPanSpeed;
		uniform float _TwistIntensity;
		uniform float _OverallUVScale;
		uniform float _NoisesTileVertical;
		uniform float _NoisesTileHorizontal;
		uniform float2 _Noises01Speed;
		uniform float _OverallSpeed;
		uniform sampler2D _NoisesTextureTertiary;
		uniform float2 _Noises03Scale;
		uniform float2 _Noises03Speed;
		uniform float _Noises03DistortIntensity;
		uniform float _ClevendercomLearnVFXNow;
		uniform float _EnableRandomRotation;
		uniform float4 _Noises01Selector;
		uniform sampler2D _NoisesTextureSecondary;
		uniform float2 _Noises02Scale;
		uniform float2 _Noises02Speed;
		uniform float4 _Noises02Selector;
		uniform float _DistortionInversion;
		uniform float _RadialDistortionMaskEro;
		uniform float _RadialDistortionMaskEroSmo;
		uniform float _RadialDistortMaskSize;
		uniform float _RadialDistortMaskSharpness;
		uniform float _DistortionIntensity;
		uniform float _LUTAmplitude;
		uniform float _LUTOffset;
		uniform float _HueShift;
		uniform float _Desaturate;
		uniform float _Emissive;
		uniform float _Erosion;
		uniform float _ErosionSmoothness;
		uniform float _CameraDepthFadeLength;
		uniform float _CameraDepthFadeOffset;


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

		void vertexDataFunc( inout appdata_full v, out Input o )
		{
			UNITY_INITIALIZE_OUTPUT( Input, o );
			float3 ase_positionWS = mul( unity_ObjectToWorld, v.vertex );
			float3 Cam_Offset236 = ( ( ase_positionWS - _WorldSpaceCameraPos ) * ( _CameraOffset * 0.01 ) );
			v.vertex.xyz += Cam_Offset236;
			v.vertex.w = 1;
			o.eyeDepth = -UnityObjectToViewPos( v.vertex.xyz ).z;
		}

		inline half4 LightingUnlit( SurfaceOutput s, half3 lightDir, half atten )
		{
			return half4 ( 0, 0, 0, s.Alpha );
		}

		void surf( Input i , inout SurfaceOutput o )
		{
			float2 temp_cast_0 = (_LUTSpeed).xx;
			float VTC_Ero29 = i.uv_texcoord.z;
			float4 ase_positionSS = float4( i.screenPos.xyz , i.screenPos.w + 1e-7 );
			float4 ase_positionSSNorm = ase_positionSS / ase_positionSS.w;
			ase_positionSSNorm.z = ( UNITY_NEAR_CLIP_VALUE >= 0 ) ? ase_positionSSNorm.z : ase_positionSSNorm.z * 0.5 + 0.5;
			float screenDepth73 = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE( _CameraDepthTexture, ase_positionSSNorm.xy ));
			float distanceDepth73 = saturate( ( screenDepth73 - LinearEyeDepth( ase_positionSSNorm.z ) ) / ( _DepthFade ) );
			float3 ase_positionWS = i.worldPos;
			float3 ase_viewVectorWS = ( _WorldSpaceCameraPos.xyz - ase_positionWS );
			float3 ase_viewDirWS = normalize( ase_viewVectorWS );
			float3 ase_normalWS = i.worldNormal;
			float fresnelNdotV79 = dot( ase_normalWS, ase_viewDirWS );
			float fresnelNode79 = ( _FresnelBias + _FresnelScale * pow( max( 1.0 - fresnelNdotV79 , 0.0001 ), _FresnelPower ) );
			float temp_output_77_0 = saturate( ( ( 1.0 - saturate( distanceDepth73 ) ) + saturate( ( saturate( fresnelNode79 ) * _FresnelIntensity ) ) ) );
			float temp_output_92_0 = ( _LUTErosion + ( VTC_Ero29 + temp_output_77_0 ) );
			float VTC_Ero_Smooth34 = i.uv2_texcoord2.z;
			float2 temp_output_34_0_g41 = ( i.uv_texcoord.xy - float2( 0.5,0.5 ) );
			float2 break39_g41 = temp_output_34_0_g41;
			float2 appendResult50_g41 = (float2(( 1.0 * ( length( temp_output_34_0_g41 ) * 2.0 ) ) , ( ( atan2( break39_g41.x , break39_g41.y ) * ( 1.0 / 6.28318548202515 ) ) * 1.0 )));
			float2 temp_output_34_0_g21 = ( i.uv_texcoord.xy - float2( 0.5,0.5 ) );
			float2 break39_g21 = temp_output_34_0_g21;
			float2 appendResult50_g21 = (float2(( _PolarCoordinatesRadialScale * ( length( temp_output_34_0_g21 ) * 2.0 ) ) , ( ( atan2( break39_g21.x , break39_g21.y ) * ( 1.0 / 6.28318548202515 ) ) * _PolarCoordinatesLengthScale )));
			float2 panner291 = ( 1.0 * _Time.y * _PolarCoordinatesPanSpeed + float2( 0,0 ));
			float2 break53_g21 = appendResult50_g21;
			float2 appendResult157 = (float2(_NoisesTileVertical , _NoisesTileHorizontal));
			float2 twistedUVs293 = ( ( ( ( appendResult50_g21 + panner291 ) + ( break53_g21.x * _TwistIntensity ) ) * _OverallUVScale ) * appendResult157 );
			float2 break266 = twistedUVs293;
			float2 appendResult272 = (float2(( (_Noises01Scale).x * break266.x ) , ( break266.y * (_Noises01Scale).y )));
			float Overall_Speed165 = _OverallSpeed;
			float mulTime258 = _Time.y * Overall_Speed165;
			float2 panner262 = ( ( (_Noises01Speed).x * mulTime258 ) * float2( 1,0 ) + float2( 0,0 ));
			float2 panner263 = ( ( mulTime258 * (_Noises01Speed).y ) * float2( 0,1 ) + float2( 0,0 ));
			float2 appendResult271 = (float2((panner262).x , (panner263).y));
			float2 break439 = twistedUVs293;
			float2 appendResult446 = (float2(( (_Noises03Scale).x * break439.x ) , ( break439.y * (_Noises03Scale).y )));
			float mulTime433 = _Time.y * Overall_Speed165;
			float2 panner440 = ( ( (_Noises03Speed).x * mulTime433 ) * float2( 1,0 ) + float2( 0,0 ));
			float2 panner441 = ( ( mulTime433 * (_Noises03Speed).y ) * float2( 0,1 ) + float2( 0,0 ));
			float2 appendResult447 = (float2((panner440).x , (panner441).y));
			float VTC_Randoff33 = i.uv2_texcoord2.y;
			float DDX243 = ddx( i.uv_texcoord.xy.x );
			float2 temp_cast_1 = (DDX243).xx;
			float DDY247 = ddy( i.uv_texcoord.xy.y );
			float2 temp_cast_2 = (DDY247).xx;
			float2 Noises_Ter_Dist172 = ( ( ( (tex2D( _NoisesTextureTertiary, ( ( appendResult446 + appendResult447 ) + ( VTC_Randoff33 * 1.777 ) ), temp_cast_1, temp_cast_2 ).rgb).xy + -0.5 ) * 2.0 ) * ( _Noises03DistortIntensity * 0.1 ) );
			float Clevender39 = _ClevendercomLearnVFXNow;
			float VTC_Randrot35 = ( i.uv2_texcoord2.w * _EnableRandomRotation );
			float cos426 = cos( VTC_Randrot35 );
			float sin426 = sin( VTC_Randrot35 );
			float2 rotator426 = mul( ( ( ( appendResult272 + appendResult271 ) + Noises_Ter_Dist172 ) + ( VTC_Randoff33 * Clevender39 ) ) - float2( 0,0 ) , float2x2( cos426 , -sin426 , sin426 , cos426 )) + float2( 0,0 );
			float2 temp_cast_3 = (DDX243).xx;
			float2 temp_cast_4 = (DDY247).xx;
			float dotResult144 = dot( tex2D( _NoisesTexture, rotator426, temp_cast_3, temp_cast_4 ) , _Noises01Selector );
			float2 break404 = twistedUVs293;
			float2 appendResult406 = (float2(( (_Noises02Scale).x * break404.x ) , ( break404.y * (_Noises02Scale).y )));
			float mulTime410 = _Time.y * Overall_Speed165;
			float2 panner415 = ( ( (_Noises02Speed).x * mulTime410 ) * float2( 1,0 ) + float2( 0,0 ));
			float2 panner416 = ( ( mulTime410 * (_Noises02Speed).y ) * float2( 0,1 ) + float2( 0,0 ));
			float2 appendResult419 = (float2((panner415).x , (panner416).y));
			float2 temp_cast_6 = (DDX243).xx;
			float2 temp_cast_7 = (DDY247).xx;
			float dotResult218 = dot( tex2D( _NoisesTextureSecondary, ( ( ( appendResult406 + appendResult419 ) + Noises_Ter_Dist172 ) + ( VTC_Randoff33 * 1.333 ) ), temp_cast_6, temp_cast_7 ) , _Noises02Selector );
			float UV_Dist279 = ( saturate( ( saturate( dotResult144 ) + saturate( dotResult218 ) ) ) * _DistortionInversion );
			float clampResult9_g40 = clamp( ( ( length(  (float2( -1,-1 ) + ( i.uv_texcoord.xy - float2( 0,0 ) ) * ( float2( 1,1 ) - float2( -1,-1 ) ) / ( float2( 1,1 ) - float2( 0,0 ) ) ) ) + -_RadialDistortMaskSize ) * _RadialDistortMaskSharpness ) , 0.0 , 1.0 );
			float smoothstepResult386 = smoothstep( _RadialDistortionMaskEro , ( _RadialDistortionMaskEro + _RadialDistortionMaskEroSmo ) , saturate( ( 1.0 - clampResult9_g40 ) ));
			float radialDistMask325 = saturate( smoothstepResult386 );
			float VTC_Dist31 = i.uv_texcoord.w;
			float finalDist382 = ( ( UV_Dist279 * radialDistMask325 ) * ( _DistortionIntensity * VTC_Dist31 ) );
			float2 temp_cast_9 = (DDX243).xx;
			float2 temp_cast_10 = (DDY247).xx;
			float smoothstepResult361 = smoothstep( _MaskErosion , ( _MaskErosion + _MaskErosionSmoothness ) , tex2D( _MasksTexture, ( appendResult50_g41 + finalDist382 ), temp_cast_9, temp_cast_10 ).g);
			float temp_output_362_0 = saturate( smoothstepResult361 );
			float finalMask389 = temp_output_362_0;
			float smoothstepResult99 = smoothstep( temp_output_92_0 , ( temp_output_92_0 + ( _LUTErosionSmoothness * VTC_Ero_Smooth34 ) ) , finalMask389);
			float2 temp_cast_11 = (( ( saturate( smoothstepResult99 ) * _LUTAmplitude ) + _LUTOffset )).xx;
			float2 panner55 = ( 1.0 * _Time.y * temp_cast_0 + temp_cast_11);
			float3 hsvTorgb48 = RGBToHSV( tex2D( _LUT, panner55 ).rgb );
			float3 hsvTorgb47 = HSVToRGB( float3(( hsvTorgb48.x + _HueShift ),hsvTorgb48.y,hsvTorgb48.z) );
			float3 desaturateInitialColor51 = hsvTorgb47;
			float desaturateDot51 = dot( desaturateInitialColor51, float3( 0.299, 0.587, 0.114 ));
			float3 desaturateVar51 = lerp( desaturateInitialColor51, desaturateDot51.xxx, _Desaturate );
			float VTC_Em32 = i.uv2_texcoord2.x;
			float4 Col25 = ( ( float4( desaturateVar51 , 0.0 ) * i.vertexColor ) * ( _Emissive * VTC_Em32 ) );
			o.Emission = Col25.rgb;
			float temp_output_67_0 = ( _Erosion + ( VTC_Ero29 + temp_output_77_0 ) );
			float smoothstepResult62 = smoothstep( temp_output_67_0 , ( temp_output_67_0 + ( _ErosionSmoothness * VTC_Ero_Smooth34 ) ) , finalMask389);
			float cameraDepthFade16 = (( i.eyeDepth -_ProjectionParams.y - _CameraDepthFadeOffset ) / _CameraDepthFadeLength);
			float Op23 = saturate( ( saturate( ( saturate( smoothstepResult62 ) * i.vertexColor.a ) ) * saturate( cameraDepthFade16 ) ) );
			o.Alpha = Op23;
		}

		ENDCG
	}
	Fallback Off
	CustomEditor "AmplifyShaderEditor.MaterialInspector"
}
/*ASEBEGIN
Version=19907
Node;AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;254;-20480,-2796;Inherit;False;4869.21;4090.016;Radial Distort;105;448;447;446;445;444;443;442;441;440;439;438;437;436;435;434;433;432;431;430;413;419;406;418;417;414;405;416;415;404;403;402;401;412;411;410;409;408;407;172;179;279;220;140;147;219;144;218;215;142;146;217;399;398;426;424;397;396;143;214;427;425;423;422;273;271;272;173;267;268;269;270;174;263;262;266;265;264;178;175;259;260;261;294;177;258;257;256;429;428;176;420;255;450;451;452;453;454;455;456;457;458;459;460;462;463;Radial Distort;0,0,0,1;0;0
Node;AmplifyShaderEditor.Vector2Node, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;430;-20352,512;Inherit;False;Property;_Noises03Speed;Noises 03 Speed;20;0;Create;True;0;0;0;False;0;False;-0.2,-0.01;0.1,0.01;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;281;-13264,-4064;Inherit;False;2511.062;736.8732;Twist - Lush you're a genius! :O;18;155;157;154;293;449;153;288;289;287;290;291;286;292;284;285;283;295;156;Twist;0,0,0,1;0;0
Node;AmplifyShaderEditor.ComponentMaskNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;431;-19968,768;Inherit;False;False;True;True;True;1;0;FLOAT2;0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ComponentMaskNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;432;-19968,512;Inherit;False;True;False;True;True;1;0;FLOAT2;0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleTimeNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;433;-19968,640;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;36;-896,-1920;Inherit;False;640;766.95;VTC;10;32;34;29;31;27;35;33;28;240;241;VTC;0,0,0,1;0;0
Node;AmplifyShaderEditor.TextureCoordinatesNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;295;-13184,-3968;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;283;-13184,-3584;Inherit;False;Property;_PolarCoordinatesLengthScale;Polar Coordinates Length Scale;28;0;Create;True;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.Vector2Node, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;285;-13184,-3840;Inherit;False;Constant;_Vector7;Vector 5;38;0;Create;True;0;0;0;False;0;False;0.5,0.5;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.Vector2Node, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;292;-12416,-3712;Inherit;False;Property;_PolarCoordinatesPanSpeed;Polar Coordinates Pan Speed;29;0;Create;True;0;0;0;False;0;False;0,0;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;434;-19712,768;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;435;-19712,512;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.Vector2Node, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;436;-20352,-128;Inherit;False;Property;_Noises03Scale;Noises 03 Scale;19;0;Create;True;0;0;0;False;0;False;1,1;1,1;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;284;-13184,-3712;Inherit;False;Property;_PolarCoordinatesRadialScale;Polar Coordinates Radial Scale;27;0;Create;True;0;0;0;False;0;False;0.5;0.5;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;391;-13234,-1586;Inherit;False;932;290.95;DDX DDY;5;242;244;245;243;247;DDX DDY;0,0,0,1;0;0
Node;AmplifyShaderEditor.FunctionNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;286;-12800,-3968;Inherit;False;Polar Coordinates;-1;;21;7dab8e02884cf104ebefaa2e788e4162;0;4;1;FLOAT2;0,0;False;2;FLOAT2;0.5,0.5;False;3;FLOAT;1;False;4;FLOAT;1;False;3;FLOAT2;0;FLOAT;55;FLOAT;56
Node;AmplifyShaderEditor.PannerNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;291;-12416,-3840;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.ComponentMaskNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;437;-19968,-128;Inherit;False;True;False;True;True;1;0;FLOAT2;0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ComponentMaskNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;438;-19968,128;Inherit;False;False;True;True;True;1;0;FLOAT2;0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.BreakToComponentsNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;439;-19200,128;Inherit;False;FLOAT2;1;0;FLOAT2;0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.PannerNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;440;-19200,512;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;1,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.PannerNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;441;-19200,784;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,1;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TexCoordVertexDataNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;28;-848,-1616;Inherit;False;1;4;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;290;-12032,-3712;Inherit;False;Property;_TwistIntensity;Twist Intensity;26;0;Create;True;0;0;0;False;3;Space(33);Header(Twist);Space(13);False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;392;-13234,-1202;Inherit;False;548;162.95;Speed;2;164;165;Speed;0,0,0,1;0;0
Node;AmplifyShaderEditor.TextureCoordinatesNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;242;-13184,-1536;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;287;-12416,-3968;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;289;-12032,-3840;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;442;-18960,-128;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;443;-18944,256;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ComponentMaskNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;444;-18944,512;Inherit;False;True;False;True;True;1;0;FLOAT2;0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ComponentMaskNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;445;-18944,784;Inherit;False;False;True;True;True;1;0;FLOAT2;0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;33;-592,-1552;Inherit;False;VTC Randoff;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DdxOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;244;-12800,-1536;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DdyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;245;-12800,-1408;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TexturePropertyNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;113;-7552,-1024;Inherit;True;Property;_NoisesTextureTertiary;Noises Texture Tertiary;18;0;Create;True;0;0;0;False;3;Space(33);Header(Noise Tertiary);Space(13);False;4a70ea1a536acfb46bbcac2460fbbf8a;4a70ea1a536acfb46bbcac2460fbbf8a;False;white;Auto;Texture2D;False;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;164;-13184,-1152;Inherit;False;Property;_OverallSpeed;Overall Speed;4;0;Create;True;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;288;-12032,-3968;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DynamicAppendNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;447;-18688,640;Inherit;True;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DynamicAppendNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;446;-18688,0;Inherit;True;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;450;-18304,256;Inherit;False;33;VTC Randoff;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;155;-11392,-3712;Inherit;False;Property;_NoisesTileVertical;Noises Tile Vertical;5;0;Create;True;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;156;-11392,-3584;Inherit;False;Property;_NoisesTileHorizontal;Noises Tile Horizontal;6;0;Create;True;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;153;-11648,-3840;Inherit;False;Property;_OverallUVScale;Overall UV Scale;3;0;Create;True;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;243;-12544,-1536;Inherit;False;DDX;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;247;-12544,-1408;Inherit;False;DDY;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;114;-7168,-1024;Inherit;False;Tex Noises Ter;-1;True;1;0;SAMPLER2D;;False;1;SAMPLER2D;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;165;-12928,-1152;Inherit;False;Overall Speed;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;449;-11648,-3968;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DynamicAppendNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;157;-11392,-3840;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;448;-18432,0;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;451;-18304,128;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;1.777;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;420;-20384,-1932;Inherit;False;165;Overall Speed;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;176;-17792,-256;Inherit;False;114;Tex Noises Ter;1;0;OBJECT;;False;1;SAMPLER2D;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;428;-17792,128;Inherit;False;243;DDX;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;429;-17792,256;Inherit;False;247;DDY;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;154;-11392,-3968;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;452;-18304,0;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.Vector2Node, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;255;-20384,-2060;Inherit;False;Property;_Noises01Speed;Noises 01 Speed;13;0;Create;True;0;0;0;False;0;False;-0.3,0.01;0.1,0.01;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.ComponentMaskNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;256;-20000,-1804;Inherit;False;False;True;True;True;1;0;FLOAT2;0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ComponentMaskNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;257;-20000,-2060;Inherit;False;True;False;True;True;1;0;FLOAT2;0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleTimeNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;258;-20000,-1932;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;177;-17792,-128;Inherit;True;Property;_TextureSample3;Texture Sample 0;25;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Instance;-1;Derivative;Texture2D;False;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;293;-11008,-3968;Inherit;False;twistedUVs;-1;True;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.Vector2Node, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;407;-20384,-908;Inherit;False;Property;_Noises02Speed;Noises 02 Speed;17;0;Create;True;0;0;0;False;0;False;-0.115,0.01;0.1,0.01;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;260;-19744,-1804;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;259;-19744,-2060;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ComponentMaskNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;178;-17408,-128;Inherit;False;True;True;False;True;1;0;FLOAT3;0,0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;294;-20352,-2304;Inherit;False;293;twistedUVs;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.ComponentMaskNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;408;-20000,-652;Inherit;False;False;True;True;True;1;0;FLOAT2;0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ComponentMaskNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;409;-20000,-908;Inherit;False;True;False;True;True;1;0;FLOAT2;0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleTimeNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;410;-20000,-780;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.Vector2Node, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;261;-20384,-2700;Inherit;False;Property;_Noises01Scale;Noises 01 Scale;12;0;Create;True;0;0;0;False;0;False;1,1;1,1;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;175;-16896,128;Inherit;False;Property;_Noises03DistortIntensity;Noises 03 Distort Intensity;21;0;Create;True;0;0;0;False;0;False;1;0.3;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;37;-896,-896;Inherit;False;637.8579;128.6712;Learn VFX now - https://clevender.com/;2;38;39;Learn VFX now - https://clevender.com/;0,0,0,1;0;0
Node;AmplifyShaderEditor.ComponentMaskNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;264;-20000,-2700;Inherit;False;True;False;True;True;1;0;FLOAT2;0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ComponentMaskNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;265;-20000,-2444;Inherit;False;False;True;True;True;1;0;FLOAT2;0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.BreakToComponentsNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;266;-19232,-2444;Inherit;False;FLOAT2;1;0;FLOAT2;0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.FunctionNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;179;-17152,-128;Inherit;False;ConstantBiasScale;-1;;39;63208df05c83e8e49a48ffbdce2e43a0;0;3;3;FLOAT2;0,0;False;1;FLOAT;-0.5;False;2;FLOAT;2;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;411;-19744,-652;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;412;-19744,-908;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.Vector2Node, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;401;-20384,-1548;Inherit;False;Property;_Noises02Scale;Noises 02 Scale;16;0;Create;True;0;0;0;False;0;False;1,1;1,1;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;174;-16512,128;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0.1;False;1;FLOAT;0
Node;AmplifyShaderEditor.PannerNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;262;-19200,-2048;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;1,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.PannerNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;263;-19200,-1792;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,1;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;270;-18992,-2700;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;269;-18976,-2316;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;173;-16384,-128;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;38;-880,-848;Inherit;False;Property;_ClevendercomLearnVFXNow;- Clevender.com - Learn VFX Now!;1;0;Create;True;0;0;0;False;3;Space(33);Header(Clevender);Space(13);False;777;777;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.ComponentMaskNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;402;-20000,-1548;Inherit;False;True;False;True;True;1;0;FLOAT2;0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ComponentMaskNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;403;-20000,-1292;Inherit;False;False;True;True;True;1;0;FLOAT2;0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.BreakToComponentsNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;404;-19232,-1292;Inherit;False;FLOAT2;1;0;FLOAT2;0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.PannerNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;415;-19232,-908;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;1,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.PannerNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;416;-19232,-636;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,1;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.ComponentMaskNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;268;-18944,-2048;Inherit;False;True;False;True;True;1;0;FLOAT2;0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ComponentMaskNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;267;-18944,-1792;Inherit;False;False;True;True;True;1;0;FLOAT2;0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;241;-592,-1248;Inherit;False;Property;_EnableRandomRotation;Enable Random Rotation;7;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;272;-18720,-2572;Inherit;True;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;172;-16128,-128;Inherit;False;Noises Ter Dist;-1;True;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;39;-496,-848;Inherit;False;Clevender;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;405;-18992,-1548;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;414;-18976,-1164;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ComponentMaskNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;417;-18976,-908;Inherit;False;True;False;True;True;1;0;FLOAT2;0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ComponentMaskNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;418;-18976,-636;Inherit;False;False;True;True;True;1;0;FLOAT2;0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;271;-18688,-1920;Inherit;True;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;240;-592,-1344;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;422;-18304,-2432;Inherit;False;172;Noises Ter Dist;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;273;-18416,-2560;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;456;-18304,-1920;Inherit;False;39;Clevender;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;457;-18304,-2048;Inherit;False;33;VTC Randoff;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;406;-18720,-1420;Inherit;True;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DynamicAppendNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;419;-18720,-780;Inherit;True;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TexturePropertyNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;109;-7552,-1536;Inherit;True;Property;_NoisesTexture;Noises Texture;10;0;Create;True;0;0;0;False;3;Space(33);Header(Noise);Space(13);False;450de62c8d369e3459c2dfe0a483fe2e;450de62c8d369e3459c2dfe0a483fe2e;False;white;Auto;Texture2D;False;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.TexturePropertyNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;111;-7552,-1280;Inherit;True;Property;_NoisesTextureSecondary;Noises Texture Secondary;14;0;Create;True;0;0;0;False;3;Space(33);Header(Noise Secondary);Space(13);False;d3916979a6f6c144fa51bfbd29afce12;d3916979a6f6c144fa51bfbd29afce12;False;white;Auto;Texture2D;False;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;35;-592,-1424;Inherit;False;VTC Randrot;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;423;-18304,-2560;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;458;-18304,-2176;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;413;-18464,-1420;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;425;-18336,-1292;Inherit;False;172;Noises Ter Dist;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;453;-18320,-896;Inherit;False;33;VTC Randoff;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;110;-7168,-1536;Inherit;False;Tex Noises;-1;True;1;0;SAMPLER2D;;False;1;SAMPLER2D;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;112;-7168,-1280;Inherit;False;Tex Noises Sec;-1;True;1;0;SAMPLER2D;;False;1;SAMPLER2D;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;427;-18080,-2060;Inherit;False;35;VTC Randrot;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;459;-18304,-2304;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;424;-18336,-1420;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;454;-18320,-1024;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;1.333;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;393;-13362,-306;Inherit;False;2212;570.9;Dist Mask;12;318;317;319;315;325;320;383;316;385;384;386;387;Dist Mask;0,0,0,1;0;0
Node;AmplifyShaderEditor.RotatorNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;426;-18080,-2188;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;2;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;143;-17792,-1920;Inherit;False;110;Tex Noises;1;0;OBJECT;;False;1;SAMPLER2D;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;396;-17792,-1536;Inherit;False;243;DDX;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;397;-17792,-1408;Inherit;False;247;DDY;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;214;-17792,-1152;Inherit;False;112;Tex Noises Sec;1;0;OBJECT;;False;1;SAMPLER2D;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;398;-17792,-768;Inherit;False;243;DDX;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;399;-17792,-640;Inherit;False;247;DDY;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;455;-18320,-1152;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TextureCoordinatesNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;318;-13312,-256;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;319;-13312,0;Inherit;False;Property;_RadialDistortMaskSharpness;Radial Distort Mask Sharpness;23;0;Create;True;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.Vector4Node, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;146;-17408,-1664;Inherit;False;Property;_Noises01Selector;Noises 01 Selector;11;0;Create;True;0;0;0;False;0;False;0,1,0,0;1,0,0,0;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;142;-17792,-1792;Inherit;True;Property;_TextureSample2;Texture Sample 0;25;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Instance;-1;Derivative;Texture2D;False;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.Vector4Node, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;217;-17408,-896;Inherit;False;Property;_Noises02Selector;Noises 02 Selector;15;0;Create;True;0;0;0;False;0;False;1,0,0,0;0,0.5,0,0;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;215;-17792,-1024;Inherit;True;Property;_TextureSample4;Texture Sample 0;25;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Instance;-1;Derivative;Texture2D;False;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;317;-13312,-128;Inherit;False;Property;_RadialDistortMaskSize;Radial Distort Mask Size;22;0;Create;True;0;0;0;False;3;Space(33);Header(Radial Distort Mask);Space(13);False;0;0.64;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;315;-12928,-256;Inherit;False;RadialGradient;-1;;40;ec972f7745a8353409da2eb8d000a2e3;0;3;1;FLOAT2;0,0;False;6;FLOAT;0;False;7;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DotProductOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;144;-17408,-1792;Inherit;False;2;0;COLOR;0,0,0,0;False;1;FLOAT4;0,0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DotProductOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;218;-17408,-1024;Inherit;False;2;0;COLOR;0,0,0,0;False;1;FLOAT4;0,0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;320;-12544,-256;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;383;-12288,128;Inherit;False;Property;_RadialDistortionMaskEroSmo;Radial Distortion Mask Ero Smo;25;0;Create;True;0;0;0;False;0;False;0.5;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;147;-17152,-1792;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;219;-17152,-1024;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;316;-12288,-256;Inherit;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;384;-11904,128;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;140;-16768,-1792;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;385;-12288,0;Inherit;False;Property;_RadialDistortionMaskEro;Radial Distortion Mask Ero;24;0;Create;True;0;0;0;False;0;False;0;0.7;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SmoothstepOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;386;-11904,-256;Inherit;True;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;220;-16640,-1792;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;463;-16384,-1920;Inherit;False;Property;_DistortionInversion;Distortion Inversion;55;0;Create;True;0;0;0;False;0;False;-1;-1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TexCoordVertexDataNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;27;-848,-1872;Inherit;False;0;4;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;387;-11648,-256;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;462;-16384,-1792;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;31;-592,-1808;Inherit;False;VTC Dist;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;325;-11392,-256;Inherit;False;radialDistMask;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;279;-16128,-1792;Inherit;False;UV Dist;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;133;-9856,1152;Inherit;False;Property;_DistortionIntensity;Distortion Intensity;33;0;Create;True;0;0;0;False;3;Space(33);Header(Distortion);Space(13);False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;135;-9856,1280;Inherit;False;31;VTC Dist;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;326;-10624,1152;Inherit;False;325;radialDistMask;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;350;-10624,1024;Inherit;False;279;UV Dist;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;115;-7602,2894;Inherit;False;996;418.95;Angle based erosion;8;81;80;82;79;83;87;85;86;Angle based erosion;0,0,0,1;0;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;134;-9600,1152;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;123;-10368,1024;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;116;-7602,2382;Inherit;False;868;290.95;Depth fade based erosion;6;78;73;74;75;76;77;Depth fade based erosion;0,0,0,1;0;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;81;-7552,2944;Inherit;False;Property;_FresnelBias;Fresnel Bias;46;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;80;-7552,3072;Inherit;False;Property;_FresnelScale;Fresnel Scale;47;0;Create;True;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;82;-7552,3200;Inherit;False;Property;_FresnelPower;Fresnel Power;48;0;Create;True;0;0;0;False;0;False;5;3;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;131;-9856,1024;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.FresnelNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;79;-7296,2944;Inherit;False;Standard;WorldNormal;ViewDir;False;True;5;0;FLOAT3;0,0,1;False;4;FLOAT3;0,0,0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;5;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;78;-7552,2560;Inherit;False;Property;_DepthFade;Depth Fade;44;0;Create;True;0;0;0;False;3;Space(33);Header(Depth Fade);Space(13);False;0.1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.Vector2Node, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;372;-10624,-128;Inherit;False;Constant;_Vector2;Vector 1;57;0;Create;True;0;0;0;False;0;False;0.5,0.5;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;374;-10624,128;Inherit;False;Constant;_Float7;Float 1;57;0;Create;True;0;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;375;-10624,-256;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;373;-10624,0;Inherit;False;Constant;_Float6;Float 0;57;0;Create;True;0;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TexturePropertyNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;104;-7552,-1792;Inherit;True;Property;_MasksTexture;Masks Texture;30;0;Create;True;0;0;0;False;3;Space(33);Header(Masks);Space(13);False;d7c607b86d2feaf489cc0f4870583d9e;d7c607b86d2feaf489cc0f4870583d9e;False;white;Auto;Texture2D;False;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;382;-9600,1024;Inherit;False;finalDist;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;83;-7040,2944;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DepthFade, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;73;-7552,2432;Inherit;False;True;True;False;2;1;FLOAT3;0,0,0;False;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;87;-6912,3072;Inherit;False;Property;_FresnelIntensity;Fresnel Intensity;45;0;Create;True;0;0;0;False;3;Space(33);Header(Fresnel);Space(13);False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;371;-10240,-256;Inherit;False;Polar Coordinates;-1;;41;7dab8e02884cf104ebefaa2e788e4162;0;4;1;FLOAT2;0,0;False;2;FLOAT2;0.5,0.5;False;3;FLOAT;1;False;4;FLOAT;1;False;3;FLOAT2;0;FLOAT;55;FLOAT;56
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;377;-10112,0;Inherit;False;382;finalDist;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;106;-7168,-1792;Inherit;False;Tex Masks;-1;True;1;0;SAMPLER2D;;False;1;SAMPLER2D;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;85;-6912,2944;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;74;-7296,2432;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;376;-9984,-256;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;395;-9728,0;Inherit;False;106;Tex Masks;1;0;OBJECT;;False;1;SAMPLER2D;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;366;-9344,96;Inherit;False;Property;_MaskErosionSmoothness;Mask Erosion Smoothness;32;0;Create;True;0;0;0;False;0;False;0.3;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;464;-9728,128;Inherit;False;243;DDX;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;465;-9728,256;Inherit;False;247;DDY;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;86;-6784,2944;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;75;-7168,2432;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;365;-9088,0;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;363;-9344,0;Inherit;False;Property;_MaskErosion;Mask Erosion;31;0;Create;True;0;0;0;False;0;False;0.7;0.7;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;394;-9728,-256;Inherit;True;Property;_TextureSample0;Texture Sample 0;59;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Instance;-1;Derivative;Texture2D;False;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;29;-592,-1872;Inherit;False;VTC Ero;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;34;-592,-1488;Inherit;False;VTC Ero Smooth;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;76;-7008,2432;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SmoothstepOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;361;-8960,-256;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;77;-6912,2432;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;362;-8704,-256;Inherit;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;97;-7552,512;Inherit;False;34;VTC Ero Smooth;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;91;-7552,256;Inherit;False;29;VTC Ero;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;96;-7552,384;Inherit;False;Property;_LUTErosionSmoothness;LUT Erosion Smoothness;41;0;Create;True;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;389;-8448,-256;Inherit;False;finalMask;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;95;-7552,128;Inherit;False;Property;_LUTErosion;LUT Erosion;40;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;93;-7168,256;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;98;-6912,384;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;390;-7552,-256;Inherit;False;389;finalMask;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;92;-7168,128;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;94;-6912,128;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SmoothstepOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;99;-6912,-256;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;58;-5760,-128;Inherit;False;Property;_LUTAmplitude;LUT Amplitude;39;0;Create;True;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;61;-6016,-256;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;57;-5760,-256;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;59;-5504,-128;Inherit;False;Property;_LUTOffset;LUT Offset;37;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;68;-7552,1280;Inherit;False;29;VTC Ero;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;71;-7552,1408;Inherit;False;Property;_ErosionSmoothness;Erosion Smoothness;9;0;Create;True;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;72;-7552,1536;Inherit;False;34;VTC Ero Smooth;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;56;-5504,-256;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;60;-5248,-128;Inherit;False;Property;_LUTSpeed;LUT Speed;38;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;66;-7552,1152;Inherit;False;Property;_Erosion;Erosion;8;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;69;-7296,1280;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;70;-7040,1408;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.PannerNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;55;-5248,-256;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;67;-7296,1152;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;65;-7040,1152;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;54;-4864,-256;Inherit;True;Property;_LUT;LUT;36;0;Create;True;0;0;0;False;3;Space(33);Header(LUT);Space(13);False;-1;e561bead12b97374ab56577fcb4b14c7;e561bead12b97374ab56577fcb4b14c7;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;False;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;117;-2354,846;Inherit;False;484;674.95;Cam Distance fade;6;18;17;16;20;21;22;Cam Distance fade;0,0,0,1;0;0
Node;AmplifyShaderEditor.SmoothstepOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;62;-6912,768;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RGBToHSVNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;48;-4224,-256;Inherit;False;1;0;FLOAT3;0,0,0;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;50;-3968,-128;Inherit;False;Property;_HueShift;Hue Shift;42;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;18;-2304,1408;Inherit;False;Property;_CameraDepthFadeOffset;Camera Depth Fade Offset;35;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;17;-2304,1280;Inherit;False;Property;_CameraDepthFadeLength;Camera Depth Fade Length;34;0;Create;True;0;0;0;False;3;Space(33);Header(Camera Depth Fade);Space(13);False;0.333;0.3;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;64;-6656,768;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;49;-3968,-256;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;238;-7602,3662;Inherit;False;1188;722.8501;Push Particle toward camera direction (no more glow clipping in the ground) | 0=Disabled;9;228;229;230;231;232;233;234;235;236;Camera Offset;0,0,0,1;0;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;89;-6656,2048;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CameraDepthFade, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;16;-2304,1152;Inherit;False;3;2;FLOAT3;0,0,0;False;0;FLOAT;1;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;32;-592,-1616;Inherit;False;VTC Em;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.HSVToRGBNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;47;-3712,-256;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;52;-3456,-128;Inherit;False;Property;_Desaturate;Desaturate;43;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.VertexColorNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;88;-7552,1792;Inherit;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;234;-7552,4096;Inherit;False;Property;_CameraOffset;Camera Offset;49;0;Create;True;0;0;0;False;3;Space(33);Header(Camera Offset);Space(13);False;0;-15;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.WorldPosInputsNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;228;-7552,3712;Inherit;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.WorldSpaceCameraPos, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;231;-7552,3856;Inherit;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;90;-6400,2048;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;20;-2048,1152;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;45;-2176,256;Inherit;False;32;VTC Em;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.DesaturateOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;51;-3456,-256;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;43;-2176,128;Inherit;False;Property;_Emissive;Emissive;2;0;Create;True;0;0;0;False;3;Space(33);Header(General);Space(13);False;1;5;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;230;-7296,3712;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;232;-6912,3840;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0.01;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;21;-2304,896;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;41;-2048,128;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;46;-2432,0;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;229;-6912,3712;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;22;-2048,896;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;40;-2048,0;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;10;848,-48;Inherit;False;1252;162.95;Lush was here! <3;5;15;14;13;12;11;Lush was here! <3;0,0,0,1;0;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;236;-6656,3712;Inherit;False;Cam Offset;-1;True;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;23;-1792,896;Inherit;False;Op;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;25;-1792,0;Inherit;False;Col;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;233;-6912,3968;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;11;1408,0;Inherit;False;Property;_Dst;Dst;52;0;Create;True;0;0;0;True;0;False;10;10;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;12;1152,0;Inherit;False;Property;_Src;Src;51;0;Create;True;0;0;0;True;0;False;5;5;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;13;1664,0;Inherit;False;Property;_ZWrite;ZWrite;53;0;Create;True;0;0;0;True;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;14;1920,0;Inherit;False;Property;_ZTest;ZTest;54;0;Create;True;0;0;0;True;0;False;2;2;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;24;-384,128;Inherit;False;23;Op;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;26;-384,0;Inherit;False;25;Col;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;15;896,0;Inherit;False;Property;_Cull;Cull;50;0;Create;True;0;0;0;True;3;Space(33);Header(AR);Space(13);False;2;2;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;239;-384,256;Inherit;False;236;Cam Offset;1;0;OBJECT;;False;1;FLOAT3;0
Node;AmplifyShaderEditor.TextureCoordinatesNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;235;-7552,4224;Inherit;False;3;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;379;-384,-128;Inherit;False;378;DEBUG;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;378;-8448,-128;Inherit;False;DEBUG;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;460;-19456,-2176;Inherit;False;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;466;0,0;Float;False;True;-1;3;AmplifyShaderEditor.MaterialInspector;0;0;Unlit;Vefects/SH_Vefects_VFX_Stylized_Fire_Radial_01_BIRP;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;Back;0;True;_ZWrite;0;True;_ZTest;False;0;False;;0;False;;False;0;0;False;;0;Custom;0.5;True;False;0;False;Transparent;;Transparent;All;12;all;True;True;True;True;0;False;;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;2;15;10;25;False;0.5;False;1;5;True;_Src;10;True;_Dst;0;0;False;;0;False;;0;False;;0;False;;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;True;Relative;0;;0;-1;-1;-1;0;False;0;0;True;_Cull;-1;0;False;;0;0;0;False;0.1;False;;0;False;;False;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;16;FLOAT4;0,0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
Node;AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;461;-19506,-2226;Inherit;False;228;162.95;Connect to UV in the panner;0;Connect to UV in the panner;0,0,0,1;0;0
WireConnection;431;0;430;0
WireConnection;432;0;430;0
WireConnection;433;0;420;0
WireConnection;434;0;433;0
WireConnection;434;1;431;0
WireConnection;435;0;432;0
WireConnection;435;1;433;0
WireConnection;286;1;295;0
WireConnection;286;2;285;0
WireConnection;286;3;284;0
WireConnection;286;4;283;0
WireConnection;291;2;292;0
WireConnection;437;0;436;0
WireConnection;438;0;436;0
WireConnection;439;0;294;0
WireConnection;440;1;435;0
WireConnection;441;1;434;0
WireConnection;287;0;286;0
WireConnection;287;1;291;0
WireConnection;289;0;286;55
WireConnection;289;1;290;0
WireConnection;442;0;437;0
WireConnection;442;1;439;0
WireConnection;443;0;439;1
WireConnection;443;1;438;0
WireConnection;444;0;440;0
WireConnection;445;0;441;0
WireConnection;33;0;28;2
WireConnection;244;0;242;1
WireConnection;245;0;242;2
WireConnection;288;0;287;0
WireConnection;288;1;289;0
WireConnection;447;0;444;0
WireConnection;447;1;445;0
WireConnection;446;0;442;0
WireConnection;446;1;443;0
WireConnection;243;0;244;0
WireConnection;247;0;245;0
WireConnection;114;0;113;0
WireConnection;165;0;164;0
WireConnection;449;0;288;0
WireConnection;449;1;153;0
WireConnection;157;0;155;0
WireConnection;157;1;156;0
WireConnection;448;0;446;0
WireConnection;448;1;447;0
WireConnection;451;0;450;0
WireConnection;154;0;449;0
WireConnection;154;1;157;0
WireConnection;452;0;448;0
WireConnection;452;1;451;0
WireConnection;256;0;255;0
WireConnection;257;0;255;0
WireConnection;258;0;420;0
WireConnection;177;0;176;0
WireConnection;177;1;452;0
WireConnection;177;3;428;0
WireConnection;177;4;429;0
WireConnection;293;0;154;0
WireConnection;260;0;258;0
WireConnection;260;1;256;0
WireConnection;259;0;257;0
WireConnection;259;1;258;0
WireConnection;178;0;177;5
WireConnection;408;0;407;0
WireConnection;409;0;407;0
WireConnection;410;0;420;0
WireConnection;264;0;261;0
WireConnection;265;0;261;0
WireConnection;266;0;294;0
WireConnection;179;3;178;0
WireConnection;411;0;410;0
WireConnection;411;1;408;0
WireConnection;412;0;409;0
WireConnection;412;1;410;0
WireConnection;174;0;175;0
WireConnection;262;1;259;0
WireConnection;263;1;260;0
WireConnection;270;0;264;0
WireConnection;270;1;266;0
WireConnection;269;0;266;1
WireConnection;269;1;265;0
WireConnection;173;0;179;0
WireConnection;173;1;174;0
WireConnection;402;0;401;0
WireConnection;403;0;401;0
WireConnection;404;0;294;0
WireConnection;415;1;412;0
WireConnection;416;1;411;0
WireConnection;268;0;262;0
WireConnection;267;0;263;0
WireConnection;272;0;270;0
WireConnection;272;1;269;0
WireConnection;172;0;173;0
WireConnection;39;0;38;0
WireConnection;405;0;402;0
WireConnection;405;1;404;0
WireConnection;414;0;404;1
WireConnection;414;1;403;0
WireConnection;417;0;415;0
WireConnection;418;0;416;0
WireConnection;271;0;268;0
WireConnection;271;1;267;0
WireConnection;240;0;28;4
WireConnection;240;1;241;0
WireConnection;273;0;272;0
WireConnection;273;1;271;0
WireConnection;406;0;405;0
WireConnection;406;1;414;0
WireConnection;419;0;417;0
WireConnection;419;1;418;0
WireConnection;35;0;240;0
WireConnection;423;0;273;0
WireConnection;423;1;422;0
WireConnection;458;0;457;0
WireConnection;458;1;456;0
WireConnection;413;0;406;0
WireConnection;413;1;419;0
WireConnection;110;0;109;0
WireConnection;112;0;111;0
WireConnection;459;0;423;0
WireConnection;459;1;458;0
WireConnection;424;0;413;0
WireConnection;424;1;425;0
WireConnection;454;0;453;0
WireConnection;426;0;459;0
WireConnection;426;2;427;0
WireConnection;455;0;424;0
WireConnection;455;1;454;0
WireConnection;142;0;143;0
WireConnection;142;1;426;0
WireConnection;142;3;396;0
WireConnection;142;4;397;0
WireConnection;215;0;214;0
WireConnection;215;1;455;0
WireConnection;215;3;398;0
WireConnection;215;4;399;0
WireConnection;315;1;318;0
WireConnection;315;6;317;0
WireConnection;315;7;319;0
WireConnection;144;0;142;0
WireConnection;144;1;146;0
WireConnection;218;0;215;0
WireConnection;218;1;217;0
WireConnection;320;0;315;0
WireConnection;147;0;144;0
WireConnection;219;0;218;0
WireConnection;316;0;320;0
WireConnection;384;0;385;0
WireConnection;384;1;383;0
WireConnection;140;0;147;0
WireConnection;140;1;219;0
WireConnection;386;0;316;0
WireConnection;386;1;385;0
WireConnection;386;2;384;0
WireConnection;220;0;140;0
WireConnection;387;0;386;0
WireConnection;462;0;220;0
WireConnection;462;1;463;0
WireConnection;31;0;27;4
WireConnection;325;0;387;0
WireConnection;279;0;462;0
WireConnection;134;0;133;0
WireConnection;134;1;135;0
WireConnection;123;0;350;0
WireConnection;123;1;326;0
WireConnection;131;0;123;0
WireConnection;131;1;134;0
WireConnection;79;1;81;0
WireConnection;79;2;80;0
WireConnection;79;3;82;0
WireConnection;382;0;131;0
WireConnection;83;0;79;0
WireConnection;73;0;78;0
WireConnection;371;1;375;0
WireConnection;371;2;372;0
WireConnection;371;3;373;0
WireConnection;371;4;374;0
WireConnection;106;0;104;0
WireConnection;85;0;83;0
WireConnection;85;1;87;0
WireConnection;74;0;73;0
WireConnection;376;0;371;0
WireConnection;376;1;377;0
WireConnection;86;0;85;0
WireConnection;75;0;74;0
WireConnection;365;0;363;0
WireConnection;365;1;366;0
WireConnection;394;0;395;0
WireConnection;394;1;376;0
WireConnection;394;3;464;0
WireConnection;394;4;465;0
WireConnection;29;0;27;3
WireConnection;34;0;28;3
WireConnection;76;0;75;0
WireConnection;76;1;86;0
WireConnection;361;0;394;2
WireConnection;361;1;363;0
WireConnection;361;2;365;0
WireConnection;77;0;76;0
WireConnection;362;0;361;0
WireConnection;389;0;362;0
WireConnection;93;0;91;0
WireConnection;93;1;77;0
WireConnection;98;0;96;0
WireConnection;98;1;97;0
WireConnection;92;0;95;0
WireConnection;92;1;93;0
WireConnection;94;0;92;0
WireConnection;94;1;98;0
WireConnection;99;0;390;0
WireConnection;99;1;92;0
WireConnection;99;2;94;0
WireConnection;61;0;99;0
WireConnection;57;0;61;0
WireConnection;57;1;58;0
WireConnection;56;0;57;0
WireConnection;56;1;59;0
WireConnection;69;0;68;0
WireConnection;69;1;77;0
WireConnection;70;0;71;0
WireConnection;70;1;72;0
WireConnection;55;0;56;0
WireConnection;55;2;60;0
WireConnection;67;0;66;0
WireConnection;67;1;69;0
WireConnection;65;0;67;0
WireConnection;65;1;70;0
WireConnection;54;1;55;0
WireConnection;62;0;390;0
WireConnection;62;1;67;0
WireConnection;62;2;65;0
WireConnection;48;0;54;5
WireConnection;64;0;62;0
WireConnection;49;0;48;1
WireConnection;49;1;50;0
WireConnection;89;0;64;0
WireConnection;89;1;88;4
WireConnection;16;0;17;0
WireConnection;16;1;18;0
WireConnection;32;0;28;1
WireConnection;47;0;49;0
WireConnection;47;1;48;2
WireConnection;47;2;48;3
WireConnection;90;0;89;0
WireConnection;20;0;16;0
WireConnection;51;0;47;0
WireConnection;51;1;52;0
WireConnection;230;0;228;0
WireConnection;230;1;231;0
WireConnection;232;0;234;0
WireConnection;21;0;90;0
WireConnection;21;1;20;0
WireConnection;41;0;43;0
WireConnection;41;1;45;0
WireConnection;46;0;51;0
WireConnection;46;1;88;0
WireConnection;229;0;230;0
WireConnection;229;1;232;0
WireConnection;22;0;21;0
WireConnection;40;0;46;0
WireConnection;40;1;41;0
WireConnection;236;0;229;0
WireConnection;23;0;22;0
WireConnection;25;0;40;0
WireConnection;233;0;234;0
WireConnection;233;1;235;2
WireConnection;378;0;362;0
WireConnection;460;0;294;0
WireConnection;466;2;26;0
WireConnection;466;9;24;0
WireConnection;466;11;239;0
ASEEND*/
//CHKSM=6B7E1386E56929E700E8EB9E735DF64C432DA01C