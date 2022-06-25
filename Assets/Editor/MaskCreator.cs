using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class MaskCreator : EditorWindow {
    [MenuItem("Mask Creator/Mask Creator Tool")]
    public static void ShowWindow() {
         GetWindow<MaskCreator>("Mask Creator");
    }

    private float luminance(float r, float g, float b) {
        return 0.299f * r + 0.587f * g + 0.114f * b;
    }

    public Texture2D InputTexture;
    public Texture2D OutputMask;
    public Texture2D UpperColorTex;
    public Texture2D LowerColorTex;
    public Texture2D BackgroundColorTex;
    public Texture2D DeletionZone;
    public Color CurrentSampledColor;
    private Vector2 MouseLocation;
    private Vector2 MouseLocation2;
    private bool HasInputTexture;

    private int ColorSelector = -1;

    private float UpperColor;
    private float LowerColor;
    private float BackgroundColor;
    private float Sharpness;
    private Vector2 MouseDownLocation;
    private Vector2 MouseDragLocation;

    private List<PixelInfo> PixelColors;
    private bool ShowDeletionZone;

    private bool UseGradient;


    public struct PixelInfo {
        public int X;
        public int Y;
    }

    private bool InsideRect(Vector2 MousePosition, Rect SampleRect) {
        if(MousePosition.x >= SampleRect.x && MousePosition.y >= SampleRect.y && MousePosition.x <= SampleRect.x + SampleRect.width && MousePosition.y <= SampleRect.y + SampleRect.height) return true;
        return false;
    }
    [System.Serializable]
    public struct TextureData {
        public Vector2 DisplayOffset;
        public float DisplayScale;
        public Rect MainRect;
        public Rect ZoomRect;
    }
    public TextureData MaskTex;
    public TextureData MainTex;

    public void ScaleTexture(ref TextureData ThisTex, float scaleFactor, Vector2 mouseTexturePos) {
        ThisTex.DisplayOffset = ThisTex.DisplayOffset - (scaleFactor-1.0f) * mouseTexturePos * ThisTex.DisplayScale;
        ThisTex.DisplayScale = Mathf.Min(ThisTex.DisplayScale * scaleFactor, 1.0f);
        Vector2 OffsetCoords = ThisTex.DisplayOffset + (ThisTex.DisplayScale * ThisTex.MainRect.size) / ThisTex.MainRect.size;
        Vector2 ChangeCoords = new Vector2(0,0);
        if(OffsetCoords.x > 1) ChangeCoords.x = 1 - OffsetCoords.x;
        if(OffsetCoords.y > 1) ChangeCoords.y = 1 - OffsetCoords.y;
        ThisTex.DisplayOffset += ChangeCoords;
        ThisTex.DisplayOffset = Vector2.Max(Vector2.Min(ThisTex.DisplayOffset, new Vector2(1,1)),new Vector2(0,0));
        ThisTex.ZoomRect.position = ThisTex.DisplayOffset;
        ThisTex.ZoomRect.size = (ThisTex.DisplayScale * ThisTex.MainRect.size)/ ThisTex.MainRect.size;
    }

    private void OnGUI() {
        Event e = Event.current;
        Rect UpperBoundColor = new Rect(10,40, 60, 60);
        Rect LowerBoundColor = new Rect(80,40, 60, 60);
        Rect BackgroundBoundColor = new Rect(150,40, 60, 60);
        Rect SharpnessSlider = new Rect(220, 55, position.width / 3, 15);
        Rect OutputToFileDisp = new Rect(10 + (position.width) / 2 - 50, 120 + (position.width) / 2, 100, 15);
        InputTexture = (Texture2D) EditorGUILayout.ObjectField("Image", InputTexture, typeof (Texture2D), false);
        Rect DeletionZoneArea = new Rect(0,0,25,25);
        Rect UpperBoundColorLabel = new Rect(21, 20, 30, 15);
        Rect LowerBoundColorLabel = new Rect(91, 20, 30, 15);
        Rect BackgroundColorLabel = new Rect(141, 20, 80, 15);
        Rect SharpnessLabel = new Rect(190 + position.width / 6, 35, position.width / 3, 15);
        Rect GradientBool = new Rect(260 + position.width / 3, 55, 110, 15);

        if(InputTexture) {
            Sharpness = GUI.HorizontalSlider(SharpnessSlider, Sharpness, 0.0f, 0.2f);
            if(!HasInputTexture) {
                UpperColorTex = new Texture2D(1,1);
                UpperColorTex.SetPixel(0,0, new Color(0,0,0,1));
                LowerColorTex = new Texture2D(1,1);
                LowerColorTex.SetPixel(0,0, new Color(0,0,0,1));
                BackgroundColorTex = new Texture2D(1,1);
                BackgroundColorTex.SetPixel(0,0, new Color(0,0,0,1));
                UpperColorTex.Apply();
                LowerColorTex.Apply();
                BackgroundColorTex.Apply();
                ColorSelector = -1;
                Sharpness = 0.1f;
                DeletionZone = new Texture2D(1,1);
                DeletionZone.SetPixel(0,0, new Color(1,0,0,1));
                DeletionZone.Apply();
                ShowDeletionZone = false;
                UseGradient = false;
                MainTex = new TextureData();
                MainTex.DisplayOffset = new Vector2(0,0);
                MainTex.DisplayScale = 1;
                MainTex.ZoomRect = new Rect(0,0,1,1);
                MainTex.MainRect = new Rect(10,110, (position.width) / 2, (position.width) / 2);
                MaskTex = new TextureData();
                MaskTex.DisplayOffset = new Vector2(0,0);
                MaskTex.DisplayScale = 1;
                MaskTex.ZoomRect = new Rect(0,0,1,1);
                MaskTex.MainRect = new Rect(10 + (position.width) / 2 ,110, (position.width) / 2, (position.width) / 2);
            }
            GUI.Label(UpperBoundColorLabel, "Max");
            GUI.Label(LowerBoundColorLabel, "Min");
            GUI.Label(BackgroundColorLabel, "Background");
            GUI.Label(SharpnessLabel, "Sharpness");
            UseGradient = GUI.Toggle(GradientBool, UseGradient, "Allow Gradient");
            GUI.Label(new Rect((position.width) / 4 - 10, 95, 100, 15), "Input Image");
            if(e.type == EventType.ScrollWheel) {
                if(InsideRect(e.mousePosition, MainTex.MainRect)) {
                    float scaleFactor = 0.95f;
                    if(e.delta.y < 0) {
                        scaleFactor = 1.0f/scaleFactor;
                    }
                    Vector2 mouseTexturePos = (e.mousePosition - MainTex.MainRect.position) / MainTex.MainRect.size;
                    mouseTexturePos.y = 1.0f - mouseTexturePos.y;
                    ScaleTexture(ref MainTex, scaleFactor, mouseTexturePos);
                } else if(InsideRect(e.mousePosition, MaskTex.MainRect)) {
                    float scaleFactor = 0.95f;
                    if(e.delta.y < 0) {
                        scaleFactor = 1.0f/scaleFactor;
                    }
                    Vector2 mouseTexturePos = (e.mousePosition - MaskTex.MainRect.position) / MaskTex.MainRect.size;
                    mouseTexturePos.y = 1.0f - mouseTexturePos.y;
                    ScaleTexture(ref MaskTex, scaleFactor, mouseTexturePos);
                }
            }
            GUI.DrawTextureWithTexCoords(MainTex.MainRect, InputTexture, MainTex.ZoomRect);
                if(e.type == EventType.MouseDown || e.type == EventType.MouseDrag) {

                        MouseLocation = (e.mousePosition -  MainTex.MainRect.position) / MainTex.MainRect.size;
                        if((MouseLocation.x <= 1.0f && MouseLocation.y <= 1.0f && MouseLocation.x >= 0.0f && MouseLocation.y >= 0.0f)) {
                            MouseLocation.y = 1.0f - MouseLocation.y;
                            MouseLocation = (MouseLocation) * ((MainTex.ZoomRect.size + MainTex.ZoomRect.position) - MainTex.ZoomRect.position) + MainTex.ZoomRect.position;
                            MouseLocation *= new Vector2(InputTexture.width, InputTexture.height);
                            CurrentSampledColor = InputTexture.GetPixel((int)(MouseLocation.x), (int)((MouseLocation.y)));
                            float col = luminance(CurrentSampledColor.r, CurrentSampledColor.g, CurrentSampledColor.b);
                            switch(ColorSelector) {
                                case 0:
                                    UpperColor = col;
                                    UpperColorTex.SetPixel(0,0, CurrentSampledColor);
                                    UpperColorTex.Apply();
                                break;
                                case 1:
                                    LowerColor = col;
                                    LowerColorTex.SetPixel(0,0, CurrentSampledColor);
                                    LowerColorTex.Apply();
                                break;
                                case 2:
                                    BackgroundColor = col;
                                    BackgroundColorTex.SetPixel(0,0, CurrentSampledColor);
                                    BackgroundColorTex.Apply();
                                break;
                                case -1:
                                    CreateNewMask();
                                break;
                                default:
                                    Debug.Log("SOMETHING BROKE");
                                break;
                            }
                        }
                    
                }
                if(e.type == EventType.MouseDown && InsideRect(e.mousePosition, MaskTex.MainRect) && !ShowDeletionZone) {
                    MouseDownLocation = e.mousePosition;
                }
                if(e.type == EventType.MouseDrag && InsideRect(e.mousePosition, MaskTex.MainRect)) {
                    ShowDeletionZone = true;
                    MouseDragLocation = e.mousePosition;
                }

                if(e.type == EventType.MouseUp) {
                    if(InsideRect(e.mousePosition, UpperBoundColor)) {
                        ColorSelector = 0;
                    } else if(InsideRect(e.mousePosition, LowerBoundColor)) {
                        ColorSelector = 1;
                    } else if(InsideRect(e.mousePosition, BackgroundBoundColor)) {
                        ColorSelector = 2;
                    } else if(ShowDeletionZone) {
                        CoverMask(new Rect(MouseDownLocation, (MouseDragLocation - MouseDownLocation)));
                    } else if(InsideRect(e.mousePosition, MaskTex.MainRect)) {
                        MouseLocation2 = (e.mousePosition - MaskTex.MainRect.position) / MaskTex.MainRect.size;
                        MouseLocation2.y = 1.0f - MouseLocation2.y;
                        MouseLocation2 = (MouseLocation2) * ((MaskTex.ZoomRect.size + MaskTex.ZoomRect.position) - MaskTex.ZoomRect.position) + MaskTex.ZoomRect.position;
                        MouseLocation2 *= new Vector2(InputTexture.width, InputTexture.height);
                        ColorSelector = 3;
                        PixelColors = new List<PixelInfo>();
                        FloodFill((int)(MouseLocation2.x), (int)(MouseLocation2.y));
                        OutputMask.Apply();
                    } else {
                        ColorSelector = -1;
                    }
                    ShowDeletionZone = false;
                }

            if(OutputMask) {
                GUI.Label(new Rect((position.width) / 1.4f - 10, 95, 100, 15), "Output Mask");
                GUI.DrawTextureWithTexCoords(MaskTex.MainRect, OutputMask, MaskTex.ZoomRect);
                if (GUI.Button(OutputToFileDisp, "Output To File")) {
                    byte[] bytes = OutputMask.EncodeToPNG();
                    var dirPath = Application.dataPath + "/../Assets/";
                    if(!System.IO.Directory.Exists(dirPath)) {
                        Debug.Log("No Valid Folder");
                    } else {
                        System.IO.File.WriteAllBytes(dirPath + "Mask" + ".psd", bytes);
                    }
                }   
            }
            if(ShowDeletionZone) {
                EditorGUI.DrawPreviewTexture(new Rect(MouseDownLocation, (MouseDragLocation - MouseDownLocation)), DeletionZone, null, ScaleMode.StretchToFill);
            }
            EditorGUI.DrawPreviewTexture(UpperBoundColor, UpperColorTex, null, ScaleMode.StretchToFill);
            EditorGUI.DrawPreviewTexture(LowerBoundColor, LowerColorTex, null, ScaleMode.StretchToFill);
            EditorGUI.DrawPreviewTexture(BackgroundBoundColor, BackgroundColorTex, null, ScaleMode.StretchToFill);
            HasInputTexture = true;
        } else {
            HasInputTexture = false;
        }
    }

    private Color FindClosest(PixelInfo Point, PixelInfo[] B, ref float Distance, Color[] TextureData) {
        PixelInfo Closest = B[0];
        Vector2 PointVector = new Vector2(Point.X, Point.Y);
        Vector2 Diff;
        float AvgDist = 0.0f;
        Color AvgCol = new Color(0,0,0,1);
        for(int i = 0; i < B.Length; i++) {
            Diff = PointVector - new Vector2(B[i].X, B[i].Y);
            AvgDist += Vector2.Dot(Diff, Diff);
            if(Vector2.Dot(Diff, Diff) < Distance) {
                Closest = B[i];
                Distance = Vector2.Dot(Diff, Diff);
            }
        }
        int width = OutputMask.width;
        for(int i = 0; i < B.Length; i++) {
            Diff = PointVector - new Vector2(B[i].X, B[i].Y);
            AvgCol += TextureData[B[i].X + B[i].Y * width] * Mathf.Pow(1.0f / (Vector2.Dot(Diff, Diff) / (AvgDist)),40);
        }

        Distance /= (AvgDist / B.Length);
        return AvgCol / B.Length;
    }
    private void FloodFill(int Cx, int Cy) {
        int recursions = 0;
        List<PixelInfo> ToCheck = new List<PixelInfo>();
        int width = OutputMask.width;
        Color[] TextureData = OutputMask.GetPixels(0);
        Color WhiteColor = new Color(1,1,2,1);
        PixelInfo ThisPixel = new PixelInfo();
        ThisPixel.X = Cx;
        ThisPixel.Y = Cy;
        ToCheck.Add(ThisPixel);
        int size = 1;
        List<PixelInfo> Border = new List<PixelInfo>();
        List<PixelInfo> Filled = new List<PixelInfo>();
        while(size > 0 && recursions < 100000) {
            recursions++;
            PixelInfo CheckingPixel = ToCheck[size - 1];
            TextureData[CheckingPixel.X + CheckingPixel.Y * width] = WhiteColor;
            ToCheck.RemoveAt(size - 1);
            size--;
            if(TextureData[CheckingPixel.X + 1 + CheckingPixel.Y * width].r < Sharpness) {
                ThisPixel.X = CheckingPixel.X + 1;
                ThisPixel.Y = CheckingPixel.Y;
                ToCheck.Add(ThisPixel);
                Filled.Add(ThisPixel);
                size++;
            } else {
                ThisPixel.X = CheckingPixel.X + 1;
                ThisPixel.Y = CheckingPixel.Y;
                if(TextureData[ThisPixel.X + ThisPixel.Y * width] != new Color(1,1,2,1)) {
                    Border.Add(ThisPixel);
                }
            }
            if(TextureData[CheckingPixel.X + (CheckingPixel.Y + 1) * width].r < Sharpness) {
                ThisPixel.X = CheckingPixel.X;
                ThisPixel.Y = CheckingPixel.Y + 1;
                ToCheck.Add(ThisPixel);
                Filled.Add(ThisPixel);
                size++;
            } else {
                ThisPixel.X = CheckingPixel.X;
                ThisPixel.Y = CheckingPixel.Y + 1;
                if(TextureData[ThisPixel.X + ThisPixel.Y * width] != new Color(1,1,2,1)) {
                    Border.Add(ThisPixel);
                }
            }
            if(TextureData[CheckingPixel.X - 1 + CheckingPixel.Y * width].r < Sharpness) {
                ThisPixel.X = CheckingPixel.X - 1;
                ThisPixel.Y = CheckingPixel.Y;
                ToCheck.Add(ThisPixel);
                Filled.Add(ThisPixel);
                size++;
            } else {
                ThisPixel.X = CheckingPixel.X - 1;
                ThisPixel.Y = CheckingPixel.Y;
                if(TextureData[ThisPixel.X + ThisPixel.Y * width] != new Color(1,1,2,1)) {
                    Border.Add(ThisPixel);
                }
            }
            if(TextureData[CheckingPixel.X + (CheckingPixel.Y - 1) * width].r < Sharpness) {
                ThisPixel.X = CheckingPixel.X;
                ThisPixel.Y = CheckingPixel.Y - 1;
                ToCheck.Add(ThisPixel);
                Filled.Add(ThisPixel);
                size++;
            } else {
                ThisPixel.X = CheckingPixel.X;
                ThisPixel.Y = CheckingPixel.Y - 1;
                if(TextureData[ThisPixel.X + ThisPixel.Y * width] != new Color(1,1,2,1)) {
                    Border.Add(ThisPixel);
                }
            }
        }
        Color TrueWhite = new Color(1,1,1,1);
        PixelInfo[] BorderArray = Border.ToArray();
        for(int i = 0; i < Filled.Count; i++) {
            TextureData[Filled[i].X + Filled[i].Y * width] = TrueWhite;//new Color(Mathf.Lerp(TextureData[Closest.X + Closest.Y * width].r, 1, T), Mathf.Lerp(TextureData[Closest.X + Closest.Y * width].g, 1, T), Mathf.Lerp(TextureData[Closest.X + Closest.Y * width].b, 1, T), 1);
        }
        OutputMask.SetPixels(TextureData);
    }

    private void CreateNewMask() {
        int width = InputTexture.width;
        int height = InputTexture.height;
        OutputMask = new Texture2D(width, height);
        Color[] TextureData = InputTexture.GetPixels(0);
        Color[] OutputTextureData = new Color[width * height];
        Color CurrentCol;
        Color MaskColor;
        Color WhiteColor  = new Color(1,1,1,1);
        Color BlackColor = new Color(0,0,0,1);
        for(int i = 0; i < width * height; i++) {
            CurrentCol = TextureData[i];
            float CurrentLum = luminance(CurrentCol.r, CurrentCol.g, CurrentCol.b);
            if(CurrentLum - UpperColor < Sharpness && CurrentLum - LowerColor >= -Sharpness && Mathf.Abs(CurrentLum - BackgroundColor) > 0.1f) {
                if(UseGradient) {
                    float GradientCol = CurrentLum - LowerColor;
                    GradientCol /= (UpperColor - LowerColor);
                    MaskColor = new Color(GradientCol, GradientCol, GradientCol,1);
                } else {
                    MaskColor = WhiteColor;
                }
            } else {
                MaskColor = BlackColor;
            }
            OutputTextureData[i] = MaskColor;
        }
        OutputMask.SetPixels(OutputTextureData);
        OutputMask.Apply();
    }

    private void CoverMask(Rect InputRect) {
        int width = InputTexture.width;
        int height = InputTexture.height;
        Vector2 CornerUL = ((InputRect.position - MaskTex.MainRect.position) / MaskTex.MainRect.size);
        Vector2 CornerLR = (((InputRect.position + InputRect.size) - MaskTex.MainRect.position) / MaskTex.MainRect.size);
        CornerUL = new Vector2(CornerUL.x, 1.0f - CornerUL.y);
        CornerLR = new Vector2(CornerLR.x, 1.0f - CornerLR.y);

        CornerUL = (CornerUL * ((MaskTex.ZoomRect.size + MaskTex.ZoomRect.position) - MaskTex.ZoomRect.position) + MaskTex.ZoomRect.position) * new Vector2(width, height);
        CornerLR = (CornerLR * ((MaskTex.ZoomRect.size + MaskTex.ZoomRect.position) - MaskTex.ZoomRect.position) + MaskTex.ZoomRect.position) * new Vector2(width, height);

        Color[] TextureData = OutputMask.GetPixels(0);
        Color ColorBlack = new Color(0,0,0,1);
        if(CornerUL.x > CornerLR.x) {
            float tmp = CornerLR.x;
            CornerLR.x = CornerUL.x;
            CornerUL.x = tmp;
        }
        if(CornerUL.y > CornerLR.y) {
            float tmp = CornerLR.y;
            CornerLR.y = CornerUL.y;
            CornerUL.y = tmp;   
        }
        for(int i = 0; i < width * height; i++) {
            int x = i % width;
            int y = i / width;
            if(x >= CornerUL.x && x <= CornerLR.x && y <= CornerLR.y && y >= CornerUL.y) {
                TextureData[i] = ColorBlack;
            }
        }
        OutputMask.SetPixels(TextureData);
        OutputMask.Apply();
    }

    void Update() {
        if (EditorWindow.focusedWindow == this &&
            EditorWindow.mouseOverWindow == this) {
            this.Repaint();
        }
    }
}
