using System;
using UnityEngine;
using UnityEngine.UI;

namespace DragonLegend.Whitebox
{
    // A region-only UI effect. Prefab poses/regions are authored offline; Animation drives pose fields.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class RecoveredRegionRig : MaskableGraphic
    {
        [Serializable] public sealed class Bone {public string name;public int parent,mode;public float x,y,rotation,scaleX=1,scaleY=1;}
        [Serializable] public sealed class Slot {public int bone;public float attachment;public Color tint=Color.white;public bool additive;[NonSerialized] public int sequenceIndex=-1;}
        [Serializable] public sealed class Region {public Vector2[] vertices,uv;public Color tint=Color.white;public int[] sequenceFrames;public int setupIndex;}
        public Bone[] bones;
        public Slot[] slots;
        public Region[] regions;
        [SerializeField] private string atlasPath;
        private Texture2D atlas;
        private Matrix4x4[] matrices;
        public override Texture mainTexture {get {if(atlas==null)atlas=Resources.Load<Texture2D>(atlasPath);return atlas;}}
        public Matrix4x4 BoneMatrix(int index)=>matrices[index];
        public void RefreshPose()
        {
            if(bones==null)return;
            if(matrices==null||matrices.Length!=bones.Length)matrices=new Matrix4x4[bones.Length];
            for(int i=0;i<bones.Length;i++) {
                var bone=bones[i];if(bone.mode!=0&&bone.mode!=1&&bone.mode!=3)throw new InvalidOperationException("Unsupported bone inheritance");var parent=bone.parent<0?Matrix4x4.identity:matrices[bone.parent];
                var local=Matrix4x4.TRS(new Vector3(bone.x,bone.y,0),Quaternion.Euler(0,0,bone.rotation),new Vector3(bone.scaleX,bone.scaleY,1));
                if(bone.mode==0)matrices[i]=parent*local;
                else if(bone.mode==1) {
                    // OnlyTranslation: inherit the transformed origin, not parent axes.
                    var position=parent.MultiplyPoint3x4(new Vector3(bone.x,bone.y,0));
                    local.SetColumn(3,new Vector4(position.x,position.y,0,1));matrices[i]=local;
                }
                else {
                    float radians=bone.rotation*Mathf.Deg2Rad;
                    var direction=parent.MultiplyVector(new Vector3(Mathf.Cos(radians),Mathf.Sin(radians),0));
                    float length=direction.magnitude;
                    direction*=length>0.00001f?1/length:length;
                    float reflection=parent.m00*parent.m11-parent.m01*parent.m10<0?-1:1;
                    var position=parent.MultiplyPoint3x4(new Vector3(bone.x,bone.y,0));
                    var matrix=Matrix4x4.identity;
                    matrix.SetColumn(0,new Vector4(direction.x*bone.scaleX,direction.y*bone.scaleX,0,0));
                    matrix.SetColumn(1,new Vector4(-direction.y*reflection*bone.scaleY,direction.x*reflection*bone.scaleY,0,0));
                    matrix.SetColumn(3,new Vector4(position.x,position.y,0,1));matrices[i]=matrix;
                }
            }
            SetVerticesDirty();
        }
        protected override void OnEnable(){base.OnEnable();RefreshPose();}
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();if(slots==null||matrices==null)return;
            for(int i=0;i<slots.Length;i++) {
                var slot=slots[i];int index=Mathf.RoundToInt(slot.attachment);if(index<0)continue;
                var region=regions[index];
                if(region.sequenceFrames!=null&&region.sequenceFrames.Length>0) {
                    int frame=slot.sequenceIndex<0?region.setupIndex:slot.sequenceIndex;
                    region=regions[region.sequenceFrames[Math.Min(frame,region.sequenceFrames.Length-1)]];
                }
                Color tint=slot.tint*region.tint*color;
                tint.r*=tint.a;tint.g*=tint.a;tint.b*=tint.a;if(slot.additive)tint.a=0;
                int first=mesh.currentVertCount;
                for(int v=0;v<4;v++)mesh.AddVert(matrices[slot.bone].MultiplyPoint3x4(region.vertices[v]),tint,region.uv[v]);
                mesh.AddTriangle(first,first+1,first+2);mesh.AddTriangle(first+2,first+3,first);
            }
        }
    }
}
