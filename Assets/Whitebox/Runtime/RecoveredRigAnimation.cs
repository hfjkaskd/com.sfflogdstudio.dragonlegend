using System;
using UnityEngine;
namespace DragonLegend.Whitebox
{
    public sealed class RecoveredRigAnimation:ScriptableObject
    {
        [Serializable] public sealed class Channel {public bool slot;public int index,component;public AnimationCurve curve;}
        public Channel[] channels;
        public void Sample(float time,RecoveredRegionRig rig)
        {
            for(int i=0;i<channels.Length;i++) {
                var channel=channels[i];float value=channel.curve.Evaluate(time);
                if(channel.slot) {
                    var slot=rig.slots[channel.index];if(channel.component<0)slot.attachment=value;else slot.tint[channel.component]=value;
                } else {
                    var bone=rig.bones[channel.index];
                    switch(channel.component){case 0:bone.rotation=value;break;case 1:bone.x=value;break;case 2:bone.y=value;break;case 3:bone.scaleX=value;break;case 4:bone.scaleY=value;break;}
                }
            }
            rig.RefreshPose();
        }
    }
}
