Shader "Hidden/FireFlyPass"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
            
            sampler2D _MainTex;
            uniform float4 _MainTex_TexelSize;

            float _Strength;
            float _Offset;

            inline float luminance(const float3 a) {
                return dot(float3(0.299f, 0.587f, 0.114f), a);
            }

            float3 RCRS(float2 UV) {
                float Center = luminance(tex2D(_MainTex, UV).rgb);
                float MaxLum = -9999;
                float MinLum = 9999;
                int2 MaxLumIndex;
                int2 MinLumIndex;
                int Radius = ((floor(_Time.w) % 2) + 1);
                float Sharpness =( Radius == 1 ? 1 : pow(0.9, Radius - 1)) * _Strength;
                for(int i = -1; i <= 1; i++) {
                    for(int j = -1; j <= 1; j++) {
                        if(i == 0 && j == 0) continue;
                        float CurLum = luminance(tex2D(_MainTex, UV + float2(i, j) * Radius * _MainTex_TexelSize.xy).rgb);
                        if(MaxLum < CurLum) {
                            MaxLum = CurLum;
                            MaxLumIndex = int2(i,j);
                        }
                        if(MinLum > CurLum) {
                            MinLum = CurLum;
                            MinLumIndex = int2(i,j);
                        }
                    }
                }
                MaxLum *= rcp(Sharpness);
                MaxLum += _Offset;
                MinLum *= Sharpness;
                if(Center >= MinLum && Center <= MaxLum) return tex2D(_MainTex, UV).rgb;
                else if(Center > MaxLum) return tex2D(_MainTex, UV + MaxLumIndex * _MainTex_TexelSize.xy).rgb;
                else return tex2D(_MainTex, UV + MinLumIndex * _MainTex_TexelSize.xy).rgb;
            }

            float4 frag (v2f i) : SV_Target {   
                return float4(RCRS(i.uv), round(tex2D(_MainTex, i.uv).w));
            }
            ENDCG
        }
    }
}
