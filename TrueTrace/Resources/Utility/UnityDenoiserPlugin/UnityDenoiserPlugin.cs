using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;

namespace DenoiserPlugin
{
    public enum DenoiserType
    {
        OptiX = 0,
        OIDN = 1,
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DenoiserConfig
    {
        public int imageWidth;
        public int imageHeight;
        public int guideAlbedo;
        public int guideNormal;
        // Used by OptiX
        public int temporalMode;
        // Used by OIDN
        public int cleanAux;
        public int prefilterAux;

        public bool Equals(DenoiserConfig cfg)
        {
            return imageWidth == cfg.imageWidth &&
                   imageHeight == cfg.imageHeight &&
                   guideAlbedo == cfg.guideAlbedo &&
                   guideNormal == cfg.guideNormal &&
                   temporalMode == cfg.temporalMode &&
                   cleanAux == cfg.cleanAux &&
                   prefilterAux == cfg.prefilterAux;
        }
    }

    class DenoiserPluginWrapper : IDisposable
    {
        public DenoiserType Type;
        public DenoiserConfig Config;

        IntPtr m_ptr;
        RenderEventDataArray m_eventData;

        public DenoiserPluginWrapper(DenoiserType type, DenoiserConfig cfg)
        {
            Type = type;
            Config = cfg;

            m_ptr = CreateDenoiser(type, ref cfg);
            m_eventData = new RenderEventDataArray();
        }

        public void Render(CommandBuffer commands, GraphicsBuffer color, GraphicsBuffer output,
                           GraphicsBuffer albedo = null, GraphicsBuffer normal = null, GraphicsBuffer motion = null)
        {
            RenderEventData eventData;
            eventData.denoiser = m_ptr;
            eventData.albedo = Config.guideAlbedo != 0 ? albedo.GetNativeBufferPtr() : IntPtr.Zero;
            eventData.normal = Config.guideNormal != 0 ? normal.GetNativeBufferPtr() : IntPtr.Zero;
            eventData.flow = Config.temporalMode != 0 ? motion.GetNativeBufferPtr() : IntPtr.Zero;
            eventData.color = color.GetNativeBufferPtr();
            eventData.output = output.GetNativeBufferPtr();

            commands.IssuePluginEventAndData(GetRenderEventFunc(), (int) Type, m_eventData.SetData(eventData));
        }

        public void Dispose()
        {
            DestroyDenoiser(Type, m_ptr);
            m_eventData.Dispose();

            GC.SuppressFinalize(this);
        }

        [DllImport("UnityDenoiserPlugin")]
        static extern IntPtr CreateDenoiser(DenoiserType type, ref DenoiserConfig cfg);

        [DllImport("UnityDenoiserPlugin")]
        static extern void DestroyDenoiser(DenoiserType type, IntPtr ptr);

        [DllImport("UnityDenoiserPlugin")]
        static extern IntPtr GetRenderEventFunc();

        [StructLayout(LayoutKind.Sequential)]
        private struct RenderEventData
        {
            public IntPtr denoiser;
            public IntPtr albedo;
            public IntPtr normal;
            public IntPtr flow;
            public IntPtr color;
            public IntPtr output;
        }

        private class RenderEventDataArray : IDisposable
        {
            const int ElementCount = 8;

            int m_index;
            IntPtr[] m_array;

            public RenderEventDataArray()
            {
                m_index = 0;

                m_array = new IntPtr[ElementCount];
                for (int i = 0; i < ElementCount; ++i)
                {
                    int maxElementSize = Marshal.SizeOf<RenderEventData>();
                    m_array[i] = Marshal.AllocHGlobal(maxElementSize);
                }
            }

            public IntPtr SetData<T>(T data)
            {
                m_index = (m_index + 1) % ElementCount;

                IntPtr ptr = m_array[m_index];
                Marshal.StructureToPtr(data, ptr, true);
                return ptr;
            }

            public void Dispose()
            {
                if (m_array != null)
                {
                    for (int i = 0; i < m_array.Length; ++i)
                    {
                        Marshal.FreeHGlobal(m_array[i]);
                    }

                    m_array = null;
                }

                GC.SuppressFinalize(this);
            }
        }
    }
}

