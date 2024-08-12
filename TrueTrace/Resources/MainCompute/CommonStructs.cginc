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

struct LightBVHData {
	float3 BBMax;
	float3 BBMin;
	uint w;
	float phi;
	uint cosTheta_oe;
	int left;
};

StructuredBuffer<LightBVHData> LightNodes;


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

RWStructuredBuffer<ColData> GlobalColors;
StructuredBuffer<ColData> PrevGlobalColorsA;
RWStructuredBuffer<ColData> PrevGlobalColorsB;