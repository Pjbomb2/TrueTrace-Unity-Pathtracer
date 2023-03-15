#include "UnityCG.cginc"
#include "../GlobalDefines.cginc"
float4x4 _CameraInverseProjection;
float4x4 _CameraProjection;
float4x4 ViewMatrix;
int frames_accumulated;
uniform int CurBounce;
uniform int MaxBounce;

static const float PI = 3.14159265f;
uint screen_width;
uint screen_height;
uint TargetWidth;
uint TargetHeight;
uniform bool UseAlteredPipeline;
uniform bool DoHeightmap;

int lighttricount;

int unitylightcount;

bool UseRussianRoulette;
bool UseNEE;
bool UseDoF;
bool UseRestir;
bool UsePermutatedSamples;
bool DoCheckerboarding;

bool DoVoxels;
int VoxelOffset;

float3 Up;
float3 Right;
float3 Forward;
float focal_distance;
float AperatureRadius;

RWStructuredBuffer<uint3> BufferData;

RWTexture2D<float> CorrectedDepthTex;

#ifdef HardwareRT
	StructuredBuffer<int> SubMeshOffsets;
	StructuredBuffer<float2> MeshOffsets;
#endif

struct BufferSizeData {
	int tracerays;
	int rays_retired;
	int brickmap_rays_retired;
	int shade_rays;
	int shadow_rays;
	int shadow_rays_retired;
	int brickmap_shadow_rays_retired;
	int heighmap_rays_retired;
};

RWStructuredBuffer<BufferSizeData> BufferSizes;


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

struct RayHit {
	float t;
	float u, v;
	int mesh_id;
	int triangle_id;
};

struct RayData {//128 bit aligned
	float3 origin;
	float3 direction;

	uint4 hits;
	uint PixelIndex;//need to bump this back down to uint1
	int HitVoxel;//need to shave off 4 bits
	float last_pdf;
	int PrevIndex;//Need for padding, slightly increases performance
};

struct ShadowRayData {
	float3 origin;
	float3 direction;
	float3 illumination;
	float3 RadianceIncomming;
	uint PixelIndex;
	float t;
	bool PrimaryNEERay;
	float LuminanceIncomming;
};

struct LightTriangleData {
	float3 pos0;
	float3 posedge1;
	float3 posedge2;
	float3 Norm;
	float2 UV1;
	float2 UV2;
	float2 UV3;
	int MatIndex;
	float3 radiance;
	float sumEnergy;
	float pdf;
	float area;
};

StructuredBuffer<LightTriangleData> LightTriangles;


struct ColData {
	float3 throughput;
	float3 Direct;
	float3 Indirect;
	uint PrimaryNEERay;
	int IsSpecular;
	float pad;
};

struct SHData {
	float4 shY;
	float2 CoCg;
};

struct GIReservoir {
	float W;
	int M;
	float3 SecondaryHitPosition;
	uint BaseColor;
	int HistoricFrame;
	int MaterialIndex;
	int ThisCase;
	int HistoricID;
	float LuminanceIncomming;
	float3 NEEPosition;
	uint PrimaryNormal;
};
RWStructuredBuffer<GIReservoir> CurrentReservoirGI;
StructuredBuffer<GIReservoir> PreviousReservoirGI;

struct ScreenSpaceData {
	float Specular;
	float Roughness;
	float Clearcoat;
	float ClearCoatGloss;
	float Metallic;
	float3 Albedo;
	float ior;
	int Normal;
	float3 Position;
	float t;
	float3 InitialRayDirection;
	int NormNormal;
	float ReflectedT;
};
RWStructuredBuffer<ScreenSpaceData> ScreenSpaceInfo;
RWStructuredBuffer<ScreenSpaceData> MatModifiersPrev;

RWStructuredBuffer<SHData> SH;

int curframe;
RWTexture2D<float4> Result;

RWStructuredBuffer<ShadowRayData> ShadowRaysBuffer;
RWStructuredBuffer<RayData> GlobalRays1;
RWStructuredBuffer<ColData> GlobalColors;
StructuredBuffer<ColData> GlobalColors2;
RWStructuredBuffer<ColData> GlobalColors3;
RWStructuredBuffer<Ray> Rays;

#ifdef HDRP
Texture2DArray<float2> MotionVectors;
Texture2DArray<float3> NormalTex;
Texture2DArray<float> Depth;
#else
Texture2D<float2> MotionVectors;
Texture2D<float3> NormalTex;
Texture2D<float> Depth;
#endif
SamplerState sampler_NormalTex;
Texture2D<float4> AlbedoTex;
SamplerState sampler_AlbedoTex;
RWTexture2D<int> TempNormTex;
RWTexture2D<float4>TempAlbedoTex;
RWTexture2D<float4> RandomNumsWrite;
Texture2D<float4> RandomNums;
RWTexture2D<int> RenderMaskTex;
SamplerState sampler_MotionVectors;
RWTexture2D<float4> _DebugTex;

static const float ONE_OVER_PI = 0.318309886548;
static const float EPSILON = 1e-8;

struct MyMeshDataCompacted {
	float4x4 Transform;
	float4x4 Inverse;
	int TriOffset;
	int NodeOffset;
	int MaterialOffset;
	int mesh_data_bvh_offsets;//could I convert this an int4?
	uint IsVoxel;
	int3 Size;
	int LightTriCount;
	float LightPDF;
};

struct BVHNode8Data {
	float3 node_0xyz;
	uint node_0w;
	uint4 node_1;
	uint4 node_2;
	uint4 node_3;
	uint4 node_4;
};

StructuredBuffer<BVHNode8Data> cwbvh_nodes;
StructuredBuffer<BVHNode8Data> VoxelTLAS;
StructuredBuffer<MyMeshDataCompacted> _MeshData;


struct TrianglePos {
	float3 pos0;
	float3 posedge1;
	float3 posedge2;
};

inline TrianglePos triangle_get_positions(const int ID) {
	TrianglePos tri;
	tri.pos0 = AggTris[ID].pos0;
	tri.posedge1 = AggTris[ID].posedge1;
	tri.posedge2 = AggTris[ID].posedge2;
	return tri;
}

Texture2D<float> AlphaAtlas;
SamplerState sampler_clamp_point;

struct MaterialData {//56
	float4 AlbedoTex;//16
	float4 NormalTex;//32
	float4 EmissiveTex;//48
	float4 MetallicTex;//64
	float4 RoughnessTex;//80
	int HasAlbedoTex;//81
	int HasNormalTex;//82
	int HasEmissiveTex;//83
	int HasMetallicTex;//84
	int HasRoughnessTex;//85
	float3 BaseColor;
	float emmissive;
	float3 EmissionColor;
	float roughness;
	int MatType;
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
	int Thin;
	float Specular;
	float2 TextureScale;
	float2 TextureOffset;
};


StructuredBuffer<MaterialData> _Materials;

struct Reservoir {
    float y;
    float wsum;
    float M;
    float W;
    float3 Radiance;
    float3 Position;
    float3 Norm;
    float MeshIndex;
    float3 PrevWorld;
    float3 PrevNorm;
    float3 FirstBounceHitPosition;
};

Texture2D<float> MetallicTex;
Texture2D<float> RoughnessTex;

RWStructuredBuffer<Reservoir> CurrentReservoir;
RWStructuredBuffer<Reservoir> PreviousReservoir;

Texture2D<half> Heightmap;
SamplerState sampler_trilinear_clamp;

struct TerrainData {
    float3 PositionOffset;
    float HeightScale;
    float TerrainDim;
    float4 AlphaMap;
    float4 HeightMap;
    int MatOffset;
};

StructuredBuffer<TerrainData> Terrains;

int TerrainCount;
uniform bool TerrainExists;
uniform bool DoWRS;
inline float luminance(const float3 a) {
    return dot(float3(0.299f, 0.587f, 0.114f), a);
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
	hit.triangle_id = 0;
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


uint packUnormArb(float3 data2) {
	data2 = (data2 + 1.0f) * 0.5f;
	const uint4 bits = uint4(10, 10, 10, 0);
	const float4 data = float4(data2, 0);
	float4 mull = exp2(float4(bits)) - 1.0;

	uint4 shift = uint4(0, bits.x, bits.x + bits.y, bits.x + bits.y + bits.z);
	uint4 shifted = uint4(data * mull + 0.5) << shift;

	return shifted.x | shifted.y | shifted.z | shifted.w;
}

float3 unpackUnormArb(const uint pack) {
	const uint4 bits = uint4(10, 10, 10, 0);
	uint4 maxValue = uint4(exp2(bits) - 1);
	uint4 shift = uint4(0, bits.x, bits.x + bits.y, bits.x + bits.y + bits.z);
	uint4 unshifted = pack >> shift;
	unshifted = unshifted & maxValue;

	return normalize((float4(unshifted).xyz * 1.0 / float4(maxValue).xyz).xyz * 2.0f - 1.0f);
}

uniform bool UseASVGF;
uniform int ReSTIRGIUpdateRate;
bool UseReSTIRGI;

float2 random(uint samdim, uint pixel_index) {
	[branch] if (UseASVGF || ReSTIRGIUpdateRate != 0) {
		uint2 pixid = uint2(pixel_index % screen_width, pixel_index / screen_width);
		uint hash = pcg_hash(((uint)RandomNums[pixid].y * (uint)112 + samdim) * (MaxBounce + 1) + CurBounce);

		const static float one_over_max_unsigned = asfloat(0x2f7fffff);


		float x = hash_with((uint)RandomNums[pixid].x, hash) * one_over_max_unsigned;
		float y = hash_with((uint)RandomNums[pixid].x + 0xdeadbeef, hash) * one_over_max_unsigned;

		return float2(x, y);
	}
	else {
		uint hash = pcg_hash((pixel_index * (uint)204 + samdim) * (MaxBounce + 1) + CurBounce);

		const static float one_over_max_unsigned = asfloat(0x2f7fffff);


		float x = hash_with(frames_accumulated, hash) * one_over_max_unsigned;
		float y = hash_with(frames_accumulated + 0xdeadbeef, hash) * one_over_max_unsigned;

		return float2(x, y);
	}
}



void set(int index, const RayHit ray_hit) {
	uint uv = (int)(ray_hit.u * 65535.0f) | ((int)(ray_hit.v * 65535.0f) << 16);

	GlobalRays1[index].hits = uint4(ray_hit.mesh_id, ray_hit.triangle_id, asuint(ray_hit.t), uv);
}

RayHit get(int index) {
	const uint4 hit = GlobalRays1[index].hits;

	RayHit ray_hit;

	ray_hit.mesh_id = hit.x;
	ray_hit.triangle_id = hit.y;

	ray_hit.t = asfloat(hit.z);

	ray_hit.u = (float)(hit.w & 0xffff) / 65535.0f;
	ray_hit.v = (float)(hit.w >> 16) / 65535.0f;

	return ray_hit;
}



uint packRGBE(float3 v)
{
	float3 va = max(0, v);
	float max_abs = max(va.r, max(va.g, va.b));
	if (max_abs == 0)
		return 0;

	float exponent = floor(log2(max_abs));

	uint result;
	result = uint(clamp(exponent + 20, 0, 31)) << 27;

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

float3 project_SH_irradiance(SHData sh, float3 N)
{
	float d = dot(sh.shY.xyz, N);
	float Y = 2.0 * (1.023326 * d + 0.886226 * sh.shY.w);
	Y = max(Y, 0.0);

	sh.CoCg *= Y * 0.282095 / (sh.shY.w + 1e-6);

	float   T = Y - sh.CoCg.y * 0.5;
	float   G = sh.CoCg.y + T;
	float   B = T - sh.CoCg.x * 0.5;
	float   R = B + sh.CoCg.x;

	return max(float3(R, G, B), 0.0);
}

SHData irradiance_to_SH(float3 color, float3 dir)
{
	SHData result;

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

float3 SH_to_irradiance(SHData sh)
{
	float   Y = sh.shY.w / 0.282095;

	float   T = Y - sh.CoCg.y * 0.5;
	float   G = sh.CoCg.y + T;
	float   B = T - sh.CoCg.x * 0.5;
	float   R = B + sh.CoCg.x;

	return max(float3(R, G, B), 0.0);
}

SHData init_SH()
{
	SHData result;
	result.shY = 0;
	result.CoCg = 0;
	return result;
}

void accumulate_SH(inout SHData accum, SHData b, float scale)
{
	accum.shY += b.shY * scale;
	accum.CoCg += b.CoCg * scale;
}

SHData mix_SH(SHData a, SHData b, float s)
{
	SHData result;
	result.shY = lerp(a.shY, b.shY, s);
	result.CoCg = lerp(a.CoCg, b.CoCg, s);
	return result;
}

Ray CreateCameraRay(float2 uv, uint pixel_index) {
	// Transform the camera origin to world space
	float3 origin = mul(unity_CameraToWorld, float4(0.0f, 0.0f, 0.0f, 1.0f)).xyz;

	// Invert the perspective projection of the view-space position
	float3 direction = mul(_CameraInverseProjection, float4(uv, 0.0f, 1.0f)).xyz;
	// Transform the direction from camera to world space and normalize
	direction = mul(unity_CameraToWorld, float4(direction, 0.0f)).xyz;
	direction = normalize(direction);
	[branch] if (UseDoF) {
		float3 cameraForward = mul(_CameraInverseProjection, float4(0, 0, 0.0f, 1.0f)).xyz;
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
		sensorPos = mul(unity_CameraToWorld, float4(cameraSpaceSensorPos, 1.0f)).xyz;

		float angle = random(6, pixel_index).x * 2.0f * PI;
		float radius = sqrt(random(6, pixel_index).y);
		float2 offset = float2(cos(angle), sin(angle)) * radius * AperatureRadius;

		float3 p = origin + direction * (focal_distance);

		float3 aperturePos = origin + Right * offset.x + Up * offset.y;

		origin = aperturePos;
		direction = normalize(p - origin);
	}


	return CreateRay(origin, direction);
}


float GetHeight(float3 CurrentPos, const TerrainData Terrain) {
    CurrentPos -= Terrain.PositionOffset;
    float3 b = float3(Terrain.TerrainDim, 0.1f, Terrain.TerrainDim);
    float3 q = (abs(CurrentPos) - b);
    q.x /= Terrain.TerrainDim;
    q.z /= Terrain.TerrainDim;
    float2 uv = float2(min(CurrentPos.x / Terrain.TerrainDim, b.x / Terrain.TerrainDim), min(CurrentPos.z / Terrain.TerrainDim, b.z / Terrain.TerrainDim));
    float h = Heightmap.SampleLevel(sampler_trilinear_clamp, uv * (Terrain.HeightMap.xy - Terrain.HeightMap.zw) + Terrain.HeightMap.zw, 0).x;
    h *= Terrain.HeightScale * 2;
    q.y -= h;
    // q = max(0,q);
    return q.y;//length(q);
}

Texture2D<float4> VideoTex;
SamplerState sampler_VideoTex;

inline uint ray_get_octant_inv4(const float3 ray_direction) {
    return
        (ray_direction.x < 0.0f ? 0 : 0x04040404) |
        (ray_direction.y < 0.0f ? 0 : 0x02020202) |
        (ray_direction.z < 0.0f ? 0 : 0x01010101);
}

inline bool triangle_intersect_shadow(int tri_id, const Ray ray, float max_distance, int mesh_id) {
    TrianglePos tri = triangle_get_positions(tri_id);

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
                int MaterialIndex = (_MeshData[mesh_id].MaterialOffset + AggTris[tri_id].MatDat);
                if(_Materials[MaterialIndex].MatType == CutoutIndex) {
                    float2 BaseUv = AggTris[tri_id].tex0 * (1.0f - u - v) + AggTris[tri_id].texedge1 * u + AggTris[tri_id].texedge2 * v;
                    float2 Uv = fmod(BaseUv + 100.0f, float2(1.0f, 1.0f)) * (_Materials[MaterialIndex].AlbedoTex.xy - _Materials[MaterialIndex].AlbedoTex.zw) + _Materials[MaterialIndex].AlbedoTex.zw;
                    if(AlphaAtlas.SampleLevel(sampler_clamp_point, Uv, 0) < 0.0001f) return false;
                }
	            #ifdef VideoIncludedInAlphaMapping
	                if(_Materials[MaterialIndex].MatType == VideoIndex) {
	                    float2 BaseUv = AggTris[tri_id].tex0 * (1.0f - u - v) + AggTris[tri_id].texedge1 * u + AggTris[tri_id].texedge2 * v;
	                    if(VideoTex.SampleLevel(sampler_VideoTex, BaseUv, 0).w < 0.1f) return false;
	                }
	            #endif
            #endif
            #ifdef IgnoreGlassShadow
                if(_Materials[MaterialIndex].specTrans == 1) return false;
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
            uint meta4 = asuint(i == 0 ? node_1.z : node_1.w);

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

                tmin3 = tmin3 * adjusted_ray_direction_inv + adjusted_ray_origin;
                tmax3 = tmax3 * adjusted_ray_direction_inv + adjusted_ray_origin;

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
bool VisabilityCheck(Ray ray, float dist) {

	uint2 stack[24];
	int stack_size = 0;
	uint ray_index;
	uint2 current_group;

	uint oct_inv4;
	int tlas_stack_size;
	int mesh_id;
	float max_distance;
	Ray ray2;

	bool inactive = stack_size == 0 && current_group.y == 0;

	ray.direction_inv = rcp(ray.direction);
	ray2 = ray;

	oct_inv4 = ray_get_octant_inv4(ray.direction);

	current_group.x = (uint)0;
	current_group.y = (uint)0x80000000;

	max_distance = dist;

	tlas_stack_size = -1;

	while (true) {//Traverse Accelleration Structure(Compressed Wide Bounding Volume Hierarchy)            
		uint2 triangle_group;
		if (current_group.y & 0xff000000) {
			uint hits_imask = current_group.y;
			uint child_index_offset = firstbithigh(hits_imask);
			uint child_index_base = current_group.x;

			current_group.y &= ~(1 << child_index_offset);

			if (current_group.y & 0xff000000) {
				stack[stack_size++] = current_group;
			}
			uint slot_index = (child_index_offset - 24) ^ (oct_inv4 & 0xff);
			uint relative_index = countbits(hits_imask & ~(0xffffffff << slot_index));
			uint child_node_index = child_index_base + relative_index;

			float3 node_0 = cwbvh_nodes[child_node_index].node_0xyz;
			uint node_0w = cwbvh_nodes[child_node_index].node_0w;

			uint4 node_1 = cwbvh_nodes[child_node_index].node_1;
			uint4 node_2 = cwbvh_nodes[child_node_index].node_2;
			uint4 node_3 = cwbvh_nodes[child_node_index].node_3;
			uint4 node_4 = cwbvh_nodes[child_node_index].node_4;

			uint hitmask = cwbvh_node_intersect(ray, oct_inv4, max_distance, node_0, node_0w, node_1, node_2, node_3, node_4);

			uint imask = (node_0w >> (3 * 8)) & 0xff;

			current_group.x = asuint(node_1.x) + ((tlas_stack_size == -1) ? 0 : _MeshData[mesh_id].NodeOffset);
			triangle_group.x = asuint(node_1.y) + ((tlas_stack_size == -1) ? 0 : _MeshData[mesh_id].TriOffset);

			current_group.y = (hitmask & 0xff000000) | (uint)(imask);
			triangle_group.y = (hitmask & 0x00ffffff);
		}
		else {
			triangle_group.x = current_group.x;
			triangle_group.y = current_group.y;
			current_group.x = (uint)0;
			current_group.y = (uint)0;
		}

		bool hit = false;

		while (triangle_group.y != 0) {
			if (tlas_stack_size == -1) {//Transfer from Top Level Accelleration Structure to Bottom Level Accelleration Structure
				uint mesh_offset = firstbithigh(triangle_group.y);
				triangle_group.y &= ~(1 << mesh_offset);

				mesh_id = triangle_group.x + mesh_offset;

				if (triangle_group.y != 0) {
					stack[stack_size++] = triangle_group;
				}

				if (current_group.y & 0xff000000) {
					stack[stack_size++] = current_group;
				}
				tlas_stack_size = stack_size;

				int root_index = (_MeshData[mesh_id].mesh_data_bvh_offsets & 0x7fffffff);

				ray.direction = (mul(_MeshData[mesh_id].Transform, float4(ray.direction, 0))).xyz;
				ray.origin = (mul(_MeshData[mesh_id].Transform, float4(ray.origin, 1))).xyz;
				ray.direction_inv = rcp(ray.direction);

				oct_inv4 = ray_get_octant_inv4(ray.direction);

				current_group.x = (uint)root_index;
				current_group.y = (uint)0x80000000;

				break;
			}
			else {
				uint triangle_index = firstbithigh(triangle_group.y);
				triangle_group.y &= ~(1 << triangle_index);

				if (triangle_intersect_shadow(triangle_group.x + triangle_index, ray, max_distance, mesh_id)) {
					hit = true;
					break;
				}
			}
		}

		if (hit) {
			return false;
		}

		if ((current_group.y & 0xff000000) == 0) {
			if (stack_size == 0) {//thread has finished traversing
				return true;
			}

			if (stack_size == tlas_stack_size) {
				tlas_stack_size = -1;
				ray = ray2;
				oct_inv4 = ray_get_octant_inv4(ray.direction);
			}
			current_group = stack[--stack_size];
		}
	}
}




float2 sample_triangle(float u1, float u2) {
	if (u2 > u1) {
		u1 *= 0.5f;
		u2 -= u1;
	}
	else {
		u2 *= 0.5f;
		u1 -= u2;
	}
	return float2(u1, u2);
}

TrianglePos triangle_get_positions2(int ID) {
    TrianglePos tri;
    tri.pos0 = LightTriangles[ID].pos0;
    tri.posedge1 = LightTriangles[ID].posedge1;
    tri.posedge2 = LightTriangles[ID].posedge2;
    return tri;
}


int LightMeshCount;

struct LightMeshData {
	float4x4 Inverse;
	float3 Center;
	float pdf;
	float CDF;
	int StartIndex;
	int IndexEnd;
	int MatOffset;
};
StructuredBuffer<LightMeshData> _LightMeshes;

struct LightData {
	float3 Radiance;
	float3 Position;
	float3 Direction;
	float pdf;
	float CDF;
	int Type;
	float2 SpotAngle;
	float ZAxisRotation;
};
StructuredBuffer<LightData> _UnityLights;





bool evaldiffuse(const float3 to_light, float cos_theta_o, inout float3 bsdf, inout float pdf) {
	if (cos_theta_o <= 0.0f) return false;

	bsdf = float3(cos_theta_o * ONE_OVER_PI, cos_theta_o * ONE_OVER_PI, cos_theta_o * ONE_OVER_PI);
	pdf = cos_theta_o * ONE_OVER_PI;

	return (pdf > 0 || pdf < 0 || pdf == 0);
}
struct HitMat {
	float3 surfaceColor;
	float emmis;
	float roughness;
	uint MatType;
	float relativeIOR;
	float specularTint;
	float metallic;
	float ior;
	float anisotropic;
	float flatness;
	float specTrans;
	float clearcoat;
	float clearcoatGloss;
	float3 transmittanceColor;
	float3 scatterDistance;
	float phaseFunction;
	float3 extinction;
	float diffTrans;
	float sheen;
	float sheenTint;
	int Thin;
	float Specular;

	bool DoesPassThrough;
};

int SelectLight(int MeshIndex, bool DoSimple, uint pixel_index) {//Need to check these to make sure they arnt simply doing uniform sampling

	const float2 rand_light = random(3, pixel_index);
	const int StartIndex = _LightMeshes[MeshIndex].StartIndex;
	const int IndexEnd = _LightMeshes[MeshIndex].IndexEnd;
	// if(DoSimple) return clamp((rand_light.y * (IndexEnd - StartIndex) + StartIndex), StartIndex, IndexEnd - 1);
	float e = LightTriangles[IndexEnd - 1].sumEnergy * rand_light.x + LightTriangles[StartIndex].pdf;
	int low = StartIndex;
	int high = IndexEnd - 1;
	if (e > LightTriangles[high - 1].pdf + LightTriangles[high - 1].sumEnergy) return high;
	int mid = -1;
	while (low < high) {
		int mid = (low + high) >> 1;
		LightTriangleData tri = LightTriangles[mid];
		if (e < tri.sumEnergy)
			high = mid;
		else if (e > tri.sumEnergy + tri.pdf)
			low = mid + 1;
		else
			return mid;
	}
	//  return mid;
	  // Failed to find a light using importance sampling, pick a random one from the array
	  // NOTE: this is a failsafe, we should never get here!
	return clamp((rand_light.y * (IndexEnd - StartIndex) + StartIndex), StartIndex, IndexEnd - 1);
}
int SelectLight(int MeshIndex, float2 rand_light) {//Need to check these to make sure they arnt simply doing uniform sampling

	const int StartIndex = _LightMeshes[MeshIndex].StartIndex;
	const int IndexEnd = _LightMeshes[MeshIndex].IndexEnd;
	// if(DoSimple) return clamp((rand_light.y * (IndexEnd - StartIndex) + StartIndex), StartIndex, IndexEnd - 1);
	float e = LightTriangles[IndexEnd - 1].sumEnergy * rand_light.x + LightTriangles[StartIndex].pdf;
	int low = StartIndex;
	int high = IndexEnd - 1;
	if (e > LightTriangles[high - 1].pdf + LightTriangles[high - 1].sumEnergy) return high;
	int mid = -1;
	while (low < high) {
		int mid = (low + high) >> 1;
		LightTriangleData tri = LightTriangles[mid];
		if (e < tri.sumEnergy)
			high = mid;
		else if (e > tri.sumEnergy + tri.pdf)
			low = mid + 1;
		else
			return mid;
	}
	//  return mid;
	  // Failed to find a light using importance sampling, pick a random one from the array
	  // NOTE: this is a failsafe, we should never get here!
	return clamp((rand_light.y * (IndexEnd - StartIndex) + StartIndex), StartIndex, IndexEnd - 1);
}

int SelectLightMesh(uint pixel_index) {//Select mesh to sample light from
	if (LightMeshCount == 1) return 0;
	const float2 rand_mesh = random(4, pixel_index);
	return clamp((rand_mesh.y * LightMeshCount), 0, LightMeshCount - 1);
}


float3 ToWorld(float3x3 X, float3 V)
{
	return normalize(mul(V, X));//V.x * X + V.y * Y + V.z * Z;
}

float3 ToLocal(float3x3 X, float3 V)
{
	return normalize(mul(X, V));
}



static uint ScatteringTexRSize = 32;
static uint ScatteringTexMUSize = 128;
static uint ScatteringTexMUSSize = 32;
static uint ScatteringTexNUSize = 8;

static float bottom_radius = 63600.0f;
static float top_radius = 64200.0f;
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
SamplerState linearClampSampler;
SamplerState sampler_scattering_texture_trilinear_clamp;

static float3 rayleigh_scattering = float3(5.85f, 13.558f, 33.10f) * 0.001f;
static float3 mie_scattering = 3.996f * 0.001f;

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

float4 TEX2D(Texture2D<float4> tex, float2 uv)
{
	return tex.SampleLevel(_LinearClamp, uv, 0);
}

float3 GetTransmittanceToTopAtmosphereBoundary(float r, float mu) {
	float2 uv = GetTransmittanceTextureUvFromRMu(r, mu);
	return TEX2D(TransmittanceTex, uv).rgb;
}

float3 GetTransmittance(float r, float mu, float d, bool ray_r_mu_intersects_ground) {

	float r_d = clamp(sqrt(d * d + 2.0f * r * mu * d + r * r), bottom_radius, top_radius);
	float mu_d = clamp((r * mu + d) / r_d, -1.0f, 1.0f);
	if (ray_r_mu_intersects_ground) {
		return min(GetTransmittanceToTopAtmosphereBoundary(r_d, -mu_d) /
			GetTransmittanceToTopAtmosphereBoundary(r, -mu),
			float3(1.0f, 1.0f, 1.0f));

	}
	else {
		return min(GetTransmittanceToTopAtmosphereBoundary(r, mu) /
			GetTransmittanceToTopAtmosphereBoundary(r_d, mu_d),
			float3(1.0f, 1.0f, 1.0f));
	}
}

float3 GetSkyRadiance(
	float3 camera, float3 view_ray, float shadow_length,
	float3 sun_direction, inout float3 transmittance) {
	camera.y += bottom_radius;
	camera.y = max(camera.y, bottom_radius + 1);

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
	}
	else if (r >= top_radius) {
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
		GetTransmittanceToTopAtmosphereBoundary(r, mu);
	float3 single_mie_scattering;
	float3 scattering;
	if (shadow_length == 0.0) {
		scattering = GetCombinedScattering(
			r, mu, mu_s, nu, ray_r_mu_intersects_ground,
			single_mie_scattering);
	}
	else {
		// Case of light shafts (shadow_length is the total length noted l in our
		// paper): we omit the scattering between the camera and the point at
		// distance l, by implementing Eq. (18) of the paper (shadow_transmittance
		// is the T(x,x_s) term, scattering is the S|x_s=x+lv term).
		float d = shadow_length;
		float r_p =
			ClampRadius(sqrt(d * d + 2.0 * r * mu * d + r * r));
		float mu_p = (r * mu + d) / r_p;
		float mu_s_p = (r * mu_s + d * nu) / r_p;

		scattering = GetCombinedScattering(
			r_p, mu_p, mu_s_p, nu, ray_r_mu_intersects_ground,
			single_mie_scattering);
		float3 shadow_transmittance =
			GetTransmittance(r, mu, shadow_length, ray_r_mu_intersects_ground);
		scattering = scattering * shadow_transmittance;
		single_mie_scattering = single_mie_scattering * shadow_transmittance;
	}
	return scattering * RayleighPhaseFunction(nu) + single_mie_scattering *
		MiePhaseFunction(0.8f, nu);
}


float Luminance(float3 c)
{
	return 0.212671 * c.x + 0.715160 * c.y + 0.072169 * c.z;
}
