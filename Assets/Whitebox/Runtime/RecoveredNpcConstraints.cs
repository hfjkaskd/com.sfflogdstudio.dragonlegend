using System;
using UnityEngine;

namespace DragonLegend.Whitebox
{
    // Authored update order for ef_long: absolute-local mirror followed by
    // absolute-world translation constraints and original descendant recomputes.
    [Serializable]
    public sealed class RecoveredNpcConstraints
    {
        [Serializable] public sealed class Constraint
        {
            public int bone,target;
            public bool local;
            public Vector2 offset,translationMix,scaleMix;
        }
        public Constraint[] constraints;
        // Nonnegative: recompute bone; negative: apply constraint (-index-1).
        public int[] steps;
        public void Evaluate(RecoveredRegionRig.Bone[] bones,Matrix4x4[] matrices)
        {
            for(int i=0;i<steps.Length;i++) {
                int step=steps[i];
                if(step>=0) {
                    var b=bones[step];
                    matrices[step]=RecoveredWorldRig.Compose(b.parent<0?Matrix4x4.identity:matrices[b.parent],
                        b.mode,b.x,b.y,b.rotation,b.scaleX,b.scaleY);
                    continue;
                }
                var c=constraints[-step-1];var bone=bones[c.bone];
                if(c.local) {
                    // This source graph reads unconstrained siblings' applied local values.
                    // Scale division is native behavior, including negative mix values.
                    var target=bones[c.target];float sx=bone.scaleX,sy=bone.scaleY;
                    if(c.scaleMix.x!=0&&sx!=0)sx=(sx+(target.scaleX-sx)*c.scaleMix.x)/sx;
                    if(c.scaleMix.y!=0&&sy!=0)sy=(sy+(target.scaleY-sy)*c.scaleMix.y)/sy;
                    matrices[c.bone]=RecoveredWorldRig.Compose(bone.parent<0?Matrix4x4.identity:matrices[bone.parent],
                        bone.mode,bone.x+(target.x-bone.x+c.offset.x)*c.translationMix.x,
                        bone.y+(target.y-bone.y+c.offset.y)*c.translationMix.y,bone.rotation,sx,sy);
                } else {
                    var point=matrices[c.target].MultiplyPoint3x4(c.offset);var value=matrices[c.bone];
                    value.m03+=(point.x-value.m03)*c.translationMix.x;
                    value.m13+=(point.y-value.m13)*c.translationMix.y;
                    matrices[c.bone]=value;
                }
            }
        }
    }
}
