# Cozy Food Factory — Design Summary

> Quick-reference overview for development. The current **Cozy Food Factory GDD.md** is the authoritative design source. If this summary conflicts with the GDD, follow the GDD. The archived Fantasy Rune Factory design summary is historical reference, not a specification for this game. This document describes intended gameplay, not proof of implemented features.

## Product promise

Grow a small farm into a charming automated food-production kingdom by choosing crops, discovering recipes through experimentation, and designing readable production lines. Farming, recipe discovery, and factory automation are equally important.

## Player experience

- Cozy, playful, visually charming 2D top-down factory building on a grid-based single persistent map.
- No player avatar or combat in the current concept; players plan and operate the factory with an omniscient camera.
- Discovery should create exciting new production possibilities, while factory design rewards clear layouts and thoughtful logistics.
- The main loop is: acquire/restore farmland and regions → grow crops → obtain processing properties → experiment and discover foods → automate production → sell and fulfill orders → unlock more of the world.
- Decoration is a separate activity supported by special currency from popup events.

## Farming and resources

- Farmland is reusable. Players select crops, can reset or switch planted crops, and eventually upgrade or relocate farming areas.
- Crops are produced automatically; ongoing raw-resource production does not require recurring monetary input.
- Seeds unlock through progression, orders, region restoration, and the seed shop.
- Crop catalog, production timings, and detailed upgrade rules are not yet finalized.

## Processing properties

Four culinary properties replace the former rune-element gameplay:

| Property | Intended use |
| --- | --- |
| Heat | Cooking, roasting, heating |
| Moisture | Boiling, simmering, extracting |
| Time | Aging, fermentation, preservation |
| Air | Drying, aeration, whipping |

- Special environmental nodes supply properties to processors through a separate pipe or connection network, not item belts.
- Initial supply nodes have limited capacity. Separate supply nodes do not merge into one shared network under the current design.
- Production-rate-based supply is a future possibility; detailed capacity, consumption, routing, and pipe-crossing rules require confirmation before implementation.

## Food manufacturing and recipes

- **Processor:** three occupied cells in an L-shaped footprint; one ingredient type, a separate property-supply port, a product output, and a separate compost output when relevant. Supports one property initially and multiple properties later.
- **Basic Mixer:** 1×2 footprint, two independent ingredient input ports, automatic recipe matching once both ingredient slots are filled, no processing-property requirement.
- **Advanced Mixer:** later machine for three or more ingredients; footprint and port layout are undecided.
- Processing recipe: `Ingredient + Property Set → Result`.
- Mixing recipe: `Ingredient Set → Result`.
- A machine selects no fixed recipe. It examines its available inputs and, for processing, **all connected properties**. Produce only if exactly one valid recipe matches.
- Invalid or ambiguous combinations neither consume ingredients nor create outputs. A previously undiscovered recipe may be discovered through automated production.
- Recipes generally span two to four production stages. Examples in the GDD illustrate the intended structure; they do not establish a final recipe catalog or balance.

## Logistics and failure feedback

- Belts move ingredients and products; reinstalling a belt deletes any item occupying that belt.
- Incorrect items can be deliberately discarded without compensation.
- Blocked machines stop. The affected output port shows a red warning.
- Certain advanced recipes can generate probabilistic compost, stored in a small separate buffer and discharged via a dedicated output port. If compost cannot discharge and its buffer fills, the machine stops.
- Exact port coordinates, capacity and clearing rules require implementation decisions; do not infer them from legacy rune machines.

## Discovery, economy, and progression

- First-time recipe discovery triggers a popup, a short playful anticipation/transformation reveal, and registration in a recipe journal. Specific animation production is undecided.
- Sell food at the market and complete orders to unlock seeds, machines, farmland, and regions.
- Exact recipes, order values, prices, timing, unlock order, and progression balance remain provisional.

## Existing Unity project: transition status

The game reuses the existing Fantasy Rune Factory Unity repository rather than starting over. Reuse mechanics when appropriate, but do not treat rune-specific design as authoritative.

- Grid, camera, basic rotation and deterministic belt stepping are reusable foundations.
- Placement/blueprint tools, rectangular footprints, input/output contracts, buffers, manufacturing, Hub delivery and objectives need targeted adaptation.
- The L-shaped Processor needs occupied-cell geometry rather than a filled bounding rectangle; the Mixer needs separately addressable inputs; the Processor needs distinct product/property/compost ports.
- RuneData, rune machines, sockets, rune progression and related scenes/UI remain migration candidates; preserve legacy assets until new paths have been validated.
- **Food Foundation Issue 1:** Codex reported adding `FoodItemData`, food-capable transport interfaces and adapters for existing rune sources/receivers, plus `FoodItemTransportTests`. Offline C# project compilation and `git diff --check` passed. Unity EditMode tests and Play Mode transport behavior had **not** been verified at the time of that report. Confirm the actual repository/Git state before relying on this status.

## Near-term implementation sequence

1. Confirm Issue 1 in the running Unity Editor and preserve changes in Git.
2. Automated farming and crop switching, producing food through the existing `IItemOutputSource` transport boundary.
3. Minimal market delivery to prove farm → belts → delivery.
4. Occupied-cell footprints, processing-property supply, automatic Processor recipes and two-input Mixer.go
5. Recipe journal/discovery presentation, sales/currency and basic orders.
6. Expand region/seed progression, richer property networks, advanced machines, compost and decoration based on the current GDD and playtesting.

## Open decisions — do not invent during unrelated tasks

- Exact crop and recipe catalogs, ingredient quantities, timings, prices, and unlock sequence.
- Rotated port positions for L-shaped Processor and Mixer; crossings and routing of property networks.
- Property-demand behavior when a node reaches capacity; exact supply/consumption semantics.
- Crop upgrading/relocation rules, machine buffer clearing, and item disposal UX.
- Scope and timing of persistent save/load; inventory, discovery, farm and machine state to persist.
- Advanced Mixer layout, multi-property processing, compost details and full discovery animation.
