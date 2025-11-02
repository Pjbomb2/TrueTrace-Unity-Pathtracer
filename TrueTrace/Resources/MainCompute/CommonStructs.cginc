struct CudaTriangleA {
	float3 pos0;
	float3 posedge1;
	float3 posedge2;

	uint tex0;
	uint texedge1;
	uint texedge2;

	uint MatDat;
};

struct CudaTriangleB {
	uint3 norms;

	uint3 tans;

	uint VertColA;
	uint VertColB;
	uint VertColC;

	uint IsEmissive;
};

struct CudaTriangleC {
	float3 pos0;
	float3 posedge1;
	float3 posedge2;
};

StructuredBuffer<CudaTriangleA> AggTrisA;
StructuredBuffer<CudaTriangleC> SkinnedMeshTriBufferPrev;
StructuredBuffer<CudaTriangleB> AggTrisB;

struct AABB {
	float3 BBMax;
	float3 BBMin;
};

struct GaussianTreeNode {
	float3 position;
	float radius;
	uint axis;
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
	int LightNodeSkinnedOffset;
	uint PathFlags;
	int SkinnedOffset;
};

StructuredBuffer<MyMeshDataCompacted> _MeshData;
StructuredBuffer<MyMeshDataCompacted> _MeshDataB;
StructuredBuffer<MyMeshDataCompacted> _MeshDataPrev;

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
	uint TriTarget;
	float SourceEnergy;
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
	float4 IESTexScaleOffset;

};
StructuredBuffer<LightData> _UnityLights;



struct BVHNode8Data {
	uint4 nodes[5];
};

StructuredBuffer<BVHNode8Data> cwbvh_nodes;


/*
	MatType
	AlphaTex
	AlphaCutoff
	SpecTrans
	Tag
	AlbedoTexScale
	Rotation

*/

struct IntersectionMat {//56
    int2 AlphaTex;//80
    int2 AlbedoTex;//80
    int Tag;
    int MatType;//Can pack into tag
    float specTrans;
    float AlphaCutoff;
    float4 AlbedoTexScale;
    float3 surfaceColor;
    float Rotation;
    float scatterDistance;
};

StructuredBuffer<IntersectionMat> _IntersectionMaterials;


struct MaterialData {//56
    int2 AlbedoTex;
    int2 NormalTex;
    int2 EmissiveTex;
    int2 MetallicTex;
    int2 RoughnessTex;
    int2 AlphaTex;
    int2 MatCapMask;
    int2 MatCapTex;
    int2 SecondaryAlbedoTex;
    int2 SecondaryAlbedoMask;
    int2 SecondaryNormalTex;
    int2 DiffTransTex;
    float4 AlbedoTexScale;
    float4 SecondaryTexScaleOffset;
    float4 NormalTexScaleOffset;
    float4 SecondaryAlbedoTexScaleOffset;
    float4 SecondaryNormalTexScaleOffset;
    float Rotation;
    float RotationNormal;
    float RotationSecondary;
    float RotationSecondaryDiffuse;
    float RotationSecondaryNormal;
    float3 surfaceColor;
    float emission;
    float3 EmissionColor;
    int Tag;
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
    float anisotropicRotation;
    float flatness;
    float diffTrans;
    float specTrans;
    float Specular;
    float scatterDistance;
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
    float ColorBleed;
    float AlbedoBlendFactor;
    float SecondaryNormalTexBlend;
    float DetailNormalStrength;
    float2 DiffTransRemap;
    float3 MatCapColor;
    float CausticStrength;
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
	float3 EndPoint;
	int FIELD;
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


struct SDFData {
    float3 A;
    float3 B;
    int Type;
    int Operation;
    float Smoothness;
    float4 Transform;
};

StructuredBuffer<SDFData> SDFs;

struct Photon {
	float4 Pos;
	float3 Flux;
	uint InitialDirection;
	float3 Dir;
	float faceNPhi;
};


struct PhotonRayData {
	float3 throughput;
	uint Norm;
	float3 origin;
	bool terminated;
	float3 direction;
	bool diffuseHit;
};

RWStructuredBuffer<PhotonRayData> PhotonRays;