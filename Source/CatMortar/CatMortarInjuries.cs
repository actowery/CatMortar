using System.Linq;
using RimWorld;
using Verse;

namespace CatMortar;

public static class CatMortarInjuries
{
    public static void ApplyCriticalLandingInjuries(Pawn cat)
    {
        var removable = cat.RaceProps.body.AllParts.Where(part => CatMortarRules.IsNonVitalPart(part.def.defName)).ToList();
        if (removable.Count > 0) cat.health.AddHediff(HediffDefOf.MissingBodyPart, removable.RandomElement());
        foreach (var part in cat.RaceProps.body.AllParts.Where(part => CatMortarRules.IsNonVitalPart(part.def.defName)).InRandomOrder().Take(3))
            cat.TakeDamage(new DamageInfo(DamageDefOf.Cut, 8f, 0f, -1f, null, part));
    }
}
