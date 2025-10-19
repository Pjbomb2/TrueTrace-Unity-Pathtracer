Shader "Hidden/WaveV2Voxelize"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
      Cull Off
      ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma target 5.0
            #pragma vertex vert
            #pragma fragment frag
           #pragma geometry geom
            #include "UnityCG.cginc"


      #define AXIS_X 0
      #define AXIS_Y 1
      #define AXIS_Z 2

        struct v2g
      {
        float4 vertex : POSITION;
        float3 worldPos : TEXCOORD4;
        float3 normal : NORMAL;
        float2 uv : TEXCOORD;
      };

      struct g2f
      {
        float4 position : SV_POSITION;
        float3 normal : NORMAL;
        float2 uv : TEXCOORD0;
        float3 worldPos : TEXCOORD4;
      };

            RWTexture2D<float4> WaveTexA;
            Texture2D<float4> TEMPTEXC;
      sampler2D _MainTex;
        float4 _MainTex_ST;

        v2g vert(appdata_base v)
      {
        v2g o;
        o.vertex = UnityObjectToClipPos(v.vertex);
        o.worldPos = mul(unity_ObjectToWorld, v.vertex);
        o.normal = UnityObjectToWorldNormal(v.normal);
        o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);

#ifdef UNITY_REVERSED_Z
        o.vertex.z = mad(o.vertex.z, -2.0, 1.0);
#endif

        return o;
      }

      // Swap coordinate axis for largest projection area
      float3 SwizzleAxis(float3 position, uint axis) {
        // Method 1:
        // switch (axis) {
        // case AXIS_X:
        //  position = position.yzx;
        //  break;
        // case AXIS_Y:
        //  position = position.zxy;
        //  break;
        // }

        // Method 2: Is it faster?
        uint a = axis + 1;
        float3 p = position;
        position.x = p[(0 + a) % 3];
        position.y = p[(1 + a) % 3];
        position.z = p[(2 + a) % 3];

        return position;
      }

     [maxvertexcount(3)]
      void geom(triangle v2g i[3], inout TriangleStream<g2f> triStream)
      {
        float3 normal = normalize(abs(cross(i[1].vertex - i[0].vertex, i[2].vertex - i[0].vertex)));
        uint axis = AXIS_Z;

        // Choose an axis with the largest projection area
        if (normal.x > normal.y && normal.x > normal.z) {
          axis = AXIS_X;
        } else if (normal.y > normal.x && normal.y > normal.z) {
          axis = AXIS_Y;
        }

        [unroll]
        for (int j = 0; j < 3; j++) {
          g2f o;

          o.position = float4(SwizzleAxis(i[j].vertex, axis), 1.0);

#ifdef UNITY_UV_STARTS_AT_TOP
          o.position.y = -o.position.y;
#endif

#ifdef UNITY_REVERSED_Z
          o.position.z = mad(o.position.z, 0.5, 0.5);
#endif

          o.normal = i[j].normal;
          o.uv = i[j].uv;
          o.worldPos = i[j].worldPos;

          triStream.Append(o);
        }
      }

        float2 RelativeScale;
        float3 ThisCamCenter;
        float PlaneDefaultY;
        float2 VoxelizerTexDimensions;


          fixed4 frag(g2f i) : SV_TARGET
          {
            i.worldPos.xz = (i.worldPos.xz + (RelativeScale / 2.0f) - ThisCamCenter.xz)  / (RelativeScale) * VoxelizerTexDimensions;
              float3 p = (i.worldPos.xyz);
            if(abs(PlaneDefaultY - p.y) < 0.25f)  {
                WaveTexA[p.xz] = float4(TEMPTEXC[p.xz].xyz, 1);
            }
            // OM5[p / 1u] = 0;

            return float4(0.0, 0.0, 0.0, 0.0);
          }

            ENDCG
        }
    }
  FallBack Off
}