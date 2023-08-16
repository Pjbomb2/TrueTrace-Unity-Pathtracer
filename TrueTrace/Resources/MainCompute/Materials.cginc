struct BsdfSample {
    float forwardPdfW;
    float reversePdfW;
    float3 reflectance;
    float3 wi;
    int InsideMedium;
    float phaseFunction;
    float3 extinction;
};

BsdfSample initbsdf() {
    BsdfSample result;
    result.forwardPdfW = 0;
    result.reversePdfW = 0;
    result.reflectance = 0;
    result.wi = 0;
    result.InsideMedium = 0;
    result.phaseFunction = 0;
    result.extinction = 0;
    return result;
}

float CosTheta(const float3 w)
{
    return w.y;
}

float AbsCosTheta(const float3 w)
{
    return abs(CosTheta(w));
}
float Cos2Theta(const float3 w)
{
    return w.y * w.y;
}

float Sin2Theta(const float3 w)
{
    return max(0.0f, 1.0f - Cos2Theta(w));
}
float SinTheta(const float3 w)
{
    return sqrt(Sin2Theta(w));
}

float TanTheta(const float3 w)
{
    return SinTheta(w) / CosTheta(w);
}

float Tan2Theta(const float3 w)
{
    return Sin2Theta(w) / Cos2Theta(w);
}


float CosPhi(const float3 w)
{
    float sinTheta = SinTheta(w);
    return (sinTheta == 0) ? 1.0f : clamp(w.x / sinTheta, -1.0f, 1.0f);
}
float SinPhi(const float3 w)
{
    float sinTheta = SinTheta(w);
    return (sinTheta == 0) ? 1.0f : clamp(w.z / sinTheta, -1.0f, 1.0f);
}
float Cos2Phi(const float3 w)
{
    float cosPhi = CosPhi(w);
    return cosPhi * cosPhi;
}
float Sin2Phi(const float3 w)
{
    float sinPhi = SinPhi(w);
    return sinPhi * sinPhi;
}

float3 Schlick(float3 r0, float rad)
{
    float exponential = pow(1.0f - rad, 5.0f);
    return r0 + (1.0f - r0) * exponential;
}

float SchlickWeight(float u)
{
    float m = saturate(1.0f - u);
    float m2 = m * m;
    return m * m2 * m2;
}
float Schlick(float r0, float rads)
{
    return lerp(1.0f, SchlickWeight(rads), r0);
}


static float3 CalculateTint(float3 baseColor)
{
    // -- The color tint is never mentioned in the SIGGRAPH presentations as far as I recall but it was done in
    // --  the BRDF Explorer so I'll replicate that here.
    float luminance = dot(float3(0.3f, 0.6f, 1.0f), baseColor);
    return (luminance > 0.0f) ? (baseColor * (1.0f / luminance)) : 1;
}

//===================================================================================================================
float SeparableSmithGGXG1(const float3 w, float a)
{
    float a2 = a * a;
    float absDotNV = AbsCosTheta(w);

    return 2.0f / (1.0f + sqrt(a2 + (1 - a2) * absDotNV * absDotNV));
}

//===================================================================================================================

float GTR1(float NDotH, float a)
{
    if (a >= 1.0)
        return rcp(PI);
    float a2 = a * a;
    float t = 1.0 + (a2 - 1.0) * NDotH * NDotH;
    return (a2 - 1.0) / (PI * log(a2) * t);
}


float GgxAnisotropicD(const float3 wm, float ax, float ay)
{
    float dotHX2 = (wm.x * wm.x);
    float dotHY2 = (wm.z * wm.z);
    float cos2Theta = Cos2Theta(wm);
    float ax2 = (ax * ax);
    float ay2 = (ay * ay);

    return 1.0f / (PI * ax * ay * pow(dotHX2 / ax2 + dotHY2 / ay2 + cos2Theta, 2));
}

//===================================================================================================================
float SeparableSmithGGXG1(const float3 w, const float3 wm, float ax, float ay)
{


    float absTanTheta = abs(TanTheta(w));
    if (!(absTanTheta < 0 || absTanTheta > 0 || absTanTheta == 0)) {
        return 0.0f;
    }

    float a = sqrt(Cos2Phi(w) * ax * ax + Sin2Phi(w) * ay * ay);
    float a2Tan2Theta = pow(a * absTanTheta, 2);

    float lambda = 0.5f * (-1.0f + sqrt(1.0f + a2Tan2Theta));
    return 1.0f / (1.0f + lambda);
}

static void CalculateAnisotropicParams(float roughness, float anisotropic, inout float ax, inout float ay)
{
    float aspect = sqrt(1.0f - 0.9f * anisotropic);
    ax = max(0.0001f, (roughness * roughness) / aspect);
    ay = max(0.0001f, (roughness * roughness) * aspect);
}

void GgxVndfAnisotropicPdf(const float3 wi, const float3 wm, const float3 wo, float ax, float ay,
    inout float forwardPdfW, inout float reversePdfW)
{
    float D = GgxAnisotropicD(wm, ax, ay);

    float absDotNL = AbsCosTheta(wi);
    float absDotHL = abs(dot(wm, wi));
    float G1v = SeparableSmithGGXG1(wo, wm, ax, ay);
    forwardPdfW = G1v * absDotHL * D / absDotNL;
}

float SchlickR0FromRelativeIOR(float eta)
{
    // https://seblagarde.wordpress.com/2013/04/29/memo-on-fresnel-equations/
    return pow(eta - 1.0f, 2) / pow(eta + 1.0f, 2);
}

float Dielectric(float cosThetaI, float ni, float nt)
{
    // Copied from PBRT. This function calculates the full Fresnel term for a dielectric material.
    // See Sebastion Legarde's link above for details.

    cosThetaI = clamp(cosThetaI, -1.0f, 1.0f);

    // Swap index of refraction if this is coming from inside the surface
    if (cosThetaI < 0.0f) {
        float temp = ni;
        ni = nt;
        nt = temp;

        cosThetaI = -cosThetaI;
    }

    float sinThetaI = sqrt(max(0.0f, 1.0f - cosThetaI * cosThetaI));
    float sinThetaT = ni / nt * sinThetaI;

    // Check for total internal reflection
    if (sinThetaT >= 1) {
        return 1;
    }

    float cosThetaT = sqrt(max(0.0f, 1.0f - sinThetaT * sinThetaT));

    float rParallel = ((nt * cosThetaI) - (ni * cosThetaT)) / ((nt * cosThetaI) + (ni * cosThetaT));
    float rPerpendicuar = ((ni * cosThetaI) - (nt * cosThetaT)) / ((ni * cosThetaI) + (nt * cosThetaT));
    return (rParallel * rParallel + rPerpendicuar * rPerpendicuar) / 2;
}

static float3 DisneyFresnel(MaterialData hitDat, const float3 wo, const float3 wm, const float3 wi)
{
    float dotHV = dot(wm, wo);

    float3 tint = CalculateTint(hitDat.surfaceColor);

    // -- See section 3.1 and 3.2 of the 2015 PBR presentation + the Disney BRDF explorer (which does their 2012 remapping
    // -- rather than the SchlickR0FromRelativeIOR seen here but they mentioned the switch in 3.2).
    float3 R0 = SchlickR0FromRelativeIOR(hitDat.relativeIOR) * lerp(1.0f, tint, hitDat.specularTint);
    R0 = lerp(R0, hitDat.surfaceColor, hitDat.metallic);

    float dielectricFresnel = Dielectric(dotHV, 1.0f, hitDat.ior);
    float3 metallicFresnel = Schlick(R0, dot(wi, wm));

    return lerp(dielectricFresnel, metallicFresnel, hitDat.metallic);
}

//===================================================================================================================



static float ThinTransmissionRoughness(float ior, float roughness)
{
    // -- Disney scales by (.65 * eta - .35) based on figure 15 of the 2015 PBR course notes. Based on their figure
    // -- the results match a geometrically thin solid fairly well.
    return saturate((0.65f * ior - 0.35f) * roughness);
}

//===================================================================================================================

float3 SampleVndf_Hemisphere(float2 u, float3 wi)
{
    // sample a spherical cap in (-wi.z, 1]
    float phi = 2.0f * PI * u.x;
    float z = mad((1.0f - u.y), (1.0f + wi.y), -wi.y);
    float sinTheta = sqrt(clamp(1.0f - z * z, 0.0f, 1.0f));
    float x = sinTheta * cos(phi);
    float y = sinTheta * sin(phi);
    float3 c = float3(x, z, y);
    // compute halfway direction;
    float3 h = c + wi;
    // return without normalization as this is done later (see line 25)
    return h;
}

// Sample the GGX VNDF
float3 SampleGgxVndfAnisotropic(float3 wi, float ax, float ay, float u1, float u2)
{
    // warp to the hemisphere configuration
    float3 wiStd = normalize(float3(wi.x * ax,wi.y, wi.z * ay));
    // sample the hemisphere
    float3 wmStd = SampleVndf_Hemisphere(float2(u1,u2), wiStd);
    // warp back to the ellipsoid configuration
    float3 wm = normalize(float3(wmStd.x * ax,wmStd.y, wmStd.z * ay));
    // return final normal
    return wm;
}


// float3 SampleGgxVndfAnisotropic(const float3 wo, float ax, float ay, float u1, float u2)
// {
//     // -- Stretch the view vector so we are sampling as though roughness==1
//     float3 v = normalize(float3(wo.x * ax, wo.y, wo.z * ay));

//     // -- Build an orthonormal basis with v, t1, and t2
//     float3 t1 = (v.y < 0.9999f) ? normalize(cross(v, float3(0, 1, 0))) : float3(1, 0, 0);
//     float3 t2 = cross(t1, v);

//     // -- Choose a point on a disk with each half of the disk weighted proportionally to its projection onto direction v
//     float a = 1.0f / (1.0f + v.y);
//     float r = sqrt(u1);
//     float phi = (u2 < a) ? (u2 / a) * PI : PI + (u2 - a) / (1.0f - a) * PI;
//     float p1 = r * cos(phi);
//     float p2 = r * sin(phi) * ((u2 < a) ? 1.0f : v.y);

//     // -- Calculate the normal in this stretched tangent space
//     float3 n = p1 * t1 + p2 * t2 + sqrt(max(0.0f, 1.0f - p1 * p1 - p2 * p2)) * v;

//     // -- unstretch and normalize the normal
//     return normalize(float3(ax * n.x, n.y, ay * n.z));
// }


bool Transmit(float3 wm, float3 wi, float n, inout float3 wo)
{
    float c = dot(wi, wm);
    if (c < 0.0f) {
        c = -c;
        wm = -wm;
    }

    float root = 1.0f - n * n * (1.0f - c * c);
    if (root <= 0)
        return false;

    wo = (n * c - sqrt(root)) * wm - n * wi;
    return true;
}


static float3 CalculateExtinction(float3 apparantColor, float scatterDistance)
{
    float3 a = apparantColor;
    float3 s = 1.9f - a + 3.5f * (a - 0.8f) * (a - 0.8f);

    return 1.0f / (s * scatterDistance);
}

float3 CosineSampleHemisphere2(float r1, float r2)
{
    float3 dir;
    float r = sqrt(r1);
    float phi = (2 * PI) * r2;
    dir.x = r * cos(phi);
    dir.z = r * sin(phi);
    dir.y = sqrt(max(0.0, 1.0 - dir.x * dir.x - dir.z * dir.z));
    return dir;
}




//Evaluate Start -----------------------------------------------------------------------------------------
static float3 EvaluateSheen(MaterialData hitDat, const float3 wo, const float3 wm, const float3 wi)
{
    if (hitDat.sheen <= 0.0f) {
        return 0;
    }

    float dotHL = dot(wm, wi);
    float3 tint = CalculateTint(hitDat.surfaceColor);
    return hitDat.sheen * lerp(1.0f, tint, hitDat.sheenTint) * SchlickWeight(dotHL);
}
static float EvaluateDisneyClearcoat(float clearcoat, float alpha, const float3 wo, const float3 wm,
    const float3 wi, inout float fPdfW, inout float rPdfW)
{
    fPdfW = 0;
    rPdfW = 0;
    if (clearcoat <= 0.0f) {
        return 0.0f;
    }
    float absDotNH = AbsCosTheta(wm);
    float dotHL = dot(wm, wi);

    float gloss = lerp(.1, .001, alpha);
    float Dr = GTR1(absDotNH, gloss);
    float FH = SchlickWeight(dotHL);
    float Fr = lerp(0.04f, 1.0f, FH);
    float Gr = SeparableSmithGGXG1(wi, 0.25f) * SeparableSmithGGXG1(wo, 0.25f);
    fPdfW = Dr / (4.0f * abs(dot(wo, wm)));
    return 0.25f * clearcoat * Fr * Gr * Dr * PI;
}


static float3 EvaluateDisneyBRDF(const MaterialData hitDat, const float3 wo, const float3 wm,
    const float3 wi, inout float fPdf, inout float rPdf)
{
    fPdf = 0.0f;
    rPdf = 0.0f;

    float dotNL = CosTheta(wi);
    float dotNV = CosTheta(wo);
    if (dotNL <= 0.0f || dotNV <= 0.0f) {
        return 0;
    }

    float ax, ay;
    CalculateAnisotropicParams(hitDat.roughness, hitDat.anisotropic, ax, ay);

    float d = GgxAnisotropicD(wm, ax, ay);
    float gl = SeparableSmithGGXG1(wi, wm, ax, ay);
    float gv = SeparableSmithGGXG1(wo, wm, ax, ay);
    float3 f = DisneyFresnel(hitDat, wo, wm, wi);

    // fPdf = gv * max(dot(wi, wm), 0.0) * d * (rcp(4.0f * (dot(wi, wm)))) / dotNL;



    GgxVndfAnisotropicPdf(wi, wm, wo, ax, ay, fPdf, rPdf);
    fPdf *= (1.0f / (4 * abs(dot(wo, wm))));
    // rPdf *= (1.0f / (4 * abs(dot(wi, wm))));
    // _DebugTex[int2(pixel_index % screen_width, pixel_index / screen_width)] = float4(d * gl * gv * f/ (4.0f * dotNL * dotNV),1);
    return d * gl * gv * f / (4.0f * dotNL * dotNV);
}




static float3 EvaluateDisneySpecTransmission(const MaterialData hitDat, const float3 wo, const float3 wm,
    const float3 wi, float ax, float ay, bool thin)
{
    float relativeIor = hitDat.ior;
    float n2 = relativeIor * relativeIor;

    float absDotNL = AbsCosTheta(wi);
    float absDotNV = AbsCosTheta(wo);
    float dotHL = dot(wm, wi);
    float dotHV = dot(wm, wo);
    float absDotHL = abs(dotHL);
    float absDotHV = abs(dotHV);

    float d = GgxAnisotropicD(wm, ax, ay);
    float gl = SeparableSmithGGXG1(wi, wm, ax, ay);
    float gv = SeparableSmithGGXG1(wo, wm, ax, ay);

    float f = Dielectric(dotHV, 1.0f, 1.0f / hitDat.ior);

    float3 color;
    if (thin)
        color = sqrt(hitDat.surfaceColor);
    else
        color = hitDat.surfaceColor;

    // Note that we are intentionally leaving out the 1/n2 spreading factor since for VCM we will be evaluating
    // particles with this. That means we'll need to model the air-[other medium] transmission if we ever place
    // the camera inside a non-air medium.
    float c = (absDotHL * absDotHV) / (absDotNL * absDotNV);
    float t = (n2 / pow(dotHL + relativeIor * dotHV, 2));
    return color * c * t * (1.0f - f) * gl * gv * d;
}

static float EvaluateDisneyRetroDiffuse(const MaterialData hitDat, const float3 wo, const float3 wm, const float3 wi)
{
    float dotNL = AbsCosTheta(wi);
    float dotNV = AbsCosTheta(wo);

    float roughness = hitDat.roughness * hitDat.roughness;

    float rr = 0.5f + 2.0f * dotNL * dotNL * roughness;
    float fl = SchlickWeight(dotNL);
    float fv = SchlickWeight(dotNV);

    return rr * (fl + fv + fl * fv * (rr - 1.0f));
}

static float EvaluateDisneyDiffuse(const MaterialData hitDat, const float3 wo, const float3 wm,
    const float3 wi, bool thin)
{
    float dotNL = AbsCosTheta(wi);
    float dotNV = AbsCosTheta(wo);

    float fl = SchlickWeight(dotNL);
    float fv = SchlickWeight(dotNV);

    float hanrahanKrueger = 0.0f;

    if (thin && hitDat.flatness > 0.0f) {
        float roughness = hitDat.roughness * hitDat.roughness;

        float dotHL = dot(wm, wi);
        float fss90 = dotHL * dotHL * roughness;
        float fss = lerp(1.0f, fss90, fl) * lerp(1.0f, fss90, fv);

        float ss = 1.25f * (fss * (1.0f / (dotNL + dotNV) - 0.5f) + 0.5f);
        hanrahanKrueger = ss;
    }

    float lambert = 1.0f;
    float retro = EvaluateDisneyRetroDiffuse(hitDat, wo, wm, wi);
    float subsurfaceApprox = lerp(lambert, hanrahanKrueger, thin ? hitDat.flatness : 0.0f);

    return rcp(PI) * (retro + subsurfaceApprox * (1.0f - 0.5f * fl) * (1.0f - 0.5f * fv));
}



//Evaluate End -----------------------------------------------------------------------------------------
//Sample Start -----------------------------------------------------------------------------------------



#define eIsotropic 0.5f
#define eVacuum 0.1f

static bool SampleDisneySpecTransmission(const MaterialData hitDat, float3 v, bool thin, inout BsdfSample sample, float3x3 TruTan, out float refracted, uint pixel_index)
{
    float3 wo = ToLocal(TruTan, v); // NDotL = L.z; NDotV = V.z; NDotH = H.z
    refracted = false;
    if (CosTheta(wo) == 0.0) {
        sample.forwardPdfW = 0.0f;
        sample.reversePdfW = 0.0f;
        sample.reflectance = 0;
        sample.wi = 0;
        return false;
    }

    // -- Scale roughness based on IOR
    float rscaled = thin ? ThinTransmissionRoughness(hitDat.ior, hitDat.roughness) : hitDat.roughness;

    float tax, tay;
    CalculateAnisotropicParams(rscaled, hitDat.anisotropic, tax, tay);

    // -- Sample visible distribution of normals
    float r0 = random(23, pixel_index).x;
    float r1 = random(23, pixel_index).y;
    float3 wm = SampleGgxVndfAnisotropic(wo, tax, tay, r0, r1);

    float dotVH = dot(wo, wm);
    if (wm.y < 0.0f) {
        dotVH = -dotVH;
    }

    // -- Disney uses the full dielectric Fresnel equation for transmission. We also importance sample F
    // -- to switch between refraction and reflection at glancing angles.
    float F = Dielectric(dotVH, 1.0f, hitDat.ior);

    // -- Since we're sampling the distribution of visible normals the pdf cancels out with a number of other terms.
    // -- We are left with the weight G2(wi, wo, wm) / G1(wi, wm) and since Disney uses a separable masking function
    // -- we get G1(wi, wm) * G1(wo, wm) / G1(wi, wm) = G1(wo, wm) as our weight.
    float G1v = SeparableSmithGGXG1(wo, wm, tax, tay);

    float pdf;
    refracted = false;

    float3 wi;
    if (random(24, pixel_index).x <= F) {
        wi = normalize(reflect(-wo, wm));

        sample.reflectance = G1v * hitDat.surfaceColor;

        float jacobian = (4 * abs(dot(wo, wm)));
        pdf = F / jacobian;
    }
    else {
        if (thin) {
            // -- When the surface is thin so it refracts into and then out of the surface during this shading event.
            // -- So the ray is just reflected then flipped and we use the sqrt of the surface color.
            wi = reflect(-wo, wm);
            wi.y = -wi.y;
            refracted = true;
            sample.reflectance = G1v * sqrt(hitDat.surfaceColor);

            // -- Since this is a thin surface we are not ending up inside of a volume so we treat this as a scatter event.
            sample.InsideMedium = 0;
        }
        else {
            sample.extinction = 1;
            if (Transmit(wm, wo, hitDat.relativeIOR, wi)) {
                sample.InsideMedium = 1;
                refracted = true;
                sample.phaseFunction = dotVH > 0.0f ? eIsotropic : eVacuum;
                sample.extinction = CalculateExtinction(hitDat.transmittanceColor, hitDat.flatness);
            }
            else {
                sample.InsideMedium = 0;
                wi = reflect(-wo, wm);
            }

            sample.reflectance = G1v * hitDat.surfaceColor;// * sample.extinction;
        }

        wi = normalize(wi);

        float dotLH = abs(dot(wi, wm));
        float jacobian = dotLH / (pow(dotLH + hitDat.relativeIOR * dotVH, 2));
        pdf = (1.0f - F) / jacobian;
    }

    if (CosTheta(wi) == 0.0f) {
        sample.forwardPdfW = 0.0f;
        sample.reversePdfW = 0.0f;
        sample.reflectance = 0;
        sample.wi = 0;
        refracted = false;
        return false;
    }

    // -- calculate VNDF pdf terms and apply Jacobian and Fresnel sampling adjustments
    GgxVndfAnisotropicPdf(wi, wm, wo, tax, tay, sample.forwardPdfW, sample.reversePdfW);
    sample.forwardPdfW *= pdf;
    sample.reversePdfW *= pdf;
    // -- convert wi back to world space
    sample.wi = normalize(ToWorld(TruTan, wi));

    return true;
}

static bool SampleDisneyDiffuse(const MaterialData hitDat, float3 v, bool thin,
    inout BsdfSample sample, float3x3 TruTan, inout bool refracted, uint pixel_index)
{
    float3 wo = ToLocal(TruTan, v); // NDotL = L.z; NDotV = V.z; NDotH = H.z

    float sig = sign(CosTheta(wo));

    // -- Sample cosine lobe
    float r0 = random(43, pixel_index).x;
    float r1 = random(43, pixel_index).y;
    float3 wi = CosineSampleHemisphere2(r0, r1);
    float3 wm = normalize(wi + wo);

    float dotNL = CosTheta(wi);
    if (dotNL == 0.0f) {
        sample.forwardPdfW = 0.0f;
        sample.reversePdfW = 0.0f;
        sample.reflectance = 0;
        sample.wi = 0;
        return false;
    }

    float dotNV = CosTheta(wo);

    float pdf;

    sample.InsideMedium = 0;

    float3 color = hitDat.surfaceColor;

    float p = random(64, pixel_index).x;
    sample.extinction = 1;
    if (p <= hitDat.diffTrans) {
        wi = -wi;
        pdf = hitDat.diffTrans;
        refracted = true;

        if (thin)
            color = sqrt(color);
        else {
            sample.InsideMedium = 1;

            sample.phaseFunction = eIsotropic;
            sample.extinction = CalculateExtinction(hitDat.transmittanceColor, hitDat.scatterDistance);
        }
    }
    else {
        pdf = (1.0f - hitDat.diffTrans);
    }

    float3 sheen = EvaluateSheen(hitDat, wo, wm, wi);

    float diffuse = EvaluateDisneyDiffuse(hitDat, wo, wm, wi, thin);
    sample.reflectance = (sheen + color * (diffuse / pdf)) * sample.extinction;
    sample.wi = normalize(ToWorld(TruTan, wi));
    sample.forwardPdfW = abs(dotNL) * pdf;
    sample.reversePdfW = abs(dotNV) * pdf;
    return true;
}

static bool SampleDisneyBRDF(const MaterialData hitDat, float3 v, inout BsdfSample sample, float3x3 TruTan, uint pixel_index)
{
    float3 wo = ToLocal(TruTan, v); // NDotL = L.z; NDotV = V.z; NDotH = H.z

        // -- Calculate Anisotropic params
    float ax, ay;
    CalculateAnisotropicParams(hitDat.roughness, hitDat.anisotropic, ax, ay);

    // -- Sample visible distribution of normals
    float r0 = random(203, pixel_index).x;
    float r1 = random(203, pixel_index).y;
    float3 wm = SampleGgxVndfAnisotropic(wo, ay, ax, r1, r0);

    // -- Reflect over wm
    float3 wi = normalize(reflect(-wo, wm));
    if (CosTheta(wi) <= 0.0f) {
        sample.forwardPdfW = 0.0f;
        sample.reversePdfW = 0.0f;
        sample.reflectance = 0;
        sample.wi = 0;
        return false;
    }

    // -- Fresnel term for this lobe is complicated since we're blending with both the metallic and the specularTint
    // -- parameters plus we must take the IOR into account for dielectrics
    float3 F = DisneyFresnel(hitDat, wo, wm, wi);

    // -- Since we're sampling the distribution of visible normals the pdf cancels out with a number of other terms.
    // -- We are left with the weight G2(wi, wo, wm) / G1(wi, wm) and since Disney uses a separable masking function
    // -- we get G1(wi, wm) * G1(wo, wm) / G1(wi, wm) = G1(wo, wm) as our weight.
    float G1v = SeparableSmithGGXG1(wo, wm, ay, ax);
    float3 specular = G1v * F;

    sample.InsideMedium = 0;
    sample.reflectance = specular;
    sample.wi = normalize(ToWorld(TruTan, wi));
    GgxVndfAnisotropicPdf(wi, wm, wo, ay, ax, sample.forwardPdfW, sample.reversePdfW);

    sample.forwardPdfW *= (1.0f / (4 * abs(dot(wo, wm))));
    sample.reversePdfW *= (1.0f / (4 * abs(dot(wi, wm))));

    return true;
}

static bool SampleDisneyClearcoat(const MaterialData hitDat, const float3 v,
    inout BsdfSample sample, float3x3 TruTan, uint pixel_index)
{
    float gloss = lerp(0.1f, 0.001f, hitDat.clearcoatGloss);
    float3 wo = ToLocal(TruTan, v); // NDotL = L.z; NDotV = V.z; NDotH = H.z

    float a = gloss;
    float a2 = a * a;

    float r0 = random(102, pixel_index).x;
    float r1 = random(102, pixel_index).y;
    float cosTheta = sqrt(max(1e-6, (1.0f - pow(a2, 1.0f - r0)) / (1.0f - a2)));
    float sinTheta = sqrt(max(1e-6, 1.0f - cosTheta * cosTheta));
    float phi = 2 * PI * r1;

    float3 wm = float3(sinTheta * cos(phi), cosTheta, sinTheta * sin(phi));
    if (dot(wm, wo) < 0.0f) {
        wm = -wm;
    }

    float3 wi = reflect(-wo, wm);
    // if(dot(wi, wo) < 0.0f) {
    //     return false;
    // }

    float clearcoatWeight = hitDat.clearcoat;
    float clearcoatGloss = hitDat.clearcoatGloss;

    float dotNH = CosTheta(wm);
    float dotLH = dot(wm, wi);

    float d = GTR1(abs(dotNH), lerp(0.1f, 0.001f, clearcoatGloss));
    float FH = SchlickWeight(dotLH);
    float f = lerp(0.04f, 1.0f, FH);//Schlick(0.04f, (dotLH));
    float g = SeparableSmithGGXG1(wi, 0.25f) * SeparableSmithGGXG1(wo, 0.25f);

    float fPdf = d / (4.0f * dot(wo, wm));

    sample.reflectance = (0.25f * clearcoatWeight * g * f * d) / fPdf;
    sample.wi = normalize(ToWorld(TruTan, wi));
    sample.forwardPdfW = fPdf;
    sample.reversePdfW = d / (4.0f * dot(wi, wm));

    return true;
}

//Sample End -----------------------------------------------------------------------------------------
//Reconstruct Start -----------------------------------------------------------------------------------------

inline float3 ReconstructDisneySpecTransmission(const MaterialData hitDat, const float3 wo, float3 wm,
    const float3 wi, inout float fPdf, inout float rPdf, uint pixel_index)
{

    // -- Scale roughness based on IOR
    float rscaled = hitDat.roughness;

    float tax, tay;
    CalculateAnisotropicParams(rscaled, hitDat.anisotropic, tax, tay);

    float dotVH = dot(wo, wm);
    if (wm.y < 0.0f) {
        dotVH = -dotVH;
    }

    float ni = wo.y > 0.0f ? 1.0f : hitDat.ior;
    float nt = wo.y > 0.0f ? hitDat.ior : 1.0f;
    float relativeIOR =  ni / nt;

    // -- Disney uses the full dielectric Fresnel equation for transmission. We also importance sample F
    // -- to switch between refraction and reflection at glancing angles.
    float F = Dielectric(dotVH, 1.0f, hitDat.ior);

    // -- Since we're sampling the distribution of visible normals the pdf cancels out with a number of other terms.
    // -- We are left with the weight G2(wi, wo, wm) / G1(wi, wm) and since Disney uses a separable masking function
    // -- we get G1(wi, wm) * G1(wo, wm) / G1(wi, wm) = G1(wo, wm) as our weight.
    float G1v = SeparableSmithGGXG1(wo, wm, tax, tay);

    float pdf;

    float3 reflectance;
    if (random(24, pixel_index).x <= F) {
        reflectance = G1v * hitDat.surfaceColor;

        float jacobian = (4 * abs(dot(wo, wm)));
        pdf = F / jacobian;
    }
    else {

        reflectance = G1v * hitDat.surfaceColor;


        float dotLH = abs(dot(wi, wm));
        float jacobian = dotLH / (pow(dotLH + relativeIOR * dotVH, 2));
        pdf = (1.0f - F) / jacobian;
    }

    // -- calculate VNDF pdf terms and apply Jacobian and Fresnel sampling adjustments
    GgxVndfAnisotropicPdf(wi, wm, wo, tax, tay, fPdf, rPdf);
    fPdf *= pdf;

    return reflectance;
}

inline float3 ReconstructDisneyBRDF(const MaterialData hitDat, const float3 wo, float3 wm,
    const float3 wi, inout float fPdf, inout float rPdf, inout bool Success)
{
    fPdf = 0.0f;
    rPdf = 0.0f;

    float dotNL = CosTheta(wi);
    float dotNV = CosTheta(wo);
    if (CosTheta(wi) <= 0.0f) {
        Success = false;
        return 0;
    }

    float ax, ay;
    CalculateAnisotropicParams(hitDat.roughness, hitDat.anisotropic, ax, ay);

    //     float r0 = random(203).x;
    // float r1 = random(203).y;
    // wm = SampleGgxVndfAnisotropic(wo, ay, ax, r1, r0);

    float3 f = DisneyFresnel(hitDat, wo, wm, wi);

    GgxVndfAnisotropicPdf(wi, wm, wo, ay, ax, fPdf, rPdf);
    fPdf *= 1.0f / (4 * abs(dot(wo, wm)));

    float G1v = SeparableSmithGGXG1(wo, wm, ay, ax);
    float3 specular = G1v * f;
    Success = (fPdf > 0);//0.00025f * hitDat.roughness);
    // _DebugTex[int2(pixel_index % screen_width, pixel_index / screen_width)] = float4(d * gl * gv * f/ (4.0f * dotNL * dotNV),1);
    return specular;//d * gl * gv * f / (4.0f * dotNL * dotNV); 
}
inline float ReconstructDisneyClearcoat(float clearcoat, float alpha, const float3 wo, const float3 wm,
    const float3 wi, inout float fPdfW, inout float rPdfW, out bool success)
{
    fPdfW = 0;
    rPdfW = 0;
    if (clearcoat <= 0.0f) {
        success = false;
        return 0.0f;
    }
    float absDotNH = AbsCosTheta(wm);
    float dotHL = dot(wm, wi);

    float gloss = lerp(.1, .001, alpha);
    float Dr = GTR1(absDotNH, gloss);
    float FH = SchlickWeight(dotHL);
    float Fr = lerp(0.04f, 1.0f, FH);
    float Gr = SeparableSmithGGXG1(wi, 0.25f) * SeparableSmithGGXG1(wo, 0.25f);
    fPdfW = Dr / (4.0f * abs(dot(wo, wm)));
    success = fPdfW > 0.1f;
    return 0.25f * clearcoat * Fr * Gr * Dr;
}


//Reconstruct End -----------------------------------------------------------------------------------------






static void CalculateLobePdfs(const MaterialData hitDat,
    inout float pSpecular, inout float pDiffuse, inout float pClearcoat, inout float pSpecTrans)
{
    float metallicBRDF = hitDat.metallic;
    float specularBSDF = (1.0f - hitDat.metallic) * hitDat.specTrans;
    float dielectricBRDF = (1.0f - hitDat.specTrans) * (1.0f - hitDat.metallic);

    float specularWeight = metallicBRDF + hitDat.Specular;
    float transmissionWeight = specularBSDF;
    float diffuseWeight = dielectricBRDF;
    float clearcoatWeight = 1.0f * saturate(hitDat.clearcoat);

    float norm = 1.0f / (specularWeight + transmissionWeight + diffuseWeight + clearcoatWeight);

    pSpecular = specularWeight * norm;
    pSpecTrans = transmissionWeight * norm;
    pDiffuse = diffuseWeight * norm;
    pClearcoat = clearcoatWeight * norm;
}



float3 EvaluateDisney(MaterialData hitDat, float3 V, float3 L, bool thin,
    inout float forwardPdf, inout float reversePdf, float3x3 TruTan, uint pixel_index)
{


    float3 wo = ToLocal(TruTan, V); // NDotL = L.z; NDotV = V.z; NDotH = H.z
    float3 wi = ToLocal(TruTan, L); // NDotL = L.z; NDotV = V.z; NDotH = H.z
// hitDat.surfaceColor *= PI;
    float3 wm = normalize(wo + wi);

    float dotNV = CosTheta(wo);
    float dotNL = CosTheta(wi);

    float3 reflectance = 0;
    forwardPdf = 0.0f;
    reversePdf = 0.0f;

    float pBRDF, pDiffuse, pClearcoat, pSpecTrans;
    CalculateLobePdfs(hitDat, pBRDF, pDiffuse, pClearcoat, pSpecTrans);

    float3 baseColor = hitDat.surfaceColor;
    float metallic = hitDat.metallic;
    float specTrans = hitDat.specTrans;
    float roughness = hitDat.roughness;

    float diffuseWeight = (1.0f - metallic) * (1.0f - specTrans);
    float transWeight = (1.0f - metallic) * specTrans;

    // -- Clearcoat
    bool upperHemisphere = dotNL > 0.0f && dotNV > 0.0f;
    if (upperHemisphere && hitDat.clearcoat > 0.0f) {

        float forwardClearcoatPdfW;
        float reverseClearcoatPdfW;

        float clearcoat = EvaluateDisneyClearcoat(hitDat.clearcoat, hitDat.clearcoatGloss, wo, wm, wi,
            forwardClearcoatPdfW, reverseClearcoatPdfW);
        reflectance += clearcoat;
        forwardPdf += pClearcoat * forwardClearcoatPdfW;
        reversePdf += pClearcoat * reverseClearcoatPdfW;
    }

    // -- Diffuse
    if (diffuseWeight > 0.0f) {
        float forwardDiffusePdfW = AbsCosTheta(wi);
        float reverseDiffusePdfW = AbsCosTheta(wo);
        float diffuse = EvaluateDisneyDiffuse(hitDat, wo, wm, wi, thin);

        float3 sheen = EvaluateSheen(hitDat, wo, wm, wi);

        reflectance += (diffuse * hitDat.surfaceColor + sheen);

        forwardPdf += pDiffuse * forwardDiffusePdfW;
        reversePdf += pDiffuse * reverseDiffusePdfW;
    }

    // -- transmission
    if (transWeight > 0.0f) {

        // Scale roughness based on IOR (Burley 2015, Figure 15).
        float rscaled = thin ? ThinTransmissionRoughness(hitDat.ior, hitDat.roughness) : hitDat.roughness;
        float tax, tay;
        CalculateAnisotropicParams(rscaled, hitDat.anisotropic, tax, tay);

        float3 transmission = EvaluateDisneySpecTransmission(hitDat, wo, wm, wi, tax, tay, thin);
        reflectance += transWeight * transmission;

        float forwardTransmissivePdfW;
        float reverseTransmissivePdfW;
        GgxVndfAnisotropicPdf(wi, wm, wo, tax, tay, forwardTransmissivePdfW, reverseTransmissivePdfW);

        float dotLH = dot(wm, wi);
        float dotVH = dot(wm, wo);
        forwardPdf += pSpecTrans * forwardTransmissivePdfW / (pow(dotLH + hitDat.relativeIOR * dotVH, 2));
        reversePdf += pSpecTrans * reverseTransmissivePdfW / (pow(dotVH + hitDat.relativeIOR * dotLH, 2));
    }

    // -- specular
    if (upperHemisphere) {
        float forwardMetallicPdfW;
        float reverseMetallicPdfW;
        // hitDat.surfaceColor *= PI;
        float3 specular = EvaluateDisneyBRDF(hitDat, wo, wm, wi, forwardMetallicPdfW, reverseMetallicPdfW);

        reflectance += specular;// * (abs(dot(wi, wm)));
        forwardPdf += pBRDF * forwardMetallicPdfW / (4 * abs(dot(wo, wm)));
        reversePdf += pBRDF * reverseMetallicPdfW / (4 * abs(dot(wi, wm)));
    }

    reflectance = reflectance * abs(dotNL);
    // _DebugTex[int2(pixel_index % screen_width, pixel_index / screen_width)] = float4(reflectance,1);

    return reflectance;
}

bool2 SampleDisney(MaterialData hitDat, float3 v, bool thin, inout BsdfSample sample, float3x3 TruTanMat, out int Case, uint pixel_index)
{
    float pSpecular;
    float pDiffuse;
    float pClearcoat;
    float pTransmission;
    hitDat.surfaceColor *= PI;
    CalculateLobePdfs(hitDat, pSpecular, pDiffuse, pClearcoat, pTransmission);
    // GetLobeProbabilities(pDiffuse, pSpecular, pTransmission, pClearcoat, hitDat);

    bool success = false;
    float3 Reflection = 0;
    float PDF = 0;
    hitDat.surfaceColor /= PI;
    bool refracted = false;
    if(pSpecular > 0) {
        success = SampleDisneyBRDF(hitDat, v, sample, TruTanMat, pixel_index);
        Reflection += sample.reflectance * pSpecular;
        PDF +=  sample.forwardPdfW * pSpecular;
    }
hitDat.surfaceColor *= PI;
    if(pClearcoat > 0) {
        success = SampleDisneyClearcoat(hitDat, v, sample, TruTanMat, pixel_index);
        Reflection += sample.reflectance * pClearcoat;
            PDF +=  sample.forwardPdfW * pClearcoat;
    }
    if(pDiffuse > 0) {
        success = SampleDisneyDiffuse(hitDat, v, thin, sample, TruTanMat, refracted, pixel_index);
        Reflection += sample.reflectance * pDiffuse;
            PDF +=  sample.forwardPdfW * pDiffuse;
    }
    float pLobe = 0.0f;
    float p = random(194, pixel_index).x;
    if (p <= pSpecular) {
        hitDat.surfaceColor /= PI;
        success = SampleDisneyBRDF(hitDat, v, sample, TruTanMat, pixel_index);
        pLobe = pSpecular;
        Case = 0;
    }
    else if (p > pSpecular && p <= (pSpecular + pClearcoat)) {
        success = SampleDisneyClearcoat(hitDat, v, sample, TruTanMat, pixel_index);
        pLobe = pClearcoat;
        Case = 1;
    }
    else if (p > pSpecular + pClearcoat && p <= (pSpecular + pClearcoat + pDiffuse)) {
        success = SampleDisneyDiffuse(hitDat, v, thin, sample, TruTanMat, refracted, pixel_index);
        pLobe = pDiffuse;
        Case = 2;
    }
    else if (pTransmission >= 0.0f) {
        hitDat.surfaceColor /= PI;
        success = SampleDisneySpecTransmission(hitDat, v, thin, sample, TruTanMat, refracted, pixel_index);
        pLobe = pTransmission;
        Case = 3;
    }
    else {
        // -- Make sure we notice if this is occurring.
        sample.reflectance = float3(1000000.0f, 0.0f, 0.0f);
        sample.forwardPdfW = 0.000000001f;
        sample.reversePdfW = 0.000000001f;
        Case = 4;
    }

    // if (pLobe > 0.0f) {
        sample.reflectance = Reflection;
        sample.forwardPdfW = PDF;
        // sample.reversePdfW *= pLobe;
    // }

    return bool2(success, refracted);
}



float3 ReconstructDisney(MaterialData hitDat, float3 V, float3 L, bool thin,
    inout float forwardPdf, float3x3 TruTan, int Case, float3 Norm, inout bool Success, uint pixel_index)
{

    float reversePdf;
    float3 wo = ToLocal(TruTan, V); // NDotL = L.z; NDotV = V.z; NDotH = H.z
    float3 wi = ToLocal(TruTan, L); // NDotL = L.z; NDotV = V.z; NDotH = H.z
    hitDat.surfaceColor *= PI;
    float3 wm = normalize(wo + wi);

    float dotNV = CosTheta(wo);
    float dotNL = CosTheta(wi);

    float3 reflectance = 0;
    forwardPdf = 0.0f;
    reversePdf = 0.0f;

    float pBRDF, pDiffuse, pClearcoat, pSpecTrans;
    CalculateLobePdfs(hitDat, pBRDF, pDiffuse, pClearcoat, pSpecTrans);

    float3 baseColor = hitDat.surfaceColor;
    float metallic = hitDat.metallic;
    float specTrans = hitDat.specTrans;
    float roughness = hitDat.roughness;

    // calculate all of the anisotropic params

    float diffuseWeight = (1.0f - metallic) * (1.0f - specTrans);
    float transWeight = (1.0f - metallic) * specTrans;


    Success = true;
    [branch]switch (Case) {
    case 0:{
            float forwardMetallicPdfW;
            float reverseMetallicPdfW;
            hitDat.surfaceColor /= PI;
            float3 specular = ReconstructDisneyBRDF(hitDat, wo, wm, wi, forwardMetallicPdfW, reverseMetallicPdfW, Success);
    
            reflectance = rcp(pBRDF) * specular;// / forwardMetallicPdfW * abs(dotNL);
            forwardPdf = pBRDF * forwardMetallicPdfW;// / (4 * abs(dot(wo, wm)));
            break;}
    case 1:{
            float forwardClearcoatPdfW;
            float reverseClearcoatPdfW;
    
            float clearcoat = ReconstructDisneyClearcoat(hitDat.clearcoat, hitDat.clearcoatGloss, wo, wm, wi,
                forwardClearcoatPdfW, reverseClearcoatPdfW, Success);
            reflectance = rcp(pClearcoat) * clearcoat / (forwardClearcoatPdfW);
            forwardPdf = pClearcoat * forwardClearcoatPdfW;
            // Success = forwardPdf > 0;
            break;}
    case 2:{
            float forwardDiffusePdfW = AbsCosTheta(wi);
            float reverseDiffusePdfW = AbsCosTheta(wo);
            float diffuse = EvaluateDisneyDiffuse(hitDat, wo, wm, wi, thin);
    
            float3 sheen = EvaluateSheen(hitDat, wo, wm, wi);
    
            reflectance = rcp(pDiffuse) * (diffuse * hitDat.surfaceColor + sheen);
    
            forwardPdf = pDiffuse * forwardDiffusePdfW;
            Success = forwardPdf > 0;
            break;}
    case 3:{
            float forwardSpecPdfW;
            float reverseSpecPdfW;
            hitDat.surfaceColor /= PI;
            reflectance = ReconstructDisneySpecTransmission(hitDat, wo, wm, wi,
                forwardSpecPdfW, reverseSpecPdfW, pixel_index);
            forwardPdf = pSpecTrans * forwardSpecPdfW;
            Success = forwardPdf > 0;
            break;}
    default:{
            reflectance = 0;
            forwardPdf = 0;
            break;}

    }

    return reflectance;
}






inline void orthonormal_basis(const float3 normal, inout float3 tangent, inout float3 binormal) {
    float sign2 = (normal.z >= 0.0f) ? 1.0f : -1.0f;
    float a = -1.0f / (sign2 + normal.z);
    float b = normal.x * normal.y * a;

    tangent = float3(1.0f + sign2 * normal.x * normal.x * a, sign2 * b, -sign2 * normal.x);
    binormal = float3(b, sign2 + normal.y * normal.y * a, -normal.y);
}



//Use GetTangentSpace, not GetTangentSpace2
void SampleSSS(MaterialData MaterialData, float3 Norm, float3 wi, int pixel_index, out float3 wo, out float pdf, out float3 bsdf_value) {
    float3 u, v;
    orthonormal_basis(Norm, u, v);
    float2 rand = random(53, pixel_index);
    float cos_theta = pow(rand.x, 1.0f / (2));
    float sin_theta = sqrt(1 - cos_theta * cos_theta);
    wo = (u * cos(rand.y * 2 * PI) + v * sin(rand.y * 2 * PI)) * sin_theta + Norm * cos_theta;
    float n_dot_o = abs(dot(Norm, wo));
    if(n_dot_o < 0.001f) {
        pdf = 0;
        bsdf_value = 0;
    } else {
        float3 diffuse = MaterialData.surfaceColor;
        bsdf_value = diffuse * rcp(PI) * n_dot_o;
        pdf = rcp(PI) * n_dot_o;
    }
}


inline bool EvaluateBsdf(const MaterialData hitDat, float3 DirectionIn, float3 DirectionOut, float3 Normal, out float PDF, out float3 bsdf_value, uint pixel_index) {
    float throwaway = 0;
    bool validbsdf = false;
    float cos_theta_hit = dot(DirectionOut, Normal);
    [branch] switch (hitDat.MatType) {//Switch between different materials
        default:
            validbsdf = evaldiffuse(DirectionOut, cos_theta_hit, bsdf_value, PDF);
            bsdf_value *= hitDat.surfaceColor;
        break;
        case DisneyIndex:
            bsdf_value = EvaluateDisney(hitDat, -DirectionIn, DirectionOut, hitDat.Thin == 1, PDF, throwaway, GetTangentSpace2(Normal), pixel_index);// DisneyEval(mat, -PrevDirection, norm, to_light, bsdf_pdf, hitDat);
            validbsdf = PDF > 0;
        break;
        case CutoutIndex:
            bsdf_value = EvaluateDisney(hitDat, -DirectionIn, DirectionOut, hitDat.Thin == 1, PDF, throwaway, GetTangentSpace2(Normal), pixel_index);// DisneyEval(mat, -PrevDirection, norm, to_light, bsdf_pdf, hitDat);
            validbsdf = PDF > 0;
        break;
        case VolumetricIndex:
            validbsdf = true;
        break;
    }
    return validbsdf;
}

inline bool ReconstructBsdf(const MaterialData hitDat, float3 DirectionIn, float3 DirectionOut, float3 Normal, out float PDF, out float3 bsdf_value, int Case, const float3x3 TangentSpaceNorm, uint pixel_index) {
    float throwaway = 0;
    bool validbsdf = false;
    float cos_theta_hit = dot(DirectionOut, Normal);
    bsdf_value = 0;
    PDF = 0;
    [branch] switch (hitDat.MatType) {//Switch between different materials
        default:
            validbsdf = evaldiffuse(DirectionOut, cos_theta_hit, bsdf_value, PDF);
            bsdf_value = hitDat.surfaceColor;
        break;
        case DisneyIndex:
            bsdf_value = ReconstructDisney(hitDat, -DirectionIn, DirectionOut, false, PDF, TangentSpaceNorm, Case, Normal, validbsdf, pixel_index);
        break;
        case CutoutIndex:
            bsdf_value = ReconstructDisney(hitDat, -DirectionIn, DirectionOut, false, PDF, TangentSpaceNorm, Case, Normal, validbsdf, pixel_index);
        break;
        case VolumetricIndex:
            validbsdf = true;
        break;
    }
    return validbsdf;
}
