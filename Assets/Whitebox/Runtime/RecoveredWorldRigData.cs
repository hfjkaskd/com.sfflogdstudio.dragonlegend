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
        [Serializable] public sealed class RelativeLocalConstraint
        {
            public int bone, target;
            public float rotationOffset, rotationMix;
            public Vector2 translationOffset, translationMix, scaleOffset, scaleMix;
            public void Apply(float[] pose)
            {
                int b=bone*5,t=target*5;
                pose[b]+=(pose[t]+rotationOffset)*rotationMix;
                pose[b+1]+=(pose[t+1]+translationOffset.x)*translationMix.x;
                pose[b+2]+=(pose[t+2]+translationOffset.y)*translationMix.y;
                pose[b+3]*=1+(pose[t+3]-1+scaleOffset.x)*scaleMix.x;
                pose[b+4]*=1+(pose[t+4]-1+scaleOffset.y)*scaleMix.y;
            }
        }
        public RelativeLocalConstraint[] relativeLocalConstraints;
        public RecoveredRegionRig.Bone[] bones;
        public Slot[] slots;
        public Attachment[] attachments;
        public RecoveredRigAnimation.Channel[] channels;
        public string atlasPath;
        public float duration, pixelsPerUnit;
    }
}
