#define ONE_OVER_PI 0.318309886548f
#define PI 3.14159265f
#define EPSILON 1e-8

#include "CommonStructs.cginc"
#define TOHALF(x) float2(f16tof32(x >> 16), f16tof32(x & 0xFFFF))

bool OIDNGuideWrite;

float4x4 CamToWorld;
float4x4 CamInvProj;
float4x4 CamToWorldPrev;
float4x4 CamInvProjPrev;

float4x4 ViewMatrix;
int MaxBounce;
int CurBounce;

uint screen_width;
uint screen_height;
int frames_accumulated;
int curframe;//might be able to get rid of this
float LEMEnergyScale;

bool UseLightBVH;
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
float3 PrevCamPos;
float3 CamDelta;

//DoF
bool UseDoF;
float focal_distance;
float AperatureRadius;
float3 MousePos;
bool IsFocusing;

RWStructuredBuffer<uint3> BufferData;

RWTexture2D<half2> CorrectedDepthTex;
Texture2D<half2> DepthTexA;
Texture2D<half2> DepthTexB;

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

RWTexture2D<float4> ScreenSpaceInfo;
Texture2D<float4> ScreenSpaceInfoRead;
Texture2D<float4> PrevScreenSpaceInfo;

bool DoExposure;
StructuredBuffer<float> Exposure;



RWTexture2D<half4> ReservoirA;
Texture2D<half4> ReservoirB;

RWTexture2D<uint4> WorldPosA;
Texture2D<uint4> WorldPosB;
RWTexture2D<uint4> WorldPosC;

RWTexture2D<half4> NEEPosA;
Texture2D<half4> NEEPosB;

RWTexture2D<float4> Result;
Texture2D<float> _AlphaAtlas;

RWStructuredBuffer<SmallerRay> Rays;
StructuredBuffer<SmallerRay> Rays2;

Texture2D<uint4> PrimaryTriData;
StructuredBuffer<int> TLASBVH8Indices;

int AlbedoAtlasSize;
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
	tri.pos0 = TOHALF(AggTris.Load(ID).tex0);
	tri.posedge1 = TOHALF(AggTris.Load(ID).texedge1);
	tri.posedge2 = TOHALF(AggTris.Load(ID).texedge2);
	return tri;
}

SamplerState my_linear_clamp_sampler;
SamplerState sampler_trilinear_clamp;
SamplerState my_point_clamp_sampler;

Texture2D<half> SingleComponentAtlas;
RWTexture2D<float4> RandomNumsWrite;
Texture2D<float4> RandomNums;
RWTexture2D<float4> _DebugTex;

Texture2D<half4> _TextureAtlas;
SamplerState sampler_TextureAtlas;
Texture2D<half2> _NormalAtlas;
SamplerState sampler_NormalAtlas;
Texture2D<half4> _EmissiveAtlas;
Texture2D<half> _IESAtlas;
Texture2D<half> Heightmap;

#if defined(UseBindless) && !defined(DX11)
	SamplerState my_trilinear_repeat_sampler;
	SamplerState my_point_repeat_sampler;
	Texture2D<float4> _BindlessTextures[2048] : register(t31);
#endif

inline float2 AlignUV(float2 BaseUV, float4 TexScale, int2 TexDim2, float Rotation = 0) {
	if(TexDim2.x <= 0) return -1;
	float4 TexDim;
    TexDim.xy = float2((float)(((uint)TexDim2.x) & 0x7FFF) / 16384.0f, (float)(((uint)TexDim2.x >> 15)) / 16384.0f);
    TexDim.zw = float2((float)(((uint)TexDim2.y) & 0x7FFF) / 16384.0f, (float)(((uint)TexDim2.y >> 15)) / 16384.0f);
	BaseUV = BaseUV * TexScale.xy + TexScale.zw;
	BaseUV = (BaseUV < 0 ? (1.0f - fmod(abs(BaseUV), 1.0f)) : fmod(abs(BaseUV), 1.0f));
	// BaseUV =fmod(abs(BaseUV), 1.0f);
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
	return clamp(BaseUV * (TexDim.xy - TexDim.zw) + TexDim.zw, TexDim.zw + 1.0f / 16384.0f, TexDim.xy - 1.0f / 16384.0f);
}


inline float4 SampleTexture(float2 UV, int TextureType, const MaterialData MatTex) {
	float4 FinalCol = 0;
	#if !defined(UseBindless) || defined(DX11)
		switch(TextureType) {
			case SampleAlbedo:
				#ifdef PointFiltering
					FinalCol = _TextureAtlas.SampleLevel(my_point_clamp_sampler, AlignUV(UV, MatTex.AlbedoTexScale, MatTex.AlbedoTex, MatTex.Rotation), 0);
				#else
					FinalCol = _TextureAtlas.SampleLevel(my_linear_clamp_sampler, AlignUV(UV, MatTex.AlbedoTexScale, MatTex.AlbedoTex, MatTex.Rotation), 0);
				#endif
			break;
			case SampleMetallic:
				FinalCol = SingleComponentAtlas.SampleLevel(my_linear_clamp_sampler, AlignUV(UV, float4(MatTex.SecondaryTexScale, MatTex.AlbedoTexScale.zw), MatTex.MetallicTex, MatTex.Rotation), 0);
			break;
			case SampleRoughness:
				FinalCol = SingleComponentAtlas.SampleLevel(my_linear_clamp_sampler, AlignUV(UV, float4(MatTex.SecondaryTexScale, MatTex.AlbedoTexScale.zw), MatTex.RoughnessTex, MatTex.Rotation), 0);
			break;
			case SampleEmission:
				FinalCol = _EmissiveAtlas.SampleLevel(my_linear_clamp_sampler, AlignUV(UV, MatTex.AlbedoTexScale, MatTex.EmissiveTex, MatTex.Rotation), 0);
			break;
			case SampleNormal:
				FinalCol = _NormalAtlas.SampleLevel(sampler_NormalAtlas, AlignUV(UV, float4(MatTex.SecondaryTexScale, MatTex.AlbedoTexScale.zw), MatTex.NormalTex, MatTex.Rotation), 0).xyxy;
			break;
			case SampleAlpha:
				#ifdef PointFiltering
					FinalCol = _AlphaAtlas.SampleLevel(my_point_clamp_sampler, AlignUV(UV, MatTex.AlbedoTexScale, MatTex.AlphaTex, MatTex.Rotation), 0);
				#else
					FinalCol = _AlphaAtlas.SampleLevel(my_linear_clamp_sampler, AlignUV(UV, MatTex.AlbedoTexScale, MatTex.AlphaTex, MatTex.Rotation), 0);
				#endif
			break;
			case SampleMatCap:
				FinalCol = _TextureAtlas.SampleLevel(my_linear_clamp_sampler, AlignUV(UV, MatTex.AlbedoTexScale, MatTex.MatCapTex, MatTex.Rotation), 0);
			break;
			case SampleMatCapMask:
				FinalCol = _TextureAtlas.SampleLevel(my_linear_clamp_sampler, AlignUV(UV, MatTex.AlbedoTexScale, MatTex.MatCapMask, MatTex.Rotation), 0);
			break;
			case SampleTerrainAlbedo:
				UV = AlignUV(UV * MatTex.surfaceColor.xy + MatTex.transmittanceColor.xy, MatTex.AlbedoTexScale, MatTex.AlbedoTex);
				FinalCol = _TextureAtlas.SampleLevel(my_point_clamp_sampler, UV, 0);
			break;
			case SampleSecondaryAlbedo:
				#ifdef PointFiltering
					FinalCol = _TextureAtlas.SampleLevel(my_point_clamp_sampler, AlignUV(UV, MatTex.SecondaryAlbedoTexScaleOffset, MatTex.SecondaryAlbedoTex, MatTex.Rotation), 0);
				#else
					FinalCol = _TextureAtlas.SampleLevel(my_linear_clamp_sampler, AlignUV(UV, MatTex.SecondaryAlbedoTexScaleOffset, MatTex.SecondaryAlbedoTex, MatTex.Rotation), 0);
				#endif
			break;
			case SampleSecondaryAlbedoMask:
				FinalCol = SingleComponentAtlas.SampleLevel(my_linear_clamp_sampler, AlignUV(UV, MatTex.AlbedoTexScale, MatTex.SecondaryAlbedoMask, MatTex.Rotation), 0);
			break;
		}
	#else//BINDLESS
		//AlbedoTexScale, AlbedoTex, and Rotation dont worry about, thats just for transforming to the atlas 
		int2 TextureIndexAndChannel = -1;// = MatTex.BindlessIndex;
		if(MatTex.Rotation != 0) {
			float sinc, cosc;
			sincos(MatTex.Rotation, sinc, cosc);
			UV -= 0.5f;
			float2 tempuv = UV;
			UV.x = tempuv.x * cosc - tempuv.y * sinc;
			UV.y = tempuv.x * sinc + tempuv.y * cosc;
			UV += 0.5f;
		}
		switch(TextureType) {
			case SampleAlbedo: TextureIndexAndChannel = MatTex.AlbedoTex; UV = UV * MatTex.AlbedoTexScale.xy + MatTex.AlbedoTexScale.zw; break;
			case SampleMetallic: TextureIndexAndChannel = MatTex.MetallicTex; UV = UV * MatTex.SecondaryTexScale.xy + MatTex.AlbedoTexScale.zw; break;
			case SampleRoughness: TextureIndexAndChannel = MatTex.RoughnessTex; UV = UV * MatTex.SecondaryTexScale.xy + MatTex.AlbedoTexScale.zw; break;
			case SampleEmission: TextureIndexAndChannel = MatTex.EmissiveTex; UV = UV * MatTex.AlbedoTexScale.xy + MatTex.AlbedoTexScale.zw; break;
			case SampleNormal: TextureIndexAndChannel = MatTex.NormalTex; UV = UV * MatTex.SecondaryTexScale.xy + MatTex.AlbedoTexScale.zw; break;
			case SampleAlpha: TextureIndexAndChannel = MatTex.AlphaTex; UV = UV * MatTex.AlbedoTexScale.xy + MatTex.AlbedoTexScale.zw; break;
			case SampleMatCap: TextureIndexAndChannel = MatTex.MatCapTex; UV = UV * MatTex.AlbedoTexScale.xy + MatTex.AlbedoTexScale.zw; break;
			case SampleMatCapMask: TextureIndexAndChannel = MatTex.MatCapMask; UV = UV * MatTex.AlbedoTexScale.xy + MatTex.AlbedoTexScale.zw; break;
			case SampleTerrainAlbedo: TextureIndexAndChannel = MatTex.AlbedoTex; UV = (UV * MatTex.surfaceColor.xy + MatTex.transmittanceColor.xy) * MatTex.AlbedoTexScale.xy + MatTex.AlbedoTexScale.zw; break;
			case SampleSecondaryAlbedo: TextureIndexAndChannel = MatTex.SecondaryAlbedoTex; UV = UV * MatTex.SecondaryAlbedoTexScaleOffset.xy + MatTex.SecondaryAlbedoTexScaleOffset.zw; break;
			case SampleSecondaryAlbedoMask: TextureIndexAndChannel = MatTex.SecondaryAlbedoMask; UV = UV * MatTex.AlbedoTexScale.xy + MatTex.AlbedoTexScale.zw; break;
		}
		int TextureIndex = TextureIndexAndChannel.x - 1;
		int TextureReadChannel = TextureIndexAndChannel.y;//0-3 is rgba, 4 is to just read all

		#ifdef UseTextureLOD
			#ifdef PointFiltering
				FinalCol = _BindlessTextures[TextureIndex].SampleLevel(my_point_repeat_sampler, UV, CurBounce);
			#else
				FinalCol = _BindlessTextures[TextureIndex].SampleLevel(my_trilinear_repeat_sampler, UV, CurBounce);
			#endif
		#else
			#ifdef PointFiltering
				FinalCol = _BindlessTextures[TextureIndex].SampleLevel(my_point_repeat_sampler, UV, 0);
			#else
				FinalCol = _BindlessTextures[TextureIndex].SampleLevel(my_trilinear_repeat_sampler, UV, 0);
			#endif
		#endif
		if(TextureReadChannel != 4) FinalCol = FinalCol[TextureReadChannel];
		if(TextureType == SampleNormal) {
			FinalCol.g = 1.0f - FinalCol.g;
			FinalCol = (FinalCol.r >= 0.99f) ? FinalCol.agag : FinalCol.rgrg;

		}



	#endif
	return FinalCol;
}


Texture2D<float4> TerrainAlphaMap;
SamplerState sampler_TerrainAlphaMap;
int MaterialCount;



int LightMeshCount;

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
float NearPlane;

SmallerRay CreateRay(float3 origin, float3 direction) {
	SmallerRay ray;
	ray.origin = origin;
	ray.direction = direction;
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

float encode_diamond(float2 p)
{
    // Project to the unit diamond, then to the x-axis.
    float x = p.x / (abs(p.x) + abs(p.y));

    // Contract the x coordinate by a factor of 4 to represent all 4 quadrants in
    // the unit range and remap
    float py_sign = sign(p.y);
    return -py_sign * 0.25f * x + 0.5f + py_sign * 0.25f;
}

float2 decode_diamond(float p)
{
    float2 v;

    // Remap p to the appropriate segment on the diamond
    float p_sign = sign(p - 0.5f);
    v.x = -p_sign * 4.f * p + 1.f + p_sign * 2.f;
    v.y = p_sign * (1.f - abs(v.x));

    // Normalization extends the point on the diamond back to the unit circle
    return normalize(v);
}

// Given a normal and tangent vector, encode the tangent as a single float that can be
// subsequently quantized.
float encode_tangent(float3 normal, float3 tangent)
{
    // First, find a canonical direction in the tangent plane
    float3 t1;
    if (abs(normal.y) > abs(normal.z))
    {
        // Pick a canonical direction orthogonal to n with z = 0
        t1 = float3(normal.y, -normal.x, 0.f);
    }
    else
    {
        // Pick a canonical direction orthogonal to n with y = 0
        t1 = float3(normal.z, 0.f, -normal.x);
    }
    t1 = normalize(t1);

    // Construct t2 such that t1 and t2 span the plane
    float3 t2 = cross(t1, normal);

    // Decompose the tangent into two coordinates in the canonical basis
    float2 packed_tangent = float2(dot(tangent, t1), dot(tangent, t2));

    // Apply our diamond encoding to our two coordinates
    return encode_diamond(packed_tangent);
}

float3 decode_tangent(float3 normal, float diamond_tangent)
{
    // As in the encode step, find our canonical tangent basis span(t1, t2)
    float3 t1;
    if (abs(normal.y) > abs(normal.z))
    {
        t1 = float3(normal.y, -normal.x, 0.f);
    }
    else
    {
        t1 = float3(normal.z, 0.f, -normal.x);
    }
    t1 = normalize(t1);

    float3 t2 = cross(t1, normal);

    // Recover the coordinates used with t1 and t2
    float2 packed_tangent = decode_diamond(diamond_tangent);

    return packed_tangent.x * t1 + packed_tangent.y * t2;
}


SmallerRay CreateCameraRay(float2 uv, uint pixel_index) {
	// Transform the camera origin to world space
	float3 origin = mul(CamToWorld, float4(0.0f, 0.0f, 0.0f, 1.0f)).xyz;

	// Invert the perspective projection of the view-space position
	float3 direction = mul(CamInvProj, float4(uv, 0.0f, 1.0f)).xyz;
	// Transform the direction from camera to world space and normalize
	direction = mul(CamToWorld, float4(direction, 0.0f)).xyz;
	direction = normalize(direction);
	uint2 id = uint2(pixel_index % screen_width, pixel_index / screen_width);
	[branch] if (!OIDNGuideWrite && UseDoF && (!IsFocusing || dot(id - int2(MousePos.x, MousePos.y), id - int2(MousePos.x, MousePos.y)) > 6.0f)) {
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

inline SmallerRay CreateCameraRay(float2 uv) {
    // Transform the camera origin to world space
    float3 origin = mul(CamToWorld, float4(0.0f, 0.0f, 0.0f, 1.0f)).xyz;

    // Invert the perspective projection of the view-space position
    float3 direction = mul(CamInvProj, float4(uv, 0.0f, 1.0f)).xyz;
    // Transform the direction from camera to world space and normalize
    direction = mul(CamToWorld, float4(direction, 0.0f)).xyz;
    direction = normalize(direction);

    return CreateRay(origin, direction);
}

inline SmallerRay CreateCameraRayPrev(float2 uv) {
    // Transform the camera origin to world space
    float3 origin = mul(CamToWorldPrev, float4(0.0f, 0.0f, 0.0f, 1.0f)).xyz;

    // Invert the perspective projection of the view-space position
    float3 direction = mul(CamInvProjPrev, float4(uv, 0.0f, 1.0f)).xyz;
    // Transform the direction from camera to world space and normalize
    direction = mul(CamToWorldPrev, float4(direction, 0.0f)).xyz;
    direction = normalize(direction);

    return CreateRay(origin, direction);
}

static float3 CalculateExtinction2(float3 apparantColor, float scatterDistance)
{
    float3 a = apparantColor;
    float3 s = 1.9f - a + 3.5f * (a - 0.8f) * (a - 0.8f);

    return 1.0f / (s * scatterDistance);
}
//shadow_triangle
inline bool triangle_intersect_shadow(int tri_id, const SmallerRay ray, float max_distance, int mesh_id, inout float3 throughput, const int MatOffset) {
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
            #if defined(AdvancedAlphaMapped) || defined(AdvancedBackground) || defined(IgnoreGlassShadow)
				if(GetFlag(_Materials[MaterialIndex].Tag, IsBackground) || GetFlag(_Materials[MaterialIndex].Tag, ShadowCaster)) return false; 
        		if(_Materials[MaterialIndex].MatType == CutoutIndex || _Materials[MaterialIndex].specTrans == 1) {
	                float2 BaseUv = tri2.pos0 * (1.0f - u - v) + tri2.posedge1 * u + tri2.posedge2 * v;
                    if(_Materials[MaterialIndex].MatType == CutoutIndex && _Materials[MaterialIndex].AlphaTex.x > 0)
                        if( SampleTexture(BaseUv, SampleAlpha, _Materials[MaterialIndex]).x < _Materials[MaterialIndex].AlphaCutoff) return false;

		            #ifdef IgnoreGlassShadow
		                if(_Materials[MaterialIndex].specTrans == 1) {
			            	#ifdef StainedGlassShadows
			            		float3 MatCol = _Materials[MaterialIndex].surfaceColor;
						        if(_Materials[MaterialIndex].AlbedoTex.x > 0) MatCol *= SampleTexture(BaseUv, SampleAlbedo, _Materials[MaterialIndex]) / 3.0f;
        						MatCol = lerp(MatCol, _Materials[MaterialIndex].BlendColor, _Materials[MaterialIndex].BlendFactor);
		    					throughput *= sqrt(exp(-CalculateExtinction2(1.0f - MatCol, _Materials[MaterialIndex].scatterDistance == 0.0f ? 1.0f : _Materials[MaterialIndex].scatterDistance)));
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
inline uint ray_get_octant_inv4(const float3 ray_direction) {
    return
        (ray_direction.x < 0.0f ? 0 : 0x04040404) |
        (ray_direction.y < 0.0f ? 0 : 0x02020202) |
        (ray_direction.z < 0.0f ? 0 : 0x01010101);
}

inline uint cwbvh_node_intersect(const SmallerRay ray, int oct_inv4, float max_distance, const BVHNode8Data TempNode) {
    uint e_x = (TempNode.nodes[0].w) & 0xff;
    uint e_y = (TempNode.nodes[0].w >> (8)) & 0xff;
    uint e_z = (TempNode.nodes[0].w >> (16)) & 0xff;

    const float3 adjusted_ray_direction_inv = float3(
        asfloat(e_x << 23),
        asfloat(e_y << 23),
        asfloat(e_z << 23)
        ) / ray.direction;
    const float3 adjusted_ray_origin = (asfloat(TempNode.nodes[0].xyz) - ray.origin) / ray.direction;
            
    uint hit_mask = 0;
    float3 tmin3;
    float3 tmax3;
    uint child_bits;
    uint bit_index;
        uint q_lo_x;
        uint q_hi_x;
        uint q_lo_y;
        uint q_hi_y;
        uint q_lo_z;
        uint q_hi_z;
    [unroll]
    for(int i = 0; i < 2; i++) {
        uint meta4 = (i == 0 ? TempNode.nodes[1].z : TempNode.nodes[1].w);

        uint is_inner4   = (meta4 & (meta4 << 1)) & 0x10101010;
        uint inner_mask4 = (is_inner4 >> 4) * 0xffu;
        uint bit_index4  = (meta4 ^ (oct_inv4 & inner_mask4)) & 0x1f1f1f1f;
        uint child_bits4 = (meta4 >> 5) & 0x07070707;
        if(i == 0u) {
	        q_lo_x = TempNode.nodes[2].x;
	        q_hi_x = TempNode.nodes[2].y;
	        q_lo_y = TempNode.nodes[3].x;
	        q_hi_y = TempNode.nodes[3].y;
	        q_lo_z = TempNode.nodes[4].x;
	        q_hi_z = TempNode.nodes[4].y;
	    } else {
	        q_lo_x = TempNode.nodes[2].z;
	        q_hi_x = TempNode.nodes[2].w;
	        q_lo_y = TempNode.nodes[3].z;
	        q_hi_y = TempNode.nodes[3].w;
	        q_lo_z = TempNode.nodes[4].z;
	        q_hi_z = TempNode.nodes[4].w;
	    }

        uint x_min = ray.direction.x < 0.0f ? q_hi_x : q_lo_x;
        uint x_max = ray.direction.x < 0.0f ? q_lo_x : q_hi_x;

        uint y_min = ray.direction.y < 0.0f ? q_hi_y : q_lo_y;
        uint y_max = ray.direction.y < 0.0f ? q_lo_y : q_hi_y;

        uint z_min = ray.direction.z < 0.0f ? q_hi_z : q_lo_z;
        uint z_max = ray.direction.z < 0.0f ? q_lo_z : q_hi_z;
        [unroll]
        for(int j = 0; j < 4; j++) {

            tmin3 = float3(((x_min >> (j * 8)) & 0xff), ((y_min >> (j * 8)) & 0xff), ((z_min >> (j * 8)) & 0xff));
            tmax3 = float3(((x_max >> (j * 8)) & 0xff), ((y_max >> (j * 8)) & 0xff), ((z_max >> (j * 8)) & 0xff));

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


inline bool VisabilityCheckCompute(SmallerRay ray, float dist) {
        uint2 stack[16];
        int stack_size = 0;
        uint2 current_group;

        uint oct_inv4;
        int tlas_stack_size = -1;
        int mesh_id = -1;
        SmallerRay ray2;
        int TriOffset = 0;
        int NodeOffset = 0;

        ray2 = ray;

        oct_inv4 = ray_get_octant_inv4(ray.direction);

        current_group.x = (uint)0;
        current_group.y = (uint)0x80000000;
        int MatOffset = 0;
        int Reps = 0;
        float3 through = 0;
        [loop] while (Reps < MaxTraversalSamples) {//Traverse Accelleration Structure(Compressed Wide Bounding Volume Hierarchy)            
        	uint2 triangle_group;
            [branch]if (current_group.y & 0xff000000) {
                uint child_index_offset = firstbithigh(current_group.y);
                
                uint slot_index = (child_index_offset - 24) ^ (oct_inv4 & 0xff);
                uint relative_index = countbits(current_group.y & ~(0xffffffff << slot_index));
                uint child_node_index = current_group.x + relative_index;
                const BVHNode8Data TempNode = cwbvh_nodes[child_node_index];

                current_group.y &= ~(1 << child_index_offset);

                if (current_group.y & 0xff000000) stack[stack_size++] = current_group;

                uint hitmask = cwbvh_node_intersect(ray, oct_inv4, dist, TempNode);

 				current_group.y = (hitmask & 0xff000000) | ((TempNode.nodes[0].w >> 24) & 0xff);
                triangle_group.y = (hitmask & 0x00ffffff);

	            current_group.x = (TempNode.nodes[1].x) + NodeOffset;
                triangle_group.x = (TempNode.nodes[1].y) + TriOffset;
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

                    oct_inv4 = ray_get_octant_inv4(ray.direction);

                    current_group.x = (uint)root_index;
                    current_group.y = (uint)0x80000000;
                } else {
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



inline float power_heuristic(float pdf_f, float pdf_g) {
    return (pdf_f * pdf_f) / (pdf_f * pdf_f + pdf_g * pdf_g); // Power of 2 hardcoded, best empirical results according to Veach
}

uint octahedral_32(float3 nor) {
	float oct = 1.0f / (abs(nor.x) + abs(nor.y) + abs(nor.z));
	float t = saturate(-nor.z);
	nor.xy = (nor.xy + (nor.xy > 0.0f ? t : -t)) * oct;
    uint2 d = uint2(round(32767.5 + nor.xy*32767.5));  
    return d.x|(d.y<<16u);
}

float3 i_octahedral_32( uint data ) {
    uint2 iv = uint2( data, data>>16u ) & 65535u; 
    float2 v = iv/32767.5f - 1.0f;
    float3 nor = float3(v, 1.0f - abs(v.x) - abs(v.y)); // Rune Stubbe's version,
    float t = max(-nor.z,0.0);                     // much faster than original
    nor.xy += (nor.xy>=0.0)?-t:t;                     // implementation of this
    return normalize( nor );
}

int RISCount;

int SelectLightMeshSmart(uint pixel_index, inout float MeshWeight, float3 Pos) {//Maybe add an "extents" to the lightmeshes to get an even better estimate?

 	int MinIndex = 0;
    float wsum = 0;
    int M = 0;
    float MinP_Hat = 0;
    float p_hat;
    const int RISFinal = RISCount + 1;
    for(int i = 0; i < RISFinal; i++) {
        float2 Rand = random(i + 11, pixel_index);
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


float SkyDesaturate;
float SecondarySkyDesaturate;

float3 SunDir;


inline float luminance(const float3 a) {
    return dot(float3(0.299f, 0.587f, 0.114f), a);
}

inline float lum2(const float3 a) {
    return dot(float3(0.21f, 0.72f, 0.07f), a);
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

inline float GetHeightRaw(float3 CurrentPos, const TerrainData Terrain) {
    CurrentPos -= Terrain.PositionOffset;
    float3 b = float3(Terrain.TerrainDim.x, 0.11f, Terrain.TerrainDim.y);
    float2 uv = float2(min(CurrentPos.x / Terrain.TerrainDim.x, b.x / Terrain.TerrainDim.x), min(CurrentPos.z / Terrain.TerrainDim.y, b.z / Terrain.TerrainDim.y));
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

static const float FLT_EPSILON = 1.192092896e-07f;



inline float2 mulsign(const float2 x, const float2 y)
{
	return asfloat((asuint(y) & 0x80000000) ^ asuint(x));
}

inline float expm1(const float x) {
    const float u = exp(x);
    const float y = u - 1.0f;
    return (u == 1.0f) ? x : (abs(x) < 1.0f ? y * x / log(u) : y);
}



float erf2(const float x)
{
	// Early return for large |x|.
	if (abs(x) >= 4.0)
	{
		return mulsign(1.0, x);
	}

	// Polynomial approximation based on the approximation posted in https://forums.developer.nvidia.com/t/optimized-version-of-single-precision-error-function-erff/40977
	[branch]if (abs(x) > 1.0)
	{
		// The maximum error is smaller than the approximation described in Abramowitz and Stegun [1964 "Handbook of Mathematical Functions with Formulas, Graphs, and Mathematical Tables", 7.1.26, p.299].
		const float A1 = 1.628459513;
		const float A2 = 9.15674746e-1;
		const float A3 = 1.54329389e-1;
		const float A4 = -3.51759829e-2;
		const float A5 = 5.66795561e-3;
		const float A6 = -5.64874616e-4;
		const float A7 = 2.58907676e-5;
		const float a = abs(x);
		const float y = 1.0 - exp2(-(((((((A7 * a + A6) * a + A5) * a + A4) * a + A3) * a + A2) * a + A1) * a));

		return mulsign(y, x);
	} else {

		// The maximum error is smaller than the 6th order Taylor polynomial.
		const float A1 = 1.128379121;
		const float A2 = -3.76123011e-1;
		const float A3 = 1.12799220e-1;
		const float A4 = -2.67030653e-2;
		const float A5 = 4.90735564e-3;
		const float A6 = -5.58853149e-4;
		const float x2 = x * x;

		return (((((A6 * x2 + A5) * x2 + A4) * x2 + A3) * x2 + A2) * x2 + A1) * x;
	}
}

float erfc(const float x)
{
	return 1.0 - erf2(x);
}


inline float vmf_hemispherical_integral ( float cosine , float sharpness ) {
	// Interpolation factor [ Tokuyoshi 2022].
	float steepness = sharpness * sqrt ((0.5 * sharpness + 0.65173288269070562) / (( sharpness + 1.3418280033141288) *
	sharpness + 7.2216687798956709) ) ;
	float v = saturate (0.5 + 0.5 * ( erf2( steepness * clamp ( cosine , -1.0 , 1.0) ) / erf2( steepness ) ) ) ;
	// Interpolation between upper and lower hemispherical integrals .
	float e = exp( - sharpness ) ;
	return lerp (e , 1.0 , v ) / ( e + 1.0) ;
}

inline float VMFHemisphericalIntegral(const float cosine, const float sharpness)
{
	// Interpolation factor [Tokuyoshi 2022].
	const float A = 0.6517328826907056171791055021459;
	const float B = 1.3418280033141287699294252888649;
	const float C = 7.2216687798956709087860872386955;
	const float steepness = sharpness * sqrt((0.5 * sharpness + A) / ((sharpness + B) * sharpness + C));
	const float lerpFactor = saturate(0.5 + 0.5 * (erf2(steepness * clamp(cosine, -1.0, 1.0)) / erf2(steepness)));

	// Interpolation between upper and lower hemispherical integrals .
	const float e = exp(-sharpness);

	return lerp(e, 1.0, lerpFactor) / (e + 1.0);
}


struct SGLobe {
	float3 axis ;
	float sharpness ;
	float logAmplitude ;
};
inline SGLobe sg_product ( float3 axis1 , float sharpness1 , float3 axis2 , float sharpness2 ) {
	float3 axis = axis1 * sharpness1 + axis2 * sharpness2 ;
	float sharpness = length ( axis ) ;
	float cosine = clamp (dot( axis1 , axis2 ) , -1.0 , 1.0) ;
	float sharpnessMin = min( sharpness1 , sharpness2 ) ;
	float sharpnessRatio = sharpnessMin / max( sharpness1 , sharpness2 ) ;
	float logAmplitude = 2.0 * sharpnessMin * ( cosine - 1.0) / (1.0 + sharpnessRatio + sqrt (2.0 * sharpnessRatio * cosine
	+ sharpnessRatio * sharpnessRatio + 1.0) ) ;
	SGLobe result = { axis / max( sharpness , EPSILON ) , sharpness , logAmplitude };
	return result ;
}
static const float FLT_MAX = 3.402823466e+38f;


// [Tokuyoshi et al. 2024 "Hierarchical Light Sampling with Accurate Spherical Gaussian Lighting (Supplementary Document)" Listing. 5]
inline float UpperSGClampedCosineIntegralOverTwoPi(const float sharpness)
{
	[branch]if (sharpness <= 0.5)
	{
		// Taylor-series approximation for the numerical stability.
		// TODO: Derive a faster polynomial approximation.
		return (((((((-1.0 / 362880.0) * sharpness + 1.0 / 40320.0) * sharpness - 1.0 / 5040.0) * sharpness + 1.0 / 720.0) * sharpness - 1.0 / 120.0) * sharpness + 1.0 / 24.0) * sharpness - 1.0 / 6.0) * sharpness + 0.5;
	}

	return (expm1(-sharpness) + sharpness) / (sharpness * sharpness);
}

// [Tokuyoshi et al. 2024 "Hierarchical Light Sampling with Accurate Spherical Gaussian Lighting (Supplementary Document)" Listing. 6]
inline float LowerSGClampedCosineIntegralOverTwoPi(const float sharpness)
{
	const float e = exp(-sharpness);

	[branch]if (sharpness <= 0.5)
	{
		// Taylor-series approximation for the numerical stability.
		// TODO: Derive a faster polynomial approximation.
		return e * (((((((((1.0 / 403200.0) * sharpness - 1.0 / 45360.0) * sharpness + 1.0 / 5760.0) * sharpness - 1.0 / 840.0) * sharpness + 1.0 / 144.0) * sharpness - 1.0 / 30.0) * sharpness + 1.0 / 8.0) * sharpness - 1.0 / 3.0) * sharpness + 0.5);
	}

	return e * (-expm1(-sharpness) - sharpness * e) / (sharpness * sharpness);
}

// Approximate product integral of an SG and clamped cosine / pi.
// [Tokuyoshi et al. 2024 "Hierarchical Light Sampling with Accurate Spherical Gaussian Lighting (Supplementary Document)" Listing. 7]
inline float SGClampedCosineProductIntegralOverPi2024(const float cosine, const float sharpness)
{
	const float t = sharpness * sqrt(0.5 * ((sharpness + 2.7360831611272558028247203765204f) * sharpness + 17.02129778174187535455530451145f) / (((sharpness + 4.0100826728510421403939290030394f) * sharpness + 15.219156263147210594866010069381f) * sharpness + 76.087896272360737270901154261082f));
	const float tz = t * cosine;//can these EVER GET NEGATIVE

	// In this HLSL implementation, we roughly implement erfc(x) = 1 - erf2(x) which can have a numerical error for large x.
	// Therefore, unlike the original impelemntation [Tokuyoshi et al. 2024], we clamp the lerp factor with the machine epsilon / 2 for a conservative approximation.
	// This clamping is unnecessary for languages that have a precise erfc function (e.g., C++).
	// The original implementation [Tokuyoshi et al. 2024] uses a precise erfc function and does not clamp the lerp factor.
	const float INV_SQRTPI = 0.56418958354775628694807945156077f; // = 1/sqrt(pi).
	const float CLAMPING_THRESHOLD = 0.5 * FLT_EPSILON; // Set zero if a precise erfc function is available.
	
	float ERFCTZ = 1.0f;
	float ERFCT = 1.0f;
	{
		const float A1 = 1.628459513;
		const float A2 = 9.15674746e-1;
		const float A3 = 1.54329389e-1;
		const float A4 = -3.51759829e-2;
		const float A5 = 5.66795561e-3;
		const float A6 = -5.64874616e-4;
		const float A7 = 2.58907676e-5;

		const float B1 = 1.128379121;
		const float B2 = -3.76123011e-1;
		const float B3 = 1.12799220e-1;
		const float B4 = -2.67030653e-2;
		const float B5 = 4.90735564e-3;
		const float B6 = -5.58853149e-4;


		if (abs(tz) >= 4.0) {
			ERFCTZ = mulsign(1.0, -tz);
		} else if (abs(tz) > 1.0) {
			const float a = abs(tz);
			const float y = 1.0 - exp2(-(((((((A7 * a + A6) * a + A5) * a + A4) * a + A3) * a + A2) * a + A1) * a));

			ERFCTZ = mulsign(y, -tz);
		} else {
			const float x2 = tz * tz;
			ERFCTZ = (((((B6 * x2 + B5) * x2 + B4) * x2 + B3) * x2 + B2) * x2 + B1) * -tz;
		}

		[branch]if (t >= 4.0) {
			ERFCT = mulsign(1.0, t);
		} else if (t > 1.0) {
			const float y = 1.0 - exp2(-(((((((A7 * t + A6) * t + A5) * t + A4) * t + A3) * t + A2) * t + A1) * t));

			ERFCT = mulsign(y, t);
		} else {
			const float x2 = t * t;
			ERFCT = (((((B6 * x2 + B5) * x2 + B4) * x2 + B3) * x2 + B2) * x2 + B1) * t;
		}
		ERFCT = 1.0f - ERFCT;
		ERFCTZ = 1.0f - ERFCTZ;



	}



	const float lerpFactor = saturate(max(0.5 * (cosine * ERFCTZ + ERFCT) - 0.5 * INV_SQRTPI * exp(-tz * tz) * expm1(t * t * (cosine * cosine - 1.0)) / t, CLAMPING_THRESHOLD));

	// Interpolation between lower and upper hemispherical integrals.
	const float lowerIntegral = LowerSGClampedCosineIntegralOverTwoPi(sharpness);
	const float upperIntegral = UpperSGClampedCosineIntegralOverTwoPi(sharpness);

	return 2.0 * lerp(lowerIntegral, upperIntegral, lerpFactor);
}


// Symmetry GGX using a 2x2 roughness matrix (i.e., Non-axis-aligned GGX w/o the Heaviside function).
inline float SGGX(const float3 m, const float2x2 roughnessMat)
{
	const float det = max(determinant(roughnessMat), EPSILON); // TODO: Use Kahan's algorithm for precise determinant. [https://pharr.org/matt/blog/2019/11/03/difference-of-floats]
	const float2x2 roughnessMatAdj = { roughnessMat._22, -roughnessMat._12, -roughnessMat._21, roughnessMat._11 };
	const float length2 = dot(m.xz, mul(roughnessMatAdj, m.xz)) / det + m.y * m.y; // TODO: Use Kahan's algorithm for precise mul and dot. [https://pharr.org/matt/blog/2019/11/03/difference-of-floatshttps://pharr.org/matt/blog/2019/11/03/difference-of-floats]

	return 1.0 / (PI * sqrt(det) * (length2 * length2));
}

// Reflection lobe based the symmetric GGX VNDF.
// [Tokuyoshi et al. 2024 "Hierarchical Light Sampling with Accurate Spherical Gaussian Lighting", Section 5.2]
inline float SGGXReflectionPDF(const float3 wi, const float3 m, const float2x2 roughnessMat)
{
	return SGGX(m, roughnessMat) / max(4.0 * sqrt(dot(wi.xz, mul(roughnessMat, wi.xz)) + wi.y * wi.y), EPSILON); // TODO: Use Kahan's algorithm for precise mul and dot. [https://pharr.org/matt/blog/2019/11/03/difference-of-floats]
}


inline float expm1_over_x(const float x)
{
	const float u = exp(x);

	if (u == 1.0)
	{
		return 1.0;
	}

	const float y = u - 1.0;

	if (abs(x) < 1.0)
	{
		return y / log(u);
	}

	return y / x;
}


inline float SGIntegral(const float sharpness)
{
	return 4.0 * PI * expm1_over_x(-2.0 * sharpness);
}


	/*
		is cross(B,N) = T?
		if so, origional actually constructs the frame as TBN, so the swap between .y and .z should hold up well
		I really need to bite the bullet and convert my materials and thus their tangent frames to use TBN instead of TNB, since everyone uses TBN
		but Z->Up is just weird
		if cross(B,N) = T, why do they use that to add the tangent to the frame? why not just use the tangent thats already in the function? is it a handedness or sign issue?


		Unrelated: see if I can use the hypothesis tests for firefly detection...

		Thank god the code snippets from the paper and for the determinant were in HLSL
		i do NOT want to deal with re-swizzling the matrices....(side note, look into this to make sure I actually dont)

	*/

inline bool IsFinite(float x)
{
    return (asuint(x) & 0x7F800000) != 0x7F800000;
}

inline float SGImportance(GaussianTreeNode TargetNode, const float3 viewDir, const float3 p, const float3 n, const float2 projRoughness2, const float reflecSharpness, const float3x3 tangentFrame, const float metallic) {
	float3 to_light = TargetNode.position - p;
	const float squareddist = dot(to_light, to_light);

	const float c = max(dot(n, -to_light) / sqrt(squareddist), 0);

	// Compute the Jacobian J for the transformation between halfvetors and reflection vectors at halfvector = normal.
	const float3 viewDirTS = mul(tangentFrame, viewDir);//their origional one constructs the tangent frame from N,T,BT, whereas mine constructs it from T,N,BT; problem? I converted all .y to .z and vice versa, but... 

	const float vlen = length(viewDirTS.xz);
	const float2 v = (vlen != 0.0) ? (viewDirTS.xz / vlen) : float2(1.0, 0.0);
	const float2x2 reflecJacobianMat = mul(float2x2(v.x, -v.y, v.y, v.x), float2x2(0.5, 0.0, 0.0, 0.5 / viewDirTS.y));

	// Compute JJ^T matrix.
	const float2x2 jjMat = mul(reflecJacobianMat, transpose(reflecJacobianMat));
	const float detJJ4 = 1.0 / (4.0 * viewDirTS.y * viewDirTS.y); // = 4 * determiant(JJ^T).

	// Preprocess for the lobe visibility.
	// Approximate the reflection lobe with an SG whose axis is the perfect specular reflection vector.
	// We use a conservative sharpness to filter the visibility.
	const float3 reflecVec = reflect(-viewDir, n) * reflecSharpness;

	// TargetNode.variance = 0.5f * TargetNode.radius * TargetNode.radius;
	float Variance = max(TargetNode.variance, (1.0f / pow(2, 31)) * squareddist);
	Variance = Variance * (1.0f - c) + 0.5f * (TargetNode.radius * TargetNode.radius) * c;
	// float Variance = max(TargetNode.variance, squareddist);


	to_light = normalize(to_light);
	const SGLobe LightLobe = sg_product((TargetNode.axis), TargetNode.sharpness, to_light, squareddist / Variance);


	const float emissive = (TargetNode.intensity) / (Variance * SGIntegral(TargetNode.sharpness));

	const float amplitude = exp(LightLobe.logAmplitude);
	const float cosine = clamp(dot(LightLobe.axis, n), -1.0, 1.0);
	const float diffuseIllumination = amplitude * SGClampedCosineProductIntegralOverPi2024(cosine, LightLobe.sharpness);


	const float3 prodVec = reflecVec + LightLobe.axis * LightLobe.sharpness; // Axis of the SG product lobe.
	const float prodSharpness = length(prodVec);
	const float3 prodDir = prodVec / prodSharpness;
	const float LightLobeVariance = rcp(LightLobe.sharpness);
	// if(id.y > screen_height / 2 + screen_height / 4) LightLobeVariance = LightLobeVariance * (1.0f - c) + 0.5f * (TargetNode.radius * TargetNode.radius) * c;
	
	const float2x2 filteredProjRoughnessMat = float2x2(projRoughness2.x, 0.0, 0.0, projRoughness2.y) + 2.0 * LightLobeVariance * jjMat;



	// Compute determinant(filteredProjRoughnessMat) in a numerically stable manner.
	// See the supplementary document (Section 5.2) of the paper for the derivation.
	const float det = projRoughness2.x * projRoughness2.y + 2.0 * LightLobeVariance * (projRoughness2.x * jjMat._11 + projRoughness2.y * jjMat._22) + LightLobeVariance * LightLobeVariance * detJJ4;

	// NDF filtering in a numerically stable manner.
	// See the supplementary document (Section 5.2) of the paper for the derivation.
	const float tr = filteredProjRoughnessMat._11 + filteredProjRoughnessMat._22;
	const float2x2 filteredRoughnessMat = IsFinite(1.0 + tr + det) ? min(filteredProjRoughnessMat + float2x2(det, 0.0, 0.0, det), FLT_MAX) / (1.0 + tr + det) : (float2x2(min(filteredProjRoughnessMat._11, FLT_MAX) / min(filteredProjRoughnessMat._11 + 1.0, FLT_MAX), 0.0, 0.0, min(filteredProjRoughnessMat._22, FLT_MAX) / min(filteredProjRoughnessMat._22 + 1.0, FLT_MAX)));

	// Evaluate the filtered distribution.
	const float3 halfvecUnormalized = viewDirTS + mul(tangentFrame, LightLobe.axis);
	const float3 halfvec = halfvecUnormalized / max(length(halfvecUnormalized), EPSILON);
	const float pdf = SGGXReflectionPDF(viewDirTS, halfvec, filteredRoughnessMat);

	// Visibility of the SG light in the upper hemisphere.
	const float visibility = VMFHemisphericalIntegral(dot(prodDir, n), prodSharpness);// * (dot(n, to_light) >= 0 || TargetNode.radius >= sqrt(squareddist));

	// Eq. 12 of the paper.
	const float specularIllumination = amplitude * visibility * pdf * SGIntegral(LightLobe.sharpness);


	return max(emissive * (diffuseIllumination + metallic * specularIllumination), 0.f);
}













inline float cosSubClamped(float sinTheta_a, float cosTheta_a, float sinTheta_b, float cosTheta_b) {
	if(cosTheta_a > cosTheta_b) return 1;
	return cosTheta_a * cosTheta_b + sinTheta_a * sinTheta_b;
}
inline float sinSubClamped(float sinTheta_a, float cosTheta_a, float sinTheta_b, float cosTheta_b) {
	if(cosTheta_a > cosTheta_b) return 0;
	return sinTheta_a * cosTheta_b - cosTheta_a * sinTheta_b;
}

float Importance(const float3 p, const float3 n, LightBVHData node, bool HasHitTLAS)
{//Taken straight from pbrt
	float cosTheta_o = (2.0f * ((float)(node.cosTheta_oe & 0x0000FFFF) / 32767.0f) - 1.0f);
    float3 pc = (node.BBMax + node.BBMin) / 2.0f;
    float pDiff = dot(p - pc, p - pc);
    float d2 = max(pDiff, length(node.BBMax - node.BBMin) / 2.0f);

    float3 wi = (pc - p) / sqrt(pDiff);
    float cosTheta_w = abs(dot(i_octahedral_32(node.w), wi));
    float sinTheta_w = sqrt(max(1.0f - cosTheta_w * cosTheta_w, 0));

    float cosTheta_b;
    {
        float radius = (all(pc >= node.BBMin) && all(pc <= node.BBMax)) ? dot(node.BBMax - pc, node.BBMax - pc) : 0;
        if (pDiff < radius)
            cosTheta_b = -1.0f;
        else
            cosTheta_b = sqrt(max(1.0f - radius / pDiff, 0));
    }
    float sinTheta_b = sqrt(max(1.0f - cosTheta_b * cosTheta_b, 0));

    float sinTheta_o = sqrt(max(1.0f - cosTheta_o * cosTheta_o, 0));
    float cosTheta_x = cosSubClamped(sinTheta_w, cosTheta_w, sinTheta_o, cosTheta_o);
    float sinTheta_x = sinSubClamped(sinTheta_w, cosTheta_w, sinTheta_o, cosTheta_o);
    float cosThetap = cosSubClamped(sinTheta_x, cosTheta_x, sinTheta_b, cosTheta_b);
    if (cosThetap <= (2.0f * ((float)(node.cosTheta_oe >> 16) / 32767.0f) - 1.0f))
        return 0;

    float importance = node.phi * cosThetap / d2;

    float cosTheta_i = (dot(wi, n));
    float sinTheta_i = sqrt(max(1.0f - cosTheta_i * cosTheta_i, 0));
    float cosThetap_i = cosSubClamped(sinTheta_i, cosTheta_i, sinTheta_b, cosTheta_b);
    importance *= cosThetap_i;

    return max(importance, 0.f); // / (float) max(node.LightCount, 1);
}

#ifdef UseSGTree
int CalcInside(GaussianTreeNode A, GaussianTreeNode B, float3 p, int Index) {
	bool Residency0 = dot(p - A.position, p - A.position) <= A.radius * A.radius;
	bool Residency1 = dot(p - B.position, p - B.position) <= B.radius * B.radius;
#else
int CalcInside(LightBVHData A, LightBVHData B, float3 p, int Index) {
	bool Residency0 = all(p <= A.BBMax) && all(p >= A.BBMin);
	bool Residency1 = all(p <= B.BBMax) && all(p >= B.BBMin);
#endif
	if(Residency0 && Residency1) {
		return Index + 2;
	} else if(Residency0) {
		return 0;
	} else if(Residency1) {
		return 1;
	} else return -1;
}

void CalcLightPDF(inout float lightPDF, float3 p, float3 p2, float3 n, const int pixel_index, const int MeshIndex, const float2 sharpness, float3 viewDir, const float metallic) {
	int node_index = 0;
	int Reps = 0;
	bool HasHitTLAS = false;
	int NodeOffset = 0;
	float3 stack[12];
	int stacksize = 0;
	float RandNum = random(264, pixel_index).x;
	

	float3x3 tangentFrame = GetTangentSpace2(n);
	const float2 roughness2 = sharpness * sharpness;
	const float2 ProjRoughness2 = roughness2 / max(1.0 - roughness2, EPSILON);
	const float reflecSharpness = (1.0 - max(roughness2.x, roughness2.y)) / max(2.0f * max(roughness2.x, roughness2.y), EPSILON);

	while(Reps < 100) {
		Reps++;
#ifdef UseSGTree
		GaussianTreeNode node = SGTree[node_index];
#else
		LightBVHData node = SGTree[node_index];
#endif
		[branch]if(node.left >= 0) {
#ifdef UseSGTree
			const float2 ci = float2(
				SGImportance(SGTree[node.left + NodeOffset], viewDir, p, n, ProjRoughness2, reflecSharpness, tangentFrame, metallic),
				SGImportance(SGTree[node.left + 1 + NodeOffset], viewDir, p, n, ProjRoughness2, reflecSharpness, tangentFrame, metallic)
			);
#else
			const float2 ci = float2(
				Importance(p, n, SGTree[node.left + NodeOffset], HasHitTLAS),
				Importance(p, n, SGTree[node.left + 1 + NodeOffset], HasHitTLAS)
			);
#endif
			// if(ci.x == 0 && ci.y == 0) {pmf = -1; return;}

		    float sumweights = (ci.x + ci.y);
            float up = RandNum * sumweights;
            if (up == sumweights)
            {
                up = asfloat(asuint(up) - 1);
            }
            int offset = 0;
            float sum = 0;
            if(sum + ci[offset] <= up) sum += ci[offset++];
            if(sum + ci[offset] <= up) sum += ci[offset++];


			int Index = CalcInside(SGTree[node.left + NodeOffset], SGTree[node.left + NodeOffset + 1], p2, offset);
			if(Index == -1) {
				if(stacksize == 0) {return;}
				float3 tempstack = stack[--stacksize];
				node_index = tempstack.x;
				lightPDF = tempstack.y;
				RandNum = tempstack.z;
				continue;
			}
			if(Index >= 2) {
				Index -= 2;
				stack[stacksize++] = float3(node.left + NodeOffset + !Index, lightPDF * (ci[!Index] / sumweights), min((up - sum) / ci[!Index], 1.0f - (1e-6)));
			}
            RandNum = min((up - sum) / ci[Index], 1.0f - (1e-6));
            node_index = node.left + Index + NodeOffset;
            lightPDF *= ci[Index] / sumweights;
		} else {
			if(HasHitTLAS) {
				return;	
			} else {
				p = mul(_MeshData[MeshIndex].W2L, float4(p,1));
				p2 = mul(_MeshData[MeshIndex].W2L, float4(p2,1));
			    float3x3 Inverse = (float3x3)inverse(_MeshData[MeshIndex].W2L);
			    float scalex = length(mul(Inverse, float3(1,0,0)));
			    float scaley = length(mul(Inverse, float3(0,1,0)));
			    float scalez = length(mul(Inverse, float3(0,0,1)));
			    float3 Scale = pow(rcp(float3(scalex, scaley, scalez)),2);
				n = normalize(mul(_MeshData[MeshIndex].W2L, float4(n,0)).xyz / Scale);
				viewDir = normalize(mul(_MeshData[MeshIndex].W2L, float4(viewDir,0)).xyz / Scale);
				NodeOffset = _MeshData[MeshIndex].LightNodeOffset;
				node_index = NodeOffset;
				HasHitTLAS = true;
				tangentFrame = GetTangentSpace2(n);//Need to maybe check to see if this holds up when the traversal backtracks due to dead end
				if(MeshIndex != _LightMeshes[-(node.left+1)].LockedMeshIndex) {
					lightPDF = 1;
					return;
				}
			}
		}
	}

	return;
}






int SampleLightBVH(float3 p, float3 n, inout float pmf, const int pixel_index, inout int MeshIndex, const float2 sharpness, float3 viewDir, const float metallic) {
	int node_index = 0;
	int Reps = 0;
	bool HasHitTLAS = false;
	int NodeOffset = 0;
	int StartIndex = 0;

	float3x3 tangentFrame = GetTangentSpace2(n);
	const float2 roughness2 = sharpness * sharpness;
	const float2 ProjRoughness2 = roughness2 / max(1.0 - roughness2, EPSILON);
	const float reflecSharpness = (1.0 - max(roughness2.x, roughness2.y)) / max(2.0f * max(roughness2.x, roughness2.y), EPSILON);

	while(Reps < 122) {
		Reps++;
#ifdef UseSGTree
		GaussianTreeNode node = SGTree[node_index];
#else
		LightBVHData node = SGTree[node_index];
#endif
		[branch]if(node.left >= 0) {
#ifdef UseSGTree
			const float2 ci = float2(
				SGImportance(SGTree[node.left + NodeOffset], viewDir, p, n, ProjRoughness2, reflecSharpness, tangentFrame, metallic),
				SGImportance(SGTree[node.left + 1 + NodeOffset], viewDir, p, n, ProjRoughness2, reflecSharpness, tangentFrame, metallic)
			);
#else
			const float2 ci = float2(
				Importance(p, n, SGTree[node.left + NodeOffset], HasHitTLAS),
				Importance(p, n, SGTree[node.left + 1 + NodeOffset], HasHitTLAS)
			);
#endif
			if(ci.x == 0 && ci.y == 0) break;

			bool Index = random(264 + Reps, pixel_index).x >= (ci.x / (ci.x + ci.y));
			pmf /= ((ci[Index] / (ci.x + ci.y)));
			node_index = node.left + Index + NodeOffset;
			
		} else {
			[branch]if(HasHitTLAS) {
				return -(node.left+1) + StartIndex;	
			} else {
				StartIndex = _LightMeshes[-(node.left+1)].StartIndex; 
				MeshIndex = _LightMeshes[-(node.left+1)].LockedMeshIndex;
			    float3x3 Inverse = (float3x3)inverse(_MeshData[MeshIndex].W2L);
			    float scalex = length(mul(Inverse, float3(1,0,0)));
			    float scaley = length(mul(Inverse, float3(0,1,0)));
			    float scalez = length(mul(Inverse, float3(0,0,1)));
			    float3 Scale = pow(rcp(float3(scalex, scaley, scalez)),2);
				p = mul(_MeshData[MeshIndex].W2L, float4(p,1));
				n = normalize(mul(_MeshData[MeshIndex].W2L, float4(n,0)).xyz / Scale);
				viewDir = normalize(mul(_MeshData[MeshIndex].W2L, float4(viewDir,0)).xyz / Scale);
				tangentFrame = GetTangentSpace2(n);
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
	if(Target.w == 1) return asfloat(Target.xyz);
    MyMeshDataCompacted Mesh = _MeshData[Target.x];
    Target.y += Mesh.TriOffset;
    float2 TriUV;
    TriUV.x = asfloat(Target.z);
    TriUV.y = asfloat(Target.w);
    float4x4 Inverse = inverse(Mesh.W2L);
    return mul(Inverse, float4(AggTris[Target.y].pos0 + TriUV.x * AggTris[Target.y].posedge1 + TriUV.y * AggTris[Target.y].posedge2,1)).xyz;
}


void Unity_Hue_Degrees_float(float3 In, float Offset, out float3 Out) {
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

void Unity_Saturation_float(float3 In, float Saturation, out float3 Out) {
    float luma = dot(In, float3(0.2126729, 0.7151522, 0.0721750));
    Out =  luma.xxx + Saturation.xxx * (In - luma.xxx);
}

void Unity_Contrast_float(inout float3 A, float Contrast) {
    float midpoint = pow(0.5, 2.2);
    A =  (A - midpoint) * Contrast + midpoint;
}

float3 DeSat(float3 In, float Saturation) {
    float luma = dot(In, float3(0.2126729, 0.7151522, 0.0721750));
    return  luma.xxx + Saturation.xxx * (In - luma.xxx);
}





#define LAYERS            5.0

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

StructuredBuffer<float> TotSum;
float2 HDRIParams;

float3 equirectUvToDirection(float2 uv) {
    uv.x -= 0.5;
    uv.y = 1.0 - uv.y;

    float theta = uv.x * 2.0 * PI;
    float phi = uv.y * PI;
    float sinPhi, sinTheta;
    sincos(phi, sinPhi, phi);
    sincos(theta, sinTheta, theta);

    return -float3(sinPhi * theta, phi, sinPhi * sinTheta);
}

float2 equirectDirectionToUv(float3 direction) {
    float2 uv = float2(atan2(direction.z, direction.x), acos(direction.y));
    uv /= float2(2.0 * PI, PI);

    uv.x += 0.5;
    uv.y = 1.0 - uv.y;
    return uv;
}

float equirectDirectionPdf(float3 direction) {
    float2 uv = equirectDirectionToUv(direction);
    float theta = uv.y * PI;
    float sinTheta = sin( theta );
    if (sinTheta == 0.0)
        return 0.0;

    return 1.0 / (2.0 * PI * PI * sinTheta);
}

Texture2D<float> CDFX;
Texture2D<float> CDFY;
Texture2D<float4> _SkyboxTexture;
SamplerState sampler_SkyboxTexture;
int FindInterval(int size, float u, Texture2D<float> CDF, int v = 0) {
    int first = 0, len = size;
    while (len > 0) {
        int hal = len >> 1, middle = first + hal;
        // Bisect range based on value of _pred_ at _middle_
        if (CDF[int2(middle, v)] <= u) {
            first = middle + 1;
            len -= hal + 1;
        } else
            len = hal;
    }
    return clamp(first - 1, 0, size - 2);
}
float2 HDRILongLat;
float2 HDRIScale;
float3 SampleLI(int pixel_index, inout float pdf, inout float3 wi) {
    float2 Rand = random(94, pixel_index);
    float2 uv;
    int v, offset;
    {
        v = FindInterval(HDRIParams.y, Rand.x, CDFY);

        float du = Rand.x - CDFY[int2(v, 0)];
        float diff = (CDFY[int2(v + 1, 0)] - CDFY[int2(v, 0)]);
        if(diff > 0)
            du /= diff;

        uv.y = ((float)v + du) / HDRIParams.y;
    }

    {
        offset = FindInterval(HDRIParams.x, Rand.y, CDFX, v);
        float du = Rand.y - CDFX[int2(offset, v)];
        float diff = (CDFX[int2(offset + 1, v)] - CDFX[int2(offset, v)]);
        if(diff > 0)
            du /= diff;

        uv.x = ((float)offset + du) / HDRIParams.x;
    }
    float2 uv2 = fmod(uv * HDRIScale + HDRILongLat / 360.0f, 1.0f);
    wi = equirectUvToDirection(uv2);

    return _SkyboxTexture.SampleLevel(my_linear_clamp_sampler, uv, 0);
}	

uint GetBounceData(uint A) {
	return (A & 0x7C000000) >> 26;
}
uint ToColorSpecPackedAdd(float3 A, uint B) {
	return (B & ~0xFFFFFE00) | ((uint)(A.x * 1022.0f)) | ((uint)(A.y * 1022.0f) << 10) | ((uint)A.z << 20);
}
uint ToColorSpecPacked(float3 A) {
	return ((uint)(A.x * 1022.0f)) | ((uint)(A.y * 1022.0f) << 10) | ((uint)A.z << 20);
}
float3 FromColorSpecPacked(uint A) {
	return float3(
		(A & 0x3FF) / 1022.0f,
		((A >> 10) & 0x3FF) / 1022.0f,
		((A >> 20) & 0x3)
		);
}

inline float3 CalcPos(uint4 TriData) {
	if(TriData.w > 0) return asfloat(TriData.xyz);
    MyMeshDataCompacted Mesh = _MeshData[TriData.x];
    float4x4 Inverse = inverse(Mesh.W2L);
    TriData.y += Mesh.TriOffset;
	return mul(Inverse, float4(AggTris[TriData.y].pos0 + ((TriData.z & 0xffff) / 65535.0f) * AggTris[TriData.y].posedge1 + ((TriData.z >> 16) / 65535.0f) * AggTris[TriData.y].posedge2,1)).xyz;
}





#define WorldCacheScale 50.0f
#define GridBias 2
#define BucketCount 32
#define PropDepth 4
#define MinSampleToContribute 0
#define MaxSampleCount 32
#define CacheCapacity (4 * 1024 * 1024)

RWByteAddressBuffer VoxelDataBufferA;
ByteAddressBuffer VoxelDataBufferB;
#define HashKeyValue uint3
RWStructuredBuffer<HashKeyValue> HashEntriesBuffer;//May want to leave the last bit unused, so I can use it as a "written to" flag instead of having to compare both A and B components for empty
RWStructuredBuffer<HashKeyValue> HashEntriesBufferA2;//May want to leave the last bit unused, so I can use it as a "written to" flag instead of having to compare both A and B components for empty
StructuredBuffer<HashKeyValue> HashEntriesBufferB;//May want to leave the last bit unused, so I can use it as a "written to" flag instead of having to compare both A and B components for empty

float CalcVoxelSize(float3 VertPos) {
    uint GridLayer = clamp(floor(log2(distance(CamPos, VertPos)) + GridBias), 1, ((1u << 10) - 1));
    return pow(2, GridLayer) / (float)(WorldCacheScale * 4);
}

int4 CalculateCellParams(float3 VertexPos) {
	float LogData = log2(distance(CamPos, VertexPos));
	uint Layer = clamp(floor(LogData) + GridBias, 1, ((1u << 10) - 1));
	return int4(floor((VertexPos * WorldCacheScale * 4) / pow(2, Layer)), Layer);
}

uint2 CompressHash(uint4 CellParams, uint NormHash) {
	uint2 HashValue;
	HashValue.x = (CellParams.x & ((1u << 17) - 1)) | ((CellParams.y & ((1u << 17) - 1)) << 17);
	HashValue.y = ((CellParams.y & ((1u << 17) - 1)) >> 15) | ((CellParams.z & ((1u << 17) - 1)) << 2) | ((CellParams.w & ((1u << 10) - 1)) << 19) | (NormHash << 29);
	return HashValue;
}

uint2 ComputeHash(float3 Position, float3 Norm) {
	uint4 CellParams = asuint(CalculateCellParams(Position));
	uint NormHash =
        (Norm.x >= 0 ? 1 : 0) +
        (Norm.y >= 0 ? 2 : 0) +
        (Norm.z >= 0 ? 4 : 0);

	return CompressHash(CellParams, NormHash);
}

//Using third hash from here: http://burtleburtle.net/bob/hash/integer.html
uint Hash32Bit(uint a) {
  	a -= (a<<6);
    a ^= (a>>17);
    a -= (a<<9);
    a ^= (a<<4);
    a -= (a<<3);
    a ^= (a<<10);
    a ^= (a>>15);
    return a;
}

uint Hash32(uint2 HashValue) {
    return Hash32Bit(HashValue.x & 0xffffffff)
         ^ Hash32Bit(HashValue.y & 0xffffffff);
}


#define PathLengthBits PropDepth
#define PathLengthMask ((1u << PathLengthBits) - 1)


float3 RTXDI_RGBToXYZInRec709(float3 c) {
    static const float3x3 M = float3x3(
        0.4123907992659595, 0.3575843393838780, 0.1804807884018343,
        0.2126390058715104, 0.7151686787677559, 0.0721923153607337,
        0.0193308187155918, 0.1191947797946259, 0.9505321522496608
    );
    return mul(M, c);
}
float3 RTXDI_XYZToRGBInRec709(float3 c) {
    static const float3x3 M = float3x3(
        3.240969941904522, -1.537383177570094, -0.4986107602930032,
        -0.9692436362808803, 1.875967501507721, 0.04155505740717569,
        0.05563007969699373, -0.2039769588889765, 1.056971514242878
    );

    return mul(M, c);
}

	uint EncodeRGB(float3 Col) {
		float3 XYZ = RTXDI_RGBToXYZInRec709(Col);
		float logY = 409.6 * (log2(XYZ.y) + 20.0);
    	uint Le = uint(clamp(logY, 0.0, 16383.0));
    	if(Le == 0) return 0;

		float invDenom = 1.0 / (-2.0 * XYZ.x + 12.0 * XYZ.y + 3.0 * (XYZ.x + XYZ.y + XYZ.z));
	    float2 uv = float2(4.0, 9.0) * XYZ.xy * invDenom;

	    uint2 uve = uint2(clamp(820.0 * uv, 0.0, 511.0));

	    return (Le << 18) | (uve.x << 9) | uve.y;
	}

	float3 DecodeRGB(uint packedColor) {
		uint Le = packedColor >> 18;
	    if (Le == 0) return float3(0, 0, 0);

	    float logY = (float(Le) + 0.5) / 409.6 - 20.0;
	    float Y = pow(2.0, logY);

	    uint2 uve = uint2(packedColor >> 9, packedColor) & 0x1ff;
	    float2 uv = (float2(uve)+0.5) / 820.0;

	    float invDenom = 1.0 / (6.0 * uv.x - 16.0 * uv.y + 12.0);
	    float2 xy = float2(9.0, 4.0) * uv * invDenom;

	    float s = Y / xy.y;
	    float3 XYZ = float3(s * xy.x, Y, s * (1.f - xy.x - xy.y));

	    return max(RTXDI_XYZToRGBInRec709(XYZ), 0.0);
	}

	struct PropogatedCacheData {
		uint2 samples[PropDepth];
		uint throughput;//not needed, just padding, if I could remove one other uint, than I could bump prop_depth up to 5
		uint pathLength;
		uint CurrentIlluminance;
		uint Norm;
	};
	RWStructuredBuffer<PropogatedCacheData> CacheBuffer;



	struct GridVoxel {
	    float3 radiance;
	    uint sampleNum;
	};

	GridVoxel RetrieveCacheData(uint CacheEntry) {
	    if (CacheEntry == 0xFFFFFFFF) return (GridVoxel)0;
	    uint4 voxelDataPacked = VoxelDataBufferB.Load4(CacheEntry * 16);

	    GridVoxel Voxel;
		Voxel.radiance = voxelDataPacked.xyz / 1e3f;
	    Voxel.sampleNum = (voxelDataPacked.w) & ((1u << 20) - 1);

	    return Voxel;
	}

	void AddDataToCache(uint CacheEntry, uint4 Values) {
	    if (CacheEntry == 0xFFFFFFFF) return;
	    CacheEntry *= 16;
	    [unroll]for(int i = 0; i < 4; i++)
	    	if(Values[i] != 0) VoxelDataBufferA.InterlockedAdd(CacheEntry + 4 * i, Values[i]);
	}


	inline uint FindOpenHash(const uint2 HashValue) {
		uint BucketIndex;
		uint hash = Hash32(HashValue);
	    uint HashIndex = hash % CacheCapacity;
	    uint baseSlot = floor(HashIndex / (float)BucketCount) * BucketCount;
	    uint temp = 0;
		if(baseSlot < CacheCapacity) {
			for(int i = 0; i < BucketCount; i++) {	
				InterlockedExchange(HashEntriesBuffer[baseSlot + i].z, 0xAAAAAAAA, BucketIndex);
				if(BucketIndex != 0xAAAAAAAA && HashEntriesBuffer[baseSlot + i].x == 0 && HashEntriesBuffer[baseSlot + i].y == 0) {
					HashEntriesBuffer[baseSlot + i].xy = HashValue.xy;
					// InterlockedExchange(HashEntriesBuffer[baseSlot + i].z, BucketIndex, temp);//May be bad
					return baseSlot + i;
				} else {
					HashKeyValue OldHashValue = HashEntriesBuffer[baseSlot + i];
					if((OldHashValue.x == 0 && OldHashValue.y == 0) || (OldHashValue.x == HashValue.x && OldHashValue.y == HashValue.y)) return baseSlot + i;
				}
			}
		}
		return 0;
	}

	inline bool HashGridFind(const uint2 HashValue, inout uint cacheEntry) {
	    uint hash = Hash32(HashValue);
	    uint HashIndex = hash % CacheCapacity;

	    uint baseSlot = floor(HashIndex / (float)BucketCount) * BucketCount;
	    for (uint i = 0; i < BucketCount; i++) {
	        uint2 PrevHash = HashEntriesBufferB[baseSlot + i];//I am read and writing to the same hash buffer, this could be a problem

	        if (PrevHash.x == HashValue.x && PrevHash.y == HashValue.y) {
	            cacheEntry = baseSlot + i;
	            return true;
	        } else if (PrevHash.x == 0 && PrevHash.y == 0) break;
	    }
	    return false;
	}


	inline bool RetrieveCacheRadiance(PropogatedCacheData CurrentProp, float3 Pos, float3 Norm, out float3 radiance) {
		uint2 HashValue = ComputeHash(Pos, Norm);
	   	uint CacheEntry = 0xFFFFFFFF;
	   	HashGridFind(HashValue, CacheEntry);

	    GridVoxel voxelData = RetrieveCacheData(CacheEntry);
	    if (voxelData.sampleNum > MinSampleToContribute) {
	        radiance = voxelData.radiance / (float)voxelData.sampleNum;
	        return true;
	    }
	    return false;
	}


	inline bool AddHitToCache(inout PropogatedCacheData CurrentProp, float3 Pos, float random) {
	    bool EarlyOut = false;
	    float3 OrigLighting = clamp(DecodeRGB(CurrentProp.CurrentIlluminance), 0, 1200.0f);
	    float3 ModifiedLighting = OrigLighting;
	    int PathLength = (CurrentProp.pathLength & PathLengthMask);

	   	uint CacheEntry = FindOpenHash(ComputeHash(Pos, i_octahedral_32(CurrentProp.Norm)));
	    if (random * 3.0f + 1.0f <= PathLength) {
	        GridVoxel Voxel = RetrieveCacheData(CacheEntry);
            EarlyOut = Voxel.sampleNum > MinSampleToContribute;
	        if (EarlyOut) ModifiedLighting = Voxel.radiance / Voxel.sampleNum;
	    }
	    if(!EarlyOut) AddDataToCache(CacheEntry, uint4(OrigLighting * 1e3f, 1));
	    for (int i = 0; i < PathLength; ++i) {
	        ModifiedLighting *= DecodeRGB(CurrentProp.samples[i].x);
	        AddDataToCache(CurrentProp.samples[i].y, uint4(ModifiedLighting * 1e3f, 0));
	    }
        if(PathLength >= 3) CurrentProp.samples[3] = CurrentProp.samples[2];
        if(PathLength >= 2) CurrentProp.samples[2] = CurrentProp.samples[1];
        if(PathLength >= 1) CurrentProp.samples[1] = CurrentProp.samples[0];
	    CurrentProp.samples[0].y = CacheEntry;
	    CurrentProp.pathLength = (CurrentProp.pathLength & ~PathLengthMask) | min(PathLength + 1, PropDepth - 1);

	    return !EarlyOut;
	}

	inline void AddMissToCache(inout PropogatedCacheData CurrentProp, float3 radiance) {
	    for (int i = 0; i < (CurrentProp.pathLength & PathLengthMask); ++i) {
	        radiance *= DecodeRGB(CurrentProp.samples[i].x);
	        AddDataToCache(CurrentProp.samples[i].y, uint4(radiance * 1e3f, 0));
	    }
	}

	uint2 GetReprojectedHash(uint2 HashValue) {
	    int3 gridPosition;
	    gridPosition.x = int((HashValue.x) & ((1u << 17) - 1));
	    gridPosition.y = int((((HashValue.y << 15)) | (HashValue.x >> 17)) & ((1u << 17) - 1));
	    gridPosition.z = int((HashValue.y >> 2) & ((1u << 17) - 1));

	    //checking 16th bit as thats the max resolution of the grid hash, and if its negative, cant you do this with msb?
	    //This is faster than calling firstbithigh as you know the index to check, aka the highest possible bit based off of max grid resolution
	    gridPosition = (gridPosition & (1 << (17 - 1))) ? (gridPosition | (~((1 << 17) - 1))) : gridPosition;

	    int level = uint((HashValue.y >> 19) & ((1u << 10) - 1));

	    float voxelSize = pow(2, level) / (WorldCacheScale * 4.0f);
	    int3 CamPosDiff = floor(CamPos / voxelSize) - gridPosition;
	    int3 CamPosDiffPrev = floor(PrevCamPos / voxelSize) - gridPosition;

	    if (dot(CamPosDiff, CamPosDiff) < dot(CamPosDiffPrev, CamPosDiffPrev)) {
	        gridPosition = floor(gridPosition / 2.0f);
	        level = min(level + 1, ((1u << 10) - 1));
	    } else {
	        gridPosition = floor(gridPosition * 2);
	        level = max(level - 1, 1);
	    }

	    return CompressHash(asuint(int4(gridPosition, level)), HashValue.y >> 29); 
	}





inline int SelectUnityLight(int pixel_index, inout float lightWeight, float3 Norm, float3 Position, float3 RayDir) {
    float2 Rand;
    int MinIndex = 0;
    float wsum = 0;
    float MinP_Hat = 0;
    float3 to_light;
    int Index;
    float LengthSquared;
    float p_hat;
    if(RISCount == 0) {
    	lightWeight *= (float)unitylightcount;
	    return clamp(( random(11, pixel_index).x * unitylightcount), 0, unitylightcount - 1);

    }

    for(int i = 0; i < RISCount + 1; i++) {
        Rand = random(i + 11, pixel_index);
        Index = clamp((Rand.x * unitylightcount), 0, unitylightcount - 1);
        LightData light = _UnityLights[Index];
        to_light = (light.Type == DIRECTIONALLIGHT ? (light.Direction * 120000.0f + Position) : light.Position) - Position;
        LengthSquared = dot(to_light, to_light);
        to_light /= sqrt(LengthSquared);

        if(light.Type == AREALIGHTQUAD|| light.Type == AREALIGHTDISK) {
            float3 RandVec = float3(random(i + 292, pixel_index), random(i + 392, pixel_index).x);
            float sinPhi, cosPhi;
            sincos(light.ZAxisRotation, sinPhi, cosPhi);
            switch(light.Type) {
                 case AREALIGHTQUAD:
                    RandVec.xy = RandVec.xy * light.SpotAngle - light.SpotAngle / 2.0f;
                    RandVec.xy = mul(float2x2(cosPhi, -sinPhi, sinPhi, cosPhi), RandVec.xy);
                    light.Position += ToWorld(GetTangentSpace2(light.Direction), normalize(float3(RandVec.x,0,RandVec.y))) * length(RandVec.xy);
                    // area = Light.SpotAngle.x * Light.SpotAngle.y;
                    // if(hitDat.MatType == 3) Radiance = 0;
                    // light.Radiance *= (light.SpotAngle.x * light.SpotAngle.y) / 2.0f;
                break;
                case AREALIGHTDISK:
                    sincos(RandVec.x * 2.0f * PI, RandVec.x, RandVec.y);
                    RandVec.xy = mul(float2x2(cosPhi, -sinPhi, sinPhi, cosPhi), RandVec.xy) * RandVec.z * light.SpotAngle.x;
                    light.Position += ToWorld(GetTangentSpace2(light.Direction), normalize(float3(RandVec.x,0,RandVec.y))) * length(RandVec.xy);
                    // area = PI * Light.SpotAngle.x * Light.SpotAngle.x;
                    // light.Radiance *= PI * light.SpotAngle.x * light.SpotAngle.x;
                break;
            }

            to_light = light.Position - Position;
            LengthSquared = dot(to_light, to_light);
            to_light /= sqrt(LengthSquared);
            float PDF = saturate(dot(to_light, -light.Direction));
            if(PDF > 0) p_hat = max(luminance(light.Radiance) / (LengthSquared * max(PDF, 0.1f)),0);
        	else p_hat = 0;
        } else {
            p_hat = max(luminance(light.Radiance) / ((light.Type == DIRECTIONALLIGHT) ? 12.0f : LengthSquared) * ((light.Type == SPOTLIGHT) ? saturate((light.Softness * light.Softness) + saturate(dot(to_light, -light.Direction)) * light.SpotAngle.x + light.SpotAngle.y) : 1)* (dot(to_light, Norm) > 0),0);
        	// float PDF = (((light.Type == DIRECTIONALLIGHT) ? 10.0f : LengthSquared) * ((light.Type == SPOTLIGHT) ? saturate(saturate(dot(to_light, -light.Direction)) * light.SpotAngle.x + light.SpotAngle.y) : 1)* (dot(to_light, Norm) > 0));
            // if(PDF > 0.01f) p_hat = max(luminance(light.Radiance) / PDF,0);
        	// else p_hat = 0;
        }
        wsum += p_hat;
        if(Rand.y < p_hat / wsum) {
            MinIndex = Index;
            MinP_Hat = p_hat;
        }

    }
    lightWeight *= (wsum / max((RISCount + 1) *MinP_Hat, 0.000001f)) * (float)unitylightcount;
    return MinIndex;
}

inline int SelectLight(const uint pixel_index, inout uint MeshIndex, inout float lightWeight, float3 Norm, float3 Position, float4x4 Transform, inout float3 Radiance, inout float3 FinalPos, float2 sharpness, float3 viewDir, float metallic) {//Need to check these to make sure they arnt simply doing uniform sampling

    float2 TriangleUV;
    int MeshTriOffset = _MeshData[_LightMeshes[MeshIndex].LockedMeshIndex].TriOffset;
    int MinIndex = 0;
    int MatOffset =_MeshData[_LightMeshes[MeshIndex].LockedMeshIndex].MaterialOffset;
    float2 FinalUV;
    #ifndef LBVH
        const int StartIndex = _LightMeshes[MeshIndex].StartIndex;
        const int LightCount = _LightMeshes[MeshIndex].IndexEnd - StartIndex;
        float3x3 Inverse = (float3x3)inverse(Transform);
        float scalex = length(mul(Inverse, float3(1,0,0)));
        float scaley = length(mul(Inverse, float3(0,1,0)));
        float scalez = length(mul(Inverse, float3(0,0,1)));
        float3 Scale = pow(rcp(float3(scalex, scaley, scalez)),2);
        Position = mul(Transform, float4(Position,1));
        Norm = normalize(mul(Transform, float4(Norm,0)).xyz / Scale);

        float2 Rand;
        float wsum = 0;
        float MinP_Hat = 0;
        int Index;
        float2 Rand2;
        float3 LightPosition;
        float p_hat;
        int CounCoun = 0;
        for(int i = 0; i < RISCount + 1; i++) {
            Rand = random(46+i, pixel_index);
            Index = (Rand.x * LightCount) + StartIndex;
            Rand2 = random(79 + i, pixel_index);

            TriangleUV = sample_triangle(Rand2.x, Rand2.y);
            LightPosition = (LightTriangles[Index].pos0 + LightTriangles[Index].posedge1 * TriangleUV.x + LightTriangles[Index].posedge2 * TriangleUV.y - Position);
            p_hat = rcp(dot(LightPosition, LightPosition));
            if(dot(normalize(LightPosition), Norm) < 0) continue;
            wsum += p_hat;
            CounCoun++;
            [branch]if(Rand.y < p_hat / wsum) {
                FinalUV = TriangleUV;
                MinIndex = Index;
                MinP_Hat = p_hat;
                FinalPos = LightPosition;
            }

        }
        if(CounCoun == 0) return -1;
        FinalPos += Position;
        lightWeight *= (wsum / max((CounCoun) * MinP_Hat, 0.000001f) * LightCount);
    #else
        MinIndex = SampleLightBVH(Position, Norm, lightWeight, pixel_index, MeshIndex, sharpness, viewDir, metallic);
        if(MinIndex == -1) return -1;
        MeshTriOffset = _MeshData[MeshIndex].TriOffset;
        MatOffset =_MeshData[MeshIndex].MaterialOffset;

        float2 Rand2 = random(79, pixel_index);
        FinalUV = sample_triangle(Rand2.x, Rand2.y);
        FinalPos = (LightTriangles[MinIndex].pos0 + LightTriangles[MinIndex].posedge1 * FinalUV.x + LightTriangles[MinIndex].posedge2 * FinalUV.y);
    #endif
    int AggTriIndex = LightTriangles[MinIndex].TriTarget + MeshTriOffset;
    int MaterialIndex = AggTris[AggTriIndex].MatDat + MatOffset;
    MaterialData MatDat = _Materials[MaterialIndex];
    #ifdef AdvancedBackground
    	if(GetFlag(MatDat.Tag, IsBackground)) return -1;
    #endif
    #ifdef WhiteLights
        MatDat.surfaceColor = 0.5f;
    #else
        float2 BaseUv = TOHALF(AggTris[AggTriIndex].tex0) * (1.0f - FinalUV.x - FinalUV.y) + TOHALF(AggTris[AggTriIndex].texedge1) * FinalUV.x + TOHALF(AggTris[AggTriIndex].texedge2) * FinalUV.y;
        if(MatDat.AlbedoTex.x > 0)
        	MatDat.surfaceColor *= SampleTexture(BaseUv, SampleAlbedo, MatDat);

        float3 TempCol = MatDat.surfaceColor;
        #ifndef DX11
            Unity_Hue_Degrees_float(TempCol, MatDat.Hue * 500.0f, MatDat.surfaceColor);
        #endif
        MatDat.surfaceColor *= MatDat.Brightness;
        TempCol = MatDat.surfaceColor;
        Unity_Saturation_float(TempCol, MatDat.Saturation, MatDat.surfaceColor);
        Unity_Contrast_float(MatDat.surfaceColor, MatDat.Contrast);
        MatDat.surfaceColor = saturate(MatDat.surfaceColor);

        if ((MatDat.EmissiveTex.x > 0 && MatDat.emission >= 0)) {
            float3 EmissCol = lerp(MatDat.EmissionColor, MatDat.surfaceColor, GetFlag(MatDat.Tag, BaseIsMap));
    		float4 EmissTex = SampleTexture(BaseUv, SampleEmission, MatDat);
            if(!GetFlag(MatDat.Tag, IsEmissionMask)) {//IS a mask
                MatDat.emission *= luminance(EmissTex.xyz);
                
                MatDat.surfaceColor = lerp(MatDat.surfaceColor, EmissCol, saturate(MatDat.emission) * GetFlag(MatDat.Tag, ReplaceBase));
            } else {//is NOT a mask
                MatDat.surfaceColor = lerp(MatDat.surfaceColor, EmissTex.xyz * EmissCol, saturate(MatDat.emission) * GetFlag(MatDat.Tag, ReplaceBase));
            }            
        }
    #endif
    Radiance = MatDat.emission * MatDat.surfaceColor;
    return AggTriIndex;
}


inline float3 temperature(float t)
{
    const static float3 c[10] = {
        float3(0.0f / 255.0f, 2.0f / 255.0f, 91.0f / 255.0f),
        float3(0.0f / 255.0f, 108.0f / 255.0f, 251.0f / 255.0f),
        float3(0.0f / 255.0f, 221.0f / 255.0f, 221.0f / 255.0f),
        float3(51.0f / 255.0f, 221.0f / 255.0f, 0.0f / 255.0f),
        float3(255.0f / 255.0f, 252.0f / 255.0f, 0.0f / 255.0f),
        float3(255.0f / 255.0f, 180.0f / 255.0f, 0.0f / 255.0f),
        float3(255.0f / 255.0f, 104.0f / 255.0f, 0.0f / 255.0f),
        float3(226.0f / 255.0f, 22.0f / 255.0f, 0.0f / 255.0f),
        float3(191.0f / 255.0f, 0.0f / 255.0f, 83.0f / 255.0f),
        float3(145.0f / 255.0f, 0.0f / 255.0f, 65.0f / 255.0f)
    };

    const float s = t * 10.0f;

    const int cur = int(s) <= 9 ? int(s) : 9;
    const int prv = cur >= 1 ? cur - 1 : 0;
    const int nxt = cur < 9 ? cur + 1 : 9;

    const float blur = 0.8f;

    const float wc = smoothstep(float(cur) - blur, float(cur) + blur, s) * (1.0f - smoothstep(float(cur + 1) - blur, float(cur + 1) + blur, s));
    const float wp = 1.0f - smoothstep(float(cur) - blur, float(cur) + blur, s);
    const float wn = smoothstep(float(cur + 1) - blur, float(cur + 1) + blur, s);

    const float3 r = wc * c[cur] + wp * c[prv] + wn * c[nxt];
    return float3(clamp(r.x, 0.0f, 1.0f), clamp(r.y, 0.0f, 1.0f), clamp(r.z, 0.0f, 1.0f));
}



	float3 pal(float t) {
	    float3 a = float3(0.5f, 0.5f, 0.5f);
	    float3 b = float3(0.5f, 0.5f, 0.5f);
	    float3 c = float3(0.8f, 0.8f, 0.8f);
	    float3 d = float3(0.0f, 0.33f, 0.67f) + 0.21f;
	    return a + b*cos( 6.28318*(c*t+d) );
	}

#if DebugView != -1
	float4 HandleDebug(int Index) {
        static const float one_over_max_unsigned = asfloat(0x2f7fffff);
        uint hash = pcg_hash(32);
        float LinearIndex = hash_with(Index, hash) * one_over_max_unsigned;
        return float4(pal(LinearIndex), 1);
    }
#endif