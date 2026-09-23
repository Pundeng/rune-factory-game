# Fantasy Rune Factory — AGENTS.md

## Authority

- Work on the existing Unity prototype. Reuse working code, scenes, prefabs, assets, and conventions instead of rebuilding systems.
- The repository is the source of truth for implementation. Do not claim repository facts without inspecting them.
- Game design documents describe intended direction, not necessarily implemented behavior or finalized balance.
- Read additional documentation only when relevant to the task.

## Product Constraints

- This is a top-down, Shapez-style factory automation game with an omniscient camera, no player avatar, and no combat.
- Core gameplay centers on manufacturing, delivering, combining, and using magical runes to improve the factory.
- Do not introduce power, fuel, resource depletion, durability, maintenance, pollution, survival, random crafting failures, or waste without explicit authorization.
- Do not implement speculative systems or late-game architecture during unrelated tasks.

## Rune and Processing Rules

- Represent runes as structured data, not separate hardcoded identities such as `FireAttackRune`.
- Keep transformation logic independent of sprites, animation, UI, and scene hierarchy.
- Reuse common transport and processing behavior instead of introducing family-specific implementations unnecessarily.
- Glyph rotation is no longer a required gameplay property. Preserve legacy rotation dependencies until an explicit migration task authorizes their removal.
- Prefer configurable recipes and machines over creating a new machine or hardcoded branch for every rune.

## Unity and Code Conventions

- Follow the existing Unity version, project structure, naming, and serialization conventions.
- Keep gameplay code under `Assets/_Project/` unless existing architecture requires otherwise.
- Do not manually edit `.meta` GUIDs or unnecessarily regenerate assets.
- Preserve working scenes, prefabs, UI, serialized data, and unrelated changes.
- Keep logical gameplay state separate from presentation.
- Keep grid placement, item transport, processing, and validation deterministic.
- Prefer small focused changes, composition, and existing patterns over speculative abstractions or broad refactors.
- Avoid introducing new warnings when reasonably possible.

## Task Workflow

1. Check the current branch and working tree before editing.
2. Inspect only files relevant to the task. Reproduce reported bugs when feasible.
3. Implement the smallest coherent change that satisfies the acceptance criteria.
4. Add or update focused tests when behavior changes.
5. Run relevant validation and review the final diff for unintended changes.

If requirements are ambiguous, preserve existing behavior and report the decision needed rather than inventing major design rules.

## Git Safety

- Stay on the current branch unless instructed otherwise.
- Never discard or overwrite unrelated user changes.
- Never use destructive Git operations without explicit authorization.
- Do not create branches, commit, push, or merge unless explicitly requested.
- Stage only task-related files when committing.
- Do not commit generated caches, logs, IDE files, or build outputs.

## Validation and Reporting

- Validate the smallest affected layer first, then relevant Unity integration.
- Distinguish automated tests, compilation, Unity Editor checks, PlayMode checks, and manual verification.
- Never claim tests or validation passed unless they actually ran successfully.
- Keep completion reports concise: changes, validation results, remaining manual checks, and relevant limitations.
- Do not repeat the original task or provide unrelated architecture commentary.

## Prompt and Context Efficiency

- Follow the task prompt and relevant project documentation without repeatedly restating stable rules.
- Do not inspect the entire repository when a focused investigation is sufficient.
- Do not broaden scope, add optional features, or refactor unrelated code without authorization.
- Prefer preserving existing behavior, player readability, determinism, testability, and implementation simplicity.

## Unity Validation

- Assume the Unity Editor is already running.
- Do not launch Unity, Unity Hub, or Unity batch mode during normal development.
- Do not run Unity Test Runner through a separate Unity process.
- Do not terminate existing Unity processes.
- Do not modify or delete Unity's Library, Temp, or licensing files.
- Validate changes through code inspection and available non-Unity checks.
- Leave Unity compilation, EditMode tests, PlayMode tests, and gameplay validation to the running Editor.
- Clearly report which checks were completed and which require manual Unity verification.
- Never report Unity tests as passed unless they actually ran successfully.