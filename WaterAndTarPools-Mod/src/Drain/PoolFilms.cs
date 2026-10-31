using System.Collections.Generic;
using DrainTarPit;
using DrainTarPit.Drain;
using UnityEngine;
using Kydl.Common;
using static DrainTarPit.Drain.TarPits;
using static WaterAndTarPools.Drain.TarPitStats;

namespace WaterAndTarPools.Drain
{
    /// <summary>Thin films of liquid around a pump pool run off fast (split from DrainTarPit's TarPitSettle, T-069).</summary>
    internal static class PoolFilms
    {
        // --- thin films run off fast --------------------------------------------------------------------------------------
        // The game's simulation spreads liquid thinly and slowly: what runs over the rim of a pool (or is pushed off a wall cell by ApplyWalls) lingered as a
        // film on the outside of the walls and on the ground for minutes. Every ShedInterval the owner goes through the cells of its pump pools that hold less than
        // [Drain] PoolFilmDepth of liquid: a film with lower liquid / ground next to it moves ALL its liquid to the lowest neighbour (about one cell per pass, so
        // it visibly slides down), a film that reaches the border of the grid leaves, and a film that lies flat with nothing deeper next to it evaporates.
        // The shore of a pool (a thin cell next to a deeper one at the same level) has no lower neighbour and stays.
        internal static readonly Dictionary<LiquidVolume, float> ShedTimers = new();
        static bool[] _filmGot = new bool[0];
        internal const float ShedInterval = 0.15f;       // s between passes
        const float FilmEvaporation = 0.05f;    // m/s for a film that lies flat
        static readonly int[] Nx = { 1, -1, 0, 0, 1, 1, -1, -1 }, Ny = { 0, 0, 1, -1, 1, -1, 1, -1 };

        internal static void ShedFilms(LiquidVolume v, float dt)
        {
            if (!ModSettings.PoolShedFilm.Value || !IsLiquid(v) || !IsPumpPool(v) || !HeightsReady(v)) return;
            var nv = Nview(v);
            if (nv == null || !nv.IsValid() || !nv.IsOwner()) return;
            var d = Depths(v); var h = Heights(v);
            int w = v.m_width, stride = w + 1, n = stride * stride;
            if (d == null || h == null || d.Count < n || h.Count < n) return;
            float film = ModSettings.PoolFilmDepth.Value;
            if (_filmGot.Length < n) _filmGot = new bool[n];
            bool changed = false;
            float edgeSum = 0f, evapSum = 0f;       // depth sums (cell units) that left the pool in this pass, for the editor's budget
            using (Hold(v))
            {
                System.Array.Clear(_filmGot, 0, n);
                for (int y = 0; y <= w; y++)
                    for (int x = 0; x <= w; x++)
                    {
                        int i = y * stride + x;
                        float di = d[i];
                        if (di <= 0f || di >= film || _filmGot[i]) continue;
                        if (x <= 1 || y <= 1 || x >= w - 1 || y >= w - 1) { edgeSum += di; d[i] = 0f; changed = true; continue; }   // runs off the grid
                        float bs = h[i] + di - 0.004f; int best = -1; bool deeper = false;
                        for (int k = 0; k < 8; k++)
                        {
                            int j = (y + Ny[k]) * stride + (x + Nx[k]);
                            if (d[j] >= film) deeper = true;
                            float sj = h[j] + d[j];
                            if (sj < bs) { bs = sj; best = j; }
                        }
                        if (best >= 0) { d[best] += di; d[i] = 0f; _filmGot[best] = true; changed = true; }
                        else if (!deeper && ModSettings.PoolFilmEvaporates.Value) { float ev = Mathf.Min(di, FilmEvaporation * dt); evapSum += ev; d[i] = di - ev; changed = true; }     // [Drain] PoolFilmEvaporates (default off) - it was bound but never read, so thin pools always evaporated
                    }
                if (changed) FDirty.SetValue(v, true);
            }
            if (edgeSum > 0f || evapSum > 0f)
            {
                var bg = BudgetOf(v); float c2 = v.m_scale * v.m_scale;
                bg.ShedEdge += edgeSum * c2; bg.Evap += evapSum * c2;
                if (TarPitLedger.Active(v) && (edgeSum + evapSum) * c2 > 0.0005f)
                {
                    float after = TotalVolume(v);
                    TarPitLedger.Checkpoint(v, "shed", after + (edgeSum + evapSum) * c2, after, "(films off the grid border " + (edgeSum * c2).ToString("0.000") + " m3, evaporated " + (evapSum * c2).ToString("0.000") + " m3)");
                }
            }
            if (changed) FNeedsSaving.SetValue(v, true);
        }
    }
}
