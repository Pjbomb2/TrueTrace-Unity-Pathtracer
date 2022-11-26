float luminance(float3 c)
{
    return 0.212671 * c.x + 0.715160 * c.y + 0.072169 * c.z;
}

float computeWeight(
    float depthCenter, float depthP, float phiDepth,
    float3 normalCenter, float3 normalP, float phiNormal,
    float luminanceIllumCenter, float luminanceIllumP, float phiIllum)
{
    const float weightNormal  = pow(saturate(dot(normalCenter, normalP)), phiNormal);
    const float weightZ       = (phiDepth == 0) ? 0.0f : abs(depthCenter - depthP) / phiDepth;
    const float weightLillum  = abs(luminanceIllumCenter - luminanceIllumP) / phiIllum;

    const float weightIllum   = exp(0.0 - max(weightLillum, 0.0) - max(weightZ, 0.0)) * weightNormal;

    return weightIllum;
}

   inline float2 oct_wrap(float2 v)
    {
        return (1.f - abs(v.y)) * (v.x >= 0.f ? 1.f : -1.f), (1.f - abs(v.x)) * (v.y >= 0.f ? 1.f : -1.f);
    }

    /** Converts normalized direction to the octahedral map (non-equal area, signed normalized).
        \param[in] n Normalized direction.
        \return Position in octahedral map in [-1,1] for each component.
    */
    inline float2 ndir_to_oct_snorm(float3 n)
    {
        // Project the sphere onto the octahedron (|x|+|y|+|z| = 1) and then onto the xy-plane.
        float2 p = float2(n.x, n.y) * (1.f / (abs(n.x) + abs(n.y) + abs(n.z)));
        p = (n.z < 0.f) ? oct_wrap(p) : p;
        return p;
    }

    /** Converts point in the octahedral map to normalized direction (non-equal area, signed normalized).
        \param[in] p Position in octahedral map in [-1,1] for each component.
        \return Normalized direction.
    */
    inline float3 oct_to_ndir_snorm(float2 p)
    {
        float3 n = float3(p.x, p.y, 1.f - abs(p.x) - abs(p.y));
        float2 tmp = (n.z < 0.0) ? oct_wrap(float2(n.x, n.y)) : float2(n.x, n.y);
        n.x = tmp.x;
        n.y = tmp.y;
        return normalize(n);
    }