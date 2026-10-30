using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace CatMortar;

public sealed class Building_CatMortar : Building, IThingHolder
{
    private ThingOwner<Thing> innerContainer;

    public Building_CatMortar() => innerContainer = new ThingOwner<Thing>(this, oneStackOnly: true);

    public Thing LoadedThing => innerContainer.FirstOrDefault();
    public bool HasLoadedCat => LoadedThing is not null;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Deep.Look(ref innerContainer, "innerContainer", this);
        if (innerContainer is null) innerContainer = new ThingOwner<Thing>(this, oneStackOnly: true);
    }

    public ThingOwner GetDirectlyHeldThings() => innerContainer;
    public void GetChildHolders(List<IThingHolder> outChildren) { }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        foreach (var gizmo in base.GetGizmos()) yield return gizmo;
        yield return new Command_Target
        {
            defaultLabel = "Load cat",
            defaultDesc = "Designate one colony cat or cat corpse. The cat is suspended inside the mortar until fired.",
            icon = TexCommand.Install,
            targetingParams = new TargetingParameters { canTargetPawns = true, canTargetItems = true, validator = target => CanAccept(target.Thing) },
            action = target => OrderLoad(target.Thing),
            disabled = HasLoadedCat,
            disabledReason = HasLoadedCat ? "A cat is already loaded." : null
        };
        yield return new Command_Target
        {
            defaultLabel = "Fire loaded cat",
            defaultDesc = "Launch the loaded cat as artillery. Loading is permanent until firing.",
            icon = TexCommand.Attack,
            targetingParams = TargetingParameters.ForAttackHostile(),
            action = target => LaunchAt(target.Cell),
            disabled = !HasLoadedCat,
            disabledReason = HasLoadedCat ? null : "Load a colony cat or cat corpse first."
        };
    }

    public bool CanAccept(Thing thing)
    {
        if (thing is null || HasLoadedCat) return false;
        if (thing is Pawn pawn) return pawn.def.defName == "Cat" && pawn.Faction == Faction.OfPlayer && !pawn.Dead;
        return thing is Corpse corpse && corpse.InnerPawn?.def?.defName == "Cat" && corpse.InnerPawn.Faction == Faction.OfPlayer;
    }

    public bool TryAccept(Thing thing)
    {
        if (!CanAccept(thing)) return false;
        if (thing is Pawn pawn) CatMortarVisuals.AssignRandomCoat(pawn);
        return innerContainer.TryAddOrTransfer(thing, canMergeWithExistingStacks: false);
    }

    public void OrderLoad(Thing target)
    {
        if (!CanAccept(target) || Map is null) return;
        var hauler = Map.mapPawns.FreeColonistsSpawned
            .Where(pawn => pawn.CanReserve(target) && pawn.CanReserve(this) && pawn.CanReach(target, PathEndMode.Touch, Danger.Some))
            .OrderBy(pawn => pawn.Position.DistanceToSquared(target.Position)).FirstOrDefault();
        if (hauler is null)
        {
            Messages.Message("No colonist can load that cat.", this, MessageTypeDefOf.RejectInput, false);
            return;
        }
        hauler.jobs.TryTakeOrderedJob(JobMaker.MakeJob(CatMortarDefOf.CatMortar_Load, target, this));
    }

    public void LaunchAt(IntVec3 targetCell)
    {
        if (!HasLoadedCat || Map is null || !targetCell.InBounds(Map)) return;
        var payload = LoadedThing;
        var shell = (Projectile_CatMortarShell)ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("Projectile_CatMortarShell"));
        shell.LoadPayload(payload, payload is Pawn pawn ? CatMortarVisuals.CoatFor(pawn) : -1);
        GenSpawn.Spawn(shell, Position, Map);
        DefDatabase<SoundDef>.GetNamed("Building_Mortar_Fire").PlayOneShot(new TargetInfo(Position, Map));
        shell.Launch(this, DrawPos, new LocalTargetInfo(targetCell), new LocalTargetInfo(targetCell), ProjectileHitFlags.IntendedTarget, false, null);
    }

    public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
    {
        if (Spawned && innerContainer?.Any == true) innerContainer.TryDropAll(Position, Map, ThingPlaceMode.Near);
        base.Destroy(mode);
    }
}
