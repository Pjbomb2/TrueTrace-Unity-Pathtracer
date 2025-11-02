#define ONE_OVER_PI 0.318309886548f
#define PI 3.14159265359f
#define EPSILON 1e-8

#include "CommonStructs.cginc"
#define TOHALF(x) float2(f16tof32(x >> 16), f16tof32(x & 0xFFFF))

bool OIDNGuideWrite;

float4x4 CamToWorld;
float4x4 CamInvProj;
float4x4 CamToWorldPrev;
float4x4 CamInvProjPrev;
float4x4 viewprojection;
int LightTreePrimaryTLASOffset;

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

float4x4 ViewMatrix;
int MaxBounce;
int CurBounce;

uint screen_width;
uint screen_height;
int frames_accumulated;
int curframe;//might be able to get rid of this
int MainDirectionalLight;
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
float2 Segment;
bool DoPanorama;

float ClayMetalOverrideValue;
float ClayRoughnessOverrideValue;

RWStructuredBuffer<uint3> BufferData;

#ifdef HardwareRT
	StructuredBuffer<int> SubMeshOffsets;
	StructuredBuffer<float2> MeshOffsets;
#endif

float aoStrength;
float aoRadius;

struct BufferSizeData {
	int tracerays;
	int shadow_rays;
	int heighmap_rays;
	int heightmap_shadow_rays;
	int TracedRays;
	int TracedRaysShadow;
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
RWTexture2D<float2> MVTexture;

RWTexture2D<float4> ScreenSpaceInfo;
Texture2D<float4> ScreenSpaceInfoRead;
Texture2D<float4> PrevScreenSpaceInfo;

bool DoExposure;
StructuredBuffer<float> Exposure;



RWTexture2DArray<uint4> ReservoirA;
Texture2DArray<uint4> ReservoirB;

RWTexture2D<uint4> WorldPosA;
Texture2D<uint4> WorldPosB;
RWTexture2D<uint4> WorldPosC;

RWTexture2D<half4> NEEPosA;
Texture2D<half4> NEEPosB;

RWTexture2D<float4> Result;
Texture2D<float> _AlphaAtlas;

Texture2D<uint4> PrimaryTriData;
Texture2D<uint4> PrimaryTriDataPrev;
StructuredBuffer<int> TLASBVH8Indices;

int AlbedoAtlasSize;
struct TrianglePos {
	float3 pos0;
	float3 posedge1;
	float3 posedge2;
};
struct TriangleUvs {
	float2 UV0;
	float2 UV1;
	float2 UV2;
};


inline TrianglePos triangle_get_positions(const int ID) {
	TrianglePos tri;
	tri.pos0 = AggTrisA.Load(ID).pos0;
	tri.posedge1 = AggTrisA.Load(ID).posedge1;
	tri.posedge2 = AggTrisA.Load(ID).posedge2;
	return tri;
}

inline TriangleUvs triangle_get_UVs(const int ID) {
	TriangleUvs tri;
	tri.UV0 = TOHALF(AggTrisA.Load(ID).tex0);
	tri.UV1 = TOHALF(AggTrisA.Load(ID).texedge1);
	tri.UV2 = TOHALF(AggTrisA.Load(ID).texedge2);
	return tri;
}

SamplerState my_linear_clamp_sampler;
SamplerState sampler_trilinear_clamp;
SamplerState my_point_clamp_sampler;
SamplerState my_trilinear_repeat_sampler;
SamplerState my_point_repeat_sampler;

Texture2D<half> SingleComponentAtlas;
Texture2D<float4> RandomNums;
RWTexture2D<float4> _DebugTex;

Texture2D<half4> _TextureAtlas;
SamplerState sampler_TextureAtlas;
Texture2D<half2> _NormalAtlas;
SamplerState sampler_NormalAtlas;
Texture2D<half4> _EmissiveAtlas;
Texture2D<half> _IESAtlas;

Texture2D<half> Heightmap;
Texture2D<float4> TerrainAlphaMap;
SamplerState sampler_TerrainAlphaMap;
int TerrainCount;
bool TerrainExists;

int MaterialCount;
int LightMeshCount;


#if !defined(DX11)
	Texture2D<float4> _BindlessTextures[2048] : register(t31);
#endif

inline void HandleRotation(inout float2 UV, float Rotation) {
	if(Rotation != 0) {
		float sinc, cosc;
		sincos(Rotation / 180.0f * 3.14159f, sinc, cosc);
		UV -= 0.5f;
		float2 tempuv = UV;
		UV.x = tempuv.x * cosc - tempuv.y * sinc;
		UV.y = tempuv.x * sinc + tempuv.y * cosc;
		UV += 0.5f;
	}
}
inline float2 AlignUV(float2 BaseUV, float4 TexScale, int2 TexDim2, float Rotation = 0) {
	if(TexDim2.x <= 0) return -1;
	float4 TexDim;
    TexDim.xy = float2((float)(((uint)TexDim2.x) & 0x7FFF) / 16384.0f, (float)(((uint)TexDim2.x >> 15)) / 16384.0f);
    TexDim.zw = float2((float)(((uint)TexDim2.y) & 0x7FFF) / 16384.0f, (float)(((uint)TexDim2.y >> 15)) / 16384.0f);
	BaseUV = BaseUV * TexScale.xy + TexScale.zw;
	BaseUV = (BaseUV < 0 ? (1.0f - fmod(abs(BaseUV), 1.0f)) : fmod(abs(BaseUV), 1.0f));
	if(Rotation != 0) {
		HandleRotation(BaseUV, Rotation);
		BaseUV = fmod(abs(BaseUV), 1.0f);
	}
	return clamp(BaseUV * (TexDim.xy - TexDim.zw) + TexDim.zw, TexDim.zw + 1.0f / 16384.0f, TexDim.xy - 1.0f / 16384.0f);
}


#if defined(UseTextureLOD) && !defined(DX11)
	#define BindlessLOD CurBounce
#else
	#define BindlessLOD 0
#endif
#ifdef PointFiltering
	#ifdef DX11
		#define TTPrimarySampler my_point_clamp_sampler
	#else
		#define TTPrimarySampler my_point_repeat_sampler
	#endif
#else 
	#ifdef DX11
		#define TTPrimarySampler my_linear_clamp_sampler
	#else
		#define TTPrimarySampler my_trilinear_repeat_sampler
	#endif
#endif

float4 SampleTexture(float2 UV, const int TextureType, const IntersectionMat MatTex) {
	float4 FinalCol = 0;
	#if defined(DX11)
		switch(TextureType) {
			case SampleAlbedo: FinalCol = _TextureAtlas.SampleLevel(TTPrimarySampler, AlignUV(UV, MatTex.AlbedoTexScale, MatTex.AlbedoTex, MatTex.Rotation), 0); break;
			case SampleAlpha: FinalCol = _AlphaAtlas.SampleLevel(TTPrimarySampler, AlignUV(UV, MatTex.AlbedoTexScale, MatTex.AlphaTex, MatTex.Rotation), 0); break;
		}
	#else//BINDLESS
		int2 TextureIndexAndChannel = -1;
		[branch] switch(TextureType) {
			case SampleAlbedo: TextureIndexAndChannel = MatTex.AlbedoTex; HandleRotation(UV, MatTex.Rotation); UV = UV * MatTex.AlbedoTexScale.xy + MatTex.AlbedoTexScale.zw; break;
			case SampleAlpha: TextureIndexAndChannel = MatTex.AlphaTex; HandleRotation(UV, MatTex.Rotation); UV = UV * MatTex.AlbedoTexScale.xy + MatTex.AlbedoTexScale.zw; break;
		}
		int TextureIndex = TextureIndexAndChannel.x - 1;
		int TextureReadChannel = TextureIndexAndChannel.y;//0-3 is rgba, 4 is to just read all

		[branch]if(TextureReadChannel != 4) {
			[branch]switch(TextureReadChannel) {
				case 0: FinalCol = _BindlessTextures[NonUniformResourceIndex(TextureIndex)].SampleLevel(TTPrimarySampler, UV, BindlessLOD).x; break;
				case 1: FinalCol = _BindlessTextures[NonUniformResourceIndex(TextureIndex)].SampleLevel(TTPrimarySampler, UV, BindlessLOD).y; break;
				case 2: FinalCol = _BindlessTextures[NonUniformResourceIndex(TextureIndex)].SampleLevel(TTPrimarySampler, UV, BindlessLOD).z; break;
				case 3: FinalCol = _BindlessTextures[NonUniformResourceIndex(TextureIndex)].SampleLevel(TTPrimarySampler, UV, BindlessLOD).w; break;
			} 
		} else FinalCol = _BindlessTextures[NonUniformResourceIndex(TextureIndex)].SampleLevel(TTPrimarySampler, UV, BindlessLOD);
	#endif
	return FinalCol;
}

inline float4 SampleTexture(float2 UV, const int TextureType, const MaterialData MatTex) {
	float4 FinalCol = 0;
	#if defined(DX11)
		switch(TextureType) {
			case SampleAlbedo: FinalCol = _TextureAtlas.SampleLevel(TTPrimarySampler, AlignUV(UV, MatTex.AlbedoTexScale, MatTex.AlbedoTex, MatTex.Rotation), 0); break;
			case SampleMetallic: FinalCol = SingleComponentAtlas.SampleLevel(my_linear_clamp_sampler, AlignUV(UV, MatTex.SecondaryTexScaleOffset, MatTex.MetallicTex, MatTex.RotationSecondary), 0); break;
			case SampleRoughness: FinalCol = SingleComponentAtlas.SampleLevel(my_linear_clamp_sampler, AlignUV(UV, MatTex.SecondaryTexScaleOffset, MatTex.RoughnessTex, MatTex.RotationSecondary), 0); break;
			case SampleEmission: FinalCol = _EmissiveAtlas.SampleLevel(my_linear_clamp_sampler, AlignUV(UV, MatTex.AlbedoTexScale, MatTex.EmissiveTex, MatTex.Rotation), 0); break;
			case SampleNormal: FinalCol = _NormalAtlas.SampleLevel(sampler_NormalAtlas, AlignUV(UV, MatTex.NormalTexScaleOffset, MatTex.NormalTex, MatTex.RotationNormal), 0).xyxy; break;
			case SampleAlpha: FinalCol = _AlphaAtlas.SampleLevel(TTPrimarySampler, AlignUV(UV, MatTex.AlbedoTexScale, MatTex.AlphaTex, MatTex.Rotation), 0); break;
			case SampleMatCap: FinalCol = _TextureAtlas.SampleLevel(my_linear_clamp_sampler, AlignUV(UV, MatTex.AlbedoTexScale, MatTex.MatCapTex, MatTex.Rotation), 0); break;
			case SampleMatCapMask: FinalCol = _TextureAtlas.SampleLevel(my_linear_clamp_sampler, AlignUV(UV, MatTex.AlbedoTexScale, MatTex.MatCapMask, MatTex.Rotation), 0); break;
			case SampleTerrainAlbedo: FinalCol = _TextureAtlas.SampleLevel(my_point_clamp_sampler, AlignUV(UV * MatTex.surfaceColor.xy + MatTex.transmittanceColor.xy, MatTex.AlbedoTexScale, MatTex.AlbedoTex), 0); break;
			case SampleSecondaryAlbedo: FinalCol = _TextureAtlas.SampleLevel(TTPrimarySampler, AlignUV(UV, MatTex.SecondaryAlbedoTexScaleOffset, MatTex.SecondaryAlbedoTex, MatTex.RotationSecondaryDiffuse), 0); break;
			case SampleSecondaryAlbedoMask: FinalCol = SingleComponentAtlas.SampleLevel(my_linear_clamp_sampler, AlignUV(UV, MatTex.AlbedoTexScale, MatTex.SecondaryAlbedoMask, MatTex.Rotation), 0); break;
			case SampleDetailNormal: FinalCol = _NormalAtlas.SampleLevel(sampler_NormalAtlas, AlignUV(UV, MatTex.SecondaryNormalTexScaleOffset, MatTex.SecondaryNormalTex, MatTex.RotationSecondaryNormal), 0).xyxy; break;
			case SampleDiffTrans: FinalCol = SingleComponentAtlas.SampleLevel(my_linear_clamp_sampler, AlignUV(UV, MatTex.AlbedoTexScale, MatTex.DiffTransTex, MatTex.Rotation), 0); break;
		}
	#else//BINDLESS
		//AlbedoTexScale, AlbedoTex, and Rotation dont worry about, thats just for transforming to the atlas 
		int2 TextureIndexAndChannel = -1;// = MatTex.BindlessIndex;
		[branch]switch(TextureType) {
			case SampleAlbedo: TextureIndexAndChannel = MatTex.AlbedoTex; HandleRotation(UV, MatTex.Rotation); UV = UV * MatTex.AlbedoTexScale.xy + MatTex.AlbedoTexScale.zw; break;
			case SampleMetallic: TextureIndexAndChannel = MatTex.MetallicTex; HandleRotation(UV, MatTex.RotationSecondary); UV = UV * MatTex.SecondaryTexScaleOffset.xy + MatTex.SecondaryTexScaleOffset.zw; break;
			case SampleRoughness: TextureIndexAndChannel = MatTex.RoughnessTex; HandleRotation(UV, MatTex.RotationSecondary); UV = UV * MatTex.SecondaryTexScaleOffset.xy + MatTex.SecondaryTexScaleOffset.zw; break;
			case SampleEmission: TextureIndexAndChannel = MatTex.EmissiveTex; HandleRotation(UV, MatTex.Rotation); UV = UV * MatTex.AlbedoTexScale.xy + MatTex.AlbedoTexScale.zw; break;
			case SampleNormal: TextureIndexAndChannel = MatTex.NormalTex; HandleRotation(UV, MatTex.RotationNormal); UV = UV * MatTex.NormalTexScaleOffset.xy + MatTex.NormalTexScaleOffset.zw; break;
			case SampleAlpha: TextureIndexAndChannel = MatTex.AlphaTex; HandleRotation(UV, MatTex.Rotation); UV = UV * MatTex.AlbedoTexScale.xy + MatTex.AlbedoTexScale.zw; break;
			case SampleMatCap: TextureIndexAndChannel = MatTex.MatCapTex; HandleRotation(UV, MatTex.Rotation); UV = UV * MatTex.AlbedoTexScale.xy + MatTex.AlbedoTexScale.zw; break;
			case SampleMatCapMask: TextureIndexAndChannel = MatTex.MatCapMask; HandleRotation(UV, MatTex.Rotation); UV = UV * MatTex.AlbedoTexScale.xy + MatTex.AlbedoTexScale.zw; break;
			case SampleTerrainAlbedo: TextureIndexAndChannel = MatTex.AlbedoTex; UV = (UV * MatTex.surfaceColor.xy + MatTex.transmittanceColor.xy) * MatTex.AlbedoTexScale.xy + MatTex.AlbedoTexScale.zw; break;
			case SampleSecondaryAlbedo: TextureIndexAndChannel = MatTex.SecondaryAlbedoTex; HandleRotation(UV, MatTex.RotationSecondaryDiffuse);  UV = UV * MatTex.SecondaryAlbedoTexScaleOffset.xy + MatTex.SecondaryAlbedoTexScaleOffset.zw; break;
			case SampleSecondaryAlbedoMask: TextureIndexAndChannel = MatTex.SecondaryAlbedoMask; HandleRotation(UV, MatTex.Rotation); UV = UV * MatTex.AlbedoTexScale.xy + MatTex.AlbedoTexScale.zw; break;
			case SampleDetailNormal: TextureIndexAndChannel = MatTex.SecondaryNormalTex; HandleRotation(UV, MatTex.RotationSecondaryNormal); UV = UV * MatTex.SecondaryNormalTexScaleOffset.xy + MatTex.SecondaryNormalTexScaleOffset.zw; break;
			case SampleDiffTrans: TextureIndexAndChannel = MatTex.DiffTransTex; HandleRotation(UV, MatTex.Rotation); UV = UV * MatTex.AlbedoTexScale.xy + MatTex.AlbedoTexScale.zw; break;
		}
		int TextureIndex = TextureIndexAndChannel.x - 1;
		int TextureReadChannel = TextureIndexAndChannel.y;//0-3 is rgba, 4 is to just read all

		[branch]if(TextureReadChannel != 4) {
			[branch]switch(TextureReadChannel) {
				case 0: FinalCol = _BindlessTextures[NonUniformResourceIndex(TextureIndex)].SampleLevel(TTPrimarySampler, UV, BindlessLOD).x; break;
				case 1: FinalCol = _BindlessTextures[NonUniformResourceIndex(TextureIndex)].SampleLevel(TTPrimarySampler, UV, BindlessLOD).y; break;
				case 2: FinalCol = _BindlessTextures[NonUniformResourceIndex(TextureIndex)].SampleLevel(TTPrimarySampler, UV, BindlessLOD).z; break;
				case 3: FinalCol = _BindlessTextures[NonUniformResourceIndex(TextureIndex)].SampleLevel(TTPrimarySampler, UV, BindlessLOD).w; break;
			} 
		} else FinalCol = _BindlessTextures[NonUniformResourceIndex(TextureIndex)].SampleLevel(TTPrimarySampler, UV, BindlessLOD);

		[branch] if(TextureType == SampleNormal || TextureType == SampleDetailNormal) {
			FinalCol.g = 1.0f - FinalCol.g;
			FinalCol = (FinalCol.r >= 0.99f) ? FinalCol.agag : FinalCol.rgrg;

		}



	#endif
	return FinalCol;
}

// ------- Compression/Decompression Functions --------
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
// ------- Companion Functions End --------








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

float3x3 build_rotated_ONB(const float3 N, float basis_rotation)
{
    float3 up = abs(N.z) < 0.9999999f ? float3(0.0f, 0.0f, 1.0f) : float3(1.0f, 0.0f, 0.0f);
    float3 T = normalize(cross(up, N));

    // Rodrigues' rotation
    T = T * cos(basis_rotation) + cross(N, T) * sin(basis_rotation) + N * dot(N, T) * (1.0f - cos(basis_rotation));
    float3 B = cross(N, T);
    return float3x3(T, N, B);
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
bool UseReSTIRGI;

float2 randomNEE(uint samdim, uint pixel_index) {
	uint hash = pcg_hash((pixel_index * (uint)526 + samdim) * (MaxBounce + 1) + CurBounce);

	static const float one_over_max_unsigned = asfloat(0x2f7fffff);


	float x = hash_with(frames_accumulated, hash) * one_over_max_unsigned;
	float y = hash_with(frames_accumulated + 0xdeadbeef, hash) * one_over_max_unsigned;

	return float2(x, y);
}

float2 random(uint samdim, uint pixel_index) {
#ifdef PhotonMappingUsed
			uint2 pixid = uint2(pixel_index % screen_width, pixel_index / screen_width);
			uint hash = pcg_hash((pixel_index * (uint)526 + samdim) * (MaxBounce + 1) + PhotonBounce[pixel_index % 1024]);

			const static float one_over_max_unsigned = asfloat(0x2f7fffff);


			float x = hash_with(frames_accumulated, hash) * one_over_max_unsigned;
			float y = hash_with(frames_accumulated + 0xdeadbeef, hash) * one_over_max_unsigned;

			return float2(x, y);
#else
	[branch] if (UseASVGF) {
		uint2 pixid = uint2(pixel_index % screen_width, pixel_index / screen_width);
		uint hash = pcg_hash(((uint)RandomNums[pixid].y * (uint)526 + samdim) * (MaxBounce + 1) + CurBounce);

		const static float one_over_max_unsigned = asfloat(0x2f7fffff);


		float x = hash_with((uint)RandomNums[pixid].x, hash) * one_over_max_unsigned;
		float y = hash_with((uint)RandomNums[pixid].x + 0xdeadbeef, hash) * one_over_max_unsigned;

		return float2(x, y);
	} else {

		uint hash = pcg_hash((pixel_index * (uint)526 + samdim) * (MaxBounce + 1) + CurBounce);

		const static float one_over_max_unsigned = asfloat(0x2f7fffff);


		float x = hash_with(frames_accumulated, hash) * one_over_max_unsigned;
		float y = hash_with(frames_accumulated + 0xdeadbeef, hash) * one_over_max_unsigned;

		return float2(x, y);
	}
#endif
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

bool IsOrtho;
float OrthoSize;

SmallerRay CreateCameraRay(float2 uv, uint pixel_index) {
	// Transform the camera origin to world space
	float3 origin = mul(CamToWorld, float4(0.0f, 0.0f, 0.0f, 1.0f)).xyz;

	// Invert the perspective projection of the view-space position
	float3 direction = mul(CamInvProj, float4(uv, 0.0f, 1.0f)).xyz;
	// Transform the direction from camera to world space and normalize

	if(!IsOrtho) {
		direction = mul(CamToWorld, float4(direction, 0.0f)).xyz;
		direction = normalize(direction);

	} else {
		float4x4 TruProj = inverse(CamInvProj);
		float orthoWidth = 1.0f / TruProj._m00;
		float orthoHeight = 1.0f / TruProj._m11;
		float Aspect = (float)screen_width / (float)screen_height;
		origin = float3(uv.x * OrthoSize * Aspect, uv.y * OrthoSize, 0);
		origin = mul(CamToWorld, float4(origin, 1)).xyz;

		direction = Forward;//normalize(CamToWorld._m20_m21_m22);
	}
	int2 id = int2(pixel_index % screen_width, pixel_index / screen_width);
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
//triangle_shadow
//putting these^ here cuz I ALWAYS ctrl + f for the WRONG term when trying to find this function...
inline bool triangle_intersect_shadow(int tri_id, const SmallerRay ray, const float max_distance, inout float3 throughput, const int MatOffset) {
    TrianglePos tri = triangle_get_positions(tri_id);

    float3 h = cross(ray.direction, tri.posedge2);
    float  a = dot(tri.posedge1, h);

    float  f = rcp(a);
    float3 s = ray.origin - tri.pos0;
    float  u = f * dot(s, h);

    float3 q = cross(s, tri.posedge1);
    float  v = f * dot(ray.direction, q);

    [branch]if (u >= 0 && v >= 0.0f && u + v <= 1.0f) {
        float t = f * dot(tri.posedge2, q);

        [branch]if (t > 0 && t < max_distance) {
		  	const TriangleUvs TriUVs = triangle_get_UVs(tri_id);
		    const int MaterialIndex = (MatOffset + AggTrisA[tri_id].MatDat);
            #if defined(AdvancedAlphaMapped) || defined(AdvancedBackground) || defined(IgnoreGlassShadow)
				if(GetFlag(_IntersectionMaterials[MaterialIndex].Tag, IsBackground) || GetFlag(_IntersectionMaterials[MaterialIndex].Tag, ShadowCaster)) return false; 
        		[branch] if(_IntersectionMaterials[MaterialIndex].MatType == CutoutIndex || _IntersectionMaterials[MaterialIndex].specTrans == 1 || _IntersectionMaterials[MaterialIndex].MatType == FadeIndex) {
	                float2 BaseUv = TriUVs.UV0 * (1.0f - u - v) + TriUVs.UV1 * u + TriUVs.UV2 * v;
        
                    if(_IntersectionMaterials[MaterialIndex].MatType == CutoutIndex && _IntersectionMaterials[MaterialIndex].AlphaTex.x > 0) {
                    	float Alph = SampleTexture(BaseUv, SampleAlpha, _IntersectionMaterials[MaterialIndex]).x;
                        if((GetFlag(_IntersectionMaterials[MaterialIndex].Tag, InvertAlpha) ? (1.0f - Alph) : Alph) < _IntersectionMaterials[MaterialIndex].AlphaCutoff) return false;
                    }

	                #ifdef FadeMapping
	                    if(_IntersectionMaterials[MaterialIndex].MatType == FadeIndex) {
	                        if(_IntersectionMaterials[MaterialIndex].AlphaTex.x > 0) {
	                        	float Alph = SampleTexture(BaseUv, SampleAlpha, _IntersectionMaterials[MaterialIndex]).x;
	                            if((GetFlag(_IntersectionMaterials[MaterialIndex].Tag, InvertAlpha) ? (1.0f - Alph) : Alph) - _IntersectionMaterials[MaterialIndex].AlphaCutoff <= 0.9f) return false;
	                        }
	                    }
	                #endif


		            #ifdef IgnoreGlassShadow
		                if(_IntersectionMaterials[MaterialIndex].specTrans == 1) {
			            	#ifdef StainedGlassShadows
			            		float3 MatCol = _IntersectionMaterials[MaterialIndex].surfaceColor;
						        if(_IntersectionMaterials[MaterialIndex].AlbedoTex.x > 0) MatCol *= SampleTexture(BaseUv, SampleAlbedo, _IntersectionMaterials[MaterialIndex]);
		    					
#ifdef ShadowGlassAttenuation
								// if(GetFlag(_IntersectionMaterials[MaterialIndex].Tag, Thin))
			    				// 	throughput *= exp(-CalculateExtinction2(1.0f - MatCol, _IntersectionMaterials[MaterialIndex].scatterDistance == 0.0f ? 1.0f : _IntersectionMaterials[MaterialIndex].scatterDistance));
			    				// else {

		    						float Dotter = (dot(normalize(cross(normalize(tri.posedge1), normalize(tri.posedge2))), ray.direction));
		    						// if(Dotter > 0)
				    					throughput *= exp(-t * CalculateExtinction2(1.0f - MatCol, _IntersectionMaterials[MaterialIndex].scatterDistance == 0.0f ? 1.0f : _IntersectionMaterials[MaterialIndex].scatterDistance));

			    				// }
#else
		    					throughput *= exp(-CalculateExtinction2(1.0f - MatCol, _IntersectionMaterials[MaterialIndex].scatterDistance == 0.0f ? 1.0f : _IntersectionMaterials[MaterialIndex].scatterDistance));
#endif
		    				#endif
		                	return false;
		                }
		            #endif
		        }
            #endif
			return true;
        }
    }
    return false;
}
inline void triangle_intersect_dist(int tri_id, const SmallerRay ray, inout float max_distance, int mesh_id, const int MatOffset) {
    TrianglePos tri = triangle_get_positions(tri_id);

    float3 h = cross(ray.direction, tri.posedge2);
    float  a = dot(tri.posedge1, h);

    float  f = rcp(a);
    float3 s = ray.origin - tri.pos0;
    float  u = f * dot(s, h);

    float3 q = cross(s, tri.posedge1);
    float  v = f * dot(ray.direction, q);

    [branch]if (u >= 0 && v >= 0.0f && u + v <= 1.0f) {
        float t = f * dot(tri.posedge2, q);

        [branch]if (t > 0 && t < max_distance) {
		  	const TriangleUvs TriUVs = triangle_get_UVs(tri_id);
		    const int MaterialIndex = (MatOffset + AggTrisA[tri_id].MatDat);
            #if defined(AdvancedAlphaMapped) || defined(AdvancedBackground)
				if(GetFlag(_IntersectionMaterials[MaterialIndex].Tag, IsBackground) || GetFlag(_IntersectionMaterials[MaterialIndex].Tag, ShadowCaster)) return; 
        		[branch] if(_IntersectionMaterials[MaterialIndex].MatType == CutoutIndex) {
	                float2 BaseUv = TriUVs.UV0 * (1.0f - u - v) + TriUVs.UV1 * u + TriUVs.UV2 * v;
                    if(_IntersectionMaterials[MaterialIndex].MatType == CutoutIndex && _IntersectionMaterials[MaterialIndex].AlphaTex.x > 0) {
                    	float Alph = SampleTexture(BaseUv, SampleAlpha, _IntersectionMaterials[MaterialIndex]).x;
                        if((GetFlag(_IntersectionMaterials[MaterialIndex].Tag, InvertAlpha) ? (1.0f - Alph) : Alph) < _IntersectionMaterials[MaterialIndex].AlphaCutoff) return;
                    }

		        }
            #endif
            #ifdef IgnoreGlassShadow
                if(_IntersectionMaterials[MaterialIndex].specTrans == 1) return;
            #endif
        	max_distance = t;
        	return;
        }
    }

    return;
}



inline uint ray_get_octant_inv4(const float3 ray_direction) {
    return
        (ray_direction.x < 0.0f ? 0 : 0x04040404) |
        (ray_direction.y < 0.0f ? 0 : 0x02020202) |
        (ray_direction.z < 0.0f ? 0 : 0x01010101);
}

inline uint cwbvh_node_intersect(const SmallerRay ray, int oct_inv4, float max_distance, const BVHNode8Data TempNode) {
    uint e_x = (TempNode.nodes[0].w) & 0xff;
    uint e_y = (TempNode.nodes[0].w >> 8) & 0xff;
    uint e_z = (TempNode.nodes[0].w >> 16) & 0xff;

    const float3 adjusted_ray_direction_inv = float3(
        asfloat(e_x << 23),
        asfloat(e_y << 23),
        asfloat(e_z << 23)
        ) / ray.direction;
    const float3 adjusted_ray_origin = (asfloat(TempNode.nodes[0].xyz) - ray.origin) / ray.direction;
            
    uint hit_mask = 0;
    const bool3 RayDirBools = ray.direction < 0;
    float3 tmin3;
    float3 tmax3;
    uint child_bits;
    uint bit_index;
    uint x_min = TempNode.nodes[2].x;
    uint x_max = TempNode.nodes[2].y;
    uint y_min = TempNode.nodes[3].x;
    uint y_max = TempNode.nodes[3].y;
    uint z_min = TempNode.nodes[4].x;
    uint z_max = TempNode.nodes[4].y;
    [branch]if(RayDirBools.x) {
    	x_min ^= x_max; x_max ^= x_min; x_min ^= x_max;
    }
    [branch]if(RayDirBools.y) {
    	y_min ^= y_max; y_max ^= y_min; y_min ^= y_max;
    }
    [branch]if(RayDirBools.z) {
    	z_min ^= z_max; z_max ^= z_min; z_min ^= z_max;
    }

    [unroll]
    for(int i = 0; i < 2; i++) {
        uint meta4 = (i == 0 ? TempNode.nodes[1].z : TempNode.nodes[1].w);

        uint is_inner4   = (meta4 & (meta4 << 1)) & 0x10101010;
        uint inner_mask4 = (is_inner4 >> 4) * 0xffu;
        uint bit_index4  = (meta4 ^ (oct_inv4 & inner_mask4)) & 0x1f1f1f1f;
        uint child_bits4 = (meta4 >> 5) & 0x07070707;


        [unroll]
        for(int j = 0; j < 4; j++) {

            tmin3 = float3(((x_min >> (j * 8)) & 0xffu), ((y_min >> (j * 8)) & 0xffu), ((z_min >> (j * 8)) & 0xffu));
            tmax3 = float3(((x_max >> (j * 8)) & 0xffu), ((y_max >> (j * 8)) & 0xffu), ((z_max >> (j * 8)) & 0xffu));

            tmin3 = mad(tmin3, adjusted_ray_direction_inv, adjusted_ray_origin);
            tmax3 = mad(tmax3, adjusted_ray_direction_inv, adjusted_ray_origin);

            float tmin = max(max(tmin3.x, tmin3.y), max(tmin3.z, EPSILON));
            float tmax = min(min(tmax3.x, tmax3.y), min(tmax3.z, max_distance));
            
            bool intersected = tmin < tmax;
            [branch]
            if (intersected) {
                child_bits = (child_bits4 >> (j * 8)) & 0xffu;
                bit_index  = (bit_index4 >> (j * 8)) & 0xffu;

                hit_mask |= child_bits << bit_index;
            }
        }
        if(i == 0) {
	        x_min = TempNode.nodes[2].z;
	        x_max = TempNode.nodes[2].w;
	        y_min = TempNode.nodes[3].z;
	        y_max = TempNode.nodes[3].w;
	        z_min = TempNode.nodes[4].z;
	        z_max = TempNode.nodes[4].w;

	        [branch]if(RayDirBools.x) {
	        	x_min ^= x_max; x_max ^= x_min; x_min ^= x_max;
	        }
	        [branch]if(RayDirBools.y) {
	        	y_min ^= y_max; y_max ^= y_min; y_min ^= y_max;
	        }
	        [branch]if(RayDirBools.z) {
	        	z_min ^= z_max; z_max ^= z_min; z_min ^= z_max;
	        }
    	}

    }
    return hit_mask;
}



inline void Closest_Hit_Compute(SmallerRay ray, inout float MinDist) {
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
        [loop] while (Reps < MaxTraversalSamples) {//Traverse Accelleration Structure(Compressed Wide Bounding Volume Hierarchy)            
        	uint2 triangle_group;
            [branch]if (current_group.y > 0x00FFFFFF) {
                uint child_index_offset = firstbithigh(current_group.y);
                
                uint slot_index = (child_index_offset - 24) ^ (oct_inv4 & 0xff);
                uint relative_index = countbits(current_group.y & ~(0xffffffff << slot_index));
                uint child_node_index = current_group.x + relative_index;
                const BVHNode8Data TempNode = cwbvh_nodes[child_node_index];

                current_group.y &= ~(1 << child_index_offset);

                if (current_group.y & 0xff000000) stack[stack_size++] = current_group;

	            current_group.x = (TempNode.nodes[1].x) + NodeOffset;
                triangle_group.x = (TempNode.nodes[1].y) + TriOffset;

                uint hitmask = cwbvh_node_intersect(ray, oct_inv4, MinDist, TempNode);

 				current_group.y = (hitmask & 0xff000000) | ((TempNode.nodes[0].w >> 24) & 0xff);
                triangle_group.y = (hitmask & 0x00ffffff);

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
	                    triangle_intersect_dist(triangle_group.x + triangle_index, ray, MinDist, mesh_id, MatOffset);
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
        return;
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

	            current_group.x = (TempNode.nodes[1].x) + NodeOffset;
                triangle_group.x = (TempNode.nodes[1].y) + TriOffset;
                
                uint hitmask = cwbvh_node_intersect(ray, oct_inv4, dist, TempNode);

 				current_group.y = (hitmask & 0xff000000) | ((TempNode.nodes[0].w >> 24) & 0xff);
                triangle_group.y = (hitmask & 0x00ffffff);

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
	                    if (triangle_intersect_shadow(triangle_group.x + triangle_index, ray, dist, through, MatOffset)) {
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
    return dot(float3(0.299f, 0.587f, 0.114f), max(a,0));
}

inline float lum2(const float3 a) {
    return dot(float3(0.21f, 0.72f, 0.07f), max(a,0));
}


float3x3 adjoint(float4x4 m)
{
    return float3x3(cross(m[1].xyz, m[2].xyz), 
                cross(m[2].xyz, m[0].xyz), 
                cross(m[0].xyz, m[1].xyz));

}

inline float3 GetTriangleNormal(const uint TriIndex, const float2 TriUV, const float3x3 Inverse) {
    float3 Normal0 = i_octahedral_32(AggTrisB[TriIndex].norms.x);
    float3 Normal1 = i_octahedral_32(AggTrisB[TriIndex].norms.y);
    float3 Normal2 = i_octahedral_32(AggTrisB[TriIndex].norms.z);
    return normalize(mul(Inverse, (Normal0 * (1.0f - TriUV.x - TriUV.y) + TriUV.x * Normal1 + TriUV.y * Normal2)).xyz);
}

inline float3 GetTriangleTangent(const uint TriIndex, const float2 TriUV, const float3x3 Inverse) {
    float3 Normal0 = i_octahedral_32(AggTrisB[TriIndex].tans.x);
    float3 Normal1 = i_octahedral_32(AggTrisB[TriIndex].tans.y);
    float3 Normal2 = i_octahedral_32(AggTrisB[TriIndex].tans.z);
    return normalize(mul(Inverse, (Normal0 * (1.0f - TriUV.x - TriUV.y) + TriUV.x * Normal1 + TriUV.y * Normal2)).xyz);
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



inline float AreaOfTriangle(float3 pt1, float3 pt2, float3 pt3) {
    float a = distance(pt1, pt2);
    float b = distance(pt2, pt3);
    float c = distance(pt3, pt1);
    float s = (a + b + c) / 2.0f;
    return sqrt(s * (s - a) * (s - b) * (s - c));
}

static const float FLT_EPSILON = 1.192092896e-07f;
static const float FLT_MIN = 1.175494351e-38f;


inline float mulsign(const float x, const float y) {
	return asfloat((asuint(y) & 0x80000000) ^ asuint(x));
}


struct SGLobe {
	float3 axis ;
	float sharpness ;
	float logAmplitude ;
};

inline SGLobe sg_product ( float3 axis1 , float sharpness1 , float3 axis2 , float sharpness2 ) {
	float3 axis = axis1 * sharpness1 + axis2 * sharpness2 ;
	float sharpness = length ( axis ) ;

	float3 d = axis1 - axis2;
	float len2 = dot(d, d);
	float logAmplitude = -sharpness1 * sharpness2 * len2 / max(sharpness + sharpness1 + sharpness2, FLT_MIN);
	SGLobe result = { axis / max( sharpness , FLT_MIN ) , sharpness , logAmplitude };
	return result ;
}

#define FLT_MAX 3.402823466e+38f

inline float expm1_over_x(const float x) {
	const float u = exp(x);

	[branch]if (u == 1.0)
		return 1.0;

	const float y = u - 1.0;

	[branch]if (abs(x) < 1.0)
		return y * rcp(log(u));

	return y * rcp(x);
}

inline float expm1(const float x) {
    const float u = exp(x);
    const float y = u - 1.0f;
    return (u == 1.0f) ? x : (abs(x) < 1.0f ? y * x / log(u) : y);
}


// Approximate product integral of an SG and clamped cosine / pi.
// [Tokuyoshi et al. 2024 "Hierarchical Light Sampling with Accurate Spherical Gaussian Lighting (Supplementary Document)" Listing. 7]
inline float SGClampedCosineProductIntegralOverPi2024(const float cosine, const float sharpness) {
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

		float a = 1.0f;
		ERFCTZ = 1.0f - mulsign(1.0f, -tz);
		ERFCT = 1.0f - mulsign(1.0f, t);
		[branch] if ((tz) > 1.0 && tz < 4.0f) {
			a = 1.0 - exp2(-(((((((A7 * tz + A6) * tz + A5) * tz + A4) * tz + A3) * tz + A2) * tz + A1) * tz));
			ERFCTZ = 1.0f - mulsign(a, -tz);
			[branch] if(t < 4.0){
				a = 1.0 - exp2(-(((((((A7 * t + A6) * t + A5) * t + A4) * t + A3) * t + A2) * t + A1) * t));
				ERFCT = 1.0f - mulsign(a, t);
			}
		} else if((tz) <= 1.0) {
			a = tz * tz;
			ERFCTZ = 1.0f - (((((B6 * a + B5) * a + B4) * a + B3) * a + B2) * a + B1) * -tz;
			[branch] if (t > 1.0 && t < 4.0f) {
				a = 1.0 - exp2(-(((((((A7 * t + A6) * t + A5) * t + A4) * t + A3) * t + A2) * t + A1) * t));
				ERFCT = 1.0f - mulsign(a, t);
			} else if(t <= 1.0) {
				a = t * t;
				ERFCT = 1.0f - (((((B6 * a + B5) * a + B4) * a + B3) * a + B2) * a + B1) * t;
			}
		}
	}


	const float lerpFactor = saturate(max(0.5f * (cosine * ERFCTZ + ERFCT) - 0.5f * INV_SQRTPI * exp(-tz * tz) * expm1(t * t * (cosine * cosine - 1.0)) * rcp(t), CLAMPING_THRESHOLD));

	const float negsharp = expm1_over_x(-sharpness);
	const float e = exp(-sharpness);
	const float lowerIntegral = e * (negsharp - e) / sharpness;
	const float upperIntegral = (1.0f - negsharp) / sharpness;

	return 2.0 * lerp(lowerIntegral, upperIntegral, lerpFactor);
}


// Symmetry GGX using a 2x2 roughness matrix (i.e., Non-axis-aligned GGX w/o the Heaviside function).
inline float SGGX(const float3 m, const float2x2 roughnessMat) {
	const float det = max(determinant(roughnessMat), EPSILON); // TODO: Use Kahan's algorithm for precise determinant. [https://pharr.org/matt/blog/2019/11/03/difference-of-floats]
	const float2x2 roughnessMatAdj = { roughnessMat._22, -roughnessMat._12, -roughnessMat._21, roughnessMat._11 };
	const float length2 = dot(m.xz, mul(roughnessMatAdj, m.xz)) / det + m.y * m.y; // TODO: Use Kahan's algorithm for precise mul and dot. [https://pharr.org/matt/blog/2019/11/03/difference-of-floatshttps://pharr.org/matt/blog/2019/11/03/difference-of-floats]

	return 1.0 / (PI * sqrt(det) * (length2 * length2));
}

// Reflection lobe based the symmetric GGX VNDF.
// [Tokuyoshi et al. 2024 "Hierarchical Light Sampling with Accurate Spherical Gaussian Lighting", Section 5.2]
inline float SGGXReflectionPDF(const float3 wi, const float3 m, const float2x2 roughnessMat) {
	return SGGX(m, roughnessMat) * rcp(max(4.0 * sqrt(dot(wi.xz, mul(roughnessMat, wi.xz)) + wi.y * wi.y), EPSILON)); // TODO: Use Kahan's algorithm for precise mul and dot. [https://pharr.org/matt/blog/2019/11/03/difference-of-floats]
}

inline float SGIntegral(const float sharpness) {
	return 4.0 * PI * expm1_over_x(-2.0 * sharpness);
}

bool IsFinite(float x) {
    return (asuint(x) & 0x7F800000) != 0x7F800000;
}

float SGImportanceDiffuse(const GaussianTreeNode TargetNode, const float3 p, const float3 n) {
	float3 to_light = TargetNode.position - p;
	const float squareddist = dot(to_light, to_light);

	to_light *= rsqrt(squareddist);
	const float c = max(dot(n, -(to_light)),0);

	float Variance = max(TargetNode.variance, (0.00001f) * squareddist);// * (1.0f - c) + 0.5f * (TargetNode.radius * TargetNode.radius) * c;
	Variance = Variance * (1.0f - c) + 0.5f * (TargetNode.radius * TargetNode.radius) * c;

	const SGLobe LightLobe = sg_product(i_octahedral_32(TargetNode.axis), TargetNode.sharpness, to_light, squareddist / Variance);

	const float emissive = (TargetNode.intensity) / (Variance * SGIntegral(TargetNode.sharpness));

	const float amplitude = exp(LightLobe.logAmplitude);
	const float cosine = (dot(LightLobe.axis, n));
	const float diffuseIllumination = amplitude * SGClampedCosineProductIntegralOverPi2024(cosine, LightLobe.sharpness);
	return max(emissive * (diffuseIllumination), 0.f);
}

float SGImportance(const GaussianTreeNode TargetNode, const float3 viewDirTS, const float3 p, const float3 n, const float2 projRoughness2, const float3x3 tangentFrame, const float metallic, const float2x2 jjMat, const float detJJ4) {
	float3 to_light = TargetNode.position - p;
	const float squareddist = dot(to_light, to_light);

	to_light *= rsqrt(squareddist);
	const float c = max(dot(n, -(to_light)),0);

	float Variance = max(TargetNode.variance, (0.00001f) * squareddist);// * (1.0f - c) + 0.5f * (TargetNode.radius * TargetNode.radius) * c;
	Variance = Variance * (1.0f - c) + 0.5f * (TargetNode.radius * TargetNode.radius) * c;

	const SGLobe LightLobe = sg_product(i_octahedral_32(TargetNode.axis), TargetNode.sharpness, to_light, squareddist / Variance);

	const float emissive = (TargetNode.intensity) / (Variance * SGIntegral(TargetNode.sharpness));

	const float amplitude = exp(LightLobe.logAmplitude);
	const float cosine = (dot(LightLobe.axis, n));
	const float diffuseIllumination = amplitude * SGClampedCosineProductIntegralOverPi2024(cosine, LightLobe.sharpness);
	[branch]if(metallic > 0.001f) {
		const float LightLobeVariance = rcp(LightLobe.sharpness);
		const float2x2 filteredProjRoughnessMat = float2x2(projRoughness2.x, 0.0, 0.0, projRoughness2.y) + 2.0 * LightLobeVariance * jjMat;
		const float det = projRoughness2.x * projRoughness2.y + 2.0 * LightLobeVariance * (projRoughness2.x * jjMat._11 + projRoughness2.y * jjMat._22) + LightLobeVariance * LightLobeVariance * detJJ4;
		const float tr = filteredProjRoughnessMat._11 + filteredProjRoughnessMat._22;
		const float2x2 filteredRoughnessMat = min(filteredProjRoughnessMat + float2x2(det, 0.0, 0.0, det), FLT_MAX) * rcp(1.0 + tr + det);//IsFinite(1.0 + tr + det) ? min(filteredProjRoughnessMat + float2x2(det, 0.0, 0.0, det), FLT_MAX) / (1.0 + tr + det) : (float2x2(min(filteredProjRoughnessMat._11, FLT_MAX) / min(filteredProjRoughnessMat._11 + 1.0, FLT_MAX), 0.0, 0.0, min(filteredProjRoughnessMat._22, FLT_MAX) / min(filteredProjRoughnessMat._22 + 1.0, FLT_MAX)));
		const float3 halfvecUnormalized = viewDirTS + mul(tangentFrame, LightLobe.axis);
		const float3 halfvec = halfvecUnormalized * rsqrt(max(dot(halfvecUnormalized, halfvecUnormalized), EPSILON));
		float pdf = SGGXReflectionPDF(viewDirTS, halfvec, filteredRoughnessMat);

		return max(emissive * (diffuseIllumination + metallic * pdf * amplitude * SGIntegral(LightLobe.sharpness)), 0.f);
	} else
		return max(emissive * (diffuseIllumination), 0.f);
}



inline float cosSubClamped(float sinTheta_a, float cosTheta_a, float sinTheta_b, float cosTheta_b) {
	if(cosTheta_a > cosTheta_b) return 1;
	return cosTheta_a * cosTheta_b + sinTheta_a * sinTheta_b;
}
inline float sinSubClamped(float sinTheta_a, float cosTheta_a, float sinTheta_b, float cosTheta_b) {
	if(cosTheta_a > cosTheta_b) return 0;
	return sinTheta_a * cosTheta_b - cosTheta_a * sinTheta_b;
}

float Importance(const float3 p, const float3 n, LightBVHData node)
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

#ifdef UseSGTree
inline void CalcLightPDF(inout float lightPDF, float3 p, float3 p2, float3 n, const int pixel_index, const float4x4 W2L, const uint LightPathFlag, const int Offset2, const uint PathFlags, int NodeOffset) {
#else
void CalcLightPDF(inout float lightPDF, float3 p, float3 p2, float3 n, const int pixel_index, const float4x4 W2L, const uint LightPathFlag, const int Offset2, const uint PathFlags, int NodeOffset) {
#endif
	int node_index = 0;
	int Reps = 0;
	bool HasHitTLAS = false;
	float3 stack[12];
	int stacksize = 0;
	float RandNum = random(264, pixel_index).x;
	

#ifdef UseSGTree
	GaussianTreeNode node = SGTree[node_index];
#else
	LightBVHData node = SGTree[node_index];
#endif
	int DepthAfterTLAS = 0;
	while(Reps < 200) {
		[branch]if(node.left >= 0) {
			Reps++;
#ifdef UseSGTree
			const GaussianTreeNode NodeA = SGTree[node.left + NodeOffset];
			const GaussianTreeNode NodeB = SGTree[node.left + 1 + NodeOffset];
			const float2 ci = float2(
				SGImportanceDiffuse(NodeA, p, n),
				SGImportanceDiffuse(NodeB, p, n)
			);
#else
			const LightBVHData NodeA = SGTree[node.left + NodeOffset];
			const LightBVHData NodeB = SGTree[node.left + 1 + NodeOffset];
			const float2 ci = float2(
				Importance(p, n, NodeA),
				Importance(p, n, NodeB)
			);
#endif
			// if(ci.x == 0 && ci.y == 0) {lightPDF = 0; return;}

		    float sumweights = (ci.x + ci.y);
            float up = RandNum * sumweights;
            if (up == sumweights)
                up = asfloat(asuint(up) - 1);
            int offset = 0;
            float sum = 0;
            if(sum + ci[offset] <= up) sum += ci[offset++];
            if(sum + ci[offset] <= up) sum += ci[offset++];


			int Index;
			if(!HasHitTLAS && Reps < 33) {
				Index = (PathFlags >> (Reps - 1)) & 0x1;
			} else {
#ifdef UseSGTree
				[branch]if((Reps < 33)) {
					Index = (LightPathFlag >> (Reps - 1)) & 0x1;
				} else {
#endif
					Index = CalcInside(NodeA, NodeB, p2, offset);

					if(Index == -1) {
						if(stacksize == 0) {return;}
						float3 tempstack = stack[--stacksize];
						node_index = tempstack.x;
						lightPDF = tempstack.y;
						RandNum = tempstack.z;
						node = SGTree[node_index];
						continue;
					}
					if(Index >= 2) {
						Index -= 2;
						if(stacksize < 10)
						stack[stacksize++] = float3(node.left + NodeOffset + !Index, lightPDF * (ci[!Index] / sumweights), min((up - sum) / ci[!Index], 1.0f - (1e-6)));
					}
					// Index = RandNum >= (ci.x / sumweights);
#ifdef UseSGTree
				}
#endif
			}
            RandNum = min((up - sum) / ci[Index], 1.0f - (1e-6));
            node_index = node.left + Index + NodeOffset;
            if(sumweights != 0) lightPDF *= ci[Index] / sumweights;
			if(Index) node = NodeB;
			else node = NodeA;
		} else {
			[branch]if(HasHitTLAS) {
				return;	
			} else {
				p = mul(W2L, float4(p,1));
				p2 = mul(W2L, float4(p2,1));
			    float3x3 Inverse = inverse(W2L);
				n = normalize(mul(Inverse, n).xyz);
				DepthAfterTLAS = Reps;
				NodeOffset = Offset2;
				node_index = NodeOffset;
				HasHitTLAS = true;
				Reps = 0;
				node = SGTree[node_index];

			}
		}
	}

	return;
}





#ifdef UseSGTree
inline int SampleLightBVH(float3 p, float3 n, inout float pmf, const int pixel_index, inout int MeshIndex, const float2 sharpness, float3 viewDir, const float metallic, const bool UsePrev) {
#else
int SampleLightBVH(float3 p, float3 n, inout float pmf, const int pixel_index, inout int MeshIndex, const float2 sharpness, float3 viewDir, const float metallic, const bool UsePrev) {
#endif
	int node_index = 0;
	int Reps = 0;
	bool HasHitTLAS = false;
	int NodeOffset = UsePrev ? LightTreePrimaryTLASOffset : 0;
	int StartIndex = 0;

	float3x3 tangentFrame = GetTangentSpace2(n);
	const float2 roughness2 = sharpness * sharpness;
	const float2 ProjRoughness2 = roughness2 / max(1.0 - roughness2, EPSILON);
	const float reflecSharpness = (1.0 - max(roughness2.x, roughness2.y)) / max(2.0f * max(roughness2.x, roughness2.y), EPSILON);
	float3 viewDirTS = mul(tangentFrame, viewDir);//their origional one constructs the tangent frame from N,T,BT, whereas mine constructs it from T,N,BT; problem? I converted all .y to .z and vice versa, but... 
	float RandNum = random(264, pixel_index).x;
	const float vlen = length(viewDirTS.xz);
	const float2 v = (vlen != 0.0) ? (viewDirTS.xz / vlen) : float2(1.0, 0.0);
	const float2x2 reflecJacobianMat = mul(float2x2(v.x, -v.y, v.y, v.x), float2x2(0.5, 0.0, 0.0, 0.5f / viewDirTS.y));

	// Compute JJ^T matrix.
	float2x2 jjMat = mul(reflecJacobianMat, transpose(reflecJacobianMat));//so this can all be precomputed, but thats actually slower for some reaon??
	float detJJ4 = rcp(4.0 * viewDirTS.y * viewDirTS.y); // = 4 * determiant(JJ^T).
#ifdef UseSGTree
	GaussianTreeNode node = SGTree[0];
#else
	LightBVHData node = SGTree[0];
#endif
	while(Reps < 233) {
		Reps++;
		[branch]if(node.left >= 0) {
#ifdef UseSGTree
			const GaussianTreeNode NodeA = SGTree[node.left + NodeOffset];
			const GaussianTreeNode NodeB = SGTree[node.left + 1 + NodeOffset];
			const float2 ci = float2(
				SGImportance(NodeA, viewDirTS, p, n, ProjRoughness2, tangentFrame, metallic, jjMat, detJJ4),
				SGImportance(NodeB, viewDirTS, p, n, ProjRoughness2, tangentFrame, metallic, jjMat, detJJ4)
			);
#else
			const LightBVHData NodeA = SGTree[node.left + NodeOffset];
			const LightBVHData NodeB = SGTree[node.left + 1 + NodeOffset];
			const float2 ci = float2(
				Importance(p, n, NodeA),
				Importance(p, n, NodeB)
			);
#endif
			if(ci.x == 0 && ci.y == 0) break;


		    float sumweights = (ci.x + ci.y);
			float prob0 = ci.x * rcp(sumweights);  // Use rcp for division optimization
	        int Index = (RandNum < prob0) ? 0 : 1;
	        float prob = (Index == 0) ? prob0 : (1.0f - prob0);
	        pmf /= prob;

	        if (Index == 0) {
	            RandNum /= prob0;
	        } else {
	            RandNum = (RandNum - prob0) / (1.0f - prob0);
	        }
	        RandNum = min(RandNum, 1.0f - 1e-6f);

	        node_index = node.left + Index + NodeOffset;
			if(Index) node = NodeB;
			else node = NodeA;


		} else {
			[branch]if(HasHitTLAS) {
				return -(node.left+1) + StartIndex;	
			} else {
				StartIndex = _LightMeshes[-(node.left+1)].StartIndex; 
				MeshIndex = _LightMeshes[-(node.left+1)].LockedMeshIndex;
			    float3x3 Inverse = adjoint(UsePrev ? _MeshDataPrev[MeshIndex].W2L : _MeshData[MeshIndex].W2L);
				p = mul(UsePrev ? _MeshDataPrev[MeshIndex].W2L : _MeshData[MeshIndex].W2L, float4(p,1));
				n = normalize(mul(Inverse, n).xyz);
				[branch]if(metallic.x > 0.001f) {
					viewDir = normalize(mul(Inverse, viewDir).xyz);
					tangentFrame = GetTangentSpace2(n);
					viewDirTS = mul(tangentFrame, viewDir);
					const float vlen2 = length(viewDirTS.xz);
					const float2 v2 = (vlen2 != 0.0) ? (viewDirTS.xz / vlen2) : float2(1.0, 0.0);
					const float2x2 reflecJacobianMat2 = mul(float2x2(v2.x, -v2.y, v2.y, v2.x), float2x2(0.5, 0.0, 0.0, 0.5f / viewDirTS.y));

					// Compute JJ^T matrix.
					jjMat = mul(reflecJacobianMat2, transpose(reflecJacobianMat2));//so this can all be precomputed, but thats actually slower for some reaon??
					detJJ4 = rcp(4.0 * viewDirTS.y * viewDirTS.y); // = 4 * determiant(JJ^T).
				}
				NodeOffset = (UsePrev && _MeshData[MeshIndex].LightNodeSkinnedOffset != -1) ? (_MeshData[MeshIndex].LightNodeSkinnedOffset) : _MeshData[MeshIndex].LightNodeOffset;
				node_index = NodeOffset;
				HasHitTLAS = true;
				node = SGTree[node_index];
			}
		}
	}
	return -1;
}
float4x4 prevviewprojection;

float3 LoadSurfaceInfoCurrentInPrev2(int2 id) {
    uint4 Target = PrimaryTriData[id.xy];
	if(Target.w == 1) return asfloat(Target.xyz);
    MyMeshDataCompacted Mesh = _MeshDataPrev[Target.x];
    float4x4 Inverse = inverse(Mesh.W2L);
    float2 TriUV;
    TriUV.x = asfloat(Target.z);
    TriUV.y = asfloat(Target.w);
    if(Mesh.SkinnedOffset == -1) {
	    Target.y += Mesh.TriOffset;
	    return mul(Inverse, float4(AggTrisA[Target.y].pos0 + TriUV.x * AggTrisA[Target.y].posedge1 + TriUV.y * AggTrisA[Target.y].posedge2,1)).xyz;
	} else {
	    Target.y += Mesh.SkinnedOffset;
	    return mul(Inverse, float4(SkinnedMeshTriBufferPrev[Target.y].pos0 + TriUV.x * SkinnedMeshTriBufferPrev[Target.y].posedge1 + TriUV.y * SkinnedMeshTriBufferPrev[Target.y].posedge2,1)).xyz;
	}	    
}

inline float3 LoadSurfaceInfoPrev2(int2 id) {
    uint4 Target = PrimaryTriDataPrev[id.xy];
	if(Target.w == 1) return asfloat(Target.xyz);
    MyMeshDataCompacted Mesh = _MeshDataPrev[Target.x];
    float2 TriUV;
    TriUV.x = asfloat(Target.z);
    TriUV.y = asfloat(Target.w);
    float4x4 Inverse = inverse(Mesh.W2L);
    if(Mesh.SkinnedOffset == -1) {
	    Target.y += Mesh.TriOffset;
	    return mul(Inverse, float4(AggTrisA[Target.y].pos0 + TriUV.x * AggTrisA[Target.y].posedge1 + TriUV.y * AggTrisA[Target.y].posedge2,1)).xyz;
	} else {
	    Target.y += Mesh.SkinnedOffset;
	    return mul(Inverse, float4(SkinnedMeshTriBufferPrev[Target.y].pos0 + TriUV.x * SkinnedMeshTriBufferPrev[Target.y].posedge1 + TriUV.y * SkinnedMeshTriBufferPrev[Target.y].posedge2,1)).xyz;
	}	    
}

inline float3 LoadSurfaceInfoPrevInCurrent(int2 id) {
    uint4 Target = PrimaryTriDataPrev[id.xy];
	if(Target.w == 1) return asfloat(Target.xyz);
    MyMeshDataCompacted Mesh = _MeshData[Target.x];
    Target.y += Mesh.TriOffset;
    float2 TriUV;
    TriUV.x = asfloat(Target.z);
    TriUV.y = asfloat(Target.w);
    float4x4 Inverse = inverse(Mesh.W2L);
    return mul(Inverse, float4(AggTrisA[Target.y].pos0 + TriUV.x * AggTrisA[Target.y].posedge1 + TriUV.y * AggTrisA[Target.y].posedge2,1)).xyz;
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
    return mul(Inverse, float4(AggTrisA[Target.y].pos0 + TriUV.x * AggTrisA[Target.y].posedge1 + TriUV.y * AggTrisA[Target.y].posedge2,1)).xyz;
}


void Unity_Hue_Degrees_float(float3 In, float Offset, out float3 Out) {
    // RGB to HSV
    float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
    float4 P = step(In.z, In.y) ? float4(In.yz, K.xy) : float4(In.zy, K.wz);
    float4 Q = step(P.x, In.x) ? float4(In.x, P.yzx) : float4(P.xyw, In.x);
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



float3 toSpherical(float3 p) {
  float r   = length(p);
  float t   = acos(p.z/r);
  float ph  = atan(p.y / p.x);
  return float3(r, t, ph);
}

// License: Unknown, author: Unknown, found: don't remember
float tanh_approx(float x) {
  //  Found this somewhere on the interwebs
  //  return tanh(x);
  float x2 = x*x;
  return clamp(x*(27.0 + x2)/(27.0+9.0*x2), -1.0, 1.0);
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

float3 stars(float3 ro, float3 rd, float2 sp, float hh) {
  float3 col = 0;
  
  hh = tanh_approx(20.0*hh);

  for (float i = 0.0; i < 5; ++i) {
    float2 pp = sp+0.5*i;
    float s = i/(4.0);
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

inline void CalcPosNorm(uint4 TriData, inout float3 Pos, inout float3 Norm) {
	if(TriData.w == 99993) {
		Pos = asfloat(TriData.xyz);
		Norm = -normalize(asfloat(TriData.xyz));
	} else {
	    MyMeshDataCompacted Mesh = _MeshData[TriData.x];
	    float4x4 Inverse = inverse(Mesh.W2L);
	    TriData.y += Mesh.TriOffset;
    	Norm = GetTriangleNormal(TriData.y, asfloat(TriData.zw), Inverse);
		Pos = mul(Inverse, float4(AggTrisA[TriData.y].pos0 + asfloat(TriData.z) * AggTrisA[TriData.y].posedge1 + asfloat(TriData.w) * AggTrisA[TriData.y].posedge2,1)).xyz;		
	}

}

inline void CalcPosOnly(uint4 TriData, inout float3 Pos) {
	if(TriData.w == 99993) {
		Pos = asfloat(TriData.xyz);
	} else {
	    MyMeshDataCompacted Mesh = _MeshData[TriData.x];
	    float4x4 Inverse = inverse(Mesh.W2L);
	    TriData.y += Mesh.TriOffset;
		Pos = mul(Inverse, float4(AggTrisA[TriData.y].pos0 + asfloat(TriData.z) * AggTrisA[TriData.y].posedge1 + asfloat(TriData.w) * AggTrisA[TriData.y].posedge2,1)).xyz;		
	}

}

inline float3 CalcPos(uint4 TriData) {
	if(TriData.w == 99993) return asfloat(TriData.xyz);
    MyMeshDataCompacted Mesh = _MeshData[TriData.x];
    float4x4 Inverse = inverse(Mesh.W2L);
    TriData.y += Mesh.TriOffset;
	return mul(Inverse, float4(AggTrisA[TriData.y].pos0 + asfloat(TriData.z) * AggTrisA[TriData.y].posedge1 + asfloat(TriData.w) * AggTrisA[TriData.y].posedge2,1)).xyz;
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
    const float3 RandVecMain = float3(random(115, pixel_index), random(116, pixel_index).x);
    const float RandVecW = random(116, pixel_index).y;
    for(int i = 0; i < RISCount + 1; i++) {
        float3 RandVec = RandVecMain;
        Rand = random(i + 11, pixel_index);
        Index = clamp((Rand.x * unitylightcount), 0, unitylightcount - 1);
        LightData light = _UnityLights[Index];

        float3 MiscInfo = float3(light.Softness * 120.0f + 1, light.SpotAngle);
        if(light.Type == AREALIGHTQUAD|| light.Type == AREALIGHTDISK) {
            float sinPhi, cosPhi;
            sincos(light.ZAxisRotation, sinPhi, cosPhi);
            switch(light.Type) {
                 case AREALIGHTQUAD:
                    RandVec.xy = RandVec.xy * light.SpotAngle - light.SpotAngle / 2.0f;
                    RandVec.xy = mul(float2x2(cosPhi, -sinPhi, sinPhi, cosPhi), RandVec.xy);
                    light.Position += ToWorld(GetTangentSpace2(light.Direction), normalize(float3(RandVec.x,0,RandVec.y))) * length(RandVec.xy);
                    light.Radiance *= PI * ((light.SpotAngle.x * light.SpotAngle.y));
                break;
                case AREALIGHTDISK:
                    sincos(RandVec.x * 2.0f * PI, RandVec.x, RandVec.y);
                    RandVec.xy = mul(float2x2(cosPhi, -sinPhi, sinPhi, cosPhi), RandVec.xy) * RandVec.z * light.SpotAngle.x;
                    light.Position += ToWorld(GetTangentSpace2(light.Direction), normalize(float3(RandVec.x,0,RandVec.y))) * length(RandVec.xy);
                break;
            }

            to_light = light.Position - Position;
            LengthSquared = dot(to_light, to_light);
            to_light /= sqrt(LengthSquared);
            light.Radiance *= pow(saturate(dot(to_light, -light.Direction)), MiscInfo.x) * (MiscInfo.x);
            float PDF = saturate(dot(to_light, -light.Direction));
            if(PDF > 0) p_hat = max(luminance(light.Radiance) / (LengthSquared * max(PDF, 0.1f)),0);
        	else p_hat = 0;
        } else {
        	if(light.Type != DIRECTIONALLIGHT) {
                light.Position += normalize(RandVec - 0.5f) * RandVecW * light.Softness * 0.1f;//Soft Shadows
        	}
	        to_light = (light.Type == DIRECTIONALLIGHT ? (light.Direction * 120000.0f + Position) : light.Position) - Position;
        	LengthSquared = dot(to_light, to_light);
        	to_light /= sqrt(LengthSquared);
        	if(light.Type == SPOTLIGHT) {
                light.Radiance *= ((1.0f - MiscInfo.z * 0.0174533) + (MiscInfo.y * 0.0174533 - MiscInfo.z * 0.0174533) / 2.0f);
                light.Radiance *= saturate(saturate(dot(to_light, -light.Direction)) * MiscInfo.y + MiscInfo.z);
        	}
            p_hat = max(luminance(light.Radiance) / ((light.Type == DIRECTIONALLIGHT) ? 12.0f : LengthSquared) * (dot(to_light, Norm) > 0),0);
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

            int TriTarget = LightTriangles[Index].TriTarget + MeshTriOffset;
            TriangleUV = sample_triangle(Rand2.x, Rand2.y);
            LightPosition = (AggTrisA[TriTarget].pos0 + AggTrisA[TriTarget].posedge1 * TriangleUV.x + AggTrisA[TriTarget].posedge2 * TriangleUV.y - Position);
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
    	int AggTriIndex = LightTriangles[Index].TriTarget + MeshTriOffset;
        FinalPos += Position;
        lightWeight *= (wsum / max((CounCoun) * MinP_Hat, 0.000001f) * LightCount);
    #else
    	#ifdef DoubleBufferSGTree
			MinIndex = SampleLightBVH(Position, Norm, lightWeight, pixel_index, MeshIndex, sharpness, viewDir, metallic, UseASVGF && RandomNums[uint2(pixel_index % screen_width, pixel_index / screen_width)].w == 1);
		#else
			MinIndex = SampleLightBVH(Position, Norm, lightWeight, pixel_index, MeshIndex, sharpness, viewDir, metallic, false);
        #endif
        if(MinIndex == -1) return -1;
        MeshTriOffset = _MeshData[MeshIndex].TriOffset;
        MatOffset =_MeshData[MeshIndex].MaterialOffset;
    	int AggTriIndex = LightTriangles[MinIndex].TriTarget + MeshTriOffset;

        float2 Rand2 = random(79, pixel_index);
        FinalUV = sample_triangle(Rand2.x, Rand2.y);
        FinalPos = (AggTrisA[AggTriIndex].pos0 + AggTrisA[AggTriIndex].posedge1 * FinalUV.x + AggTrisA[AggTriIndex].posedge2 * FinalUV.y);
    #endif
    int MaterialIndex = AggTrisA[AggTriIndex].MatDat + MatOffset;
    MaterialData TTMat = _Materials[MaterialIndex];
    #ifdef AdvancedBackground
    	if(GetFlag(TTMat.Tag, IsBackground)) return -1;
    #endif
    float2 BaseUv = TOHALF(AggTrisA[AggTriIndex].tex0) * (1.0f - FinalUV.x - FinalUV.y) + TOHALF(AggTrisA[AggTriIndex].texedge1) * FinalUV.x + TOHALF(AggTrisA[AggTriIndex].texedge2) * FinalUV.y;
    if(TTMat.AlbedoTex.x > 0)
    	TTMat.surfaceColor *= SampleTexture(BaseUv, SampleAlbedo, TTMat);

    float3 TempCol = TTMat.surfaceColor;
    Unity_Hue_Degrees_float(TempCol, TTMat.Hue * 500.0f, TTMat.surfaceColor);
    TTMat.surfaceColor *= TTMat.Brightness;
    TempCol = TTMat.surfaceColor;
    Unity_Saturation_float(TempCol, TTMat.Saturation, TTMat.surfaceColor);
    Unity_Contrast_float(TTMat.surfaceColor, TTMat.Contrast);
    TTMat.surfaceColor = saturate(TTMat.surfaceColor);


    {
        [branch]if(TTMat.MatCapTex.x > 0) {
            float3 worldViewUp = normalize(float3(0, 1, 0) - viewDir * dot(viewDir, float3(0, 1, 0)));
            float3 worldViewRight = normalize(cross(viewDir, worldViewUp));
            
            float2 matcapUV = float2(dot(worldViewRight, Norm), dot(worldViewUp, Norm)) * 0.5f + 0.5f;

            float3 matcap = SampleTexture(matcapUV, SampleMatCap, TTMat) * TTMat.MatCapColor;
            Unity_Hue_Degrees_float(matcap, TTMat.Hue * 500.0f, matcap);
            matcap *= TTMat.Brightness;
            // TempCol = TempMat.surfaceColor;
            Unity_Saturation_float(matcap, TTMat.Saturation, matcap);
            Unity_Contrast_float(matcap, TTMat.Contrast);
            if(TTMat.MatCapMask.x > 0) TTMat.surfaceColor = lerp(TTMat.surfaceColor, matcap.xyz, SampleTexture(BaseUv, SampleMatCapMask, TTMat).x);
            else TTMat.surfaceColor = matcap.xyz;
        }
    }


    if(TTMat.emission > 0) {
        if (TTMat.EmissiveTex.x > 0) {
            float3 EmissCol = lerp(TTMat.EmissionColor, TTMat.surfaceColor, GetFlag(TTMat.Tag, BaseIsMap));
            float4 EmissTex = SampleTexture(BaseUv, SampleEmission, TTMat);
            if(!GetFlag(TTMat.Tag, IsEmissionMask)) {//IS a mask
                TTMat.emission *= EmissTex.x * (luminance(EmissTex.xyz) > 0.01f);
            } else EmissCol *= EmissTex.xyz;
            TTMat.surfaceColor = lerp(TTMat.surfaceColor, EmissCol, saturate(TTMat.emission) * GetFlag(TTMat.Tag, ReplaceBase));
        } else {
            TTMat.surfaceColor *= TTMat.EmissionColor;
        }
    }
    Radiance = TTMat.emission * TTMat.surfaceColor;
    return AggTriIndex;
}

float3 pal(float t) {
    float3 a = float3(0.5f, 0.5f, 0.5f);
    float3 b = float3(0.5f, 0.5f, 0.5f);
    float3 c = float3(0.8f, 0.8f, 0.8f);
    float3 d = float3(0.0f, 0.33f, 0.67f) + 0.21f;
    return a + b*cos( 6.28318*(c*t+d) );
}

float4 HandleDebug(int Index) {
    static const float one_over_max_unsigned = asfloat(0x2f7fffff);
    uint hash = pcg_hash(32);
    float LinearIndex = hash_with(Index, hash) * one_over_max_unsigned;
    return float4(pal(LinearIndex), 1);
}



float3 SampleDirectionSphere(float u1, float u2)
{
    float z = u1 * 2.0f - 1.0f;
    float r = sqrt(max(0.0f, 1.0f - z * z));
    float phi = 2 * PI * u2;
    float x = r * cos(phi);
    float y = r * sin(phi);

    return float3(x, y, z);
}

#define BucketCount 4
#define PropDepth 5
#define CacheCapacity (BucketCount * 1024 * 1024)
static const uint HashOffset = (4 * 1024 * 1024 * 16);

RWByteAddressBuffer VoxelDataBufferA;
ByteAddressBuffer VoxelDataBufferB;
struct PropogatedCacheData {
	uint2 samples[PropDepth];
	uint pathLength;//[0,2] = PathLength, [3,5] = Computed Norm//the NORMAL I STORE IT WITH ONLY NEEDS TO BE 3 FUCKKIN BITS, NOT A  WHOLE UINT OR DEDICATED NORMAL!
	uint RunningIlluminance;//this gets cleared every bounce basically, since the last NEE and last bounce share the same normal
};
RWStructuredBuffer<PropogatedCacheData> CacheBuffer;



inline uint Hash32Bit(uint a) {
  	a -= (a<<6);
    a ^= (a>>17);
    a -= (a<<9);
    a ^= (a<<4);
    a -= (a<<3);
    a ^= (a<<10);
    a ^= (a>>15);
    return a;
}


//double hash counting? take advantage of the hash collisions to store multiple values per hash?
inline uint GenHash(float3 Pos, float3 Norm) {
	Pos = abs(Pos) < 0.00001f ? 0.00001f : Pos;
    int Layer = max(floor(log2(length(CamPos - Pos)) + 1), 1);//length() seems to work better, but I reallly wanna find a way to make Dot() work, as thats wayyyy faster

    Pos = floor(Pos * 35.0f / pow(2, Layer));
    uint3 Pos2 = asuint((int3)Pos);
    uint ThisHash = ((Pos2.x & 255) << 0) | ((Pos2.y & 255) << 8) | ((Pos2.z & 255) << 16);
    ThisHash |= (Layer & 31) << 24;

    Norm  = i_octahedral_32(octahedral_32(Norm));
    uint NormHash =
        (Norm.x >= 0 ? 1 : 0) +
        (Norm.y >= 0 ? 2 : 0) +
        (Norm.z >= 0 ? 4 : 0);

    ThisHash |= (NormHash) << 29;
    return ThisHash;
}

inline uint GenHashPrecompedLayer(float3 Pos, const int Layer, const float3 Norm) {
	Pos = abs(Pos) < 0.00001f ? 0.00001f : Pos;

    Pos = floor(Pos * 35.0f / pow(2, Layer));
    uint3 Pos2 = asuint((int3)Pos);
    uint ThisHash = ((Pos2.x & 255) << 0) | ((Pos2.y & 255) << 8) | ((Pos2.z & 255) << 16);
    ThisHash |= (Layer & 31) << 24;

    uint NormHash =
        (Norm.x >= 0 ? 1 : 0) +
        (Norm.y >= 0 ? 2 : 0) +
        (Norm.z >= 0 ? 4 : 0);

    ThisHash |= (NormHash) << 29;
    return ThisHash;
}

inline float GetVoxSize(float3 Pos, inout int Layer) {
	Pos = abs(Pos) < 0.00001f ? 0.00001f : Pos;
    Layer = max(floor(log2(length(CamPos - Pos)) + 1), 1);
    return pow(2, Layer) / 35.0f;
}

inline uint GenHashComputedNorm(float3 Pos, uint NormHash) {
	Pos = abs(Pos) < 0.00001f ? 0.00001f : Pos;
    int Layer = max(floor(log2(length(CamPos - Pos)) + 1), 1);//length() seems to work better, but I reallly wanna find a way to make Dot() work, as thats wayyyy faster

    Pos = floor(Pos * 35.0f / pow(2, Layer));
    uint3 Pos2 = asuint((int3)Pos);
    uint ThisHash = ((Pos2.x & 255) << 0) | ((Pos2.y & 255) << 8) | ((Pos2.z & 255) << 16);
    ThisHash |= (Layer & 31) << 24;

    ThisHash |= (NormHash) << 29;//we need to be fuckin doin the hash32bit IN THE HASH INDEX, OTHERWISE WE ARE SAVING THE SCRAMBLED HASH! AND CANT RECONSTRUCT WORLD POSITION!
    return ThisHash;
}

inline bool FindHashEntry(const uint HashValue, inout uint cacheEntry) {
    uint HashIndex = Hash32Bit(HashValue) % CacheCapacity;

    uint baseSlot = floor(HashIndex / (float)BucketCount) * BucketCount;
    [unroll]for (uint i = 0; i < BucketCount; i++) {
        uint PrevHash = VoxelDataBufferB.Load(HashOffset + (baseSlot + i) * 4);//I am read and writing to the same hash buffer, this could be a problem
        if (PrevHash == HashValue) {
            cacheEntry = baseSlot + i;
            return true;
        } else if (PrevHash == 0) break;
    }
    return false;
}


	struct GridVoxel {
	    float3 radiance;
	    uint SampleNum;
	    uint FrameNum;
	};

	inline GridVoxel RetrieveCacheData(uint CacheEntry2) {
		uint CacheEntry = CacheEntry2;
		[branch]if(!FindHashEntry(CacheEntry2, CacheEntry)) return (GridVoxel)0;
	    else {
	    	uint4 voxelDataPacked = VoxelDataBufferB.Load4(CacheEntry * 16);

		    GridVoxel Voxel;
			Voxel.radiance = (float3)voxelDataPacked.xyz / 1e3f;
		    Voxel.SampleNum = voxelDataPacked.w & 0x00FFFFFF;
		    Voxel.FrameNum = (voxelDataPacked.w >> 24) & 0xFF;

		    return Voxel;
		}
	}

	inline void AddVoxelData(uint CacheEntry, uint4 Values) {
	    CacheEntry *= 16;
	    [unroll]for(int i = 0; i < 4; i++)
	    	if(Values[i] != 0) VoxelDataBufferA.InterlockedAdd(CacheEntry + 4 * i, Values[i]);//move to gaussians later, requires a full compressor every bounce tho since I cant rely on interlockedadds
	}

	inline uint FindOpenEntryInHash(const uint HashValue) {
		uint BucketContents;
	    uint HashIndex = Hash32Bit(HashValue) % CacheCapacity;
	    uint baseSlot = floor(HashIndex / (float)BucketCount) * BucketCount;
		if(baseSlot < CacheCapacity) {
			[unroll]for(int i = 0; i < BucketCount; i++) {	
				VoxelDataBufferA.InterlockedCompareExchange(HashOffset + (baseSlot + i) * 4, 0, HashValue, BucketContents);
				if(BucketContents == 0 || BucketContents == HashValue) {
					return baseSlot + i;
				}
			}
		}
		return 0;
	}


	inline bool AddHitToCacheFull(inout PropogatedCacheData CurrentProp, inout uint PathLength, const float3 Pos, const float3 bsdf) {//Run every frame in shading due to 
		float3 RunningIlluminance = unpackRGBE(CurrentProp.RunningIlluminance);
		uint CurHash = FindOpenEntryInHash(GenHashComputedNorm(Pos, (PathLength >> 3) & 7));
		if(CurHash == 0) return false;
		uint ActualPropDepth = min(PathLength & 7, PropDepth);
		AddVoxelData(CurHash, uint4(RunningIlluminance * 1e3f, 1));
		for(uint i = 0; i < ActualPropDepth; i++) {
			RunningIlluminance *= unpackRGBE(CurrentProp.samples[i].x);
			AddVoxelData(CurrentProp.samples[i].y, uint4(RunningIlluminance * 1e3f, 0));
		}

        if(ActualPropDepth >= 4) CurrentProp.samples[4] = CurrentProp.samples[3];
        if(ActualPropDepth >= 3) CurrentProp.samples[3] = CurrentProp.samples[2];
        if(ActualPropDepth >= 2) CurrentProp.samples[2] = CurrentProp.samples[1];
        if(ActualPropDepth >= 1) CurrentProp.samples[1] = CurrentProp.samples[0];
        CurrentProp.samples[0].y = CurHash;
        CurrentProp.RunningIlluminance = 0;
        CurrentProp.samples[0].x = packRGBE(bsdf);
        return true;
	}


	inline bool AddHitToCachePartial(inout PropogatedCacheData CurrentProp, float3 Pos) {//Run every frame in shading due to 
		float3 RunningIlluminance = unpackRGBE(CurrentProp.RunningIlluminance);
		uint CurHash = FindOpenEntryInHash(GenHashComputedNorm(Pos, (CurrentProp.pathLength >> 3) & 7));
		if(CurHash == 0) return false;
		uint ActualPropDepth = min(CurrentProp.pathLength & 7, PropDepth);
		AddVoxelData(CurHash, uint4(RunningIlluminance * 1e3f, 1));
        for(uint i = 0; i < ActualPropDepth; i++) {
			RunningIlluminance *= unpackRGBE(CurrentProp.samples[i].x);
			AddVoxelData(CurrentProp.samples[i].y, uint4(RunningIlluminance * 1e3f, 0));
		}
        CurrentProp.RunningIlluminance = 0;
        return true;
	}



bool plane_distance_disocclusion_check(float3 current_pos, float3 history_pos, float3 current_normal)
{
    float3  to_current    = current_pos - history_pos;
    float dist_to_plane = abs(dot(to_current, current_normal));

    return dist_to_plane > 0.01f;
}



#define FLT_EPS 5.960464478e-8

// 'height' is the altitude.
// 'cosTheta' is the Z component of the ray direction.
// 'dist' is the distance.
// seaLvlExt = (sigma_t * b) is the sea-level (height = 0) extinction coefficient.
// n = (1 / H) is the falloff exponent, where 'H' is the scale height.
float3 OptDepthRectExpMedium(float height, float cosTheta, float dist,
                               float3 seaLvlExt, float n)
{
    float p = -cosTheta * n;

    // Equation 26.
    float3 optDepth = seaLvlExt * dist;

    if (abs(p) > FLT_EPS) // Uniformity check
    {
        // Equation 34.
        optDepth = seaLvlExt * rcp(p) * exp(height * n) * (exp(p * dist) - 1);
    }

    return optDepth;
}

// 'optDepth' is the value of optical depth.
// 'height' is the altitude.
// 'cosTheta' is the Z component of the ray direction.
// seaLvlExtRcp = (1 / seaLvlExt).
// n = (1 / H) is the falloff exponent, where 'H' is the scale height.
float SampleRectExpMedium(float optDepth, float height, float cosTheta,
                          float seaLvlExtRcp, float n)
{
    float p = -cosTheta * n;

    // Equation 27.
    float dist = optDepth * seaLvlExtRcp;

    if (abs(p) > FLT_EPS) // Uniformity check
    {
        // Equation 35.
        dist = rcp(p) * log(1 + dist * p * exp(height * n));
    }

    return dist;
}

// Max Abs Error: 0.000000969658452.
// Max Rel Error: 0.000001091639525.
float Exp2Erfc(float x)
{
    float t, u, y;

    t = 3.9788608f * rcp(x + 3.9788608f); // Reduce the range
    u = t - 0.5f;                         // Center around 0

    y =           -0.010297533124685f;
    y = mad(y, u, 0.288184314966202f);
    y = mad(y, u, 0.805188119411469f);
    y = mad(y, u, 1.203098773956299f);
    y = mad(y, u, 1.371236562728882f);
    y = mad(y, u, 1.312000870704651f);
    y = mad(y, u, 1.079175233840942f);
    y = mad(y, u, 0.774399876594543f);
    y = mad(y, u, 0.490166693925858f);
    y = mad(y, u, 0.275374621152878f);

    return y * t; // Expand the range
}

float ChapmanUpper(float z, float absCosTheta)
{
    float sinTheta = sqrt(saturate(1 - absCosTheta * absCosTheta));

    float zm12 = rsqrt(z);           // z^(-1/2)
    float zp12 = z * zm12;           // z^(+1/2)

    float tp   = 1 + sinTheta;       // 1 + Sin
    float rstp = rsqrt(tp);          // 1 / Sqrt[1 + Sin]
    float rtp  = rstp * rstp;        // 1 / (1 + Sin)
    float stm  = absCosTheta * rstp; // Sqrt[1 - Sin] = Abs[Cos] / Sqrt[1 + Sin]
    float arg  = zp12 * stm;         // Sqrt[z - z * Sin], argument of Erfc
    float e2ec = Exp2Erfc(arg);      // Exp[x^2] * Erfc[x]

    // Term 1 of Equation 46.
    float mul1 = absCosTheta * rtp;  // Sqrt[(1 - Sin) / (1 + Sin)] = Abs[Cos] / (1 + Sin)
    float trm1 = mul1 * (1 - 0.5 * rtp);

    // Term 2 of Equation 46.
    float mul2 = sqrt(PI) * rstp * e2ec; // Sqrt[Pi / (1 + Sin)] * Exp[x^2] * Erfc[x]
    float trm2 = mul2 * (zp12 * (-1.5 + tp + rtp) +
                         zm12 * 0.25 * (2 * tp - 1) * rtp);
    return trm1 + trm2;
}

float ChapmanHorizontal(float z)
{
    float zm12 = rsqrt(z);           // z^(-1/2)
    float zm32 = zm12 * zm12 * zm12; // z^(-3/2)

    float p = -0.14687275046666018 + z * (0.4699928014933126 + z * 1.2533141373155001);

    // Equation 47.
    return p * zm32;
}

// z = (r / H), Z = (R / H).
float RescaledChapman(float z, float Z, float cosTheta)
{
    float sinTheta = sqrt(saturate(1 - cosTheta * cosTheta));

    // Cos[Pi - theta] = -Cos[theta],
    // Sin[Pi - theta] =  Sin[theta],
    // so we can just use Abs[Cos[theta]].
    float ch = ChapmanUpper(z, abs(cosTheta)) * exp(Z - z); // Rescaling adds 'exp'

    if (cosTheta < 0)
    {
        // Ch[z, theta] = 2 * Exp[z - z_0] * Ch[z_0, Pi/2] - Ch[z, Pi - theta].
        // z_0 = r_0 / H = (r / H) * Sin[theta] = z * Sin[theta].
        float z_0 = z * sinTheta;
        float chP = ChapmanHorizontal(z_0) * exp(Z - z_0); // Rescaling adds 'exp'

        // Equation 48.
        ch = 2 * chP - ch;
    }

    return ch;
}



float RadAtDist(float r, float rRcp, float cosTheta, float s)
{
    float x2 = 1 + (s * rRcp) * ((s * rRcp) + 2 * cosTheta);

    // Equation 38.
    return r * sqrt(x2);
}

float CosAtDist(float r, float rRcp, float cosTheta, float s)
{
    float x2 = 1 + (s * rRcp) * ((s * rRcp) + 2 * cosTheta);

    // Equation 39.
    return ((s * rRcp) + cosTheta) * rsqrt(x2);
}

// This variant of the function evaluates optical depth along an infinite path.
// 'r' is the radial distance from the center of the planet.
// 'cosTheta' is the value of the dot product of the ray direction and the surface normal.
// seaLvlExt = (sigma_t * b) is the sea-level (height = 0) extinction coefficient.
// 'R' is the radius of the planet.
// n = (1 / H) is the falloff exponent, where 'H' is the scale height.
float3 OptDepthSpherExpMedium(float r, float cosTheta, float R,
                                float3 seaLvlExt, float H, float n)
{
    float z = r * n;
    float Z = R * n;

    float ch = RescaledChapman(z, Z, cosTheta);

    return ch * H * seaLvlExt;
}

// This variant of the function evaluates optical depth along a bounded path.
// 'r' is the radial distance from the center of the planet.
// rRcp = (1 / r).
// 'cosTheta' is the value of the dot product of the ray direction and the surface normal.
// 'dist' is the distance.
// seaLvlExt = (sigma_t * b) is the sea-level (height = 0) extinction coefficient.
// 'R' is the radius of the planet.
// n = (1 / H) is the falloff exponent, where 'H' is the scale height.
float3 OptDepthSpherExpMedium(float r, float rRcp, float cosTheta, float dist, float R,
                                float3 seaLvlExt, float H, float n)
{
    float rX        = r;
    float rRcpX     = rRcp;
    float cosThetaX = cosTheta;
    float rY        = RadAtDist(rX, rRcpX, cosThetaX, dist);
    float cosThetaY = CosAtDist(rX, rRcpX, cosThetaX, dist);

    // Potentially swap X and Y.
    // Convention: at point Y, the ray points up.
    cosThetaX = (cosThetaY >= 0) ? cosThetaX : -cosThetaX;

    float zX  = rX * n;
    float zY  = rY * n;
    float Z   = R  * n;

    float chX = RescaledChapman(zX, Z, cosThetaX);
    float chY = ChapmanUpper(zY, abs(cosThetaY)) * exp(Z - zY); // Rescaling adds 'exp'

    // We may have swapped X and Y.
    float ch = abs(chX - chY);

    return ch * H * seaLvlExt;
}

#define EPS_ABS  0.0001
#define EPS_REL  0.0001
#define MAX_ITER 4

// 'optDepth' is the value to solve for.
// 'maxOptDepth' is the maximum value along the ray, s.t. (maxOptDepth >= optDepth).
// 'maxDist' is the maximum distance along the ray.
float SampleSpherExpMedium(float optDepth, float r, float rRcp, float cosTheta, float R,
                           float2 seaLvlExt, float2 H, float2 n, // Air & aerosols
                           float maxOptDepth, float maxDist)
{
    const float  optDepthRcp = rcp(optDepth);
    const float2 Z           = R * n;

    // Make an initial guess (assume the medium is uniform).
    float t = maxDist * (optDepth * rcp(maxOptDepth));

    // Establish the ranges of valid distances ('tRange') and function values ('fRange').
    float tRange[2], fRange[2];
    tRange[0] = 0;        /* -> */  fRange[0] = 0           - optDepth;
    tRange[1] = maxDist;  /* -> */  fRange[1] = maxOptDepth - optDepth;

    uint  iter = 0;
    float absDiff = optDepth, relDiff = 1;

    do // Perform a Newton–Raphson iteration.
    {
        float radAtDist = RadAtDist(r, rRcp, cosTheta, t);
        float cosAtDist = CosAtDist(r, rRcp, cosTheta, t);
        // Evaluate the function and its derivatives:
        // f  [t] = OptDepthAtDist[t] - GivenOptDepth = 0,
        // f' [t] = ExtCoefAtDist[t],
        // f''[t] = ExtCoefAtDist'[t] = -ExtCoefAtDist[t] * CosAtDist[t] / H.
        float optDepthAtDist = 0, extAtDist = 0, extAtDistDeriv = 0;
        optDepthAtDist += OptDepthSpherExpMedium(r, rRcp, cosTheta, t, R,
                                                 seaLvlExt.x, H.x, n.x);
        optDepthAtDist += OptDepthSpherExpMedium(r, rRcp, cosTheta, t, R,
                                                 seaLvlExt.y, H.y, n.y);
        extAtDist      += seaLvlExt.x * exp(Z.x - radAtDist * n.x);
        extAtDist      += seaLvlExt.y * exp(Z.y - radAtDist * n.y);
        extAtDistDeriv -= seaLvlExt.x * exp(Z.x - radAtDist * n.x) * n.x;
        extAtDistDeriv -= seaLvlExt.y * exp(Z.y - radAtDist * n.y) * n.y;
        extAtDistDeriv *= cosAtDist;

        float   f = optDepthAtDist - optDepth;
        float  df = extAtDist;
        float ddf = extAtDistDeriv;
        float  dg = df - 0.5 * f * (ddf * rcp(df));

        // assert(df > 0 && dg > 0);

    #if 0
        // https://en.wikipedia.org/wiki/Newton%27s_method
        float slope = rcp(df);
    #else
        // https://en.wikipedia.org/wiki/Halley%27s_method
        float slope = rcp(dg);
    #endif

        float dt = -f * slope;

        // Find the boundary value we are stepping towards:
        // supremum for (f < 0) and infimum for (f > 0).
        uint  sgn     = asuint(f) >> 31;
        float tBound  = tRange[sgn];
        float fBound  = fRange[sgn];
        float tNewton = t + dt;
        // if(iter == 0) _DebugTex[id] = df;//-tNewton;//tRange[0] < tNewton;

        bool isInRange = tRange[0] < tNewton && tNewton < tRange[1];
        if (!isInRange)
        {
            // Newton's algorithm has effectively run out of digits of precision.
            // While it's possible to continue improving precision (to a certain degree)
            // via bisection, it is costly, and the convergence rate is low.
            // It's better to recall that, for short distances, optical depth is a
            // linear function of distance to an excellent degree of approximation.
            slope = (tBound - t) * rcp(fBound - f);
            dt    = -f * slope;
            iter  = MAX_ITER;
        }

        tRange[1 - sgn] = t; // Adjust the range using the
        fRange[1 - sgn] = f; // previous values of 't' and 'f'

        t = t + dt;

        absDiff = abs(optDepthAtDist - optDepth);
        relDiff = abs(optDepthAtDist * optDepthRcp - 1);

        iter++;

        // Stop when the accuracy goal has been reached.
        // Note that this uses the accuracy corresponding to the old value of 't'.
        // The new value of 't' we just computed should result in higher accuracy.
    } while ((absDiff > EPS_ABS) && (relDiff > EPS_REL) && (iter < MAX_ITER));

    return t;
}


float HenyeyGreenstein(float g, float mu) {
    float gg = g * g;
    return (1.0 / (4.0 * PI)) * ((1.0 - gg) / pow(1.0 + gg - 2.0 * g * mu, 1.5));
}

float DualHenyeyGreenstein(float g, float costh) {
    return lerp(HenyeyGreenstein(-g, costh), HenyeyGreenstein(g, costh), 0.7f);
}

float PhaseFunction(float g, float costh) {
    return DualHenyeyGreenstein(g, costh);
}

float3 MultipleOctaveScattering(float density, float mu) {
    float attenuation = 0.2;
    float contribution = 0.2;
    float phaseAttenuation = 0.5;

    float a = 1.0;
    float b = 1.0;
    float c = 1.0;
    float g = 0.85;
    const float scatteringOctaves = 4.0;

    float3 luminance = 0.0;

    for (float i = 0.0; i < scatteringOctaves; i++) {
        float phaseFunction = PhaseFunction(0.3 * c, mu);
        float3 beers = exp(-density * float3(0.8, 0.8, 1) * a);

        luminance += b * phaseFunction * beers;

        a *= attenuation;
        b *= contribution;
        c *= (1.0 - phaseAttenuation);
    }
    return luminance;
}



float3 waveLengthToRGB(float Wavelength) {
    const float Gamma = 0.80;

    float factor;
    float Red, Green, Blue;

    if((Wavelength >= 380) && (Wavelength < 440)) {
        Red = -(Wavelength - 440) / (440 - 380);
        Green = 0.0;
        Blue = 1.0;
    } else if((Wavelength >= 440) && (Wavelength < 490)) {
        Red = 0.0;
        Green = (Wavelength - 440) / (490 - 440);
        Blue = 1.0;
    } else if((Wavelength >= 490) && (Wavelength < 510)) {
        Red = 0.0;
        Green = 1.0;
        Blue = -(Wavelength - 510) / (510 - 490);
    } else if((Wavelength >= 510) && (Wavelength < 580)) {
        Red = (Wavelength - 510) / (580 - 510);
        Green = 1.0;
        Blue = 0.0;
    } else if((Wavelength >= 580) && (Wavelength < 645)) {
        Red = 1.0;
        Green = -(Wavelength - 645) / (645 - 580);
        Blue = 0.0;
    } else if((Wavelength >= 645) && (Wavelength < 781)) { 
        Red = 1.0;
        Green = 0.0;
        Blue = 0.0;
    } else {
        Red = 0.0;
        Green = 0.0;
        Blue = 0.0;
    }

    // Let the intensity fall off near the vision limits

    if((Wavelength >= 380) && (Wavelength < 420)) {
        factor = 0.3 + 0.7 * (Wavelength - 380) / (420 - 380);
    } else if((Wavelength >= 420) && (Wavelength < 701)) {
        factor = 1.0;
    } else if((Wavelength >= 701) && (Wavelength < 781)) {
        factor = 0.3 + 0.7 * (780 - Wavelength) / (780 - 700);
    } else {
        factor = 0.0;
    }

    return pow(float3(Red, Green, Blue) * factor, Gamma);
}

#define DRAINE_G 0.65
#define DRAINE_A 32.0


float phase_draine_eval(const float u, const float g, const float a)
{
    return ((1 - g*g)*(1 + a*u*u))/(4.*(1 + (a*(1 + 2*g*g))/3.) * PI * pow(1 + g*g - 2*g*u,1.5));
}

// sample: (sample an exact deflection cosine)
//   xi = a uniform random real in [0,1]
float phase_draine_sample(const float xi, const float g, const float a)
{
    const float g2 = g * g;
    const float g3 = g * g2;
    const float g4 = g2 * g2;
    const float g6 = g2 * g4;
    const float pgp1_2 = (1 + g2) * (1 + g2);
    const float T1 = (-1 + g2) * (4 * g2 + a * pgp1_2);
    const float T1a = -a + a * g4;
    const float T1a3 = T1a * T1a * T1a;
    const float T2 = -1296 * (-1 + g2) * (a - a * g2) * (T1a) * (4 * g2 + a * pgp1_2);
    const float T3 = 3 * g2 * (1 + g * (-1 + 2 * xi)) + a * (2 + g2 + g3 * (1 + 2 * g2) * (-1 + 2 * xi));
    const float T4a = 432 * T1a3 + T2 + 432 * (a - a * g2) * T3 * T3;
    const float T4b = -144 * a * g2 + 288 * a * g4 - 144 * a * g6;
    const float T4b3 = T4b * T4b * T4b;
    const float T4 = T4a + sqrt(-4 * T4b3 + T4a * T4a);
    const float T4p3 = pow(T4, 1.0 / 3.0);
    const float T6 = (2 * T1a + (48 * pow(2, 1.0 / 3.0) *
        (-(a * g2) + 2 * a * g4 - a * g6)) / T4p3 + T4p3 / (3. * pow(2, 1.0 / 3.0))) / (a - a * g2);
    const float T5 = 6 * (1 + g2) + T6;
    return (1 + g2 - pow(-0.5 * sqrt(T5) + sqrt(6 * (1 + g2) - (8 * T3) / (a * (-1 + g2) * sqrt(T5)) - T6) / 2., 2)) / (2. * g);
}

float3x3 make_frame(const float3 z) {
    const float sign = (z.z >= 0) ? 1 : -1;
    const float a = -1.0 / (sign + z.z);
    const float b = z.x * z.y * a;
    float3 A = float3(1.0 + sign * z.x * z.x * a, sign * b, -sign * z.x);
    float3 B = float3(b, sign + z.y * z.y * a, -z.y);
    return float3x3(float3(A.x,B.x,z.x), float3(A.y,B.y,z.y), float3(A.z,B.z,z.z));
    // return float3x3(float3(1.0 + sign * z.x * z.x * a, sign * b, -sign * z.x),
                // float3(b, sign + z.y * z.y * a, -z.y),
                // z);
}


float3 phase_draine_sample(const float2 xi, const float3 wi, const float g, const float a) {
    const float deflection_cos = phase_draine_sample(xi.x, g, a);
    const float z2 = sqrt(1.0 - deflection_cos * deflection_cos);
    return mul(make_frame(wi), float3(z2 * cos(2.0f * PI * xi.y), z2 * sin(2.0f * PI * xi.y), deflection_cos));
}
