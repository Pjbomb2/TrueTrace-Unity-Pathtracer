using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CommonVars;
using UnityEngine.Rendering;

namespace TrueTrace {
    [System.Serializable]
    public class AtmosphereGenerator
    {
        public RenderTexture _TransmittanceLUT;
        public RenderTexture MultiScatterTex;
        private RenderTexture _RayleighTex;
        private RenderTexture _MieTex;

        private RenderTexture DeltaIrradianceTex;
        public RenderTexture IrradianceTex;

        private RenderTexture ScatteringTex;

        private RenderTexture DeltaScatteringTex;

        private RenderTexture DeltaMultiScatterTex;


        public RenderTexture CloudShapeTex;
        public RenderTexture CloudShapeDetailTex;
        public RenderTexture WeatherTex;


        private ComputeShader Atmosphere;
        private ComputeBuffer rayleigh_densityC;
        private ComputeBuffer mie_densityC;
        private ComputeBuffer absorption_densityC;

        // public Texture2D CloudSampA;
        // public Texture2D CloudSampB;

        private int SkyViewKernel;

        public struct DensityProfileLayer
        {
            public float width;
            public float exp_term;
            public float exp_scale;
            public float linear_term;
            public float constant_term;
        }

        public void AssignTextures(ComputeShader ThisShader, int Kernel) {
            ThisShader.SetTexture(Kernel, "ScatteringTex", MultiScatterTex);
            ThisShader.SetTexture(Kernel, "TransmittanceTex", _TransmittanceLUT);
            ThisShader.SetTexture(Kernel, "IrradianceTex", IrradianceTex);
        }

        public void Dispose() {
            if(_TransmittanceLUT != null) _TransmittanceLUT.Release();
            if(MultiScatterTex != null) MultiScatterTex.Release();
            if(IrradianceTex != null) IrradianceTex.Release();
            if(CloudShapeTex != null) CloudShapeTex.Release();
            if(CloudShapeDetailTex != null) CloudShapeDetailTex.Release();
            if(WeatherTex != null) WeatherTex.Release();
        }

        public AtmosphereGenerator(float BottomRadius, float TopRadius, int MultiScatterIterations)
        {
            if (Atmosphere == null) { Atmosphere = Resources.Load<ComputeShader>("Utility/Atmosphere/AtmosphereLUTGenerator"); }
            List<DensityProfileLayer> rayleigh_density = new List<DensityProfileLayer>();
            List<DensityProfileLayer> mie_density = new List<DensityProfileLayer>();
            List<DensityProfileLayer> absorption_density = new List<DensityProfileLayer>();
            float rayleigh_scale_height = 8696.45f;
            float mie_scale_height = 1200.0f;
            float ozone_scale_height = 22349.90f;
            float ozone_height = 35660.71f;
            // float density = 0.001f;
            Vector3 ray_s = new Vector3(0.005802339f, 0.013557760f, 0.033100010f);
            Vector3 ray_a = new Vector3(0.0f, 0.0f, 0.0f);
            Vector3 ray_e = ray_s + ray_a;
            Vector3 mie_s = new Vector3 (0.003996000f, 0.003996000f, 0.003996000f);
            Vector3 mie_a = new Vector3 (0.004440000f, 0.004440000f, 0.004440000f);
            Vector3 mie_e = mie_s + mie_a;
            Vector3 ozo_e = new Vector3 (0.000649717f, 0.001880900f, 0.000085017f);
            rayleigh_density.Add(new DensityProfileLayer()
            {
                width = 0.0f,
                exp_term = 0.0f,
                exp_scale = 0.0f,
                linear_term = 0.0f,
                constant_term = 0.0f
            });
            rayleigh_density.Add(new DensityProfileLayer()
            {
                width = 0.0f,
                exp_term = 1.0f,
                exp_scale = -1.0f / rayleigh_scale_height * 1000.0f,
                linear_term = 0.0f,
                constant_term = 0.0f
            });
            mie_density.Add(new DensityProfileLayer()
            {
                width = 0.0f,
                exp_term = 0.0f,
                exp_scale = 0.0f,
                linear_term = 0.0f,
                constant_term = 0.0f
            });
            mie_density.Add(new DensityProfileLayer()
            {
                width = 0.0f,
                exp_term = 1.0f,
                exp_scale = -1.0f / mie_scale_height * 1000.0f,
                linear_term = 0.0f,
                constant_term = 0.0f
            });
            absorption_density.Add(new DensityProfileLayer()
            {
                width = ozone_height / 1000.0f,
                exp_term = 0.0f,
                exp_scale = 0.0f,
                linear_term = 1.0f / ozone_scale_height * 1000.0f,
                constant_term = -0.666666666666667f
            });
            absorption_density.Add(new DensityProfileLayer()
            {
                width = 0.0f,
                exp_term = 0.0f,
                exp_scale = 0.0f,
                linear_term = -1.0f / ozone_scale_height * 1000.0f,
                constant_term = 2.66666666666667f
            });

            int TransmittanceKernel = Atmosphere.FindKernel("Transmittance_Kernel");
            int SingleScatterKernel = Atmosphere.FindKernel("SingleScatter_Kernel");
            int DirectIrradianceKernel = Atmosphere.FindKernel("DirectIrradiance_Kernel");
            int IndirectIrradianceKernel = Atmosphere.FindKernel("IndirectIrradiance_Kernel");
            int ScatteringDensityKernel = Atmosphere.FindKernel("ScatteringDensity_kernel");
            int MultipleScatteringKernel = Atmosphere.FindKernel("MultiScatter_kernel");
            int CloudShapeKernel = Atmosphere.FindKernel("CloudShapeKernel");
            int CloudShapeDetailKernel = Atmosphere.FindKernel("CloudShapeDetailKernel");
            int WeatherKernel = Atmosphere.FindKernel("WeatherKernel");

            CommonFunctions.CreateComputeBuffer(ref rayleigh_densityC, rayleigh_density);
            CommonFunctions.CreateComputeBuffer(ref mie_densityC, mie_density);
            CommonFunctions.CreateComputeBuffer(ref absorption_densityC, absorption_density);

            Atmosphere.SetVector("rayleigh_scattering", ray_s);
            Atmosphere.SetVector("solar_irradiance", new Vector3(1.474f, 1.8504f, 1.91198f));
            Atmosphere.SetVector("absorption_extinction", ozo_e);
            Atmosphere.SetVector("mie_extinction", mie_e);
            Atmosphere.SetVector("mie_scattering", mie_s);
            Atmosphere.SetFloat("mu_s_min", -0.5f);

            Atmosphere.SetBuffer(TransmittanceKernel, "rayleigh_density", rayleigh_densityC);
            Atmosphere.SetBuffer(TransmittanceKernel, "mie_density", mie_densityC);
            Atmosphere.SetBuffer(TransmittanceKernel, "absorption_density", absorption_densityC);
            Atmosphere.SetBuffer(SingleScatterKernel, "rayleigh_density", rayleigh_densityC);
            Atmosphere.SetBuffer(SingleScatterKernel, "mie_density", mie_densityC);
            Atmosphere.SetBuffer(SingleScatterKernel, "absorption_density", absorption_densityC);
            Atmosphere.SetBuffer(ScatteringDensityKernel, "rayleigh_density", rayleigh_densityC);
            Atmosphere.SetBuffer(ScatteringDensityKernel, "mie_density", mie_densityC);
            Atmosphere.SetBuffer(ScatteringDensityKernel, "absorption_density", absorption_densityC);
            Atmosphere.SetFloat("sun_angular_radius", 0.00935f / 2.0f);
            Atmosphere.SetFloat("bottom_radius", BottomRadius);
            Atmosphere.SetFloat("top_radius", TopRadius);

            _TransmittanceLUT = new RenderTexture(256, 64, 0,
            RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            _TransmittanceLUT.enableRandomWrite = true;
            _TransmittanceLUT.Create();
            _RayleighTex = new RenderTexture(256, 128, 0,
            RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.sRGB);
            _RayleighTex.volumeDepth = 32;
            _RayleighTex.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
            _RayleighTex.enableRandomWrite = true;
            _RayleighTex.Create();
            _MieTex = new RenderTexture(256, 128, 0,
            RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.sRGB);
            _MieTex.volumeDepth = 32;
            _MieTex.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
            _MieTex.enableRandomWrite = true;
            _MieTex.Create();

            DeltaIrradianceTex = new RenderTexture(64, 16, 0,
            RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.sRGB);
            DeltaIrradianceTex.enableRandomWrite = true;
            DeltaIrradianceTex.Create();

            IrradianceTex = new RenderTexture(64, 16, 0,
            RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.sRGB);
            IrradianceTex.enableRandomWrite = true;
            IrradianceTex.Create();

            ScatteringTex = new RenderTexture(8 * 32, 128, 0,
            RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.sRGB);
            ScatteringTex.volumeDepth = 32;
            ScatteringTex.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
            ScatteringTex.enableRandomWrite = true;
            ScatteringTex.Create();

            DeltaScatteringTex = new RenderTexture(8 * 32, 128, 0,
            RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.sRGB);
            DeltaScatteringTex.volumeDepth = 32;
            DeltaScatteringTex.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
            DeltaScatteringTex.enableRandomWrite = true;
            DeltaScatteringTex.Create();

            DeltaMultiScatterTex = new RenderTexture(8 * 32, 128, 0,
            RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.sRGB);
            DeltaMultiScatterTex.volumeDepth = 32;
            DeltaMultiScatterTex.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
            DeltaMultiScatterTex.enableRandomWrite = true;
            DeltaMultiScatterTex.Create();

            MultiScatterTex = new RenderTexture(8 * 32, 128, 0,
            RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.sRGB);
            MultiScatterTex.volumeDepth = 32;
            MultiScatterTex.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
            MultiScatterTex.enableRandomWrite = true;
            MultiScatterTex.Create();

            Atmosphere.SetTexture(TransmittanceKernel, "TransmittanceTex", _TransmittanceLUT);
            Atmosphere.Dispatch(TransmittanceKernel, 32, 8, 1);
            Atmosphere.SetTexture(SingleScatterKernel, "TransmittanceTexRead", _TransmittanceLUT);
            Atmosphere.SetTexture(SingleScatterKernel, "RayleighTex", _RayleighTex);
            Atmosphere.SetTexture(SingleScatterKernel, "MieTex", _MieTex);
            Atmosphere.SetTexture(SingleScatterKernel, "ScatteringTex", ScatteringTex);
            Atmosphere.Dispatch(SingleScatterKernel, 32, 16, 4);

            Atmosphere.SetInt("ScatteringOrder", 1);
            int NumScatteringOrder = MultiScatterIterations;
            Atmosphere.SetTexture(DirectIrradianceKernel, "DeltaIrradianceTex", DeltaIrradianceTex);
            Atmosphere.SetTexture(DirectIrradianceKernel, "IrradianceTex", IrradianceTex);
            Atmosphere.SetTexture(DirectIrradianceKernel, "TransmittanceTexRead", _TransmittanceLUT);
            Atmosphere.Dispatch(DirectIrradianceKernel, 64, 16, 1);

            Graphics.CopyTexture(ScatteringTex, MultiScatterTex);
            for (int ScatteringOrder = 2; ScatteringOrder <= NumScatteringOrder; ++ScatteringOrder)
            {
                var TempScatOrder = ScatteringOrder;
                Atmosphere.SetInt("ScatteringOrder", TempScatOrder);
                Atmosphere.SetTexture(ScatteringDensityKernel, "IrradianceTexRead", DeltaIrradianceTex);
                Atmosphere.SetTexture(ScatteringDensityKernel, "TransmittanceTexRead", _TransmittanceLUT);
                Atmosphere.SetTexture(ScatteringDensityKernel, "RayleighTexRead", _RayleighTex);
                Atmosphere.SetTexture(ScatteringDensityKernel, "MieTexRead", _MieTex);
                Atmosphere.SetTexture(ScatteringDensityKernel, "MultipleScatteringTexRead", DeltaMultiScatterTex);
                Atmosphere.SetTexture(ScatteringDensityKernel, "ScatteringDensityTex", DeltaScatteringTex);
                Atmosphere.Dispatch(ScatteringDensityKernel, 32, 16, 4);

                var TempScatOrder2 = ScatteringOrder - 1;
                Atmosphere.SetInt("ScatteringOrder", TempScatOrder2);
                Atmosphere.SetTexture(IndirectIrradianceKernel, "IrradianceTex", IrradianceTex);
                Atmosphere.SetTexture(IndirectIrradianceKernel, "DeltaIrradianceTex", DeltaIrradianceTex);
                Atmosphere.SetTexture(IndirectIrradianceKernel, "RayleighTexRead", _RayleighTex);
                Atmosphere.SetTexture(IndirectIrradianceKernel, "MieTexRead", _MieTex);
                Atmosphere.SetTexture(IndirectIrradianceKernel, "MultipleScatteringTexRead", DeltaMultiScatterTex);
                Atmosphere.Dispatch(IndirectIrradianceKernel, 64, 16, 1);

                Atmosphere.SetInt("ScatteringOrder", TempScatOrder);
                Atmosphere.SetTexture(MultipleScatteringKernel, "DeltaMultipleScattering", DeltaMultiScatterTex);
                Atmosphere.SetTexture(MultipleScatteringKernel, "MultiScatterTex", MultiScatterTex);
                Atmosphere.SetTexture(MultipleScatteringKernel, "ScatteringDensityTexRead", DeltaScatteringTex);
                Atmosphere.SetTexture(MultipleScatteringKernel, "TransmittanceTexRead", _TransmittanceLUT);
                Atmosphere.Dispatch(MultipleScatteringKernel, 32, 16, 4);


            }

            rayleigh_densityC.Release();
            mie_densityC.Release();
            absorption_densityC.Release();

            _RayleighTex.Release();
            _MieTex.Release();
            DeltaIrradianceTex.Release();
            ScatteringTex.Release();
            DeltaScatteringTex.Release();
            DeltaMultiScatterTex.Release();


//             CloudShapeTex = new RenderTexture(128, 128, 0,
//             RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.sRGB);
//             CloudShapeTex.volumeDepth = 128;
//             CloudShapeTex.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
//             CloudShapeTex.enableRandomWrite = true;
//             CloudShapeTex.Create();

//             CloudShapeDetailTex = new RenderTexture(32, 32, 0,
//             RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.sRGB);
//             CloudShapeDetailTex.volumeDepth = 32;
//             CloudShapeDetailTex.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
//             CloudShapeDetailTex.enableRandomWrite = true;
//             CloudShapeDetailTex.Create();

//             WeatherTex = new RenderTexture(512, 512, 0,
//             RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.sRGB);
//             WeatherTex.useMipMap = true;
//             WeatherTex.autoGenerateMips = false;
//             WeatherTex.enableRandomWrite = true;
//             WeatherTex.Create();

//             Atmosphere.SetTexture(CloudShapeKernel, "CloudShapeTex", CloudShapeTex);
//             Atmosphere.Dispatch(CloudShapeKernel, 128, 128, 128);

//             Atmosphere.SetTexture(CloudShapeDetailKernel, "CloudShapeDetailTex", CloudShapeDetailTex);
//             Atmosphere.Dispatch(CloudShapeDetailKernel, 32, 32, 32);
            
// CloudSampA = Resources.Load<Texture2D>("Utility/Atmosphere/GgAa20KbQAABHGm"); 
// CloudSampB = Resources.Load<Texture2D>("Utility/Atmosphere/GgAa3ZuaIAAjUL8"); 
//             Atmosphere.SetTexture(WeatherKernel, "WeatherTex", WeatherTex);
//             Atmosphere.SetTexture(WeatherKernel, "CloudSampA", CloudSampA);
//             Atmosphere.SetTexture(WeatherKernel, "CloudSampB", CloudSampB);
//             Atmosphere.Dispatch(WeatherKernel, Mathf.CeilToInt(512.0f / 16.0f), Mathf.CeilToInt(512.0f / 16.0f), 1);

//             WeatherTex.GenerateMips();
        }


    }
}