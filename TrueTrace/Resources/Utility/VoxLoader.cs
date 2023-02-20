using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using CommonVars;

namespace TrueTrace {
    public sealed class VoxLoader {
        #region Chunk names
        private const string HEADER = "VOX ";
        private const string MAIN = "MAIN";
        private const string SIZE = "SIZE";
        private const string XYZI = "XYZI";
        private const string RGBA = "RGBA";
        private const string MATT = "MATT";
        private const string PACK = "PACK";

        private const string nTRN = "nTRN";
        private const string nGRP = "nGRP";
        private const string nSHP = "nSHP";
        private const string LAYR = "LAYR";
        private const string MATL = "MATL";
        private const string rOBJ = "rOBJ";
        #endregion

        private const int VERSION = 150;
        private int childCount = 0;
        public static readonly Quaternion toUnity = Quaternion.AngleAxis(90, Vector3.right);


        public VoxLoader(string Files2) {
        LoadModel(Files2);

        VoxelObjects = new List<VoxelObjectData>();

        TraverseTransforms(0, new Vector3(0,0,0), Matrix4x4.identity, 0);
    }

    public Color[] palette;
    public List<VoxelData> voxelFrames = new List<VoxelData>();
    public List<MaterialChunk> materialChunks = new List<MaterialChunk>();
        public List<TransformNodeChunk> transformNodeChunks = new List<TransformNodeChunk>();
        public List<GroupNodeChunk> groupNodeChunks = new List<GroupNodeChunk>();
        public List<ShapeNodeChunk> shapeNodeChunks = new List<ShapeNodeChunk>();
        public List<LayerChunk> layerChunks = new List<LayerChunk>();
        public List<RendererSettingChunk> rendererSettingChunks = new List<RendererSettingChunk>();
        public void SetAlphaFromTranparency() {
            for (int i = 0, count = Mathf.Min(palette.Length, materialChunks.Count); i < count; i++) {
                palette[i].a = materialChunks[i].Alpha;
            }
        }

        public static Quaternion GetTransform(ROTATION r, out Vector3 scale) {
            var b = (byte)r;
            var m = Matrix4x4.zero;
            var x = b & 3;
            var y = (b >> 2) & 3;
            scale.x = ((b >> 4) & 1) != 0 ? -1 : 1;
            scale.y = ((b >> 5) & 1) != 0 ? -1 : 1;
            scale.z = ((b >> 6) & 1) != 0 ? -1 : 1;
            m[x, 0] = scale.x;
            m[y, 1] = scale.y;
            m[Mathf.Clamp(3 - x - y, 0, 2), 2] = scale.z;
            m[3, 3] = 1;
            //Debug.Log($"{r} ({b})↓\r\n{m}");
            //Debug.Log($"lossyScale={m.lossyScale}");
            //Debug.Log($"scale={scale}");
            scale = m.lossyScale;
            return m.rotation;
        }

        public static Matrix4x4 ReadRotationVector(byte br)
        {
            Matrix4x4 rotMat = Matrix4x4.zero;

            byte rot = br;


            int r0v = ((rot & 8) == 0) ? 1 : -1;
            int r1v = ((rot & 16) == 0) ? 1 : -1;
            int r2v = ((rot & 32) == 0) ? 1 : -1;

            bool negativeFirst = ((rot & 0b0010000) >> 4) == 1;
            bool negativeSecond = ((rot & 0b0100000) >> 5) == 1;
            bool negativeThird = ((rot & 0b1000000) >> 6) == 1;


            int r0i = rot & 3;
            int r1i = (rot & 12) >> 2;

            int[] array = { -1, -1, -1 };
            int index = 0;

            array[r0i] = 0;
            array[r1i] = 0;

            for (int i = 0; i < 3; i++)
            {
                if (array[i] == -1)
                {
                    index = i;

                    break;
                }
            }

            int r2i = index;

            int[] result = { 0, 0, 0, 0, 0, 0, 0, 0, 0 };
                
            rotMat[0, r0i] = negativeFirst ? -1 : 1;
            rotMat[1, r1i] = negativeSecond ? -1 : 1;
            rotMat[2, r2i] = negativeThird ? -1 : 1;
            rotMat[3, 3] = 1;
            return rotMat;
        }

        public List<VoxelObjectData> VoxelObjects;
    private void TraverseTransforms(int Index, Vector3 Offsets, Matrix4x4 Rotation, int node) {
    var chunks = transformNodeChunks.Cast<NodeChunk>()
        .Concat(groupNodeChunks)
        .Concat(shapeNodeChunks)
        .ToDictionary(x => x.id, x => x);
        int ReversedIndex = 0;
            int ID = chunks[Index].id;
         var toVox = Quaternion.Inverse(toUnity);
        Vector3 Translation = new Vector3(0,0,0);
        Matrix4x4 ModifiedRotation = Matrix4x4.identity;
        int SecondaryIndex = Index;
        if(chunks[Index].Type == NodeType.Transform) {
            for(int i2 = 0; i2 < transformNodeChunks.Count; i2++) {
                if(ID == transformNodeChunks[i2].id) ReversedIndex = i2;
            }
            Translation = transformNodeChunks[ReversedIndex].TranslationAt(0);
            Translation.z = -Translation.z;
            ModifiedRotation = ReadRotationVector((byte)transformNodeChunks[ReversedIndex].RotationAt(0));
            TraverseTransforms(transformNodeChunks[ReversedIndex].childId, Offsets + Translation, (ModifiedRotation), transformNodeChunks[ReversedIndex].childId); 
        } else if(chunks[Index].Type == NodeType.Group){
            for(int i2 = 0; i2 < groupNodeChunks.Count; i2++) {
                if(ID == transformNodeChunks[i2].id) ReversedIndex = i2;
            }
            foreach (var childId in groupNodeChunks[ReversedIndex].childIds) {
                TraverseTransforms(childId, Offsets, Rotation, childId);
            }
        } else if(chunks[Index].Type == NodeType.Shape) {
            for(int i2 = 0; i2 < shapeNodeChunks.Count; i2++) {
                if(ID == shapeNodeChunks[i2].id) ReversedIndex = i2;
            }
            foreach (var sm in shapeNodeChunks[ReversedIndex].models) {
                VoxelData tempdata = voxelFrames[sm.modelId];
                tempdata.OriginOffset =  toUnity * Offsets;
                tempdata.Rotation = Rotation;
                VoxelObjects.Add(new VoxelObjectData() {
                    colors = tempdata.colors,
                    Size = new Vector3(tempdata.VoxelsWide, tempdata.VoxelsTall, tempdata.VoxelsDeep),
                    Translation = toUnity * Offsets,
                    Rotation = Rotation.transpose
                    });

                voxelFrames[sm.modelId] = tempdata;
            }
            SecondaryIndex -= transformNodeChunks.Count + groupNodeChunks.Count;
        } else {
            Debug.Log("FUCK");
        }
    }


    [System.Serializable]
    public class RendererSettingChunk { // rOBJ: Renderer Setting Chunk (undocumented)
        public KeyValue[] attributes;
    }

    [System.Serializable]
    public struct KeyValue {
        public string Key, Value;
    }
    public enum NodeType { Transform, Group, Shape, }
        public delegate void Logger(string message);

        public bool LoadModel(string absolutePath) {
            var name = Path.GetFileNameWithoutExtension(absolutePath);
            //Load the whole file
            using (var reader = new BinaryReader(new MemoryStream(File.ReadAllBytes(absolutePath)))) {
                var head = new string(reader.ReadChars(4));
                if (!head.Equals(HEADER)) {
                    return false;
                }
                int version = reader.ReadInt32();
                if (version != VERSION)
                    Debug.Log("Version number:" + version + " Was designed for " + VERSION);
                childCount = 0;
                while (reader.BaseStream.Position != reader.BaseStream.Length)
                    ReadChunk(reader);
            }
LoadDefaultPalette();
                        SetAlphaFromTranparency();


            for (int i = 0; i < voxelFrames.Count; i++) {
                var frame = voxelFrames[i];

            }

            #region LOD supports: added by XELF
            var dataList = voxelFrames.ToArray();
            var sizeList = voxelFrames.Select(d =>
                new Int3(d.VoxelsWide, d.VoxelsTall, d.VoxelsDeep)).ToArray();
            
            #endregion
            return true;
        }

        /// <summary>
        /// Loads the default color palette
        /// </summary>
        private Color[] LoadDefaultPalette() {
            var colorCount = default_palette.Length;
            var result = new Color[256];
            byte r, g, b, a;
            for (int i = 0; i < colorCount; i++) {
                var source = default_palette[i];
                r = (byte)(source & 0xff);
                g = (byte)((source >> 8) & 0xff);
                b = (byte)((source >> 16) & 0xff);
                a = (byte)((source >> 24) & 0xff);
                result[i] = new Color32(r, g, b, a);
            }
            return result;
        }
        /// <summary>
        /// palettes are offset by 1
        /// </summary>
        /// <param name="palette"></param>
        /// <returns></returns>
        private Color[] LoadPalette(BinaryReader cr) {
            var result = new Color[256];
            for (int i = 1; i < 256; i++)
                result[i] = new Color32(cr.ReadByte(), cr.ReadByte(), cr.ReadByte(), cr.ReadByte());
            return result;
        }

        #region vox extension, etc. : added by XELF

        private struct Int3 {
            public int X, Y, Z;
            public Int3(int x, int y, int z) { X = x; Y = y; Z = z; }
        }

        private static string ReadSTRING(BinaryReader reader) {
            var size = reader.ReadInt32();
            var bytes = reader.ReadBytes(size);
            return System.Text.Encoding.UTF8.GetString(bytes);
        }
        private delegate T ItemReader<T>(BinaryReader reader);
        private static T[] ReadArray<T>(BinaryReader reader, ItemReader<T> itemReader) =>
            Enumerable.Range(0, reader.ReadInt32())
                .Select(i => itemReader(reader)).ToArray();
        private static KeyValue[] ReadDICT(BinaryReader reader) {
            return Enumerable.Range(0, reader.ReadInt32())
                .Select(i => new KeyValue {
                    Key = ReadSTRING(reader),
                    Value = ReadSTRING(reader),
                }).ToArray();
        }
        private static MaterialChunk ReadMaterialChunk(BinaryReader reader) =>
            new MaterialChunk {
                id = reader.ReadInt32(),
                properties = ReadDICT(reader),
            };
        private static TransformNodeChunk ReadTransformNodeChunk(BinaryReader reader) =>
            new TransformNodeChunk {
                id = reader.ReadInt32(),
                attributes = ReadDICT(reader),
                childId = reader.ReadInt32(),
                reservedId = reader.ReadInt32(),
                layerId = reader.ReadInt32(),
                frameAttributes = ReadArray(reader, r => new DICT(ReadDICT(r))),
            };

        private static GroupNodeChunk ReadGroupNodeChunk(BinaryReader reader) =>
            new GroupNodeChunk {
                id = reader.ReadInt32(),
                attributes = ReadDICT(reader),
                childIds = ReadArray(reader, r => r.ReadInt32()),
            };
        private static ShapeNodeChunk ReadShapeNodeChunk(BinaryReader reader) =>
            new ShapeNodeChunk {
                id = reader.ReadInt32(),
                attributes = ReadDICT(reader),
                models = ReadArray(reader, r => new ShapeModel {
                    modelId = r.ReadInt32(),
                    attributes = ReadDICT(r),
                }),
            };
        private static LayerChunk ReadLayerChunk(BinaryReader reader) =>
            new LayerChunk {
                id = reader.ReadInt32(),
                attributes = ReadDICT(reader),
                unknown = reader.ReadInt32(),
            };
        #endregion

        private void ReadChunk(BinaryReader reader) {
            var chunkName = new string(reader.ReadChars(4));
            var chunkSize = reader.ReadInt32();
            var childChunkSize = reader.ReadInt32();
            //get current chunk bytes and process
            var chunk = reader.ReadBytes(chunkSize);
            var children = reader.ReadBytes(childChunkSize);
            using (var chunkReader = new BinaryReader(new MemoryStream(chunk))) {
                //Debug.Log(chunkName);

                switch (chunkName) {
                    case MAIN:
                        break;
                    case SIZE:
                        int w = chunkReader.ReadInt32();
                        int h = chunkReader.ReadInt32();
                        int d = chunkReader.ReadInt32();
                        if (childCount >= voxelFrames.Count)
                            voxelFrames.Add(new VoxelData());
                        voxelFrames[childCount].Resize(w, d, h);
                        childCount++;
                        break;
                    case XYZI:
                        var voxelCount = chunkReader.ReadInt32();
                        var frame = voxelFrames[childCount - 1];
                        byte x, y, z;
                        for (int i = 0; i < voxelCount; i++) {
                            x = chunkReader.ReadByte();
                            y = chunkReader.ReadByte();
                            z = chunkReader.ReadByte();
                            frame.Set(x, z, y, chunkReader.ReadByte());
                        }
                        break;
                    case RGBA:
                        palette = LoadPalette(chunkReader);
                        break;
                    case MATT:
                        break;
                    case PACK:
                        int frameCount = chunkReader.ReadInt32();
                        for (int i = 0; i < frameCount; i++)
                            voxelFrames.Add(new VoxelData());
                        break;

                    #region Vox extension: added by XELF

                    case nTRN:
                        transformNodeChunks.Add(ReadTransformNodeChunk(chunkReader));
                        break;
                    case nGRP:
                        groupNodeChunks.Add(ReadGroupNodeChunk(chunkReader));
                        break;
                    case nSHP:
                        shapeNodeChunks.Add(ReadShapeNodeChunk(chunkReader));
                        break;
                    case LAYR:
                        layerChunks.Add(ReadLayerChunk(chunkReader));
                        break;
                    case MATL:
                        materialChunks.Add(ReadMaterialChunk(chunkReader));
                        break;
                    case rOBJ:
                        rendererSettingChunks.Add(ReadRObjectChunk(chunkReader));
                        break;

                    #endregion

                    default:
                        // Debug.LogError($"Unknown chunk: \"{chunkName}\"");
                        break;
                }
            }
            //read child chunks
            using (var childReader = new BinaryReader(new MemoryStream(children))) {
                while (childReader.BaseStream.Position != childReader.BaseStream.Length)
                    ReadChunk(childReader);
            }
        }
        #region Default Palette
        private static uint[] default_palette = new uint[] {
            0x00000000, 0xffffffff, 0xffccffff, 0xff99ffff, 0xff66ffff, 0xff33ffff, 0xff00ffff, 0xffffccff, 0xffccccff, 0xff99ccff, 0xff66ccff, 0xff33ccff, 0xff00ccff, 0xffff99ff, 0xffcc99ff, 0xff9999ff,
            0xff6699ff, 0xff3399ff, 0xff0099ff, 0xffff66ff, 0xffcc66ff, 0xff9966ff, 0xff6666ff, 0xff3366ff, 0xff0066ff, 0xffff33ff, 0xffcc33ff, 0xff9933ff, 0xff6633ff, 0xff3333ff, 0xff0033ff, 0xffff00ff,
            0xffcc00ff, 0xff9900ff, 0xff6600ff, 0xff3300ff, 0xff0000ff, 0xffffffcc, 0xffccffcc, 0xff99ffcc, 0xff66ffcc, 0xff33ffcc, 0xff00ffcc, 0xffffcccc, 0xffcccccc, 0xff99cccc, 0xff66cccc, 0xff33cccc,
            0xff00cccc, 0xffff99cc, 0xffcc99cc, 0xff9999cc, 0xff6699cc, 0xff3399cc, 0xff0099cc, 0xffff66cc, 0xffcc66cc, 0xff9966cc, 0xff6666cc, 0xff3366cc, 0xff0066cc, 0xffff33cc, 0xffcc33cc, 0xff9933cc,
            0xff6633cc, 0xff3333cc, 0xff0033cc, 0xffff00cc, 0xffcc00cc, 0xff9900cc, 0xff6600cc, 0xff3300cc, 0xff0000cc, 0xffffff99, 0xffccff99, 0xff99ff99, 0xff66ff99, 0xff33ff99, 0xff00ff99, 0xffffcc99,
            0xffcccc99, 0xff99cc99, 0xff66cc99, 0xff33cc99, 0xff00cc99, 0xffff9999, 0xffcc9999, 0xff999999, 0xff669999, 0xff339999, 0xff009999, 0xffff6699, 0xffcc6699, 0xff996699, 0xff666699, 0xff336699,
            0xff006699, 0xffff3399, 0xffcc3399, 0xff993399, 0xff663399, 0xff333399, 0xff003399, 0xffff0099, 0xffcc0099, 0xff990099, 0xff660099, 0xff330099, 0xff000099, 0xffffff66, 0xffccff66, 0xff99ff66,
            0xff66ff66, 0xff33ff66, 0xff00ff66, 0xffffcc66, 0xffcccc66, 0xff99cc66, 0xff66cc66, 0xff33cc66, 0xff00cc66, 0xffff9966, 0xffcc9966, 0xff999966, 0xff669966, 0xff339966, 0xff009966, 0xffff6666,
            0xffcc6666, 0xff996666, 0xff666666, 0xff336666, 0xff006666, 0xffff3366, 0xffcc3366, 0xff993366, 0xff663366, 0xff333366, 0xff003366, 0xffff0066, 0xffcc0066, 0xff990066, 0xff660066, 0xff330066,
            0xff000066, 0xffffff33, 0xffccff33, 0xff99ff33, 0xff66ff33, 0xff33ff33, 0xff00ff33, 0xffffcc33, 0xffcccc33, 0xff99cc33, 0xff66cc33, 0xff33cc33, 0xff00cc33, 0xffff9933, 0xffcc9933, 0xff999933,
            0xff669933, 0xff339933, 0xff009933, 0xffff6633, 0xffcc6633, 0xff996633, 0xff666633, 0xff336633, 0xff006633, 0xffff3333, 0xffcc3333, 0xff993333, 0xff663333, 0xff333333, 0xff003333, 0xffff0033,
            0xffcc0033, 0xff990033, 0xff660033, 0xff330033, 0xff000033, 0xffffff00, 0xffccff00, 0xff99ff00, 0xff66ff00, 0xff33ff00, 0xff00ff00, 0xffffcc00, 0xffcccc00, 0xff99cc00, 0xff66cc00, 0xff33cc00,
            0xff00cc00, 0xffff9900, 0xffcc9900, 0xff999900, 0xff669900, 0xff339900, 0xff009900, 0xffff6600, 0xffcc6600, 0xff996600, 0xff666600, 0xff336600, 0xff006600, 0xffff3300, 0xffcc3300, 0xff993300,
            0xff663300, 0xff333300, 0xff003300, 0xffff0000, 0xffcc0000, 0xff990000, 0xff660000, 0xff330000, 0xff0000ee, 0xff0000dd, 0xff0000bb, 0xff0000aa, 0xff000088, 0xff000077, 0xff000055, 0xff000044,
            0xff000022, 0xff000011, 0xff00ee00, 0xff00dd00, 0xff00bb00, 0xff00aa00, 0xff008800, 0xff007700, 0xff005500, 0xff004400, 0xff002200, 0xff001100, 0xffee0000, 0xffdd0000, 0xffbb0000, 0xffaa0000,
            0xff880000, 0xff770000, 0xff550000, 0xff440000, 0xff220000, 0xff110000, 0xffeeeeee, 0xffdddddd, 0xffbbbbbb, 0xffaaaaaa, 0xff888888, 0xff777777, 0xff555555, 0xff444444, 0xff222222, 0xff111111
        };
        #endregion

[System.Serializable]
    public abstract class NodeChunk {
        public int id;
        public KeyValue[] attributes;
        public abstract NodeType Type { get; }
    }

    [System.Serializable]
    public class TransformNodeChunk : NodeChunk { // nTRN: Transform Node Chunk
        public int childId;
        public int reservedId;
        public int layerId;
        public DICT[] frameAttributes;

        public override NodeType Type => NodeType.Transform;

        public ROTATION RotationAt(int frame = 0)
            => frame < frameAttributes.Length ? frameAttributes[frame]._r : ROTATION._PX_PY_P;
        public Vector3 TranslationAt(int frame = 0)
            => frame < frameAttributes.Length ? frameAttributes[frame]._t : Vector3.zero;

        public string Name {
            get {
                var kv = attributes.FirstOrDefault(a => a.Key == "_name");
                return kv.Key != null ? kv.Value : string.Empty;
            }
        }
        public bool Hidden {
            get {
                var kv = attributes.FirstOrDefault(a => a.Key == "_hidden");
                return kv.Key != null ? kv.Value != "0" : false;
            }
        }
    }
    [System.Serializable]
    public class DICT { // DICT
        public KeyValue[] items;

        public DICT(KeyValue[] items) {
            this.items = items;
        }
        public DICT() { }

        public ROTATION _r {
            get {
                var item = items.FirstOrDefault(i => i.Key == "_r");
                if (item.Key == null)
                    return ROTATION._PX_PY_P;
                byte result;
                if (!byte.TryParse(item.Value, out result))
                    return ROTATION._PX_PY_P;
                return (ROTATION)result;
            }
        }
        public Vector3 _t {
            get {
                var result = Vector3.zero;
                var item = items.FirstOrDefault(i => i.Key == "_t");
                if (item.Key == null)
                    return result;
                var data = item.Value.Split(' ');
                if (data.Length > 0)
                    float.TryParse(data[0], out result.x);
                if (data.Length > 1)
                    float.TryParse(data[1], out result.y);
                if (data.Length > 2)
                    float.TryParse(data[2], out result.z);
                return result;
            }
        }
    }
    [System.Serializable]
    public class GroupNodeChunk : NodeChunk { // nGRP: Group Node Chunk
        public int[] childIds;
        public override NodeType Type => NodeType.Group;
    }
    [System.Serializable]
    public class ShapeNodeChunk : NodeChunk { // nSHP: Shape Node Chunk
        public ShapeModel[] models;
        public override NodeType Type => NodeType.Shape;
    }
    public enum MaterialType {
        _diffuse, _metal, _glass, _emit
    }
        [System.Serializable]
    public class ShapeModel {
        public int modelId;
        public KeyValue[] attributes; // reserved
    }
    [System.Serializable]
    public class MaterialChunk { // MATL: Material Chunk
        public int id;
        public KeyValue[] properties;

        #region Getters
        public MaterialType Type {
            get {
                var result = MaterialType._diffuse;
                var item = properties.FirstOrDefault(i => i.Key == "_type");
                if (item.Key != null)
                    System.Enum.TryParse(item.Value, out result);
                return result;
            }
        }
        public float Weight {
            get {
                var result = 1f;
                var item = properties.FirstOrDefault(i => i.Key == "_weight");
                if (item.Key != null)
                    float.TryParse(item.Value, out result);
                return result;
            }
        }
        public float Rough {
            get {
                var result = 1f;
                var item = properties.FirstOrDefault(i => i.Key == "_rough");
                if (item.Key != null)
                    float.TryParse(item.Value, out result);
                return result;
            }
        }
        public float Spec {
            get {
                var result = 1f;
                var item = properties.FirstOrDefault(i => i.Key == "_spec");
                if (item.Key != null)
                    float.TryParse(item.Value, out result);
                return result;
            }
        }
        public float Flux {
            get {
                var result = 1f;
                var item = properties.FirstOrDefault(i => i.Key == "_flux");
                if (item.Key != null)
                    float.TryParse(item.Value, out result);
                return result;
            }
        }
        #endregion

        #region Getters friendly for Unity Standard Shader

        public float Smoothness => 1 - Rough;
        public float Metallic => Type == MaterialType._metal ? Weight : 0;
        public float Emission => Type == MaterialType._emit ? Weight * Flux : 0;
        public float Transparency => Type == MaterialType._glass ? Weight : 0;
        public float Alpha => 1 - Transparency;

        #endregion
    }
        [System.Serializable]
    public class LayerChunk { // LAYR: Layer Chunk
        public int id;
        public KeyValue[] attributes;
        public int unknown;

        public string Name {
            get {
                var item = attributes.FirstOrDefault(i => i.Key == "_name");
                return item.Key != null ? item.Value : string.Empty;
            }
        }
        public bool Hidden {
            get {
                var item = attributes.FirstOrDefault(i => i.Key == "_hidden");
                return item.Key != null ? item.Value != "0" : false;
            }
        }
    }
            private static RendererSettingChunk ReadRObjectChunk(BinaryReader reader) =>
            new RendererSettingChunk {
                //id = reader.ReadInt32(),
                attributes = ReadDICT(reader),
            };

        [System.Flags]
    public enum ROTATION : byte { // ROTATION
        _PX_PX_P, _PY_PX_P, _PZ_PX_P, _PW_PX_P,
        _PX_PY_P, _PY_PY_P, _PZ_PY_P, _PW_PY_P,
        _PX_PZ_P, _PY_PZ_P, _PZ_PZ_P, _PW_PZ_P,
        _PX_PW_P, _PY_PW_P, _PZ_PW_P, _PW_PW_P,
        _NX_PX_P, _NY_PX_P, _NZ_PX_P, _NW_PX_P,
        _NX_PY_P, _NY_PY_P, _NZ_PY_P, _NW_PY_P,
        _NX_PZ_P, _NY_PZ_P, _NZ_PZ_P, _NW_PZ_P,
        _NX_PW_P, _NY_PW_P, _NZ_PW_P, _NW_PW_P,
        _PX_NX_P, _PY_NX_P, _PZ_NX_P, _PW_NX_P,
        _PX_NY_P, _PY_NY_P, _PZ_NY_P, _PW_NY_P,
        _PX_NZ_P, _PY_NZ_P, _PZ_NZ_P, _PW_NZ_P,
        _PX_NW_P, _PY_NW_P, _PZ_NW_P, _PW_NW_P,
        _NX_NX_P, _NY_NX_P, _NZ_NX_P, _NW_NX_P,
        _NX_NY_P, _NY_NY_P, _NZ_NY_P, _NW_NY_P,
        _NX_NZ_P, _NY_NZ_P, _NZ_NZ_P, _NW_NZ_P,
        _NX_NW_P, _NY_NW_P, _NZ_NW_P, _NW_NW_P,

        _PX_PX_N, _PY_PX_N, _PZ_PX_N, _PW_PX_N,
        _PX_PY_N, _PY_PY_N, _PZ_PY_N, _PW_PY_N,
        _PX_PZ_N, _PY_PZ_N, _PZ_PZ_N, _PW_PZ_N,
        _PX_PW_N, _PY_PW_N, _PZ_PW_N, _PW_PW_N,
        _NX_PX_N, _NY_PX_N, _NZ_PX_N, _NW_PX_N,
        _NX_PY_N, _NY_PY_N, _NZ_PY_N, _NW_PY_N,
        _NX_PZ_N, _NY_PZ_N, _NZ_PZ_N, _NW_PZ_N,
        _NX_PW_N, _NY_PW_N, _NZ_PW_N, _NW_PW_N,
        _PX_NX_N, _PY_NX_N, _PZ_NX_N, _PW_NX_N,
        _PX_NY_N, _PY_NY_N, _PZ_NY_N, _PW_NY_N,
        _PX_NZ_N, _PY_NZ_N, _PZ_NZ_N, _PW_NZ_N,
        _PX_NW_N, _PY_NW_N, _PZ_NW_N, _PW_NW_N,
        _NX_NX_N, _NY_NX_N, _NZ_NX_N, _NW_NX_N,
        _NX_NY_N, _NY_NY_N, _NZ_NY_N, _NW_NY_N,
        _NX_NZ_N, _NY_NZ_N, _NZ_NZ_N, _NW_NZ_N,
        _NX_NW_N, _NY_NW_N, _NZ_NW_N, _NW_NW_N,
    }


    public sealed class VoxelData {

        [SerializeField] public Vector3 OriginOffset;
        [SerializeField] public Matrix4x4 Rotation;
        [SerializeField]
        public int _voxelsWide, _voxelsTall, _voxelsDeep;
        [SerializeField, HideInInspector]
        public byte[] colors;
        /// <summary>
        /// Creates a new empty voxel data
        /// </summary>
        public VoxelData() { }

        /// <summary>
        /// Creates a voxeldata with provided dimensions
        /// </summary>
        /// <param name="voxelsWide"></param>
        /// <param name="voxelsTall"></param>
        /// <param name="voxelsDeep"></param>
        public VoxelData(int voxelsWide, int voxelsTall, int voxelsDeep) {
            Resize(voxelsWide, voxelsTall, voxelsDeep);
        }

        public void Resize(int voxelsWide, int voxelsTall, int voxelsDeep) {
            _voxelsWide = voxelsWide;
            _voxelsTall = voxelsTall;
            _voxelsDeep = voxelsDeep;
            colors = new byte[_voxelsWide * _voxelsTall * _voxelsDeep];
        }
        /// <summary>
        /// Gets a grid position from voxel coordinates
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <returns></returns>
        public int GetGridPos(int x, int y, int z)
            => (_voxelsWide * _voxelsTall) * z + (_voxelsWide * y) + x;

        /// <summary>
        /// Sets a color index from voxel coordinates
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <param name="value"></param>
        public void Set(int x, int y, int z, byte value)
            => colors[GetGridPos(x, y, z)] = value;

        /// <summary>
        /// Sets a color index from grid position
        /// </summary>
        /// <param name="x"></param>
        /// <param name="value"></param>
        public void Set(int x, byte value)
            => colors[x] = value;

        /// <summary>
        /// Gets a palette index from voxel coordinates
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <returns></returns>
        public int Get(int x, int y, int z)
            => colors[GetGridPos(x, y, z)];
        /// <summary>
        /// Gets a pa
        /// lette index from a grid position
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public byte Get(int x) => colors[x];

        /// <summary>
        /// width of the data in voxels
        /// </summary>
        public int VoxelsWide => _voxelsWide;

        /// <summary>
        /// height of the data in voxels
        /// </summary>
        public int VoxelsTall => _voxelsTall;

        /// <summary>
        /// Depth of the voxels in data
        /// </summary>
        public int VoxelsDeep => _voxelsDeep;

        /// <summary>
        /// Voxel dimension as integers
        /// </summary>
        public Vector3 VoxelDimension =>
            new Vector3(_voxelsWide, _voxelsTall, _voxelsDeep);

        public byte[] Colors => colors;

        #region added by XELF
        public bool Contains(int x, int y, int z)
            => x >= 0 && y >= 0 && z >= 0 && x < _voxelsWide && y < VoxelsTall && z < VoxelsDeep;
        public byte GetSafe(int x, int y, int z)
            => Contains(x, y, z) ? colors[GetGridPos(x, y, z)] : (byte)0;

        public delegate bool ColorPredicator(Color color);

        public VoxelData Where(Color[] palette, ColorPredicator predicate) {
            var result = new VoxelData(_voxelsWide, _voxelsTall, _voxelsDeep);
            for (int i = 0; i < colors.Length; i++) {
                if (predicate(palette[colors[i]]))
                    result.colors[i] = colors[i];
            }
            return result;
        }
        public bool Any(Color[] palette, ColorPredicator predicate)
            => colors.Any(c => predicate(palette[c]));

        public VoxelData ToSmaller() {
            var work = new byte[8];
            var result = new VoxelData(
                (_voxelsWide + 1) >> 1,
                (_voxelsTall + 1) >> 1,
                (_voxelsDeep + 1) >> 1);
            int i = 0;
            for (int z = 0; z < _voxelsDeep; z += 2) {
                var z1 = z + 1;
                for (int y = 0; y < _voxelsTall; y += 2) {
                    var y1 = y + 1;
                    for (int x = 0; x < _voxelsWide; x += 2) {
                        var x1 = x + 1;
                        work[0] = GetSafe(x, y, z);
                        work[1] = GetSafe(x1, y, z);
                        work[2] = GetSafe(x, y1, z);
                        work[3] = GetSafe(x1, y1, z);
                        work[4] = GetSafe(x, y, z1);
                        work[5] = GetSafe(x1, y, z1);
                        work[6] = GetSafe(x, y1, z1);
                        work[7] = GetSafe(x1, y1, z1);

                        if (work.Any(color => color != 0)) {
                            var groups = work.Where(color => color != 0).GroupBy(v => v).OrderByDescending(v => v.Count());
                            var count = groups.ElementAt(0).Count();
                            var group = groups.TakeWhile(v => v.Count() == count)
                                .OrderByDescending(v => v.Key).First();
                            result.colors[i++] = group.Key;
                        } else {
                            result.colors[i++] = 0;
                        }
                    }
                }
            }
            return result;
        }
        #endregion
    }
    }
}