# Manual test checklist

1. Research Mortars, build **Cat Mortar**, and verify its icon, cat-themed mortar sprite, cost, and one-cell placement.
2. Use **Load cat** on a reachable colony cat. Confirm a colonist carries that exact pawn inside, its coat visibly changes, and the mortar reports loaded status.
3. Save and reload with a cat loaded; confirm it remains held. Destroy the mortar and confirm the stored living cat is released instead of deleted.
4. Select **Load cat meat** on a cat-meat stack. Confirm it loads no more than 20 pieces, reports its chunk count and 7-tile scatter area, persists through save/load, and is never chosen by automatic reload.
4. Fire each live coat, confirming a 1.25× spinning cat during flight, 4.5-tile primary blast, blood/fragments, and the relevant orange fire, black EMP/smoke/darkness, cream firefoam/stun, or brown 6-tile aftershock.
5. Confirm the exact cat lands 7–9 cells away after all effects, with non-vital injuries and severe bleeding but no immediate vital-part death.
7. Load a colony cat corpse. Confirm its 3-tile degraded blast, toxic cloud, no coat payload, and no survivor.
8. Fire 1, 10, and 20 cat meat. Confirm each loaded piece creates one direct-impact chunk somewhere within the 7-tile scatter field; confirm chunk damage rises from 4 to 7 and brief-stun chance from 15% to 35% as the rack fills, alongside light building damage, blood, a restrained flesh-impact splat sound, and no meaningful splash. Confirm no toxic cloud, coat payload, or survivor.
7. Save during flight, reload, and confirm the projectile retains its exact cat/corpse payload and detonates normally.
8. Repeat smoke, firefoam, EMP, and toxic-gas checks on both RimWorld 1.6 and 1.7.
