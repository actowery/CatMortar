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
    private int loadedCoatIndex = -1;

    public Building_CatMortar() => innerContainer = new ThingOwner<Thing>(this, oneStackOnly: true);

    public Thing? LoadedThing => innerContainer.FirstOrDefault();
    public bool HasLoadedCat => LoadedThing is not null;
    private bool IsManned => GetComp<CompMannable>()?.MannedNow ?? false;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Deep.Look(ref innerContainer, "innerContainer", this);
        Scribe_Values.Look(ref loadedCoatIndex, "loadedCoatIndex", -1);
        if (innerContainer is null) innerContainer = new ThingOwner<Thing>(this, oneStackOnly: true);
    }

    public ThingOwner GetDirectlyHeldThings() => innerContainer;
    public void GetChildHolders(List<IThingHolder> outChildren) { }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        // A minified building has no map or interaction cell. Its own install command is
        // supplied by MinifiedThing; importantly, avoid base building gizmos as well.
        if (!Spawned || Map is null) yield break;

        foreach (var gizmo in base.GetGizmos()) yield return gizmo;

        var loadCommand = new Command_Target
        {
            defaultLabel = "Load cat",
            defaultDesc = "Designate one colony cat or cat corpse. The cat is suspended inside the mortar until fired.",
            icon = TexCommand.Install,
            targetingParams = new TargetingParameters { canTargetPawns = true, canTargetItems = true, canTargetCorpses = true, validator = target => CanAccept(target.Thing) },
            action = target => OrderLoad(target.Thing),
        };
        if (HasLoadedCat) loadCommand.Disable("A cat is already loaded.");
        yield return loadCommand;
        var fireCommand = new Command_Target
        {
            defaultLabel = "Fire loaded cat",
            defaultDesc = "Launch the loaded cat as artillery. Loading is permanent until firing.",
            icon = TexCommand.Attack,
            targetingParams = new TargetingParameters { canTargetLocations = true, canTargetPawns = false, canTargetBuildings = false, canTargetItems = false, validator = target => IsValidTargetCell(target.Cell) },
            action = target => LaunchAt(target.Cell),
        };
        if (!HasLoadedCat) fireCommand.Disable("Load a colony cat or cat corpse first.");
        else if (!IsManned) fireCommand.Disable("A colonist must man the cat mortar.");
        else if (Position.Roofed(Map)) fireCommand.Disable("The cat mortar cannot fire while roofed.");
        yield return fireCommand;
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
        if (!innerContainer.TryAddOrTransfer(thing, canMergeWithExistingStacks: false)) return false;
        loadedCoatIndex = thing is Pawn pawn ? CatMortarVisuals.AssignRandomCoat(pawn) : -1;
        DefDatabase<SoundDef>.GetNamed("Artillery_ShellLoaded").PlayOneShot(new TargetInfo(Position, Map));
        return true;
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
        var job = JobMaker.MakeJob(CatMortarDefOf.CatMortar_Load, target, this);
        job.count = 1;
        hauler.jobs.TryTakeOrderedJob(job);
    }

    public void LaunchAt(IntVec3 targetCell)
    {
        if (!HasLoadedCat || Map is null || !targetCell.InBounds(Map) || !IsManned || Position.Roofed(Map) || !IsValidTargetCell(targetCell)) return;
        var payload = LoadedThing;
        if (payload is null) return;
        var shell = (Projectile_CatMortarShell)ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("Projectile_CatMortarShell"));
        shell.LoadPayload(payload, payload is Pawn ? loadedCoatIndex : -1);
        loadedCoatIndex = -1;
        GenSpawn.Spawn(shell, Position, Map);
        DefDatabase<SoundDef>.GetNamed("Mortar_LaunchA").PlayOneShot(new TargetInfo(Position, Map));
        DefDatabase<SoundDef>.GetNamed("Pawn_Cat_Call").PlayOneShot(new TargetInfo(Position, Map));
        shell.Launch(this, DrawPos, new LocalTargetInfo(targetCell), new LocalTargetInfo(targetCell), ProjectileHitFlags.IntendedTarget, false, null);
    }

    public override string GetInspectString()
    {
        if (!Spawned || Map is null) return string.Empty;
        var inspect = base.GetInspectString();
        return HasLoadedCat ? $"{inspect}\nLoaded: {LoadedThing!.LabelCap}\nPayload: {CatMortarVisuals.CoatLabel(loadedCoatIndex)}" : inspect;
    }

    private bool IsValidTargetCell(IntVec3 targetCell)
    {
        if (Map is null || !targetCell.InBounds(Map)) return false;
        return CatMortarRules.IsWithinMortarRange(Position.DistanceTo(targetCell));
    }

    public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
    {
        if (Spawned && innerContainer?.Any == true) innerContainer.TryDropAll(Position, Map, ThingPlaceMode.Near);
        loadedCoatIndex = -1;
        base.Destroy(mode);
    }
}
