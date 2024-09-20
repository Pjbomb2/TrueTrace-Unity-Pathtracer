
Shader "ConvolutionBloom/KernelGenerate"
{
    Properties
    {
        _MainTex ("_MainTex", 2D) = "black" {}
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            HLSLPROGRAM
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

            float WrapUV(float u)
            {
                if(u < 0 && u > -0.5)
                {
                    return u + 0.5;
                }
                if(u > 0.5 && u < 1)
                {
                    return u - 0.5;
                }
                return -1;
            }

            SamplerState my_linear_repeat_sampler;
            SamplerState my_linear_clamp_sampler;
            Texture2D _MainTex;

            float4 FFTBloomKernelGenParam;
            float4 FFTBloomKernelGenParam1;

            float Luma(float3 color)
            {
                return dot(color, float3(0.299f, 0.587f, 0.114f));
            }

            float4 frag (v2f i) : SV_Target
            {
                float2 offset = FFTBloomKernelGenParam.xy;
                float2 scale = FFTBloomKernelGenParam.zw;
                float KernelDistanceExp = FFTBloomKernelGenParam1.x;
                float KernelDistanceExpClampMin = FFTBloomKernelGenParam1.y;
                float KernelDistanceExpScale = FFTBloomKernelGenParam1.z;
                bool bUseLuminance = FFTBloomKernelGenParam1.w > 0.0f;

                // 用来缩放那些不是 hdr 格式的滤波盒
                float dis = (1.0 - length(i.uv - float2(0.5, 0.5)));
                float kernelScale = max(pow(dis, KernelDistanceExp) * KernelDistanceExpScale, KernelDistanceExpClampMin); 

                float2 xy = i.uv * 2 - 1;
                xy /= scale;
                xy = xy * 0.5 + 0.5;

                float2 uv = xy - 0.5;
                uv.x = WrapUV(uv.x);
                uv.y = WrapUV(uv.y);

                //return float4(_MainTex.Sample(my_linear_repeat_sampler, i.uv - 0.5).rgb, 0.0);
                if(bUseLuminance)
                {
                    float lum = Luma(_MainTex.SampleLevel(my_linear_clamp_sampler, xy, 0).rgb);
                    return float4(float3(lum, lum, lum) * kernelScale, 0.0);
                }
                return float4(_MainTex.SampleLevel(my_linear_clamp_sampler, xy, 0).rgb * kernelScale, 0.0);
            }
            ENDHLSL
        }
    }
}
