/******************************************************************************
 * OneSweep Implementation Toy Demo
 *
 * SPDX-License-Identifier: MIT
 * Author:  Thomas Smith 3/14/2024
 * 
 * Based off of Research by:
 *          Andy Adinets, Nvidia Corporation
 *          Duane Merrill, Nvidia Corporation
 *          https://research.nvidia.com/publication/2022-06_onesweep-faster-least-significant-digit-radix-sort-gpus
 *
 ******************************************************************************/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using System.IO;

public class OneSweep
{
    private static OneSweep instance = new OneSweep(20000000);
    ComputeBuffer toSort;
    ComputeBuffer tempKeyBuffer;
    ComputeBuffer tempPayloadBuffer;
    ComputeBuffer tempGlobalHistBuffer;
    ComputeBuffer tempPassHistBuffer;
    ComputeBuffer tempIndexBuffer;
    // ComputeBuffer errCount;
    ComputeShader m_cs;

        protected const int k_radix = 256;
        protected const int k_radixPasses = 4;
        protected const int k_partitionSize = 3840;

        protected const int k_minSize = 1;
        protected const int k_maxSize = 65535 * k_partitionSize;

        protected LocalKeyword m_ascendKeyword;
        protected LocalKeyword m_ISLIGHTKeyword;
        private int k_maxKeysAllocated;


        protected const int k_globalHistPartSize = 32768;

        protected int m_kernelInit = -1;
        protected int m_kernelGlobalHist = -1;
        protected int m_kernelScan = -1;
        protected int m_digitBinningPass = -1;

        protected readonly bool k_keysOnly;

        protected static int DivRoundUp(int x, int y)
        {
            return (x + y - 1) / y;
        }
        public void Dispose() {
            tempKeyBuffer?.Dispose();
            tempPayloadBuffer?.Dispose();
            tempGlobalHistBuffer?.Dispose();
            tempPassHistBuffer?.Dispose();
            tempIndexBuffer?.Dispose();
            toSort?.Dispose();
            instance = null;
            Debug.LogError("DISPOSED");
        }

        public static OneSweep Instance
        {
            get
            {
                return instance;
            }
        }

        //pairs
        public OneSweep(int k_maxKeysAllocated)
        {
            this.k_maxKeysAllocated = k_maxKeysAllocated;
            if (m_cs == null) {m_cs = Resources.Load<ComputeShader>("Utility/OneSweep/OneSweep"); }
            k_keysOnly = false;

            m_ascendKeyword = new LocalKeyword(m_cs, "SHOULD_ASCEND");
            m_ISLIGHTKeyword = new LocalKeyword(m_cs, "IS_LIGHT");
            InitKernels();


            tempKeyBuffer?.Dispose();
            tempPayloadBuffer?.Dispose();
            tempGlobalHistBuffer?.Dispose();
            tempPassHistBuffer?.Dispose();
            tempIndexBuffer?.Dispose();
            toSort?.Dispose();

            tempKeyBuffer = new ComputeBuffer(k_maxKeysAllocated, 4);
            tempPayloadBuffer = new ComputeBuffer(k_maxKeysAllocated, 4);
            tempGlobalHistBuffer = new ComputeBuffer(k_radix * k_radixPasses, 4);
            tempPassHistBuffer = new ComputeBuffer(k_radix * DivRoundUp(k_maxKeysAllocated, k_partitionSize) * k_radixPasses, 4);
            tempIndexBuffer = new ComputeBuffer(k_radixPasses, 4);
            toSort = new ComputeBuffer(k_maxKeysAllocated, 4);
        }

        protected virtual void InitKernels()
        {
            bool isValid;
            if (m_cs)
            {
                m_kernelInit = m_cs.FindKernel("InitSweep");
                m_kernelGlobalHist = m_cs.FindKernel("GlobalHistogram");
                m_kernelScan = m_cs.FindKernel("Scan");
                m_digitBinningPass = m_cs.FindKernel("DigitBinningPass");
            }

            isValid = m_kernelInit >= 0 &&
                        m_kernelGlobalHist >= 0 &&
                        m_kernelScan >= 0 &&
                        m_digitBinningPass >= 0;

            if (isValid)
            {
                if (!m_cs.IsSupported(m_kernelInit) ||
                    !m_cs.IsSupported(m_kernelGlobalHist) ||
                    !m_cs.IsSupported(m_kernelScan) ||
                    !m_cs.IsSupported(m_digitBinningPass))
                {
                    isValid = false;
                }
            }

        }


        private void SetStaticRootParameters(
            CommandBuffer _cmd,
            ComputeBuffer _sortBuffer,
            ComputeBuffer _passHistBuffer,
            ComputeBuffer _globalHistBuffer,
            ComputeBuffer _indexBuffer,
            int KeyCount)
        {
            _cmd.SetComputeIntParam(m_cs, "e_numKeys", KeyCount);

            _cmd.SetComputeBufferParam(m_cs, m_kernelInit, "b_passHist", _passHistBuffer);
            _cmd.SetComputeBufferParam(m_cs, m_kernelInit, "b_globalHist", _globalHistBuffer);
            _cmd.SetComputeBufferParam(m_cs, m_kernelInit, "b_index", _indexBuffer);

            _cmd.SetComputeBufferParam(m_cs, 0, "b_sort", _sortBuffer);

            _cmd.SetComputeBufferParam(m_cs, m_kernelGlobalHist, "b_sort", _sortBuffer);
            _cmd.SetComputeBufferParam(m_cs, m_kernelGlobalHist, "b_globalHist", _globalHistBuffer);

            _cmd.SetComputeBufferParam(m_cs, m_kernelScan, "b_passHist", _passHistBuffer);
            _cmd.SetComputeBufferParam(m_cs, m_kernelScan, "b_globalHist", _globalHistBuffer);

            _cmd.SetComputeBufferParam(m_cs, m_digitBinningPass, "b_passHist", _passHistBuffer);
            _cmd.SetComputeBufferParam(m_cs, m_digitBinningPass, "b_index", _indexBuffer);
        }

        private void Dispatch(
            CommandBuffer _cmd,
            ComputeBuffer _toSort,
            ComputeBuffer _toSortPayload,
            ComputeBuffer _alt,
            ComputeBuffer _altPayload,
            int KeyCount)
        {
            int threadBlocks = DivRoundUp(KeyCount, k_partitionSize);
            int globalHistThreadBlocks = DivRoundUp(KeyCount, k_globalHistPartSize);
            _cmd.SetComputeIntParam(m_cs, "e_threadBlocks", threadBlocks);
            _cmd.BeginSample("KernInit");
            _cmd.DispatchCompute(m_cs, m_kernelInit, 256, 1, 1);
            _cmd.EndSample("KernInit");

            _cmd.BeginSample("GlobHist");
            _cmd.SetComputeIntParam(m_cs, "e_threadBlocks", globalHistThreadBlocks);
            _cmd.DispatchCompute(m_cs, m_kernelGlobalHist, globalHistThreadBlocks, 1, 1);
            _cmd.EndSample("GlobHist");

            _cmd.SetComputeIntParam(m_cs, "e_threadBlocks", threadBlocks);
            _cmd.BeginSample("KernScan");
            _cmd.DispatchCompute(m_cs, m_kernelScan, k_radixPasses, 1, 1);
            _cmd.EndSample("KernScan");
            for (int radixShift = 0; radixShift < 32; radixShift += 8)
            {
                _cmd.SetComputeIntParam(m_cs, "e_radixShift", radixShift);
                _cmd.SetComputeBufferParam(m_cs, m_digitBinningPass, "b_sort", _toSort);
                _cmd.SetComputeBufferParam(m_cs, m_digitBinningPass, "b_sortPayload", _toSortPayload);
                _cmd.SetComputeBufferParam(m_cs, m_digitBinningPass, "b_alt", _alt);
                _cmd.SetComputeBufferParam(m_cs, m_digitBinningPass, "b_altPayload", _altPayload);
                _cmd.BeginSample("DigBin " + radixShift);
                _cmd.DispatchCompute(m_cs, m_digitBinningPass, threadBlocks, 1, 1);
                _cmd.EndSample("DigBin " + radixShift);

                (_toSort, _alt) = (_alt, _toSort);
                (_toSortPayload, _altPayload) = (_altPayload, _toSortPayload);
            }
        }



        public void Sort(
            CommandBuffer _cmd,
            ComputeBuffer AABBBuffer,
            ComputeBuffer toSortPayload,
            int SortDimension,
            bool ISLIGHT,
            int KeyCount)
        {

            _cmd.EnableKeyword(m_cs, m_ascendKeyword);
            if(ISLIGHT) _cmd.EnableKeyword(m_cs, m_ISLIGHTKeyword);
            else _cmd.DisableKeyword(m_cs, m_ISLIGHTKeyword);


            SetStaticRootParameters(
                _cmd,
                toSort,
                tempPassHistBuffer,
                tempGlobalHistBuffer,
                tempIndexBuffer,
                KeyCount);

            _cmd.SetComputeIntParam(m_cs, "TriCount", KeyCount);
            _cmd.SetComputeIntParam(m_cs, "InputDim", SortDimension);
            _cmd.SetComputeBufferParam(m_cs, 0, "Input", AABBBuffer);
            _cmd.SetComputeBufferParam(m_cs, 0, "b_sortPayload", toSortPayload);
            _cmd.BeginSample("SortInit");
            _cmd.DispatchCompute(m_cs, 0, Mathf.CeilToInt((float)KeyCount / 1024.0f), 1, 1);
            _cmd.EndSample("SortInit");
            Dispatch(
                _cmd,
                toSort,
                toSortPayload,
                tempKeyBuffer,
                tempPayloadBuffer,
                KeyCount);
        }

    }

