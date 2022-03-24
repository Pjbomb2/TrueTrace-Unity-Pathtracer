using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml;
 using UnityEditor;
//God im so sorry, this is an absolute mess, but hey it works, barely, but it was my first attempt, will improve later
/*
Breaks if you use bumpmaps, so all bumpmap materials must be rearranged so the bumpmap isnt the root

*/

public class XMLParser : MonoBehaviour {
Vector3[] vertices;

    Vector3 transform_position(Matrix4x4 matrix, Vector3 position) {
        return new Vector3(
            matrix[0, 0] * position.x + matrix[0, 1] * position.y + matrix[0, 2] * position.z + matrix[0, 3],
            matrix[1, 0] * position.x + matrix[1, 1] * position.y + matrix[1, 2] * position.z + matrix[1, 3],
            matrix[2, 0] * position.x + matrix[2, 1] * position.y + matrix[2, 2] * position.z + matrix[2, 3]
        );
    }

     public static Vector3 StringToVector3(string sVector) {
         // Remove the parentheses
         if (sVector.StartsWith ("(") && sVector.EndsWith (")")) {
             sVector = sVector.Substring(1, sVector.Length-2);
         }
 
         // split the items
         string[] sArray = sVector.Split(',');
 
         // store as a Vector3
         if(sArray.Length == 1) {
         Debug.Log(sArray[0]);
     }
         Vector3 result = new Vector3(
             float.Parse(sArray[0]),
             float.Parse(sArray[1]),
             float.Parse(sArray[2]));
 
         return result;
     }

    public XmlDocument xmlDoc;
    public TextAsset textXml;
    public List<BSDFMaterial> Bsdfs;
    public List<string> MaterialMeshTie;
    public List<int> TempMatTie;
    public List<string> MeshLink;
    public List<string> TempMeshLink;

    public void LoadXml(string fileName) {
        MaterialMeshTie = new List<string>();
        Bsdfs = new List<BSDFMaterial>();
        textXml = (TextAsset)AssetDatabase.LoadAssetAtPath(fileName, typeof(TextAsset));
        xmlDoc = new XmlDocument();
        xmlDoc.LoadXml(textXml.ToString());
        Debug.Log(xmlDoc);
        List<string> AvailableData;

        string FolderPathName = fileName.Replace("scene.xml", "");



        foreach(XmlElement BSDFNode in xmlDoc.SelectNodes("scene/bsdf")) {
            AvailableData = new List<string>();
            BSDFMaterial BSDF = new BSDFMaterial();
            XmlNode TextureNode = null;

            List<XmlNode> DataChildNodes = new List<XmlNode>();
            List<XmlNode> FirstDataChildNodes = new List<XmlNode>();
            List<string> AvailableFirstNodes = new List<string>();
            if(BSDFNode.HasChildNodes) {
                foreach(XmlNode BeginNodes in BSDFNode.ChildNodes) {
                    FirstDataChildNodes.Add(BeginNodes);
                    AvailableFirstNodes.Add(BeginNodes.Attributes[0].Value);
                }
                if(BSDFNode.FirstChild.HasChildNodes) {
                    BSDF.ObjName = BSDFNode.GetAttribute("type");
                    BSDF.MatName = BSDFNode.GetAttribute("id");
                    MaterialMeshTie.Add(BSDFNode.GetAttribute("id"));
                    BSDF.MatType = BSDFNode.FirstChild.Attributes[0].Value;
                    foreach(XmlNode InfoNode in BSDFNode.FirstChild.ChildNodes) {
                        DataChildNodes.Add(InfoNode);
                        AvailableData.Add(InfoNode.Attributes[0].Value);
                        if(InfoNode.HasChildNodes) {
                        TextureNode = InfoNode.FirstChild;
                        }
                    }
                } else {
                    BSDF.ObjName = BSDFNode.Attributes[0].Value;
                    BSDF.MatName = BSDFNode.Attributes[1].Value;
                    BSDF.MatType = BSDFNode.Attributes[0].Value;
                    MaterialMeshTie.Add(BSDFNode.Attributes[1].Value);
                    DataChildNodes.AddRange(FirstDataChildNodes);
                    AvailableData.AddRange(AvailableFirstNodes);

                }
            }


    BSDF.MatIndex = 0;

    if(TextureNode != null) {BSDF.HasTexture = true; }else{BSDF.HasTexture = false; }
    if(System.String.Equals(BSDF.MatType, "diffuse")) {
        if(BSDF.HasTexture) {
            string Path = FolderPathName + "/" + TextureNode.Attributes[1].Value;
            BSDF.AlbedoTexture = (Texture2D)AssetDatabase.LoadAssetAtPath(Path, typeof(Texture2D));    
            BSDF.SurfaceColor = new Vector3(0.9f,0.9f,0.9f);    
        } else {
            BSDF.SurfaceColor = StringToVector3(DataChildNodes[DataChildNodes.Count - 1].Attributes[1].Value);
        }
    } else if(System.String.Equals(BSDF.MatType, "roughconductor") || System.String.Equals(BSDF.MatType, "conductor") || System.String.Equals(BSDF.MatType, "coating")) {
        BSDF.MatIndex = 1;
        if(System.String.Equals(BSDF.MatType, "conductor")) {
            BSDF.Linear_Roughness = 0.0f;
        } else {
            if(AvailableData.Contains("alpha")) {
                BSDF.Linear_Roughness = Mathf.Sqrt(float.Parse(DataChildNodes[AvailableData.IndexOf("alpha")].Attributes[1].Value));
            } else {
                BSDF.Linear_Roughness = Mathf.Sqrt(0.25f);
            }
        }

        if(AvailableData.Contains("material")) {
            BSDF.eta = new Vector3(0.0f, 0.0f, 0.0f);
            BSDF.k = new Vector3(1.0f, 1.0f, 1.0f);
        } else {
            BSDF.eta = (AvailableData.Contains("eta")) ? StringToVector3(DataChildNodes[AvailableData.IndexOf("eta")].Attributes[1].Value) : new Vector3(1.33f, 1.33f, 1.33f);
            BSDF.k = (AvailableData.Contains("k")) ? StringToVector3(DataChildNodes[AvailableData.IndexOf("k")].Attributes[1].Value) : new Vector3(1.0f, 1.0f, 1.0f);
        }
        if(System.String.Equals(BSDF.MatType, "coating")) {
            if(AvailableData.Contains("sigmaA")) {
                BSDF.eta = StringToVector3(DataChildNodes[AvailableData.IndexOf("sigmaA")].Attributes[1].Value);
            }
        }

    } else if(System.String.Equals(BSDF.MatType, "thindielectric") || System.String.Equals(BSDF.MatType, "dielectric") || System.String.Equals(BSDF.MatType, "roughdielectric")) {
        BSDF.MatIndex = 2;
        if(AvailableData.Contains("intIOR")) {
            BSDF.int_IOR = (AvailableData.Contains("intIOR")) ? float.Parse(DataChildNodes[AvailableData.IndexOf("intIOR")].Attributes[1].Value) : 1.33f;
        }

        if(System.String.Equals(BSDF.MatType, "roughdielectric")) {
            if(AvailableData.Contains("alpha")) {
                BSDF.Linear_Roughness = Mathf.Sqrt(float.Parse(DataChildNodes[AvailableData.IndexOf("alpha")].Attributes[1].Value));
            } else {
                BSDF.Linear_Roughness = Mathf.Sqrt(0.25f);
            }
        } else {
            BSDF.Linear_Roughness = 0.0f;
        }


    } else if(System.String.Equals(BSDF.MatType, "plastic") || System.String.Equals(BSDF.MatType, "roughplastic") || System.String.Equals(BSDF.MatType, "roughdiffuse")) {
        BSDF.MatIndex = 3;
        if(System.String.Equals(BSDF.MatType, "plastic")) {
            BSDF.Linear_Roughness = 0.0f;   
        } else {
            if(AvailableData.Contains("alpha")) {
                BSDF.Linear_Roughness = Mathf.Sqrt(float.Parse(DataChildNodes[AvailableData.IndexOf("alpha")].Attributes[1].Value));
            } else {
                BSDF.Linear_Roughness = Mathf.Sqrt(0.25f);
            }
        }
        if(BSDF.HasTexture) {
            string Path = FolderPathName + "/" + TextureNode.Attributes[1].Value;
            BSDF.AlbedoTexture = (Texture2D)AssetDatabase.LoadAssetAtPath(Path, typeof(Texture2D));
            BSDF.SurfaceColor = new Vector3(0.9f,0.9f,0.9f);    
        } else {
            if(AvailableData.Contains("diffuseReflectance")) {
                BSDF.SurfaceColor = StringToVector3(DataChildNodes[AvailableData.IndexOf("diffuseReflectance")].Attributes[1].Value);
            } else if(AvailableData.Contains("reflectance")) {
                BSDF.SurfaceColor = StringToVector3(DataChildNodes[AvailableData.IndexOf("reflectance")].Attributes[1].Value);
            } else {
                BSDF.SurfaceColor = new Vector3(0.9f, 0.9f, 0.9f);
            }
        }
        if(AvailableData.Contains("intIOR")) {  
            BSDF.int_IOR = float.Parse(DataChildNodes[AvailableData.IndexOf("intIOR")].Attributes[1].Value);
        }

    } else {
        BSDF.SurfaceColor = new Vector3(0.9f,0.9f,1.0f);
    }
            
      
            

            Bsdfs.Add(BSDF);
        }

vertices = new Vector3[8];
            TempMatTie = new List<int>();
            List<int> TempTempMatTie = new List<int>();
        MeshLink = new List<string>();
        TempMeshLink = new List<string>();
        List<BSDFMaterial> TempBsdfs = new List<BSDFMaterial>();
        GameObject SourceCube = GameObject.Find("DefaultCube");
        GameObject SourceSphere = GameObject.Find("DefaultSphere");
        GameObject SourceRectangle = GameObject.Find("DefaultRectangle");
        int iterator = 0;
        foreach(XmlElement ObjectNode in xmlDoc.SelectNodes("scene/shape")) {
                    string type = ObjectNode.GetAttribute("type");
                                if(type.Equals("cube") || type.Equals("rectangle")) {
                                    GameObject TargChild = (type.Equals("cube")) ? Object.Instantiate(SourceCube, this.gameObject.transform) : Object.Instantiate(SourceRectangle, this.gameObject.transform);
                                    TargChild.name += iterator;
                                    

                                    BSDFMaterial CurrentBsdf = new BSDFMaterial();
                                    if(ObjectNode["ref"] != null) {
                                        CurrentBsdf = Bsdfs[MaterialMeshTie.IndexOf(ObjectNode["ref"].Attributes[0].Value)];
                                    } else {
                                        if(ObjectNode["bsdf"].FirstChild.Attributes[0].Value.Equals("diffuse")) {
                                            CurrentBsdf.SurfaceColor = StringToVector3(ObjectNode["bsdf"].FirstChild.FirstChild.Attributes[1].Value);
                                        }
                                    }
                                    if(ObjectNode["emitter"] != null) {
                                        CurrentBsdf.SurfaceColor = StringToVector3(ObjectNode["emitter"]["rgb"].Attributes[1].Value);
                                        CurrentBsdf.emissive = 1.0f;
                                    }
                                    if(ObjectNode["ref"] != null) {
                                        if(CurrentBsdf.HasBeenModified) {
                                            BSDFMaterial TempTempBsdf = Bsdfs[MaterialMeshTie.IndexOf(ObjectNode["ref"].Attributes[0].Value)];
                                            TempTempBsdf.HasBeenModified = true;
                                            CurrentBsdf.MatName = TargChild.name;
                                            Bsdfs[MaterialMeshTie.IndexOf(ObjectNode["ref"].Attributes[0].Value)] = TempTempBsdf;
                                            TempBsdfs.Add(CurrentBsdf);
                                        } else {
                                            Bsdfs[MaterialMeshTie.IndexOf(ObjectNode["ref"].Attributes[0].Value)] = CurrentBsdf;
                                        }
                                    } else {
                                        CurrentBsdf.MatName = TargChild.name;
                                        TempBsdfs.Add(CurrentBsdf);

                                        }

                                    
                                    string TransformType = ObjectNode["transform"].Attributes[0].Value;
                                    if(TransformType.Equals("toWorld")) {
                                        TransformType = ObjectNode["transform"]["matrix"].Attributes[0].Value;
                                        string[] textSplit = TransformType.Split(" "[0]);
                                        Matrix4x4 Transform = Matrix4x4.zero;
                                        for(int i = 0; i < 4; i++) {
                                            Transform.SetRow(i, new Vector4(
                                                float.Parse(textSplit[0 + i * 4]),
                                                float.Parse(textSplit[1 + i * 4]),
                                                float.Parse(textSplit[2 + i * 4]),
                                                float.Parse(textSplit[3 + i * 4])
                                                ));
                                        }
                                        Vector3 RotationTransform = Transform.rotation.eulerAngles;
                                        RotationTransform.x += 180;
                                        TargChild.transform.Rotate(RotationTransform);
                                        TargChild.transform.localPosition = new Vector3(-Transform.m03, Transform.m13, Transform.m23);
                                        TargChild.transform.localScale = new Vector3( Transform.GetColumn( 0 ).magnitude, Transform.GetColumn( 1 ).magnitude, Transform.GetColumn( 2 ).magnitude ) * 2.0f;// -Transform.lossyScale;
                                    }
                                    TempMeshLink.Add(TargChild.name);

                                    
                                }else if(type.Equals("sphere")) {
                                    GameObject TargChild = Object.Instantiate(SourceSphere, this.gameObject.transform);
                                    TargChild.name += iterator;
                                    BSDFMaterial CurrentBsdf = new BSDFMaterial();
                                    if(ObjectNode["ref"] != null) {
                                        CurrentBsdf = Bsdfs[MaterialMeshTie.IndexOf(ObjectNode["ref"].Attributes[0].Value)];
                                    } else {
                                        if(ObjectNode["bsdf"].FirstChild.Attributes[0].Value.Equals("diffuse")) {
                                            CurrentBsdf.SurfaceColor = StringToVector3(ObjectNode["bsdf"].FirstChild.FirstChild.Attributes[1].Value);
                                        }
                                    }
                                    if(ObjectNode["emitter"] != null) {
                                        CurrentBsdf.SurfaceColor = StringToVector3(ObjectNode["emitter"]["rgb"].Attributes[1].Value);
                                        CurrentBsdf.emissive = 1.0f;
                                    }
                                    if(ObjectNode["ref"] != null) {
                                        if(CurrentBsdf.HasBeenModified) {
                                            BSDFMaterial TempTempBsdf = Bsdfs[MaterialMeshTie.IndexOf(ObjectNode["ref"].Attributes[0].Value)];
                                            TempTempBsdf.HasBeenModified = true;
                                            CurrentBsdf.MatName = TargChild.name;
                                            Bsdfs[MaterialMeshTie.IndexOf(ObjectNode["ref"].Attributes[0].Value)] = TempTempBsdf;
                                            TempBsdfs.Add(CurrentBsdf);
                                        } else {
                                            Bsdfs[MaterialMeshTie.IndexOf(ObjectNode["ref"].Attributes[0].Value)] = CurrentBsdf;
                                        }
                                    } else {
                                        CurrentBsdf.MatName = TargChild.name;
                                        TempBsdfs.Add(CurrentBsdf);

                                        }
                                    float Radius = float.Parse(ObjectNode.ChildNodes[0].Attributes[1].Value) * 2.0f;
                                    Vector3 Center = new Vector3(float.Parse(ObjectNode.ChildNodes[1].Attributes[1].Value), float.Parse(ObjectNode.ChildNodes[1].Attributes[2].Value), float.Parse(ObjectNode.ChildNodes[1].Attributes[3].Value));
                                    TargChild.transform.localScale = new Vector3(Radius, Radius, Radius);
                                    TargChild.transform.localPosition = Center;
                                    TempMeshLink.Add(TargChild.name);
                                }
                                iterator++;

        }



        XmlNodeList elemList = xmlDoc.GetElementsByTagName("ref");
        for(int i = 0; i < elemList.Count; i++) {
            if(elemList[i].ParentNode.Attributes[0].Value.Equals("obj")) {
                if(MaterialMeshTie.Contains(elemList[i].Attributes[0].Value)) {
                        string Path = elemList[i].ParentNode.FirstChild.Attributes[1].Value;
                        Path = Path.Substring(0, Path.Length - 4);
                        Path = Path.Replace("models/", "");
                        Path = Path + "(Clone)";
                        MeshLink.Add(Path);
                        TempMatTie.Add(MaterialMeshTie.IndexOf(elemList[i].Attributes[0].Value));
                }
            } else {
                TempMatTie.Add(MaterialMeshTie.IndexOf(elemList[i].Attributes[0].Value));


            }

        }
        for(int i = 0; i < TempBsdfs.Count; i++) {
            TempMatTie.Add(Bsdfs.Count + i);
        }
        MaterialMeshTie.AddRange(TempMeshLink);
        Bsdfs.AddRange(TempBsdfs);
        MeshLink.AddRange(TempMeshLink);
        GameObject Root = gameObject;
        GameObject TargetObject;
        for(int i = 0; i < transform.childCount; i++) {
        if(transform.GetChild(i).gameObject.transform.childCount != 0) {
            for(int i2 = 0; i2 < transform.GetChild(i).gameObject.transform.childCount; i2++) {
                TargetObject =  transform.GetChild(i).gameObject.transform.GetChild(i2).gameObject;
                if(TargetObject.GetComponent<MeshFilter>() != null && TargetObject.GetComponent<MeshRenderer>() != null) {
                        BSDFMaterial TargetBSDF = Bsdfs[TempMatTie[MeshLink.IndexOf(transform.GetChild(i).gameObject.name)]];
                        if(System.String.Equals(TargetObject.GetComponent<MeshRenderer>().sharedMaterial.name, "defaultMat")) {
                            Material NewMaterial = new Material(TargetObject.GetComponent<MeshRenderer>().sharedMaterial);
                            if(MeshLink.Contains(transform.GetChild(i).gameObject.name)) {
                                NewMaterial.mainTexture = TargetBSDF.AlbedoTexture;
                                Vector3 TempCol = TargetBSDF.SurfaceColor;
                                if(TargetBSDF.MatIndex == 1 || TargetBSDF.MatIndex == 2) {
                                    TempCol = TargetBSDF.k;
                                    if(TempCol.x > 1.0f || TempCol.y > 1.0f || TempCol.z > 1.0f) {
                                        TempCol = Vector3.Normalize(TempCol);
                                    }
                                }
                                NewMaterial.color = new Color(TempCol.x, TempCol.y, TempCol.z, 1.0f);
                            }
                            TargetObject.GetComponent<MeshRenderer>().material = NewMaterial;
                        }                
                    if(TargetObject.GetComponent<RayTracingObject>() == null) {
                        TargetObject.AddComponent(typeof(RayTracingObject));
                    }
                    RayTracingObject TargetRayTracingObject = TargetObject.GetComponent<RayTracingObject>();
                    TargetRayTracingObject.MatType[0] = TargetBSDF.MatIndex;
                    TargetRayTracingObject.Roughness[0] = TargetBSDF.Linear_Roughness;
                    if(TargetBSDF.MatIndex == 2) {
                        TargetRayTracingObject.eta[0] = new Vector3(TargetBSDF.int_IOR, 0.0f, 0.0f);
                        TargetRayTracingObject.BaseColor[0] = TargetBSDF.k;
                    } else if(TargetBSDF.MatIndex == 1) {
                        if(TargetRayTracingObject.Roughness[0] == 0.0f) TargetRayTracingObject.Roughness[0] = 0.0001f;
                        TargetRayTracingObject.BaseColor[0] = TargetBSDF.k;
                        TargetRayTracingObject.eta[0] = TargetBSDF.eta;
                    } else {
                        TargetRayTracingObject.BaseColor[0] = TargetBSDF.SurfaceColor;
                        TargetRayTracingObject.eta[0] = TargetBSDF.eta;
                    }

                } else {Debug.Log("TARGETING INVALID OBJECT");}
            }

        }else {
                TargetObject =  transform.GetChild(i).gameObject;
                if(TargetObject.GetComponent<MeshFilter>() != null && TargetObject.GetComponent<MeshRenderer>() != null) {
                        BSDFMaterial TargetBSDF = Bsdfs[TempMatTie[MeshLink.IndexOf(transform.GetChild(i).gameObject.name)]];
                        if(System.String.Equals(TargetObject.GetComponent<MeshRenderer>().sharedMaterial.name, "defaultMat")) {
                            Material NewMaterial = new Material(TargetObject.GetComponent<MeshRenderer>().sharedMaterial);
                            if(MeshLink.Contains(transform.GetChild(i).gameObject.name)) {
                                NewMaterial.mainTexture = TargetBSDF.AlbedoTexture;
                                Vector3 TempCol = TargetBSDF.SurfaceColor;
                                if(TargetBSDF.MatIndex == 1 || TargetBSDF.MatIndex == 2) {
                                    TempCol = TargetBSDF.k;
                                    if(TempCol.x > 1.0f || TempCol.y > 1.0f || TempCol.z > 1.0f) {
                                        TempCol = Vector3.Normalize(TempCol);
                                    }
                                }
                                NewMaterial.color = new Color(TempCol.x, TempCol.y, TempCol.z, 1.0f);
                            }
                            TargetObject.GetComponent<MeshRenderer>().material = NewMaterial;
                        }                
                    if(TargetObject.GetComponent<RayTracingObject>() == null) {
                        TargetObject.AddComponent(typeof(RayTracingObject));
                    }
                    RayTracingObject TargetRayTracingObject = TargetObject.GetComponent<RayTracingObject>();
                    TargetRayTracingObject.MatType[0] = TargetBSDF.MatIndex;
                    TargetRayTracingObject.Roughness[0] = TargetBSDF.Linear_Roughness;
                    if(TargetBSDF.MatIndex == 2) {
                        TargetRayTracingObject.eta[0] = new Vector3(TargetBSDF.int_IOR, 0.0f, 0.0f);
                        TargetRayTracingObject.BaseColor[0] = TargetBSDF.k;
                    } else if(TargetBSDF.MatIndex == 1) {
                        if(TargetRayTracingObject.Roughness[0] == 0.0f) TargetRayTracingObject.Roughness[0] = 0.0001f;
                        TargetRayTracingObject.BaseColor[0] = TargetBSDF.k;
                        TargetRayTracingObject.eta[0] = TargetBSDF.eta;
                    } else {
                        TargetRayTracingObject.BaseColor[0] = TargetBSDF.SurfaceColor;
                        TargetRayTracingObject.eta[0] = TargetBSDF.eta;
                    }
                    TargetRayTracingObject.emmission[0] = TargetBSDF.emissive;

                } else {Debug.Log("TARGETING INVALID OBJECT");}
            

        } 

        } 
        Debug.Log("CHILD COUNT: " + transform.childCount);
    }


   /* void OnDrawGizmos() {
        for(int i = 0; i < 8; i++) {
            //Gizmos.color = new Color(i-1, i-2, i-3, 0.5f);
            
            Gizmos.DrawSphere(vertices[i], 0.1f);
        }
    }*/

}
