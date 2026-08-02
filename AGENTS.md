# Repository Architecture Contract

These rules are mandatory for every task that writes, reviews, or designs code.

## Required workflow

Before changing code, identify the affected responsibility, layer, and existing contracts. After changing code, explicitly validate the result against every applicable item below. Report static/build checks separately from Unity Test Runner, Play Mode, multiplayer, visual, and profiling validation.

## Dependencies

- Dependencies point inward: `Runtime/Network -> Application -> Domain`.
- Domain does not depend on Unity, networking, UI, scenes, or persistence.
- Application does not depend on `MonoBehaviour`, `GameObject`, RPC, or scenes.
- Direct dependencies on Domain types are allowed.
- External resources are exposed through interfaces declared by the consuming Application layer.

## Contracts

- Cross-layer interaction uses interfaces, commands, events, DTOs, or result types where a real boundary requires them.
- Add an interface only for a real boundary or multiple implementations; do not mirror every class with `IClassName`.
- A contract belongs to the consuming layer, not the implementing layer.
- DTOs contain data only, never gameplay rules.
- Domain events do not depend on `UnityEvent`.

## Layer responsibilities

- Domain owns state, entities, invariants, and gameplay rules.
- Application owns use cases and coordination of Domain objects.
- Runtime translates input, physics, UI, and Unity lifecycle events into Application calls.
- Network receives RPCs, calls Application, and replicates results; RPC methods contain no gameplay rules.
- Editor configures and validates assets, prefabs, and scenes.

## Files and responsibilities

- One file has one zone of responsibility (one file - one ZO).
- Prefer one top-level type per file.
- Split files that mix unrelated input, presentation, persistence, networking, state, or gameplay-rule concerns.
- A coordinator may call several focused collaborators, but must not absorb their implementation details.
- Do not add abstractions for hypothetical future changes.

## State and testability

- Gameplay state is not stored only in UI or a network component.
- Strings are not entity identifiers.
- MonoBehaviours do not mutate Domain outside Application use cases.
- Domain never calls UI, audio, animation, or networking.
- Domain and Application run without a Unity scene.
- Time, random, persistence, and external services are replaceable at their real boundaries.
- Cover core gameplay rules with unit tests and network scenarios separately with PlayMode tests.

## Required validation checklist

- Recheck dependency direction and asmdef references.
- Recheck that each changed file has one coherent responsibility.
- Recheck that gameplay logic is absent from RPC and MonoBehaviour glue.
- Recheck that a mechanic change does not require duplicated edits across layers.
- Recheck that every new abstraction is justified by a current boundary or implementation.
- Run the strongest relevant automated checks available and state what still requires Unity Play Mode, multiplayer, visual inspection, or profiling.
