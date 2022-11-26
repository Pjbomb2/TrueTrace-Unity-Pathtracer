Shader "Hidden/SVGFFilterMoments"
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
            Name "FilterMoments"
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
            uniform Texture2D   gIllumination;
            uniform Texture2D   gMoments;
            uniform Texture2D   gHistoryLength;
            uniform Texture2D   gLinearZAndNormal;

            float       gPhiColor;
            float       gPhiNormal;

            fixed4 frag (v2f i) : SV_Target
            {
                int2 screenSize = 0;
                int thr = 0;
                gIllumination.GetDimensions(0, screenSize.x, screenSize.y, thr);
               const int2 ipos       = i.uv * screenSize;

    float h = gHistoryLength[ipos].x;

    if (h < 4.0) // not enough temporal history available
    {
        float sumWIllumination   = 0.0;
        float3 sumIllumination   = float3(0.0, 0.0, 0.0);
        float2 sumMoments  = float2(0.0, 0.0);

        const float4 illuminationCenter = gIllumination[ipos];
        const float lIlluminationCenter = luminance(illuminationCenter.rgb);

        const float2 zCenter = gLinearZAndNormal[ipos].xy;
        if (zCenter.x < 0)
        {
            // current pixel does not a valid depth => must be envmap => do nothing
            return illuminationCenter;
        }
        const float3 nCenter = oct_to_ndir_snorm(gLinearZAndNormal[ipos].zw);
        const float phiLIllumination   = gPhiColor;
        const float phiDepth     = max(zCenter.y, 1e-8) * 3.0;

        // compute first and second moment spatially. This code also applies cross-bilateral
        // filtering on the input illumination.
        const int radius = 3;

        for (int yy = -radius; yy <= radius; yy++)
        {
            for (int xx = -radius; xx <= radius; xx++)
            {
                const int2 p     = ipos + int2(xx, yy);
                const bool inside = all(p >= int2(0,0)) && all(p < screenSize);
                const bool samePixel = (xx ==0 && yy == 0);
                const float kernel = 1.0;

                if (inside)
                {
                    const float3 illuminationP = gIllumination[p].rgb;
                    const float2 momentsP      = gMoments[p].xy;
                    const float lIlluminationP = luminance(illuminationP.rgb);
                    const float zP = gLinearZAndNormal[p].x;
                    const float3 nP = oct_to_ndir_snorm(gLinearZAndNormal[p].zw);

                    const float w = computeWeight(
                        zCenter.x, zP, phiDepth * length(float2(xx, yy)),
                        nCenter, nP, gPhiNormal,
                        lIlluminationCenter, lIlluminationP, phiLIllumination);

                    sumWIllumination += w;
                    sumIllumination  += illuminationP * w;
                    sumMoments += momentsP * w;
                }
            }
        }

        // Clamp sum to >0 to avoid NaNs.
        sumWIllumination = max(sumWIllumination, 1e-6f);

        sumIllumination   /= sumWIllumination;
        sumMoments  /= sumWIllumination;

        // compute variance using the first and second moments
        float variance = sumMoments.g - sumMoments.r * sumMoments.r;

        // give the variance a boost for the first frames
        variance *= 4.0 / h;

        return float4(sumIllumination, variance.r);
    }
    else
    {
        // do nothing, pass data unmodified
        return gIllumination[ipos];
    }
            }
            ENDCG
        }
    }
}
