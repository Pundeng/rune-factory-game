## Migration assessment

  Git state: main tracks origin/main. The only working-tree entry is the pre-existing untracked docs/
  COZY_FOOD_FACTORY_GDD. I made no changes, commits, or pushes. prompt.txt names files that are stored here as /C:/
  Users/JHUANPC/Desktop/Projects/rune-factory-game/docs/ARCHITECTURE.md and /C:/Users/JHUANPC/Desktop/Projects/rune-
  factory-game/docs/COZY_FOOD_FACTORY_GDD.

  The GDD explicitly replaces the rune game design. The repository still implements a rune prototype; the architecture
  document was useful as a map, but the classifications below come from code and scene inspection.

   Classification                       Systems and evidence
  ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━  ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
   Reusable as-is                       Grid coordinates and camera controls; four-way building rotation as a utility;
                                        deterministic belt stepping and the existing behavior where removing a belt
                                        discards its item. See /C:/Users/JHUANPC/Desktop/Projects/rune-factory-game/
                                        Assets/_Project/Scripts/Buildings/GridOccupancy.cs and /C:/Users/JHUANPC/
                                        Desktop/Projects/rune-factory-game/Assets/_Project/Scripts/Logistics/
                                        BeltTransportSystem.cs. “As-is” applies to these mechanics, not their rune-
                                        typed interfaces or visuals.
  ───────────────────────────────────  ─────────────────────────────────────────────────────────────────────────────────
   Reusable with modification           Placement, previews, selection, copy/move, belts, output buffering, machine
                                        timing, Hub delivery, and count/rate objective logic. Placement currently fills
                                        a rectangular Vector2Int footprint, so an L-shaped Processor would incorrectly
                                        reserve its empty fourth cell. Transport and buffers carry RuneData; machines
                                        expose one ingredient receiver and one output source. See /C:/Users/JHUANPC/
                                        Desktop/Projects/rune-factory-game/Assets/_Project/Scripts/Buildings/
                                        BuildingPlacement.cs, /C:/Users/JHUANPC/Desktop/Projects/rune-factory-game/
                                        Assets/_Project/Scripts/Logistics/IRuneInputReceiver.cs, and /C:/Users/JHUANPC/
                                        Desktop/Projects/rune-factory-game/Assets/_Project/Scripts/Production/
                                        RuneOutputBuffer.cs.
  ───────────────────────────────────  ─────────────────────────────────────────────────────────────────────────────────
   Obsolete / replacement candidates    RuneData transformations, rune deposits and Extractor behavior, Engraver/
                                        Infuser/Rotator recipes, Acceleration socket inventory, and rune-specific
                                        objective assets and UI. The current Prototype scene (/C:/Users/JHUANPC/
                                        Desktop/Projects/rune-factory-game/Assets/_Project/Scenes/Prototype.unity:687)
                                        offers Extractor, Belt, Engraver, and Infuser; it has no food machine placement
                                        options.
  ───────────────────────────────────  ─────────────────────────────────────────────────────────────────────────────────
   Missing                              Configurable crop growth and switching; property collectors and capacity-
                                        limited pipe networks; food items and automatic recipe matching/discovery; a
                                        two-slot Mixer; L-shaped Processor with property and compost ports; market
                                        sales, currency, orders, and unlocks. No implementation of these appeared in
                                        the inspected runtime scripts or scene.
  ───────────────────────────────────  ─────────────────────────────────────────────────────────────────────────────────
   Unknown / unverified                 A persistent-world save/load contract was not found in the inspected runtime
                                        scripts. Scene and ScriptableObject serialization exists, but that does not
                                        establish save-game support. Unity compilation and gameplay behavior were not
                                        checked in the running Editor.

  ### Main technical gaps and risks

  - Footprints and ports: /C:/Users/JHUANPC/Desktop/Projects/rune-factory-game/Assets/_Project/Scripts/Buildings/
    BuildingDefinition.cs stores width and height; occupancy, group placement, highlights, and previews all assume
    filled rectangles. An occupied-cell shape must be applied consistently before the three-cell Processor can be placed
    or rotated safely. /C:/Users/JHUANPC/Desktop/Projects/rune-factory-game/Assets/_Project/Scripts/Buildings/
    BuildingPreview.cs has visual input/output markers, but its port kinds do not represent ingredient versus property
    versus compost.

  - Transport and mixing: /C:/Users/JHUANPC/Desktop/Projects/rune-factory-game/Assets/_Project/Scripts/Logistics/
    BeltTransportSystem.cs indexes receivers by one cell and passes RuneData. Current processes hold one rune and move
    through idle, processing, and waiting-for-output states. A Mixer needs two independently addressable slots,
    validation before consumption, and a way to retain invalid or ambiguous inputs. A Processor with product and compost
    outputs needs separate output state and blocked-port reporting.

  - Farming and properties: /C:/Users/JHUANPC/Desktop/Projects/rune-factory-game/Assets/_Project/Scripts/Production/
    RuneExtractorProcess.cs provides a useful timed-producer pattern, but its resource is a fixed rune deposit. Crop
    switching, farmland rules, upgrades, and relocation need new state. Property capacity and pipes are a separate
    network; routing them through belts would conflict with the GDD.

  - Recipes, sales, and persistence: /C:/Users/JHUANPC/Desktop/Projects/rune-factory-game/Assets/_Project/Scripts/
    Production/ElementInfuserProcess.cs snapshots a manually selected transformation when an item arrives. The GDD
    requires automatic matching against the complete property set, exactly one match, and discovery on first
    production. /C:/Users/JHUANPC/Desktop/Projects/rune-factory-game/Assets/_Project/Scripts/Objectives/
    ObjectiveProgress.cs has reusable counting/rate logic, but matches whole runes. /C:/Users/JHUANPC/Desktop/Projects/
    rune-factory-game/Assets/_Project/Scripts/Objectives/Hub.cs consumes deliveries without prices, orders, or currency.
    A persistent world will also need explicit save state for farm settings, networks, machine buffers, discoveries,
    inventory, and progression if none exists elsewhere.

  - Replacement dependency: Rune types cross transport interfaces, belt contents, output buffers, production processes,
    Hub/objectives, UI, serialized assets, tests, and FantasyShapez assembly/namespace references. Replacing RuneData
    alone would break these boundaries. The safest migration is a narrow, staged item contract change with scene/prefab
    and test updates at each boundary; preserve old assets until the new loop is verified.

  ### Decisions for senior developer review

  The GDD fixes the one-source capacity network, non-consumption of invalid inputs, automatic matching, two-input Basic
  Mixer, three-cell L-shaped Processor, market delivery, and blocked-port warnings. It leaves critical rules open: port
  locations and rotation, behavior when property demand exceeds capacity, pipe crossings, buffer clearing, crop timing
  and catalog, recipe quantities, prices, and save/load scope. Compost generation is described as probabilistic for some
  advanced recipes, while compost recycling and multi-property recipes are deferred from the proposed initial prototype.
  Decide a minimal deterministic first recipe catalog and the open port/supply rules before implementing the vertical
  slice; do not treat the GDD’s example recipes or prototype scope as finalized balance.

  ### Smallest phased plan

  1. Food transport foundation: Introduce a food-item identity at the transport boundary, adapt belts and a simple farm
     producer, and prove crop switching plus delivery through existing placement tools.

  2. Playable production loop: Add one-property supply, the shaped Processor, two-slot Mixer, and a small data-driven
     recipe catalog. Match recipes automatically; keep invalid or ambiguous inputs in place and stop on blocked outputs.

  3. Discovery and sales: Record first production, show a simple discovery notification/journal entry, deliver products
     to a market sink, and award currency. Use one or two orders to exercise progression.

  4. Broaden after the loop works: Add full pipe planning, region/seed progression, richer discovery presentation, then
     compost and later GDD systems. Retire rune scene wiring and assets only after replacement paths are validated.