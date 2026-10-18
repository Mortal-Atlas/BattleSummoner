// Made with Amplify Shader Editor v1.9.9.7
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Vefects/SH_Vefects_VFX_Stylized_Fire_01_BIRP"
{
	Properties
	{
		[Space(33)][Header(Clevender)][Space(13)] _ClevendercomLearnVFXNow( "- Clevender.com - Learn VFX Now!", Float ) = 777
		[Space(33)][Header(General)][Space(13)] _Emissive( "Emissive", Float ) = 1
		_OverallScale( "Overall Scale", Float ) = 1
		_OverallSpeed( "Overall Speed", Float ) = 1
		_NoisesTileVertical( "Noises Tile Vertical", Float ) = 1
		_NoisesTileHorizontal( "Noises Tile Horizontal", Float ) = 1
		_EnableRandomRotation( "Enable Random Rotation", Float ) = 0
		_Erosion( "Erosion", Float ) = 0
		_ErosionSmoothness( "Erosion Smoothness", Float ) = 1
		[Space(33)][Header(Noise)][Space(13)] _NoisesTexture( "Noises Texture", 2D ) = "white" {}
		_Noises01Selector( "Noises 01 Selector", Vector ) = ( 0, 1, 0, 0 )
		_Noises01Scale( "Noises 01 Scale", Vector ) = ( 1, 1, 0, 0 )
		_Noises01Speed( "Noises 01 Speed", Vector ) = ( 0.1, -1, 0, 0 )
		[Space(33)][Header(Noise Secondary)][Space(13)] _NoisesTextureSecondary( "Noises Texture Secondary", 2D ) = "white" {}
		_Noises02Selector( "Noises 02 Selector", Vector ) = ( 1, 0, 0, 0 )
		_Noises02Scale( "Noises 02 Scale", Vector ) = ( 0.555, 0.555, 0, 0 )
		_Noises02Speed( "Noises 02 Speed", Vector ) = ( -0.01, -0.2, 0, 0 )
		[Space(33)][Header(Noise Tertiary)][Space(13)] _NoisesTextureTertiary( "Noises Texture Tertiary", 2D ) = "white" {}
		_Noises03Scale( "Noises 03 Scale", Vector ) = ( 1, 1, 0, 0 )
		_Noises03Speed( "Noises 03 Speed", Vector ) = ( 0.05, -0.5, 0, 0 )
		[Space(33)][Header(Masks)][Space(13)] _MasksTexture( "Masks Texture", 2D ) = "white" {}
		[Space(33)][Header(Distortion)][Space(13)] _DistortionIntensity( "Distortion Intensity", Float ) = 1
		_DistortionVertical( "Distortion Vertical", Float ) = -1
		_DistortionHorizontal( "Distortion Horizontal", Float ) = 0.1
		_DistortionMaskEro( "Distortion Mask Ero", Float ) = 0
		_DistortionMaskEroSmoothness( "Distortion Mask Ero Smoothness", Float ) = 0.3
		_NoisesTertiaryDistortionIntensity( "Noises Tertiary Distortion Intensity", Float ) = 1
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
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] _texcoord2( "", 2D ) = "white" {}
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
		uniform sampler2D _MasksTexture;
		uniform sampler2D _NoisesTexture;
		uniform float2 _Noises01Speed;
		uniform float _OverallSpeed;
		uniform float _OverallScale;
		uniform float _NoisesTileVertical;
		uniform float _NoisesTileHorizontal;
		uniform float2 _Noises01Scale;
		uniform float _ClevendercomLearnVFXNow;
		uniform sampler2D _NoisesTextureTertiary;
		uniform float2 _Noises03Speed;
		uniform float2 _Noises03Scale;
		uniform float _NoisesTertiaryDistortionIntensity;
		uniform float _EnableRandomRotation;
		uniform float4 _Noises01Selector;
		uniform sampler2D _NoisesTextureSecondary;
		uniform float2 _Noises02Speed;
		uniform float2 _Noises02Scale;
		uniform float4 _Noises02Selector;
		uniform float _DistortionMaskEro;
		uniform float _DistortionMaskEroSmoothness;
		uniform float4 _MasksTexture_ST;
		uniform float _DistortionIntensity;
		uniform float _DistortionHorizontal;
		uniform float _DistortionVertical;
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
			float Overall_Speed165 = _OverallSpeed;
			float2 appendResult157 = (float2(_NoisesTileVertical , _NoisesTileHorizontal));
			float2 temp_output_154_0 = ( ( i.uv_texcoord.xy * _OverallScale ) * appendResult157 );
			float2 panner160 = ( 1.0 * _Time.y * ( _Noises01Speed * Overall_Speed165 ) + ( temp_output_154_0 * _Noises01Scale ));
			float VTC_Randoff33 = i.uv2_texcoord2.y;
			float Clevender39 = _ClevendercomLearnVFXNow;
			float2 panner226 = ( 1.0 * _Time.y * ( _Noises03Speed * Overall_Speed165 ) + ( temp_output_154_0 * _Noises03Scale ));
			float2 Noises_Ter_Dist172 = ( ( ( (tex2D( _NoisesTextureTertiary, ( panner226 + ( VTC_Randoff33 * 1.777 ) ) ).rgb).xy + -0.5 ) * 2.0 ) * ( _NoisesTertiaryDistortionIntensity * 0.1 ) );
			float VTC_Randrot35 = ( i.uv2_texcoord2.w * _EnableRandomRotation );
			float cos148 = cos( VTC_Randrot35 );
			float sin148 = sin( VTC_Randrot35 );
			float2 rotator148 = mul( ( ( panner160 + ( VTC_Randoff33 * Clevender39 ) ) + Noises_Ter_Dist172 ) - float2( 0,0 ) , float2x2( cos148 , -sin148 , sin148 , cos148 )) + float2( 0,0 );
			float dotResult144 = dot( tex2D( _NoisesTexture, rotator148 ) , _Noises01Selector );
			float2 panner207 = ( 1.0 * _Time.y * ( _Noises02Speed * Overall_Speed165 ) + ( temp_output_154_0 * _Noises02Scale ));
			float dotResult218 = dot( tex2D( _NoisesTextureSecondary, ( ( panner207 + ( VTC_Randoff33 * 1.333 ) ) + Noises_Ter_Dist172 ) ) , _Noises02Selector );
			float temp_output_140_0 = ( saturate( dotResult144 ) + saturate( dotResult218 ) );
			float2 uv_MasksTexture = i.uv_texcoord * _MasksTexture_ST.xy + _MasksTexture_ST.zw;
			float smoothstepResult118 = smoothstep( _DistortionMaskEro , ( _DistortionMaskEro + _DistortionMaskEroSmoothness ) , tex2D( _MasksTexture, uv_MasksTexture ).r);
			float VTC_Dist31 = i.uv_texcoord.w;
			float temp_output_132_0 = ( saturate( i.uv_texcoord.xy.y ) * ( saturate( ( temp_output_140_0 * saturate( smoothstepResult118 ) ) ) * ( _DistortionIntensity * VTC_Dist31 ) ) );
			float2 appendResult127 = (float2(( i.uv_texcoord.xy.x + ( temp_output_132_0 * _DistortionHorizontal ) ) , ( i.uv_texcoord.xy.y + ( temp_output_132_0 * _DistortionVertical ) )));
			float4 tex2DNode102 = tex2D( _MasksTexture, appendResult127 );
			float smoothstepResult99 = smoothstep( temp_output_92_0 , ( temp_output_92_0 + ( _LUTErosionSmoothness * VTC_Ero_Smooth34 ) ) , tex2DNode102.g);
			float2 temp_cast_3 = (( ( saturate( smoothstepResult99 ) * _LUTAmplitude ) + _LUTOffset )).xx;
			float2 panner55 = ( 1.0 * _Time.y * temp_cast_0 + temp_cast_3);
			float3 hsvTorgb48 = RGBToHSV( tex2D( _LUT, panner55 ).rgb );
			float3 hsvTorgb47 = HSVToRGB( float3(( hsvTorgb48.x + _HueShift ),hsvTorgb48.y,hsvTorgb48.z) );
			float3 desaturateInitialColor51 = hsvTorgb47;
			float desaturateDot51 = dot( desaturateInitialColor51, float3( 0.299, 0.587, 0.114 ));
			float3 desaturateVar51 = lerp( desaturateInitialColor51, desaturateDot51.xxx, _Desaturate );
			float VTC_Em32 = i.uv2_texcoord2.x;
			float4 Col25 = ( ( float4( desaturateVar51 , 0.0 ) * i.vertexColor ) * ( _Emissive * VTC_Em32 ) );
			o.Emission = Col25.rgb;
			float temp_output_67_0 = ( _Erosion + ( VTC_Ero29 + temp_output_77_0 ) );
			float smoothstepResult62 = smoothstep( temp_output_67_0 , ( temp_output_67_0 + ( _ErosionSmoothness * VTC_Ero_Smooth34 ) ) , tex2DNode102.g);
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
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;221;-14720,3072;Inherit;False;165;Overall Speed;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.Vector2Node, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;223;-15488,2944;Inherit;False;Property;_Noises03Scale;Noises 03 Scale;19;0;Create;True;0;0;0;False;0;False;1,1;0.5,1;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.Vector2Node, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;222;-14976,2944;Inherit;False;Property;_Noises03Speed;Noises 03 Speed;20;0;Create;True;0;0;0;False;0;False;0.05,-0.5;0.067,-0.67;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;224;-15488,2816;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;225;-14720,2944;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TexturePropertyNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;113;-7552,-1024;Inherit;True;Property;_NoisesTextureTertiary;Noises Texture Tertiary;18;0;Create;True;0;0;0;False;3;Space(33);Header(Noise Tertiary);Space(13);False;4a70ea1a536acfb46bbcac2460fbbf8a;4a70ea1a536acfb46bbcac2460fbbf8a;False;white;Auto;Texture2D;False;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;181;-14336,3072;Inherit;False;33;VTC Randoff;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;114;-7168,-1024;Inherit;False;Tex Noises Ter;-1;True;1;0;SAMPLER2D;;False;1;SAMPLER2D;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;182;-14336,2944;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;1.777;False;1;FLOAT;0
Node;AmplifyShaderEditor.PannerNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;226;-14976,2816;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;176;-14080,2688;Inherit;False;114;Tex Noises Ter;1;0;OBJECT;;False;1;SAMPLER2D;0
Node;AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;180;-14336,2816;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;36;-896,-1920;Inherit;False;640;766.95;VTC;10;32;34;29;31;27;35;33;28;240;241;VTC;0,0,0,1;0;0
Node;AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;37;-896,-896;Inherit;False;637.8579;128.6712;Learn VFX now - https://clevender.com/;2;38;39;Learn VFX now - https://clevender.com/;0,0,0,1;0;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;164;-17792,896;Inherit;False;Property;_OverallSpeed;Overall Speed;4;0;Create;True;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;177;-14080,2816;Inherit;True;Property;_TextureSample3;Texture Sample 0;25;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Instance;-1;Auto;Texture2D;False;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.TextureCoordinatesNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;151;-16768,896;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;153;-16768,1024;Inherit;False;Property;_OverallScale;Overall Scale;3;0;Create;True;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;155;-16256,1024;Inherit;False;Property;_NoisesTileVertical;Noises Tile Vertical;5;0;Create;True;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;156;-16256,1152;Inherit;False;Property;_NoisesTileHorizontal;Noises Tile Horizontal;6;0;Create;True;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TexCoordVertexDataNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;28;-848,-1616;Inherit;False;1;4;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;38;-880,-848;Inherit;False;Property;_ClevendercomLearnVFXNow;- Clevender.com - Learn VFX Now!;1;0;Create;True;0;0;0;False;3;Space(33);Header(Clevender);Space(13);False;777;777;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;165;-17536,896;Inherit;False;Overall Speed;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;175;-13184,3072;Inherit;False;Property;_NoisesTertiaryDistortionIntensity;Noises Tertiary Distortion Intensity;27;0;Create;True;0;0;0;False;0;False;1;0.3;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.ComponentMaskNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;178;-13696,2816;Inherit;False;True;True;False;True;1;0;FLOAT3;0,0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;152;-16512,896;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DynamicAppendNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;157;-16000,1024;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;33;-592,-1552;Inherit;False;VTC Randoff;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;39;-496,-848;Inherit;False;Clevender;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;174;-12800,3072;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0.1;False;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;179;-13440,2816;Inherit;False;ConstantBiasScale;-1;;1;63208df05c83e8e49a48ffbdce2e43a0;0;3;3;FLOAT2;0,0;False;1;FLOAT;-0.5;False;2;FLOAT;2;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;154;-16000,896;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;163;-14720,1152;Inherit;False;165;Overall Speed;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.Vector2Node, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;161;-14976,1024;Inherit;False;Property;_Noises01Speed;Noises 01 Speed;13;0;Create;True;0;0;0;False;0;False;0.1,-1;-0.1,-1.3;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.Vector2Node, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;159;-15488,1024;Inherit;False;Property;_Noises01Scale;Noises 01 Scale;12;0;Create;True;0;0;0;False;0;False;1,1;1.333,1.5;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;173;-12672,2816;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;202;-14720,1920;Inherit;False;165;Overall Speed;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.Vector2Node, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;201;-14976,1792;Inherit;False;Property;_Noises02Speed;Noises 02 Speed;17;0;Create;True;0;0;0;False;0;False;-0.01,-0.2;-0.1,-1.3;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.Vector2Node, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;200;-15488,1792;Inherit;False;Property;_Noises02Scale;Noises 02 Scale;16;0;Create;True;0;0;0;False;0;False;0.555,0.555;1,0.7;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;169;-14336,1280;Inherit;False;39;Clevender;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;158;-15488,896;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;162;-14720,1024;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;168;-14336,1152;Inherit;False;33;VTC Randoff;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;241;-592,-1248;Inherit;False;Property;_EnableRandomRotation;Enable Random Rotation;7;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;205;-14336,1920;Inherit;False;33;VTC Randoff;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;172;-12416,2816;Inherit;False;Noises Ter Dist;-1;True;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;203;-15488,1664;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;204;-14720,1792;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.PannerNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;160;-14976,896;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;167;-14336,1024;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;240;-592,-1344;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;35;-592,-1424;Inherit;False;VTC Randrot;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;208;-14336,1792;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;1.333;False;1;FLOAT;0
Node;AmplifyShaderEditor.PannerNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;207;-14976,1664;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TexturePropertyNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;111;-7552,-1280;Inherit;True;Property;_NoisesTextureSecondary;Noises Texture Secondary;14;0;Create;True;0;0;0;False;3;Space(33);Header(Noise Secondary);Space(13);False;d3916979a6f6c144fa51bfbd29afce12;d3916979a6f6c144fa51bfbd29afce12;False;white;Auto;Texture2D;False;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;166;-14336,896;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;171;-13824,1024;Inherit;False;172;Noises Ter Dist;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TexturePropertyNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;109;-7552,-1536;Inherit;True;Property;_NoisesTexture;Noises Texture;10;0;Create;True;0;0;0;False;3;Space(33);Header(Noise);Space(13);False;450de62c8d369e3459c2dfe0a483fe2e;450de62c8d369e3459c2dfe0a483fe2e;False;white;Auto;Texture2D;False;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;112;-7168,-1280;Inherit;False;Tex Noises Sec;-1;True;1;0;SAMPLER2D;;False;1;SAMPLER2D;0
Node;AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;209;-14336,1664;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;210;-13824,1792;Inherit;False;172;Noises Ter Dist;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;170;-13824,896;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;110;-7168,-1536;Inherit;False;Tex Noises;-1;True;1;0;SAMPLER2D;;False;1;SAMPLER2D;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;150;-13056,1024;Inherit;False;35;VTC Randrot;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;212;-13824,1664;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;214;-12672,1536;Inherit;False;112;Tex Noises Sec;1;0;OBJECT;;False;1;SAMPLER2D;0
Node;AmplifyShaderEditor.RotatorNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;148;-13056,896;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;2;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;143;-12672,768;Inherit;False;110;Tex Noises;1;0;OBJECT;;False;1;SAMPLER2D;0
Node;AmplifyShaderEditor.SamplerNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;215;-12672,1664;Inherit;True;Property;_TextureSample4;Texture Sample 0;25;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Instance;-1;Auto;Texture2D;False;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.Vector4Node, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;217;-12288,1792;Inherit;False;Property;_Noises02Selector;Noises 02 Selector;15;0;Create;True;0;0;0;False;0;False;1,0,0,0;0,0.5,0,0;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;108;-10752,1296;Inherit;False;106;Tex Masks;1;0;OBJECT;;False;1;SAMPLER2D;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;121;-10752,1792;Inherit;False;Property;_DistortionMaskEroSmoothness;Distortion Mask Ero Smoothness;26;0;Create;True;0;0;0;False;0;False;0.3;0.5;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;142;-12672,896;Inherit;True;Property;_TextureSample2;Texture Sample 0;25;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Instance;-1;Auto;Texture2D;False;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.Vector4Node, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;146;-12288,1024;Inherit;False;Property;_Noises01Selector;Noises 01 Selector;11;0;Create;True;0;0;0;False;0;False;0,1,0,0;1,0,0,0;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.DotProductOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;218;-12288,1664;Inherit;False;2;0;COLOR;0,0,0,0;False;1;FLOAT4;0,0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;122;-10368,1664;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;120;-10752,1664;Inherit;False;Property;_DistortionMaskEro;Distortion Mask Ero;25;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;103;-10752,1408;Inherit;True;Property;_TextureSample1;Texture Sample 0;25;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Instance;-1;Auto;Texture2D;False;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.DotProductOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;144;-12288,896;Inherit;False;2;0;COLOR;0,0,0,0;False;1;FLOAT4;0,0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;115;-7602,2894;Inherit;False;996;418.95;Angle based erosion;8;81;80;82;79;83;87;85;86;Angle based erosion;0,0,0,1;0;0
Node;AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;219;-12032,1664;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TexCoordVertexDataNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;27;-848,-1872;Inherit;False;0;4;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SmoothstepOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;118;-10240,1408;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;147;-12032,896;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;116;-7602,2382;Inherit;False;868;290.95;Depth fade based erosion;6;78;73;74;75;76;77;Depth fade based erosion;0,0,0,1;0;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;81;-7552,2944;Inherit;False;Property;_FresnelBias;Fresnel Bias;40;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;80;-7552,3072;Inherit;False;Property;_FresnelScale;Fresnel Scale;41;0;Create;True;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;82;-7552,3200;Inherit;False;Property;_FresnelPower;Fresnel Power;42;0;Create;True;0;0;0;False;0;False;5;3;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;31;-592,-1808;Inherit;False;VTC Dist;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;119;-9984,1408;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;140;-11648,896;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.FresnelNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;79;-7296,2944;Inherit;False;Standard;WorldNormal;ViewDir;False;True;5;0;FLOAT3;0,0,1;False;4;FLOAT3;0,0,0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;5;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;78;-7552,2560;Inherit;False;Property;_DepthFade;Depth Fade;38;0;Create;True;0;0;0;False;3;Space(33);Header(Depth Fade);Space(13);False;0.1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;135;-9600,1280;Inherit;False;31;VTC Dist;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;133;-9600,1152;Inherit;False;Property;_DistortionIntensity;Distortion Intensity;22;0;Create;True;0;0;0;False;3;Space(33);Header(Distortion);Space(13);False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;123;-10752,896;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;83;-7040,2944;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DepthFade, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;73;-7552,2432;Inherit;False;True;True;False;2;1;FLOAT3;0,0,0;False;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;87;-6912,3072;Inherit;False;Property;_FresnelIntensity;Fresnel Intensity;39;0;Create;True;0;0;0;False;3;Space(33);Header(Fresnel);Space(13);False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;134;-9344,1152;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;136;-10752,-128;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;124;-10496,896;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;85;-6912,2944;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;74;-7296,2432;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;131;-9600,1024;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;137;-10496,-128;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;86;-6784,2944;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;75;-7168,2432;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;132;-9600,896;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;130;-9472,256;Inherit;False;Property;_DistortionHorizontal;Distortion Horizontal;24;0;Create;True;0;0;0;False;0;False;0.1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;139;-9472,640;Inherit;False;Property;_DistortionVertical;Distortion Vertical;23;0;Create;True;0;0;0;False;0;False;-1;-1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;29;-592,-1872;Inherit;False;VTC Ero;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;34;-592,-1488;Inherit;False;VTC Ero Smooth;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;76;-7008,2432;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TexturePropertyNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;104;-7552,-1792;Inherit;True;Property;_MasksTexture;Masks Texture;21;0;Create;True;0;0;0;False;3;Space(33);Header(Masks);Space(13);False;6954d810b425a864d9b8919b07d8fe74;6954d810b425a864d9b8919b07d8fe74;False;white;Auto;Texture2D;False;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.TextureCoordinatesNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;125;-10752,128;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;129;-9216,256;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;138;-9216,640;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;106;-7168,-1792;Inherit;False;Tex Masks;-1;True;1;0;SAMPLER2D;;False;1;SAMPLER2D;0
Node;AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;77;-6912,2432;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;126;-9088,128;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;128;-9088,512;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;97;-7552,512;Inherit;False;34;VTC Ero Smooth;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;91;-7552,256;Inherit;False;29;VTC Ero;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;96;-7552,384;Inherit;False;Property;_LUTErosionSmoothness;LUT Erosion Smoothness;35;0;Create;True;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;107;-7552,688;Inherit;False;106;Tex Masks;1;0;OBJECT;;False;1;SAMPLER2D;0
Node;AmplifyShaderEditor.DynamicAppendNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;127;-8960,128;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;93;-7296,256;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;98;-7040,384;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;95;-7552,128;Inherit;False;Property;_LUTErosion;LUT Erosion;34;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;102;-7552,768;Inherit;True;Property;_TextureSample0;Texture Sample 0;25;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Instance;-1;Auto;Texture2D;False;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;92;-7296,128;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;94;-7040,128;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SmoothstepOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;99;-6912,-256;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;58;-5760,-128;Inherit;False;Property;_LUTAmplitude;LUT Amplitude;33;0;Create;True;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;61;-6016,-256;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;57;-5760,-256;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;59;-5504,-128;Inherit;False;Property;_LUTOffset;LUT Offset;31;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;68;-7552,1280;Inherit;False;29;VTC Ero;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;71;-7552,1408;Inherit;False;Property;_ErosionSmoothness;Erosion Smoothness;9;0;Create;True;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;72;-7552,1536;Inherit;False;34;VTC Ero Smooth;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;56;-5504,-256;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;60;-5248,-128;Inherit;False;Property;_LUTSpeed;LUT Speed;32;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;69;-7296,1280;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;66;-7552,1152;Inherit;False;Property;_Erosion;Erosion;8;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;70;-7040,1408;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.PannerNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;55;-5248,-256;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;67;-7296,1152;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;65;-7040,1152;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;54;-4864,-256;Inherit;True;Property;_LUT;LUT;30;0;Create;True;0;0;0;False;3;Space(33);Header(LUT);Space(13);False;-1;e561bead12b97374ab56577fcb4b14c7;e561bead12b97374ab56577fcb4b14c7;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;False;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;117;-2354,846;Inherit;False;484;674.95;Cam Distance fade;6;18;17;16;20;21;22;Cam Distance fade;0,0,0,1;0;0
Node;AmplifyShaderEditor.SmoothstepOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;62;-6912,768;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RGBToHSVNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;48;-4224,-256;Inherit;False;1;0;FLOAT3;0,0,0;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;50;-3968,-128;Inherit;False;Property;_HueShift;Hue Shift;36;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;18;-2304,1408;Inherit;False;Property;_CameraDepthFadeOffset;Camera Depth Fade Offset;29;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;17;-2304,1280;Inherit;False;Property;_CameraDepthFadeLength;Camera Depth Fade Length;28;0;Create;True;0;0;0;False;3;Space(33);Header(Camera Depth Fade);Space(13);False;0.333;0.3;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;64;-6656,768;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;49;-3968,-256;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;238;-7602,3662;Inherit;False;1188;722.8501;Push Particle toward camera direction (no more glow clipping in the ground) | 0=Disabled;9;228;229;230;231;232;233;234;235;236;Camera Offset;0,0,0,1;0;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;89;-6656,2048;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CameraDepthFade, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;16;-2304,1152;Inherit;False;3;2;FLOAT3;0,0,0;False;0;FLOAT;1;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;32;-592,-1616;Inherit;False;VTC Em;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.HSVToRGBNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;47;-3712,-256;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;52;-3456,-128;Inherit;False;Property;_Desaturate;Desaturate;37;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.VertexColorNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;88;-7552,1792;Inherit;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;234;-7552,4096;Inherit;False;Property;_CameraOffset;Camera Offset;43;0;Create;True;0;0;0;False;3;Space(33);Header(Camera Offset);Space(13);False;0;-15;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;90;-6400,2048;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;20;-2048,1152;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.WorldPosInputsNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;228;-7552,3712;Inherit;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.WorldSpaceCameraPos, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;231;-7552,3856;Inherit;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;45;-2176,256;Inherit;False;32;VTC Em;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.DesaturateOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;51;-3456,-256;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;43;-2176,128;Inherit;False;Property;_Emissive;Emissive;2;0;Create;True;0;0;0;False;3;Space(33);Header(General);Space(13);False;1;5;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;21;-2304,896;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;230;-7296,3712;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;232;-6912,3840;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0.01;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;41;-2048,128;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;46;-2432,0;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;22;-2048,896;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;229;-6912,3712;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;40;-2048,0;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;10;848,-48;Inherit;False;1252;162.95;Lush was here! <3;5;15;14;13;12;11;Lush was here! <3;0,0,0,1;0;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;23;-1792,896;Inherit;False;Op;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;236;-6656,3712;Inherit;False;Cam Offset;-1;True;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;25;-1792,0;Inherit;False;Col;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;233;-6912,3968;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;11;1408,0;Inherit;False;Property;_Dst;Dst;46;0;Create;True;0;0;0;True;0;False;10;10;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;12;1152,0;Inherit;False;Property;_Src;Src;45;0;Create;True;0;0;0;True;0;False;5;5;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;13;1664,0;Inherit;False;Property;_ZWrite;ZWrite;47;0;Create;True;0;0;0;True;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;14;1920,0;Inherit;False;Property;_ZTest;ZTest;48;0;Create;True;0;0;0;True;0;False;2;2;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;24;-384,128;Inherit;False;23;Op;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;26;-384,0;Inherit;False;25;Col;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;15;896,0;Inherit;False;Property;_Cull;Cull;44;0;Create;True;0;0;0;True;3;Space(33);Header(AR);Space(13);False;2;2;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;239;-384,256;Inherit;False;236;Cam Offset;1;0;OBJECT;;False;1;FLOAT3;0
Node;AmplifyShaderEditor.TextureCoordinatesNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;235;-7552,4224;Inherit;False;3;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;220;-11392,768;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.Vector2Node, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;149;-13312,1024;Inherit;False;Constant;_Vector0;Vector 0;34;0;Create;True;0;0;0;False;0;False;0.5,0.5;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.StandardSurfaceOutputNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;242;0,0;Float;False;True;-1;3;AmplifyShaderEditor.MaterialInspector;0;0;Unlit;Vefects/SH_Vefects_VFX_Stylized_Fire_01_BIRP;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;Back;0;True;_ZWrite;0;True;_ZTest;False;0;False;;0;False;;False;0;0;False;;0;Custom;0.5;True;False;0;False;Transparent;;Transparent;All;12;all;True;True;True;True;0;False;;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;2;15;10;25;False;0.5;False;1;5;True;_Src;10;True;_Dst;0;0;False;;0;False;;0;False;;0;False;;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;True;Relative;0;;0;-1;-1;-1;0;False;0;0;True;_Cull;-1;0;False;;0;0;0;False;0.1;False;;0;False;;False;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;16;FLOAT4;0,0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;224;0;154;0
WireConnection;224;1;223;0
WireConnection;225;0;222;0
WireConnection;225;1;221;0
WireConnection;114;0;113;0
WireConnection;182;0;181;0
WireConnection;226;0;224;0
WireConnection;226;2;225;0
WireConnection;180;0;226;0
WireConnection;180;1;182;0
WireConnection;177;0;176;0
WireConnection;177;1;180;0
WireConnection;165;0;164;0
WireConnection;178;0;177;5
WireConnection;152;0;151;0
WireConnection;152;1;153;0
WireConnection;157;0;155;0
WireConnection;157;1;156;0
WireConnection;33;0;28;2
WireConnection;39;0;38;0
WireConnection;174;0;175;0
WireConnection;179;3;178;0
WireConnection;154;0;152;0
WireConnection;154;1;157;0
WireConnection;173;0;179;0
WireConnection;173;1;174;0
WireConnection;158;0;154;0
WireConnection;158;1;159;0
WireConnection;162;0;161;0
WireConnection;162;1;163;0
WireConnection;172;0;173;0
WireConnection;203;0;154;0
WireConnection;203;1;200;0
WireConnection;204;0;201;0
WireConnection;204;1;202;0
WireConnection;160;0;158;0
WireConnection;160;2;162;0
WireConnection;167;0;168;0
WireConnection;167;1;169;0
WireConnection;240;0;28;4
WireConnection;240;1;241;0
WireConnection;35;0;240;0
WireConnection;208;0;205;0
WireConnection;207;0;203;0
WireConnection;207;2;204;0
WireConnection;166;0;160;0
WireConnection;166;1;167;0
WireConnection;112;0;111;0
WireConnection;209;0;207;0
WireConnection;209;1;208;0
WireConnection;170;0;166;0
WireConnection;170;1;171;0
WireConnection;110;0;109;0
WireConnection;212;0;209;0
WireConnection;212;1;210;0
WireConnection;148;0;170;0
WireConnection;148;2;150;0
WireConnection;215;0;214;0
WireConnection;215;1;212;0
WireConnection;142;0;143;0
WireConnection;142;1;148;0
WireConnection;218;0;215;0
WireConnection;218;1;217;0
WireConnection;122;0;120;0
WireConnection;122;1;121;0
WireConnection;103;0;108;0
WireConnection;144;0;142;0
WireConnection;144;1;146;0
WireConnection;219;0;218;0
WireConnection;118;0;103;1
WireConnection;118;1;120;0
WireConnection;118;2;122;0
WireConnection;147;0;144;0
WireConnection;31;0;27;4
WireConnection;119;0;118;0
WireConnection;140;0;147;0
WireConnection;140;1;219;0
WireConnection;79;1;81;0
WireConnection;79;2;80;0
WireConnection;79;3;82;0
WireConnection;123;0;140;0
WireConnection;123;1;119;0
WireConnection;83;0;79;0
WireConnection;73;0;78;0
WireConnection;134;0;133;0
WireConnection;134;1;135;0
WireConnection;124;0;123;0
WireConnection;85;0;83;0
WireConnection;85;1;87;0
WireConnection;74;0;73;0
WireConnection;131;0;124;0
WireConnection;131;1;134;0
WireConnection;137;0;136;2
WireConnection;86;0;85;0
WireConnection;75;0;74;0
WireConnection;132;0;137;0
WireConnection;132;1;131;0
WireConnection;29;0;27;3
WireConnection;34;0;28;3
WireConnection;76;0;75;0
WireConnection;76;1;86;0
WireConnection;129;0;132;0
WireConnection;129;1;130;0
WireConnection;138;0;132;0
WireConnection;138;1;139;0
WireConnection;106;0;104;0
WireConnection;77;0;76;0
WireConnection;126;0;125;1
WireConnection;126;1;129;0
WireConnection;128;0;125;2
WireConnection;128;1;138;0
WireConnection;127;0;126;0
WireConnection;127;1;128;0
WireConnection;93;0;91;0
WireConnection;93;1;77;0
WireConnection;98;0;96;0
WireConnection;98;1;97;0
WireConnection;102;0;107;0
WireConnection;102;1;127;0
WireConnection;92;0;95;0
WireConnection;92;1;93;0
WireConnection;94;0;92;0
WireConnection;94;1;98;0
WireConnection;99;0;102;2
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
WireConnection;62;0;102;2
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
WireConnection;21;0;90;0
WireConnection;21;1;20;0
WireConnection;230;0;228;0
WireConnection;230;1;231;0
WireConnection;232;0;234;0
WireConnection;41;0;43;0
WireConnection;41;1;45;0
WireConnection;46;0;51;0
WireConnection;46;1;88;0
WireConnection;22;0;21;0
WireConnection;229;0;230;0
WireConnection;229;1;232;0
WireConnection;40;0;46;0
WireConnection;40;1;41;0
WireConnection;23;0;22;0
WireConnection;236;0;229;0
WireConnection;25;0;40;0
WireConnection;233;0;234;0
WireConnection;233;1;235;2
WireConnection;220;0;140;0
WireConnection;242;2;26;0
WireConnection;242;9;24;0
WireConnection;242;11;239;0
ASEEND*/
//CHKSM=0E20166624ACEA7EA970C057F2918823776C53C4