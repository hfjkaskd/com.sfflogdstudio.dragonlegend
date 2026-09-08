using System;
using System.Collections.Generic;
using DragonLegend.Whitebox;
using DragonLegend.Whitebox.Recovered;
using NUnit.Framework;

public sealed class RecoveredSingleSymbolTests
{
    private static RecoveredSlotBoard Board() => new RecoveredSlotBoard(
        new RecoveredGameplayRules(new GoldenDragonAutoGenConfig()));

    [Test]
    public void PlacesOnePerColumnUpdatesReservationsAndStopsAtFive()
    {
        var board = Board(); var columns = new List<int>();
        board.BeginSingleSymbol(8, columns, 10);
        for (int col = 0; col < 5; col++)
        {
            int next = col;
            bool columnDraw = true;
            Assert.IsTrue(board.StepSingleSymbol((min,max) => {
                int value = columnDraw ? next : 2; columnDraw = false; return value;
            }));
            Assert.AreEqual(10,board.GetSymbol(col,2));
            Assert.IsTrue(board.IsReserved(col,2));
        }
        Assert.IsFalse(board.StepSingleSymbol((min,max) => throw new Exception("Unexpected random draw")));
        CollectionAssert.AreEqual(new[]{0,1,2,3,4},columns);
    }

    [Test]
    public void RetryRetainsEarlierColumnCandidatesEvenWhenTargetRowIsReserved()
    {
        var board = Board(); var first = new Queue<int>(new[]{1,0});
        board.BeginSingleSymbol(1,new List<int>(),9);
        Assert.IsTrue(board.StepSingleSymbol((min,max) => first.Dequeue()));
        Assert.IsFalse(board.StepSingleSymbol());
        Assert.IsTrue(board.IsReserved(1,0));
        var forbidden = new List<int>{0};
        board.BeginSingleSymbol(1,forbidden,10);
        Assert.IsTrue(board.StepSingleSymbol((min,max) => 0)); // rejected column adds 0,1,2
        int draw = 0;
        Assert.IsTrue(board.StepSingleSymbol((min,max) => {
            if (draw++ == 0) { Assert.AreEqual(5,max); return 1; }
            Assert.AreEqual(5,max); // old [0,1,2] plus new [1,2]
            return 0;
        }));
        Assert.AreEqual(10,board.GetSymbol(1,0)); // native overwrite, not sanitized
        Assert.IsFalse(board.StepSingleSymbol());
        CollectionAssert.AreEqual(new[]{0,1},forbidden);
    }

    [TestCase(0)] [TestCase(-1)]
    public void NonpositiveCountConsumesNoRandom(int count)
    {
        var board = Board(); board.BeginSingleSymbol(count,new List<int>(),9);
        Assert.IsFalse(board.StepSingleSymbol((min,max) => throw new Exception("Unexpected random draw")));
        Assert.IsFalse(board.IsPlacingSingleSymbol);
    }

    [Test]
    public void RejectedColumnYieldsWithoutSpinningInternally()
    {
        var board = Board(); board.BeginSingleSymbol(1,new List<int>{0},9);
        int calls = 0;
        for (int attempt = 0; attempt < 20; attempt++)
            Assert.IsTrue(board.StepSingleSymbol((min,max) => { calls++; return 0; }));
        Assert.AreEqual(20,calls);
        Assert.IsTrue(board.IsPlacingSingleSymbol);
        Assert.Throws<InvalidOperationException>(() => board.BeginSingleSymbol(1,new List<int>(),10));
    }
}
