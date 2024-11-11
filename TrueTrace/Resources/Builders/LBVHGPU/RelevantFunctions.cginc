float3 i_octahedral_32( uint data ) {
    uint2 iv = uint2( data, data>>16u ) & 65535u; 
    float2 v = iv/32767.5f - 1.0f;
    float3 nor = float3(v, 1.0f - abs(v.x) - abs(v.y)); // Rune Stubbe's version,
    float t = max(-nor.z,0.0);                     // much faster than original
    nor.xy += (nor.xy>=0.0)?-t:t;                     // implementation of this
    return normalize( nor );
}

uint octahedral_32(float3 nor) {
    float oct = 1.0f / (abs(nor.x) + abs(nor.y) + abs(nor.z));
    float t = saturate(-nor.z);
    nor.xy = (nor.xy + (nor.xy > 0.0f ? t : -t)) * oct;
    uint2 d = uint2(round(32767.5 + nor.xy*32767.5));  
    return d.x|(d.y<<16u);
}



inline float surface_area(AABB aabb) {
    float3 sizes = aabb.BBMax - aabb.BBMin;
    return 2.0f * ((sizes.x * sizes.y) + (sizes.x * sizes.z) + (sizes.y * sizes.z)); 
}


inline float luminance(const float3 a) {
    return dot(float3(0.299f, 0.587f, 0.114f), a);
}



float AngleBetween(float3 v1, float3 v2) {
    if(dot(v1, v2) < 0) return 3.14159f - 2.0f * asin(length(v1 + v2) / 2.0f);
    else return 2.0f * asin(length(v2 - v1) / 2.0f);
}


float4x4 Rotate(float sinTheta, float cosTheta, float3 axis) {
    float3 a = normalize(axis);
    float4x4 m;
    m[0][0] = a.x * a.x + (1 - a.x * a.x) * cosTheta;
    m[0][1] = a.x * a.y * (1 - cosTheta) - a.z * sinTheta;
    m[0][2] = a.x * a.z * (1 - cosTheta) + a.y * sinTheta;
    m[0][3] = 0;

    m[1][0] = a.x * a.y * (1 - cosTheta) + a.z * sinTheta;
    m[1][1] = a.y * a.y + (1 - a.y * a.y) * cosTheta;
    m[1][2] = a.y * a.z * (1 - cosTheta) - a.x * sinTheta;
    m[1][3] = 0;

    m[2][0] = a.x * a.z * (1 - cosTheta) - a.y * sinTheta;
    m[2][1] = a.y * a.z * (1 - cosTheta) + a.x * sinTheta;
    m[2][2] = a.z * a.z + (1 - a.z * a.z) * cosTheta;
    m[2][3] = 0;

    m[3][0] = 0;
    m[3][1] = 0;
    m[3][2] = 0;
    m[3][3] = 1;

return mul(m, transpose(m));
}

float4x4 Rotate(float Theta, float3 axis) {
    return Rotate(sin(Theta), cos(Theta), axis);
}
float GetCosThetaO(uint cosTheta) {
    return (2.0f * ((float)(cosTheta & 0x0000FFFF) / 32767.0f) - 1.0f);
}
float GetCosThetaE(uint cosTheta) {
    return (2.0f * ((float)(cosTheta >> 16) / 32767.0f) - 1.0f);
}
uint CompCosTheta(float cosTheta_o, float cosTheta_e) {
    return (uint)floor(32767.0f * ((cosTheta_o + 1.0f) / 2.0f)) | ((uint)floor(32767.0f * ((cosTheta_e + 1.0f) / 2.0f)) << 16);
}

inline float4 DoCone(float4 A, float4 B) {
    if(all(A.xyz == 0)) return B;
    if(all(B.xyz == 0)) return A;
    
    float theta_a = acos(A.w);
    float theta_b = acos(B.w);
    float theta_d = AngleBetween(A.xyz, B.xyz);
    if(min(theta_d + theta_b, 3.14159f) <= theta_a) return A;
    if(min(theta_d + theta_a, 3.14159f) <= theta_b) return B;

    float theta_o = (theta_a + theta_d + theta_b) / 2.0f;
    if(theta_o >= 3.14159f) return float4(0,0,0,-1);

    float theta_r = theta_o - theta_a;
    float3 wr = cross(A.xyz, B.xyz);
    if(dot(wr, wr) == 0) return float4(0,0,0,-1);
    float3 w = mul(Rotate(theta_r, wr), float4(A.xyz,0)).xyz;
    return float4(w, cos(theta_o));
}


inline LightBVHData Union(const LightBVHData A, const LightBVHData B, int Left) {
    float4 Cone = DoCone(float4(i_octahedral_32(A.w), GetCosThetaO(A.cosTheta_oe)), float4(i_octahedral_32(B.w), GetCosThetaO(B.cosTheta_oe)));
    float cosTheta_o = Cone.w;
    float cosTheta_e = min(GetCosThetaE(A.cosTheta_oe), GetCosThetaE(B.cosTheta_oe));
    LightBVHData Dat = {max(A.BBMax, B.BBMax), min(A.BBMin, B.BBMin), octahedral_32(Cone.xyz), A.phi + B.phi, CompCosTheta(cosTheta_o, cosTheta_e), Left};
    return Dat;
}

float EvaluateCost(LightBounds b, float Kr, int dim) {
    float theta_o = (float)acos(b.cosTheta_o);
    float theta_e = (float)acos(b.cosTheta_e);
    float theta_w = min(theta_o + theta_e, 3.14159f);
    float sinTheta_o = sqrt(1.0f - b.cosTheta_o * b.cosTheta_o);
    float M_omega = 2.0f * 3.14159f * (1.0f - b.cosTheta_o) +
                    3.14159f / 2.0f *
                        (2.0f * theta_w * sinTheta_o -(float)cos(theta_o - 2.0f * theta_w) -
                         2.0f * theta_o * sinTheta_o + b.cosTheta_o);

    float Radius = distance((b.b.BBMax + b.b.BBMin) / 2.0f, b.b.BBMax);
    float SA = 4.0f * 3.14159f * Radius * Radius;

    return b.phi * M_omega * Kr * surface_area(b.b) / (float)max(b.LightCount, 1);
}

