using System;
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
    private int pendingLaunchTick = -1;
    private int cooldownUntilTick;
    private int nextAutoReloadAttemptTick;
    private IntVec3 pendingTargetCell;
    private bool autoTargeting;
    private bool reloadPending;
    private Pawn? pendingReloadPawn;

    public Building_CatMortar() => innerContainer = new ThingOwner<Thing>(this, oneStackOnly: true);

    public Thing? LoadedThing => innerContainer.FirstOrDefault();
    public bool HasLoadedCat => LoadedThing is not null;
    private bool IsManned => GetComp<CompMannable>()?.MannedNow ?? false;
    private Pawn? ManningPawn => GetComp<CompMannable>()?.ManningPawn;
    private bool LaunchPending => pendingLaunchTick >= 0;
    private bool CoolingDown => Find.TickManager.TicksGame < cooldownUntilTick;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Deep.Look(ref innerContainer, "innerContainer", this);
        Scribe_Values.Look(ref loadedCoatIndex, "loadedCoatIndex", -1);
        Scribe_Values.Look(ref pendingLaunchTick, "pendingLaunchTick", -1);
        Scribe_Values.Look(ref cooldownUntilTick, "cooldownUntilTick", 0);
        Scribe_Values.Look(ref nextAutoReloadAttemptTick, "nextAutoReloadAttemptTick", 0);
        Scribe_Values.Look(ref pendingTargetCell, "pendingTargetCell");
        Scribe_Values.Look(ref autoTargeting, "autoTargeting", false);
        Scribe_Values.Look(ref reloadPending, "reloadPending", false);
        Scribe_References.Look(ref pendingReloadPawn, "pendingReloadPawn");
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
            targetingParams = new TargetingParameters { canTargetLocations = false, canTargetPawns = true, canTargetAnimals = true, canTargetCorpses = true },
            action = target => TryOrderCatLoad(target.Thing),
        };
        if (HasLoadedCat) loadCommand.Disable("A cat is already loaded.");
        yield return loadCommand;
        var loadMeatCommand = new Command_Target
        {
            defaultLabel = "CatMortar.LoadCatMeat".Translate(),
            defaultDesc = "CatMortar.LoadCatMeatDesc".Translate(CatMortarRules.MaximumCatMeatPerShot),
            icon = TexCommand.Install,
            targetingParams = new TargetingParameters { canTargetLocations = false, canTargetItems = true },
            action = target => TryOrderCatMeatLoad(target.Thing),
        };
        if (HasLoadedCat) loadMeatCommand.Disable("A cat is already loaded.");
        yield return loadMeatCommand;
        var fireCommand = new Command_Target
        {
            defaultLabel = "Force target",
            defaultDesc = "Aim the loaded cat like a mortar. Targets inside the red dead zone are rejected.",
            icon = TexCommand.Attack,
            targetingParams = new TargetingParameters { canTargetLocations = true, canTargetPawns = true, canTargetBuildings = true, canTargetItems = true },
            action = target => QueueLaunch(target.Cell, false),
        };
        if (!HasLoadedCat) fireCommand.Disable("Load a colony cat or cat corpse first.");
        else if (!IsManned) fireCommand.Disable("A colonist must man the cat mortar.");
        else if (Position.Roofed(Map)) fireCommand.Disable("The cat mortar cannot fire while roofed.");
        else if (LaunchPending) fireCommand.Disable("The cat mortar is aiming.");
        else if (CoolingDown) fireCommand.Disable("The cat mortar is cooling down.");
        yield return fireCommand;

        yield return new Command_Toggle
        {
            defaultLabel = "Auto-target enemies",
            defaultDesc = "When manned and loaded, automatically aim at the nearest hostile pawn outside the mortar's dead zone.",
            icon = TexCommand.Attack,
            isActive = () => autoTargeting,
            toggleAction = () => autoTargeting = !autoTargeting,
        };
    }

    public bool CanAccept(Thing thing)
    {
        if (thing is null || HasLoadedCat) return false;
        return IsLivingCatOrCorpse(thing) || IsCatMeat(thing);
    }

    public bool TryAccept(Thing thing)
    {
        if (!CanAccept(thing)) return false;
        if (!innerContainer.TryAddOrTransfer(thing, canMergeWithExistingStacks: false)) return false;
        loadedCoatIndex = thing is Pawn pawn ? CatMortarVisuals.EnsureCoat(pawn) : -1;
        DefDatabase<SoundDef>.GetNamed("Artillery_ShellLoaded").PlayOneShot(new TargetInfo(Position, Map));
        return true;
    }

    public bool OrderLoad(Thing target, int count = 1, Pawn? preferredHauler = null)
    {
        if (!CanAccept(target) || Map is null) return false;
        var candidates = new List<Pawn>();
        if (preferredHauler is not null) candidates.Add(preferredHauler);
        if (ManningPawn is not null && !candidates.Contains(ManningPawn)) candidates.Add(ManningPawn);
        candidates.AddRange(Map.mapPawns.FreeColonistsSpawned
            .Where(pawn => !candidates.Contains(pawn))
            .OrderBy(pawn => pawn.Position.DistanceToSquared(target.Position)));
        // A manning pawn already reserves the mortar. Its newly ordered load job
        // replaces manning and then obtains the normal job reservation, so do not
        // reject it merely because the building is currently reserved.
        var hauler = candidates.FirstOrDefault(pawn => pawn.Spawned
            && !pawn.Dead
            && pawn.CanReserve(target)
            && pawn.CanReach(target, PathEndMode.Touch, Danger.Some)
            && pawn.CanReach(this, PathEndMode.Touch, Danger.Some));
        if (hauler is null)
        {
            if (preferredHauler is null) Messages.Message("No colonist can reach and load that ammunition.", this, MessageTypeDefOf.RejectInput, false);
            return false;
        }
        var job = JobMaker.MakeJob(CatMortarDefOf.CatMortar_Load, target, this);
        job.count = Math.Min(Math.Max(1, count), target.stackCount);
        if (!hauler.jobs.TryTakeOrderedJob(job))
        {
            Messages.Message("The selected colonist could not start loading the cat mortar.", this, MessageTypeDefOf.RejectInput, false);
            return false;
        }
        Messages.Message($"{hauler.LabelShortCap} is loading {job.count} {target.LabelCap}.", this, MessageTypeDefOf.NeutralEvent, false);
        return true;
    }

    private void TryOrderCatLoad(Thing? target)
    {
        if (target is null || !IsLivingCatOrCorpse(target))
        {
            Messages.Message("Select a colony cat or colony cat corpse.", this, MessageTypeDefOf.RejectInput, false);
            return;
        }
        OrderLoad(target);
    }

    private void TryOrderCatMeatLoad(Thing? target)
    {
        if (target is null || !IsCatMeat(target))
        {
            Messages.Message("Select vanilla cat meat (Meat_Cat), not another animal's meat.", this, MessageTypeDefOf.RejectInput, false);
            return;
        }
        OrderLoad(target, Math.Min(CatMortarRules.MaximumCatMeatPerShot, target.stackCount));
    }

    protected override void Tick()
    {
        base.Tick();
        if (!Spawned || Map is null) return;

        var now = Find.TickManager.TicksGame;
        if (LaunchPending && now >= pendingLaunchTick)
        {
            pendingLaunchTick = -1;
            LaunchAt(pendingTargetCell);
        }

        if (reloadPending && !HasLoadedCat && !LaunchPending && now >= nextAutoReloadAttemptTick) TryOperatorReload(now);
        if (autoTargeting && HasLoadedCat && !LaunchPending && !CoolingDown) TryQueueAutoTarget();
    }

    public override void DrawExtraSelectionOverlays()
    {
        base.DrawExtraSelectionOverlays();
        if (Spawned) GenDraw.DrawRadiusRing(Position, CatMortarRules.MortarMinimumRange, Color.red);
    }

    protected override void DrawAt(Vector3 drawLoc, bool flip = false)
    {
        base.DrawAt(drawLoc, flip);
        var topGraphic = GraphicDatabase.Get<Graphic_Single>(
            "Things/Building/Security/TurretMortar_Top",
            ShaderDatabase.Cutout,
            new Vector2(4f, 4f),
            new Color(0.92f, 0.62f, 0.28f));
        drawLoc.y += 0.01f;
        topGraphic.Draw(drawLoc, Rotation, this);
    }

    private void QueueLaunch(IntVec3 targetCell, bool automatic)
    {
        if (!CanStartLaunch(targetCell, automatic)) return;
        pendingTargetCell = targetCell;
        pendingLaunchTick = Find.TickManager.TicksGame + CatMortarRules.MortarWarmupTicks;
        if (!automatic) Messages.Message("Cat mortar aiming: 4 seconds.", this, MessageTypeDefOf.NeutralEvent, false);
    }

    private bool CanStartLaunch(IntVec3 targetCell, bool automatic)
    {
        if (!HasLoadedCat || Map is null || !targetCell.InBounds(Map) || !IsManned || Position.Roofed(Map) || LaunchPending || CoolingDown) return false;
        var distance = Position.DistanceTo(targetCell);
        if (CatMortarRules.IsTooCloseForMortar(distance))
        {
            if (!automatic) Messages.Message("Target is too close for the cat mortar.", this, MessageTypeDefOf.RejectInput, false);
            return false;
        }
        if (!CatMortarRules.IsWithinMortarRange(distance))
        {
            if (!automatic) Messages.Message("Target is beyond the cat mortar's range.", this, MessageTypeDefOf.RejectInput, false);
            return false;
        }
        return true;
    }

    private void LaunchAt(IntVec3 targetCell)
    {
        if (!CanStartLaunch(targetCell, true)) return;
        var payload = LoadedThing;
        if (payload is null) return;
        var shell = (Projectile_CatMortarShell)ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("Projectile_CatMortarShell"));
        shell.LoadPayload(payload, payload is Pawn ? loadedCoatIndex : -1);
        loadedCoatIndex = -1;
        GenSpawn.Spawn(shell, Position, Map);
        DefDatabase<SoundDef>.GetNamed("Mortar_LaunchA").PlayOneShot(new TargetInfo(Position, Map));
        DefDatabase<SoundDef>.GetNamed("Pawn_Cat_Call").PlayOneShot(new TargetInfo(Position, Map));
        shell.Launch(this, DrawPos, new LocalTargetInfo(targetCell), new LocalTargetInfo(targetCell), ProjectileHitFlags.IntendedTarget, false, null);
        var now = Find.TickManager.TicksGame;
        cooldownUntilTick = now + CatMortarRules.MortarCooldownTicks;
        pendingReloadPawn = ManningPawn;
        reloadPending = pendingReloadPawn is not null;
        nextAutoReloadAttemptTick = cooldownUntilTick;
    }

    public override string GetInspectString()
    {
        if (!Spawned || Map is null) return string.Empty;
        var inspect = base.GetInspectString();
        if (!HasLoadedCat) return inspect;

        var payloadInfo = IsCatMeat(LoadedThing!)
            ? $"Loaded: {LoadedThing!.stackCount} cat meat\nPayload: meat barrage — {CatMortarRules.CatMeatDamage(LoadedThing.stackCount)} damage, {CatMortarRules.CatMeatExplosionRadius(LoadedThing.stackCount):0.##}-tile radius\nMode: manual load only"
            : $"Loaded: {LoadedThing!.LabelCap}\nPayload: {CatMortarVisuals.CoatLabel(loadedCoatIndex)}\nMode: {(autoTargeting ? "auto-target enemies" : "force target")}";
        return string.IsNullOrWhiteSpace(inspect) ? payloadInfo : $"{inspect}\n{payloadInfo}";
    }

    private void TryQueueAutoTarget()
    {
        var target = Map.mapPawns.AllPawnsSpawned
            .Where(pawn => pawn.HostileTo(Faction.OfPlayer) && CanStartLaunch(pawn.Position, true))
            .OrderBy(pawn => Position.DistanceToSquared(pawn.Position))
            .FirstOrDefault();
        if (target is not null) QueueLaunch(target.Position, true);
    }

    private void TryOperatorReload(int now)
    {
        nextAutoReloadAttemptTick = now + CatMortarRules.AutoReloadRetryTicks;
        if (pendingReloadPawn is null || pendingReloadPawn.Dead || !pendingReloadPawn.Spawned)
        {
            reloadPending = false;
            return;
        }
        var cat = Map.mapPawns.AllPawnsSpawned
            .Where(IsAutomaticAmmoCat)
            .OrderBy(pawn => Position.DistanceToSquared(pawn.Position))
            .FirstOrDefault();
        if (cat is not null && OrderLoad(cat, preferredHauler: pendingReloadPawn)) reloadPending = false;
    }

    private static bool IsAutomaticAmmoCat(Pawn pawn) => pawn.def.defName == "Cat"
        && pawn.Faction == Faction.OfPlayer
        && pawn.Spawned
        && !pawn.Dead
        && !pawn.Downed
        && pawn.relations?.DirectRelations?.All(relation => relation.def != PawnRelationDefOf.Bond) == true;

    private static bool IsLivingCatOrCorpse(Thing? thing)
    {
        if (thing is Pawn pawn) return pawn.def.defName == "Cat" && pawn.Faction == Faction.OfPlayer && !pawn.Dead;
        return thing is Corpse corpse && corpse.InnerPawn?.def?.defName == "Cat" && corpse.InnerPawn.Faction == Faction.OfPlayer;
    }

    private static bool IsCatMeat(Thing? thing) => thing is not null
        && CatMortarRules.IsEligibleCatMeat(thing.def.IsMeat, thing.def.defName == "Meat_Cat");

    public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
    {
        if (Spawned && innerContainer?.Any == true) innerContainer.TryDropAll(Position, Map, ThingPlaceMode.Near);
        loadedCoatIndex = -1;
        pendingLaunchTick = -1;
        reloadPending = false;
        pendingReloadPawn = null;
        base.Destroy(mode);
    }
}
