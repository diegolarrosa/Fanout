# Changelog

All notable changes to this project are documented here. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and the project uses
[semantic versioning](https://semver.org/spec/v2.0.0.html).

## [0.1.0] — unreleased

First public release. The code dates from 2005; this is the port that makes it publishable.

### Changed — porting

- Targets **.NET 8** instead of .NET 2.0; all projects are SDK-style.
- Every identifier and comment is in **English**. The old `Sld` prefix is gone; see the rename
  table below.
- Source files are **UTF-8** throughout. The original mixed Windows-1252 and UTF-16LE, which is
  why some comments had lost their accented characters.
- Nullable reference types and implicit usings are enabled, and the library builds with warnings
  as errors.

### Fixed

- **`SldLista` had no constructor.** Its internal `Hashtable` was left null and the class only
  worked because `SldBloquePrincipal` happened to assign the property from outside. Any other use
  threw a `NullReferenceException`. The replacement, `GateQueue`, owns its storage.
- **Adding an input to a gate was quadratic.** `AgregaEntrada` called `Array.Resize` per input, so
  building a wide gate copied the array on every call. Inputs now live in a list.
- **Every gate created its output in a private `Inicializa` method**, so a gate that forgot to call
  it had a null output. `Gate` now creates the output in its own constructor and it is never null.
- **The ripple counter chained the wrong output**, which made it count incorrectly for its own
  default settings. Which output feeds the next stage depends on the clock edge, so
  `RippleCounter` now derives it from `ClockEdge` and `CountDirection` and the caller never
  has to know the rule. This is a **behaviour change** against the original.
- **The NAND set-reset latch had its inputs named backwards.** As wired, pulling the pin the old
  code called `R` low *sets* the latch, because a NAND latch is active low. The pins are now
  `SN` and `RN` and the behaviour is documented.
- **A circuit with no stable state used to hang.** `Circuit.Run` now gives up after `MaxRounds`
  propagation rounds and throws `CircuitOscillationException`.
- **`SldBloquePersonalizado` looked its layout up in a global static `Hashtable` keyed by
  string**, which meant an unregistered module threw a `NullReferenceException` on first use —
  as the original sample programs did. `CustomModule` takes its layout as a constructor argument.
- Asking for an unknown signal name now throws a `KeyNotFoundException` that lists the names that
  do exist, instead of a `NullReferenceException`.

### Removed

- The two WinForms test harnesses. They were 6,340 of the original 11,118 lines, almost all of it
  designer-generated, and they tied half the repository to Windows. A cross-platform console
  sample replaces them.
- `SldPropiedades`, which was never read by anything.

### Added

- **A test suite.** The original had none. xUnit, covering every gate's truth table, an exhaustive
  4-bit adder and adder/subtractor, multiplexer and demultiplexer selection, latch and flip-flop
  behaviour including asynchronous preset and clear, register and counter sequences, and the
  oscillation guard.
- `samples/Fanout.Demo`, a console program that prints an adder, a counter, a shift register and
  a carry-depth table.
- XML documentation on every public member, and a NuGet package.
- `PinPolarity` is now settable after construction through `Gate.SetInputPolarity`.

### Rename table

| Original | Now |
|---|---|
| `SldEstado` (`Cero`/`Uno`/`Indeterminado`) | `LogicState` (`Zero`/`One`/`Unknown`) |
| `SldFlanco` (`Bajada`/`Subida`) | `ClockEdge` (`Falling`/`Rising`) |
| `SldControlEntrada` (`Normal`/`Negado`) | `PinPolarity` (`Normal`/`Inverted`) |
| `SldEntrada` | `InputPin` |
| `SldSalida` | `OutputPin` |
| `SldLinea` | `Net` |
| `SldEntradaSalida` | `Port` |
| `SldLista` | `GateQueue` |
| `SldPuerta` | `Gate` |
| `SldBloque` | `Module` |
| `SldBloquePrincipal` | `Circuit` |
| `SldBloquePersonalizado` | `CustomModule` |
| `SldDescripcionDeBloque` | `ModuleLayout` |
| `SldDescripcionES` | `PortGroup` |
| `Yes` | `BufferGate` |
| `SumadorBasico` | `FullAdder` |
| `Sumador` | `RippleCarryAdder` |
| `Restador` | `AddSubtractor` |
| `Multiplexor` / `Demultiplexor` | `Multiplexer` / `Demultiplexer` |
| `LatchNandRS` / `LatchNorRS` | `NandSRLatch` / `NorSRLatch` |
| `LatchD` / `LatchD_Puerta` | `DLatch` / `DLatchPrimitive` |
| `FlipFlopD` / `FlipFlopDsinPyR` | `DFlipFlop` / `SimpleDFlipFlop` |
| `FlipFlopT` / `FlipFlopTsinPyR` | `TFlipFlop` / `SimpleTFlipFlop` |
| `FlipFlopJK` / `FlipFlopJKsinPyR` | `JKFlipFlop` / `SimpleJKFlipFlop` |
| `RegistroSerie` | `ShiftRegister` |
| `RegistroParalelo` | `ParallelRegister` |
| `RegistroSerieParalelo` | `ShiftLoadRegister` |
| `Contador` | `RippleCounter` |
| `ProcesaBloque` | `Circuit.Run` |
| `EjecutaPuerta` / `ProcesaPuerta` | `TryEvaluate` / `Evaluate` |
| `ContieneEntradasIndeterminadas` | `HasUnknownInputs` |
| `CambiaEstado` | `SetState` |
| `Conecta` | `ConnectTo` |
| `DeterminaSalida` | `ForceOutput` |
| `AgregaEntrada` / `AgregaSalida` | `AddInput` / `AddOutput` |
| `E_ACARREO` / `S_ACARREO` | `CIN` / `COUT` |
| `P` / `R` on flip-flops | `PRE` / `CLR` |
