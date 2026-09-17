
# Fantasy Shapez — AGENTS.md

## Project Overview

Fantasy Shapez is a 2D factory automation game inspired by Shapez.

The core gameplay is about:

- extracting rune stones,
- engraving glyphs,
- rotating glyphs,
- infusing elemental properties,
- combining rune structures,
- transporting processed runes through factories,
- and eventually building a universal rune factory capable of producing arbitrary target runes.

The game should focus on production logic and factory design rather than factory management.

Do NOT introduce unnecessary systems such as:

- power consumption,
- fuel,
- resource depletion,
- enemy attacks,
- combat,
- player-controlled characters,
- maintenance,
- pollution,
- waste systems,
- durability,
- survival mechanics.

The main source of complexity should come from WHAT the player produces and HOW the production chain is designed.

---

# Current Development Goal

The current target is MVP 0.1.

MVP 0.1 should validate the core gameplay loop:

Rune Source
→ Belt Transport
→ Processing Machines
→ Hub Delivery
→ Objective Progress

The first playable version should include:

- Grid
- Camera controls
- Building placement
- Rune stone resource node
- Rune extractor
- Belts
- Engraver
- Glyph rotator
- Element infuser
- Hub
- Target rune validation
- Basic objectives

Available rune components for MVP 0.1:

Base shape:
- Circle

Glyphs:
- Attack
- Split

Glyph rotations:
- 0°
- 90°
- 180°
- 270°

Elements:
- Fire
- Air

Element zones:
- Left
- Right

---

# Core Architecture Principles

## 1. Rune state must be data-driven

A rune should be representable as structured data.

Conceptually:

```text
Rune
- baseShape
- glyphs[]
- elementZones[]
- sealTier
- layers[]
```

Possible supporting structures:

```text
Glyph
- type
- rotation
- slot

ElementZone
- zone
- element
```

The exact implementation may evolve.

Do not hardcode rune identity using strings such as:

```text
"FireAttackRune"
"AirSplitRune"
```

Rune identity should be determined by its data.

---

## 2. Machines operate on rune data

Machines should apply transformations to rune data.

Conceptually:

```text
Input Rune
→ Apply Operation
→ Output Rune
```

Examples:

- EngraveOperation
- RotateGlyphOperation
- InfuseElementOperation
- ApplySealOperation
- OverlayOperation

Do not tightly couple rune transformation logic to visual GameObjects.

Rune processing should be testable without rendering.

---

## 3. Separate gameplay logic from presentation

Game logic should not depend directly on:

- sprites,
- animations,
- VFX,
- UI,
- scene hierarchy.

Where possible, keep logical state in plain C# classes or clearly separated components.

Visuals should represent game state, not define it.

---

## 4. Design for future automation

All machine behavior should eventually be configurable by automation.

Future endgame systems will include a Make Anything Machine (MAM).

Avoid implementations where a building can only ever perform one permanently hardcoded operation.

Example:

Bad:

```text
AttackGlyphEngraver
```

Preferred direction:

```text
Engraver
- selectedGlyph
- rotation
```

This allows future systems to dynamically configure machines.

Do not implement MAM functionality yet.

Only ensure current architecture does not make it impossible.

---

## 5. Avoid premature overengineering

Do NOT build systems before they are needed.

Examples that are not required yet:

- chunk streaming
- infinite world generation
- save/load architecture
- networking
- multiplayer
- ECS
- DOTS
- complex dependency injection
- procedural biomes
- portal networks
- advanced research trees
- rune layering
- seals
- elemental fusion

Prefer the simplest implementation that cleanly supports the current feature.

---

# Grid System

The game uses a logical 2D grid.

Requirements:

- World positions can convert to grid coordinates.
- Grid coordinates can convert to world positions.
- Buildings occupy grid cells.
- Building placement must rely on logical grid coordinates.
- Grid logic should not depend on visuals.

Do not build chunking unless it becomes necessary.

---

# Building System

A building conceptually contains:

```text
Building
- type
- position
- rotation
- inputPorts[]
- outputPorts[]
- processingSpeed
```

Exact implementation may differ.

Buildings should communicate through clear input/output rules.

Avoid building-specific transport hacks.

---

# Logistics

The logistics system should remain deterministic and understandable.

Belts transport rune items between buildings.

Important priorities:

1. correctness,
2. clear visual behavior,
3. predictable throughput,
4. ease of debugging.

Do not add realistic physics-based item transport.

---

# Code Organization

Project code should live under:

```text
Assets/_Project/
```

Recommended structure:

```text
Assets/_Project/
├── Art/
├── Audio/
├── Materials/
├── Prefabs/
├── Scenes/
├── Scripts/
│   ├── Core/
│   ├── Grid/
│   ├── Camera/
│   ├── Buildings/
│   ├── Logistics/
│   ├── Runes/
│   └── UI/
├── ScriptableObjects/
└── Tests/
```

Do not place gameplay scripts directly under `Assets/`.

Unity-generated settings should remain in their existing Unity-managed folders.

---

# C# Style

Use:

- clear descriptive names,
- small focused classes,
- PascalCase for public types and members,
- camelCase for local variables and private fields,
- explicit access modifiers,
- `[SerializeField] private` instead of public fields for Inspector configuration.

Prefer composition over inheritance when reasonable.

Avoid unnecessary abstractions.

Avoid large "manager" classes that control unrelated systems.

---

# Unity Guidelines

Use Unity 6 compatible APIs.

The project version is:

```text
Unity 6000.5.2f1
```

Do not modify generated folders such as:

- Library
- Temp
- Logs
- obj
- UserSettings

Do not manually edit `.meta` GUIDs.

Do not delete or regenerate assets unless required by the task.

Preserve existing project settings unless a task explicitly requires changing them.

---

# Scene and Prefab Guidelines

Keep scene hierarchy simple.

Do not store important gameplay state only inside scene objects.

Reusable machines should become prefabs once appropriate.

Avoid creating large numbers of manually configured scene-specific objects when they can be generated or configured from data.

---

# Testing

Core logic should be testable where practical.

Priority test targets:

- grid coordinate conversion,
- rune equality,
- rune transformations,
- glyph rotation,
- element zone assignment,
- objective validation.

Avoid writing tests that only verify Unity lifecycle calls.

Use deterministic tests.

---

# Codex Task Rules

When implementing a task:

1. Inspect the existing project first.
2. Reuse existing systems where appropriate.
3. Do not rewrite unrelated systems.
4. Keep the change focused on the requested feature.
5. Avoid speculative features.
6. Do not add third-party packages unless explicitly required.
7. Do not silently change project architecture.
8. Keep Unity compilation clean.
9. Do not leave warnings caused by newly added code when reasonably avoidable.
10. Summarize modified files after completing the task.

If a requirement is ambiguous:

- prefer the smallest implementation consistent with this document,
- avoid inventing major gameplay rules,
- leave extensibility points only where clearly useful.

---

# Git Guidelines

Keep commits focused when commits are requested.

Suggested commit prefixes:

```text
feat:
fix:
refactor:
test:
chore:
docs:
```

Examples:

```text
feat: add grid coordinate system
feat: add camera pan and zoom
feat: add rune data model
fix: correct belt item transfer
test: add rune rotation tests
```

Do not include:

- generated cache files,
- IDE-specific files,
- temporary build artifacts.

---

# Non-Goals for MVP 0.1

Do not implement the following unless explicitly requested:

- elemental fusion,
- secondary elements,
- seals,
- rune layering,
- biome generation,
- portals,
- research tree,
- blueprints,
- copy/paste,
- MAM,
- giant rune arrays,
- save system,
- production statistics,
- optimization tools.

These belong to later versions.

---

# Design Priority

When choosing between two approaches, prefer the option that improves:

1. clarity,
2. determinism,
3. testability,
4. modularity,
5. future automation support.

Do not sacrifice simplicity for hypothetical future flexibility.

The game should remain easy to reason about as factory complexity grows.
