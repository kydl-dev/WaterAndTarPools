using System;

namespace WaterAndTarPools.Drain;

/// <summary>The priority queue of HeightField (split out so the file stays under the class-size limit).</summary>
internal static partial class HeightField
{
    /// Binary min-heap of (key, cell); System.Collections.Generic.PriorityQueue does not exist in net48.
    sealed class MinHeap
    {
        float[] _k; int[] _v; int _n;
        public MinHeap(int cap) { _k = new float[cap]; _v = new int[cap]; }
        public int Count { get { return _n; } }
        public void Push(float k, int v)
        {
            if (_n == _k.Length) { Array.Resize(ref _k, _n * 2); Array.Resize(ref _v, _n * 2); }
            int i = _n++;
            while (i > 0)
            {
                int p = (i - 1) >> 1;
                if (_k[p] <= k) break;
                _k[i] = _k[p]; _v[i] = _v[p]; i = p;
            }
            _k[i] = k; _v[i] = v;
        }
        public int Pop(out float key)
        {
            key = _k[0]; int top = _v[0];
            float k = _k[--_n]; int v = _v[_n];
            int i = 0;
            while (true)
            {
                int l = 2 * i + 1;
                if (l >= _n) break;
                int c = l + 1 < _n && _k[l + 1] < _k[l] ? l + 1 : l;
                if (_k[c] >= k) break;
                _k[i] = _k[c]; _v[i] = _v[c]; i = c;
            }
            if (_n > 0) { _k[i] = k; _v[i] = v; }
            return top;
        }
    }
}
