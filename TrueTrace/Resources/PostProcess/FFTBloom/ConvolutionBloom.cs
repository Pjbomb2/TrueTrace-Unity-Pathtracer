using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using CommonVars;
//https://github.com/AKGWSB/FFTConvolutionBloom/blob/main/Assets/Scripts/ConvolutionBloom.cs

namespace TrueTrace {
    [System.Serializable]
    public class ConvolutionBloom
    {
        public bool Initialized = false;
        public RenderTexture m_sourceTexture;
        public RenderTexture SourceFreqTexture;
        public RenderTexture m_kernelTexture;
        public RenderTexture KernelFreqTexture;
        public Material FFTBlitMaterial;
        public ComputeShader FFTShader;

        public Vector2 KernelPositionOffset = new Vector2(0, 0);

        public Texture2D KernelTexture;

        private int kFFTVertical;
        private int kConvolution;
        private int kKernelTransform;
        private int kTwoForOneFFTForwardHorizontal;
        private int kTwoForOneFFTInverseHorizontal;

        public void ClearAll() {
            m_sourceTexture.ReleaseSafe();
            m_kernelTexture.ReleaseSafe();
            SourceFreqTexture.ReleaseSafe();
            KernelFreqTexture.ReleaseSafe();
            Initialized = false;
        }
        void OnApplicationQuit() {
            ClearAll();
        }

        public void Init() {

            if (FFTShader == null) FFTShader = Resources.Load<ComputeShader>("PostProcess/FFTBloom/FFTCS");
            if (FFTBlitMaterial == null) FFTBlitMaterial = new Material(Shader.Find("ConvolutionBloom/FFTBlit"));

            KernelTexture = Resources.Load<Texture2D>("PostProcess/FFTBloom/DefaultBloomKernel");

            int FFTSpaceSize = 512;

            CommonFunctions.CreateRenderTexture(ref m_sourceTexture, FFTSpaceSize, FFTSpaceSize, CommonFunctions.RTFull4);
            CommonFunctions.CreateRenderTexture(ref m_kernelTexture, FFTSpaceSize, FFTSpaceSize, CommonFunctions.RTFull4);

            CommonFunctions.CreateRenderTexture(ref SourceFreqTexture, FFTSpaceSize * 2, FFTSpaceSize, CommonFunctions.RTFull4);
            CommonFunctions.CreateRenderTexture(ref KernelFreqTexture, FFTSpaceSize * 2, FFTSpaceSize, CommonFunctions.RTFull4);


            kFFTVertical = FFTShader.FindKernel("FFTVertical");
            kConvolution = FFTShader.FindKernel("Convolution");
            kKernelTransform = FFTShader.FindKernel("KernelTransform");
            kTwoForOneFFTForwardHorizontal = FFTShader.FindKernel("TwoForOneFFTForwardHorizontal");
            kTwoForOneFFTInverseHorizontal = FFTShader.FindKernel("TwoForOneFFTInverseHorizontal");


            Initialized = true;
        }

        public void Execute(CommandBuffer cmd, RenderTexture destination, float Intensity, float Threshold, Vector2 KernelSizeScale, float KernelDistanceExp, float KernelDistanceExpClampMin, float KernelDistanceExpScale) {
            Shader.SetGlobalTexture("SourceFrequencyTexture", SourceFreqTexture);
            Shader.SetGlobalTexture("KernelFrequencyTexture", KernelFreqTexture);
            cmd.SetGlobalFloat("FFTBloomIntensity", Intensity);
            cmd.SetGlobalFloat("FFTBloomThreshold", Threshold);
            Vector4 kernelGenParam = new Vector4(KernelPositionOffset.x, KernelPositionOffset.y, KernelSizeScale.x, KernelSizeScale.y);
            cmd.SetGlobalVector("FFTBloomKernelGenParam", kernelGenParam);
            Vector4 kernelGenParam1 = new Vector4(KernelDistanceExp, KernelDistanceExpClampMin, KernelDistanceExpScale, 1);
            cmd.SetGlobalVector("FFTBloomKernelGenParam1", kernelGenParam1);

            cmd.Blit(KernelTexture, m_kernelTexture, FFTBlitMaterial, 2);

// 对 kernel 做变换
            cmd.SetComputeTextureParam(FFTShader, kKernelTransform, "SourceTexture", m_kernelTexture);
            cmd.DispatchCompute(FFTShader, kKernelTransform, 512 / 8, 256 / 8, 1); 
            
            // 降采样 scene color
            cmd.Blit(destination, m_sourceTexture, FFTBlitMaterial, 1);

            // 对卷积核做 FFT
            // 水平
            cmd.SetComputeTextureParam(FFTShader, kTwoForOneFFTForwardHorizontal, "SourceTexture", m_kernelTexture);
            cmd.SetComputeTextureParam(FFTShader, kTwoForOneFFTForwardHorizontal, "FrequencyTexture", KernelFreqTexture);
            cmd.DispatchCompute(FFTShader, kTwoForOneFFTForwardHorizontal, 512, 1, 1); 
            // 竖直
            cmd.SetComputeFloatParam(FFTShader, "IsForward", 1.0f);
            cmd.SetComputeTextureParam(FFTShader, kFFTVertical, "FrequencyTexture", KernelFreqTexture);
            cmd.DispatchCompute(FFTShader, kFFTVertical, 512, 1, 1); 

            // 对原图像做 FFT
            // 水平
            cmd.BeginSample("FFTForwardHorizontal");
            cmd.SetComputeTextureParam(FFTShader, kTwoForOneFFTForwardHorizontal, "SourceTexture", m_sourceTexture);
            cmd.SetComputeTextureParam(FFTShader, kTwoForOneFFTForwardHorizontal, "FrequencyTexture", SourceFreqTexture);
            cmd.DispatchCompute(FFTShader, kTwoForOneFFTForwardHorizontal, 512, 1, 1); 
            cmd.EndSample("FFTForwardHorizontal");
            // 竖直
            cmd.BeginSample("FFTForwardVertical");
            cmd.SetComputeFloatParam(FFTShader, "IsForward", 1.0f);
            cmd.SetComputeTextureParam(FFTShader, kFFTVertical, "FrequencyTexture", SourceFreqTexture);
            cmd.DispatchCompute(FFTShader, kFFTVertical, 512, 1, 1); 
            cmd.EndSample("FFTForwardVertical");

            // 频域卷积
            cmd.BeginSample("Convolution");
            cmd.SetComputeTextureParam(FFTShader, kConvolution, "SourceFrequencyTexture", SourceFreqTexture);
            cmd.SetComputeTextureParam(FFTShader, kConvolution, "KernelFrequencyTexture", KernelFreqTexture);
            cmd.DispatchCompute(FFTShader, kConvolution, 512 / 8, 512 / 8, 1); 
            cmd.EndSample("Convolution");

            // 还原原图像
            // 竖直
            cmd.BeginSample("FFTInverseVertical");
            cmd.SetComputeFloatParam(FFTShader, "IsForward", 0.0f);
            cmd.SetComputeTextureParam(FFTShader, kFFTVertical, "FrequencyTexture", SourceFreqTexture);
            cmd.DispatchCompute(FFTShader, kFFTVertical, 512, 1, 1); 
            cmd.EndSample("FFTInverseVertical");
            // 水平
            cmd.BeginSample("FFTInverseHorizontal");
            cmd.SetComputeTextureParam(FFTShader, kTwoForOneFFTInverseHorizontal, "FrequencyTexture", SourceFreqTexture);
            cmd.DispatchCompute(FFTShader, kTwoForOneFFTInverseHorizontal, 512, 1, 1);       
            cmd.EndSample("FFTInverseHorizontal");

            // final blit
            cmd.Blit(SourceFreqTexture, destination, FFTBlitMaterial, 0);
        }


    }
}