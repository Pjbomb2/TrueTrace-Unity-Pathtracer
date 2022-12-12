Shader "Hidden/SVGFPack"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            Name "Pack"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "SVGFCommon.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;
            sampler2D _CameraGBufferTexture0;
            sampler2D _CameraGBufferTexture2;
            sampler2D _CameraDepthTexture;
            int width;
            int height;
            float3 Forward;
            float4x4 _CameraInverseProjection;

            float3 CreateCameraRay(float2 uv) {
                // Invert the perspective projection of the view-space position
                float3 direction = mul(_CameraInverseProjection, float4(uv, 0.0f, 1.0f)).xyz;
                // Transform the direction from camera to world space and normalize
                direction = mul(unity_CameraToWorld, float4(direction, 0.0f)).xyz;
                direction = normalize(direction);
                return direction;
            }   

            fixed4 frag (v2f i) : SV_Target
            {
                int2 screenSize = int2(width, height);
               const int2 ipos       = i.uv * screenSize;
               float3 Dir = CreateCameraRay(i.uv * 2.0f - 1.0f);
                const float2 nPacked = ndir_to_oct_snorm(tex2D(_CameraGBufferTexture2, i.uv).xyz * 2.0f - 1.0f);
                float Depth = length(Dir / dot(Dir, Forward) * LinearEyeDepth(tex2D(_CameraDepthTexture, i.uv).x));
                return float4(Depth, max(abs(ddx(Depth)), abs(ddy(Depth))), nPacked.x, nPacked.y);
            }
            ENDCG
        }
    }
}
