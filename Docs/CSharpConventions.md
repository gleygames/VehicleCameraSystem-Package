# C# conventions — repository copy

Copied from the user's csharp-code-conventions skill on 2026-09-21 for fresh-task handoffs. The user-approved step workflow supplies scoped authorization after the mandatory Hard checkpoint where applicable; the copied working-style rule does not add repeated per-file approval. Read the live skill when available and follow later explicit user decisions.

# C# Code Conventions

These are standing preferences. They apply to every C# file, in every project, unless the user says otherwise for a given task.

## 1. Never add comments

No inline comments, no summary comments, no XML doc comments. Only add a comment when the user explicitly asks for one.

Common rationalizations that are **not** exemptions:
- "It's XML documentation on a public API / interface member, not a comment." It is a comment.
- "It's a `// TEMPORARY` marker on throwaway diagnostic code." Still a comment.
- "The surrounding code is densely documented, so matching it is house style." Still no.
- "This records a hard-won measured finding worth preserving." Measured findings go in memory files or the conversation, never in source.

The pull toward commenting is strongest right after a difficult diagnosis. That is exactly when to write the finding down somewhere other than the code.

When moving or renaming a member that already carries a **user-written** comment, preserve the existing text verbatim or ask — never rewrite it into something longer.

## 2. Always use braces

Every `if`, `else`, `for`, `foreach`, `while`, and `using` body gets `{}`, even a single-line body. No braceless control flow anywhere.

## 3. No ternary expressions

Never use `condition ? a : b`. Write an explicit `if`/`else` block, even for a trivial one-liner assignment.

```csharp
// wrong
float rate = isFast ? highRate : lowRate;

// right
float rate;
if (isFast)
{
    rate = highRate;
}
else
{
    rate = lowRate;
}
```

## 4. No LINQ, no lambdas

No `System.Linq`, no `Where`/`Select`/`Any`/`FirstOrDefault`, no lambda expressions for filtering or transforming collections. Write an explicit `for`/`foreach` loop with an `if` block.

## 5. Properties over public fields

Expose data as properties, never as public fields. For values set once at construction, use a getter-only auto-property:

```csharp
public float MaxTorque { get; }
```

Not `public readonly float MaxTorque` and not a bare `public float MaxTorque`. Use `{ get; private set; }` only when the value genuinely mutates after construction.

There is no performance cost — trivial auto-property getters are inlined. Note that `{ get; private set; }` on a struct blocks `readonly struct` and can trigger defensive copies through `in` parameters, so prefer `{ get; }` unless mutation is actually needed.

## 6. No static classes or static methods

Don't propose a static class or static method as a design solution — not even for "pure function" extraction that would conventionally be static, such as pulling decision logic out of a stateful class.

Instead, extract an **instance-based helper class**: constructed with its config and dependencies, called through an instance. This applies to new design going forward; a pre-existing static class already used across a codebase is not automatically a rewrite target.

## 7. Per-tick method naming: `Update<Domain><Phase>`

Any method that is part of a cross-object tick-dispatch chain — one class calling another to say "do your per-tick work" — is named `Update<Domain><Phase>`.

Examples: `UpdateTruckPhysics`, `UpdateDriverVisuals`, `UpdateShiftPhysics`, `UpdateWheelSuspensionPhysics`.

The name alone must tell the reader both *what* it updates and *when* it runs, with zero callstack tracing.

Rules:
- Both parts, always, even when the class only ever runs in one phase and the domain seems unambiguous.
- Two identically-named methods across a delegating pair (e.g. `Wheel.UpdateWheelVisuals` calling `WheelVisualUpdater.UpdateWheelVisuals`) is expected and correct — the inner class does exactly what the outer one delegates.
- Private helpers called only once, from within their own class's already-conforming method, are excluded. The reader is already inside the caller, so there is no ambiguity to resolve.
- Engine-reserved entry points (in Unity: `MonoBehaviour.FixedUpdate` / `Update`) keep their required names. They just forward into the conventionally-named method.
- Per-tick methods take an explicit `deltaTime` parameter rather than reading a global, for determinism.

Rejected alternatives, so don't re-propose them: naming for *what* only (`UpdateDriver` — can't tell which tick it runs on); a two-tier rule where only classes straddling both phases get the phase suffix (requires a judgment call per class, and still fails for methods with a plausible dual meaning); reusing the engine's reserved names on plain C# classes.

When applying this to an existing file, look for tick-driven methods that aren't literally named `Update*` yet — a method named `Process` or `Step` may well be one — before concluding the file needs no rename.

## 8. Member order within a file

Every class file follows this order:

1. **Fields, then properties** — see the lane rules below.
2. **Constructor** (or `Initialize` if the type has no constructor).
3. **`Update<Class>Physics`, then `Update<Class>Visuals`** (whichever exist).
4. **Remaining public methods.**
5. **Private methods, in first-use order** — a helper goes immediately after the first already-placed method (public or private, scanning in final file order) that calls it. A helper called from several places still gets exactly one unambiguous slot this way.
6. **Cleanup/teardown last** — `OnDestroy`, `Dispose`, any teardown — always last, public or private, overriding rules 4 and 5.

Step-down ordering (each public method followed by the private helpers it calls) is explicitly rejected: a helper used by more than one public method has no single natural home under that scheme. Grouping by accessibility plus first-use order gives every member exactly one bucket.

### Field and property lanes

Six lanes, each separated by exactly one blank line. Skip the blank line for an empty lane — never stack blank lines.

1. Const fields
2. Readonly fields
3. Private non-readonly fields
4. Private properties
5. Events
6. Public properties

No blank lines *within* lane 3, even between a custom-object sub-group and a primitive sub-group.

### Tier order within a lane

Within lanes 2, 3, 4, and 6 **independently**, order members:

0. **Collections first** — `T[]`, `List<T>`, `IReadOnlyList<T>`, `IList<T>`, etc. — ahead of everything else, even concrete custom types. Multiple collections in one lane sort among themselves by their *element* type using the same tier order below.
1. **Concrete custom types** — classes, structs, enums — keeping existing relative order.
2. **Interfaces** — keeping existing relative order. Always after concrete custom types, always before primitives.
3. **Built-in value types**, ordered `string → float → int → bool`, keeping existing relative order within the same type.

`Telemetry`, if the class has one, always goes first in lane 6, ahead of every other public property regardless of tier.

### Traps when applying this

- **Check readonly-ness per field, don't eyeball it.** A non-primitive field is not necessarily readonly. A non-readonly enum field belongs in lane 3, not lane 2.
- **Enums and custom structs count as custom objects**, not primitives.
- **Never two blank lines in a row.** Collapse any stray double blank lines found while reordering, even ones unrelated to the change you were making.
- **Files that already "look sorted" usually aren't.** Run the lane and tier rules against them properly.
- Consts stay untouched in lane 1.

## 9. Working style with these rules

- Present a plan and get explicit approval before modifying any file.
- Prefer the cleanest design over the smallest diff. If a redesign is better than a patch, propose the redesign.
- Apply structural conventions to one file at a time, on request — not as an unprompted sweep across a codebase.
- Don't bundle behaviour changes into a structural or bug-fix change.
