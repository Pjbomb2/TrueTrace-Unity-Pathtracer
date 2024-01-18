
bool evaldiffuse(const float3 to_light, float cos_theta_o, inout float3 bsdf, inout float pdf) {
    if (cos_theta_o <= 0.0f) return false;

    bsdf = float3(cos_theta_o * ONE_OVER_PI, cos_theta_o * ONE_OVER_PI, cos_theta_o * ONE_OVER_PI);
    pdf = cos_theta_o * ONE_OVER_PI;

    return (pdf > 0 || pdf < 0 || pdf == 0);
}



//Disney

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

float3 Schlick3(float3 r0, float rad)
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
    ax = max(0.000001f, (roughness * roughness) / aspect);
    ay = max(0.000001f, (roughness * roughness) * aspect);
}

void GgxVndfAnisotropicPdf ( float3 wi , float3 wm , float3 wo, float ax, float ay, inout float pdf) {
float2 alpha = float2(ax, ay);
float3 i = wi.xzy;
float3 o = wo.xzy;
float ndf = GgxAnisotropicD(wm, ax, ay);
float2 ai = alpha * i . xy ;
float len2 = dot( ai , ai ) ;
float t = sqrt ( len2 + i . z * i . z ) ;
if ( i . z >= 0.0f ) {
float a = saturate (min( alpha .x, alpha .y)); // Eq. 6
float s = 1.0f + length ( float2 (i.x, i.y)); // Omit sgn for a <=1
float a2 = a * a; float s2 = s * s;
float k = (1.0f - a2) * s2 / (s2 + a2 * i.z * i.z); // Eq. 5
pdf = ndf / (2.0f * (k * i . z + t ) ) ; // Eq. 8 * || dm/do ||
return;
}
// Numerically stable form of the previous PDF for i.z < 0
pdf = ndf * ( t - i . z ) / (2.0f * len2 ) ; // = Eq. 7 * || dm/do ||
}

void GgxVndfAnisotropicPdf2(const float3 wi, const float3 wm, const float3 wo, float ax, float ay,
    inout float forwardPdfW)
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
    return (rParallel * rParallel + rPerpendicuar * rPerpendicuar) / 2.0f;
}

static float3 DisneyFresnel(MaterialData hitDat, const float3 wo, const float3 wm, const float3 wi)
{
    float dotHV = dot(wm, wo);

    float3 tint = CalculateTint(hitDat.surfaceColor);

    // -- See section 3.1 and 3.2 of the 2015 PBR presentation + the Disney BRDF explorer (which does their 2012 remapping
    // -- rather than the SchlickR0FromRelativeIOR seen here but they mentioned the switch in 3.2).
    float3 R0 = SchlickR0FromRelativeIOR(1.0f / hitDat.ior) * lerp(1.0f, tint, hitDat.specularTint);
    R0 = lerp(R0, hitDat.surfaceColor, hitDat.metallic);

    float dielectricFresnel = Dielectric(dotHV, 1.0f, hitDat.ior);
    float3 metallicFresnel = Schlick3(R0, dot(wi, wm));

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
float3 SampleGgxVndfAnisotropic2(float3 wi, float ax, float ay, float u1, float u2)
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

float3 SampleGgxVndfAnisotropic ( float3 i , float alphax, float alphay , float randx, float randy, inout float3 m2) {
    float2 alpha = float2(alphax, alphay);
    float2 rand = float2(randx, randy);
i = i.xzy;    
float3 i_std = normalize ( float3 ( i . xy * alpha , i . z ) ) ;
// Sample a spherical cap
float phi = 2.0f * PI * rand . x ;
float a = saturate (min( alpha .x, alpha .y)); // Eq. 6
float s = 1.0f + length ( float2 (i.x, i.y)); // Omit sgn for a <=1
float a2 = a * a; float s2 = s * s;
float k = (1.0f - a2) * s2 / (s2 + a2 * i.z * i.z); // Eq. 5
float b = i . z > 0 ? k * i_std . z : i_std . z ;
float z = mad (1.0f - rand .y , 1.0f + b , -b ) ;
float sinTheta = sqrt ( saturate (1.0f - z * z ) ) ;
float3 o_std = { sinTheta * cos( phi ) , sinTheta * sin( phi ) , z };
// Compute the microfacet normal m
float3 m_std = i_std + o_std ;
float3 m = normalize ( float3 ( m_std . xy * alpha , m_std . z ) ) ;
m2 = m.xzy;
// Return the reflection vector o
float3 i2 = 2.0f * dot(i , m ) * m - i ;
return i2.xzy;
}


float3 SampleGgxVndfAnisotropic3(const float3 wo, float ax, float ay, float u1, float u2)
{
    // -- Stretch the view vector so we are sampling as though roughness==1
    float3 v = normalize(float3(wo.x * ax, wo.y, wo.z * ay));

    // -- Build an orthonormal basis with v, t1, and t2
    float3 t1 = (v.y < 0.9999f) ? normalize(cross(v, float3(0, 1, 0))) : float3(1, 0, 0);
    float3 t2 = cross(t1, v);

    // -- Choose a point on a disk with each half of the disk weighted proportionally to its projection onto direction v
    float a = 1.0f / (1.0f + v.y);
    float r = sqrt(u1);
    float phi = (u2 < a) ? (u2 / a) * PI : PI + (u2 - a) / (1.0f - a) * PI;
    float p1 = r * cos(phi);
    float p2 = r * sin(phi) * ((u2 < a) ? 1.0f : v.y);

    // -- Calculate the normal in this stretched tangent space
    float3 n = p1 * t1 + p2 * t2 + sqrt(max(0.0f, 1.0f - p1 * p1 - p2 * p2)) * v;

    // -- unstretch and normalize the normal
    return normalize(float3(ax * n.x, n.y, ay * n.z));
}


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

    float dotHL = abs(dot(wm, wi));
    float3 tint = CalculateTint(hitDat.surfaceColor);
    return hitDat.sheen * lerp(1.0f, tint, hitDat.sheenTint) * SchlickWeight(dotHL);
}
static float EvaluateDisneyClearcoat(float clearcoat, float alpha, const float3 wo, const float3 wm,
    const float3 wi, inout float fPdfW)
{
    fPdfW = 0;
    if (clearcoat <= 0.0f) {
        return 0.0f;
    }
    float absDotNH = AbsCosTheta(wm);
    float dotHL = dot(wm, wi);

    float gloss = lerp(.1, .001, alpha);
    float Dr = GTR1(absDotNH, gloss);
    float FH =  SchlickWeight(dotHL);
    float Fr = lerp(0.04f, 1.0f, FH);
    float Gr = SeparableSmithGGXG1(wi, 0.25f) * SeparableSmithGGXG1(wo, 0.25f);
    fPdfW = Dr / (4.0f * abs(dot(wo, wm)));
    return 0.25f * clearcoat * Fr * Gr * Dr;
}


static float3 EvaluateDisneyBRDF(const MaterialData hitDat, const float3 wo, const float3 wm,
    const float3 wi, inout float fPdf, int pixel_index)
{
    fPdf = 0.0f;

    float dotNL = CosTheta(wi);
    float dotNV = AbsCosTheta(wo);
    if (dotNL <= 0.0f) {
        return 0;
    }

    float ax, ay;
    CalculateAnisotropicParams(hitDat.roughness, hitDat.anisotropic, ax, ay);

    float d = GgxAnisotropicD(wm, ax, ay);
    float gl = SeparableSmithGGXG1(wi, wm, ax, ay);
    float gv = SeparableSmithGGXG1(wo, wm, ax, ay);
    float3 f = DisneyFresnel(hitDat, wo, wm, wi);

    // fPdf = gv * max(dot(wi, wm), 0.0) * d * (rcp(4.0f * (dot(wi, wm)))) / dotNL;



    GgxVndfAnisotropicPdf(wi, wm, wo, ax, ay, fPdf);
    fPdf *= (1.0f / (4 * abs(dot(wo, wm))));
    // rPdf *= (1.0f / (4 * abs(dot(wi, wm))));

    return d * gl * gv * f / (4.0f * dotNL * dotNV);
}




static float3 EvaluateDisneySpecTransmission(const MaterialData hitDat, const float3 wo, const float3 wm,
    const float3 wi, float ax, float ay, bool thin)
{
    float relativeIor;
    float ni = wo.y > 0.0f ? 1.0f : hitDat.ior;
    float nt = wo.y > 0.0f ? hitDat.ior : 1.0f;
    relativeIor = ni / nt;
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

    float f = Dielectric(dotHV, 1.0f, hitDat.ior);

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

static float3 SampleDisneySpecTransmission(const MaterialData hitDat, float3 wo, bool thin, out float forwardPdfW, out float3 wi, out float refracted, uint pixel_index, bool GotFlipped)
{
    wi = 0;
    forwardPdfW = 0;
    refracted = false;
    if (CosTheta(wo) == 0.0) {
        return -1;
    }
 
    // -- Scale roughness based on IOR
    float rscaled = thin ? ThinTransmissionRoughness(hitDat.ior, hitDat.roughness) : hitDat.roughness;

    float tax, tay;
    CalculateAnisotropicParams(rscaled, hitDat.anisotropic, tax, tay);

    // -- Sample visible distribution of normals
    float2 r = random(119, pixel_index);
    float3 wm = SampleGgxVndfAnisotropic3(wo, tax, tay, r.x, r.y);

    float dotVH = dot(wo, wm);
    if (wm.y < 0.0f) {
        dotVH = -dotVH;
    }

    float ni = !GotFlipped ? 1.0f : hitDat.ior;
    float nt = !GotFlipped ? hitDat.ior : 1.0f;
    float relativeIOR = ni / nt;

    // -- Disney uses the full dielectric Fresnel equation for transmission. We also importance sample F
    // -- to switch between refraction and reflection at glancing angles.
    float F = Dielectric(dotVH, 1.0f, hitDat.ior);

    // -- Since we're sampling the distribution of visible normals the pdf cancels out with a number of other terms.
    // -- We are left with the weight G2(wi, wo, wm) / G1(wi, wm) and since Disney uses a separable masking function
    // -- we get G1(wi, wm) * G1(wo, wm) / G1(wi, wm) = G1(wo, wm) as our weight.
    float3 G1v = SeparableSmithGGXG1(wo, wm, tax, tay);

    float pdf;
    refracted = false;

    if (saturate(random(120, pixel_index).x + hitDat.flatness) <= F) {

        wi = normalize(reflect(-wo, wm));

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
            G1v *= sqrt(hitDat.surfaceColor);
        }
        else {
            if (Transmit(wm, wo, relativeIOR, wi) || hitDat.flatness == 1) {
                refracted = true;
            }
            else {
                wi = reflect(-wo, wm);
            }

        }

        wi = normalize(wi);

        float dotLH = abs(dot(wi, wm));
        float jacobian = dotLH / (pow(dotLH + relativeIOR * dotVH, 2));
        pdf = (1.0f - F) / jacobian;
    }

    if (CosTheta(wi) == 0.0f) {
        refracted = false;
        return -1;
    }

    // -- calculate VNDF pdf terms and apply Jacobian and Fresnel sampling adjustments
    GgxVndfAnisotropicPdf2(wi, wm, wo, tax, tay, forwardPdfW);
    forwardPdfW *= pdf;
    // -- convert wi back to world space

    return G1v;
}

static float3 SampleDisneyDiffuse(const MaterialData hitDat, float3 wo, bool thin, out float forwardPdfW, out float3 wi, inout bool refracted, uint pixel_index)
{
    wi = 0;
    forwardPdfW = 0;

    float sig = sign(CosTheta(wo));

    // -- Sample cosine lobe
    float2 r = random(121, pixel_index);
    wi = CosineSampleHemisphere2(r.x, r.y);
    float3 wm = normalize(wi + wo);

    float dotNL = CosTheta(wi);
    if (dotNL == 0.0f) {
        return -1;
    }

    float dotNV = CosTheta(wo);

    float pdf;

    float3 color = hitDat.surfaceColor;
    float3 extinction = 1;
    float p = random(122, pixel_index).x;
    if (p <= hitDat.diffTrans) {
        wi = -wi;
        pdf = hitDat.diffTrans;
        refracted = true;

        if (thin)
            color = sqrt(color);
        else {
            extinction = CalculateExtinction(hitDat.transmittanceColor, hitDat.scatterDistance);
        }
    }
    else {
        pdf = (1.0f - hitDat.diffTrans);
    }

    float3 sheen = EvaluateSheen(hitDat, wo, wm, wi);

    float diffuse = EvaluateDisneyDiffuse(hitDat, wo, wm, wi, thin);
    forwardPdfW = abs(dotNL) * pdf;
    return (sheen + color * (diffuse / pdf)) * extinction;
}

static float3 SampleDisneyBRDF(const MaterialData hitDat, float3 wo, out float forwardPdfW, out float3 wi, uint pixel_index)
{
    forwardPdfW = 0;
    wi = 0;

        // -- Calculate Anisotropic params
    float ax, ay;
    CalculateAnisotropicParams(hitDat.roughness, hitDat.anisotropic, ax, ay);

    // -- Sample visible distribution of normals
    float2 r = random(123, pixel_index);
    float3 wm;
    wi = SampleGgxVndfAnisotropic(wo, ay, ax, r.y, r.x, wm);

    if (CosTheta(wi) <= 0.0f) {
        return -1;//reflectance
    }

    // -- Fresnel term for this lobe is complicated since we're blending with both the metallic and the specularTint
    // -- parameters plus we must take the IOR into account for dielectrics
    float3 F = DisneyFresnel(hitDat, wo, wm, wi);

    // -- Since we're sampling the distribution of visible normals the pdf cancels out with a number of other terms.
    // -- We are left with the weight G2(wi, wo, wm) / G1(wi, wm) and since Disney uses a separable masking function
    // -- we get G1(wi, wm) * G1(wo, wm) / G1(wi, wm) = G1(wo, wm) as our weight.
    float G1v = SeparableSmithGGXG1(wo, wm, ay, ax);
    float3 specular = G1v * F;

    
    GgxVndfAnisotropicPdf(wi, wm, wo, ay, ax, forwardPdfW);

    forwardPdfW *= (1.0f / (4 * abs(dot(wo, wm))));

    return specular;
}

static float3 SampleDisneyClearcoat(const MaterialData hitDat, const float3 wo, out float forwardPdfW, out float3 wi, uint pixel_index)
{
    forwardPdfW = 0;
    wi = 0;
    float gloss = lerp(0.1f, 0.001f, hitDat.clearcoatGloss);

    float a = gloss;
    float a2 = a * a;

    float2 r = random(124, pixel_index);
    float cosTheta = sqrt(max(1e-6, (1.0f - pow(a2, 1.0f - r.x)) / (1.0f - a2)));
    float sinTheta = sqrt(max(1e-6, 1.0f - cosTheta * cosTheta));
    float phi = 2 * PI * r.y;

    float3 wm = float3(sinTheta * cos(phi), cosTheta, sinTheta * sin(phi));
    if (dot(wm, wo) < 0.0f) {
        wm = -wm;
    }

    wi = reflect(-wo, wm);

    float clearcoatWeight = hitDat.clearcoat;
    float clearcoatGloss = hitDat.clearcoatGloss;
    wm = normalize(wi + wo);

    float dotNH = CosTheta(wm);
    float dotLH = dot(wm, wi);

    float d = GTR1(abs(dotNH), lerp(0.1f, 0.001f, clearcoatGloss));
    float FH = SchlickWeight(dotLH);
    float f = lerp(0.04f, 1.0f, FH);//Schlick(0.04f, (dotLH));
    float g = SeparableSmithGGXG1(wi, 0.25f) * SeparableSmithGGXG1(wo, 0.25f);

    float fPdf = d / (4.0f * abs(dot(wo, wm)));

    forwardPdfW = fPdf;

    return (0.25f * clearcoatWeight * g * f * d) / fPdf;
}

//Sample End -----------------------------------------------------------------------------------------
//Reconstruct Start -----------------------------------------------------------------------------------------

inline float3 ReconstructDisneySpecTransmission(MaterialData hitDat, const float3 wo, float3 wm,
    const float3 wi, inout float fPdf, uint pixel_index)
{
    hitDat.surfaceColor /= PI;

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
    if (random(120, pixel_index).x <= F) {
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
    GgxVndfAnisotropicPdf2(wi, wm, wo, tax, tay, fPdf);
    fPdf *= pdf;

    return reflectance;
}

inline float3 ReconstructDisneyBRDF(MaterialData hitDat, const float3 wo, float3 wm,
    const float3 wi, inout float fPdf, inout bool Success)
{
    hitDat.surfaceColor /= PI;
    fPdf = 0.0f;

    float dotNL = CosTheta(wi);
    float dotNV = AbsCosTheta(wo);
    if (dotNL <= 0.0f) {
        Success = false;
        return 0;
    }

    float ax, ay;
    CalculateAnisotropicParams(hitDat.roughness, hitDat.anisotropic, ax, ay);

    float3 f = DisneyFresnel(hitDat, wo, wm, wi);

    GgxVndfAnisotropicPdf(wi, wm, wo, ay, ax, fPdf);
    fPdf *= 1.0f / (4 * abs(dot(wo, wm)));

    float G1v = SeparableSmithGGXG1(wo, wm, ay, ax);
    float3 specular = G1v * f;
    Success = (fPdf > 0);
    return specular;
}

inline float ReconstructDisneyClearcoat(float clearcoat, float alpha, const float3 wo, const float3 wm,
    const float3 wi, inout float fPdfW, out bool success)
{
    fPdfW = 0;
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
    success = fPdfW > 0;
    return 0.25f * clearcoat * Fr * Gr * Dr / fPdfW;// * PI;
}


//Reconstruct End -----------------------------------------------------------------------------------------



static float4 CalculateLobePdfs(const MaterialData hitDat) {
    float metallicBRDF = hitDat.metallic;
    float specularBSDF = (1.0f - hitDat.metallic) * hitDat.specTrans;
    float dielectricBRDF = (1.0f - hitDat.specTrans) * (1.0f - hitDat.metallic);
    
    float4 P = float4(metallicBRDF + hitDat.Specular, 1.0f * saturate(hitDat.clearcoat), dielectricBRDF, specularBSDF);

    float norm = 1.0f / (P.x + P.y + P.z + P.w);
    return P * norm;
}


float3 EvaluateDisney(MaterialData hitDat, float3 V, float3 L, bool thin,
    inout float forwardPdf, float3x3 TruTan, uint pixel_index)
{
    float3 wo = ToLocal(TruTan, V); // NDotL = L.z; NDotV = V.z; NDotH = H.z
    float3 wi = ToLocal(TruTan, L); // NDotL = L.z; NDotV = V.z; NDotH = H.z

    float3 wm = normalize(wo + wi);

    float dotNV = CosTheta(wo);
    float dotNL = CosTheta(wi);

    float3 reflectance = 0;
    forwardPdf = 0.0f;

    float4 P = CalculateLobePdfs(hitDat);

    float metallic = hitDat.metallic;
    float specTrans = hitDat.specTrans;

    float diffuseWeight = (1.0f - metallic) * (1.0f - specTrans);
    float transWeight = (1.0f - metallic) * specTrans;

    // -- Clearcoat
    bool upperHemisphere = dotNL > 0.0f && dotNV > 0.0f;
    if (upperHemisphere && hitDat.clearcoat > 0.0f) {

        float forwardClearcoatPdfW;

        float clearcoat = EvaluateDisneyClearcoat(hitDat.clearcoat, hitDat.clearcoatGloss, wo, wm, wi, forwardClearcoatPdfW);
        reflectance += clearcoat / abs(dotNL);
        forwardPdf += forwardClearcoatPdfW * P[1];
    }

    // -- Diffuse
    if (diffuseWeight > 0.0f) {
        float forwardDiffusePdfW = AbsCosTheta(wi);
        float diffuse = EvaluateDisneyDiffuse(hitDat, wo, wm, wi, thin);
        float3 sheen = EvaluateSheen(hitDat, wo, wm, wi);

        reflectance += (diffuse * hitDat.surfaceColor + sheen / PI);

        forwardPdf += forwardDiffusePdfW * P[2];
    }

    // -- specular
    if (P.x > 0) {
        float forwardMetallicPdfW;

        float3 specular = EvaluateDisneyBRDF(hitDat, wo, wm, wi, forwardMetallicPdfW, pixel_index);

        reflectance += specular;
        forwardPdf += forwardMetallicPdfW / (4 * abs(dot(wo, wm)));
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
        GgxVndfAnisotropicPdf2(wi, wm, wo, tax, tay, forwardTransmissivePdfW);

        float ni = wo.y > 0.0f ? 1.0f : hitDat.ior;
        float nt = wo.y > 0.0f ? hitDat.ior : 1.0f;

        float dotLH = dot(wm, wi);
        float dotVH = dot(wm, wo);
        forwardPdf += P[3] * forwardTransmissivePdfW / (pow(dotLH + (ni / nt) * dotVH, 2));
    }


    reflectance = reflectance * abs(dotNL);

    return reflectance;
}
float3 ReconstructDisney(MaterialData hitDat, float3 wo, float3 wi, bool thin,
    inout float forwardPdf, float3x3 TruTan, inout bool Success, uint pixel_index, int Case)
{

    wo = ToLocal(TruTan, wo); // NDotL = L.z; NDotV = V.z; NDotH = H.z
    wi = ToLocal(TruTan, wi); // NDotL = L.z; NDotV = V.z; NDotH = H.z
    hitDat.surfaceColor *= PI;
    float3 wm = normalize(wo + wi);

    float3 reflectance = 0;
    forwardPdf = 0.0f;

    float4 P = CalculateLobePdfs(hitDat);


    switch(Case) {
        case 0:
            if(P.x > 0) reflectance = ReconstructDisneyBRDF(hitDat, wo, wm, wi, forwardPdf, Success);
        break;
        case 1:
            if(P.y > 0) reflectance = ReconstructDisneyClearcoat(hitDat.clearcoat, hitDat.clearcoatGloss, wo, wm, wi, forwardPdf, Success);
        break;
        case 2:
            if(P.z > 0) { 
                reflectance = (EvaluateDisneyDiffuse(hitDat, wo, wm, wi, thin) * hitDat.surfaceColor + EvaluateSheen(hitDat, wo, wm, wi));
                forwardPdf = AbsCosTheta(wi);
                Success = forwardPdf > 0;
            }
        break;
        case 3:
        break;
    }

    if(P.w > 0) {
        float forwardSpecPdfW;
        float3 SpecCol = ReconstructDisneySpecTransmission(hitDat, wo, wm, wi,
            forwardSpecPdfW, pixel_index);
            reflectance += SpecCol * (Case == 3 ? rcp(P.w) : 1.0f);
            if(Case == 3) forwardPdf += forwardSpecPdfW * P.w;
        if(forwardSpecPdfW > 0) {
            Success = Success || true;
        }
    } else {
        reflectance = saturate(reflectance / P[Case]);
        forwardPdf *= P[Case];
    }

    return reflectance;
}
bool SampleDisney(MaterialData hitDat, inout float3 v, bool thin, out float PDF, inout float3 throughput, float3 norm, out int Case, uint pixel_index, out bool Refracted, bool GotFlipped)
{
    float3x3 TruTanMat = GetTangentSpace(norm);
    float4 P = CalculateLobePdfs(hitDat);//pSpecular, pClearcoat, pDiffuse, pTransmission
    v = ToLocal(TruTanMat, -v);
    Refracted = false;
    float3 Reflection = 0;
    PDF = 0;
    float3 wi;
    float p = random(125, pixel_index).x;

    if(p <= P.x) {
        Case = 0;
    } else if(p <= P.x + P.y) {
        Case = 1;
    } else if(p <= P.x + P.y + P.z) {
        Case = 2;
    } else Case = 3;
    switch(Case) {
        case 0:
            Reflection = SampleDisneyBRDF(hitDat, v, PDF, wi, pixel_index);            
        break;
        case 1:
            Reflection = SampleDisneyClearcoat(hitDat, v, PDF, wi, pixel_index);
        break;
        case 2:
            hitDat.surfaceColor *= PI;
            Reflection = SampleDisneyDiffuse(hitDat, v, thin, PDF, wi, Refracted, pixel_index);
        break;
        case 3:
            Reflection = SampleDisneySpecTransmission(hitDat, v, thin, PDF, wi, Refracted, pixel_index, GotFlipped);
        break;
    }

    v = normalize(ToWorld(TruTanMat, wi));
    throughput *= (Reflection / P[Case]);
    PDF *= P[Case];

    return Reflection.x != -1;
}


inline bool EvaluateBsdf(const MaterialData hitDat, float3 DirectionIn, float3 DirectionOut, float3 Normal, out float PDF, out float3 bsdf_value, uint pixel_index) {
    bool validbsdf = false;
    float cos_theta_hit = dot(DirectionOut, Normal);
    [branch] switch (hitDat.MatType) {//Switch between different materials
        case CutoutIndex:
        case DisneyIndex:
            bsdf_value = EvaluateDisney(hitDat, -DirectionIn, DirectionOut, hitDat.Thin == 1, PDF, GetTangentSpace(Normal), pixel_index);// DisneyEval(mat, -PrevDirection, norm, to_light, bsdf_pdf, hitDat);
            validbsdf = PDF > 0;
        break;
        case VolumetricIndex:
            validbsdf = evaldiffuse(DirectionOut, cos_theta_hit, bsdf_value, PDF);
            bsdf_value *= hitDat.surfaceColor;
        break;
        default:
            validbsdf = evaldiffuse(DirectionOut, cos_theta_hit, bsdf_value, PDF);
            bsdf_value *= hitDat.surfaceColor;
        break;
    }
    return validbsdf;
}

inline bool ReconstructBsdf(const MaterialData hitDat, float3 DirectionIn, float3 DirectionOut, float3 Normal, out float PDF, out float3 bsdf_value, const float3x3 TangentSpaceNorm, uint pixel_index, uint Case) {
    bool validbsdf = false;
    bsdf_value = 0;
    PDF = 0;
    [call] switch (hitDat.MatType) {//Switch between different materials
        case CutoutIndex:
        case DisneyIndex:
            bsdf_value = ReconstructDisney(hitDat, -DirectionIn, DirectionOut, hitDat.Thin == 1, PDF, TangentSpaceNorm, validbsdf, pixel_index, Case);
        break;
        case VolumetricIndex:
            validbsdf = true;
        break;
        default:
            validbsdf = evaldiffuse(DirectionOut, dot(DirectionOut, Normal), bsdf_value, PDF);
            bsdf_value = hitDat.surfaceColor;
        break;
    }
    return validbsdf;
}