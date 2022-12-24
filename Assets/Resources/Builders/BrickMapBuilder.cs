using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CommonVars;
using System.Runtime.InteropServices;


[System.Serializable]
unsafe public class BrickMapBuilder
{

    private Vector3 Size;
    public Vector3 Center;
    public Vector3 BBMaxTop;
    public Vector3 BBMinTop;
    public Vector3 OrigOrigSize;

    void writetime(string section, System.TimeSpan ts)
    {
        // Format and display the TimeSpan value.
        string elapsedTime = System.String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
            ts.Hours, ts.Minutes, ts.Seconds,
            ts.Milliseconds / 10);
        Debug.Log(section + elapsedTime);
    }

    public List<int> OrderedVoxels;
    private Vector3 GetPosition(uint Index)
    {
        Vector3 location = new Vector3(0, 0, 0);
        location.x = Mathf.Floor((float)(Index % (uint)Size.x));
        location.y = Mathf.Floor((float)(((Index - (uint)location.x) / (uint)Size.x) % (uint)Size.y));
        location.z = Mathf.Floor((float)(((Index - (uint)location.x - (uint)Size.x * (uint)location.y) / ((uint)Size.y * (uint)Size.x))));
        return location + new Vector3(0.5f, 0.5f, 0.5f);
    }

    private Vector3 GetPositionOrig(int Index)
    {
        Vector3 location;
        location.x = Mathf.Floor((float)(Index % (int)OrigOrigSize.x));
        location.y = Mathf.Floor((float)(((Index - (int)location.x) / (int)OrigOrigSize.x) % (int)OrigOrigSize.y));
        location.z = Mathf.Floor((float)(((Index - (int)location.x - (int)OrigOrigSize.x * (int)location.y) / ((int)OrigOrigSize.y * (int)OrigOrigSize.x))));
        return location + new Vector3(0.5f, 0.5f, 0.5f);
    }
    public int TotalDepth;

    public List<NewBrick> BrickMap;
    public int VoxelSize;
    public int BrickSize;
    public int FinalSize;

    System.TimeSpan ts;
    System.Diagnostics.Stopwatch StopWatch = new System.Diagnostics.Stopwatch();
    public void NaiveConstruct(Voxel[] Voxels, int LargestAxis, Vector3 OrigionalSize)
    {
        BrickMap = new List<NewBrick>();
        OrderedVoxels = new List<int>();
        //OrigionalSize = new Vector3(LargestAxis,LargestAxis,LargestAxis);
        //  StopWatch.Start();
        BrickSize = 16;
        VoxelSize = 16;
        FinalSize = VoxelSize;
        do
        {
            FinalSize *= BrickSize;
        } while (FinalSize < LargestAxis);
        List<NewBrick> TempWorkGroup = new List<NewBrick>();
        List<Voxel>[] VoxelBricks = new List<Voxel>[BrickSize * BrickSize * BrickSize];
        Vector3 BBMax = new Vector3(FinalSize, FinalSize, FinalSize);
        Vector3 BBMin = new Vector3(0, 0, 0);
        Vector3 Size = BBMax - BBMin;
        List<Voxel> AliveVoxels = new List<Voxel>();
        List<Voxel>[] VoxelBins = new List<Voxel>[BrickSize * BrickSize * BrickSize];
        for (int i = 0; i < BrickSize * BrickSize * BrickSize; i++)
        {
            VoxelBins[i] = new List<Voxel>();
        }
        int VoxCount = Voxels.Length;
        for (int i2 = 0; i2 < VoxCount; i2++)
        {
            Vector3 location2 = new Vector3(0, 0, 0);
            uint Index = (uint)Voxels[i2].Index;
            location2.x = ((float)(Index % (uint)OrigionalSize.x));
            location2.y = ((float)(((Index - (uint)location2.x) / (uint)OrigionalSize.x) % (uint)OrigionalSize.y));
            location2.z = ((float)(((Index - (uint)location2.x - (uint)OrigionalSize.x * (uint)location2.y) / ((uint)OrigionalSize.x * (uint)OrigionalSize.y))));
            location2 = location2 / (FinalSize / BrickSize);
            location2 = new Vector3(Mathf.Floor(location2.x), Mathf.Floor(location2.y), Mathf.Floor(location2.z));
            VoxelBins[(uint)location2.x + (uint)BrickSize * (uint)location2.y + (uint)BrickSize * (uint)BrickSize * (uint)location2.z].Add(Voxels[i2]);
        }

        //   Debug.Log("A");
        for (int i = 0; i < BrickSize * BrickSize * BrickSize; i++)
        {
            Vector3 location = new Vector3(0, 0, 0);
            location.x = Mathf.Floor((float)((uint)i % (uint)BrickSize));
            location.y = Mathf.Floor((float)((((uint)i - (uint)location.x) / (uint)BrickSize) % (uint)BrickSize));
            location.z = Mathf.Floor((float)((((uint)i - (uint)location.x - (uint)BrickSize * (uint)location.y) / ((uint)BrickSize * (uint)BrickSize))));
            location *= FinalSize / BrickSize;
            Vector3 ThisBBMax = location + Size / BrickSize;
            Vector3 ThisBBMin = location;
            if (VoxelBins[i].Count != 0)
            {
                //   Debug.Log("EEEE " + VoxelBins[i].Count);
                TempWorkGroup.Add(new NewBrick()
                {
                    StartingIndex = BrickMap.Count,
                    BBMax = ThisBBMax,
                    BBMin = ThisBBMin,
                    BrickVoxels = new List<Voxel>(VoxelBins[i])
                });
            }
            BrickMap.Add(new NewBrick()
            {
                StartingIndex = (VoxelBins[i].Count == 0 ? -1 : 0),
                BBMax = ThisBBMax,
                BBMin = ThisBBMin,
                BrickVoxels = VoxelBins[i]
            });
        }
        // StopWatch.Stop(); ts = StopWatch.Elapsed; writetime("Top Layer Pass : ", ts);
        //TempWorkGroup.Reverse();
        //   BrickMap.Reverse();
        List<NewBrick> TempWorkGroup2 = new List<NewBrick>();
        TotalDepth = 0;
        while ((TempWorkGroup2.Count != 0 || TotalDepth == 0) && TotalDepth < 12)
        {
            // StopWatch = System.Diagnostics.Stopwatch.StartNew();
            TempWorkGroup2.Clear();
            int InteriorDepth = 0;
            TempWorkGroup.Reverse();
            Size = TempWorkGroup[TempWorkGroup.Count - 1].BBMax - TempWorkGroup[TempWorkGroup.Count - 1].BBMin;
            int LayerWideOffset = BrickMap.Count;
            while (TempWorkGroup.Count > 0 && Size.x > VoxelSize)
            {
                NewBrick CurrentBrick = TempWorkGroup[TempWorkGroup.Count - 1];
                AliveVoxels = TempWorkGroup[TempWorkGroup.Count - 1].BrickVoxels;
                for (int i = 0; i < BrickSize * BrickSize * BrickSize; i++)
                {
                    VoxelBins[i] = new List<Voxel>();
                }
                int AliveVoxCount = AliveVoxels.Count;
                for (int i2 = 0; i2 < AliveVoxCount; i2++)
                {
                    uint Index = (uint)AliveVoxels[i2].Index;
                    Vector3 location2 = new Vector3(0, 0, 0);
                    location2.x = ((float)(Index % (uint)OrigionalSize.x));
                    location2.y = ((float)(((Index - (uint)location2.x) / (uint)OrigionalSize.x) % (uint)OrigionalSize.y));
                    location2.z = ((float)(((Index - (uint)location2.x - (uint)OrigionalSize.x * (uint)location2.y) / ((uint)OrigionalSize.x * (uint)OrigionalSize.y))));
                    location2 = (location2 - CurrentBrick.BBMin) * BrickSize / (CurrentBrick.BBMax.x - CurrentBrick.BBMin.x);
                    location2 = new Vector3(Mathf.Floor(location2.x), Mathf.Floor(location2.y), Mathf.Floor(location2.z));
                    VoxelBins[(uint)location2.x + (uint)BrickSize * (uint)location2.y + (uint)BrickSize * (uint)BrickSize * (uint)location2.z].Add(AliveVoxels[i2]);
                }


                for (int i = 0; i < BrickSize * BrickSize * BrickSize; i++)
                {
                    if (i == 0)
                    {
                        int InArrayIndex = TempWorkGroup[TempWorkGroup.Count - 1].StartingIndex;
                        var TempBrick = BrickMap[InArrayIndex];
                        TempBrick.StartingIndex = BrickMap.Count;
                        BrickMap[InArrayIndex] = TempBrick;
                    }
                    Vector3 location = new Vector3(0, 0, 0);
                    location.x = ((float)((uint)i % (uint)BrickSize));
                    location.y = ((float)((((uint)i - (uint)location.x) / (uint)BrickSize) % (uint)BrickSize));
                    location.z = ((float)((((uint)i - (uint)location.x - (uint)BrickSize * (uint)location.y) / ((uint)BrickSize * (uint)BrickSize))));
                    location *= (Size.x / (BrickSize));
                    location += CurrentBrick.BBMin;
                    Vector3 ThisBBMax = location + Size / BrickSize;
                    Vector3 ThisBBMin = location;
                    if (VoxelBins[i].Count != 0)
                    {
                        TempWorkGroup2.Add(new NewBrick()
                        {
                            StartingIndex = BrickMap.Count,
                            BBMax = ThisBBMax,
                            BBMin = ThisBBMin,
                            BrickVoxels = VoxelBins[i]
                        });
                    }
                    BrickMap.Add(new NewBrick()
                    {
                        StartingIndex = -1,
                        BBMax = ThisBBMax,
                        BBMin = ThisBBMin,
                        BrickVoxels = VoxelBins[i]
                    });
                    InteriorDepth++;
                    // Debug.Log("EEEE");
                }
                TempWorkGroup.RemoveAt(TempWorkGroup.Count - 1);
            }
            int PublicOffset = 0;
            if (Size.x <= VoxelSize)
            {
                TempWorkGroup.Reverse();
                int[] BrickVox = new int[VoxelSize * VoxelSize * VoxelSize];
                for (int i = 0; i < TempWorkGroup.Count; i++)
                {
                    var TempBrick = BrickMap[TempWorkGroup[i].StartingIndex];
                    TempBrick.StartingIndex = BrickMap.Count + OrderedVoxels.Count;
                    BrickMap[TempWorkGroup[i].StartingIndex] = TempBrick;
                    System.Array.Fill(BrickVox, -1);
                    int BrickVoxCount = TempBrick.BrickVoxels.Count;
                    for (int i2 = 0; i2 < BrickVoxCount; i2++)
                    {
                        Vector3 location2 = new Vector3(0, 0, 0);
                        uint Index = (uint)TempBrick.BrickVoxels[i2].Index;
                        location2.x = Mathf.Floor((float)(Index % (uint)OrigionalSize.x));
                        location2.y = Mathf.Floor((float)(((Index - (uint)location2.x) / (uint)OrigionalSize.x) % (uint)OrigionalSize.y));
                        location2.z = Mathf.Floor((float)(((Index - (uint)location2.x - (uint)OrigionalSize.x * (uint)location2.y) / ((uint)OrigionalSize.x * (uint)OrigionalSize.y))));
                        location2 -= TempBrick.BBMin;
                        BrickVox[(uint)location2.x + (uint)location2.y * (uint)VoxelSize + (uint)location2.z * (uint)VoxelSize * (uint)VoxelSize] = TempBrick.BrickVoxels[i2].Material;
                    }
                    PublicOffset += VoxelSize * VoxelSize * VoxelSize;
                    OrderedVoxels.AddRange(BrickVox);
                }
            }
            TempWorkGroup.AddRange(TempWorkGroup2);
            TotalDepth++;

        }




    }
}
