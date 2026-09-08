using System;
using UnityEngine;

namespace DragonLegend.Whitebox
{
    // Reusable geometric clipping for the original convex four-vertex Wild light mask.
    // UVs are interpolated at each intersection; the mask is not approximated by its bounds.
    public sealed class RecoveredConvexClipper
    {
        public readonly struct Vertex
        {
            public readonly Vector3 position;
            public readonly Vector2 uv;
            public Vertex(Vector3 position, Vector2 uv) { this.position=position; this.uv=uv; }
        }
        private readonly Vector2[] boundary=new Vector2[4];
        private Vertex[] input=new Vertex[8], output=new Vertex[8];
        private int boundaryCount;
        private float winding;
        private bool empty;
        public int Count { get; private set; }
        public Vertex At(int index)=>input[index];
        public void SetBoundary(Vector3[] points,int count)
        {
            if(count!=4)throw new InvalidOperationException("The authored light requires a convex quadrilateral");
            boundaryCount=count;float area=0;
            for(int i=0;i<count;i++) {boundary[i]=points[i];var next=points[(i+1)%count];area+=points[i].x*next.y-next.x*points[i].y;}
            winding=area<0?-1:1;empty=Mathf.Abs(area)<.00000001f;
        }
        private static float Cross(Vector2 a,Vector2 b)=>a.x*b.y-a.y*b.x;
        public void Clip(Vertex a,Vertex b,Vertex c)
        {
            Count=0;if(empty)return;
            input[0]=a;input[1]=b;input[2]=c;int size=3;
            for(int edge=0;edge<boundaryCount&&size>0;edge++) {
                Vector2 start=boundary[edge],direction=boundary[(edge+1)%boundaryCount]-start;
                int nextSize=0;var previous=input[size-1];
                float before=winding*Cross(direction,(Vector2)previous.position-start);
                for(int v=0;v<size;v++) {
                    var current=input[v];float after=winding*Cross(direction,(Vector2)current.position-start);
                    if((before>=0)!=(after>=0)) {
                        float t=before/(before-after);
                        output[nextSize++]=new Vertex(Vector3.LerpUnclamped(previous.position,current.position,t),Vector2.LerpUnclamped(previous.uv,current.uv,t));
                    }
                    if(after>=0)output[nextSize++]=current;
                    previous=current;before=after;
                }
                var swap=input;input=output;output=swap;size=nextSize;
            }
            Count=size;
        }
    }
}
