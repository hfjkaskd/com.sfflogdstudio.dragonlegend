using System;
using UnityEngine;

namespace DragonLegend.Whitebox
{
    public sealed class RecoveredWorldRigData : ScriptableObject
    {
        [Serializable] public sealed class Slot { public int bone, attachment; public Color tint; public bool additive; }
        [Serializable] public sealed class Attachment
        {
            public string name;
            public int slot;
            public bool clipping;
            public int endSlot;
            public Vector2[] positions, uv;
            public int[] triangles, counts, boneIndices;
            public float[] weights;
            public Color tint;
            public DeformFrame[] deform;
        }
        [Serializable] public sealed class DeformFrame { public float time; public float[] values; public AnimationCurve progress; }
        public RecoveredRegionRig.Bone[] bones;
        public Slot[] slots;
        public Attachment[] attachments;
        public RecoveredRigAnimation.Channel[] channels;
        public string atlasPath;
        public float duration, pixelsPerUnit;
    }
}
