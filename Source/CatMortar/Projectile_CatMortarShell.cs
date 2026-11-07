using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace CatMortar;

public sealed class Projectile_CatMortarShell : Projectile, IThingHolder
{
    private ThingOwner<Thing> payloadContainer;
    private int coatIndex = -1;
    private int visualFacing = -1;
    private int spinDirection;
    private int firstDrawTick = -1;
    private bool launchSoundPlayed;

    public Projectile_CatMortarShell() => payloadContainer = new ThingOwner<Thing>(this, oneStackOnly: true);

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Deep.Look(ref payloadContainer, "payloadContainer", this);
        Scribe_Values.Look(ref coatIndex, "coatIndex", -1);
        Scribe_Values.Look(ref visualFacing, "visualFacing", -1);
        Scribe_Values.Look(ref spinDirection, "spinDirection", 0);
        Scribe_Values.Look(ref firstDrawTick, "firstDrawTick", -1);
        Scribe_Values.Look(ref launchSoundPlayed, "launchSoundPlayed", false);
        if (payloadContainer is null) payloadContainer = new ThingOwner<Thing>(this, oneStackOnly: true);
    }

    public ThingOwner GetDirectlyHeldThings() => payloadContainer;
    public void GetChildHolders(List<IThingHolder> outChildren) { }

    public void LoadPayload(Thing payload, int assignedCoatIndex)
    {
        coatIndex = assignedCoatIndex;
        payloadContainer.TryAddOrTransfer(payload, canMergeWithExistingStacks: false);
    }

    protected override void DrawAt(Vector3 drawLoc, bool flip = false)
    {
        InitializeVisuals();
        var payload = payloadContainer.FirstOrDefault();
        var rotationDegrees = CatMortarRules.FlightRotation(visualFacing, spinDirection, Find.TickManager.TicksGame - firstDrawTick);
        if (IsCatMeat(payload))
        {
            payload!.Graphic.Draw(drawLoc, new Rot4(visualFacing), payload, rotationDegrees);
            return;
        }
        var color = coatIndex >= 0 ? CatMortarVisuals.ColorFor(coatIndex) : Color.gray;
        var graphic = GraphicDatabase.Get<Graphic_Multi>("Things/Pawn/Animal/Cat/Cat", ShaderDatabase.Cutout, new Vector2(CatMortarRules.FlightScale, CatMortarRules.FlightScale), color);
        var rotation = Quaternion.AngleAxis(rotationDegrees, Vector3.up);
        Graphics.DrawMesh(MeshPool.plane10, drawLoc, rotation, graphic.MatAt(new Rot4(visualFacing)), 0);
    }

    protected override void Impact(Thing hitThing, bool blockedByShield = false)
    {
        var map = Map;
        var cell = Position;
        var payload = payloadContainer.FirstOrDefault();
        if (map is null || payload is null) { Destroy(); return; }

        var ammoType = CatMortarRules.AmmoTypeFor(payload is Pawn, payload is Corpse, IsCatMeat(payload));
        switch (ammoType)
        {
            case CatMortarAmmoType.CatMeat:
                MakeCatMeatBarrage(cell, map, payload.stackCount);
                break;
            case CatMortarAmmoType.CatCorpse:
                GenExplosion.DoExplosion(cell, map, CatMortarRules.CorpseExplosionRadius, DamageDefOf.Bomb, Launcher);
                DefDatabase<SoundDef>.GetNamed("Pawn_Cat_Death").PlayOneShot(new TargetInfo(cell, map));
                MakeBloodAndFragments(cell, map, CatMortarRules.BloodCellCount(Rand.Value));
                MakeToxicCloud(cell, map);
                break;
            case CatMortarAmmoType.LivingCat:
                GenExplosion.DoExplosion(cell, map, CatMortarRules.LivingExplosionRadius, DamageDefOf.Bomb, Launcher);
                DefDatabase<SoundDef>.GetNamed("Pawn_Cat_Death").PlayOneShot(new TargetInfo(cell, map));
                MakeBloodAndFragments(cell, map, CatMortarRules.BloodCellCount(Rand.Value));
                ApplyCoatPayload(cell, map, CatMortarRules.PayloadFor(coatIndex));
                ReleaseLivingCat((Pawn)payload, cell, map);
                break;
        }
        Destroy();
    }

    private void ApplyCoatPayload(IntVec3 cell, Map map, CatCoatPayload payload)
    {
        switch (payload)
        {
            case CatCoatPayload.OrangeFire:
                foreach (var fireCell in GenRadial.RadialCellsAround(cell, 3.5f, true))
                    if (fireCell.InBounds(map) && fireCell.Standable(map) && Rand.Chance(0.40f)) FireUtility.TryStartFireIn(fireCell, map, 0.7f, Launcher);
                break;
            case CatCoatPayload.BlackEMP:
                GenExplosion.DoExplosion(cell, map, 4.5f, DamageDefOf.EMP, Launcher, postExplosionGasType: GasType.BlindSmoke, postExplosionGasRadiusOverride: 5f, postExplosionGasAmount: 255);
                for (var index = 0; index < 10; index++) MoteMaker.MakeStaticMote(cell.ToVector3Shifted() + Rand.InsideUnitCircleVec3 * 3.5f, map, DefDatabase<ThingDef>.GetNamed("Mote_CatMortarDarkness"), 4.5f);
                break;
            case CatCoatPayload.CreamFirefoamStun:
                GenExplosion.DoExplosion(cell, map, 4f, DamageDefOf.Extinguish, Launcher, postExplosionSpawnThingDef: ThingDefOf.Filth_FireFoam, postExplosionSpawnChance: 1f, postExplosionSpawnThingCount: 10);
                GenExplosion.DoExplosion(cell, map, 4f, DamageDefOf.Stun, Launcher);
                break;
            case CatCoatPayload.BrownAntigrain:
                GenExplosion.DoExplosion(cell, map, CatMortarRules.BrownAftershockRadius, DefDatabase<DamageDef>.GetNamed("BombSuper"), Launcher);
                break;
        }
    }

    private static void MakeToxicCloud(IntVec3 cell, Map map)
    {
        // Cat Mortar itself has no DLC dependency. A corpse shell simply omits the
        // Biotech-only gas effect when that expansion is not active.
        if (!ModLister.BiotechInstalled) return;
        foreach (var gasCell in GenRadial.RadialCellsAround(cell, 5f, true)) if (gasCell.InBounds(map)) GasUtility.AddGas(gasCell, map, GasType.ToxGas, 180);
    }

    private static void MakeBloodAndFragments(IntVec3 cell, Map map, int bloodCellCount)
    {
        FilthMaker.TryMakeFilth(cell, map, ThingDefOf.Filth_Blood);
        for (var index = 0; index < bloodCellCount; index++)
        {
            var target = cell + new IntVec3(Rand.RangeInclusive(-5, 5), 0, Rand.RangeInclusive(-5, 5));
            if (!target.InBounds(map)) continue;
            FilthMaker.TryMakeFilth(target, map, ThingDefOf.Filth_Blood);
            if (index < 8) MoteMaker.MakeStaticMote(target.ToVector3(), map, DefDatabase<ThingDef>.GetNamed("Mote_CatMortarFragment"), 0.45f);
        }
    }

    // Meat is intentionally not an explosion.  Every loaded unit gets one small,
    // independently scattered impact cell, keeping it situational rather than a
    // second high-precision artillery shell.
    private void MakeCatMeatBarrage(IntVec3 center, Map map, int stackCount)
    {
        // Mote_FoodBitMeat is a native, guaranteed-present meat visual. Using it
        // avoids a second texture path that can break during Core asset changes.
        var fragmentDef = DefDatabase<ThingDef>.GetNamed("Mote_FoodBitMeat");
        DefDatabase<SoundDef>.GetNamed("BulletImpact_Flesh").PlayOneShot(new TargetInfo(center, map));
        foreach (var _ in Enumerable.Range(0, CatMortarRules.CatMeatChunkCount(stackCount)))
        {
            var cell = RandomMeatImpactCell(center, map);
            FilthMaker.TryMakeFilth(cell, map, ThingDefOf.Filth_Blood);
            MoteMaker.MakeStaticMote(cell.ToVector3(), map, fragmentDef, 0.35f);

            var pawn = cell.GetFirstPawn(map);
            if (pawn is not null)
            {
                pawn.TakeDamage(new DamageInfo(CatMortarDefOf.CatMortar_MeatSlap, CatMortarRules.CatMeatChunkDamage, instigator: Launcher));
                if (Rand.Chance(CatMortarRules.CatMeatStunChance))
                    pawn.stances?.stunner?.StunFor(CatMortarRules.CatMeatStunTicks, Launcher, addBattleLog: true, showMote: true, disableRotation: false);
                continue;
            }

            var edifice = cell.GetEdifice(map);
            edifice?.TakeDamage(new DamageInfo(CatMortarDefOf.CatMortar_MeatSlap, CatMortarRules.CatMeatChunkDamage, instigator: Launcher));
        }
    }

    private static IntVec3 RandomMeatImpactCell(IntVec3 center, Map map)
    {
        var candidates = GenRadial.RadialCellsAround(center, CatMortarRules.CatMeatScatterRadius, useCenter: true)
            .Where(candidate => candidate.InBounds(map))
            .ToList();
        return candidates.Count > 0 ? candidates.RandomElement() : center;
    }

    private void ReleaseLivingCat(Pawn cat, IntVec3 center, Map map)
    {
        if (cat is null) return;
        var landing = FindSafeLandingCell(center, map);
        payloadContainer.Remove(cat);
        GenSpawn.Spawn(cat, landing, map);
        CatMortarVisuals.EnsureCoat(cat);
        CatMortarInjuries.ApplyCriticalLandingInjuries(cat);
    }

    private static IntVec3 FindSafeLandingCell(IntVec3 center, Map map)
    {
        foreach (var candidate in GenRadial.RadialCellsAround(center, 9f, true))
            if (CatMortarRules.IsSafeLandingDistance(candidate.DistanceTo(center)) && candidate.InBounds(map) && candidate.Standable(map) && candidate.GetFirstPawn(map) is null && !candidate.ContainsStaticFire(map)) return candidate;
        return CellFinder.RandomClosewalkCellNear(center, map, 12, candidate => candidate.Standable(map) && !candidate.ContainsStaticFire(map));
    }

    private void InitializeVisuals()
    {
        if (visualFacing < 0) visualFacing = Rand.RangeInclusive(0, 3);
        if (spinDirection == 0) spinDirection = Rand.Bool ? 1 : -1;
        if (firstDrawTick < 0) firstDrawTick = Find.TickManager.TicksGame;
        if (!launchSoundPlayed && Map is not null)
        {
            DefDatabase<SoundDef>.GetNamed("Pawn_Cat_Call").PlayOneShot(new TargetInfo(Position, Map));
            launchSoundPlayed = true;
        }
    }

    private static bool IsCatMeat(Thing? thing) => thing is not null
        && CatMortarRules.IsEligibleCatMeat(thing.def.IsMeat, thing.def.defName == "Meat_Cat");
}
