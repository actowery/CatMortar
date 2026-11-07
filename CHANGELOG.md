# Changelog

## 1.1.0 - 2026-11-07

- Add cat meat ammunition: load up to 20 pieces, then scatter one direct-hit meat chunk per piece across a 7-tile field. Damage, brief-stun chance, blood, and light building damage scale with the loaded rack.
- Add automatic nearby cat-meat reload that never falls back to loading a colony cat.
- Add **Waste loaded ammunition** to permanently clear the chamber, cancel automatic actions, and stand the operator down.
- Add persisted forced pawn/building targets, repeated firing after reload and cooldown, and **Clear forced target**. Force target uses RimWorld's rebindable Misc 4 hotkey (default `N`).
- Refine meat audio: no cat vocalization; use a layered flesh-splat and blunt-thump impact instead.
- Fix survivor landing injuries for cats that already have missing limbs.
- Refresh Workshop, README, manual-test, package, and release documentation.

## 1.0.0 - 2026-10-31

- Initial public release of Cat Mortar for RimWorld 1.6 and 1.7 unstable.
- Add a buildable, manned vanilla-style mortar that uses one colony cat or cat corpse as ammunition.
- Give each colony cat a persistent, visible orange, black, cream, or brown coat before loading; preserve that exact coat through flight and survivor recovery.
- Add coat-specific delayed payloads, corpse toxic-gas rounds, vanilla-style gore, min-range targeting, roof restrictions, and mortar-style operator/reload flow.
- Add dual-version builds, automated tests, package validation, Codecov, CodeQL, dependency review, and release automation.
