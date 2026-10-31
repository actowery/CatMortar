using UnityEngine;
using Verse;

namespace CatMortar;

public static class CatMortarVisuals
{
    public static readonly Color[] CoatColors =
    {
        new(0.95f, 0.48f, 0.16f), new(0.18f, 0.18f, 0.18f),
        new(0.88f, 0.82f, 0.70f), new(0.45f, 0.36f, 0.25f)
    };

    public static readonly string[] CoatHediffs =
    {
        "CatMortar_LaunchCoatOrange", "CatMortar_LaunchCoatBlack",
        "CatMortar_LaunchCoatCream", "CatMortar_LaunchCoatBrown"
    };

    public static Color ColorFor(int coatIndex) => CoatColors[CatMortarRules.NormalizeCoatIndex(coatIndex)];

    public static int CoatFor(Pawn pawn)
    {
        for (var index = 0; index < CoatHediffs.Length; index++)
            if (pawn.health?.hediffSet?.HasHediff(DefDatabase<HediffDef>.GetNamedSilentFail(CoatHediffs[index])) == true) return index;
        return 0;
    }

    public static int AssignRandomCoat(Pawn pawn)
    {
        return AssignCoat(pawn, CatMortarRules.CoatForRoll(Rand.Value));
    }

    public static int AssignCoat(Pawn pawn, int coatIndex)
    {
        var normalized = CatMortarRules.NormalizeCoatIndex(coatIndex);
        RemoveLaunchCoats(pawn);
        if (pawn.health is not null) pawn.health.AddHediff(DefDatabase<HediffDef>.GetNamed(CoatHediffs[normalized]));
        return normalized;
    }

    public static string CoatLabel(int coatIndex) => CatMortarRules.NormalizeCoatIndex(coatIndex) switch
    {
        0 => "orange fireburst",
        1 => "black EMP",
        2 => "cream firefoam",
        _ => "brown aftershock"
    };

    public static void RemoveLaunchCoats(Pawn pawn)
    {
        var health = pawn.health;
        if (health is null) return;
        foreach (var hediffName in CoatHediffs)
        {
            var def = DefDatabase<HediffDef>.GetNamedSilentFail(hediffName);
            var hediff = def is null ? null : health.hediffSet.GetFirstHediffOfDef(def);
            if (hediff is not null) health.RemoveHediff(hediff);
        }
    }
}
