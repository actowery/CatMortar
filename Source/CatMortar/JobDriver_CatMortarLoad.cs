using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace CatMortar;

public sealed class JobDriver_CatMortarLoad : JobDriver
{
    private Thing Cat => TargetA.Thing;
    private Building_CatMortar Mortar => (Building_CatMortar)TargetB.Thing;

    public override bool TryMakePreToilReservations(bool errorOnFailed) => pawn.Reserve(Cat, job, 1, -1, null, errorOnFailed) && pawn.Reserve(Mortar, job, 1, -1, null, errorOnFailed);

    protected override IEnumerable<Toil> MakeNewToils()
    {
        yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
        yield return Toils_Haul.StartCarryThing(TargetIndex.A);
        yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.Touch);
        yield return new Toil
        {
            initAction = () => { if (!Mortar.TryAccept(pawn.carryTracker.CarriedThing)) EndJobWith(JobCondition.Incompletable); },
            defaultCompleteMode = ToilCompleteMode.Instant
        };
    }
}
