#ifndef __FFT_COMMON__
#define __FFT_COMMON__

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

// 3bit 一组反转
uint GetInputIndexRadix8(uint index)
{
	uint low3bits  = index & 0x7;
	uint mid3bits  = (index & (0x7 << 3)) >> 3;
	uint high3bits = (index & (0x7 << 6)) >> 6;
	return (low3bits << 6) + (mid3bits << 3) + high3bits;
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

void Radix8FFT_1_thread_8_signal(in Complex f_n[8], in uint threadIndex, in bool bIsForward)
{
	GroupMemoryBarrierWithGroupSync();
	for(uint i=0; i<8; i++)
	{
		uint index = threadIndex * 8 + i;
		uint inputIndex = GetInputIndexRadix8(index);
		groupSharedBuffer[inputIndex] = f_n[i];
	}

	// 将 8 个长度为 N/8 的序列合成为 1 个长度为 N 的序列
	for(uint N=8; N<=SourceTextureSize; N*=8)
	{
		// 一个线程处理 8 个输入信号, 但仅 sync 一次
		GroupMemoryBarrierWithGroupSync();
		Complex F_ks[8];
		for(uint s=0; s<8; s++)
		{
			uint index = threadIndex * 8 + s;	// 当前输入信号的下标
			uint i = index % N;					
			uint k = index % (N / 8);
			uint w = i / (N / 8);				
			uint startIndex = index - i;		// 子序列起始下标

			Complex F_0_k = groupSharedBuffer[startIndex + 0 * (N/8) + k];
			Complex F_1_k = groupSharedBuffer[startIndex + 1 * (N/8) + k];
			Complex F_2_k = groupSharedBuffer[startIndex + 2 * (N/8) + k];
			Complex F_3_k = groupSharedBuffer[startIndex + 3 * (N/8) + k];
			Complex F_4_k = groupSharedBuffer[startIndex + 4 * (N/8) + k];
			Complex F_5_k = groupSharedBuffer[startIndex + 5 * (N/8) + k];
			Complex F_6_k = groupSharedBuffer[startIndex + 6 * (N/8) + k];
			Complex F_7_k = groupSharedBuffer[startIndex + 7 * (N/8) + k];

			Complex F_k = Complex(0, 0);
			if(bIsForward)
			{
				F_k += ComplexMultiply(ComplexMultiply(W_N_k(8, 0 * w), W_N_k(N, 0 * k)), F_0_k);
				F_k += ComplexMultiply(ComplexMultiply(W_N_k(8, 1 * w), W_N_k(N, 1 * k)), F_1_k);
				F_k += ComplexMultiply(ComplexMultiply(W_N_k(8, 2 * w), W_N_k(N, 2 * k)), F_2_k);
				F_k += ComplexMultiply(ComplexMultiply(W_N_k(8, 3 * w), W_N_k(N, 3 * k)), F_3_k);
				F_k += ComplexMultiply(ComplexMultiply(W_N_k(8, 4 * w), W_N_k(N, 4 * k)), F_4_k);
				F_k += ComplexMultiply(ComplexMultiply(W_N_k(8, 5 * w), W_N_k(N, 5 * k)), F_5_k);
				F_k += ComplexMultiply(ComplexMultiply(W_N_k(8, 6 * w), W_N_k(N, 6 * k)), F_6_k);
				F_k += ComplexMultiply(ComplexMultiply(W_N_k(8, 7 * w), W_N_k(N, 7 * k)), F_7_k);
			}
			else
			{
				F_k += ComplexMultiply(ComplexConjugate(ComplexMultiply(W_N_k(8, 0 * w), W_N_k(N, 0 * k))), F_0_k);
				F_k += ComplexMultiply(ComplexConjugate(ComplexMultiply(W_N_k(8, 1 * w), W_N_k(N, 1 * k))), F_1_k);
				F_k += ComplexMultiply(ComplexConjugate(ComplexMultiply(W_N_k(8, 2 * w), W_N_k(N, 2 * k))), F_2_k);
				F_k += ComplexMultiply(ComplexConjugate(ComplexMultiply(W_N_k(8, 3 * w), W_N_k(N, 3 * k))), F_3_k);
				F_k += ComplexMultiply(ComplexConjugate(ComplexMultiply(W_N_k(8, 4 * w), W_N_k(N, 4 * k))), F_4_k);
				F_k += ComplexMultiply(ComplexConjugate(ComplexMultiply(W_N_k(8, 5 * w), W_N_k(N, 5 * k))), F_5_k);
				F_k += ComplexMultiply(ComplexConjugate(ComplexMultiply(W_N_k(8, 6 * w), W_N_k(N, 6 * k))), F_6_k);
				F_k += ComplexMultiply(ComplexConjugate(ComplexMultiply(W_N_k(8, 7 * w), W_N_k(N, 7 * k))), F_7_k);
			}

			F_ks[s] = F_k;
		}

		GroupMemoryBarrierWithGroupSync();
		for(uint s=0; s<8; s++)
		{
			uint index = threadIndex * 8 + s;
			groupSharedBuffer[index] = F_ks[s];
		}
	}
}

void Radix8FFT(in Complex f_n, in uint index, in bool bIsForward)
{
	GroupMemoryBarrierWithGroupSync();
	uint inputIndex = GetInputIndexRadix8(index);
	groupSharedBuffer[inputIndex] = f_n;

	// 将 8 个长度为 N/8 的序列合成为 1 个长度为 N 的序列
	for(uint N=8; N<=SourceTextureSize; N*=8)
	{
		uint i = index % N;			
		uint startIndex = index - i;	// 子序列起始下标		
		uint w = i / (N / 8);			// 子序列序号
		uint k = index % (N / 8);		// 取子序列的第几个元素
		
		GroupMemoryBarrierWithGroupSync();
		Complex F_0_k = groupSharedBuffer[startIndex + 0 * (N/8) + k];
		Complex F_1_k = groupSharedBuffer[startIndex + 1 * (N/8) + k];
		Complex F_2_k = groupSharedBuffer[startIndex + 2 * (N/8) + k];
		Complex F_3_k = groupSharedBuffer[startIndex + 3 * (N/8) + k];
		Complex F_4_k = groupSharedBuffer[startIndex + 4 * (N/8) + k];
		Complex F_5_k = groupSharedBuffer[startIndex + 5 * (N/8) + k];
		Complex F_6_k = groupSharedBuffer[startIndex + 6 * (N/8) + k];
		Complex F_7_k = groupSharedBuffer[startIndex + 7 * (N/8) + k];

		Complex F_k = Complex(0, 0);
		if(bIsForward)
		{
			F_k += ComplexMultiply(ComplexMultiply(W_N_k(8, 0 * w), W_N_k(N, 0 * k)), F_0_k);
			F_k += ComplexMultiply(ComplexMultiply(W_N_k(8, 1 * w), W_N_k(N, 1 * k)), F_1_k);
			F_k += ComplexMultiply(ComplexMultiply(W_N_k(8, 2 * w), W_N_k(N, 2 * k)), F_2_k);
			F_k += ComplexMultiply(ComplexMultiply(W_N_k(8, 3 * w), W_N_k(N, 3 * k)), F_3_k);
			F_k += ComplexMultiply(ComplexMultiply(W_N_k(8, 4 * w), W_N_k(N, 4 * k)), F_4_k);
			F_k += ComplexMultiply(ComplexMultiply(W_N_k(8, 5 * w), W_N_k(N, 5 * k)), F_5_k);
			F_k += ComplexMultiply(ComplexMultiply(W_N_k(8, 6 * w), W_N_k(N, 6 * k)), F_6_k);
			F_k += ComplexMultiply(ComplexMultiply(W_N_k(8, 7 * w), W_N_k(N, 7 * k)), F_7_k);
		}
		else
		{
			F_k += ComplexMultiply(ComplexConjugate(ComplexMultiply(W_N_k(8, 0 * w), W_N_k(N, 0 * k))), F_0_k);
			F_k += ComplexMultiply(ComplexConjugate(ComplexMultiply(W_N_k(8, 1 * w), W_N_k(N, 1 * k))), F_1_k);
			F_k += ComplexMultiply(ComplexConjugate(ComplexMultiply(W_N_k(8, 2 * w), W_N_k(N, 2 * k))), F_2_k);
			F_k += ComplexMultiply(ComplexConjugate(ComplexMultiply(W_N_k(8, 3 * w), W_N_k(N, 3 * k))), F_3_k);
			F_k += ComplexMultiply(ComplexConjugate(ComplexMultiply(W_N_k(8, 4 * w), W_N_k(N, 4 * k))), F_4_k);
			F_k += ComplexMultiply(ComplexConjugate(ComplexMultiply(W_N_k(8, 5 * w), W_N_k(N, 5 * k))), F_5_k);
			F_k += ComplexMultiply(ComplexConjugate(ComplexMultiply(W_N_k(8, 6 * w), W_N_k(N, 6 * k))), F_6_k);
			F_k += ComplexMultiply(ComplexConjugate(ComplexMultiply(W_N_k(8, 7 * w), W_N_k(N, 7 * k))), F_7_k);
		}

		GroupMemoryBarrierWithGroupSync();
		groupSharedBuffer[index] = F_k;
	}
}

#endif  // __FFT_COMMON__