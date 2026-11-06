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
    public const int MinimumCatMeatDamage = 6;
    public const int MaximumCatMeatDamage = 25;
    public const float MinimumCatMeatExplosionRadius = 0.825f;
    public const float MaximumCatMeatExplosionRadius = 2.25f;
    public const int MinimumCatMeatBloodCells = 4;
    public const int MaximumCatMeatBloodCells = 14;

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

    public static int CatMeatDamage(int count)
    {
        var bounded = ClampCatMeatCount(count);
        return MinimumCatMeatDamage + (bounded - 1) * (MaximumCatMeatDamage - MinimumCatMeatDamage) / (MaximumCatMeatPerShot - 1);
    }

    public static float CatMeatExplosionRadius(int count)
    {
        var progress = (ClampCatMeatCount(count) - 1f) / (MaximumCatMeatPerShot - 1f);
        return MinimumCatMeatExplosionRadius + (MaximumCatMeatExplosionRadius - MinimumCatMeatExplosionRadius) * progress;
    }

    public static int CatMeatBloodCellCount(int count)
    {
        var progress = (ClampCatMeatCount(count) - 1f) / (MaximumCatMeatPerShot - 1f);
        return MinimumCatMeatBloodCells + (int)Math.Round((MaximumCatMeatBloodCells - MinimumCatMeatBloodCells) * progress, MidpointRounding.AwayFromZero);
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
