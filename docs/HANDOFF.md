# Handoff — state of the port

This document is the first thing to read when picking the project up again. It says what is
closed, what is open, and what has and has not been verified.

## Status

| Item | State |
|---|---|
| Port to .NET 8, SDK-style projects | ✅ **Done** |
| Full translation to English (types, members, comments, signal names) | ✅ **Done** — rename table in `CHANGELOG.md` |
| Source normalised to UTF-8 | ✅ **Done** — the original mixed Windows-1252 and UTF-16LE |
| `GateQueue` owns its storage (the old null-`Hashtable` defect) | ✅ **Fixed** |
| Quadratic `Array.Resize` on every added input | ✅ **Fixed** — inputs live in a list |
| Every gate's output created in the base constructor, never null | ✅ **Fixed** |
| Ripple counter chains the correct output for its edge and direction | ✅ **Fixed** — behaviour change against the original |
| NAND latch pins named for what they actually do (`SN`/`RN`) | ✅ **Fixed** |
| Oscillation guard instead of an infinite loop | ✅ **Added** — `CircuitOscillationException` |
| `CustomModule` takes its layout instead of a global static registry | ✅ **Fixed** |
| Unknown signal name throws a message that lists the valid names | ✅ **Added** |
| WinForms harnesses removed, console sample in their place | ✅ **Done** |
| xUnit test suite (the original had none) | ✅ **Written** — see the caveat below |
| XML documentation on every public member | ✅ **Done** |
| English docs (`README.md`, `docs/`) | ✅ **Done** |
| Spanish docs (`es/`) | ✅ **Done** |
| MIT `LICENSE`, `.gitignore`, `.editorconfig`, CI workflow | ✅ **Done** |
| **`dotnet build` and `dotnet test` actually run** | ✅ **Verified.** Builds clean on .NET 8.0.25; **74 of 74 tests pass**; the sample runs correctly |
| Round counts presented as a depth metric | ✅ **Corrected.** They are order-dependent and are not logic depth; docs, sample and test fixed |
| Synchronous (delta-cycle) evaluation mode, so rounds *would* mean depth | ⬜ **Open idea**, ~40 lines, deliberately not built yet |
| Package name `Fanout` confirmed free on nuget.org | ✅ **Free and kept.** `packageid:fanout` returns nothing; the 17 search hits are messaging packages that merely mention fan-out |
| GitHub repository created and pushed | ⬜ **Open** |
| A worked "build your own CPU" tutorial | ⬜ **Idea**, not started |
| Netlist import/export | ⬜ **Idea**, not started |

## How this was verified

The port was written without a .NET SDK available, so it was reviewed statically and then checked
on a real machine. The result:

```bash
dotnet restore
dotnet build -c Release      # clean, warnings as errors, no XML-doc gaps
dotnet test -c Release       # 74 passed, 0 failed, 0 skipped
dotnet run --project samples/Fanout.Demo
```

Exactly one compile error came out of the whole port: `CS0419`, an ambiguous `cref` pointing at
`Circuit.AddInput`, which has two overloads. Documentation comments that name an overloaded member
need the signature — `<see cref="Circuit.AddInput(Port)"/>`. If a new overload is added later,
check the doc comments that mention it.

The sample surfaced one substantive defect that the tests had not: round counts were being
presented as a measure of logic depth, and they are not. See
[architecture.md](architecture.md#rounds-are-not-depth); the claim has been corrected in the code,
the docs, the sample and the test suite.

### Where to look first if something breaks later

In rough order of likelihood:

1. **Overload resolution on `ConnectTo`.** There are several overloads across `Gate`, `Port` and
   `Module`, distinguished by whether the target is a gate, a module, an index or a name. A call
   that binds to the wrong one will usually still compile and then misbehave, so a failing
   *component* test points here before it points at the logic.
2. **Port order versus layout.** The order of `AddInput` and `AddOutput` calls in a module's
   constructor defines the port indices, and `ModuleLayout` has to agree. A mismatch is silent at
   compile time. The exhaustive adder test and the multiplexer test are the ones that catch it.
3. **Doc comments and `TreatWarningsAsErrors`.** The library builds with warnings as errors and
   `GenerateDocumentationFile`, so a public member missing an XML comment is a build failure. It
   is a one-line fix each time.

The test and sample projects deliberately set `TreatWarningsAsErrors` to `false`, so an analyzer
update in xUnit cannot break the build. The library itself keeps it on.

## Before publishing

1. Check that `Fanout` is free on nuget.org — the ID is global and first come, first served.
   If it is taken, it appears in `Directory.Build.props`, `src/Fanout/Fanout.csproj`, the
   namespaces and the docs.
2. Confirm the copyright line in `LICENSE` and `Directory.Build.props`.
3. Push to GitHub as a private repository, let CI go green, then make it public.
4. Publish 0.1.0 to NuGet.

## Design decisions worth not relitigating

- **This library stays an object graph.** `docs/scaling.md` sets out the cost per gate and why a
  packed representation is a different project rather than a refactor of this one.
- **No timing model.** `Run` returns the settled state. Adding delays turns "settle" into
  "schedule", which is a different simulator with a different API.
- **One driver per input.** No bus contention, no high-impedance state.
- **The blocks stay readable.** A constructor full of `ConnectTo` calls that reads like a
  schematic is the feature, not an implementation detail to be optimised away.
