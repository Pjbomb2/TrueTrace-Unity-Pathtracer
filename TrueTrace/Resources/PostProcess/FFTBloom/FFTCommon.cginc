//https://github.com/AKGWSB/FFTConvolutionBloom/blob/main/Assets/Scripts/ConvolutionBloom.cs
#ifndef __FFT_COMMON__
#define __FFT_COMMON__
#if !defined(FINALBLIT)

#define PI 3.14159265
#define Complex float2
#define SourceTextureSize 512

groupshared Complex groupSharedBuffer[SourceTextureSize];

Complex ComplexMultiply(in Complex A, in Complex B)
{
	return Complex(A.x * B.x - A.y * B.y, A.x * B.y + B.x * A.y);
}

Complex ComplexConjugate(in Complex Z)
{
	return Complex(Z.x, -Z.y);
}

Complex W_N_k(uint N, uint k)
{
	float theta = 2 * PI * float(k) / float(N);
	return Complex(cos(theta), -sin(theta));
}

uint ReverseBits32(uint bits)
{
	bits = ( bits << 16) | ( bits >> 16);
	bits = ( (bits & 0x00ff00ff) << 8 ) | ( (bits & 0xff00ff00) >> 8 );
	bits = ( (bits & 0x0f0f0f0f) << 4 ) | ( (bits & 0xf0f0f0f0) >> 4 );
	bits = ( (bits & 0x33333333) << 2 ) | ( (bits & 0xcccccccc) >> 2 );
	bits = ( (bits & 0x55555555) << 1 ) | ( (bits & 0xaaaaaaaa) >> 1 );
	return bits;
}

uint ReversLowerNBits(uint bits, uint N)
{
	return ReverseBits32(bits) >> (32 - N);
}

void SplitTwoForOne(in uint index, in Complex X_k, in Complex Y_k)
{
	uint rollIndex = (SourceTextureSize - index) % SourceTextureSize;

	Complex Z_k = groupSharedBuffer[index] / float(sqrt(SourceTextureSize));
	Complex Z_k_c = ComplexConjugate(groupSharedBuffer[rollIndex] / float(sqrt(SourceTextureSize)));

	X_k = (Z_k + Z_k_c) / 2;
	Y_k = ComplexMultiply(Complex(0, -1), (Z_k - Z_k_c) / 2);
}

Complex DFT(in Complex f_n, in uint index, in bool bIsForward)
{
	groupSharedBuffer[index] = f_n;
	GroupMemoryBarrierWithGroupSync();

	uint n = index;
	uint N = SourceTextureSize;
	f_n = Complex(0, 0);
	for(uint k=0; k<N; k++)
	{
		Complex W = W_N_k(N, k * n);
		if(!bIsForward) W = ComplexConjugate(W);

		Complex F_k = groupSharedBuffer[k];
		f_n += ComplexMultiply(F_k, W);
	}
	GroupMemoryBarrierWithGroupSync();
	return f_n;
}


/*
N = 2, 4, 8, 16, 32, 64, 128, 256, 512


*/

Complex CooleyTukeyFFT(in Complex f_n, in uint index, in bool bIsForward)
{
	uint reverseIndex = ReversLowerNBits(index, log2(SourceTextureSize));
	GroupMemoryBarrierWithGroupSync();
	groupSharedBuffer[reverseIndex] = f_n;

	for(uint N=2; N<=SourceTextureSize; N*=2)
	{
		uint i = index % N;
		uint k = index % (N / 2);
		uint evenStartIndex = index - i;
		uint oddStartIndex  = evenStartIndex + N / 2;
		
		GroupMemoryBarrierWithGroupSync();
		Complex F_even_k = groupSharedBuffer[evenStartIndex + k];
		Complex F_odd_k  = groupSharedBuffer[oddStartIndex  + k];

		Complex W = W_N_k(N, k);
		if(!bIsForward) W = ComplexConjugate(W);
		
		Complex F_k;
		if(i < N/2)
			F_k = F_even_k + ComplexMultiply(W, F_odd_k);
		else
			F_k = F_even_k - ComplexMultiply(W, F_odd_k);
		
		GroupMemoryBarrierWithGroupSync();
		groupSharedBuffer[index] = F_k;
	}

	GroupMemoryBarrierWithGroupSync();
	Complex F_k = groupSharedBuffer[index];
	return F_k / float(sqrt(SourceTextureSize));
}

void TwoForOneFFTForward(in Complex z_n, in uint index, out Complex X_k, out Complex Y_k)
{
	CooleyTukeyFFT(z_n, index, true);
	Complex Z_k = groupSharedBuffer[index] / float(sqrt(SourceTextureSize));

	uint rollIndex = (SourceTextureSize - index) % SourceTextureSize;
	Complex Z_k_c = ComplexConjugate(groupSharedBuffer[rollIndex] / float(sqrt(SourceTextureSize)));

	X_k = (Z_k + Z_k_c) / 2;
	Y_k = ComplexMultiply(Complex(0, -1), (Z_k - Z_k_c) / 2);
}

void TwoForOneFFTInverse(in Complex X_k, in Complex Y_k, in uint index, out float x_n, out float y_n)
{
	Complex Z_k = X_k + ComplexMultiply(Complex(0, 1), Y_k);
	Complex z_n = CooleyTukeyFFT(Z_k, index, false);
	x_n = z_n.x;	// real
	y_n = z_n.y;	// imag
}


#endif
#endif  // __FFT_COMMON__

#if defined(FINALBLIT)
	#include "UnityCG.cginc"
    float FFTBloomIntensity;
    float FFTBloomThreshold;
    float4 FFTBloomKernelGenParam;
    float4 FFTBloomKernelGenParam1;
	Texture2D _MainTex;
    SamplerState my_linear_clamp_sampler;

	float Luma(float3 color) {
        return dot(color, float3(0.299f, 0.587f, 0.114f));
    }

	float WrapUV(float u) {
	    if(u < 0 && u > -0.5)
	        return u + 0.5;
	    if(u > 0.5 && u < 1)
	        return u - 0.5;
	    return -1;
	}

	struct appdata {
	    float4 vertex : POSITION;
	    float2 uv : TEXCOORD0;
	};

	struct v2f {
	    float2 uv : TEXCOORD0;
	    float4 vertex : SV_POSITION;
	};

    v2f CommonVert (appdata v) {
        v2f o;
        o.vertex = UnityObjectToClipPos(v.vertex);
        o.uv = v.uv;
        return o;
    }
	float4 FinalBlitFrag (v2f i) : SV_Target {
	    float aspect = _ScreenParams.x / _ScreenParams.y;
	    float2 uv = i.uv;
	    uv.x *= 0.5;
	    if(_ScreenParams.x > _ScreenParams.y)
	    {
	        uv.y /= aspect;
	    }
	    else
	    {
	        uv.x *= aspect;
	    }
	    // uv.y *= 1 / aspect;
	    return float4(_MainTex.SampleLevel(my_linear_clamp_sampler, uv, 0).rgb * FFTBloomIntensity, 1.0);
	}

    float4 SourceGenFrag (v2f i) : SV_Target
    {
        float2 uv = i.uv;
        // uv.y = 1.0 - uv.y;
        uv -= 2.0f * rcp(_ScreenParams.xy);
        float aspect = _ScreenParams.x / _ScreenParams.y;
        if(_ScreenParams.x > _ScreenParams.y)
        {
            uv.y *= aspect;
        }
        else
        {
            uv.x /= aspect;
        }
        if(any(uv.xy > 1.0)) return float4(0, 0, 0, 0);
    #define USE_FILTER 1
    #if USE_FILTER
        float3 color = float3(0, 0, 0);
        float weight = 0.0;
        const float2 offsets[4] = {float2(-0.5, -0.5), float2(-0.5, +0.5), float2(+0.5, -0.5), float2(+0.5, +0.5)};
        for(int i=0; i<4; i++)
        {
            float2 offset = 2.0 * offsets[i] / float2(1920.0f, 1080.0f);
            float3 c = _MainTex.SampleLevel(my_linear_clamp_sampler, uv + offset, 0).rgb;
            float lu = Luma(c);
            float w = 1.0 / (1.0 + lu);
            color += c * w;
            weight += w;
        }
        color /= weight;
    #else
        float3 color = _MainTex.SampleLevel(my_linear_clamp_sampler, uv, 0).rgb;
    #endif  // USE_FILTER

        float luma = Luma(color);
        float scale = saturate(luma - FFTBloomThreshold);
        float3 finalColor = color * scale;
        return float4(finalColor, 0.0);
    }

    float4 KernGenFrag (v2f i) : SV_Target {
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

#endif