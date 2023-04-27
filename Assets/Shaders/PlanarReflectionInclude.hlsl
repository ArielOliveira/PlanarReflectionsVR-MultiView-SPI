#ifndef PLANAR_REFLECTION_INCLUDE
#define PLANAR_REFLECTION_INCLUDE

TEXTURE2D(_LeftReflCameraTex); SAMPLER(sampler_LeftReflCameraTex);
TEXTURE2D(_RightReflCameraTex); SAMPLER(sampler_RightReflCameraTex);

half4 GetReflectionTexture(float4 screenPos) {
    float2 uv = screenPos.xy / screenPos.w;

    if (unity_StereoEyeIndex == 0)
        return SAMPLE_TEXTURE2D(_LeftReflCameraTex, sampler_LeftReflCameraTex, uv);
    else 
        return SAMPLE_TEXTURE2D(_RightReflCameraTex, sampler_RightReflCameraTex, uv);
}

uniform float3 _StereoCamPosWS;

#endif 