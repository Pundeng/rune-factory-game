# Cozy Food Factory — AGENTS.md

## Authority

- Work in the existing Unity project. Reuse working code, scenes, prefabs, assets, and conventions when suitable for Cozy Food Factory; do not preserve obsolete rune behavior merely because it exists.
- The repository is the source of truth for implementation. Do not claim a feature is implemented without inspecting its code or assets.
- The current Cozy Food Factory GDD is authoritative for intended gameplay. `docs/ARCHITECTURE.md` describes the existing implementation and may include Fantasy Rune Factory assumptions. Archived design documents are historical reference only.
- The GDD describes intended behavior, not proof of implementation or finalized balance. Distinguish confirmed decisions from proposals or undecided rules.
- Read only task-relevant documentation and files.

## Product Constraints

- Cozy farming, recipe discovery, and factory automation on a grid-based, 2D top-down persistent map; omniscient camera, no player avatar or direct combat.
- Preserve three core activities: automated crop production, discovering recipes through ingredient/property combinations, and designing production lines.
- Food/items use belts; processing properties use a separate supply network when implemented. Do not prematurely implement undecided pipe/network rules.
- Machines determine recipes automatically from their available inputs. Produce a result only when exactly one recipe matches; invalid or ambiguous combinations do not consume ingredients.
- Do not assume legacy runes, RuneData, rune machines, upgrade sockets, or rune progression are new-game requirements.
- Do not implement deferred systems, speculative abstractions, or unapproved design/balance rules during unrelated tasks.

## Unity and Code Conventions

- Follow the existing Unity version, structure, naming, and serialization conventions.
- Keep gameplay code under `Assets/_Project/` unless existing architecture requires otherwise.
- Do not manually edit `.meta` GUIDs or regenerate assets unnecessarily.
- Preserve unrelated working scenes, prefabs, UI, serialized data, and user changes.
- Keep logical gameplay state separate from presentation; keep placement, transport, processing, and validation deterministic where appropriate. Do not replace explicitly designed probabilistic behavior with deterministic behavior without authorization.
- Prefer focused changes, composition, and existing patterns over broad refactors or frameworks built for future features.
- Avoid introducing new warnings when reasonably possible.

## Task Workflow

1. Check the current branch and working tree before editing.
2. Inspect only task-relevant files; reproduce reported bugs when feasible.
3. Implement the smallest coherent change that meets the stated acceptance criteria.
4. Add or update focused tests when behavior changes.
5. Run available validation and review the diff for unintended changes.

If a gameplay rule is unresolved, report the decision needed instead of inventing a major rule. Keep legacy code/assets until the relevant replacement and migration are verified unless removal is explicitly requested.

## Git Safety

- Stay on the current branch unless instructed otherwise.
- Never discard or overwrite unrelated changes or use destructive Git operations without explicit authorization.
- Do not create branches, commit, push, or merge unless explicitly requested.
- Stage only task-related files; exclude generated caches, logs, IDE files, and build outputs.

## Validation and Reporting

- Validate the smallest affected layer first, then relevant integration.
- Distinguish compilation, focused/offline checks, Unity EditMode tests, PlayMode tests, and manual gameplay verification.
- Never claim tests passed unless they ran and passed.
- Report concisely: changes, validation results, manual checks needed, and relevant limitations. Do not repeat the task or add unrelated commentary.

## Prompt and Context Efficiency

- Follow the current task prompt and relevant documents without repeating these standing rules.
- Avoid whole-repository scans when a focused investigation suffices.
- Do not broaden scope, implement optional features, or refactor unrelated systems without authorization.

## Unity Validation

- Assume the Unity Editor is already running.
- Do not launch Unity, Unity Hub, batch mode, or a separate Unity Test Runner process during normal development.
- Do not terminate Unity processes or modify/delete `Library`, `Temp`, or licensing files.
- Use code inspection and available non-Unity checks; leave Unity compilation, EditMode tests, PlayMode tests, and gameplay verification to the running Editor.
- Explicitly identify checks that require manual Unity verification.
