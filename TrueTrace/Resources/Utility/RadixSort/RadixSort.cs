using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CommonVars;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TrueTrace {
    public class RadixSort : MonoBehaviour
    {
        ComputeShader RadixShader;
        ComputeBuffer InputBuffer;


        ComputeBuffer KeysBuffer;
        ComputeBuffer IndexesBuffer;
        ComputeBuffer SecondaryIndexesBuffer;
        ComputeBuffer RadixCountsBuffer;
        ComputeBuffer PrefixBuffer;
        ComputeBuffer TileIndexBuffer;

        public uint[] DebugIndexBuffer;
        public uint[] DebugPrefixBuffer;
        public uint[] DebugCountsBuffer;
        public uint[] CPUDebugCountsBuffer;
        public uint[] DebugKeysBuffer;
        public uint[] CPUDebugKeysBuffer;
        
        uint ExtractBits256(uint A, uint Selection) {
            return (A >> (int)(Selection * 8)) & 0xFF;
        }


        private const int BufferCount = 1024;
        public AABB[] BufferAABBs;
        public void InitiateTest() {
            if (RadixShader == null) RadixShader = Resources.Load<ComputeShader>("Utility/RadixSort/RadixSort");
            BufferAABBs = new AABB[BufferCount];
            Random.InitState(1224323);

            for(int i = 0; i < BufferCount; i++) {
                BufferAABBs[i] = new AABB();
                BufferAABBs[i].BBMax = new Vector3(Random.value, Random.value, Random.value) * 12.0f;
                BufferAABBs[i].Extend(new Vector3(Random.value, Random.value, Random.value) * 12.0f);
            }
            CommonFunctions.CreateDynamicBuffer(ref InputBuffer, BufferCount, 24);
            InputBuffer.SetData(BufferAABBs);

            CommonFunctions.CreateDynamicBuffer(ref KeysBuffer, BufferCount * 3, 4);
            CommonFunctions.CreateDynamicBuffer(ref IndexesBuffer, BufferCount * 3, 4);
            CommonFunctions.CreateDynamicBuffer(ref SecondaryIndexesBuffer, BufferCount * 3, 4);
            CommonFunctions.CreateDynamicBuffer(ref RadixCountsBuffer, 256 * 4 * 3, 4);
            CommonFunctions.CreateDynamicBuffer(ref PrefixBuffer, 256 * 4 * 3, 4);
            CommonFunctions.CreateDynamicBuffer(ref TileIndexBuffer, Mathf.CeilToInt((float)BufferCount / 256.0f), 4);

            uint[] DebugTileIndexBuffer = new uint[Mathf.CeilToInt((float)BufferCount / 256.0f)];
            TileIndexBuffer.SetData(DebugTileIndexBuffer);

            DebugCountsBuffer = new uint[256 * 4 * 3];
            CPUDebugCountsBuffer = new uint[256 * 4 * 3];
            DebugPrefixBuffer = new uint[256 * 4 * 3];
            DebugKeysBuffer = new uint[BufferCount * 3];
            CPUDebugKeysBuffer = new uint[BufferCount * 3];
            RadixCountsBuffer.SetData(DebugCountsBuffer);
            PrefixBuffer.SetData(DebugPrefixBuffer);

            RadixShader.SetInt("TriCount", BufferCount);
            RadixShader.SetBuffer(0, "Input", InputBuffer);
            RadixShader.SetBuffer(0, "Keys", KeysBuffer);
            RadixShader.SetBuffer(0, "Indexes", IndexesBuffer);
            RadixShader.Dispatch(0, Mathf.CeilToInt((float)BufferCount / 128.0f), 1, 1);

            RadixShader.SetBuffer(1, "Keys", KeysBuffer);
            RadixShader.SetBuffer(1, "RadixCounts", RadixCountsBuffer);
            RadixShader.Dispatch(1, Mathf.CeilToInt((float)BufferCount / 64.0f), 1, 1);

            RadixShader.SetBuffer(2, "RadixCounts", RadixCountsBuffer);
            RadixShader.SetBuffer(2, "PrefixBuffer", PrefixBuffer);
            RadixShader.Dispatch(2, 1, 1, 1);


            RadixShader.SetInt("RADIX", 0);
            RadixShader.SetBuffer(3, "Keys", KeysBuffer);
            RadixShader.SetBuffer(3, "PrefixBuffer", PrefixBuffer);
            RadixShader.SetBuffer(3, "TileIndexBuffer", TileIndexBuffer);
            RadixShader.SetBuffer(3, "IndexBufferRead", IndexesBuffer);
            RadixShader.SetBuffer(3, "IndexBufferWrite", SecondaryIndexesBuffer);
            RadixShader.Dispatch(3, Mathf.CeilToInt(BufferCount / 256.0f), 1, 1);

            // RadixShader.SetInt("RADIX", 1);
            // RadixShader.SetBuffer(3, "IndexBufferWrite", IndexesBuffer);
            // RadixShader.SetBuffer(3, "IndexBufferRead", SecondaryIndexesBuffer);
            // RadixShader.Dispatch(3, BufferCount, 3, 1);

            // RadixShader.SetInt("RADIX", 2);
            // RadixShader.SetBuffer(3, "IndexBufferRead", IndexesBuffer);
            // RadixShader.SetBuffer(3, "IndexBufferWrite", SecondaryIndexesBuffer);
            // RadixShader.Dispatch(3, BufferCount, 3, 1);

            // RadixShader.SetInt("RADIX", 3);
            // RadixShader.SetBuffer(3, "IndexBufferWrite", IndexesBuffer);
            // RadixShader.SetBuffer(3, "IndexBufferRead", SecondaryIndexesBuffer);
            // RadixShader.Dispatch(3, BufferCount, 3, 1);


            DebugIndexBuffer = new uint[BufferCount * 3];
            SecondaryIndexesBuffer.GetData(DebugIndexBuffer);

            // float CurPos = -9999999.0f;
            // for(int i = 0; i < BufferCount; i++) {
            //     float NewPos = (BufferAABBs[DebugIndexBuffer[i]].BBMax.x + BufferAABBs[DebugIndexBuffer[i]].BBMin.x) / 2.0f;
            //     if(CurPos > NewPos) {
            //         Debug.LogError("GODDAMNIT: " + i + "; " + NewPos + " : " + CurPos);
            //     }
            //     CurPos = NewPos;
            // }





            PrefixBuffer.GetData(DebugPrefixBuffer);





            RadixCountsBuffer.GetData(DebugCountsBuffer);

            KeysBuffer.GetData(DebugKeysBuffer);

            // for(uint i2 = 0; i2 < 4; i2++) {
            //     uint OverallOffset = 256 * i2 * 3;
            //     for(int i = 0; i < BufferCount; i++) {
            //         uint Bin = ExtractBits256(DebugKeysBuffer[i*3], i2);
            //         CPUDebugCountsBuffer[Bin*3+OverallOffset]++;
            //         Bin = ExtractBits256(DebugKeysBuffer[i*3+1], i2);
            //         CPUDebugCountsBuffer[Bin*3+1+OverallOffset]++;
            //         Bin = ExtractBits256(DebugKeysBuffer[i*3+2], i2);
            //         CPUDebugCountsBuffer[Bin*3+2+OverallOffset]++;
            //     }
            // }
            // for(int i = 0; i <  256 * 3 * 4; i++) {
            //     if(CPUDebugCountsBuffer[i] != DebugCountsBuffer[i]) Debug.LogError("FUCK: " + i + "; " + DebugCountsBuffer[i] + " : " +CPUDebugCountsBuffer[i]);
            // }

            InputBuffer.Dispose();
            KeysBuffer.Dispose();
            IndexesBuffer.Dispose();
            RadixCountsBuffer.Dispose();

        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(RadixSort))]
    public class RadixSortEditor : Editor 
    {
        // SerializedProperty lookAtPoint;
        
        void OnEnable()
        {
            // lookAtPoint = serializedObject.FindProperty("lookAtPoint");
        }

        public override void OnInspectorGUI()
        {

            base.OnInspectorGUI();
            var script = (RadixSort)target;

            if(GUILayout.Button("TestButton", GUILayout.Height(40))) {
                script.InitiateTest();
            }
            // serializedObject.Update();
            // EditorGUILayout.PropertyField(lookAtPoint);
            // serializedObject.ApplyModifiedProperties();
        }
    }


#endif
}