using System;
using UnityEngine;
namespace DragonLegend.Whitebox
{
    public sealed class RecoveredRigAnimation:ScriptableObject
    {
        [Serializable] public sealed class Channel {public bool slot;public int index,component;public AnimationCurve curve;}
        [Serializable] public sealed class SequenceFrame {public float time,delay;public int mode,index;}
        [Serializable] public sealed class SequenceChannel {public int slot,attachment,count;public SequenceFrame[] frames;}
        public Channel[] channels;
        public SequenceChannel[] sequences;
        public static int SequenceIndex(SequenceFrame frame,float time,int count)
        {
            int index=frame.index;
            if(frame.mode!=0)index+=(int)((time-frame.time)/frame.delay+0.00001f);
            switch(frame.mode) {
                case 0:return index;
                case 1:return Math.Min(count-1,index);
                case 2:return index%count;
                case 3:{int length=count*2-2;int value=length==0?0:index%length;return value>=count?length-value:value;}
                case 4:return Math.Max(count-1-index,0);
                case 5:return count-1-index%count;
                case 6:{int length=count*2-2;int value=length==0?0:(index+count-1)%length;return value>=count?length-value:value;}
                default:throw new InvalidOperationException("Unsupported sequence mode");
            }
        }
        public void Sample(float time,RecoveredRegionRig rig)
        {
            for(int i=0;i<channels.Length;i++) {
                var channel=channels[i];float value=channel.curve.Evaluate(time);
                if(channel.slot) {
                    var slot=rig.slots[channel.index];if(channel.component<0) {
                        if(slot.attachment!=value)slot.sequenceIndex=-1;
                        slot.attachment=value;
                    } else slot.tint[channel.component]=value;
                } else {
                    var bone=rig.bones[channel.index];
                    switch(channel.component){case 0:bone.rotation=value;break;case 1:bone.x=value;break;case 2:bone.y=value;break;case 3:bone.scaleX=value;break;case 4:bone.scaleY=value;break;}
                }
            }
            if(sequences!=null)foreach(var sequence in sequences) {
                var slot=rig.slots[sequence.slot];if(slot.attachment!=sequence.attachment)continue;
                int frame=sequence.frames.Length-1;
                while(frame>=0&&time<sequence.frames[frame].time)frame--;
                slot.sequenceIndex=frame<0?-1:SequenceIndex(sequence.frames[frame],time,sequence.count);
            }
            rig.RefreshPose();
        }
    }
}
