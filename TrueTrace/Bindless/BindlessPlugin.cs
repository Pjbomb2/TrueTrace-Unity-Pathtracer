using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace Meetem.Bindless
{
    public enum TextureDataType : byte
    {
        None = 0,
        Resource,
        ShaderResourceView
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct BindlessTexture
    {
        public const byte MipUnassigned = 255;
        
        private IntPtr data;
        private TextureDataType type;
        private byte minMip;
        private byte maxMip;
        private byte forceFormat;
        
        private uint unused;
        public readonly IntPtr Data => data;
        public readonly TextureDataType Type => type;
        public readonly byte MinMip => minMip;
        public readonly byte MaxMip => maxMip;
        public readonly bool IsMaxMipAssigned => maxMip != MipUnassigned;
        //public readonly byte ForceFormat => forceFormat;
        
        public static BindlessTexture FromTexture2D(Texture2D texture)
        {
            var d = new BindlessTexture();
            d.minMip = 0;
            d.maxMip = MipUnassigned;

            if (texture == null)
            {
                d.type = TextureDataType.None;
                d.data = IntPtr.Zero;
            }
            else
            {
                d.type = TextureDataType.Resource;
                d.data = texture.GetNativeTexturePtr();
            }

            return d;
        }

        public static BindlessTexture FromRenderTexture(RenderTexture texture)
        {
            var d = new BindlessTexture();
            d.minMip = 0;
            d.maxMip = MipUnassigned;

            if (texture == null)
            {
                d.type = TextureDataType.None;
                d.data = IntPtr.Zero;
            }
            else
            {
                d.type = TextureDataType.Resource;
                d.data = texture.GetNativeTexturePtr();
            }

            return d;
        }

        public static BindlessTexture FromResource(IntPtr ptrFromUnityOrNative)
        {
            var d = new BindlessTexture();
            d.minMip = 0;
            d.maxMip = MipUnassigned;

            if (ptrFromUnityOrNative == IntPtr.Zero)
            {
                d.type = TextureDataType.None;
                d.data = IntPtr.Zero;
            }
            else
            {
                d.type = TextureDataType.Resource;
                d.data = ptrFromUnityOrNative;
            }

            return d;
        }

        public static BindlessTexture FromSrv(IntPtr srvPtr)
        {
            var d = new BindlessTexture();

            // ignored.
            d.minMip = 0;
            d.maxMip = 255;

            if (srvPtr == IntPtr.Zero)
            {
                d.type = TextureDataType.None;
                d.data = IntPtr.Zero;
            }
            else
            {
                d.type = TextureDataType.ShaderResourceView;
                d.data = srvPtr;
            }

            return d;
        }
    }

    public static class BindlessPluginExt
    {
        /*
        public static void InitializeBindless(this CommandBuffer cmdBuf)
        {
        }
        */

        /*
        // Not supported in demo.
        public static void SetBindlessGlobalOffset(this CommandBuffer cmdBuf, int offset)
        {
            if (cmdBuf == null)
                return;
            
            cmdBuf.IssuePluginEventAndData(BindlessPlugin.MeetemBindless_GetRenderEventFuncWithData(), 2147473649, new IntPtr(offset));
        }
        */

        public static void Upload(this BindlessTexture[] array, int arrayOffset = 0, int count = 0)
        {
            BindlessPlugin.SetBindlessTextures(array, arrayOffset, count);
        }
    }
    
    public static class BindlessPlugin
    {
        public const string libName = "GfxPluginMeetemBindless";

        [StructLayout(LayoutKind.Sequential)]
        public struct RenderCustomData
        {
            public IntPtr D3DBufferDrawArgs;
            public IntPtr D3DBufferCountBuffer;
            public int argsOffset;
            public int countOffset;
            public int maxCommands;
        }

        [DllImport(libName)]
        public static extern IntPtr MeetemBindless_GetRenderEventFuncWithData();

        [DllImport(libName)]
        public static extern int MeetemBindless_SetBindlessTextures(uint numTextures, in BindlessTexture bindlessTextures);
        
        [DllImport(libName)]
        public static extern int MeetemBindless_SetBindlessTextures(uint numTextures, IntPtr textures);

        public static bool SetBindlessTextures(BindlessTexture[] pinnedData, int arrayOffset = 0, int arrayCount = 0)
        {
            var ptr = Marshal.UnsafeAddrOfPinnedArrayElement(pinnedData, arrayOffset);
            if (arrayCount <= 0)
                arrayCount = pinnedData.Length - arrayOffset;
            
            return MeetemBindless_SetBindlessTextures((uint)arrayCount, ptr) != 0;
        }
        
        public static bool SetBindlessTextures(ReadOnlySpan<BindlessTexture> data)
        {
            return MeetemBindless_SetBindlessTextures((uint)data.Length, in data.GetPinnableReference()) != 0;
        }
        

    }
}