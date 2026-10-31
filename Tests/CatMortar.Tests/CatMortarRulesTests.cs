using Xunit;

namespace CatMortar.Tests;

public sealed class CatMortarRulesTests
{
    [Theory]
    [InlineData(-1f, 0)] [InlineData(0f, 0)] [InlineData(0.249f, 0)] [InlineData(0.25f, 1)] [InlineData(0.5f, 2)] [InlineData(0.75f, 3)] [InlineData(1f, 3)]
    public void Coat_roll_selects_a_defined_payload(float roll, int expected) => Assert.Equal(expected, CatMortarRules.CoatForRoll(roll));

    [Theory]
    [InlineData(-1, CatCoatPayload.OrangeFire)] [InlineData(0, CatCoatPayload.OrangeFire)] [InlineData(1, CatCoatPayload.BlackEMP)] [InlineData(2, CatCoatPayload.CreamFirefoamStun)] [InlineData(3, CatCoatPayload.BrownAntigrain)] [InlineData(99, CatCoatPayload.BrownAntigrain)]
    public void Every_coat_maps_to_its_stable_payload(int coat, CatCoatPayload expected) => Assert.Equal(expected, CatMortarRules.PayloadFor(coat));

    [Theory]
    [InlineData(true, true, false, true)] [InlineData(true, true, true, true)] [InlineData(true, false, false, false)] [InlineData(false, true, false, false)]
    public void Only_colony_cats_are_valid_ammunition(bool isCat, bool playerFaction, bool isCorpse, bool expected) => Assert.Equal(expected, CatMortarRules.IsEligibleCat(isCat, playerFaction, isCorpse));

    [Theory]
    [InlineData(false, 4.5f)] [InlineData(true, 3f)]
    public void Corpse_ammunition_uses_a_degraded_blast(bool corpse, float expected) => Assert.Equal(expected, CatMortarRules.ExplosionRadius(corpse));

    [Theory]
    [InlineData(-1f, 12)] [InlineData(0f, 12)] [InlineData(0.5f, 16)] [InlineData(0.999f, 20)] [InlineData(2f, 20)]
    public void Blood_splatter_count_stays_inside_the_visual_budget(float roll, int expected) => Assert.Equal(expected, CatMortarRules.BloodCellCount(roll));

    [Theory]
    [InlineData(6.9f, false)] [InlineData(7f, true)] [InlineData(8f, true)] [InlineData(9f, true)] [InlineData(9.1f, false)]
    public void Survivor_landing_ring_avoids_the_primary_blast(float distance, bool expected) => Assert.Equal(expected, CatMortarRules.IsSafeLandingDistance(distance));

    [Theory]
    [InlineData(29.8f, false)] [InlineData(29.9f, true)] [InlineData(500f, true)] [InlineData(500.1f, false)]
    public void Mortar_targeting_enforces_vanilla_artillery_range(float distance, bool expected) => Assert.Equal(expected, CatMortarRules.IsWithinMortarRange(distance));

    [Theory]
    [InlineData(0f, true)] [InlineData(29.8f, true)] [InlineData(29.9f, false)] [InlineData(500f, false)]
    public void Mortar_dead_zone_is_reported_separately(float distance, bool expected) => Assert.Equal(expected, CatMortarRules.IsTooCloseForMortar(distance));

    [Theory]
    [InlineData("Brain", false)] [InlineData("Heart", false)] [InlineData("Neck", false)] [InlineData("Torso", false)] [InlineData("Leg", true)] [InlineData("Tail", true)] [InlineData("", false)]
    public void Injury_selector_excludes_vital_body_parts(string part, bool expected) => Assert.Equal(expected, CatMortarRules.IsNonVitalPart(part));

    [Theory]
    [InlineData(0, 1, 12, 30f)] [InlineData(2, -1, 12, 150f)]
    public void Cat_spin_is_independent_from_its_facing(int facing, int spinDirection, int ticks, float expected) => Assert.Equal(expected, CatMortarRules.FlightRotation(facing, spinDirection, ticks));
}
