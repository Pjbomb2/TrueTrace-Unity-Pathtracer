
Shader "Hidden/CustomMotionVectors"
{

     Properties
    {
        _MainTex("", 2D) = "white" {}
    }
    SubShader
    {
         Tags { "RenderType" = "Opaque" }
    // CGINCLUDE


        Pass
        {
            Tags { "LightMode" = "ForwardBase" }
            CGPROGRAM
    struct InstDat {
        float4x4 A;
        float4x4 B;
    };
            StructuredBuffer<InstDat> InstanceTransfs;
            #pragma vertex vert
            #pragma fragment frag


            #include "UnityCG.cginc"
            #define UNITY_INDIRECT_DRAW_ARGS IndirectDrawIndexedArgs
            #include "UnityIndirect.cginc"

            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 color : COLOR0;
            };

            // uniform float4x4 _ObjectToWorld;
            v2f vert(appdata_base v, uint svInstanceID : SV_InstanceID)
            {
                InitIndirectDrawArgs(0);
                v2f o;
                uint cmdID = GetCommandID(0);
                uint instanceID = GetIndirectInstanceID(svInstanceID);
                float4 wpos = mul(InstanceTransfs[instanceID].A, v.vertex);
                o.pos = mul(UNITY_MATRIX_VP, wpos);
                o.color = 1;//float4(cmdID & 1 ? 0.0f : 1.0f, cmdID & 1 ? 1.0f : 0.0f, instanceID / float(GetIndirectInstanceCount()), 0.0f);
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                return i.color;
            }
            ENDCG
        }



        Pass
        {
            Tags { "LightMode" = "MotionVectors" }
            ZWrite Off

            CGPROGRAM
            #pragma vertex VertMotionVectors
            #pragma fragment FragMotionVectors
            // #pragma target 3.5

            #include "UnityCG.cginc"
            #define UNITY_INDIRECT_DRAW_ARGS IndirectDrawIndexedArgs
            #include "UnityIndirect.cginc"
            float4x4 _PreviousVP;
            float4x4 CurVP;
            struct MotionVectorData
            {
                float4 transferPos : TEXCOORD0;
                float4 transferPosOld : TEXCOORD1;
                float4 pos : SV_POSITION;
            };

            struct MotionVertexInput
            {
                float4 vertex : POSITION;
                float3 oldPos : NORMAL;
            };

        struct InstDat {
            float4x4 A;
            float4x4 B;
        };
        StructuredBuffer<InstDat> InstanceTransfs;
            MotionVectorData VertMotionVectors(MotionVertexInput v,uint svInstanceID : SV_InstanceID)
            {
                InitIndirectDrawArgs(0);
                uint cmdID = GetCommandID(0);
                uint instanceID = GetIndirectInstanceID(svInstanceID);
                MotionVectorData o;
                float4 vp0 = mul(InstanceTransfs[instanceID].A, v.vertex);
                float4 vp1 = mul(InstanceTransfs[instanceID].B, v.vertex);

                o.pos = UnityObjectToClipPos(vp1);
                o.transferPos = mul(CurVP, mul(InstanceTransfs[instanceID].A, v.vertex));
                o.transferPosOld = mul(_PreviousVP, mul(InstanceTransfs[instanceID].B, v.vertex));
                return o;
            }

            half4 FragMotionVectors(MotionVectorData i) : SV_Target
            {
                float3 hPos = (i.transferPos.xyz / i.transferPos.w);
                float3 hPosOld = (i.transferPosOld.xyz / i.transferPosOld.w);

                // V is the viewport position at this pixel in the range 0 to 1.
                float2 vPos = (hPos.xy + 1.0f) / 2.0f;
                float2 vPosOld = (hPosOld.xy + 1.0f) / 2.0f;

        #if UNITY_UV_STARTS_AT_TOP
                vPos.y = 1.0 - vPos.y;
                vPosOld.y = 1.0 - vPosOld.y;
        #endif
                return half4(vPos - vPosOld, 0, 1);
            }   

            ENDCG
        }



        // CGPROGRAM

        // #pragma surface Surf Standard fullforwardshadows addshadow
        // #pragma instancing_options procedural:Setup
        // // #pragma target 3.5

        // sampler2D _MainTex;

        // struct Input
        // {
        //     float2 uv_MainTex;
        // };
        // struct InstDat {
        //     float4x4 A;
        //     float4x4 B;
        // };
        // StructuredBuffer<InstDat> InstanceTransfs;
        // float4x4 _LocalToWorld;
        // float4x4 _WorldToLocal;


        // void Setup()
        // {
        // #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
        //     unity_ObjectToWorld = InstanceTransfs[unity_InstanceID].A;// mul(_LocalToWorld, anim.instanceToObject);
        //     unity_WorldToObject = InstanceTransfs[unity_InstanceID].B;//mul(anim.objectToInstance, _WorldToLocal);
        // #endif
        // }

        // void Surf(Input IN, inout SurfaceOutputStandard o)
        // {
        //     o.Albedo = float4(1,1,1,1);//tex2D(_MainTex, IN.uv_MainTex).rgb;
        // }

        // ENDCG
    }
}


// Shader "Hidden/CustomMotionVectors"
// {
//     SubShader
//     {
//         Pass
//         {
//             CGPROGRAM
//             #pragma vertex vert
//             #pragma fragment frag

//     struct InstDat {
//         float4x4 A;
//         float4x4 B;
//     };

//             #include "UnityCG.cginc"
//             #define UNITY_INDIRECT_DRAW_ARGS IndirectDrawIndexedArgs
//             #include "UnityIndirect.cginc"

//             struct v2f
//             {
//                 float4 pos : SV_POSITION;
//                 float4 color : COLOR0;
//             };

//             // uniform float4x4 _ObjectToWorld;
//             StructuredBuffer<InstDat> InstanceTransfs;
//             v2f vert(appdata_base v, uint svInstanceID : SV_InstanceID)
//             {
//                 InitIndirectDrawArgs(0);
//                 v2f o;
//                 uint cmdID = GetCommandID(0);
//                 uint instanceID = GetIndirectInstanceID(svInstanceID);
//                 float4 wpos = mul(InstanceTransfs[instanceID].A, v.vertex);
//                 o.pos = mul(UNITY_MATRIX_VP, wpos);
//                 o.color = float4(cmdID & 1 ? 0.0f : 1.0f, cmdID & 1 ? 1.0f : 0.0f, instanceID / float(GetIndirectInstanceCount()), 0.0f);
//                 return o;
//             }

//             float4 frag(v2f i) : SV_Target
//             {
//                 return i.color;
//             }
//             ENDCG
//         }
//     }
// }