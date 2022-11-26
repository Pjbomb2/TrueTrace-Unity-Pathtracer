Shader "Hidden/SVGFReproject"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            Name "Reproject"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "SVGFCommon.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;
    uniform Texture2D   gPositionNormalFwidth;
    uniform Texture2D   gColor;
    uniform Texture2D   gAlbedo;
    uniform Texture2D   gPrevIllum;
    uniform Texture2D   gPrevMoments;
    uniform Texture2D   gLinearZAndNormal;
    uniform Texture2D   gPrevLinearZAndNormal;
    uniform Texture2D   gPrevHistoryLength;
    sampler2D _CameraMotionVectorsTexture;

    float       gAlpha;
    float       gMomentsAlpha;

float3 demodulate(float3 c, float3 albedo)
{
    return c / max(albedo, float3(0.001, 0.001, 0.001));
}

bool isReprjValid(int2 coord, float Z, float Zprev, float fwidthZ, float3 normal, float3 normalPrev, float fwidthNormal)
{
    int2 screenSize = 0;
    int thr = 0;
    gColor.GetDimensions(0, screenSize.x, screenSize.y, thr);
    const int2 imageDim = screenSize;

    // check whether reprojected pixel is inside of the screen
    if (any(coord < int2(1,1)) || any(coord > imageDim - int2(1,1))) return false;

    // check if deviation of depths is acceptable
    if (abs(Zprev - Z) / (fwidthZ + 1e-2f) / min(Z, Zprev) > 10.f) return false;

    // check normals for compatibility
    if (distance(normal, normalPrev) / (fwidthNormal + 1e-2) > 16.0) return false;

    return true;
}

bool loadPrevData(float2 posH, out float4 prevIllum, out float2 prevMoments, out float historyLength)
{
    const int2 ipos = posH;
    int2 screenSize = 0;
    int thr = 0;
    gColor.GetDimensions(0, screenSize.x, screenSize.y, thr);
    const int2 imageDim = screenSize;

    const float2 motion = tex2D(_CameraMotionVectorsTexture, ipos / float2(imageDim)).xy;
    const float normalFwidth = 0.2f;//gPositionNormalFwidth[ipos].y;

    // +0.5 to account for texel center offset
    const int2 iposPrev = int2(float2(ipos) - motion.xy * imageDim + float2(0.5,0.5));

    float2 depth = gLinearZAndNormal[ipos].xy;
    float3 normal = oct_to_ndir_snorm(gLinearZAndNormal[ipos].zw);

    prevIllum   = float4(0,0,0,0);
    prevMoments = float2(0,0);

    bool v[4];
    const float2 posPrev = floor(posH.xy) - motion.xy * imageDim;
    const int2 offset[4] = { int2(0, 0), int2(1, 0), int2(0, 1), int2(1, 1) };

    // check for all 4 taps of the bilinear filter for validity
    bool valid = false;
    for (int sampleIdx = 0; sampleIdx < 4; sampleIdx++)
    {
        int2 loc = int2(posPrev) + offset[sampleIdx];
        float2 depthPrev = gPrevLinearZAndNormal[loc].xy;
        float3 normalPrev = oct_to_ndir_snorm(gPrevLinearZAndNormal[loc].zw);

        v[sampleIdx] = isReprjValid(iposPrev, depth.x, depthPrev.x, depth.y, normal, normalPrev, normalFwidth);

        valid = valid || v[sampleIdx];
    }

    if (valid)
    {
        float sumw = 0;
        float x = frac(posPrev.x);
        float y = frac(posPrev.y);

        // bilinear weights
        const float w[4] = { (1 - x) * (1 - y),
                                  x  * (1 - y),
                             (1 - x) *      y,
                                  x  *      y };

        // perform the actual bilinear interpolation
        for (int sampleIdx = 0; sampleIdx < 4; sampleIdx++)
        {
            const int2 loc = int2(posPrev) + offset[sampleIdx];
            if (v[sampleIdx])
            {
                prevIllum   += w[sampleIdx] * gPrevIllum[loc];
                prevMoments += w[sampleIdx] * gPrevMoments[loc].xy;
                sumw        += w[sampleIdx];
             }
        }

        // redistribute weights in case not all taps were used
        valid = (sumw >= 0.01);
        prevIllum   = valid ? prevIllum / sumw   : float4(0, 0, 0, 0);
        prevMoments = valid ? prevMoments / sumw : float2(0, 0);
    }

    if (!valid) // perform cross-bilateral filter in the hope to find some suitable samples somewhere
    {
        float nValid = 0.0;

        // this code performs a binary descision for each tap of the cross-bilateral filter
        const int radius = 1;
        for (int yy = -radius; yy <= radius; yy++)
        {
            for (int xx = -radius; xx <= radius; xx++)
            {
                const int2 p = iposPrev + int2(xx, yy);
                const float2 depthFilter = gPrevLinearZAndNormal[p].xy;
                const float3 normalFilter = oct_to_ndir_snorm(gPrevLinearZAndNormal[p].zw);

                if (isReprjValid(iposPrev, depth.x, depthFilter.x, depth.y, normal, normalFilter, normalFwidth))
                {
                    prevIllum += gPrevIllum[p];
                    prevMoments += gPrevMoments[p].xy;
                    nValid += 1.0;
                }
            }
        }
        if (nValid > 0)
        {
            valid = true;
            prevIllum   /= nValid;
            prevMoments /= nValid;
        }
    }

    if (valid)
    {
        // crude, fixme
        historyLength = gPrevHistoryLength[iposPrev].x;
    }
    else
    {
        prevIllum   = float4(0,0,0,0);
        prevMoments = float2(0,0);
        historyLength = 0;
    }

    return valid;
}
struct PS_OUT
{
    float4 OutIllumination  : SV_TARGET0;
    float2 OutMoments       : SV_TARGET1;
    float  OutHistoryLength : SV_TARGET2;
};
// not used currently
float computeVarianceScale(float numSamples, float loopLength, float alpha)
{
    const float aa = (1.0 - alpha) * (1.0 - alpha);
    return (1.0 - pow(aa, min(loopLength, numSamples))) / (1.0 - aa);
}
            PS_OUT frag (v2f i) : SV_Target
            {
                int2 screenSize = 0;
                int thr = 0;
                gColor.GetDimensions(0, screenSize.x, screenSize.y, thr);
               const int2 ipos       = i.uv * screenSize;

    float3 illumination = demodulate(gColor[ipos].rgb, gAlbedo[ipos].rgb);
    // Workaround path tracer bugs. TODO: remove this when we can.
    if (!all(illumination > 0 || illumination < 0 || illumination == 0 ))
    {
        illumination = float3(0, 0, 0);
    }

    float historyLength;
    float4 prevIllumination;
    float2 prevMoments;
    bool success = loadPrevData(ipos, prevIllumination, prevMoments, historyLength);
    historyLength = min(32.0f, success ? historyLength + 1.0f : 1.0f);

    // this adjusts the alpha for the case where insufficient history is available.
    // It boosts the temporal accumulation to give the samples equal weights in
    // the beginning.
    const float alpha        = success ? max(gAlpha,        1.0 / historyLength) : 1.0;
    const float alphaMoments = success ? max(gMomentsAlpha, 1.0 / historyLength) : 1.0;

    // compute first two moments of luminance
    float2 moments;
    moments.r = luminance(illumination);
    moments.g = moments.r * moments.r;

    float2 pm = moments;

    // temporal integration of the moments
    moments = lerp(prevMoments, moments, alphaMoments);

    float variance = max(0.f, moments.g - moments.r * moments.r);

    //variance *= computeVarianceScale(16, 16, alpha);

    PS_OUT psOut;
    // temporal integration of illumination
    psOut.OutIllumination = lerp(prevIllumination,   float4(illumination,   0), alpha);
    // variance is propagated through the alpha channel
    psOut.OutIllumination.a = variance;
    psOut.OutMoments = moments;
    psOut.OutHistoryLength = historyLength;

    return psOut;
            }
            ENDCG
        }
    }
}
