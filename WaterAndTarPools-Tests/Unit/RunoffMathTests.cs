using System;
using FsCheck;
using WaterAndTarPools.Drain;

namespace WaterAndTarPools.Tests.Unit
{
    /// <summary>D-029: runoff goes on into the next pool only within 20 m of the pour point, never back towards it, and never into the sea.</summary>
    [TestFixture]
    public class RunoffMathTests
    {
        static bool Decide(float into, float prev, bool sea, out string why) { return RunoffMath.Decide(into, prev, () => sea, out why); }

        [Test]
        public void Allowed_WhenNearEnough_FartherThanBefore_AndNotTheSea()
        {
            string why;

            Assert.That(Decide(10f, 5f, false, out why), Is.True);
            Assert.That(why, Is.Empty);
        }

        [Test]
        public void TooFar_Vanishes_AndSaysHowFar()
        {
            string why;

            Assert.That(Decide(20.01f, 5f, false, out why), Is.False);
            Assert.That(why, Does.Contain("20.0 m").And.Contain("limit 20"));
        }

        [Test]
        public void ExactlyAtTheLimit_IsStillAllowed()
        {
            string why;

            Assert.That(Decide(20f, 5f, false, out why), Is.True);
        }

        [Test]
        public void FlowingBack_IsRefused_IncludingTheMargin()
        {
            string why;

            Assert.That(Decide(5.25f, 5f, false, out why), Is.False, "within 0.25 m of the previous landing counts as flowing back");
            Assert.That(why, Does.Contain("flow back"));
            Assert.That(Decide(5.26f, 5f, false, out why), Is.True);
        }

        [Test]
        public void Sea_EndsTheRunoff()
        {
            string why;

            Assert.That(Decide(10f, 5f, true, out why), Is.False);
            Assert.That(why, Does.Contain("sea"));
        }

        [Test]
        public void TooFar_IsReportedBeforeFlowBack_AndBeforeTheSea()
        {
            string why;

            Decide(30f, 40f, true, out why);

            Assert.That(why, Does.Contain("limit"));
        }

        [Test]
        public void SeaCheck_IsOnlyAskedWhenTheCheapChecksPass()
        {
            var asked = 0;
            string why;

            RunoffMath.Decide(30f, 1f, () => { asked++; return false; }, out why);
            RunoffMath.Decide(2f, 5f, () => { asked++; return false; }, out why);
            RunoffMath.Decide(10f, 5f, () => { asked++; return false; }, out why);

            Assert.That(asked, Is.EqualTo(1));
        }

        [Test]
        public void NoSeaCheck_MeansNotTheSea()
        {
            string why;

            Assert.That(RunoffMath.Decide(10f, 5f, null, out why), Is.True);
        }

        [Test]
        public void Property_AllowedExactlyWhenAllThreeRulesPass()
        {
            var dist = Gen.Choose(0, 400).Select(x => x / 10f);
            Props.Check(Prop.ForAll(Arb.From(dist), Arb.From(dist), Arb.From(Gen.Elements(true, false)), (into, prev, sea) =>
            {
                string why;
                var ok = Decide(into, prev, sea, out why);
                var expected = into <= RunoffMath.MaxRunoffDistance && into > prev + RunoffMath.BackflowMargin && !sea;
                return ok == expected && (ok == (why.Length == 0));
            }));
        }
    }
}
