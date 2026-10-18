// ================================================================
// VEFECTS | Vefects_Radial_Zoom_Blur_URP
// Copyright (c) 2026 Vefects.
// Particle-centered radial zoom blur | 2-64 samples | URP
// ================================================================

#ifndef VEFECTS_RADIAL_ZOOM_BLUR_URP_INCLUDED
#define VEFECTS_RADIAL_ZOOM_BLUR_URP_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

// ASE Custom Expression: File mode, Float4 output, Float precision,
// Precision Suffix disabled, Name = Vefects_Radial_Zoom_Blur_URP.
// Add the four In ports in this exact argument order.
// ScreenUV uses ASE Screen Position / Normalized / XY.
// CenterWS is the particle Center stream, already in world space.
// Positive strength samples toward the center, producing outward zoom trails.
// RGB is straight color. A is an effect-validity gate, not scene opacity.
float4 Vefects_Radial_Zoom_Blur_URP(
    float2 ScreenUV,
    float3 CenterWS,
    float Strength,
    float Samples)
{
    float strength = clamp(Strength, -0.5, 0.5);
    float4 centerCS = TransformWorldToHClip(CenterWS);
    float eyeDepth = -TransformWorldToView(CenterWS).z;

    // Suppress invalid centers for perspective and orthographic cameras.
    // A zero-strength particle must not repaint the opaque snapshot.
    if (abs(strength) <= 0.000001 ||
        centerCS.w <= 0.00001 ||
        eyeDepth <= max(_ProjectionParams.y, 0.00001))
    {
        return float4(0.0, 0.0, 0.0, 0.0);
    }

    // Match URP's positionNDC / ASE normalized Screen Position convention.
    // Do not clamp the center: it can legitimately be outside the viewport.
    float2 centerUV = centerCS.xy / centerCS.w;
    centerUV *= float2(0.5, 0.5 * _ProjectionParams.x);
    centerUV += 0.5;

    int sampleCount = (int)clamp(floor(Samples + 0.5), 2.0, 64.0);
    float2 totalOffset = (centerUV - ScreenUV) * strength;
    float inverseIntervals = rcp((float)(sampleCount - 1));
    float3 accumulated = float3(0.0, 0.0, 0.0);

    [loop]
    for (int sampleIndex = 0; sampleIndex < sampleCount; ++sampleIndex)
    {
        float fraction = (float)sampleIndex * inverseIntervals;
        float2 sampleUV = ScreenUV + totalOffset * fraction;
        // Clamp samples, never the radial origin. URP handles texture access.
        float2 sourceUV = ClampAndScaleUVForBilinear(UnityStereoTransformScreenSpaceTex(saturate(sampleUV)), _CameraOpaqueTexture_TexelSize.xy);
        accumulated += SAMPLE_TEXTURE2D_X_LOD(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, sourceUV, 0.0).rgb;
    }

    return float4(accumulated / (float)sampleCount, 1.0);
}

#endif
