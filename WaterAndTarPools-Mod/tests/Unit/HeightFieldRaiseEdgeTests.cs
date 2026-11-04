using System;
using System.Collections.Generic;
using WaterAndTarPools.Drain;

namespace WaterAndTarPools.Tests.Unit;

/// <summary>A new wall next to the grid border, an old top below the ground, and the search for the nearest open cell across the whole grid.</summary>
[TestFixture]
public class HeightFieldRaiseEdgeTests
{
    const int W = 10;
    const int Stride = W + 1, N = Stride * Stride;

    static int Cell(int x, int y) { return y * Stride + x; }

    // a wall at (wx, wy) with 1.0 of liquid under it; all four neighbours are open
    static float[] PushFrom(int wx, int wy, out HeightField.RaiseResult r)
    {
        var h = new float[N];
        var d = new float[N];
        d[Cell(wx, wy)] = 1f;
        r = HeightField.RaiseCells(h, d, W, new Dictionary<int, float> { { Cell(wx, wy), 2f } }, null, null);
        return d;
    }

    [TestCase(1, 5, 0, 5)]        // the neighbour is the first column
    [TestCase(W - 1, 5, W, 5)]    // the neighbour is the last column
    [TestCase(5, 1, 5, 0)]        // first row
    [TestCase(5, W - 1, 5, W)]    // last row
    public void WallNextToTheGridBorder_SharesWithTheBorderCellToo(int wx, int wy, int bx, int by)
    {
        HeightField.RaiseResult r;
        float[] d = PushFrom(wx, wy, out r);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(d[Cell(bx, by)], Is.EqualTo(0.25f).Within(1e-5f), "the border cell is a neighbour like any other");
            Assert.That(d[Cell(wx, wy)], Is.EqualTo(0f));
            Assert.That(r.Displaced, Is.EqualTo(1f).Within(1e-5f));
            Assert.That(r.DisplacedCells, Is.EqualTo(1));
            Assert.That(r.Far, Is.EqualTo(0f));
        }
    }

    [Test]
    public void WallOnTheBorderCell_SharesOnlyWithItsThreeInGridNeighbours()
    {
        HeightField.RaiseResult r;
        float[] d = PushFrom(0, 5, out r);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(d[Cell(1, 5)], Is.EqualTo(1f / 3f).Within(1e-5f));
            Assert.That(d[Cell(0, 4)], Is.EqualTo(1f / 3f).Within(1e-5f));
            Assert.That(d[Cell(0, 6)], Is.EqualTo(1f / 3f).Within(1e-5f));
            Assert.That(r.Displaced, Is.EqualTo(1f).Within(1e-5f));
        }
    }

    [Test]
    public void WallOnTheFarCorner_SharesWithTwoNeighbours()
    {
        HeightField.RaiseResult r;
        float[] d = PushFrom(W, W, out r);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(d[Cell(W - 1, W)], Is.EqualTo(0.5f).Within(1e-5f));
            Assert.That(d[Cell(W, W - 1)], Is.EqualTo(0.5f).Within(1e-5f));
        }
    }

    [Test]
    public void OldTopBelowTheGround_DoesNotLowerTheBase()
    {
        var h = new float[N];
        var d = new float[N];
        int wall = Cell(5, 5);
        h[wall] = 1f;                              // ground at 1
        d[wall] = 1.5f;                            // liquid stands on it (surface 2.5)
        var prevTop = new Dictionary<int, float> { { wall, 0.5f } };   // an old top far below the ground
        HeightField.RaiseResult r = HeightField.RaiseCells(h, d, W, new Dictionary<int, float> { { wall, 2f } }, prevTop, null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(d[wall], Is.EqualTo(0.5f).Within(1e-5f), "base stays the ground (1): the liquid up to the top (2) is above the wall");
            Assert.That(r.Displaced, Is.EqualTo(1f).Within(1e-5f));
            Assert.That(r.KeptDepth, Is.EqualTo(0.5f).Within(1e-5f));
        }
    }

    [Test]
    public void NoPreviousTops_IsFine()
    {
        HeightField.RaiseResult r;
        PushFrom(5, 5, out r);
        Assert.That(r.Raised, Is.EqualTo(1));
    }

    [Test]
    public void NearestOpenCells_AcrossTheWholeGrid_ReachesTheOnlyOpenCorner()
    {
        for (int corner = 0; corner < 4; corner++)
        {
            int ox = (corner & 1) == 0 ? 0 : W, oy = (corner & 2) == 0 ? 0 : W;
            var isWall = new bool[N];
            var walls = new List<int>();
            for (int i = 0; i < N; i++) { isWall[i] = true; walls.Add(i); }
            isWall[Cell(ox, oy)] = false;
            walls.Remove(Cell(ox, oy));
            int[] target = HeightField.NearestOpenCells(isWall, walls, W);
            foreach (int i in walls)
                Assert.That(target[i], Is.EqualTo(Cell(ox, oy)), "corner " + corner + " cell " + i);
        }
    }

    [Test]
    public void NearestOpenCells_PicksTheCloserOfTwoOpenCells()
    {
        var isWall = new bool[N];
        var walls = new List<int>();
        for (int y = 3; y <= 7; y++) for (int x = 3; x <= 7; x++) { isWall[Cell(x, y)] = true; walls.Add(Cell(x, y)); }
        int[] target = HeightField.NearestOpenCells(isWall, walls, W);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(target[Cell(3, 5)], Is.EqualTo(Cell(2, 5)));
            Assert.That(target[Cell(7, 5)], Is.EqualTo(Cell(8, 5)));
            Assert.That(target[Cell(5, 3)], Is.EqualTo(Cell(5, 2)));
            Assert.That(target[Cell(5, 7)], Is.EqualTo(Cell(5, 8)));
        }
    }
}
