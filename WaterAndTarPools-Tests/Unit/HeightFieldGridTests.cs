using System.Collections.Generic;
using System.Linq;
using WaterAndTarPools.Drain;

namespace WaterAndTarPools.Tests.Unit
{
    /// <summary>
    /// The building blocks of HeightField on a tiny 9 x 9 grid where every number can be worked out by hand: where a basin ends (its rim), which cells
    /// are inside it, where liquid runs downhill, where a pour starts, and how a wall pushes liquid aside. The bigger scenarios are in HeightFieldTests.
    /// Border cells of the grid are x or y of 0, 1, 7 and 8 (the lip of the grid), interior cells are 2 to 6.
    /// </summary>
    [TestFixture]
    public class HeightFieldGridTests
    {
        const int W = 8;
        const int Stride = W + 1, N = Stride * Stride;
        const float Eps = 1e-4f;

        static int Cell(int x, int y) { return y * Stride + x; }
        static float[] Flat(float h) { var a = new float[N]; for (int i = 0; i < N; i++) a[i] = h; return a; }
        static bool OnBorder(int cell) { int x = cell % Stride, y = cell / Stride; return x <= 1 || y <= 1 || x >= W - 1 || y >= W - 1; }

        // ------------------------------------------------------------------------------------------------ FindBasin

        [Test]
        public void FindBasin_BowlInFlatGround_RimIsTheGroundPlusTheBorderLip()
        {
            var h = Flat(1f); h[Cell(4, 4)] = 0f; var d = new float[N];

            var b = HeightField.FindBasin(d, h, W, Cell(4, 4), float.NegativeInfinity);

            Assert.That(b.Rim, Is.EqualTo(1f + HeightField.MinSpillDepth).Within(Eps), "the liquid leaves over the border cells, which carry the lip");
            Assert.That(b.Floor, Is.EqualTo(0f));
            Assert.That(b.Low, Is.EqualTo(Cell(4, 4)));
            Assert.That(b.Depth, Is.EqualTo(1f + HeightField.MinSpillDepth).Within(Eps));
            Assert.That(b.Enclosed, Is.True);
            Assert.That(OnBorder(b.RimCell), Is.True, "the rim cell is where the liquid leaves the grid");
        }

        [Test]
        public void FindBasin_BowlInFlatGround_InsideIsEveryInteriorCellAndNoBorderCell()
        {
            var h = Flat(1f); h[Cell(4, 4)] = 0f; var d = new float[N];

            var b = HeightField.FindBasin(d, h, W, Cell(4, 4), float.NegativeInfinity);

            Assert.That(b.Cells, Is.EqualTo(25), "the 5 x 5 interior");
            Assert.That(b.Inside.Count(x => x), Is.EqualTo(25));
            for (int i = 0; i < N; i++) Assert.That(b.Inside[i], Is.EqualTo(!OnBorder(i)), "cell " + (i % Stride) + "," + (i / Stride));
        }

        [TestCase(0.10f, false)]
        [TestCase(0.12f, true)]
        public void FindBasin_Enclosed_NeedsTheRimToBeAtLeast15cmOverTheFloor(float ground, bool enclosed)
        {
            var h = Flat(ground); h[Cell(4, 4)] = 0f; var d = new float[N];

            var b = HeightField.FindBasin(d, h, W, Cell(4, 4), float.NegativeInfinity);

            Assert.That(b.Enclosed, Is.EqualTo(enclosed), "rim " + b.Rim + ", floor " + b.Floor);
        }

        [Test]
        public void FindBasin_ASolidColumn_StopsTheFlood()
        {
            var h = Flat(1f); h[Cell(2, 4)] = 0f; var d = new float[N];
            for (int y = 0; y <= W; y++) h[Cell(5, y)] = -5f;               // exactly the buried level: solid

            var b = HeightField.FindBasin(d, h, W, Cell(2, 4), -5f);

            Assert.That(b.Inside[Cell(4, 4)], Is.True);
            Assert.That(b.Inside[Cell(5, 4)], Is.False, "the solid column itself is never part of the pool");
            Assert.That(b.Inside[Cell(6, 4)], Is.False, "behind the solid column is not reachable");
            Assert.That(b.Cells, Is.EqualTo(15), "x 2 to 4, y 2 to 6: only the interior cells on this side of the column");
        }

        [Test]
        public void FindBasin_WaterAlreadyInTheLowCell_RaisesWhereTheFloodStarts()
        {
            var h = Flat(0.2f); h[Cell(4, 4)] = 0f; var d = new float[N]; d[Cell(4, 4)] = 1f;

            var b = HeightField.FindBasin(d, h, W, Cell(4, 4), float.NegativeInfinity);

            Assert.That(b.Rim, Is.EqualTo(1f).Within(Eps), "the surface at the low cell (ground + liquid) is the starting level, the rim cannot be lower");
        }

        [Test]
        public void FindBasin_LiquidOnTheSurroundingCells_RaisesTheRim()
        {
            var h = Flat(0.2f); var d = Flat(0.5f); h[Cell(4, 4)] = 0f;

            var b = HeightField.FindBasin(d, h, W, Cell(4, 4), float.NegativeInfinity);

            Assert.That(b.Rim, Is.EqualTo(0.2f + 0.5f + HeightField.MinSpillDepth).Within(Eps), "the surface is ground + liquid");
        }

        [Test]
        public void FindBasin_RimPassesOverTheLowestGapOfARing()
        {
            var h = Flat(0f); var d = new float[N];
            for (int y = 2; y <= 6; y++) for (int x = 2; x <= 6; x++) if (x == 2 || x == 6 || y == 2 || y == 6) h[Cell(x, y)] = 1f;   // ring of 1 m
            h[Cell(6, 4)] = 0.4f;                                                                                                     // a gap in the ring

            var b = HeightField.FindBasin(d, h, W, Cell(4, 4), float.NegativeInfinity);

            Assert.That(b.Rim, Is.EqualTo(0.4f).Within(Eps), "the liquid leaves through the lowest point of the ring");
            Assert.That(b.Cells, Is.EqualTo(9), "the 3 x 3 inside the ring; the gap cell itself is at the rim level, not below it");
            Assert.That(b.Inside[Cell(6, 4)], Is.False);
            Assert.That(b.Inside[Cell(4, 4)], Is.True);
            Assert.That(b.Inside[Cell(2, 2)], Is.False, "the ring itself is higher than the level the liquid reaches");
        }

        // ------------------------------------------------------------------------------------------------ Descend

        [TestCase(6, 4, 1, 4, true)]
        [TestCase(2, 4, 7, 4, false)]
        public void Descend_DownASlopeInX_StopsAtTheLastInteriorCellOfTheGrid(int sx, int sy, int ex, int ey, bool risesWithX)
        {
            var h = new float[N]; var d = new float[N];
            for (int y = 0; y <= W; y++) for (int x = 0; x <= W; x++) h[Cell(x, y)] = risesWithX ? x : W - x;

            Assert.That(HeightField.Descend(d, h, W, Cell(sx, sy), float.NegativeInfinity), Is.EqualTo(Cell(ex, ey)));
        }

        [TestCase(4, 6, 4, 1, true)]
        [TestCase(4, 2, 4, 7, false)]
        public void Descend_DownASlopeInY_StopsAtTheLastInteriorCellOfTheGrid(int sx, int sy, int ex, int ey, bool risesWithY)
        {
            var h = new float[N]; var d = new float[N];
            for (int y = 0; y <= W; y++) for (int x = 0; x <= W; x++) h[Cell(x, y)] = risesWithY ? y : W - y;

            Assert.That(HeightField.Descend(d, h, W, Cell(sx, sy), float.NegativeInfinity), Is.EqualTo(Cell(ex, ey)));
        }

        [Test]
        public void Descend_LiquidCountsAsHeight()
        {
            var h = new float[N]; var d = new float[N];
            d[Cell(4, 4)] = 2f; d[Cell(5, 4)] = 1f;                         // the surface at 4,4 is 2.0, at 5,4 it is 1.0, everywhere else 0

            var end = HeightField.Descend(d, h, W, Cell(4, 4), float.NegativeInfinity);

            Assert.That(h[end] + d[end], Is.EqualTo(0f), "it runs on past the lower surface to the flat ground");
        }

        [Test]
        public void Descend_DifferenceBelowHalfAMillimetre_IsLevel()
        {
            var h = new float[N]; var d = new float[N];
            h[Cell(3, 4)] = -0.0004f;

            Assert.That(HeightField.Descend(d, h, W, Cell(4, 4), float.NegativeInfinity), Is.EqualTo(Cell(4, 4)));
        }

        [Test]
        public void Descend_DifferenceOfAMillimetre_IsAStep()
        {
            var h = new float[N]; var d = new float[N];
            h[Cell(3, 4)] = -0.001f;

            Assert.That(HeightField.Descend(d, h, W, Cell(4, 4), float.NegativeInfinity), Is.EqualTo(Cell(3, 4)));
        }

        [Test]
        public void Descend_SolidCell_IsNeverEntered()
        {
            var h = new float[N]; var d = new float[N];
            h[Cell(3, 4)] = -5f;                                              // exactly the buried level

            Assert.That(HeightField.Descend(d, h, W, Cell(4, 4), -5f), Is.EqualTo(Cell(4, 4)));
        }

        [Test]
        public void Descend_PicksTheLowestOfSeveralNeighbours()
        {
            var h = new float[N]; var d = new float[N];
            h[Cell(5, 4)] = -1f; h[Cell(4, 3)] = -3f; h[Cell(3, 4)] = -2f; h[Cell(4, 5)] = -0.5f;

            var end = HeightField.Descend(d, h, W, Cell(4, 4), float.NegativeInfinity);

            Assert.That(end, Is.EqualTo(Cell(4, 3)));
        }

        // ------------------------------------------------------------------------------------------------ where a pour starts

        [TestCase(0, 0, 1, 1)]
        [TestCase(8, 8, 7, 7)]
        [TestCase(0, 8, 1, 7)]
        [TestCase(8, 0, 7, 1)]
        [TestCase(4, 4, 4, 4)]
        public void Fill_StartOnTheEdgeOfTheGrid_BeginsAtTheNearestInteriorCell(int sx, int sy, int ex, int ey)
        {
            var h = new float[N]; var d = new float[N];

            var r = HeightField.Fill(d, h, W, Cell(sx, sy), 1f, new HeightField.FillOptions());

            Assert.That(r.Start, Is.EqualTo(Cell(ex, ey)));
        }

        [Test]
        public void Fill_StartInsideSolidGround_BeginsAtTheNearestCellThatIsNot()
        {
            var h = new float[N]; var d = new float[N];
            for (int y = 3; y <= 5; y++) for (int x = 3; x <= 5; x++) h[Cell(x, y)] = -10f;   // a solid 3 x 3 block around the landing point

            var r = HeightField.Fill(d, h, W, Cell(4, 4), 1f, new HeightField.FillOptions { Buried = -5f });

            Assert.That(r.Start, Is.EqualTo(Cell(4, 2)), "two cells away; of the four equally near cells the first one in reading order wins");
        }

        [TestCase(3, 4, 7, 4)]
        [TestCase(4, 1, 4, 5)]
        [TestCase(4, 7, 4, 3)]
        [TestCase(7, 4, 3, 4)]
        public void Fill_StartInsideSolidGround_LooksUpToFourCellsAway(int sx, int sy, int ex, int ey)
        {
            var h = Flat(-10f); var d = new float[N];
            h[Cell(ex, ey)] = 0f;                                              // the only cell that is not solid, exactly four cells off in one direction

            var r = HeightField.Fill(d, h, W, Cell(sx, sy), 1f, new HeightField.FillOptions { Buried = -5f });

            Assert.That(r.Start, Is.EqualTo(Cell(ex, ey)));
            Assert.That(r.Unplaced, Is.EqualTo(0f));
        }

        [Test]
        public void Fill_NoCellThatIsNotSolidNearby_LeavesEverythingUnplaced()
        {
            var h = Flat(-10f); var d = new float[N];

            var r = HeightField.Fill(d, h, W, Cell(4, 4), 5f, new HeightField.FillOptions { Buried = -5f });

            Assert.That(r.Unplaced, Is.EqualTo(5f));
            Assert.That(r.Path, Does.Contain("no reachable cell"));
            Assert.That(d.Sum(), Is.EqualTo(0f), "nothing was written");
        }

        // ------------------------------------------------------------------------------------------------ walls pushing liquid aside

        static HeightField.RaiseResult Raise(float[] h, float[] d, IDictionary<int, float> raise, IDictionary<int, float> prev = null, ICollection<int> last = null)
        {
            return HeightField.RaiseCells(h, d, W, raise, prev, last);
        }

        [Test]
        public void RaiseCells_PieceBarelyOverTheGround_IsNotRaised()
        {
            var h = new float[N]; var d = new float[N]; d[Cell(4, 4)] = 1f;

            var r = Raise(h, d, new Dictionary<int, float> { { Cell(4, 4), HeightField.WallMinRise } });

            Assert.That(r.Raised, Is.EqualTo(0));
            Assert.That(h[Cell(4, 4)], Is.EqualTo(0f));
            Assert.That(d[Cell(4, 4)], Is.EqualTo(1f));
        }

        [Test]
        public void RaiseCells_CellsOutsideTheGrid_AreIgnored()
        {
            var h = new float[N]; var d = new float[N];

            var r = Raise(h, d, new Dictionary<int, float> { { -1, 5f }, { N, 5f }, { N + 3, 5f } });

            Assert.That(r.Raised, Is.EqualTo(0));
        }

        [Test]
        public void RaiseCells_LiquidUnderANewWall_IsPushedEvenlyToTheOpenNeighbours()
        {
            var h = new float[N]; var d = new float[N]; d[Cell(4, 4)] = 1f;

            var r = Raise(h, d, new Dictionary<int, float> { { Cell(4, 4), 2f } });

            Assert.That(h[Cell(4, 4)], Is.EqualTo(2f));
            Assert.That(d[Cell(4, 4)], Is.EqualTo(0f));
            foreach (var n in new[] { Cell(5, 4), Cell(3, 4), Cell(4, 5), Cell(4, 3) }) Assert.That(d[n], Is.EqualTo(0.25f).Within(Eps));
            Assert.That(r.Raised, Is.EqualTo(1));
            Assert.That(r.DisplacedCells, Is.EqualTo(1));
            Assert.That(r.Displaced, Is.EqualTo(1f).Within(Eps));
            Assert.That(r.KeptCells, Is.EqualTo(0));
            Assert.That(r.Moved, Is.EqualTo(new[] { Cell(4, 4) }));
            Assert.That(r.NoOpenNeighbour, Is.EqualTo(0));
        }

        [Test]
        public void RaiseCells_SubmergedPiece_KeepsTheLiquidAboveItsTop()
        {
            var h = new float[N]; var d = new float[N]; d[Cell(4, 4)] = 3f;

            var r = Raise(h, d, new Dictionary<int, float> { { Cell(4, 4), 1f } });

            Assert.That(d[Cell(4, 4)], Is.EqualTo(2f).Within(Eps), "3 m of liquid over a 1 m piece: 2 m stay on top of it");
            Assert.That(r.KeptCells, Is.EqualTo(1));
            Assert.That(r.KeptDepth, Is.EqualTo(2f).Within(Eps));
            Assert.That(r.Displaced, Is.EqualTo(1f).Within(Eps), "only the part below the top is displaced");
            Assert.That(d[Cell(5, 4)], Is.EqualTo(0.25f).Within(Eps));
        }

        [Test]
        public void RaiseCells_NeighbouringWallCells_GetNoLiquid()
        {
            var h = new float[N]; var d = new float[N]; d[Cell(4, 4)] = 1.2f;

            var r = Raise(h, d, new Dictionary<int, float> { { Cell(4, 4), 2f }, { Cell(5, 4), 2f } });

            Assert.That(d[Cell(5, 4)], Is.EqualTo(0f), "a wall cell is not open");
            foreach (var n in new[] { Cell(3, 4), Cell(4, 5), Cell(4, 3) }) Assert.That(d[n], Is.EqualTo(0.4f).Within(Eps), "a third each");
            Assert.That(r.Raised, Is.EqualTo(2));
        }

        [Test]
        public void RaiseCells_PieceAlreadyAtThisTopLastPass_DisplacesNothingAgain()
        {
            var h = new float[N]; var d = new float[N]; d[Cell(4, 4)] = 1f;

            var r = Raise(h, d, new Dictionary<int, float> { { Cell(4, 4), 2.01f } }, new Dictionary<int, float> { { Cell(4, 4), 2f } });

            Assert.That(r.Moved, Is.Empty);
            Assert.That(r.KeptCells, Is.EqualTo(1));
            Assert.That(r.KeptDepth, Is.EqualTo(1f).Within(Eps), "the liquid stood on that top already");
            Assert.That(d[Cell(4, 4)], Is.EqualTo(1f).Within(Eps));
        }

        [Test]
        public void RaiseCells_SolidCellWithoutLiquid_IsRaisedAndMovesNothing()
        {
            var h = new float[N]; var d = new float[N];

            var r = Raise(h, d, new Dictionary<int, float> { { Cell(4, 4), 2f } });

            Assert.That(r.Raised, Is.EqualTo(1));
            Assert.That(r.Moved, Is.Empty);
            Assert.That(r.KeptCells, Is.EqualTo(0));
            Assert.That(d.Sum(), Is.EqualTo(0f));
        }

        [Test]
        public void RaiseCells_CellThatDisplacedLastPass_IsCountedAsARepeat()
        {
            var h = new float[N]; var d = new float[N]; d[Cell(4, 4)] = 1f;

            var r = Raise(h, d, new Dictionary<int, float> { { Cell(4, 4), 2f } }, null, new List<int> { Cell(4, 4) });

            Assert.That(r.RepeatCells, Is.EqualTo(1));
            Assert.That(r.RepeatDepth, Is.EqualTo(1f).Within(Eps));
        }

        [Test]
        public void RaiseCells_CellThatDidNotDisplaceLastPass_IsNotARepeat()
        {
            var h = new float[N]; var d = new float[N]; d[Cell(4, 4)] = 1f;

            var r = Raise(h, d, new Dictionary<int, float> { { Cell(4, 4), 2f } }, null, new List<int> { Cell(2, 2) });

            Assert.That(r.RepeatCells, Is.EqualTo(0));
            Assert.That(r.RepeatDepth, Is.EqualTo(0f));
        }

        [Test]
        public void RaiseCells_WallAllAround_SendsTheLiquidToTheNearestOpenCell()
        {
            var h = new float[N]; var d = new float[N]; d[Cell(4, 4)] = 1f;
            var raise = new Dictionary<int, float>
            {
                { Cell(4, 4), 2f }, { Cell(3, 4), 2f }, { Cell(5, 4), 2f }, { Cell(4, 3), 2f }, { Cell(4, 5), 2f },
            };

            var r = Raise(h, d, raise);

            Assert.That(r.NoOpenNeighbour, Is.EqualTo(1));
            Assert.That(r.Far, Is.EqualTo(1f).Within(Eps));
            Assert.That(r.Displaced, Is.EqualTo(1f).Within(Eps));
            Assert.That(d[Cell(2, 4)], Is.EqualTo(1f).Within(Eps), "walked over the wall cells to the first open cell found");
            Assert.That(d.Sum(), Is.EqualTo(1f).Within(Eps), "nothing is lost");
        }

        [Test]
        public void RaiseCells_NoOpenCellAnywhere_TheLiquidStaysAndIsReported()
        {
            var h = new float[N]; var d = new float[N]; d[Cell(4, 4)] = 0.5f;
            var raise = new Dictionary<int, float>();
            for (int i = 0; i < N; i++) raise[i] = 2f;

            var r = Raise(h, d, raise);

            Assert.That(r.UnplacedCells, Is.EqualTo(1));
            Assert.That(r.Unplaced, Is.EqualTo(0.5f).Within(Eps));
            Assert.That(d[Cell(4, 4)], Is.EqualTo(0.5f).Within(Eps), "liquid is never deleted (W-5)");
        }

        [Test]
        public void NearestOpenCells_NoOpenCell_GivesMinusOneForEveryWall()
        {
            var walls = Enumerable.Range(0, N).ToList();
            var isWall = Enumerable.Repeat(true, N).ToArray();

            var t = HeightField.NearestOpenCells(isWall, walls, W);

            Assert.That(t.All(x => x == -1), Is.True);
        }

        [Test]
        public void NearestOpenCells_WallBlock_EveryCellPointsAtAnOpenCell()
        {
            var isWall = new bool[N]; var walls = new List<int>();
            for (int y = 3; y <= 5; y++) for (int x = 3; x <= 5; x++) { isWall[Cell(x, y)] = true; walls.Add(Cell(x, y)); }

            var t = HeightField.NearestOpenCells(isWall, walls, W);

            foreach (int w in walls) Assert.That(t[w] >= 0 && !isWall[t[w]], Is.True, "wall " + (w % Stride) + "," + (w / Stride));
            Assert.That(t[Cell(4, 4)], Is.GreaterThanOrEqualTo(0), "the middle of the block is reached over its neighbours");
        }
    }
}
