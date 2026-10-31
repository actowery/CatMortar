using System;

namespace CatMortar;

public enum CatCoatPayload
{
    OrangeFire,
    BlackEMP,
    CreamFirefoamStun,
    BrownAntigrain
}

public static class CatMortarRules
{
    public const int CoatCount = 4;
    public const float LivingExplosionRadius = 4.5f;
    public const float CorpseExplosionRadius = 3f;
    public const float BrownAftershockRadius = 6f;
    public const float FlightScale = 1.25f;
    public const float MortarMinimumRange = 29.9f;
    public const float MortarMaximumRange = 500f;
    public const int MinimumBloodCells = 12;
    public const int MaximumBloodCells = 20;

    public static CatCoatPayload PayloadFor(int coatIndex) => (CatCoatPayload)NormalizeCoatIndex(coatIndex);

    public static int CoatForRoll(float roll)
    {
        var bounded = Math.Min(0.999999f, Math.Max(0f, roll));
        return (int)(bounded * CoatCount);
    }

    public static int NormalizeCoatIndex(int index) => Math.Min(CoatCount - 1, Math.Max(0, index));

    public static bool IsEligibleCat(bool isCat, bool isPlayerFaction, bool isCorpse) => isCat && isPlayerFaction;

    public static bool IsWithinMortarRange(float distance) => distance >= MortarMinimumRange && distance <= MortarMaximumRange;

    public static float ExplosionRadius(bool isCorpse) => isCorpse ? CorpseExplosionRadius : LivingExplosionRadius;

    public static int BloodCellCount(float roll)
    {
        var bounded = Math.Min(0.999999f, Math.Max(0f, roll));
        return MinimumBloodCells + (int)(bounded * (MaximumBloodCells - MinimumBloodCells + 1));
    }

    public static bool IsSafeLandingDistance(float distance) => distance >= 7f && distance <= 9f;

    public static bool IsNonVitalPart(string partDefName) => !string.IsNullOrWhiteSpace(partDefName)
        && !partDefName.Equals("Brain", StringComparison.OrdinalIgnoreCase)
        && !partDefName.Equals("Heart", StringComparison.OrdinalIgnoreCase)
        && !partDefName.Equals("Neck", StringComparison.OrdinalIgnoreCase)
        && !partDefName.Equals("Torso", StringComparison.OrdinalIgnoreCase);

    public static float FlightRotation(int facing, int spinDirection, int elapsedTicks)
    {
        var direction = spinDirection >= 0 ? 1f : -1f;
        return NormalizeFacing(facing) * 90f + direction * elapsedTicks * 2.5f;
    }

    public static int NormalizeFacing(int facing) => ((facing % 4) + 4) % 4;
}
