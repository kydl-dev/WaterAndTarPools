using System;

namespace WaterAndTarPools.Drain
{
    /// <summary>
    /// The decision part of the runoff rules (RunoffRules.Allowed) without any game type, so unit tests cover it. Distances are horizontal, in metres.
    /// Order matters: too far, then flowing back, then the sea (the sea check may be expensive, so it is only asked when the first two pass).
    /// </summary>
    internal static class RunoffMath
    {
        public const float MaxRunoffDistance = 20f;     // m, from the landing point of the player's stroke
        public const float BackflowMargin = 0.25f;      // m: landing this close to (or nearer than) the previous landing counts as flowing back

        /// <param name="farInto">distance from the pour point to where the runoff would land in the next pool</param>
        /// <param name="farPrev">distance from the pour point to where it landed in the pool it leaves</param>
        /// <param name="reachedSea">asked last: is the ground at the new landing point under the world's water level?</param>
        public static bool Decide(float farInto, float farPrev, Func<bool> reachedSea, out string why)
        {
            if (farInto > MaxRunoffDistance) { why = "stops " + farInto.ToString("0.0") + " m from where it was poured (limit " + MaxRunoffDistance + " m) - vanishes at the ground"; return false; }
            if (farInto <= farPrev + BackflowMargin) { why = "would flow back towards where it was poured - vanishes here"; return false; }
            if (reachedSea != null && reachedSea()) { why = "reached the sea / a river - joins it"; return false; }
            why = "";
            return true;
        }
    }
}
