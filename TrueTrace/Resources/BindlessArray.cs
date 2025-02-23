using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Meetem.Bindless
{
    // Meetem: Not burst-friendly, but fine for demo.
    public class BindlessArray : IDisposable
    {
        private GCHandle _arrayHandle;
        private BindlessTexture[] textures;
        private Texture[] textureMap;
        
        private volatile bool isUpdateNeeded;
        private int lastTextureIndex;
        
        public bool IsUpdateNeeded => isUpdateNeeded;
        
        // limited to 2048 in demo
        public BindlessArray(int capacity = 2048)
        {
            textureMap = new Texture[capacity];
            textures = new BindlessTexture[capacity];
            _arrayHandle = GCHandle.Alloc(textures, GCHandleType.Pinned);
        }
        
        public void Clear()
        {
            for (int i = 0; i < textures.Length; i++)
            {
                textureMap[i] = default;
                textures[i] = default;
            }

            lastTextureIndex = 0;
            isUpdateNeeded = true;
        }

        // public int AppendPtr(System.IntPtr nativePtr)
        // {
        //     textures[lastTextureIndex] = BindlessTexture.FromResource(nativePtr);
        //     textureMap[lastTextureIndex] = tex;
            
        //     lastTextureIndex++;
        //     isUpdateNeeded = true;
        //     return lastTextureIndex - 1;
        // }

        public int AppendRaw(Texture tex)
        {
            if (tex is not Texture2D
                && tex is not RenderTexture)
            {
                throw new InvalidOperationException(
                    $"Only Texture2D and RenderTextures are supported in bindless for demo");
            }
            
            var nativePtr = tex.GetNativeTexturePtr();
            textures[lastTextureIndex] = BindlessTexture.FromResource(nativePtr);
            textureMap[lastTextureIndex] = tex;
            
            lastTextureIndex++;
            isUpdateNeeded = true;
            return lastTextureIndex - 1;
        }

        public int Append(Texture2D tex)
        {
            return AppendRaw(tex);
        }
        
        public int Append(RenderTexture tex)
        {
            return AppendRaw(tex);
        }

        public void SetTexture(Texture2D texture, int idx)
        {
            if (ReferenceEquals(textureMap[idx], texture))
                return;

            textures[idx] = BindlessTexture.FromTexture2D(texture);
            textureMap[idx] = texture;
            isUpdateNeeded = true;
        }
        
        public void SetTexture(RenderTexture texture, int idx)
        {
            if (ReferenceEquals(textureMap[idx], texture))
                return;

            textures[idx] = BindlessTexture.FromRenderTexture(texture);
            textureMap[idx] = texture;
            isUpdateNeeded = true;
        }

        public void SetDirty()
        {
            isUpdateNeeded = true;
        }
        
        public void UpdateDescriptors()
        {
            if (!isUpdateNeeded)
                return;

            isUpdateNeeded = false;
            textures.Upload();
        }

        public void Dispose()
        {
            _arrayHandle.Free();
            textureMap = null;
            textures = null;
        }
    }
}