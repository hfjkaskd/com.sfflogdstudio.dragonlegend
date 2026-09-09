using UnityEngine.UI;

namespace DragonLegend.Whitebox
{
    // Original EmptyRaycastGraphic: participates in standard UI raycasts without drawing a mesh.
    public sealed class RecoveredEmptyRaycastGraphic : Graphic
    {
        protected override void OnPopulateMesh(VertexHelper helper)=>helper.Clear();
    }
}
