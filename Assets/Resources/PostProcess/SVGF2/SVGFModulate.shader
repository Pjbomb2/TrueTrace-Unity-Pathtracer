Shader "Hidden/SVGFModulate"
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
            Name "Modulate"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

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
    uniform Texture2D   gAlbedo;
    uniform Texture2D   gIllumination;
inline float luminance(const float3 a) {
    return dot(float3(0.299f, 0.587f, 0.114f), a);
}
            fixed4 frag (v2f i) : SV_Target
            {
                 int2 screenSize = 0;
                int thr = 0;
                gAlbedo.GetDimensions(0, screenSize.x, screenSize.y, thr);
               const int2 ipos       = i.uv * screenSize;
               if(!(luminance(gAlbedo[ipos].xyz) < 100000.0f)) return 0;
                 return gAlbedo[ipos] * gIllumination[ipos];
            }
            ENDCG
        }
    }
}
