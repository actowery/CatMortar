using RimWorld;
using Verse;

namespace CatMortar;

[DefOf]
public static class CatMortarDefOf
{
    public static JobDef CatMortar_Load = null!;
    static CatMortarDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(CatMortarDefOf));
}
