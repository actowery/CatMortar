using RimWorld;
using Verse;

namespace CatMortar;

[DefOf]
public static class CatMortarDefOf
{
    public static JobDef CatMortar_Load = null!;
    public static DamageDef CatMortar_MeatSlap = null!;
    static CatMortarDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(CatMortarDefOf));
}
