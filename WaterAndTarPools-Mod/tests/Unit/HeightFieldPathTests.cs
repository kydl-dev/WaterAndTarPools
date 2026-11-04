using System;
using WaterAndTarPools.Drain;

namespace WaterAndTarPools.Tests.Unit;

/// <summary>
/// The text of the pour log (what the game log and the bug reports show), the direction of the flow on an uneven grid and the pool numbers.
/// Same synthetic tanks as HeightFieldTests.
/// </summary>
[TestFixture]
public class HeightFieldPathTests
{
    const int W = 40;
    const int Stride = W + 1, N = Stride * Stride, C = W / 2;
    const float Eps = 1e-3f;

    static int Cell(int dx, int dy) { return (C + dy) * Stride + C + dx; }

    static float[] Tank(int r, float top, Func<int, int, float?> shape = null)
    {
        var h = new float[N];
        for (int y = 0; y <= W; y++)
            for (int x = 0; x <= W; x++)
            {
                int dx = x - C, dy = y - C, cheb = Math.Max(Math.Abs(dx), Math.Abs(dy));
                float v = cheb <= r ? 0f : cheb <= r + 3 ? top : -0.5f;
                var s = shape != null ? shape(dx, dy) : null;
                h[y * Stride + x] = s ?? v;
            }
        return h;
    }

    static float Capacity(float[] h, int r, float rim)
    {
        double s = 0;
        for (int dy = -r; dy <= r; dy++) for (int dx = -r; dx <= r; dx++) s += Math.Max(0f, rim - h[Cell(dx, dy)]);
        return (float)s;
    }

    static HeightField.FillResult Pour(float[] d, float[] h, float vol, bool rim = false, int at = -1)
    {
        var r = HeightField.Fill(d, h, W, at < 0 ? Cell(0, 0) : at, vol, new HeightField.FillOptions { RimDisappear = rim, MaxDepth = 12f });
        Assert.That(Math.Abs(r.Residual), Is.LessThan(Eps * Math.Max(1f, vol)), "accounting must balance: " + r.Path);
        return r;
    }

    // ---------------------------------------------------------------- the log text

    [Test]
    public void Path_ClosedTank_RunsOverTheRim_DescribesPoolAndBasins()
    {
        var h = Tank(8, 1f); var d = new float[N];
        float cap = Capacity(h, 8, 1f);
        var r = Pour(d, h, cap * 2f);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(r.Path, Does.StartWith("pool: floor 0.000, rim 1.000, "));
            Assert.That(r.Path, Does.Contain(" cells, enclosed"));
            Assert.That(r.Path, Does.Not.Contain("not enclosed"));
            Assert.That(r.Path, Does.Contain(" | basin 1: "));
            Assert.That(r.Path, Does.Contain(" cells, level 1.000"));
            Assert.That(r.Path, Does.Contain(", runs over the RIM"));
            Assert.That(r.Path, Does.Contain(" | basin 2 (outside the rim): "));
            Assert.That(r.Pool.Cells, Is.EqualTo(17 * 17));
        }
    }

    [Test]
    public void Path_RimOn_SaysTheExcessWasRemoved()
    {
        var h = Tank(8, 1f); var d = new float[N];
        var r = Pour(d, h, Capacity(h, 8, 1f) * 2f, rim: true);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(r.Path, Does.Contain(", full to the rim (excess removed)"));
            Assert.That(r.Path, Does.Not.Contain("outside the rim)"));
            Assert.That(r.RimCrossings, Is.EqualTo(0));
        }
    }

    [Test]
    public void Path_ThinFilmOutside_IsNamed_AndNothingElseIsClaimed()
    {
        var h = Tank(8, 1f); var d = new float[N];
        Pour(d, h, Capacity(h, 8, 1f));
        var r = Pour(d, h, 2f);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(r.Path, Does.Contain(", thin film outside the rim, not tracked"));
            Assert.That(r.Path, Does.Not.Contain("full to the rim"));
            Assert.That(r.Path, Does.Not.Contain("leaves the grid"));
        }
    }

    [Test]
    public void Path_ADentInFlatGround_IsNotEnclosed()
    {
        var h = Tank(8, 0.05f); var d = new float[N];
        var r = Pour(d, h, 10f);
        Assert.That(r.Path, Does.Contain("not enclosed (just runs downhill)"));
        Assert.That(r.Pool.Enclosed, Is.False);
    }

    [Test]
    public void Path_InnerLip_IsNotTheRim()
    {
        var h = Tank(12, 1f, (dx, dy) => Math.Max(Math.Abs(dx), Math.Abs(dy)) <= 12 ? (float?)(dx == 0 ? 0.03f : dx > 0 ? -0.01f : 0f) : null);
        var d = new float[N];
        var r = Pour(d, h, 12 * 25 * 0.03f * 1.3f, at: Cell(-6, 0));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(r.Path, Does.Contain(", runs over an inner lip"));
            Assert.That(r.Path, Does.Not.Contain("runs over the RIM"));
            Assert.That(r.RimCrossings, Is.EqualTo(0));
        }
    }

    [Test]
    public void Path_NoReachableCell_IsReported()
    {
        var h = new float[N];
        for (int i = 0; i < N; i++) h[i] = -1f;
        var d = new float[N];
        var r = HeightField.Fill(d, h, W, Cell(0, 0), 5f, new HeightField.FillOptions { MaxDepth = 12f, Buried = 0f });
        using (Assert.EnterMultipleScope())
        {
            Assert.That(r.Path, Is.EqualTo("no reachable cell near the landing point"));
            Assert.That(r.Unplaced, Is.EqualTo(5f));
        }
    }

    // ---------------------------------------------------------------- how the pour is booked

    [Test]
    public void Booking_ClosedTankOverTheRim_PassesStartAndAccounts()
    {
        var h = Tank(8, 1f); var d = new float[N];
        float cap = Capacity(h, 8, 1f);
        var r = Pour(d, h, cap * 2f);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(r.Start, Is.EqualTo(Cell(0, 0)));
            Assert.That(r.Passes, Is.EqualTo(2), r.Path);
            Assert.That(r.Retained, Is.EqualTo(cap).Within(cap * 0.01f), "the pool filled in the landing basin");
            Assert.That(r.OtherBasins, Is.EqualTo(0f).Within(1e-3f));
            Assert.That(r.Unplaced, Is.EqualTo(0f).Within(1e-3f));
            Assert.That(r.EdgeCell, Is.EqualTo(-1).Or.GreaterThanOrEqualTo(0));
            Assert.That(r.Accounted, Is.EqualTo(r.Poured).Within(1e-2f));
        }
    }

    [Test]
    public void Booking_RimOn_NoEdgeCellAndOnePass()
    {
        var h = Tank(8, 1f); var d = new float[N];
        var r = Pour(d, h, Capacity(h, 8, 1f) * 2f, rim: true);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(r.EdgeCell, Is.EqualTo(-1));
            Assert.That(r.Passes, Is.EqualTo(1));
            Assert.That(r.Retained, Is.GreaterThan(0f));
            Assert.That(r.ToEdge, Is.EqualTo(0f));
        }
    }

    [Test]
    public void Fill_NoReachableCell_LeavesTheMarkersUnset()
    {
        var h = new float[N];
        for (int i = 0; i < N; i++) h[i] = -1f;
        var r = HeightField.Fill(new float[N], h, W, Cell(0, 0), 5f, new HeightField.FillOptions { MaxDepth = 12f, Buried = 0f });
        using (Assert.EnterMultipleScope())
        {
            Assert.That(r.Start, Is.EqualTo(-1));
            Assert.That(r.EdgeCell, Is.EqualTo(-1));
            Assert.That(r.Passes, Is.EqualTo(0));
            Assert.That(r.Pool, Is.Null);
        }
    }

    [Test]
    public void Fill_LandingOnACellExactlyAtTheBuriedLevel_MovesToTheNearestOpenCell()
    {
        var h = new float[N];
        for (int i = 0; i < N; i++) h[i] = 1f;
        h[Cell(0, 0)] = 0f;                         // Buried = 0: this cell is solid ground
        var r = HeightField.Fill(new float[N], h, W, Cell(0, 0), 1f, new HeightField.FillOptions { MaxDepth = 12f, Buried = 0f });
        Assert.That(r.Start, Is.Not.EqualTo(Cell(0, 0)));
        Assert.That(Math.Abs(r.Start % Stride - C) + Math.Abs(r.Start / Stride - C), Is.EqualTo(1), "a direct neighbour");
    }

    [Test]
    public void Fill_CellsExactlyAtTheBuriedLevel_AreNotCandidates()
    {
        var h = new float[N];                       // everything is at the Buried level (solid) ...
        h[Cell(3, 0)] = 1f;                         // ... except one open cell three away
        var r = HeightField.Fill(new float[N], h, W, Cell(0, 0), 1f, new HeightField.FillOptions { MaxDepth = 12f, Buried = 0f });
        Assert.That(r.Start, Is.EqualTo(Cell(3, 0)));
    }

    // ---------------------------------------------------------------- the pool numbers

    [Test]
    public void Pool_FloorRimDepthAndCells_OfAnUnevenTank()
    {
        var h = Tank(8, 1f, (dx, dy) => Math.Max(Math.Abs(dx), Math.Abs(dy)) <= 8 ? -0.3f : (float?)null);
        var d = new float[N];
        var r = Pour(d, h, 5f);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(r.Pool.Floor, Is.EqualTo(-0.3f).Within(1e-4));
            Assert.That(r.Pool.Rim, Is.EqualTo(1f).Within(1e-4));
            Assert.That(r.Pool.Depth, Is.EqualTo(1.3f).Within(1e-4), "rim minus floor");
            Assert.That(r.Pool.Enclosed, Is.True);
            Assert.That(r.Pool.Cells, Is.EqualTo(17 * 17));
            Assert.That(r.Pool.Low, Is.GreaterThanOrEqualTo(0));
            Assert.That(r.Pool.RimCell, Is.GreaterThanOrEqualTo(0));
        }
    }

    // ---------------------------------------------------------------- the flow leaves on the right side (all four borders)

    /// Ground outside the ring falls evenly towards ONE border; sx / sy = direction of the fall.
    static float[] TankFallingTowards(int sx, int sy)
    {
        return Tank(8, 1f, (dx, dy) =>
        {
            int cheb = Math.Max(Math.Abs(dx), Math.Abs(dy));
            return cheb > 11 ? -0.5f - 0.1f * (sx * dx + sy * dy + 2 * C) : (float?)null;
        });
    }

    [TestCase(1, 0)]
    [TestCase(-1, 0)]
    [TestCase(0, 1)]
    [TestCase(0, -1)]
    public void FullTank_OnASlope_LeavesAtTheBorderItFallsTo(int sx, int sy)
    {
        var h = TankFallingTowards(sx, sy); var d = new float[N];
        Pour(d, h, Capacity(h, 8, 1f));
        var r = Pour(d, h, 100f);
        int ex = r.EdgeCell % Stride, ey = r.EdgeCell / Stride;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(r.ToEdge, Is.GreaterThan(90f), r.Path);
            Assert.That(r.EdgeCell, Is.GreaterThanOrEqualTo(0), r.Path);
            Assert.That(sx != 0 ? (sx > 0 ? ex == W : ex == 0) : (sy > 0 ? ey == W : ey == 0), Is.True, "edge cell " + ex + "," + ey + " " + r.Path);
            Assert.That(r.Path, Does.Contain(", leaves the grid at cell " + r.EdgeCell));
        }
    }

    // ---------------------------------------------------------------- a basin that is not in the middle of the grid

    [TestCase(1, 1)]
    [TestCase(-1, 1)]
    [TestCase(1, -1)]
    [TestCase(-1, -1)]
    public void OffCentreRectangularPool_FillsExactlyItsOwnCells(int qx, int qy)
    {
        // a 6 x 2 trough in one quadrant (walls 1 m, ground 1 m around, no outside lower than the walls)
        var h = new float[N];
        for (int i = 0; i < N; i++) h[i] = 1f;
        int x0 = qx > 0 ? 5 : -10, y0 = qy > 0 ? 12 : -13;
        for (int dy = 0; dy < 2; dy++) for (int dx = 0; dx < 6; dx++) h[Cell(x0 + dx, y0 + dy)] = 0f;
        var d = new float[N];
        var r = HeightField.Fill(d, h, W, Cell(x0 + 5, y0 + 1), 6f, new HeightField.FillOptions { MaxDepth = 12f });   // exactly half full
        int wet = 0;
        for (int i = 0; i < N; i++) if (d[i] > 1e-4f) wet++;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(r.Path, Does.Contain(" | basin 1: 12 cells, level 0.500"));
            Assert.That(wet, Is.EqualTo(12), r.Path);
            Assert.That(d[Cell(x0, y0)], Is.EqualTo(0.5f).Within(1e-3f));
            Assert.That(d[Cell(x0 + 5, y0 + 1)], Is.EqualTo(0.5f).Within(1e-3f));
            Assert.That(d[Cell(x0 - 1, y0)], Is.EqualTo(0f));
            Assert.That(d[Cell(x0 + 6, y0 + 1)], Is.EqualTo(0f));
        }
    }
}
