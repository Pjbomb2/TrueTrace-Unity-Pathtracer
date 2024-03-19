// #include "UnityCG.cginc"

#define ONE_OVER_PI 0.318309886548f
#define PI 3.14159265f
#define EPSILON 1e-8


float4x4 CamToWorld;
float4x4 CamInvProj;

float4x4 ViewMatrix;
int MaxBounce;
int CurBounce;

uint screen_width;
uint screen_height;
uint TargetWidth;
uint TargetHeight;
int frames_accumulated;
int curframe;//might be able to get rid of this


bool UseLightBVH;
bool DoHeightmap;
bool UseReCur;
bool DiffRes;
bool UseRussianRoulette;
bool UseNEE;
bool DoPartialRendering;
int PartialRenderingFactor;

int unitylightcount;

//Cam Info
float3 Forward;
float3 Right;
float3 Up;
float3 CamPos;

//DoF
bool UseDoF;
float focal_distance;
float AperatureRadius;


RWStructuredBuffer<uint3> BufferData;

RWTexture2D<half2> CorrectedDepthTex;

#ifdef HardwareRT
	StructuredBuffer<int> SubMeshOffsets;
	StructuredBuffer<float2> MeshOffsets;
#endif


struct BufferSizeData {
	int tracerays;
	int shadow_rays;
	int heighmap_rays;
	int heightmap_shadow_rays;
};

globallycoherent RWStructuredBuffer<BufferSizeData> BufferSizes;


struct CudaTriangle {
	float3 pos0;
	float3 posedge1;
	float3 posedge2;

	uint3 norms;

	uint3 tans;

	float2 tex0;
	float2 texedge1;
	float2 texedge2;

	uint MatDat;
};

StructuredBuffer<CudaTriangle> AggTris;

struct Ray {
	float3 origin;
	float3 direction;
	float3 direction_inv;
};

struct SmallerRay {
	float3 origin;
	float3 direction;
};


struct RayHit {
	float t;
	float u, v;
	int mesh_id;
	int triangle_id;
};

struct RayData {//128 bit aligned
	float3 origin;
	uint direction;
	uint PixelIndex;//need to bump this back down to uint1
	float last_pdf;
	uint4 hits;
};

RWStructuredBuffer<RayData> GlobalRays;

RWTexture2D<float4> ScreenSpaceInfo;
Texture2D<float4> ScreenSpaceInfoRead;
Texture2D<float4> PrevScreenSpaceInfo;

bool DoExposure;
StructuredBuffer<float> Exposure;

struct ShadowRayData {
	float3 origin;
	uint direction;
	float3 illumination;
	float LuminanceIncomming;
	float t;
	uint PixelIndex;
};
RWStructuredBuffer<ShadowRayData> ShadowRaysBuffer;


struct ColData {
	float3 throughput;
	float3 Direct;
	float3 Indirect;
	uint PrimaryNEERay;
	int IsSpecular;
	float pad;
};

RWStructuredBuffer<ColData> GlobalColors;
StructuredBuffer<ColData> PrevGlobalColorsA;
RWStructuredBuffer<ColData> PrevGlobalColorsB;



RWTexture2D<half4> ReservoirA;
Texture2D<half4> ReservoirB;

RWTexture2D<uint4> WorldPosA;
Texture2D<uint4> WorldPosB;
RWTexture2D<uint4> WorldPosC;

RWTexture2D<half4> NEEPosA;
Texture2D<half4> NEEPosB;

RWTexture2D<float4> Result;

RWStructuredBuffer<SmallerRay> Rays;
StructuredBuffer<SmallerRay> Rays2;

Texture2D<uint4> PrimaryTriData;
StructuredBuffer<int> TLASBVH8Indices;

struct MyMeshDataCompacted {
	float4x4 W2L;
	int TriOffset;
	int NodeOffset;
	int MaterialOffset;
	int mesh_data_bvh_offsets;//could I convert this an int4?
	int LightTriCount;
	int LightNodeOffset;
};

StructuredBuffer<MyMeshDataCompacted> _MeshData;


struct BVHNode8Data {
	float3 node_0xyz;
	uint node_0w;
	uint4 node_1;
	uint4 node_2;
	uint4 node_3;
	uint4 node_4;
};

StructuredBuffer<BVHNode8Data> cwbvh_nodes;


struct TrianglePos {
	float3 pos0;
	float3 posedge1;
	float3 posedge2;
};
struct TriangleUvs {
	float2 pos0;
	float2 posedge1;
	float2 posedge2;
};


inline TrianglePos triangle_get_positions(const int ID) {
	TrianglePos tri;
	tri.pos0 = AggTris.Load(ID).pos0;
	tri.posedge1 = AggTris.Load(ID).posedge1;
	tri.posedge2 = AggTris.Load(ID).posedge2;
	return tri;
}

inline TriangleUvs triangle_get_positions2(const int ID) {
	TriangleUvs tri;
	tri.pos0 = AggTris.Load(ID).tex0;
	tri.posedge1 = AggTris.Load(ID).texedge1;
	tri.posedge2 = AggTris.Load(ID).texedge2;
	return tri;
}

struct MaterialData {//56
	float4 AlbedoTex;//16
	float4 NormalTex;//32
	float4 EmissiveTex;//48
	float4 MetallicTex;//64
	float4 RoughnessTex;//80
	float3 surfaceColor;
	float emmissive;
	float3 EmissionColor;
	uint Tag;
	float roughness;
	int MatType;//Can pack into tag
	float3 transmittanceColor;
	float ior;
	float metallic;
	float sheen;
	float sheenTint;
	float specularTint;
	float clearcoat;
	float clearcoatGloss;
	float anisotropic;
	float flatness;
	float diffTrans;
	float specTrans;
	float Specular;
	float scatterDistance;
	float4 AlbedoTexScale;
	float2 MetallicRemap;
	float2 RoughnessRemap;
	float AlphaCutoff;
	float NormalStrength;
	float Hue;
	float Saturation;
	float Contrast;
	float Brightness;
	float3 BlendColor;
	float BlendFactor;
	float2 SecondaryTexScale;
	float Rotation;
};

StructuredBuffer<MaterialData> _Materials;

SamplerState my_linear_clamp_sampler;
SamplerState sampler_trilinear_clamp;
SamplerState my_point_clamp_sampler;

Texture2D<half> MetallicTex;
Texture2D<half> RoughnessTex;
RWTexture2D<half4> TempAlbedoTex;
RWTexture2D<float4> RandomNumsWrite;
Texture2D<float4> RandomNums;
RWTexture2D<half4> _DebugTex;

Texture2D<float4> VideoTex;
SamplerState sampler_VideoTex;
Texture2D<half4> _TextureAtlas;
SamplerState sampler_TextureAtlas;
Texture2D<float2> _NormalAtlas;
SamplerState sampler_NormalAtlas;
Texture2D<half4> _EmissiveAtlas;

Texture2D<half> Heightmap;

struct TerrainData {
    float3 PositionOffset;
    float HeightScale;
    float TerrainDim;
    float4 AlphaMap;
    float4 HeightMap;
    int MatOffset;
};

StructuredBuffer<TerrainData> Terrains;

Texture2D<float4> TerrainAlphaMap;
SamplerState sampler_TerrainAlphaMap;
int MaterialCount;


int TerrainCount;
bool TerrainExists;

float3x3 GetTangentSpace(float3 normal) {
    // Choose a helper floattor for the cross product
    float3 helper = float3(1, 0, 0);
    if (abs(normal.x) > 0.99f)
        helper = float3(0, 0, 1);

    // Generate floattors
    float3 tangent = normalize(cross(normal, helper));
    float3 binormal = cross(normal, tangent);

    return float3x3(tangent, normal, binormal);
}

float3x3 GetTangentSpace2(float3 normal) {

    float3 tangent = normalize(cross(normal, float3(0, 1, 0)));
    float3 binormal = cross(normal, tangent);

    return float3x3(tangent, normal, binormal);
}

float FarPlane;

Ray CreateRay(float3 origin, float3 direction) {
	Ray ray;
	ray.origin = origin;
	ray.direction = direction;
	ray.direction_inv = rcp(direction);
	return ray;
}

RayHit CreateRayHit() {
	RayHit hit;
	hit.t = FarPlane;
	hit.u = 0;
	hit.v = 0;
	hit.mesh_id = 0;
	hit.triangle_id = -1;
	return hit;
}

uint hash_with(uint seed, uint hash) {
	// Wang hash
	seed = (seed ^ 61) ^ hash;
	seed += seed << 3;
	seed ^= seed >> 4;
	seed *= 0x27d4eb2d;
	return seed;
}
uint pcg_hash(uint seed) {
	uint state = seed * 747796405u + 2891336453u;
	uint word = ((state >> ((state >> 28u) + 4u)) ^ state) * 277803737u;
	return (word >> 22u) ^ word;
}

bool UseASVGF;
int ReSTIRGIUpdateRate;
bool UseReSTIRGI;

float2 randomNEE(uint samdim, uint pixel_index) {
	uint hash = pcg_hash((pixel_index * (uint)258 + samdim) * (MaxBounce + 1) + CurBounce);

	static const float one_over_max_unsigned = asfloat(0x2f7fffff);


	float x = hash_with(frames_accumulated, hash) * one_over_max_unsigned;
	float y = hash_with(frames_accumulated + 0xdeadbeef, hash) * one_over_max_unsigned;

	return float2(x, y);
}

uint randomNEE2(uint samdim, uint pixel_index) {
	uint hash = pcg_hash((pixel_index * (uint)258 + samdim) * (MaxBounce + 1) + CurBounce);


	return hash_with(frames_accumulated, hash);
}



float2 random(uint samdim, uint pixel_index) {
	[branch] if (UseASVGF || ReSTIRGIUpdateRate != 0) {
		uint2 pixid = uint2(pixel_index % screen_width, pixel_index / screen_width);
		uint hash = pcg_hash(((uint)RandomNums[pixid].y * (uint)258 + samdim) * (MaxBounce + 1) + CurBounce);

		const static float one_over_max_unsigned = asfloat(0x2f7fffff);


		float x = hash_with((uint)RandomNums[pixid].x, hash) * one_over_max_unsigned;
		float y = hash_with((uint)RandomNums[pixid].x + 0xdeadbeef, hash) * one_over_max_unsigned;

		return float2(x, y);
	}
	else {
		uint hash = pcg_hash((pixel_index * (uint)258 + samdim) * (MaxBounce + 1) + CurBounce);

		const static float one_over_max_unsigned = asfloat(0x2f7fffff);


		float x = hash_with(frames_accumulated, hash) * one_over_max_unsigned;
		float y = hash_with(frames_accumulated + 0xdeadbeef, hash) * one_over_max_unsigned;

		return float2(x, y);
	}
}

void set(int index, const RayHit ray_hit) {
	uint uv = (uint)(ray_hit.u * 65535.0f) | ((uint)(ray_hit.v * 65535.0f) << 16);

	GlobalRays[index].hits = uint4(ray_hit.mesh_id, ray_hit.triangle_id, asuint(ray_hit.t), uv);
}
uint4 set2(const RayHit ray_hit) {
	uint uv = (uint)(ray_hit.u * 65535.0f) | ((uint)(ray_hit.v * 65535.0f) << 16);

	return uint4(ray_hit.mesh_id, ray_hit.triangle_id, asuint(ray_hit.t), uv);
}

RayHit get(int index) {
	const uint4 hit = GlobalRays[index].hits;

	RayHit ray_hit;

	ray_hit.mesh_id = hit.x;
	ray_hit.triangle_id = hit.y;

	ray_hit.t = asfloat(hit.z);

	ray_hit.u = (hit.w & 0xffff) / 65535.0f;
	ray_hit.v = (hit.w >> 16) / 65535.0f;

	return ray_hit;
}
RayHit get2(const uint4 hit) {

	RayHit ray_hit;

	ray_hit.mesh_id = hit.x;
	ray_hit.triangle_id = hit.y;

	ray_hit.t = asfloat(hit.z);

	ray_hit.u = (hit.w & 0xffff) / 65535.0f;
	ray_hit.v = (hit.w >> 16) / 65535.0f;

	return ray_hit;
}

int Pack2To1(int A, int B) {
    return A | (B << 16);
}

int2 Unpack1To2(int A) {
    return int2(A >> 16, A & 0x0000FFFF);
}

uint packRGBE(float3 v)
{
	float3 va = max(0, v);
	float max_abs = max(va.r, max(va.g, va.b));
	if (max_abs == 0) return 0;

	float exponent = floor(log2(max_abs));

	uint result = uint(clamp(exponent + 20, 0, 31)) << 27;

	float scale = pow(2, -exponent) * 256.0;
	uint3 vu = min(511, round(va * scale));
	result |= vu.r;
	result |= vu.g << 9;
	result |= vu.b << 18;

	return result;
}

float3 unpackRGBE(uint x)
{
    int exponent = int(x >> 27) - 20;
    float scale = pow(2, exponent) / 256.0;

    float3 v;
    v.r = float(x & 0x1ff) * scale;
    v.g = float((x >> 9) & 0x1ff) * scale;
    v.b = float((x >> 18) & 0x1ff) * scale;

    return v;
}

SmallerRay CreateCameraRay(float2 uv, uint pixel_index) {
	// Transform the camera origin to world space
	float3 origin = mul(CamToWorld, float4(0.0f, 0.0f, 0.0f, 1.0f)).xyz;

	// Invert the perspective projection of the view-space position
	float3 direction = mul(CamInvProj, float4(uv, 0.0f, 1.0f)).xyz;
	// Transform the direction from camera to world space and normalize
	direction = mul(CamToWorld, float4(direction, 0.0f)).xyz;
	direction = normalize(direction);
	[branch] if (UseDoF) {
		float3 cameraForward = mul(CamInvProj, float4(0, 0, 0.0f, 1.0f)).xyz;
		// Transform the direction from camera to world space and normalize
		float4 sensorPlane;
		sensorPlane.xyz = cameraForward;
		sensorPlane.w = -dot(cameraForward, (origin - cameraForward));

		float t = -(dot(origin, sensorPlane.xyz) + sensorPlane.w) / dot(direction, sensorPlane.xyz);
		float3 sensorPos = origin + direction * t;

		float3 cameraSpaceSensorPos = mul(ViewMatrix, float4(sensorPos, 1.0f)).xyz;

		// elongate z by the focal length
		cameraSpaceSensorPos.z *= focal_distance;

		// convert back into world space
		sensorPos = mul(CamToWorld, float4(cameraSpaceSensorPos, 1.0f)).xyz;

		float angle = random(9, pixel_index).x * 2.0f * PI;
		float radius = sqrt(random(9, pixel_index).y);
		float2 offset = float2(cos(angle), sin(angle)) * radius * AperatureRadius;

		float3 p = origin + direction * (focal_distance);

		float3 aperturePos = origin + Right * offset.x + Up * offset.y;

		origin = aperturePos;
		direction = normalize(p - origin);
	}

	SmallerRay smolray;
	smolray.origin = origin;
	smolray.direction = direction;
	return smolray;
}

inline Ray CreateCameraRay(float2 uv) {
    // Transform the camera origin to world space
    float3 origin = mul(CamToWorld, float4(0.0f, 0.0f, 0.0f, 1.0f)).xyz;

    // Invert the perspective projection of the view-space position
    float3 direction = mul(CamInvProj, float4(uv, 0.0f, 1.0f)).xyz;
    // Transform the direction from camera to world space and normalize
    direction = mul(CamToWorld, float4(direction, 0.0f)).xyz;
    direction = normalize(direction);

    return CreateRay(origin, direction);
}

inline float2 AlignUV(float2 BaseUV, const float4 TexScale, const float4 TexDim, const float Rotation = 0, const bool IsAlbedo = false) {
	if(TexDim.x <= 0) return -1;
	BaseUV = BaseUV * TexScale.xy + TexScale.zw;
	BaseUV = (BaseUV < 0 ? (1.0f - fmod(abs(BaseUV), 1.0f)) : fmod(abs(BaseUV), 1.0f));
	if(IsAlbedo) BaseUV = clamp(BaseUV, 8.0f/16384.0f, 1.0f);
	if(Rotation != 0) {
		float sinc, cosc;
		sincos(Rotation, sinc, cosc);
		BaseUV -= 0.5f;
		float2 tempuv = BaseUV;
		BaseUV.x = tempuv.x * cosc - tempuv.y * sinc;
		BaseUV.y = tempuv.x * sinc + tempuv.y * cosc;
		BaseUV += 0.5f;
		BaseUV = fmod(abs(BaseUV), 1.0f);
	}
	return BaseUV * (TexDim.xy - TexDim.zw) + TexDim.zw;
}

inline uint ray_get_octant_inv4(const float3 ray_direction) {
    return
        (ray_direction.x < 0.0f ? 0 : 0x04040404) |
        (ray_direction.y < 0.0f ? 0 : 0x02020202) |
        (ray_direction.z < 0.0f ? 0 : 0x01010101);
}
inline bool triangle_intersect_shadow(int tri_id, const Ray ray, float max_distance, int mesh_id, inout float3 throughput, const int MatOffset) {
    TrianglePos tri = triangle_get_positions(tri_id);
  	TriangleUvs tri2 = triangle_get_positions2(tri_id);
    int MaterialIndex = (MatOffset + AggTris[tri_id].MatDat);

    float3 h = cross(ray.direction, tri.posedge2);
    float  a = dot(tri.posedge1, h);

    float  f = rcp(a);
    float3 s = ray.origin - tri.pos0;
    float  u = f * dot(s, h);

    if (u >= 0.0f && u <= 1.0f) {
        float3 q = cross(s, tri.posedge1);
        float  v = f * dot(ray.direction, q);

        if (v >= 0.0f && u + v <= 1.0f) {
            float t = f * dot(tri.posedge2, q);
            #ifdef AdvancedAlphaMapped
        		if(_Materials[MaterialIndex].MatType == DiffuseIndex) return false;
        		if(_Materials[MaterialIndex].MatType == CutoutIndex || _Materials[MaterialIndex].specTrans == 1) {
	                float2 BaseUv = tri2.pos0 * (1.0f - u - v) + tri2.posedge1 * u + tri2.posedge2 * v;
	                float2 Uv = AlignUV(BaseUv, _Materials[MaterialIndex].AlbedoTexScale, _Materials[MaterialIndex].AlbedoTex);
	                float4 BaseCol = _TextureAtlas.SampleLevel(my_point_clamp_sampler, Uv, 0);
	                if(_Materials[MaterialIndex].MatType == CutoutIndex && BaseCol.w < _Materials[MaterialIndex].AlphaCutoff) return false;

		            #ifdef IgnoreGlassShadow
		                if(_Materials[MaterialIndex].specTrans == 1) {
			            	#ifdef StainedGlassShadows
		    	            	throughput *= _Materials[MaterialIndex].surfaceColor * (BaseCol.xyz + 2.0f) / 3.0f;
		    				#endif
		                	return false;
		                }
		            #endif
		        }
            #endif
            if (t > 0.0f && t < max_distance) return true;
        }
    }

    return false;
}

inline uint cwbvh_node_intersect(const Ray ray, int oct_inv4, float max_distance, const float3 node_0, uint node_0w, const uint4 node_1, const uint4 node_2, const uint4 node_3, const uint4 node_4) {
    uint e_x = (node_0w) & 0xff;
    uint e_y = (node_0w >> (8)) & 0xff;
    uint e_z = (node_0w >> (16)) & 0xff;

    const float3 adjusted_ray_direction_inv = float3(
        asfloat(e_x << 23) * ray.direction_inv.x,
        asfloat(e_y << 23) * ray.direction_inv.y,
        asfloat(e_z << 23) * ray.direction_inv.z
        );
    const float3 adjusted_ray_origin = ray.direction_inv * (node_0 - ray.origin);
            
    uint hit_mask = 0;
    float3 tmin3;
    float3 tmax3;
    uint child_bits;
    uint bit_index;
    [unroll]
    for(int i = 0; i < 2; i++) {
        uint meta4 = (i == 0 ? node_1.z : node_1.w);

        uint is_inner4   = (meta4 & (meta4 << 1)) & 0x10101010;
        uint inner_mask4 = (((is_inner4 << 3) >> 7) & 0x01010101) * 0xff;
        uint bit_index4  = (meta4 ^ (oct_inv4 & inner_mask4)) & 0x1f1f1f1f;
        uint child_bits4 = (meta4 >> 5) & 0x07070707;

        uint q_lo_x = (i == 0 ? node_2.x : node_2.y);
        uint q_hi_x = (i == 0 ? node_2.z : node_2.w);

        uint q_lo_y = (i == 0 ? node_3.x : node_3.y);
        uint q_hi_y = (i == 0 ? node_3.z : node_3.w);

        uint q_lo_z = (i == 0 ? node_4.x : node_4.y);
        uint q_hi_z = (i == 0 ? node_4.z : node_4.w);

        uint x_min = ray.direction.x < 0.0f ? q_hi_x : q_lo_x;
        uint x_max = ray.direction.x < 0.0f ? q_lo_x : q_hi_x;

        uint y_min = ray.direction.y < 0.0f ? q_hi_y : q_lo_y;
        uint y_max = ray.direction.y < 0.0f ? q_lo_y : q_hi_y;

        uint z_min = ray.direction.z < 0.0f ? q_hi_z : q_lo_z;
        uint z_max = ray.direction.z < 0.0f ? q_lo_z : q_hi_z;
        [unroll]
        for(int j = 0; j < 4; j++) {

            tmin3 = float3((float)((x_min >> (j * 8)) & 0xff), (float)((y_min >> (j * 8)) & 0xff), (float)((z_min >> (j * 8)) & 0xff));
            tmax3 = float3((float)((x_max >> (j * 8)) & 0xff), (float)((y_max >> (j * 8)) & 0xff), (float)((z_max >> (j * 8)) & 0xff));

            tmin3 = mad(tmin3, adjusted_ray_direction_inv, adjusted_ray_origin);
            tmax3 = mad(tmax3, adjusted_ray_direction_inv, adjusted_ray_origin);

            float tmin = max(max(tmin3.x, tmin3.y), max(tmin3.z, EPSILON));
            float tmax = min(min(tmax3.x, tmax3.y), min(tmax3.z, max_distance));
            
            bool intersected = tmin < tmax;
            [branch]
            if (intersected) {
                child_bits = (child_bits4 >> (j * 8)) & 0xff;
                bit_index  = (bit_index4 >> (j * 8)) & 0xff;

                hit_mask |= child_bits << bit_index;
            }
        }
    }
    return hit_mask;
}

float2 sample_triangle(float u1, float u2) {
	if (u2 > u1) {
		u1 *= 0.5f;
		u2 -= u1;
	} else {
		u2 *= 0.5f;
		u1 -= u2;
	}
	return float2(u1, u2);
}

struct LightTriData {
	float3 pos0;
	float3 posedge1;
	float3 posedge2;
	uint TriTarget;
};

StructuredBuffer<LightTriData> LightTriangles;

int LightMeshCount;

struct LightMeshData {//remove 74 bytes
	float3 Center;
	int StartIndex;
	int IndexEnd;
	int MatOffset;
	int LockedMeshIndex;
};
StructuredBuffer<LightMeshData> _LightMeshes;

struct LightData {
	float3 Radiance;
	float3 Position;
	float3 Direction;
	int Type;
	float2 SpotAngle;
	float ZAxisRotation;
	float Softness;
};
StructuredBuffer<LightData> _UnityLights;

inline float power_heuristic(float pdf_f, float pdf_g) {
    return (pdf_f * pdf_f) / (pdf_f * pdf_f + pdf_g * pdf_g); // Power of 2 hardcoded, best empirical results according to Veach
}

uint octahedral_32(float3 nor) {
	float2 Signs = (nor.xy>=0.0) ? 1.0 : -1.0;
    nor.xy /= ( nor.x * Signs.x + nor.y * Signs.y + abs( nor.z ) );
    nor.xy  = (nor.z >= 0.0) ? nor.xy : (1.0-(nor.yx * Signs.yx)) * Signs.xy;
    uint2 d = uint2(round(32767.5 + nor.xy*32767.5));  
    return d.x|(d.y<<16u);
}

float3 i_octahedral_32( uint data ) {
    uint2 iv = uint2( data, data>>16u ) & 65535u; 
    float2 v = float2(iv)/32767.5f - 1.0f;
    float3 nor = float3(v, 1.0f - abs(v.x) - abs(v.y)); // Rune Stubbe's version,
    float t = max(-nor.z,0.0);                     // much faster than original
    nor.x += (nor.x>0.0)?-t:t;                     // implementation of this
    nor.y += (nor.y>0.0)?-t:t;                     // technique
    return normalize( nor );
}

int RISCount;



int SelectLightMesh(uint pixel_index) {//Select mesh to sample light from
	if (LightMeshCount == 1) return 0;
	const float2 rand_mesh = random(10, pixel_index);
	return clamp((rand_mesh.y * LightMeshCount), 0, LightMeshCount - 1);
}

int SelectLightMeshSmart(uint pixel_index, inout float MeshWeight, float3 Pos) {//Maybe add an "extents" to the lightmeshes to get an even better estimate?

 	int MinIndex = 0;
    float wsum = 0;
    int M = 0;
    float MinP_Hat = 0;
    int FinalMesh = 0;
    float p_hat;
    float2 Rand;
    const int RISFinal = RISCount + 1;
    for(int i = 0; i < RISFinal; i++) {
        Rand = random(i + 11, pixel_index);
        int Index = clamp((Rand.x * LightMeshCount), 0, LightMeshCount - 1);
        p_hat = 1.0f / dot(Pos - _LightMeshes[Index].Center, Pos - _LightMeshes[Index].Center);
        wsum += p_hat;
        M++;
        if(Rand.y < p_hat / wsum) {
            MinIndex = Index;
            MinP_Hat = p_hat;
        }
    }
    MeshWeight *= (wsum / max((RISFinal) * MinP_Hat, 0.000001f));
    return MinIndex;
}


float3 ToWorld(float3x3 X, float3 V) {
	return normalize(mul(V, X));//V.x * + StartIndex X + V.y * StartIndex + IndexEnd.z * Z;
}

float3 ToLocal(float3x3 X, float3 V) {
	return normalize(mul(X, V));
}



static uint ScatteringTexRSize = 32;
static uint ScatteringTexMUSize = 128;
static uint ScatteringTexMUSSize = 32;
static uint ScatteringTexNUSize = 8;

#define bottom_radius 6360
#define top_radius 6420
static uint TransmittanceTexWidth = 256;
static uint TransmittanceTexHeight = 64;

float RayleighPhaseFunction(float nu) {
	float k = 3.0 / (16.0 * PI);
	return k * (1.0 + nu * nu);
}
float GetTextureCoordFromUnitRange(float x, int texture_size) {
	return 0.5f / (float)texture_size + x * (1.0f - 1.0f / (float)texture_size);
}

float MiePhaseFunction(float g, float nu) {
	float k = 3.0 / (8.0 * PI) * (1.0 - g * g) / (2.0 + g * g);
	return k * (1.0 + nu * nu) / pow(1.0 + g * g - 2.0 * g * nu, 1.5);
}

float GetUnitRangeFromTextureCoord(float u, int texture_size) {
	return (u - 0.5f / (float)texture_size) / (1.0f - 1.0f / (float)texture_size);
}

float DistanceToTopAtmosphereBoundary(float r, float mu) {
	float discriminant = r * r * (mu * mu - 1.0f) + top_radius * top_radius;
	return max(-r * mu + sqrt(max(discriminant, 0.0f)), 0.0f);
}
float DistanceToBottomAtmosphereBoundary(float r, float mu) {
	if (mu < -1 || mu > 1 || r > top_radius) return 0;
	float discriminant = r * r * (mu * mu - 1.0f) + bottom_radius * bottom_radius;
	return max(-r * mu - sqrt(max(discriminant, 0.0f)), 0.0f);
}


float4 GetScatteringTextureUvwzFromRMuMuSNu(float r, float mu, float mu_s, float nu, bool ray_r_mu_intersects_ground) {
	float H = sqrt(top_radius * top_radius - bottom_radius * bottom_radius);
	float rho = sqrt(max(r * r - bottom_radius * bottom_radius, 0.0f));
	float u_r = GetTextureCoordFromUnitRange(rho / H, ScatteringTexRSize);

	float r_mu = r * mu;
	float discriminant = r_mu * r_mu - r * r + bottom_radius * bottom_radius;
	float u_mu;
	if (ray_r_mu_intersects_ground) {
		float d = -r_mu - sqrt(max(discriminant, 0.0f));
		float d_min = r - bottom_radius;
		float d_max = rho;
		u_mu = 0.5f - 0.5f * GetTextureCoordFromUnitRange((d_max == d_min) ? 0.0f : (d - d_min) / (d_max - d_min), ScatteringTexMUSize / 2);
	}
	else {
		float d = -r_mu + sqrt(max(discriminant + H * H, 0.0f));
		float d_min = top_radius - r;
		float d_max = rho + H;
		u_mu = 0.5f + 0.5f * GetTextureCoordFromUnitRange((d - d_min) / (d_max - d_min), ScatteringTexMUSize / 2);
	}

	float d = DistanceToTopAtmosphereBoundary(bottom_radius, mu_s);
	float d_min = top_radius - bottom_radius;
	float d_max = H;
	float a = (d - d_min) / (d_max - d_min);
	float D = DistanceToTopAtmosphereBoundary(bottom_radius, -0.8f);
	float A = (D - d_min) / (d_max - d_min);

	float u_mu_s = GetTextureCoordFromUnitRange(max(1.0f - a / A, 0.0f) / (1.0f + a), ScatteringTexMUSSize);

	float u_nu = (nu + 1.0f) / 2.0f;
	return float4(u_nu, u_mu_s, u_mu, u_r);
}


Texture3D<float4> scattering_texture;
Texture2D<float4> TransmittanceTex;
Texture2D<float4> IrradianceTex;
SamplerState linearClampSampler;
SamplerState sampler_scattering_texture_trilinear_clamp;

#define rayleigh_scattering float3(0.00585f, 0.013558f, 0.03310f)
#define mie_scattering 0.003996f

float ClampCosine(float mu) {
	return clamp(mu, -1.0f, 1.0f);
}

float ClampRadius(float r) {
	return clamp(r, bottom_radius, top_radius);
}


float3 GetExtrapolatedSingleMieScattering(
	float4 scattering) {
	if (scattering.r == 0.0) {
		return 0;
	}
	return scattering.rgb * scattering.a / scattering.r *
		(rayleigh_scattering.r / mie_scattering.r) *
		(mie_scattering / rayleigh_scattering);
}

float3 GetCombinedScattering(
	float r, float mu, float mu_s, float nu,
	bool ray_r_mu_intersects_ground,
	inout float3 single_mie_scattering) {
	float4 uvwz = GetScatteringTextureUvwzFromRMuMuSNu(r, mu, mu_s, nu, ray_r_mu_intersects_ground);
	float tex_coord_x = uvwz.x * float(ScatteringTexNUSize - 1);
	float tex_x = floor(tex_coord_x);
	float lerp2 = tex_coord_x - tex_x;
	float3 uvw0 = float3((tex_x + uvwz.y) / float(ScatteringTexNUSize),
		uvwz.z, uvwz.w);
	float3 uvw1 = float3((tex_x + 1.0 + uvwz.y) / float(ScatteringTexNUSize),
		uvwz.z, uvwz.w);
	float4 combined_scattering =
		scattering_texture.SampleLevel(sampler_scattering_texture_trilinear_clamp, uvw0, 0) * (1.0 - lerp2) +
		scattering_texture.SampleLevel(sampler_scattering_texture_trilinear_clamp, uvw1, 0) * lerp2;
	float3 scattering = float3(combined_scattering.rgb);
	single_mie_scattering =
		GetExtrapolatedSingleMieScattering(combined_scattering);
	return scattering;
}

bool RayIntersectsGround(float r, float mu) {
	return (mu < 0.0f && r * r * (mu * mu - 1.0f) + bottom_radius * bottom_radius >= 0.0f);
}

float2 GetTransmittanceTextureUvFromRMu(float r, float mu) {
	float H = sqrt(top_radius * top_radius - bottom_radius * bottom_radius);

	float rho = sqrt(max(r * r - bottom_radius * bottom_radius, 0.0f));

	float d = DistanceToTopAtmosphereBoundary(r, mu);
	float d_min = top_radius - r;
	float d_max = rho + H;
	float x_mu = (d - d_min) / (d_max - d_min);
	float x_r = rho / H;
	return float2(GetTextureCoordFromUnitRange(x_mu, TransmittanceTexWidth), GetTextureCoordFromUnitRange(x_r, TransmittanceTexHeight));
}

SamplerState _LinearClamp;

float3 GetTransmittanceToTopAtmosphereBoundary(float r, float mu) {
	float2 uv = GetTransmittanceTextureUvFromRMu(r, mu);
	return TransmittanceTex.SampleLevel(_LinearClamp, uv, 0).rgb;
}

float3 GetTransmittance(float r, float mu, float d, bool ray_r_mu_intersects_ground) {
	float r_d = clamp(sqrt(d * d + 2.0f * r * mu * d + r * r), bottom_radius, top_radius);
	float mu_d = clamp((r * mu + d) / r_d, -1.0f, 1.0f);
	if (ray_r_mu_intersects_ground) {
		return min(exp(GetTransmittanceToTopAtmosphereBoundary(r_d, -mu_d) -
			GetTransmittanceToTopAtmosphereBoundary(r, -mu)),
			float3(1.0f, 1.0f, 1.0f));
	} else {
		return min(exp(GetTransmittanceToTopAtmosphereBoundary(r, mu) -
			GetTransmittanceToTopAtmosphereBoundary(r_d, mu_d)),
			float3(1.0f, 1.0f, 1.0f));
	}
}
#define IrradianceTexWidth 64
#define IrradianceTexHeight 16
float2 GetIrradianceTextureUvFromRMuS(float r, float mu_s) {
	float x_r = (r - bottom_radius) /
		(top_radius - bottom_radius);
	float x_mu_s = mu_s * 0.5 + 0.5;
	return float2(GetTextureCoordFromUnitRange(x_mu_s, IrradianceTexWidth),
		GetTextureCoordFromUnitRange(x_r, IrradianceTexHeight));
}

float3 GetIrradiance(float r, float mu_s) {
	float2 uv = GetIrradianceTextureUvFromRMuS(r, mu_s);
	return IrradianceTex.SampleLevel(_LinearClamp, uv, 0);
}

float3 GetTransmittanceToSun(float r, float mu_s) {
	float sin_theta_h = bottom_radius / r;
	float cos_theta_h = -sqrt(max(1.0f - sin_theta_h * sin_theta_h, 0.0f));
	return exp(GetTransmittanceToTopAtmosphereBoundary(r, mu_s)) *
		smoothstep(-sin_theta_h * (0.00935f / 2.0f) / 1.0f,
			sin_theta_h * (0.00935f / 2.0f) / 1.0f,
			mu_s - cos_theta_h);
}

float3 GetSkyRadianceToPoint(
	float3 camera, float3 pos,
	float3 sun_direction, out float3 transmittance)
{
	// Compute the distance to the top atmosphere boundary along the view ray,
	// assuming the viewer is in space (or NaN if the view ray does not intersect
	// the atmosphere).
	float3 view_ray = normalize(pos - camera);
	float r = length(camera);
	float rmu = dot(camera, view_ray);
	float distance_to_top_atmosphere_boundary = -rmu - sqrt(rmu * rmu - r * r + top_radius * top_radius);

	// If the viewer is in space and the view ray intersects the atmosphere, move
	// the viewer to the top atmosphere boundary (along the view ray):
	if (distance_to_top_atmosphere_boundary > 0.0)
	{
		camera = camera + view_ray * distance_to_top_atmosphere_boundary;
		r = top_radius;
		rmu += distance_to_top_atmosphere_boundary;
	}

	// Compute the r, mu, mu_s and nu parameters for the first texture lookup.
	float mu = rmu / r;
	float mu_s = dot(camera, sun_direction) / r;
	float nu = dot(view_ray, sun_direction);
	float d = length(pos - camera);
	bool ray_r_mu_intersects_ground = RayIntersectsGround(r, mu);

	transmittance = GetTransmittance(
		r, mu, d, ray_r_mu_intersects_ground);

	float3 single_mie_scattering;
	float3 scattering = GetCombinedScattering(
		r, mu, mu_s, nu, ray_r_mu_intersects_ground,
		single_mie_scattering);

	// Compute the r, mu, mu_s and nu parameters for the second texture lookup.
	// If shadow_length is not 0 (case of light shafts), we want to ignore the
	// scattering along the last shadow_length meters of the view ray, which we
	// do by subtracting shadow_length from d (this way scattering_p is equal to
	// the S|x_s=x_0-lv term in Eq. (17) of our paper).
	d = max(d, 0.0);
	float r_p = ClampRadius(sqrt(d * d + 2.0 * r * mu * d + r * r));
	float mu_p = (r * mu + d) / r_p;
	float mu_s_p = (r * mu_s + d * nu) / r_p;

	float3 single_mie_scattering_p;
	float3 scattering_p = GetCombinedScattering(
		r_p, mu_p, mu_s_p, nu, ray_r_mu_intersects_ground,
		single_mie_scattering_p);

	// Combine the lookup results to get the scattering between camera and point.
	float3  shadow_transmittance = transmittance;
	scattering = scattering - shadow_transmittance * scattering_p;
	single_mie_scattering = single_mie_scattering - shadow_transmittance * single_mie_scattering_p;
	// Hack to avoid rendering artifacts when the sun is below the horizon.
	single_mie_scattering = single_mie_scattering *
		smoothstep(0, 0.01, mu_s);

	return scattering * RayleighPhaseFunction(nu) + single_mie_scattering *
		MiePhaseFunction(0.8f, nu);
}
float3 GetSunAndSkyIrradiance(float3 groundpoint, float3 normal, float3 sun_direction, inout float3 sky_irradiance) {
	float r = length(groundpoint);
	float mu_s = dot(groundpoint, sun_direction) / r;

	sky_irradiance = GetIrradiance(r, mu_s) * (1.0f + abs(dot(normal, groundpoint)) / r) * 0.5f;
	return GetTransmittanceToSun(r, mu_s) * max(dot(normal, sun_direction), 0);
}
float3 GroundColor;
float3 ClayColor;
float3 GetSkyRadiance(
	float3 camera, float3 view_ray, float shadow_length,
	float3 sun_direction, inout float3 transmittance, inout float3 debug) {
	camera /= 2048.0f;
	camera.y = max(camera.y, 0);
	camera.y += bottom_radius;

	// Compute the distance to the top atmosphere boundary along the view ray,
	// assuming the viewer is in space (or NaN if the view ray does not intersect
	// the atmosphere).
	float r = length(camera);
	float rmu = dot(camera, view_ray);
	float distance_to_top_atmosphere_boundary = -rmu -
		sqrt(rmu * rmu - r * r + top_radius * top_radius);
	// If the viewer is in space and the view ray intersects the atmosphere, move
	// the viewer to the top atmosphere boundary (along the view ray):
	if (distance_to_top_atmosphere_boundary > 0.0) {
		camera = camera + view_ray * distance_to_top_atmosphere_boundary;
		r = top_radius;
		rmu += distance_to_top_atmosphere_boundary;
	} else if (r >= top_radius) {
		// If the view ray does not intersect the atmosphere, simply return 0.
		transmittance = 1;
		return 0;
	}
	// Compute the r, mu, mu_s and nu parameters needed for the texture lookups.
	float mu = rmu / r;
	float mu_s = dot(camera, sun_direction) / r;
	float nu = dot(view_ray, sun_direction);
	bool ray_r_mu_intersects_ground = RayIntersectsGround(r, mu);

	transmittance = ray_r_mu_intersects_ground ? 0.0 :
		exp(GetTransmittanceToTopAtmosphereBoundary(r, mu));
	float3 single_mie_scattering;
	float3 scattering;
		scattering = GetCombinedScattering(
			r, mu, mu_s, nu, ray_r_mu_intersects_ground,
			single_mie_scattering);
		if(ray_r_mu_intersects_ground) {
			float3 Position = camera + view_ray * DistanceToBottomAtmosphereBoundary(r, mu);
			float3 Normal = normalize(Position - float3(0,-bottom_radius,0));
			float3 sky_irradiance;

			float3 sun_irradiance = GetSunAndSkyIrradiance(Position, Normal, sun_direction, sky_irradiance);
			float3 trans;
			float3 in_scatter = GetSkyRadianceToPoint(camera, Position, sun_direction, trans);
			debug = (GroundColor * (1.0f / PI) * (sun_irradiance + sky_irradiance)) * trans + in_scatter * 100.0f;
		}
	return scattering * RayleighPhaseFunction(nu) + single_mie_scattering *
		MiePhaseFunction(0.8f, nu);
}



bool RayIntersectsGround2(float r, float mu) {
	return (mu < -0.01f && r * r * (mu * mu - 1.0f) + bottom_radius * bottom_radius >= 0.0f);
}

float3 GetSkyTransmittance(
	float3 camera, float3 view_ray, float shadow_length,
	float3 sun_direction) {
	camera /= 2048.0f;
	camera.y += bottom_radius;

	// Compute the distance to the top atmosphere boundary along the view ray,
	// assuming the viewer is in space (or NaN if the view ray does not intersect
	// the atmosphere).
	float r = length(camera);
	float rmu = dot(camera, view_ray);
	float distance_to_top_atmosphere_boundary = -rmu -
		sqrt(rmu * rmu - r * r + top_radius * top_radius);
	// If the viewer is in space and the view ray intersects the atmosphere, move
	// the viewer to the top atmosphere boundary (along the view ray):
	if (distance_to_top_atmosphere_boundary > 0.0) {
		camera = camera + view_ray * distance_to_top_atmosphere_boundary;
		r = top_radius;
		rmu += distance_to_top_atmosphere_boundary;
	} else if (r >= top_radius) {
		// If the view ray does not intersect the atmosphere, simply return 0.
		return 1;
	}
	// Compute the r, mu, mu_s and nu parameters needed for the texture lookups.
	float mu = rmu / r;
	float mu_s = dot(camera, sun_direction) / r;
	float nu = dot(view_ray, sun_direction);
	bool ray_r_mu_intersects_ground = RayIntersectsGround2(r, mu);

	return ray_r_mu_intersects_ground ? 0.0 :
		exp(GetTransmittanceToTopAtmosphereBoundary(r, mu));
}

bool VisabilityCheckCompute(Ray ray, float dist) {
        uint2 stack[24];
        int stack_size = 0;
        uint2 current_group;

        uint oct_inv4;
        int tlas_stack_size = -1;
        int mesh_id = -1;
        Ray ray2;
        int TriOffset = 0;
        int NodeOffset = 0;

        ray.direction_inv = rcp(ray.direction);
        ray2 = ray;

        oct_inv4 = ray_get_octant_inv4(ray.direction);

        current_group.x = (uint)0;
        current_group.y = (uint)0x80000000;
        int MatOffset = 0;
        int Reps = 0;
        uint2 triangle_group;
        [loop] while (Reps < 1000) {//Traverse Accelleration Structure(Compressed Wide Bounding Volume Hierarchy)            
            [branch]if (current_group.y & 0xff000000) {
                uint child_index_offset = firstbithigh(current_group.y);
                
                uint slot_index = (child_index_offset - 24) ^ (oct_inv4 & 0xff);
                uint relative_index = countbits(current_group.y & ~(0xffffffff << slot_index));
                uint child_node_index = current_group.x + relative_index;

                current_group.y &= ~(1 << child_index_offset);

                if (current_group.y & 0xff000000) stack[stack_size++] = current_group;

                const BVHNode8Data TempNode = cwbvh_nodes[child_node_index];
                float3 node_0 = TempNode.node_0xyz;
                uint node_0w = TempNode.node_0w;

                uint4 node_1 = TempNode.node_1;
                uint4 node_2 = TempNode.node_2;
                uint4 node_3 = TempNode.node_3;
                uint4 node_4 = TempNode.node_4;

                uint hitmask = cwbvh_node_intersect(ray, oct_inv4, dist, node_0, node_0w, node_1, node_2, node_3, node_4);

 				current_group.y = (hitmask & 0xff000000) | ((node_0w >> 24) & 0xff);
                triangle_group.y = (hitmask & 0x00ffffff);

	            current_group.x = (node_1.x) + NodeOffset;
                triangle_group.x = (node_1.y) + TriOffset;
                Reps++;
            } else {
                triangle_group = current_group;
                current_group = (uint2)0;
            }

            if(triangle_group.y != 0) {
                [branch]if (tlas_stack_size == -1) {//Transfer from Top Level Accelleration Structure to Bottom Level Accelleration Structure
                    uint mesh_offset = firstbithigh(triangle_group.y);
                    triangle_group.y &= ~(1 << mesh_offset);
                    mesh_id = TLASBVH8Indices[triangle_group.x + mesh_offset];
                    NodeOffset = _MeshData[mesh_id].NodeOffset;
                    TriOffset = _MeshData[mesh_id].TriOffset;

                    if (triangle_group.y != 0) stack[stack_size++] = triangle_group;

                    if (current_group.y & 0xff000000) stack[stack_size++] = current_group;

                    tlas_stack_size = stack_size;

                    int root_index = (_MeshData[mesh_id].mesh_data_bvh_offsets & 0x7fffffff);

                    MatOffset = _MeshData[mesh_id].MaterialOffset;
                    ray.direction = (mul((float3x3)_MeshData[mesh_id].W2L, ray.direction)).xyz;
                    ray.origin = (mul(_MeshData[mesh_id].W2L, float4(ray.origin, 1))).xyz;
                    ray.direction_inv = rcp(ray.direction);

                    oct_inv4 = ray_get_octant_inv4(ray.direction);

                    current_group.x = (uint)root_index;
                    current_group.y = (uint)0x80000000;
                } else {
                    float3 through = 0;
					while (triangle_group.y != 0) {                        
                        uint triangle_index = firstbithigh(triangle_group.y);
                        triangle_group.y &= ~(1 << triangle_index);
	                    if (triangle_intersect_shadow(triangle_group.x + triangle_index, ray, dist, mesh_id, through, MatOffset)) {
							return false;
	                    }
                    }
                }
            }

            if ((current_group.y & 0xff000000) == 0) {
                if (stack_size == 0) {//thread has finished traversing
                    break;
                }

                if (stack_size == tlas_stack_size) {
					NodeOffset = 0;
                    TriOffset = 0;
                    tlas_stack_size = -1;
                    ray = ray2;
                    oct_inv4 = ray_get_octant_inv4(ray.direction);
                }
                current_group = stack[--stack_size];
            }
        }
        return true;
}

float SunDesaturate;
float SkyDesaturate;

float3 SunDir;


inline float luminance(const float3 a) {
    return dot(float3(0.299f, 0.587f, 0.114f), a);
}

inline float3 GetTriangleNormal(const uint TriIndex, const float2 TriUV, const float3x3 Inverse) {
    float3 Normal0 = i_octahedral_32(AggTris[TriIndex].norms.x);
    float3 Normal1 = i_octahedral_32(AggTris[TriIndex].norms.y);
    float3 Normal2 = i_octahedral_32(AggTris[TriIndex].norms.z);
    Normal2 = mul(Inverse, (Normal0 * (1.0f - TriUV.x - TriUV.y) + TriUV.x * Normal1 + TriUV.y * Normal2));
    float wldScale = rsqrt(dot(Normal2, Normal2));
    return mul(wldScale, Normal2);	
}

inline float3 GetTriangleTangent(const uint TriIndex, const float2 TriUV, const float3x3 Inverse) {
    float3 Normal0 = i_octahedral_32(AggTris[TriIndex].tans.x);
    float3 Normal1 = i_octahedral_32(AggTris[TriIndex].tans.y);
    float3 Normal2 = i_octahedral_32(AggTris[TriIndex].tans.z);
    Normal2 = mul(Inverse, (Normal0 * (1.0f - TriUV.x - TriUV.y) + TriUV.x * Normal1 + TriUV.y * Normal2));
    float wldScale = rsqrt(dot(Normal2, Normal2));
    return mul(wldScale, Normal2);
}

float GetHeightRaw(float3 CurrentPos, const TerrainData Terrain) {
    CurrentPos -= Terrain.PositionOffset;
    float3 b = float3(Terrain.TerrainDim, 0.11f, Terrain.TerrainDim);
    float2 uv = float2(min(CurrentPos.x / Terrain.TerrainDim, b.x / Terrain.TerrainDim), min(CurrentPos.z / Terrain.TerrainDim, b.z / Terrain.TerrainDim));
    float h = Heightmap.SampleLevel(sampler_trilinear_clamp, uv * (Terrain.HeightMap.xy - Terrain.HeightMap.zw) + Terrain.HeightMap.zw, 0).x;
    h *= Terrain.HeightScale * 2.0f;
    return h;
}

inline float3 GetHeightmapNormal(float3 Position, uint TerrainID) {
	TerrainData CurrentTerrain = Terrains[TerrainID];
	static const float2 Offset = float2(0.25f,0);
	float3 Center = float3(Position.x, GetHeightRaw(Position, CurrentTerrain), Position.z);
	float3 OffX = float3(Position.x + Offset.x, GetHeightRaw(Position + Offset.xyy, CurrentTerrain), Position.z);
	float3 OffY = float3(Position.x, GetHeightRaw(Position + Offset.yyx, CurrentTerrain), Position.z + Offset.x);
	return normalize(cross(normalize(OffX - Center), normalize(OffY - Center)));
}

inline void orthonormal_basis(const float3 normal, inout float3 tangent, inout float3 binormal) {
    float sign2 = (normal.z >= 0.0f) ? 1.0f : -1.0f;
    float a = -1.0f / (sign2 + normal.z);
    float b = normal.x * normal.y * a;

    tangent = float3(1.0f + sign2 * normal.x * normal.x * a, sign2 * b, -sign2 * normal.x);
    binormal = float3(b, sign2 + normal.y * normal.y * a, -normal.y);
}

inline float2 vogelDiskSample(int i, int num_samples, float r_offset, float phi_offset) {
    float r = sqrt((float(i) + 0.07f + r_offset*0.93f) / float(num_samples));
    float phi = float(i) * 2.399963229728f + 2.0f * PI * phi_offset;
    float sinc;
    sincos(phi, sinc, phi);
    return r * float2(sinc,phi);
}

inline float4x4 inverse(float4x4 m) {
    float n11 = m[0][0], n12 = m[1][0], n13 = m[2][0], n14 = m[3][0];
    float n21 = m[0][1], n22 = m[1][1], n23 = m[2][1], n24 = m[3][1];
    float n31 = m[0][2], n32 = m[1][2], n33 = m[2][2], n34 = m[3][2];
    float n41 = m[0][3], n42 = m[1][3], n43 = m[2][3], n44 = m[3][3];

    float t11 = n23 * n34 * n42 - n24 * n33 * n42 + n24 * n32 * n43 - n22 * n34 * n43 - n23 * n32 * n44 + n22 * n33 * n44;
    float t12 = n14 * n33 * n42 - n13 * n34 * n42 - n14 * n32 * n43 + n12 * n34 * n43 + n13 * n32 * n44 - n12 * n33 * n44;
    float t13 = n13 * n24 * n42 - n14 * n23 * n42 + n14 * n22 * n43 - n12 * n24 * n43 - n13 * n22 * n44 + n12 * n23 * n44;
    float t14 = n14 * n23 * n32 - n13 * n24 * n32 - n14 * n22 * n33 + n12 * n24 * n33 + n13 * n22 * n34 - n12 * n23 * n34;

    float det = n11 * t11 + n21 * t12 + n31 * t13 + n41 * t14;
    float idet = 1.0f / det;

    float4x4 ret;

    ret[0][0] = t11 * idet;
    ret[0][1] = (n24 * n33 * n41 - n23 * n34 * n41 - n24 * n31 * n43 + n21 * n34 * n43 + n23 * n31 * n44 - n21 * n33 * n44) * idet;
    ret[0][2] = (n22 * n34 * n41 - n24 * n32 * n41 + n24 * n31 * n42 - n21 * n34 * n42 - n22 * n31 * n44 + n21 * n32 * n44) * idet;
    ret[0][3] = (n23 * n32 * n41 - n22 * n33 * n41 - n23 * n31 * n42 + n21 * n33 * n42 + n22 * n31 * n43 - n21 * n32 * n43) * idet;

    ret[1][0] = t12 * idet;
    ret[1][1] = (n13 * n34 * n41 - n14 * n33 * n41 + n14 * n31 * n43 - n11 * n34 * n43 - n13 * n31 * n44 + n11 * n33 * n44) * idet;
    ret[1][2] = (n14 * n32 * n41 - n12 * n34 * n41 - n14 * n31 * n42 + n11 * n34 * n42 + n12 * n31 * n44 - n11 * n32 * n44) * idet;
    ret[1][3] = (n12 * n33 * n41 - n13 * n32 * n41 + n13 * n31 * n42 - n11 * n33 * n42 - n12 * n31 * n43 + n11 * n32 * n43) * idet;

    ret[2][0] = t13 * idet;
    ret[2][1] = (n14 * n23 * n41 - n13 * n24 * n41 - n14 * n21 * n43 + n11 * n24 * n43 + n13 * n21 * n44 - n11 * n23 * n44) * idet;
    ret[2][2] = (n12 * n24 * n41 - n14 * n22 * n41 + n14 * n21 * n42 - n11 * n24 * n42 - n12 * n21 * n44 + n11 * n22 * n44) * idet;
    ret[2][3] = (n13 * n22 * n41 - n12 * n23 * n41 - n13 * n21 * n42 + n11 * n23 * n42 + n12 * n21 * n43 - n11 * n22 * n43) * idet;

    ret[3][0] = t14 * idet;
    ret[3][1] = (n13 * n24 * n31 - n14 * n23 * n31 + n14 * n21 * n33 - n11 * n24 * n33 - n13 * n21 * n34 + n11 * n23 * n34) * idet;
    ret[3][2] = (n14 * n22 * n31 - n12 * n24 * n31 - n14 * n21 * n32 + n11 * n24 * n32 + n12 * n21 * n34 - n11 * n22 * n34) * idet;
    ret[3][3] = (n12 * n23 * n31 - n13 * n22 * n31 + n13 * n21 * n32 - n11 * n23 * n32 - n12 * n21 * n33 + n11 * n22 * n33) * idet;

    return ret;
}

inline float AreaOfTriangle(float3 pt1, float3 pt2, float3 pt3) {
    float a = distance(pt1, pt2);
    float b = distance(pt2, pt3);
    float c = distance(pt3, pt1);
    float s = (a + b + c) / 2.0f;
    return sqrt(s * (s - a) * (s - b) * (s - c));
}





struct SH {
	float4 shY;
	float2 CoCg;
};

inline SH init_SH()
{
	SH result;
	result.shY = 0;
	result.CoCg = 0;
	return result;
}

inline void accumulate_SH(inout SH accum, SH b, float scale)
{
	accum.shY += b.shY * scale;
	accum.CoCg += b.CoCg * scale;
}

inline SH mix_SH(SH a, SH b, float s)
{
	SH result;
	result.shY = lerp(a.shY, b.shY, s);
	result.CoCg = lerp(a.CoCg, b.CoCg, s);
	return result;
}

inline SH load_SH(Texture2D<float4> img_shY, Texture2D<float2> img_CoCg, int2 p)
{
	SH result;
	result.shY = img_shY[p];
	result.CoCg = img_CoCg[p];
	return result;
}

inline SH load_SH(RWTexture2D<float4> img_shY, RWTexture2D<float2> img_CoCg, int2 p)
{
	SH result;
	result.shY = img_shY[p];
	result.CoCg = img_CoCg[p];
	return result;
}

// Use a macro to work around the glslangValidator errors about function argument type mismatch
#define STORE_SH(img_shY, img_CoCg, p, sh) {img_shY[p] = sh.shY; img_CoCg[p] = sh.CoCg; }

inline void store_SH(RWTexture2D<float4> img_shY, RWTexture2D<float2> img_CoCg, int2 p, SH sh)
{
	img_shY[p] = sh.shY;
	img_CoCg[p] = sh.CoCg;
}


SH irradiance_to_SH(float3 color, float3 dir)
{
	SH result;
	color = log(color + 1);
	float   Co = color.r - color.b;
	float   t = color.b + Co * 0.5;
	float   Cg = color.g - t;
	float   Y = max(t + Cg * 0.5, 0.0);

	result.CoCg = float2(Co, Cg);

	float   L00 = 0.282095;
	float   L1_1 = 0.488603 * dir.y;
	float   L10 = 0.488603 * dir.z;
	float   L11 = 0.488603 * dir.x;

	result.shY = float4 (L11, L1_1, L10, L00) * Y;

	return result;
}

inline float3 project_SH_irradiance(SH sh, float3 N)
{
	float d = dot(sh.shY.xyz, N);
	float Y = 2.0 * (1.023326 * d + 0.886226 * sh.shY.w);
	Y = max(Y, 0.0);

	sh.CoCg *= Y * 0.282095 / (sh.shY.w + 1e-6);

	float   T = Y - sh.CoCg.y * 0.5;
	float   G = sh.CoCg.y + T;
	float   B = T - sh.CoCg.x * 0.5;
	float   R = B + sh.CoCg.x;

	return max(exp(float3(R, G, B)) - 1, 0.0);
}


float3 sample_projected_triangle(float3 pt, TrianglePos pos, float2 rnd, out float3 light_normal, out float pdfw, out float2 UVs, bool DoSimple)
{
	if(DoSimple) {
		UVs = sample_triangle(rnd.x, rnd.y);
		float3 posA = pos.pos0;
		float3 posB = posA + pos.posedge1;
		float3 posC = posA + pos.posedge2;
		light_normal = cross(posB - posA, posC - posA);
		light_normal = normalize(light_normal);
		float3 LightPos = posA + pos.posedge1 * UVs.x + pos.posedge2 * UVs.y;
		pdfw = rcp(dot(LightPos - pt, LightPos - pt)) * (AreaOfTriangle(posA, posB, posC));
		return LightPos;		
	}
	float3 posA = pos.pos0;
	float3 posB = posA + pos.posedge1;
	float3 posC = posA + pos.posedge2;
	light_normal = cross(posB - posA, posC - posA);
	light_normal = normalize(light_normal);

	// Use surface point as origin
	posA = posA - pt;
	posB = posB - pt;
	posC = posC - pt;

	// Distance of triangle to origin
	float o = dot(light_normal, posA);

	// Project triangle to unit sphere
	float3 A = normalize(posA);
	float3 B = normalize(posB);
	float3 C = normalize(posC);
	// Planes passing through two vertices and origin. They'll be used to obtain the angles.
	float3 norm_AB = normalize(cross(A, B));
	float3 norm_BC = normalize(cross(B, C));
	float3 norm_CA = normalize(cross(C, A));
	// Side of spherical triangle
	float cos_c = dot(A, B);
	// Angles at vertices
	float cos_alpha = dot(norm_AB, -norm_CA);
	float cos_beta = dot(norm_BC, -norm_AB);
	float cos_gamma = dot(norm_CA, -norm_BC);

	// Area of spherical triangle. From: "On the Measure of Solid Angles", F. Eriksson, 1990.
	float area = 2 * atan(abs(dot(A, cross(B, C))) / (1 + dot(A, B) + dot(B, C) + dot(A, C)));

	// Use one random variable to select the new area.
	float new_area = rnd.x * area;

	float sin_alpha = sqrt(1 - cos_alpha * cos_alpha); // = sin(acos(cos_alpha))
	float sin_new_area = sin(new_area);
	float cos_new_area = cos(new_area);
	// Save the sine and cosine of the angle phi.
	float p = sin_new_area * cos_alpha - cos_new_area * sin_alpha;
	float q = cos_new_area * cos_alpha + sin_new_area * sin_alpha;

	// Compute the pair (u, v) that determines new_beta.
	float u = q - cos_alpha;
	float v = p + sin_alpha * cos_c;

	// Let cos_b be the cosine of the new edge length new_b.
	float cos_b = clamp(((v * q - u * p) * cos_alpha - v) / ((v * p + u * q) * sin_alpha), -1, 1);

	// Compute the third vertex of the sub-triangle.
	float3 new_C = cos_b * A + sqrt(1 - cos_b * cos_b) * normalize(C - dot(C, A) * A);

	// Use the other random variable to select cos(phi).
	float z = 1 - rnd.y * (1 - dot(new_C, B));

	// Construct the corresponding point on the sphere.
	float3 direction = z * B + sqrt(1 - z * z) * normalize(new_C - dot(new_C, B) * B);
	// ...which is also the direction!

	// Line-plane intersection
	float3 lo = direction * (o / dot(light_normal, direction));

	// Since the solid angle is distributed uniformly, the PDF wrt to solid angle is simply:
	pdfw = area;
	UVs = float2(u, v);
	return lo;
}

float get_spherical_triangle_pdfw(float3 A, float3 B, float3 C, float3 p)
{
	A -= p;
	B -= p;
	C -= p;
	// Project triangle to unit sphere
	A = normalize(A);
	B = normalize(B);
	C = normalize(C);
	// Planes passing through two vertices and origin. They'll be used to obtain the angles.
	float3 norm_AB = normalize(cross(A, B));
	float3 norm_BC = normalize(cross(B, C));
	float3 norm_CA = normalize(cross(C, A));
	// Side of spherical triangle
	float cos_c = dot(A, B);
	// Angles at vertices
	float cos_alpha = dot(norm_AB, -norm_CA);
	float cos_beta = dot(norm_BC, -norm_AB);
	float cos_gamma = dot(norm_CA, -norm_BC);

	// Area of spherical triangle
	float area = 2 * atan(abs(dot(A, cross(B, C))) / (1 + dot(A, B) + dot(B, C) + dot(A, C)));

	// Since the solid angle is distributed uniformly, the PDF wrt to solid angle is simply:
	return 1 / area;
}


static const float starCount = 20000.0;
static const float flickerSpeed = 6.0;

float randS(float p){
    p = frac(p * .1031);
    p *= p + 33.33;
    p *= p + p;
    return frac(p);
}

float randS(float2 co){
    return frac(sin(dot(co.xy,float2(12.9898,78.233))) * 43758.5453);
}
float getGlow(float dist, float radius, float intensity){
    dist = max(dist, 5e-7);
	return pow(radius/dist, intensity);	
}


//Get Cartesian coordinates from spherical.
float3 getStarPosition(float theta, float phi){
	return normalize(float3(	sin(theta)*cos(phi),
               				sin(theta)*sin(phi),
               				cos(theta)));
}

bool isActiveElevation(float theta, float level){
    return sin(theta) > randS(float2(theta, level));
}

float getDistToStar(float3 p, float theta, float phi){
    float3 starPos = getStarPosition(theta, phi);
    return 0.5+0.5*dot(starPos, p);
}

float rand(float2 co) {
	float a = 12.9898;
	float b = 78.233;
	float c = 43758.5453;
	float dt = dot(co.xy, float2(a, b));
	float sn = fmod(dt, 3.14);

	return 2.0 * frac(sin(sn) * c) - 1.0;
}

//Get star colour from view direction.
float StarRender(float3 rayDir){
    
    //acos returns a value in the range [0, PI].
    //The theta of the original view ray.
    float theta = acos(rayDir.z);

    //Extent of each level.
    float width = PI/starCount;
    
    //The level on which the view ray falls.
    float level = floor((theta/PI)*starCount);
    
    //The theta of the level considered.
    float theta_;
    //Random angle of the star on the level.
    float phi_;
    
    float stars = 0.0;
    float dist;
    
    //Variable to keep track of neighbouring levels.
    float level_;
    
    float rnd;
    
    //For a set number of layers above and below the view ray one,
    //accumulate the star colour.
    for(float l = -10.0; l <= 10.0; l++){
        
    	level_ = min(starCount-1.0, max(0.0, level+l));
        theta_ = (level_+0.5)*width;

        //Uniformly picked latitudes lead to stars concentrating at the poles.
        //Make the likelyhood of rendering stars a function of sin(theta_)
        if(!isActiveElevation(theta_, 0.0)){
            continue;
        }
        
        rnd = randS(PI+theta_);
        phi_ = PI*2.0f*randS(level_);
        dist = getDistToStar(rayDir, theta_, phi_);
        
        stars += getGlow(1.0-dist, rnd*8e-7, 2.9 + (sin(rand(rnd)*flickerSpeed*frames_accumulated / 0.01f)));
    }
    
    return 0.05*stars;
}





struct LightBVHData {
	float3 BBMax;
	float3 BBMin;
	float3 w;
	float phi;
	float cosTheta_o;
	float cosTheta_e;
	int LightCount;
	float Pad1;
	int left;
	int isLeaf;
};

StructuredBuffer<LightBVHData> LightNodes;

float cosSubClamped(float sinTheta_a, float cosTheta_a, float sinTheta_b, float cosTheta_b) {
	if(cosTheta_a > cosTheta_b) return 1;
	return cosTheta_a * cosTheta_b + sinTheta_a * sinTheta_b;
}
float sinSubClamped(float sinTheta_a, float cosTheta_a, float sinTheta_b, float cosTheta_b) {
	if(cosTheta_a > cosTheta_b) return 0;
	return sinTheta_a * cosTheta_b - cosTheta_a * sinTheta_b;
}

float BoundSubtendedDirections(float3 BBMax, float3 BBMin, float3 p) {
    float3 pCenter = (BBMax + BBMin) / 2.0f;
    float radius = (all(pCenter >= BBMin) && all(pCenter <= BBMax)) ? dot(BBMax - pCenter, BBMax - pCenter) : 0;
    float pDiff = dot(p - pCenter, p - pCenter);
    if(pDiff < radius) return -1;
    float sin2ThetaMax = (radius) / pDiff;
    float cosThetaMax = sqrt(1.0f - sin2ThetaMax);
    return cosThetaMax;
}


 float Importance(const float3 p, const float3 n, const LightBVHData node) {
	float3 pc = (node.BBMax + node.BBMin) / 2.0f;
	float d2 = dot(p - pc, p - pc);
	d2 = max(d2, length(node.BBMax - node.BBMin) / 2.0f);

	float3 wi = normalize(p - pc);
	float cosTheta_w = abs(dot(node.w, wi));
	float sinTheta_w = sqrt(max(1.0f - cosTheta_w * cosTheta_w,0));

	float cosTheta_b = BoundSubtendedDirections(node.BBMax, node.BBMin, p);
	float sinTheta_b = sqrt(max(1.0f - cosTheta_b * cosTheta_b,0));

	float sinTheta_o = sqrt(max(1.0f - node.cosTheta_o * node.cosTheta_o,0));
	float cosTheta_x = cosSubClamped(sinTheta_w, cosTheta_w, sinTheta_o, node.cosTheta_o);
	float sinTheta_x = sinSubClamped(sinTheta_w, cosTheta_w, sinTheta_o, node.cosTheta_o);
	float cosThetap = cosSubClamped(sinTheta_x, cosTheta_x, sinTheta_b, cosTheta_b);
	if(cosThetap <= node.cosTheta_e) return 0;

	float importance = node.phi * cosThetap / d2;

	float cosTheta_i = abs(dot(wi, n));
	float sinTheta_i = sqrt(max(1.0f - cosTheta_i * cosTheta_i,0));
	float cosThetap_i = cosSubClamped(sinTheta_i, cosTheta_i, sinTheta_b, cosTheta_b);
	importance *= cosThetap_i;

	return importance;// / (float)max(node.LightCount,1);
}

int CalcInside(LightBVHData A, LightBVHData B, float3 p, int Index) {
	bool Residency0 = all(p <= A.BBMax) && all(p >= A.BBMin);
	bool Residency1 = all(p <= B.BBMax) && all(p >= B.BBMin);
	if(Residency0 && Residency1) {
		return Index + 2;
	} else if(Residency0) {
		return 0;
	} else if(Residency1) {
		return 1;
	} else return -1;
}

void CalcLightPDF(inout float pmf, float3 p, float3 p2, float3 n, int pixel_index, int MeshIndex) {
	int node_index = 0;
	int Reps = 0;
	bool HasHitTLAS = false;
	int NodeOffset = 0;
	int StartIndex = 0;
	float2 stack[32];
	int stacksize = 0;
	while(Reps < 100) {
		Reps++;
		LightBVHData node = LightNodes[node_index];
		if(node.isLeaf == 0) {
			float2 ci = float2(
				Importance(p, n, LightNodes[node.left + NodeOffset]),
				Importance(p, n, LightNodes[node.left + 1 + NodeOffset])
			);
			// if(ci.x == 0 && ci.y == 0) {pmf = -1; return;}

			int Index = CalcInside(LightNodes[node.left + NodeOffset], LightNodes[node.left + NodeOffset + 1], p2, random(264 + Reps, pixel_index).x > (ci.x / (ci.x + ci.y)));
			if(Index == -1) {
				if(stacksize == 0) {return;}
				float2 tempstack = stack[--stacksize];
				node_index = tempstack.x;
				pmf = tempstack.y;
				continue;
			}
			if(Index >= 2) {
				Index -= 2;
				stack[stacksize++] = float2(node.left + NodeOffset + !Index, pmf * ((ci[!Index] / (ci.x + ci.y))));
			}
			pmf *= ((ci[Index] / (ci.x + ci.y)));
			node_index = node.left + Index + NodeOffset;
		} else {
			if(HasHitTLAS) {
        		// if(CurBounce == 0) _DebugTex[int2(pixel_index % screen_width, pixel_index / screen_width)] = pmf / 12000.0f;
				if(node_index > 0 || Importance(p, n, node) > 0) return;	
			} else {
				StartIndex = _LightMeshes[node.left].StartIndex;
				// if(_LightMeshes[node.left].LockedMeshIndex != MeshIndex) return -1; 
				p = mul(_MeshData[MeshIndex].W2L, float4(p,1));
				p2 = mul(_MeshData[MeshIndex].W2L, float4(p2,1));
			    float3x3 Inverse = (float3x3)inverse(_MeshData[MeshIndex].W2L);
			    float scalex = length(mul(Inverse, float3(1,0,0)));
			    float scaley = length(mul(Inverse, float3(0,1,0)));
			    float scalez = length(mul(Inverse, float3(0,0,1)));
			    float3 Scale = pow(rcp(float3(scalex, scaley, scalez)),2);
				n = normalize(mul(_MeshData[MeshIndex].W2L, float4(n,0)).xyz / Scale);
				NodeOffset = _MeshData[MeshIndex].LightNodeOffset;
				node_index = NodeOffset;
				HasHitTLAS = true;
			}
		}
	}

	return;
}

int SampleLightBVH(float3 p, float3 n, inout float pmf, int pixel_index, inout int MeshIndex) {
	int node_index = 0;
	int Reps = 0;
	bool HasHitTLAS = false;
	int NodeOffset = 0;
	int StartIndex = 0;
	while(Reps < 122) {
		Reps++;
		LightBVHData node = LightNodes[node_index];
		[branch]if(node.isLeaf == 0) {
			const float2 ci = float2(
				Importance(p, n, LightNodes[node.left + NodeOffset]),
				Importance(p, n, LightNodes[node.left + 1 + NodeOffset])
			);
			if(ci.x == 0 && ci.y == 0) break;

			bool Index = random(264 + Reps, pixel_index).x > (ci.x / (ci.x + ci.y));
			pmf /= ((ci[Index] / (ci.x + ci.y)));
			node_index = node.left + Index + NodeOffset;
		} else {
			[branch]if(HasHitTLAS) {
				if(node_index > 0 || Importance(p, n, node) > 0) return node.left + StartIndex;	
			} else {
				StartIndex = _LightMeshes[node.left].StartIndex; 
				MeshIndex = _LightMeshes[node.left].LockedMeshIndex;
				p = mul(_MeshData[MeshIndex].W2L, float4(p,1));
			    float3x3 Inverse = (float3x3)inverse(_MeshData[MeshIndex].W2L);
			    float scalex = length(mul(Inverse, float3(1,0,0)));
			    float scaley = length(mul(Inverse, float3(0,1,0)));
			    float scalez = length(mul(Inverse, float3(0,0,1)));
			    float3 Scale = pow(rcp(float3(scalex, scaley, scalez)),2);
				n = normalize(mul(_MeshData[MeshIndex].W2L, float4(n,0)).xyz / Scale);
				NodeOffset = _MeshData[MeshIndex].LightNodeOffset;
				node_index = NodeOffset;
				HasHitTLAS = true;
			}
		}
	}
	return -1;
}





float3 LoadSurfaceInfo(int2 id) {
    uint4 Target = PrimaryTriData[id.xy];
    MyMeshDataCompacted Mesh = _MeshData[Target.x];
    Target.y += Mesh.TriOffset;
    float2 TriUV;
    TriUV.x = asfloat(Target.z);
    TriUV.y = asfloat(Target.w);
    float4x4 Inverse = inverse(Mesh.W2L);
    return mul(Inverse, float4(AggTris[Target.y].pos0 + TriUV.x * AggTris[Target.y].posedge1 + TriUV.y * AggTris[Target.y].posedge2,1)).xyz;
}


void Unity_Hue_Degrees_float(float3 In, float Offset, out float3 Out)
{
    // RGB to HSV
    float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
    float4 P = lerp(float4(In.bg, K.wz), float4(In.gb, K.xy), step(In.b, In.g));
    float4 Q = lerp(float4(P.xyw, In.r), float4(In.r, P.yzx), step(P.x, In.r));
    float D = Q.x - min(Q.w, Q.y);
    float E = 1e-10;
    float V = (D == 0) ? Q.x : (Q.x + E);
    float3 hsv = float3(abs(Q.z + (Q.w - Q.y)/(6.0 * D + E)), D / (Q.x + E), V);

    float hue = hsv.x + Offset / 360;
    hsv.x = (hue < 0)
            ? hue + 1
            : (hue > 1)
                ? hue - 1
                : hue;

    // HSV to RGB
    float4 K2 = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
    float3 P2 = abs(frac(hsv.xxx + K2.xyz) * 6.0 - K2.www);
    Out = hsv.z * lerp(K2.xxx, saturate(P2 - K2.xxx), hsv.y);
}

void Unity_Saturation_float(float3 In, float Saturation, out float3 Out)
{
    float luma = dot(In, float3(0.2126729, 0.7151522, 0.0721750));
    Out =  luma.xxx + Saturation.xxx * (In - luma.xxx);
}

void Unity_Saturation_float(inout float3 In, float Saturation)
{
    float luma = dot(In, float3(0.2126729, 0.7151522, 0.0721750));
    In =  luma.xxx + Saturation.xxx * (In - luma.xxx);
}

void Unity_Contrast_float(float3 In, float Contrast, out float3 Out)
{
    float midpoint = pow(0.5, 2.2);
    Out =  (In - midpoint) * Contrast + midpoint;
}

float3 DeSat(float3 In, float Saturation)
{
    float luma = dot(In, float3(0.2126729, 0.7151522, 0.0721750));
    return  luma.xxx + Saturation.xxx * (In - luma.xxx);
}





#define LAYERS            5.0

#define PI                3.141592654
#define TAU               (2.0*PI)
#define TIME              fmod(iTime, 30.0)
#define TTIME             (TAU*TIME)
#define RESOLUTION        iResolution
#define ROT(a)            float2x2(cos(a), sin(a), -sin(a), cos(a))

// License: Unknown, author: nmz (twitter: @stormoid), found: https://www.shadertoy.com/view/NdfyRM
float sRGB(float t) { return lerp(1.055*pow(t, 1./2.4) - 0.055, 12.92*t, step(t, 0.0031308)); }
// License: Unknown, author: nmz (twitter: @stormoid), found: https://www.shadertoy.com/view/NdfyRM
float3 sRGB(in float3 c) { return float3 (sRGB(c.x), sRGB(c.y), sRGB(c.z)); }

// License: Unknown, author: Matt Taylor (https://github.com/64), found: https://64.github.io/tonemapping/
float3 aces_approx(float3 v) {
  v = max(v, 0.0);
  v *= 0.6f;
  float a = 2.51f;
  float b = 0.03f;
  float c = 2.43f;
  float d = 0.59f;
  float e = 0.14f;
  return clamp((v*(a*v+b))/(v*(c*v+d)+e), 0.0f, 1.0f);
}

// License: Unknown, author: Unknown, found: don't remember
float tanh_approx(float x) {
  //  Found this somewhere on the interwebs
  //  return tanh(x);
  float x2 = x*x;
  return clamp(x*(27.0 + x2)/(27.0+9.0*x2), -1.0, 1.0);
}


// License: WTFPL, author: sam hocevar, found: https://stackoverflow.com/a/17897228/418488
const float4 hsv2rgb_K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
float3 hsv2rgb(float3 c) {
  float3 p = abs(frac(c.xxx + hsv2rgb_K.xyz) * 6.0 - hsv2rgb_K.www);
  return c.z * lerp(hsv2rgb_K.xxx, clamp(p - hsv2rgb_K.xxx, 0.0, 1.0), c.y);
}

// License: MIT OR CC-BY-NC-4.0, author: mercury, found: https://mercury.sexy/hg_sdf/
float2 mod2(inout float2 p, float2 size) {
  float2 c = floor((p + size*0.5)/size);
  p = fmod(p + size*0.5,size) - size*0.5;
  return c;
}

// License: Unknown, author: Unknown, found: don't remember
float2 hash2(float2 p) {
  p = float2(dot (p, float2 (127.1, 311.7)), dot (p, float2 (269.5, 183.3)));
  return frac(sin(p)*43758.5453123);
}

float2 shash2(float2 p) {
  return -1.0+2.0*hash2(p);
}

float3 toSpherical(float3 p) {
  float r   = length(p);
  float t   = acos(p.z/r);
  float ph  = atan(p.y / p.x);
  return float3(r, t, ph);
}


// License: CC BY-NC-SA 3.0, author: Stephane Cuillerdier - Aiekick/2015 (twitter:@aiekick), found: https://www.shadertoy.com/view/Mt3GW2
float3 blackbody(float Temp) {
  float3 col = 255;
  col.x = 56100000. * pow(Temp,(-3. / 2.)) + 148.;
  col.y = 100.04 * log(Temp) - 623.6;
  if (Temp > 6500.) col.y = 35200000. * pow(Temp,(-3. / 2.)) + 184.;
  col.z = 194.18 * log(Temp) - 1448.6;
  col = clamp(col, 0., 255.)/255.;
  if (Temp < 1000.) col *= Temp/1000.;
  return col;
}


// License: MIT, author: Inigo Quilez, found: https://www.shadertoy.com/view/XslGRr
float noise(float2 p) {
  // Found at https://www.shadertoy.com/view/sdlXWX
  // Which then redirected to IQ shader
  float2 i = floor(p);
  float2 f = frac(p);
  float2 u = f*f*(3.-2.*f);
  
  float n =
         lerp( lerp( dot(shash2(i + float2(0.,0.) ), f - float2(0.,0.)), 
                   dot(shash2(i + float2(1.,0.) ), f - float2(1.,0.)), u.x),
              lerp( dot(shash2(i + float2(0.,1.) ), f - float2(0.,1.)), 
                   dot(shash2(i + float2(1.,1.) ), f - float2(1.,1.)), u.x), u.y);

  return 2.0*n;              
}

float fbm(float2 p, float o, float s, int iters) {
  p *= s;
  p += o;

  const float aa = 0.5;
  const float2x2 pp = 2.04*ROT(1.0);

  float h = 0.0;
  float a = 1.0;
  float d = 0.0;
  for (int i = 0; i < iters; ++i) {
    d += a;
    h += a*noise(p);
    p += float2(10.7, 8.3);
    p = mul(p, pp);
    a *= aa;
  }
  h /= d;
  
  return h;
}

float height(float2 p) {
  float h = fbm(p, 0.0, 5.0, 5);
  h *= 0.3;
  h += 0.0;
  return (h);
}

float3 stars(float3 ro, float3 rd, float2 sp, float hh) {
  float3 col = 0;
  
  const float m = LAYERS;
  hh = tanh_approx(20.0*hh);

  for (float i = 0.0; i < m; ++i) {
    float2 pp = sp+0.5*i;
    float s = i/(m-1.0);
    float2 dim  = (lerp(0.05, 0.003, s)*PI);
    float2 np = mod2(pp, dim);
    float2 h = hash2(np+127.0+i);
    float2 o = -1.0+2.0*h;
    float y = sin(sp.x);
    pp += o*dim*0.5;
    pp.y *= y;
    float l = length(pp);
  
    float h1 = frac(h.x*1667.0);
    float h2 = frac(h.x*1887.0);
    float h3 = frac(h.x*2997.0);

    float3 scol = lerp(8.0*h2, 0.25*h2*h2, s)*blackbody(lerp(3000.0, 22000.0, h1*h1));

    float3 ccol = col + exp(-(lerp(6000.0, 2000.0, hh)/lerp(2.0, 0.25, s))*max(l-0.001, 0.0))*scol;
    col = h3 < y ? ccol : col;
  }
  
  return col;
}

bool GetFlag(int FlagVar, int flag) {
    return (((int)FlagVar >> flag) & (int)1) == 1;
}