using System;
using System.Collections.Generic;
using UnityEngine;

namespace DragonLegend.Whitebox
{
    // Original weighted vertices and deformation, rendered with Unity MeshRenderer.
    // Definition/curves are immutable shared assets. Each pooled instance owns reusable pose/mesh buffers.
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class RecoveredWorldRig : MonoBehaviour
    {
        [SerializeField] private string dataPath;
        [SerializeField] private float poseTime;
        private RecoveredWorldRigData data;
        private Mesh mesh;
        private Matrix4x4[] matrices;
        private float[] local;
        private int[] attachments;
        private Color[] tints;
        private List<Vector3> vertices;
        private List<Vector2> uvs;
        private List<Color> colors;
        private List<int> triangles;
        public RecoveredWorldRigData Data => data;
        public Mesh CurrentMesh => mesh;
        public Matrix4x4 BoneMatrix(int index) => matrices[index];
        public void Sample(float time) { poseTime = time; RefreshPose(); }
        private void OnEnable() => RefreshPose();
        private void OnDidApplyAnimationProperties() => RefreshPose();

        private bool Initialize()
        {
            if (data != null) return true;
            if (string.IsNullOrEmpty(dataPath)) return false;
            data = Resources.Load<RecoveredWorldRigData>(dataPath);
            if (data == null) throw new InvalidOperationException("Missing world rig data: " + dataPath);
            matrices = new Matrix4x4[data.bones.Length]; local = new float[data.bones.Length * 5];
            attachments = new int[data.slots.Length]; tints = new Color[data.slots.Length];
            int vertexCapacity = 0, indexCapacity = 0;
            for (int i = 0; i < data.attachments.Length; i++) {
                vertexCapacity += data.attachments[i].uv.Length; indexCapacity += data.attachments[i].triangles.Length;
            }
            vertices = new List<Vector3>(vertexCapacity); uvs = new List<Vector2>(vertexCapacity);
            colors = new List<Color>(vertexCapacity); triangles = new List<int>(indexCapacity);
            mesh = new Mesh { name = data.name + " instance" }; mesh.MarkDynamic();
            GetComponent<MeshFilter>().sharedMesh = mesh;
            var properties = new MaterialPropertyBlock();
            properties.SetTexture("_MainTex", Resources.Load<Texture2D>(data.atlasPath));
            GetComponent<MeshRenderer>().SetPropertyBlock(properties);
            return true;
        }

        private void RefreshPose()
        {
            if (!Initialize()) return;
            for (int i = 0; i < data.bones.Length; i++) {
                var b = data.bones[i]; int p = i * 5;
                local[p] = b.rotation; local[p+1] = b.x; local[p+2] = b.y;
                local[p+3] = b.scaleX; local[p+4] = b.scaleY;
            }
            for (int i = 0; i < data.slots.Length; i++) { attachments[i] = data.slots[i].attachment; tints[i] = data.slots[i].tint; }
            for (int i = 0; i < data.channels.Length; i++) {
                var c = data.channels[i]; float value = c.curve.Evaluate(poseTime);
                if (!c.slot) local[c.index * 5 + c.component] = value;
                else if (c.component < 0) attachments[c.index] = Mathf.RoundToInt(value);
                else { var tint = tints[c.index]; tint[c.component] = value; tints[c.index] = tint; }
            }
            for (int i = 0; i < data.bones.Length; i++) {
                var b = data.bones[i]; int p = i * 5;
                matrices[i] = Compose(b.parent < 0 ? Matrix4x4.identity : matrices[b.parent], b.mode,
                    local[p+1], local[p+2], local[p], local[p+3], local[p+4]);
            }
            vertices.Clear(); uvs.Clear(); colors.Clear(); triangles.Clear();
            for (int s = 0; s < attachments.Length; s++) {
                int a = attachments[s]; if (a < 0) continue;
                Draw(data.attachments[a], s);
            }
            mesh.Clear(); mesh.SetVertices(vertices); mesh.SetUVs(0, uvs); mesh.SetColors(colors);
            mesh.SetTriangles(triangles, 0, true);
        }

        public static Matrix4x4 Compose(Matrix4x4 parent, int mode, float x, float y, float rotation, float sx, float sy)
        {
            var localMatrix = Matrix4x4.TRS(new Vector3(x, y, 0), Quaternion.Euler(0, 0, rotation), new Vector3(sx, sy, 1));
            if (mode == 0) return parent * localMatrix;
            Vector3 position = parent.MultiplyPoint3x4(new Vector3(x, y, 0));
            if (mode == 1) {
                localMatrix.SetColumn(3, new Vector4(position.x, position.y, 0, 1)); return localMatrix;
            }
            if (mode != 3 && mode != 4) throw new InvalidOperationException("Unconverted world bone inheritance");
            float angle = rotation * Mathf.Deg2Rad;
            Vector3 axis = parent.MultiplyVector(new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0));
            float length = axis.magnitude; axis *= length > .00001f ? 1 / length : length;
            float reflection = mode == 3 && parent.m00 * parent.m11 - parent.m01 * parent.m10 < 0 ? -1 : 1;
            var matrix = Matrix4x4.identity;
            matrix.SetColumn(0, new Vector4(axis.x * sx, axis.y * sx, 0, 0));
            matrix.SetColumn(1, new Vector4(-axis.y * reflection * sy, axis.x * reflection * sy, 0, 0));
            matrix.SetColumn(3, new Vector4(position.x, position.y, 0, 1)); return matrix;
        }

        private void Draw(RecoveredWorldRigData.Attachment a, int slotIndex)
        {
            var frames = a.deform; int frame = -1; float mix = 0;
            if (frames != null) {
                for (int f = 0; f < frames.Length && frames[f].time <= poseTime; f++) frame = f;
                if (frame >= 0 && frame + 1 < frames.Length) mix = frames[frame].progress.Evaluate(poseTime);
            }
            Color tint = a.tint * tints[slotIndex]; tint.r *= tint.a; tint.g *= tint.a; tint.b *= tint.a;
            if (data.slots[slotIndex].additive) tint.a = 0;
            int first = vertices.Count, influence = 0;
            bool weighted = a.counts.Length != 0;
            for (int v = 0; v < a.uv.Length; v++) {
                Vector3 point = Vector3.zero;
                int count = weighted ? a.counts[v] : 1;
                for (int w = 0; w < count; w++, influence++) {
                    Vector2 value = a.positions[influence];
                    if (frame >= 0) {
                        int index = influence * 2; var values = frames[frame].values;
                        Vector2 deform = new Vector2(values[index], values[index+1]);
                        if (frame + 1 < frames.Length) {
                            var next = frames[frame+1].values;
                            deform = Vector2.LerpUnclamped(deform, new Vector2(next[index], next[index+1]), mix);
                        }
                        value = weighted ? value + deform : deform;
                    }
                    point += matrices[weighted ? a.boneIndices[influence] : data.slots[slotIndex].bone].MultiplyPoint3x4(value)
                        * (weighted ? a.weights[influence] : 1);
                }
                vertices.Add(point / data.pixelsPerUnit); uvs.Add(a.uv[v]); colors.Add(tint);
            }
            for (int t = 0; t < a.triangles.Length; t++) triangles.Add(first + a.triangles[t]);
        }
        private void OnDestroy() { if (mesh != null) Destroy(mesh); }
    }
}
