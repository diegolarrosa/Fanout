# Fanout

A gate-level digital logic simulator for .NET. Eight primitive gates, three-valued logic, and an
event-driven propagation loop — and everything else in the library, up to shift registers and
ripple counters, is built from nothing but those gates wired together.

[![CI](https://github.com/diegolarrosa/Fanout/actions/workflows/ci.yml/badge.svg)](https://github.com/diegolarrosa/Fanout/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/Fanout.svg)](https://www.nuget.org/packages/Fanout)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](https://github.com/diegolarrosa/Fanout/blob/main/LICENSE)

*Documentación en castellano: [`es/README.md`](https://github.com/diegolarrosa/Fanout/blob/main/es/README.md).*

## What it is

Fanout simulates a circuit the way the circuit actually behaves: a value changes on a wire, the
gates that wire reaches are re-evaluated, and whatever changes as a result propagates onward,
until nothing is changing any more. There is no clock in the simulator, no time step, and no
event calendar. There is a queue of gates that have something new to look at.

What makes that work is the third logic state. Every wire starts **unknown**, not zero, and a
gate is evaluated only once it has enough information to produce an answer. "Enough" is less
than "all", which is the interesting part:

```
An AND gate with one input already low has a known output. It does not wait for the others.
```

That single rule — evaluate on a controlling value — is why a circuit can settle with half of it
still undriven, why a cross-coupled latch resolves the moment one side is forced, and why the
simulator can tell you that a latch has *no* state yet instead of quietly inventing a zero.

## Install

```bash
dotnet add package Fanout
```

## A first circuit

```csharp
using Fanout;
using Fanout.Combinational;

var circuit = new Circuit();
var adder = new RippleCarryAdder(width: 4);

// Top-level ports, wired to the adder's named inputs.
var a = new Port[4];
var b = new Port[4];

for (int i = 0; i < 4; i++)
{
    a[i] = circuit.AddInput();
    a[i].ConnectTo(adder, "A", i);

    b[i] = circuit.AddInput();
    b[i].ConnectTo(adder, "B", i);
}

var carryIn = circuit.AddInput();
carryIn.ConnectTo(adder, "CIN");

// Drive 9 + 6 + 0 and settle.
SetBus(a, 9);
SetBus(b, 6);
carryIn.SetState(LogicState.Zero);

int rounds = circuit.Run();

// SUM = 1111, COUT = 0, settled in `rounds` propagation rounds.
Console.WriteLine(adder.Output("SUM", 3).State);   // One
Console.WriteLine(adder.Output("COUT").State);     // Zero

static void SetBus(Port[] bus, int value)
{
    for (int i = 0; i < bus.Length; i++)
    {
        bus[i].SetState(((value >> i) & 1) == 0 ? LogicState.Zero : LogicState.One);
    }
}
```

`dotnet run --project samples/Fanout.Demo` runs a longer version of this: an adder, a counter,
a shift register, and a table of how propagation depth grows with a carry chain.

## What is in the box

| Namespace | Contents |
|---|---|
| `Fanout` | `LogicState`, `Gate`, `InputPin`, `OutputPin`, `Net`, `Port`, `Module`, `Circuit`, `ModuleLayout` |
| `Fanout.Gates` | `And`, `Or`, `Not`, `Nand`, `Nor`, `Xor`, `Xnor`, `BufferGate` |
| `Fanout.Combinational` | `FullAdder`, `RippleCarryAdder`, `AddSubtractor`, `Multiplexer`, `Demultiplexer` |
| `Fanout.Sequential` | `NandSRLatch`, `NorSRLatch`, `DLatch`, `DLatchPrimitive`, `DFlipFlop`, `SimpleDFlipFlop`, `TFlipFlop`, `SimpleTFlipFlop`, `JKFlipFlop`, `SimpleJKFlipFlop`, `ShiftRegister`, `ParallelRegister`, `ShiftLoadRegister`, `RippleCounter` |

Every block above the gate level is assembled from gates and from other blocks — the JK
flip-flop is eight NANDs, an AND and two inverters, and the ripple counter is a chain of those.
Nothing is shortcut with behavioural code, with one deliberate exception (`DLatchPrimitive`)
kept as the worked example of extending `Gate` directly.

## Documentation

- [Getting started](https://github.com/diegolarrosa/Fanout/blob/main/docs/getting-started.md) — ports, driving inputs, reading results
- [How it works](https://github.com/diegolarrosa/Fanout/blob/main/docs/architecture.md) — the propagation loop, controlling values, why it terminates
- [Component reference](https://github.com/diegolarrosa/Fanout/blob/main/docs/components.md) — every block, its signals, and its quirks
- [Scale and memory](https://github.com/diegolarrosa/Fanout/blob/main/docs/scaling.md) — what this design costs per gate, and where the ceiling is

## What it is good for, and what it is not

It is good for understanding and for building circuits you can reason about: a teaching aid, a
reference implementation, a way to check that a design does what you drew. The blocks are small
enough to read end to end, and the propagation rule is one paragraph long.

It is not a production EDA tool. It has no timing model, so a real circuit's hazards and glitches
are invisible here — `Run` reports the settled result and nothing else. It has no multi-driver
resolution, no high-impedance state, and no netlist import. And it holds every gate as a graph of
.NET objects, which puts a practical ceiling on circuit size well below what a packed
representation would reach. [docs/scaling.md](https://github.com/diegolarrosa/Fanout/blob/main/docs/scaling.md) works that number out honestly
rather than leaving it implied.

## Provenance

The original was written in 2005 against .NET 2.0 and Visual Studio 2005, and sat unpublished for
twenty years. This release ports it to .NET 8, translates it to English, replaces the non-generic
collections, fixes the defects listed in [CHANGELOG.md](https://github.com/diegolarrosa/Fanout/blob/main/CHANGELOG.md), and adds the test suite it
never had. The propagation design — three states, controlling-value evaluation, the double-buffered
work queue — is unchanged, because it was right the first time.

## Licence

MIT. See [LICENSE](https://github.com/diegolarrosa/Fanout/blob/main/LICENSE).
