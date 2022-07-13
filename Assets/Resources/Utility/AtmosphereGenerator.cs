using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class AtmosphereGenerator {
    
    private RenderTexture _TransmittanceLUT;
    public RenderTexture _RayleighTex;
    public RenderTexture _MieTex;
    public RenderTexture _MultiScatterTex;
    public RenderTexture _SkyViewTex;

    public ComputeShader Atmosphere;
    private ComputeBuffer rayleigh_densityC;
    private ComputeBuffer mie_densityC;
    private ComputeBuffer absorption_densityC;

    private int SkyViewKernel;

    public struct DensityProfileLayer {
        public float width;
        public float exp_term;
        public float exp_scale;
        public float linear_term;
        public float constant_term;
    }

    private void CreateComputeBuffer<T>(ref ComputeBuffer buffer, List<T> data, int stride)
        where T : struct
    {
        // Do we already have a compute buffer?
        if (buffer != null) {
            // If no data or buffer doesn't match the given criteria, release it
            if (data.Count == 0 || buffer.count != data.Count || buffer.stride != stride) {
                buffer.Release();
                buffer = null;
            }
        }

        if (data.Count != 0) {
            // If the buffer has been released or wasn't there to
            // begin with, create it
            if (buffer == null) {
                buffer = new ComputeBuffer(data.Count, stride);
            }
            // Set data on the buffer
            buffer.SetData(data);
        }
    }

    public AtmosphereGenerator(ComputeShader Atmosphere, float BottomRadius, float TopRadius) {

        this.Atmosphere = Atmosphere;
        List<DensityProfileLayer> rayleigh_density = new List<DensityProfileLayer>();
        List<DensityProfileLayer> mie_density = new List<DensityProfileLayer>();
        List<DensityProfileLayer> absorption_density = new List<DensityProfileLayer>();
        float rayleigh_scale_height = 8000.0f;
        float mie_scale_height      = 1200.0f;
        float ozone_scale_height    = 15000.0f;
        float ozone_height          = 25000.0f;
        float density = 0.001f;
        Vector3 ray_s = new Vector3(5.802f, 13.558f, 33.1f) * density;
        Vector3 ray_a = new Vector3(0.0f, 0.0f, 0.0f);
        Vector3 ray_e = ray_s + ray_a;
        Vector3 mie_s = new Vector3(3.996f, 3.996f, 3.996f) * density;
        Vector3 mie_a = new Vector3(1.0f, 2.0f, 4.4f) * density;
        Vector3 mie_e = mie_s + mie_a;
        Vector3 ozo_s = new Vector3(0.0f, 0.0f, 0.0f);
        Vector3 ozo_a = new Vector3(0.65f, 1.881f, 0.085f) * density;
        Vector3 ozo_e = ozo_s + ozo_a;
        rayleigh_density.Add(new DensityProfileLayer() {
            width = 0.0f,
            exp_term = 0.0f,
            exp_scale = 0.0f,
            linear_term = 0.0f,
            constant_term = 0.0f
            });
        rayleigh_density.Add(new DensityProfileLayer() {
            width = 0.0f,
            exp_term = 1.0f,
            exp_scale = -1000.0f / rayleigh_scale_height,
            linear_term = 0.0f,
            constant_term = 0.0f
            });
        mie_density.Add(new DensityProfileLayer() {
            width = 0.0f,
            exp_term = 0.0f,
            exp_scale = 0.0f,
            linear_term = 0.0f,
            constant_term = 0.0f
            });
        mie_density.Add(new DensityProfileLayer() {
            width = 0.0f,
            exp_term = 1.0f,
            exp_scale = -1000.0f / mie_scale_height,
            linear_term = 0.0f,
            constant_term = 0.0f
            });
        absorption_density.Add(new DensityProfileLayer() {
            width = ozone_height / 1000.0f,
            exp_term = 0.0f,
            exp_scale = 0.0f,
            linear_term = 1000.0f / ozone_scale_height,
            constant_term = -2.0f / 3.0f
            });
        absorption_density.Add(new DensityProfileLayer() {
            width = 0.0f,
            exp_term = 0.0f,
            exp_scale = 0.0f,
            linear_term = -1000.0f / ozone_scale_height,
            constant_term = 8.0f / 3.0f
            });

        int TransmittanceKernel = Atmosphere.FindKernel("Transmittance_Kernel");
        int SingleScatterKernel = Atmosphere.FindKernel("SingleScatter_Kernel");
        int MultiScatKernel = Atmosphere.FindKernel("NewMultiScattCS_kernel");
        SkyViewKernel = Atmosphere.FindKernel("SkyView_Kernel");

        CreateComputeBuffer(ref rayleigh_densityC, rayleigh_density, 20);
        CreateComputeBuffer(ref mie_densityC, mie_density, 20);
        CreateComputeBuffer(ref absorption_densityC, absorption_density, 20);

        Atmosphere.SetVector("rayleigh_scattering", ray_e);
        Atmosphere.SetVector("solar_irradiance", new Vector3(1.5f, 1.5f, 1.5f));
        Atmosphere.SetVector("absorption_extinction", ozo_e);
        Atmosphere.SetVector("mie_extinction", mie_e);
        Atmosphere.SetVector("mie_scattering", mie_s);
        Atmosphere.SetFloat("mu_s_min",  -0.2f);
    
        Atmosphere.SetBuffer(TransmittanceKernel, "rayleigh_density", rayleigh_densityC);
        Atmosphere.SetBuffer(TransmittanceKernel, "mie_density", mie_densityC);
        Atmosphere.SetBuffer(TransmittanceKernel, "absorption_density", absorption_densityC);
        Atmosphere.SetBuffer(SingleScatterKernel, "rayleigh_density", rayleigh_densityC);
        Atmosphere.SetBuffer(SingleScatterKernel, "mie_density", mie_densityC);
        Atmosphere.SetBuffer(SingleScatterKernel, "absorption_density", absorption_densityC);
        Atmosphere.SetBuffer(MultiScatKernel, "rayleigh_density", rayleigh_densityC);
        Atmosphere.SetBuffer(MultiScatKernel, "mie_density", mie_densityC);
        Atmosphere.SetBuffer(MultiScatKernel, "absorption_density", absorption_densityC);
        Atmosphere.SetFloat("sun_angular_radius", 0.05f);
        Atmosphere.SetFloat("bottom_radius", BottomRadius);
        Atmosphere.SetFloat("top_radius", TopRadius);

        _TransmittanceLUT = new RenderTexture(256, 64, 0,
        RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.sRGB);
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

        _MultiScatterTex = new RenderTexture(32, 32, 0,
        RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.sRGB);
        _MultiScatterTex.enableRandomWrite = true;
        _MultiScatterTex.Create();

        _SkyViewTex = new RenderTexture(192, 108, 0,
        RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.sRGB);
        _SkyViewTex.enableRandomWrite = true;
        _SkyViewTex.Create();

        Atmosphere.SetTexture(SkyViewKernel, "SkyViewTex", _SkyViewTex);


        Atmosphere.SetTexture(TransmittanceKernel, "TransmittanceTex", _TransmittanceLUT);
        Atmosphere.Dispatch(TransmittanceKernel, 256, 64, 1);
        Atmosphere.SetTexture(SingleScatterKernel, "TransmittanceTex", _TransmittanceLUT);
        Atmosphere.SetTexture(SingleScatterKernel, "RayleighTex", _RayleighTex);
        Atmosphere.SetTexture(SingleScatterKernel, "MieTex", _MieTex);
        Atmosphere.Dispatch(SingleScatterKernel, 256, 128, 32);

        Atmosphere.SetTexture(SkyViewKernel, "TransmittanceTexRead", _TransmittanceLUT);

        Atmosphere.SetTexture(MultiScatKernel, "TransmittanceTexRead", _TransmittanceLUT);
        Atmosphere.SetTexture(MultiScatKernel, "MultiScatTex", _MultiScatterTex);
        Atmosphere.Dispatch(MultiScatKernel, 32,32,1);

        rayleigh_densityC?.Release();
        mie_densityC?.Release();
        absorption_densityC?.Release();
        _TransmittanceLUT.Release();
    }

    public void UpdateSky(Vector3 SunDir) {
        Atmosphere.SetMatrix("InvViewProjMat", (Camera.main.projectionMatrix * Camera.main.worldToCameraMatrix).inverse);
        Atmosphere.SetVector("camPos", Camera.main.transform.position);
        Atmosphere.SetVector("UpVector", new Vector3(0,1,0));
        Atmosphere.SetVector("sun_direction", SunDir);
        Atmosphere.Dispatch(SkyViewKernel, (int)Mathf.Ceil(192.0f / 16.0f), (int)Mathf.Ceil(108.0f / 16.0f), 1);
    }

    private void OnDisable() {
        _RayleighTex.Release();
        _MieTex.Release();
    }

}
