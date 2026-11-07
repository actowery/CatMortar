using System;

namespace CatMortar;

public enum CatCoatPayload
{
    OrangeFire,
    BlackEMP,
    CreamFirefoamStun,
    BrownAntigrain
}

public enum CatMortarAmmoType
{
    LivingCat,
    CatCorpse,
    CatMeat,
    Unknown
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
    public const int MortarWarmupTicks = 240;
    public const int MortarCooldownTicks = 360;
    public const int AutoReloadRetryTicks = 600;
    public const int MinimumBloodCells = 12;
    public const int MaximumBloodCells = 20;
    public const int MaximumCatMeatPerShot = 20;
    public const float CatMeatScatterRadius = 7f;
    public const int MinimumCatMeatChunkDamage = 4;
    public const int MaximumCatMeatChunkDamage = 7;
    public const float MinimumCatMeatStunChance = 0.15f;
    public const float MaximumCatMeatStunChance = 0.35f;
    public const int CatMeatStunTicks = 30;
    public const float BestCatShotDeviation = 1.5f;
    public const float WorstCatShotDeviation = 4f;

    public static CatCoatPayload PayloadFor(int coatIndex) => (CatCoatPayload)NormalizeCoatIndex(coatIndex);

    public static int CoatForRoll(float roll)
    {
        var bounded = Math.Min(0.999999f, Math.Max(0f, roll));
        return (int)(bounded * CoatCount);
    }

    public static int NormalizeCoatIndex(int index) => Math.Min(CoatCount - 1, Math.Max(0, index));

    public static bool IsEligibleCat(bool isCat, bool isPlayerFaction, bool isCorpse) => isCat && isPlayerFaction;

    public static bool IsEligibleCatMeat(bool isMeat, bool hasCatMeatSource) => isMeat && hasCatMeatSource;

    public static CatMortarAmmoType AmmoTypeFor(bool isLivingCat, bool isCatCorpse, bool isCatMeat) =>
        isLivingCat ? CatMortarAmmoType.LivingCat :
        isCatCorpse ? CatMortarAmmoType.CatCorpse :
        isCatMeat ? CatMortarAmmoType.CatMeat :
        CatMortarAmmoType.Unknown;

    public static bool IsWithinMortarRange(float distance) => distance >= MortarMinimumRange && distance <= MortarMaximumRange;

    public static bool IsTooCloseForMortar(float distance) => distance < MortarMinimumRange;

    public static float ExplosionRadius(bool isCorpse) => isCorpse ? CorpseExplosionRadius : LivingExplosionRadius;

    public static int ClampCatMeatCount(int count) => Math.Min(MaximumCatMeatPerShot, Math.Max(1, count));

    public static int CatMeatChunkCount(int count) => ClampCatMeatCount(count);

    // More meat still means more scattered chunks, but each chunk also hits a
    // little harder. A full rack is worthwhile without becoming a precision
    // substitute for a corpse or live-cat shell.
    public static int CatMeatChunkDamage(int count) => MinimumCatMeatChunkDamage
        + (ClampCatMeatCount(count) - 1) * (MaximumCatMeatChunkDamage - MinimumCatMeatChunkDamage)
        / (MaximumCatMeatPerShot - 1);

    public static float CatMeatStunChance(int count) => MinimumCatMeatStunChance
        + (ClampCatMeatCount(count) - 1) * (MaximumCatMeatStunChance - MinimumCatMeatStunChance)
        / (MaximumCatMeatPerShot - 1);

    public static float CatShotDeviation(int shootingLevel)
    {
        var bounded = Math.Min(20, Math.Max(0, shootingLevel));
        return WorstCatShotDeviation - (WorstCatShotDeviation - BestCatShotDeviation) * bounded / 20f;
    }

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
