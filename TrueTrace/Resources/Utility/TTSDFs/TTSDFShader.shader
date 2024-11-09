// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'
// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Unlit/TTSDFShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100



        Pass
        {
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite On
        ZTest LEqual
        Cull Off //  HERE IS WHERE YOU PUT CULL OFF
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag


            #include "UnityCG.cginc"
            #include "../../MainCompute/CommonStructs.cginc"
            sampler2D _CameraDepthTexture;



            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 wPos : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;


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




            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.wPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            int SDFCount;



inline float opSubtraction( float d1, float d2 ){return max(-d1,d2);}


inline float sdSphere( float3 p, float s ) {return length(p)-s;}
inline float opUnion( float d1, float d2 ) {return min(d1,d2);}
inline float opSmoothUnion( float d1, float d2, float k ) {
    float h = clamp( 0.5 + 0.5*(d2-d1)/k, 0.0, 1.0 );
    return lerp( d2, d1, h ) - k*h*(1.0-h);
}

inline float sdBox( float3 p, float3 b ) {
    float3 q = abs(p) - b;
    return length(max(q,0.0)) + min(max(q.x,max(q.y,q.z)),0.0);
}

inline float opXor(float d1, float d2 ){return max(min(d1,d2),-max(d1,d2));}


inline float opIntersection( float d1, float d2 ){return max(d1,d2);}


inline float4 qmul(float4 q1, float4 q2) {
    return float4(
        q2.xyz * q1.w + q1.xyz * q2.w + cross(q1.xyz, q2.xyz),
        q1.w * q2.w - dot(q1.xyz, q2.xyz)
    );
}
    
inline float3 rotate_vector(float3 v, float4 r) {
    float4 r_c = r * float4(-1, -1, -1, 1);
    return qmul(r, qmul(float4(v, 0), r_c)).xyz;
}

            inline float Map(float3 p, inout int Color) {
                float MinDist = 999999.0f;
                for(int i = 0; i < SDFCount; i++) {
                    float MinDist2 = MinDist;
                    const SDFData TempSDF = SDFs[i];
                    [branch]switch(SDFs[i].Type) {
                        case 0: MinDist = sdSphere(rotate_vector(p - TempSDF.A, TempSDF.Transform), SDFs[i].B.x); break;
                        case 1: MinDist = sdBox(rotate_vector(p - TempSDF.A, TempSDF.Transform), TempSDF.B); break;
                        default: break;
                    }
                    [branch]switch(SDFs[i].Operation) {
                        case 0: MinDist = opSmoothUnion(MinDist2, MinDist, SDFs[i].Smoothness); break;
                        case 1: MinDist = opSubtraction(MinDist, MinDist2); break;
                        case 2: MinDist = opXor(MinDist, MinDist2); break;
                        case 3: MinDist = opIntersection(MinDist, MinDist2); break;
                        default: break;
                    }
                    if(abs(MinDist2 - MinDist) > 0.0001f) {
                        Color = i;
                    }
                }
                return MinDist;
            }

            inline bool Traverse(float3 P, const float3 rayDir, float maxT, inout float TravT, inout float3 Color) {

                int CurStep = 0;
                float CurT;
                int Col = 0;
                [loop]while(CurStep++ < 100 && TravT < maxT) {
                    CurT = Map(P, Col);
                    if(CurT < 0.01f) {
                        P += rayDir * CurT * 2.0f;
                        CurT = Map(P, Col);
                        Color = HandleDebug(Col);
                        return true;
                    }
                    P += rayDir * max(CurT, 0.1f);
                    TravT += CurT;
                }
                return false;
            }


            float3 Scale;
            float LinearDepthToRawDepth(float linearDepth)
            {
                return (1. - linearDepth * _ZBufferParams.y) / (linearDepth * _ZBufferParams.x);
            }


            fixed4 frag (v2f i, out float outputDepth : SV_Depth) : SV_Target
            {

                float3 viewDirection = normalize(i.wPos - _WorldSpaceCameraPos);

                float TravT = 0;



                float4 Color = float4(1,1,1,0);
                outputDepth = 0;
                float3 Col = 0;
                if(Traverse(_WorldSpaceCameraPos, viewDirection, 1000.0f, TravT, Col)) {
                    float linearDepth = (TravT - _ProjectionParams.y) / (_ProjectionParams.z - _ProjectionParams.y);
                    outputDepth = LinearDepthToRawDepth(linearDepth);
                    Color = float4(Col, 1);
                } else {
                    // discard;
                }



                return Color;
            }
            ENDCG
        }
    }
}
