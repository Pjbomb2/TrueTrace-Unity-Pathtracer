    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using CommonVars;

namespace TrueTrace {
    public class DetailedObjectInstance
    {
        public GameObject Prefab;
        public Vector3 Position;
        public Vector3 Rotation;
        public Vector3 Scale;

        public static DetailedObjectInstance[] ExportObjects(Terrain terrain)
        {

            List<DetailedObjectInstance> output = new List<DetailedObjectInstance>();

            TerrainData data = terrain.terrainData;
            if (terrain.detailObjectDensity != 0)
            {

                int detailWidth = data.detailWidth;
                int detailHeight = data.detailHeight;


                float delatilWToTerrainW = data.size.x / detailWidth;
                float delatilHToTerrainW = data.size.z / detailHeight;

                Vector3 mapPosition = terrain.transform.position;

                bool doDentisy = false;
                float targetDentisty = 0;
                if (terrain.detailObjectDensity != 1)
                {
                    targetDentisty = (1 / (1f - terrain.detailObjectDensity));
                    doDentisy = true;
                }


                float currentDentity = 0;

                DetailPrototype[] details = data.detailPrototypes;
                for (int i = 0; i < details.Length; i++)
                {
                    GameObject Prefab = details[i].prototype;

                    float minWidth = details[i].minWidth;
                    float maxWidth = details[i].maxWidth;

                    float minHeight = details[i].minHeight;
                    float maxHeight = details[i].maxHeight;

                    int[,] map = data.GetDetailLayer(0, 0, data.detailWidth, data.detailHeight, 0);

                    List<Vector3> grasses = new List<Vector3>();
                    for (var y = 0; y < data.detailHeight; y++)
                    {
                        for (var x = 0; x < data.detailWidth; x++)
                        {
                            if (map[x, y] > 0)
                            {
                                currentDentity += 1f;


                                bool pass = false;
                                if (!doDentisy)
                                    pass = true;
                                else
                                    pass = currentDentity < targetDentisty;

                                if (pass)
                                {
                                    float _z = (x * delatilWToTerrainW) + mapPosition.z;
                                    float _x = (y * delatilHToTerrainW) + mapPosition.x;
                                    float _y = terrain.SampleHeight(new Vector3(_x, 0, _z));
                                    grasses.Add(new Vector3(
                                        _x,
                                        _y,
                                        _z
                                        ));
                                }
                                else
                                {
                                    currentDentity -= targetDentisty;
                                }

                            }
                        }
                    }

                    foreach (var item in grasses)
                    {
                        DetailedObjectInstance e = new DetailedObjectInstance();
                        e.Prefab = Prefab;

                        e.Position = item;
                        e.Rotation = new Vector3(0, UnityEngine.Random.Range(0, 360), 0);
                        e.Scale = new Vector3(UnityEngine.Random.Range(minWidth, maxWidth), UnityEngine.Random.Range(minHeight, maxHeight), UnityEngine.Random.Range(minWidth, maxWidth));

                        output.Add(e);
                    }
                }
            }


            return output.ToArray();
        }
    }


    [ExecuteInEditMode]
    [System.Serializable]
    public class TerrainObject : MonoBehaviour
    {
        Terrain TerrainTile;
        public Texture2D HeightMap;
        public Texture2D AlphaMap;
        public TreeInstance[] Trees;
        public DetailedObjectInstance[] Details;
        // Start is called before the first frame update
        public List<Texture> AlbedoTexs;
        public List<RayObjects> AlbedoIndexes;
        public List<Texture> NormalTexs;
        public List<RayObjects> NormalIndexes;
        public List<Texture> MetallicTexs;
        public List<RayObjects> MetallicIndexes;
        public List<Texture> RoughnessTexs;
        public List<RayObjects> RoughnessIndexes;
        public List<MaterialData> Materials;
        public int[] MaterialIndex;

        public int TerrainDim;
        public float HeightScale;

        public void ClearAll()
        {
            Trees = null;
            TerrainTile = null;
        }
        public void Load()
        {
            TerrainTile = this.gameObject.GetComponent<Terrain>();
            HeightMap = new Texture2D(TerrainTile.terrainData.heightmapResolution, TerrainTile.terrainData.heightmapResolution, UnityEngine.Experimental.Rendering.GraphicsFormat.R16_UNorm, UnityEngine.Experimental.Rendering.TextureCreationFlags.None);

            Graphics.ConvertTexture(TerrainTile.terrainData.heightmapTexture, HeightMap);
            HeightMap.ReadPixels(new Rect(0, 0, HeightMap.width, HeightMap.height), 0, 0, true);
            HeightMap.Apply();//ReadPixels(new Rect(0,0,1,1)
            AlphaMap = TerrainTile.terrainData.alphamapTextures[0];//new RenderTexture(TerrainTile.terrainData.alphamapResolution, TerrainTile.terrainData.alphamapResolution, 0, TerrainTile.terrainData.alphamapTextures[0].format, RenderTextureReadWrite.Linear);

            HeightScale = TerrainTile.terrainData.heightmapScale.y;
            TerrainDim = (int)TerrainTile.terrainData.size.x;

            AlbedoTexs = new List<Texture>();
            AlbedoIndexes = new List<RayObjects>();
            NormalTexs = new List<Texture>();
            NormalIndexes = new List<RayObjects>();
            MetallicTexs = new List<Texture>();
            MetallicIndexes = new List<RayObjects>();
            RoughnessTexs = new List<Texture>();
            RoughnessIndexes = new List<RayObjects>();
            Materials = new List<MaterialData>();

            int TerrainLayerCount = TerrainTile.terrainData.terrainLayers.Length;
            MaterialIndex = new int[TerrainLayerCount];
            for (int i = 0; i < TerrainLayerCount; i++)
            {
                RayObjectTextureIndex TempObj = new RayObjectTextureIndex();
                TempObj.Terrain = this;
                TempObj.ObjIndex = i;
                if (TerrainTile.terrainData.terrainLayers[i].diffuseTexture != null)
                {
                    if (AlbedoTexs.Contains(TerrainTile.terrainData.terrainLayers[i].diffuseTexture))
                    {
                        AlbedoIndexes[AlbedoTexs.IndexOf(TerrainTile.terrainData.terrainLayers[i].diffuseTexture)].RayObjectList.Add(TempObj);
                    }
                    else
                    {
                        AlbedoIndexes.Add(new RayObjects());
                        AlbedoIndexes[AlbedoIndexes.Count - 1].RayObjectList.Add(TempObj);
                        AlbedoTexs.Add(TerrainTile.terrainData.terrainLayers[i].diffuseTexture);
                    }
                }
                if (TerrainTile.terrainData.terrainLayers[i].normalMapTexture != null)
                {
                    if (NormalTexs.Contains(TerrainTile.terrainData.terrainLayers[i].normalMapTexture))
                    {
                        NormalIndexes[NormalTexs.IndexOf(TerrainTile.terrainData.terrainLayers[i].normalMapTexture)].RayObjectList.Add(TempObj);
                    }
                    else
                    {
                        NormalIndexes.Add(new RayObjects());
                        NormalIndexes[NormalIndexes.Count - 1].RayObjectList.Add(TempObj);
                        NormalTexs.Add(TerrainTile.terrainData.terrainLayers[i].normalMapTexture);
                    }
                }

                Materials.Add(new MaterialData()
                {
                    metallic = TerrainTile.terrainData.terrainLayers[i].metallic,
                    Specular = 0,//TerrainTile.terrainData.terrainLayers[i].smoothness,
                    IOR = 1,//TerrainTile.terrainData.terrainLayers[i].smoothness != 0 ? 1.33f : 1,
                    BaseColor = new Vector3(TerrainTile.terrainData.size.x / TerrainTile.terrainData.terrainLayers[i].tileSize.x, TerrainTile.terrainData.size.z / TerrainTile.terrainData.terrainLayers[i].tileSize.y, 0),
                    TransmittanceColor = new Vector3(TerrainTile.terrainData.terrainLayers[i].tileOffset.x / TerrainTile.terrainData.terrainLayers[i].tileSize.x, TerrainTile.terrainData.terrainLayers[i].tileOffset.y / TerrainTile.terrainData.terrainLayers[i].tileSize.y, 0),
                    MatType = 1,
                    AlbedoTextureScale = new Vector4(1,1,0,0),
                });

                MaterialIndex[i] = i;
            }



            Trees = TerrainTile.terrainData.treeInstances;
            TreePrototype[] TreeObjects = TerrainTile.terrainData.treePrototypes;
            InstancedManager Instanced = GameObject.Find("InstancedStorage").GetComponent<InstancedManager>();
            List<GameObject> TreeSources = new List<GameObject>();
            foreach (TreePrototype Tree in TreeObjects)
            {
                if (!Instanced.transform.Find(Tree.prefab.name + "(Clone)"))
                {
                    GameObject TempObject = GameObject.Instantiate(Tree.prefab, Instanced.transform);
                    TempObject.AddComponent<ParentObject>();
                    TempObject.AddComponent<RayTracingObject>();
                    TreeSources.Add(TempObject);
                }
                else
                {
                    TreeSources.Add(Instanced.transform.Find(Tree.prefab.name + "(Clone)").gameObject);
                }
            }






            if (this.gameObject.transform.childCount == 0)
            {
                foreach (var Tree in Trees)
                {
                    GameObject TempGameObject = new GameObject();
                    TempGameObject.transform.parent = this.gameObject.transform;
                    TempGameObject.AddComponent<InstancedObject>();
                    TempGameObject.GetComponent<InstancedObject>().InstanceParent = GameObject.Find(TreeSources[Tree.prototypeIndex].name).GetComponent<ParentObject>();
                    TempGameObject.transform.position = new Vector3(Tree.position.x * TerrainDim, Tree.position.y * HeightScale, Tree.position.z * TerrainDim) + this.transform.position;

                }
            }
        }
    }
}