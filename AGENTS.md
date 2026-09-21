# Fantasy Rune Factory AGENTS.md

## Purpose and authority

This file guides Codex work on the existing Unity prototype for Fantasy Rune Factory.

- Treat the current project as a substantially built prototype, not a greenfield rewrite.
- Reuse working architecture, scenes, prefabs, assets, and workflows before adding replacements.
- The repository is the authority for implementation details. This file is the authority for product direction and work conventions.
- The integrated design is directional, not a final recipe table or balance specification.
- If code, tests, saved data, or prefabs depend on an obsolete rule, assess dependency and migration cost before removal.
- Do not claim a repository fact until it has been inspected in the current task.

## Product direction

Fantasy Rune Factory is a top-down factory automation game about discovering, manufacturing, upgrading, and delivering magical runes.

The player controls an omniscient Shapez-style camera and builds factories. There is no player avatar, character movement, or combat.

Core loop:

```text
Discover recipe or contract
-> secure limited-throughput inputs
-> design supply and processing lines
-> deliver, socket, or consume runes in advanced recipes
-> improve the factory
-> discover the next magical family
```

Depth should come from recipe relationships, meaningful processing order, production ratios, shared-resource allocation, logistics, throughput, and choices between delivery, machine upgrades, and advanced crafting.

Do not introduce power, fuel, depletion, enemies, combat, survival, durability, pollution, maintenance, random crafting failure, or waste systems unless a task explicitly changes the design.

## Rune model

Runes must be structured data, not identities encoded as names such as `FireAttackRune`.

The model must be able to express, as needed:

- central sigil or rune family,
- element,
- base, synthesis, ascension, or optional layered state,
- recipe ancestry or ingredients where gameplay requires it,
- optional material or core type if that system is later validated.

Current rune families include two broad uses without creating separate item systems:

- Delivery-oriented magical sigils, such as spirit, celestial, dragon, and ancient families.
- Functional sigils, such as acceleration, connection, storage, and distribution.

Functional runes may also be delivered or consumed as ingredients or catalysts. A category describes primary use, not a different inventory type.

Elements currently planned are fire, water, earth, and wind. Do not assume every element, family, recipe, name, tier count, or unlock order is final.

### Rotation policy

Glyph rotation is no longer a mandatory production property or target condition.

- Do not add new objectives, recipes, UI, or machines that require rune rotation.
- Do not assume the existing Rotator, rotation fields, tests, serialization, or save data can be deleted immediately.
- Before removal, trace dependencies in data, validation, UI, prefabs, scenes, tests, and compatibility paths.
- Prefer disabling obsolete gameplay use first; remove code only when the task defines migration and acceptance criteria.
- Machine animation may rotate visually; that does not give the manufactured rune a directional quality.

Attack and Split glyphs, fixed 0/90/180/270 glyph rotations, and left/right elemental zones are obsolete MVP 0.1 specifications. Preserve them only where temporarily required for compatibility during an explicit migration.

## Processing model

Machines transform rune data through explicit, deterministic operations:

```text
Input rune data + configured recipe
-> validate inputs
-> consume inputs
-> apply transformation
-> produce output rune data
```

Keep transformation logic independent of sprites, animation, VFX, UI, and scene hierarchy so it can be tested without rendering.

Prefer shared processing and port behavior over family-specific transport code. A family-specific Engraver may have unique configuration and presentation while reusing common processing logic.

Introduce a new machine only when it creates a genuinely new production behavior. Do not add one machine per recipe when configuration of an existing process is sufficient.

### Later core systems

Synthesis and ascension are planned core progression systems, but they are not prerequisites for every current task.

- Synthesis transforms multiple completed runes into a new sigil or rune.
- Ascension evolves an existing rune using explicit sacrifices or catalysts.
- Sacrifices and catalysts are deterministic recipe inputs, not random enhancement chances.
- Layering preserves multiple rune identities and remains a later candidate, not a committed near-term requirement.
- Refining, mixed mana, and special core materials are introduced only after a demonstrated production need.

Design data and operations so synthesis and ascension can be added compositionally. Do not prebuild their full frameworks during unrelated work.

## Machine rune sockets

The intended upgrade model is one rune socket per machine.

- A completed rune is consumed or assigned to the socket according to the eventual replacement policy.
- Socketed runes should create visible machine behavior, not only hidden percentage bonuses.
- The first intended example is an acceleration rune that changes an Engraver's operating cadence and increases observable throughput.
- Upgrade runes compete with delivery and advanced-recipe demand.
- Exact install, removal, replacement, and refund behavior is unresolved; do not invent it outside the task.

Keep socket effects compositional and data-driven where practical. Avoid a global manager or inheritance hierarchy solely for hypothetical future effects.

## Resources and world

Prefer throughput limits over finite depletion:

- extraction point count and items per second create scarcity,
- rune families share inputs and create allocation choices,
- distance should create a meaningful logistics tradeoff rather than busywork.

Mixed deposits, refining, special materials, procedural worlds, and long-distance transport aids are candidates. Add them only through explicit tasks backed by a tested gameplay need.

## Campaign and freeplay

Campaign progression teaches one manufacturing concept at a time. Early play should be readable by shape and feedback; deeper optimization should emerge from the same rules rather than mandatory upfront explanation.

Freeplay contracts select only unlocked, manufacturable results and may vary cumulative quantity, sustained delivery rate, simultaneous delivery conditions, or mixed cumulative and sustained targets.

Delivery-rate checks must use actual Hub delivery across a suitable measurement window. A temporary buffer dump must not satisfy a sustained-rate contract. Long-cycle advanced runes need fair averaging windows. Simultaneous contracts must be true within the same evaluation interval.

Never generate impossible contracts, locked ingredient requirements, meaningless direction requirements, or random synthesis failures.

A universal factory comparable to a MAM may be an emergent player goal in long-term freeplay. It is not inevitable, is not a committed machine or feature, and must not drive present architecture beyond ordinary configurability and reuse.

## Phased implementation

Follow this order unless a task provides a narrower priority:

1. Stabilize existing extraction, belts, processing, Hub consumption, objective validation, and runtime configuration updates.
2. Assess rotation dependencies and remove rotation from required targets without breaking compatibility.
3. Validate one complete delivery rune and one player-manufactured socket upgrade with observable throughput impact.
4. Add early spirit sigils and test two simultaneous deliveries sharing resources.
5. Add a second magical family by reusing common processing and test resource allocation.
6. Validate one synthesis recipe and one ascension recipe using explicit sacrifices or catalysts.
7. Add refining, materials, world-generation complexity, or layering only when playtesting demonstrates the need.

The near-term success criterion is not the presence of every planned system. It is that manufacturing a rune, upgrading a machine, and fulfilling competing deliveries creates a new and understandable factory-design problem.

## Known prototype context

The supplied design handoff describes the Unity MVP as roughly 80 percent built. Work from the existing project rather than recreating it.

The handoff identifies these areas for early verification:

- incorrect rune consumption at the Hub,
- Play Mode configuration changes not taking effect,
- collisions or transfer problems involving reverse-facing belts.

These are reported concerns, not repository-verified facts. Reproduce and inspect them before changing code.

## Architecture and Unity conventions

### General

- Use Unity 6 compatible APIs. The supplied project convention names Unity `6000.5.2f1`; verify the actual project version when relevant.
- Keep gameplay code under `Assets/_Project/` and follow the closest existing folder convention.
- Preserve Unity-managed settings and generated folders.
- Do not edit `.meta` GUIDs manually.
- Do not delete or regenerate assets unless the task requires it.
- Preserve existing UI and art unless the task explicitly changes them.
- Reuse existing systems and patterns before introducing managers, packages, or parallel sources of truth.

### Separation of concerns

- Keep logical state independent of presentation.
- Let visuals represent state rather than define it.
- Keep grid conversion and occupancy logical and deterministic.
- Use explicit machine input/output contracts.
- Do not use physics as the source of truth for item transport.
- Keep belt movement and handoff predictable and easy to debug.
- Store important gameplay state in data or dedicated components, not only in scene object arrangement.

### C# style

- Use descriptive names and small focused classes.
- Use PascalCase for public types and members, camelCase for locals and private fields, and explicit access modifiers.
- Prefer `[SerializeField] private` for Inspector configuration.
- Prefer composition over inheritance when it reduces coupling.
- Avoid broad manager classes, speculative abstractions, and unrelated refactors.
- Keep new warnings out of the project when reasonably possible.

### Data and configuration

- Prefer data-driven recipes, objectives, rune definitions, and machine configuration.
- Do not duplicate runtime state across components without a clear ownership rule.
- When direction matters for building ports or logistics, use the existing placement rotation or transform as the source of truth.
- Do not convert planned content into hardcoded enums or branches unless the task and current architecture make that the smallest safe solution.

## Testing and validation

Test the smallest relevant layer first, then the Unity integration affected by the change.

Priority deterministic tests include:

- grid coordinate conversion and occupancy,
- rune value equality and serialization,
- recipe validation and transformations,
- machine input consumption and output production,
- belt handoff and throughput,
- Hub target validation and sustained-rate windows,
- socket installation and effect behavior,
- synthesis and ascension when implemented,
- compatibility during removal of obsolete rotation requirements.

Avoid tests that only restate Unity lifecycle calls. Distinguish clearly between pure C# or offline tests, Unity EditMode tests, Unity PlayMode tests, player builds, and manual Editor verification. Never report a validation type that was not actually run.

## Codex task workflow

For each task:

1. Read this file and the task prompt.
2. Check working-tree and branch state before editing.
3. Inspect only the implementation paths needed for the task.
4. Reproduce reported regressions before patching when feasible.
5. Reuse the nearest working pattern.
6. Make the smallest coherent change that meets the acceptance criteria.
7. Add or update focused tests when behavior changes.
8. Run validation proportional to risk and report unavailable validation honestly.
9. Review the final diff for scope, generated files, and accidental asset changes.
10. Commit and push only when the task explicitly requests them.

If requirements are ambiguous, preserve current behavior and data, avoid inventing major design rules, and surface the decision needed. Do not silently broaden the task.

Do not require every task to start from `main`, create a branch, commit, or push. Follow the task's explicit Git delivery instructions and preserve unrelated user changes.

## Git conventions

- Never discard, overwrite, or reformat unrelated changes.
- Do not use destructive reset or checkout operations without explicit authorization.
- Stage only task-related files.
- Keep commits focused when commits are requested.
- Do not commit generated caches, logs, IDE files, temporary artifacts, or build outputs.
- Never merge to `main` unless explicitly requested.

Preferred commit prefixes:

```text
feat: fix: refactor: test: chore: docs:
```

## Completion report

Keep the final report concise and evidence-based. Include only what is relevant: outcome, changed files, tests and checks actually run, manual Unity steps still needed, known limitations or unresolved decisions, and branch and commit hash when applicable.

Do not restate the entire task or provide speculative architecture commentary.

## Prompt efficiency

Task prompts should normally contain only:

```text
Goal
Relevant context
Constraints
Acceptance criteria
Do not touch
Git delivery
```

Reference this file instead of repeating stable project rules. Keep one focused issue per task and replace temporary `prompt.txt` content rather than accumulating old instructions.

## Design decision rule

When two approaches both satisfy the task, prefer the one that best preserves:

1. existing working behavior,
2. player readability,
3. determinism,
4. testability,
5. compositional reuse,
6. implementation simplicity.

Do not sacrifice current clarity for a hypothetical universal factory or an unconfirmed late-game system.