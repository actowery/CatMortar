using System.Linq;
using RimWorld;
using Verse;

namespace CatMortar;

public static class CatMortarInjuries
{
    public static void ApplyCriticalLandingInjuries(Pawn cat)
    {
        // A previously launched/surviving cat can already lack a paw or other
        // limb. Never try to add another hediff to a part that is absent.
        var presentNonVitalParts = cat.RaceProps.body.AllParts
            .Where(part => CatMortarRules.IsNonVitalPart(part.def.defName) && !cat.health.hediffSet.PartIsMissing(part))
            .ToList();

        if (presentNonVitalParts.Count > 0)
        {
            var lostPart = presentNonVitalParts.RandomElement();
            cat.health.AddHediff(HediffDefOf.MissingBodyPart, lostPart);
            presentNonVitalParts.Remove(lostPart);
        }

        foreach (var part in presentNonVitalParts.InRandomOrder().Take(3))
            cat.TakeDamage(new DamageInfo(DamageDefOf.Cut, 8f, 0f, -1f, null, part));
    }
}
