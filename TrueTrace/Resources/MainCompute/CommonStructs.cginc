struct CudaTriangle {
	float3 pos0;
	float3 posedge1;
	float3 posedge2;

	uint3 norms;

	uint3 tans;

	uint tex0;
	uint texedge1;
	uint texedge2;

	uint VertColA;
	uint VertColB;
	uint VertColC;

	uint MatDat;
};

StructuredBuffer<CudaTriangle> AggTris;

struct GaussianTreeNode {
	float3 position;
	float radius;
	float3 axis;
	float variance;
	float sharpness;
	float intensity;
	int left;
};

struct LightBVHData {
	float3 BBMax;
	float3 BBMin;
	uint w;
	float phi;
	uint cosTheta_oe;
	int left;
};

#ifdef UseSGTree
	StructuredBuffer<GaussianTreeNode> SGTree;
#else 
	StructuredBuffer<LightBVHData> SGTree;
#endif

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

struct TerrainData {
    float3 PositionOffset;
    float HeightScale;
    float2 TerrainDim;
    float4 AlphaMap;
    float4 HeightMap;
    int MatOffset;
};

StructuredBuffer<TerrainData> Terrains;

struct LightTriData {
	float3 pos0;
	float3 posedge1;
	float3 posedge2;
	uint TriTarget;
	float SourceEnergy;
	// uint NormalizedColor;
};

StructuredBuffer<LightTriData> LightTriangles;

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
	int2 IESTex;//16

};
StructuredBuffer<LightData> _UnityLights;



struct BVHNode8Data {
	uint4 nodes[5];
};

StructuredBuffer<BVHNode8Data> cwbvh_nodes;


struct MaterialData {//56
	int2 AlbedoTex;//16
	int2 NormalTex;//32
	int2 EmissiveTex;//48
	int2 MetallicTex;//64
	int2 RoughnessTex;//80
	int2 AlphaTex;//80
	int2 MatCapMask;
	int2 MatCapTex;
	int2 SecondaryAlbedoTex;
	int2 SecondaryAlbedoMask;
    int2 SecondaryNormalTex;
	float3 surfaceColor;
	float emission;
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
    float4 SecondaryAlbedoTexScaleOffset;
	float Rotation;
	float ColorBleed;
	float AlbedoBlendFactor;
	float4 SecondaryNormalTexScaleOffset;
    float SecondaryNormalTexBlend;
    float DetailNormalStrength;
};

StructuredBuffer<MaterialData> _Materials;



struct RayData {//128 bit aligned
	float3 origin;
	uint PixelIndex;//need to bump this back down to uint1
	float3 direction;
	float last_pdf;
	uint4 hits;
};
RWStructuredBuffer<RayData> GlobalRays;



struct ShadowRayData {
	float3 origin;
	uint DiffuseIlluminance;
	float3 direction;
	float t;
	float3 illumination;
	uint PixelIndex;
};
RWStructuredBuffer<ShadowRayData> ShadowRaysBuffer;


struct ColData {
	float3 throughput;
	float3 Direct;
	float3 Indirect;
	uint PrimaryNEERay;
	uint Flags;
	uint MetRoughIsSpec;
	float3 Data;//could compress down to one uint for the color, and store the bounce flag in the existing metroughisspec flag, its already 14 bits for metallic and roughness, which is very unneeded
	float InWaterDistance;
};

/*
	MetRoughIsSpec Bit Purposes:
	0-9: Metallic
	10-19: Roughness
	20-21: MatLobe
	22-22: Refracted
	23-25: Water Stage Flag
	26-30: BounceCount
	31-31: BackupRefractionFlag


*/

RWStructuredBuffer<ColData> GlobalColors;
StructuredBuffer<ColData> PrevGlobalColorsA;
RWStructuredBuffer<ColData> PrevGlobalColorsB;