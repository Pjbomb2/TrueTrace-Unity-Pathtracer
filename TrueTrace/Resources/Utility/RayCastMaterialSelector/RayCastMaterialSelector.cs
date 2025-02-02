#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CommonVars;

namespace TrueTrace {
    public class RayCastMaterialSelector
    {
        ComputeShader RayCastShader;
        ComputeShader IntersectionShader;
        ComputeBuffer BufferSizeBuffer;
        ComputeBuffer GlobalRayBuffer;
        ComputeBuffer LightingBuffer;
        ComputeBuffer OutputBuffer;
        RenderTexture ThrowawayTex;

        [System.Serializable]
        public struct BufferSizeData
        {
            public int tracerays;
            public int shadow_rays;
            public int heightmap_rays;
            public int Heightmap_shadow_rays;
            public int TracedRays;
            public int TracedRaysShadow;
        }

        public int CastRay(Camera _camera, int SourceWidth, int SourceHeight) {
            RayCastShader = Resources.Load<ComputeShader>("Utility/RayCastMaterialSelector/RayCastKernels");
            IntersectionShader = Resources.Load<ComputeShader>("MainCompute/IntersectionKernels");
            BufferSizeData[] BufferSizes = new BufferSizeData[1];
            BufferSizes[0].tracerays = 1;

            BufferSizeBuffer = new ComputeBuffer(1, 24);
            BufferSizeBuffer.SetData(BufferSizes);

            CommonFunctions.CreateDynamicBuffer(ref GlobalRayBuffer, 1, 48);
            CommonFunctions.CreateRenderTexture(ref ThrowawayTex, 1, 1, CommonFunctions.RTFull4);
            CommonFunctions.CreateDynamicBuffer(ref LightingBuffer, 1, 64);
            CommonFunctions.CreateDynamicBuffer(ref OutputBuffer, 1, 4);
            

            RayCastShader.SetInt("screen_width", SourceWidth);
            RayCastShader.SetInt("screen_height", SourceHeight);
            RayCastShader.SetVector("ScreenPosition", Input.mousePosition);
            RayCastShader.SetFloat("NearPlane", _camera.nearClipPlane);
            RayCastShader.SetFloat("FarPlane", _camera.farClipPlane);
            RayCastShader.SetMatrix("CamInvProj", _camera.projectionMatrix.inverse);
            RayCastShader.SetMatrix("CamToWorld", _camera.cameraToWorldMatrix);
            RayCastShader.SetComputeBuffer(0, "GlobalRays", GlobalRayBuffer);
            RayCastShader.Dispatch(0, 1, 1, 1);

            AssetManager.Assets.SetMeshTraceBuffers(IntersectionShader, 0);
            IntersectionShader.SetInt("CurBounce", 0);
            IntersectionShader.SetComputeBuffer(0, "BufferSizes", BufferSizeBuffer);
            IntersectionShader.SetComputeBuffer(0, "GlobalRays", GlobalRayBuffer);
            IntersectionShader.SetComputeBuffer(0, "GlobalColors", LightingBuffer);
            IntersectionShader.SetTexture(0, "_PrimaryTriangleInfo", ThrowawayTex);
            IntersectionShader.Dispatch(0, 1, 1, 1);


            AssetManager.Assets.SetMeshTraceBuffers(RayCastShader, 1);
            RayCastShader.SetComputeBuffer(1, "GlobalRays", GlobalRayBuffer);
            RayCastShader.SetComputeBuffer(1, "OutputBuffer", OutputBuffer);
            RayCastShader.Dispatch(1, 1, 1, 1);
            
            int[] TempOut = new int[1];

            OutputBuffer.GetData(TempOut);

            GlobalRayBuffer.Release();
            ThrowawayTex.Release();
            LightingBuffer.Release();
            OutputBuffer.Release();
            BufferSizeBuffer.Release();

            RayTracingObject[] PossibleObjects = GameObject.FindObjectsOfType<RayTracingObject>();
            int ObjCount = PossibleObjects.Length;
            for(int i = 0; i < ObjCount; i++) {
                int MaterialCount = (int)Mathf.Min(PossibleObjects[i].MaterialIndex.Length, PossibleObjects[i].Indexes.Length);
                for (int i3 = 0; i3 < MaterialCount; i3++) {
                    if(TempOut[0] == (PossibleObjects[i].MaterialIndex[i3] + PossibleObjects[i].MatOffset)) {
                        UnityEditor.Selection.activeGameObject = PossibleObjects[i].gameObject;
                        return i3;
                    }

                }
            }
            return -1;


        }

    }
}
#endif
