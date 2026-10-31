using RimWorld;
using Verse;

namespace CatMortar;

/// <summary>Assigns a persistent visible coat to each eligible colony cat before it can become mortar ammunition.</summary>
public sealed class CatMortar_CatCoatMapComponent : MapComponent
{
    private int nextScanTick;

    public CatMortar_CatCoatMapComponent(Map map) : base(map) { }

    public override void MapComponentTick()
    {
        base.MapComponentTick();
        if (Find.TickManager.TicksGame < nextScanTick) return;
        nextScanTick = Find.TickManager.TicksGame + 60;

        foreach (var pawn in map.mapPawns.AllPawnsSpawned)
            if (pawn.def.defName == "Cat" && pawn.Faction == Faction.OfPlayer && !pawn.Dead)
                CatMortarVisuals.EnsureCoat(pawn);
    }
}
