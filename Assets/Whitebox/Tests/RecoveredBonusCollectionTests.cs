using DragonLegend.Whitebox;
using NUnit.Framework;
using UnityEngine;

public sealed class RecoveredBonusCollectionTests
{
    [Test]
    public void OriginalCountSelectsChildrenAndTargetsIndependentlyOfVisibility()
    {
        var root=Object.Instantiate(Resources.Load<GameObject>("RecoveredUI/BonusCollection"));
        try {
            var collection=root.GetComponent<RecoveredBonusCollection>();
            collection.Initialize(new[]{0,1,2,3,-1});
            for(int column=0;column<5;column++) {
                Assert.IsNull(collection.GetUnselectedTarget(column,0));
                Assert.IsNull(collection.GetUnselectedTarget(column,-1));
                Assert.IsNull(collection.GetUnselectedTarget(column,3));
                for(int count=1;count<=2;count++) {
                    var item=collection.GetUnselectedTarget(column,count);
                    Assert.AreSame(root.transform.GetChild(column).GetChild(count-1),item);
                    Assert.AreEqual((new[]{0,1,2,3,-1})[column]>=count,item.GetChild(0).gameObject.activeSelf);
                    Assert.AreEqual(new Vector2(54,55),item.sizeDelta);
                    Assert.AreEqual(new Vector3(.21f,.21f,.2f),item.GetChild(0).localScale);
                }
            }
            var rect=(RectTransform)root.transform;
            Assert.AreEqual(new Vector2(-.46f,245.21f),rect.anchoredPosition);
            Assert.AreEqual(5,root.transform.childCount);
        } finally {Object.DestroyImmediate(root);}
    }
}
