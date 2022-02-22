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

    TempMatTie = new List<int>();
        MeshLink = new List<string>();
        XmlNodeList elemList = xmlDoc.GetElementsByTagName("ref");
        for(int i = 0; i < elemList.Count; i++) {
            if(MaterialMeshTie.Contains(elemList[i].Attributes[0].Value)) {
                string Path = elemList[i].ParentNode.FirstChild.Attributes[1].Value;
                Path = Path.Substring(0, Path.Length - 4);
                Path = Path.Replace("models/", "");
                Path = Path + "(Clone)";
                MeshLink.Add(Path);
                TempMatTie.Add(MaterialMeshTie.IndexOf(elemList[i].Attributes[0].Value));
            }
        }
        
        GameObject Root = gameObject;
        GameObject TargetObject;
        for(int i = 0; i < transform.childCount; i++) {
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
                        TargetRayTracingObject.BaseColor[0] = TargetBSDF.k;
                        TargetRayTracingObject.eta[0] = TargetBSDF.eta;
                    } else {
                        TargetRayTracingObject.BaseColor[0] = TargetBSDF.SurfaceColor;
                        TargetRayTracingObject.eta[0] = TargetBSDF.eta;
                    }

                } else {Debug.Log("TARGETING INVALID OBJECT");}
            }

        } 
        Debug.Log("CHILD COUNT: " + transform.childCount);
    }




}
