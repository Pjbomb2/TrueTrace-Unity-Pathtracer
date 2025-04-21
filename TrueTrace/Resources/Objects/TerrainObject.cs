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
    [RequireComponent(typeof(Terrain))]
    public class TerrainObject : MonoBehaviour
    {
        Terrain TerrainTile;
        public Texture2D HeightMap;
        public Texture2D AlphaMap;
        public TreeInstance[] Trees;
        public DetailedObjectInstance[] Details;
        // Start is called before the first frame update
        public List<MaterialData> Materials;
        public List<Texture> MaskTexs;
        public List<Texture> AlbedoTexs;
        public List<Texture> NormalTexs;

        public int TerrainDimX;
        public int TerrainDimY;
        public float HeightScale;

        public void ClearAll()
        {
            Trees = null;
            TerrainTile = null;
        }
        private void TextureParse(ref List<Texture> Texs, ref int TextureIndex, Texture Tex) {
            TextureIndex = 0;
            if (Tex != null) {
                TextureIndex = Texs.IndexOf(Tex) + 1;
                if (TextureIndex != 0) {
                    return;
                } else {
                    Texs.Add(Tex);
                    TextureIndex = Texs.Count;
                    return;
                }
            }
            return;
        }

        public void Load()
        {
            TerrainTile = this.gameObject.GetComponent<Terrain>();
            if(TerrainTile.terrainData == null) {
                DestroyImmediate(this);
                return;
            }
            HeightMap = new Texture2D(TerrainTile.terrainData.heightmapResolution, TerrainTile.terrainData.heightmapResolution, UnityEngine.Experimental.Rendering.GraphicsFormat.R16_UNorm, UnityEngine.Experimental.Rendering.TextureCreationFlags.None);

            Graphics.ConvertTexture(TerrainTile.terrainData.heightmapTexture, HeightMap);
            HeightMap.ReadPixels(new Rect(0, 0, HeightMap.width, HeightMap.height), 0, 0, true);
            HeightMap.Apply();//ReadPixels(new Rect(0,0,1,1)
            AlphaMap = (TerrainTile != null && TerrainTile.terrainData != null && TerrainTile.terrainData.alphamapTextures != null && TerrainTile.terrainData.alphamapTextures.Length != 0) ? TerrainTile.terrainData.alphamapTextures[0] : Texture2D.whiteTexture;//new RenderTexture(TerrainTile.terrainData.alphamapResolution, TerrainTile.terrainData.alphamapResolution, 0, TerrainTile.terrainData.alphamapTextures[0].format, RenderTextureReadWrite.Linear);

            HeightScale = TerrainTile.terrainData.heightmapScale.y;
            TerrainDimX = (int)TerrainTile.terrainData.size.x;
            TerrainDimY = (int)TerrainTile.terrainData.size.z;

            AlbedoTexs = new List<Texture>();
            NormalTexs = new List<Texture>();
            MaskTexs = new List<Texture>();
            Materials = new List<MaterialData>();

            int TerrainLayerCount = TerrainTile.terrainData.terrainLayers.Length;
            for (int i = 0; i < TerrainLayerCount; i++)
            {
                MaterialData MatDat = new MaterialData();
                int TempIndex = 0;
                TextureParse(ref AlbedoTexs, ref TempIndex, TerrainTile.terrainData.terrainLayers[i].diffuseTexture); MatDat.Textures.AlbedoTex.x = TempIndex;
                TextureParse(ref NormalTexs, ref TempIndex, TerrainTile.terrainData.terrainLayers[i].normalMapTexture); MatDat.Textures.NormalTex.x = TempIndex;
                TextureParse(ref MaskTexs, ref TempIndex, TerrainTile.terrainData.terrainLayers[i].maskMapTexture); MatDat.Textures.MetallicTex.x = TempIndex;
                TextureParse(ref MaskTexs, ref TempIndex, TerrainTile.terrainData.terrainLayers[i].maskMapTexture); MatDat.Textures.RoughnessTex.x = TempIndex;

                MatDat.MatData.Metallic = TerrainTile.terrainData.terrainLayers[i].metallic;
                MatDat.MatData.Specular = 0;//TerrainTile.terrainData.terrainLayers[i].smoothness,
                MatDat.MatData.IOR = 1;//TerrainTile.terrainData.terrainLayers[i].smoothness != 0 ? 1.33f : 1,
                MatDat.MatData.BaseColor = new Vector3(TerrainTile.terrainData.size.x / TerrainTile.terrainData.terrainLayers[i].tileSize.x, TerrainTile.terrainData.size.z / TerrainTile.terrainData.terrainLayers[i].tileSize.y, 0);
                MatDat.MatData.TransmittanceColor = new Vector3(TerrainTile.terrainData.terrainLayers[i].tileOffset.x / TerrainTile.terrainData.terrainLayers[i].tileSize.x, TerrainTile.terrainData.terrainLayers[i].tileOffset.y / TerrainTile.terrainData.terrainLayers[i].tileSize.y, 0);
                MatDat.MatData.MatType = 1;
                MatDat.MatData.Tag.SetFlag(CommonFunctions.Flags.UseSmoothness, true);
                MatDat.MatData.TextureModifiers.MainTexScaleOffset = new Vector4(1,1,0,0);
                Materials.Add(MatDat);
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
                    TempGameObject.transform.position = new Vector3(Tree.position.x * TerrainDimX, Tree.position.y * HeightScale, Tree.position.z * TerrainDimY) + this.transform.position;
                    TempGameObject.transform.localScale = new Vector3(Tree.widthScale, Tree.heightScale, Tree.widthScale);

                }
            }
        }
    }
}