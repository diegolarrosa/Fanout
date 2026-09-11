# How it works

The engine is about three hundred lines. This page is what those three hundred lines are doing
and why they are arranged that way.

## The three states

```csharp
public enum LogicState { Zero, One, Unknown }
```

`Unknown` is not an error value and not a fourth logic level. It is the simulator's record of
*how much it currently knows*, and it is what makes the whole propagation scheme work. A gate
whose inputs are unknown has nothing to compute, so it is skipped. When one of its inputs later
changes, it comes back.

Without a third state you would need an initialisation pass that decides what every wire starts
at, and that decision is a lie for anything with feedback in it.

## The propagation loop

```
drive an input  →  the value differs  →  queue the gates it reaches
                                              ↓
                         ┌──────── evaluate every queued gate ────────┐
                         │   output differs → queue what it reaches   │
                         └──────────────── repeat ────────────────────┘
                                              ↓
                                     nothing queued: settled
```

In code, that is all of `Circuit.Run`:

```csharp
while (current.Count > 0)
{
    next.Clear();

    foreach (Gate gate in current)
    {
        gate.TryEvaluate(next);
    }

    (current, next) = (next, current);
}
```

Two things about this are load-bearing.

**The queue is a set, not a list.** A three-input gate whose inputs all change in the same round
appears once, so it is evaluated once. Gates are compared by reference, which is the right notion
of identity for a circuit element.

**It is double buffered, not a worklist.** Everything queued in round *n* is evaluated before
anything queued in round *n+1*. That is breadth-first *scheduling*, and it is worth more than it
looks: it means every element at the same logical depth is re-examined together. When a clock line
fans out to eight flip-flops, all eight see the edge in the same round, so every master latch
closes before any slave opens and no stage can act on a neighbour's new output. A depth-first
worklist would make the shift register in this library race.

## Rounds are not depth

What is double buffered is the *queue*, not the *values*. A gate writes its output immediately, so
a gate evaluated later in the same round already sees it. Within a round the evaluation order is
unspecified — it is hash order over a set — which means:

- **Results do not depend on order.** Propagation converges to a fixed point, and for a
  combinational network that fixed point is unique. The answer is the answer.
- **`Circuit.LastRunRounds` does depend on order.** A favourable order lets a 32-bit carry chain
  settle in three rounds; an unfavourable one takes sixty. The number is a rough cost signal and
  nothing more.

If you want the round count to mean logic depth, the evaluation has to snapshot values at the
start of each round and commit them at the end — a synchronous or *delta cycle* evaluation. That
is a real and worthwhile mode, and it is not what this engine does today.

## Why it terminates

Because `SetState` is guarded:

```csharp
if (State == value)
{
    return;
}
```

A gate is only queued when a value **actually changed**. A circuit with finitely many wires can
only change finitely many times before it runs out of changes to make — unless it has a feedback
loop with an odd number of inversions, in which case it genuinely never settles and the right
answer is to say so. `Circuit.MaxRounds` bounds the loop and
`CircuitOscillationException` reports it.

Note what is *not* here: no iteration limit per gate, no convergence heuristic, no damping. The
loop is exact.

## Controlling values

The default rule is that a gate waits for all its inputs. Several gates can do better:

| Gate | Controlling value | Then the output is |
|---|---|---|
| `And` | `Zero` | `Zero` |
| `Nand` | `Zero` | `One` |
| `Or` | `One` | `One` |
| `Nor` | `One` | `Zero` |
| `Xor`, `Xnor` | none | — |
| `Not`, `BufferGate` | — (one input) | — |

Those four override `HasUnknownInputs` so they resolve as soon as they see a controlling value,
without waiting for the rest. This is ordinary constraint propagation, and it is doing real work:

- A cross-coupled NAND latch resolves the instant one side is pulled low, which is exactly how the
  physical circuit behaves. Without it, neither gate could ever resolve and the latch would be
  permanently unknown.
- An asynchronous clear reaches the output through a chain of gates that are all held at a
  controlling value, so it takes effect regardless of what the clock and data inputs are doing —
  which is what "asynchronous" means.
- A large circuit with most of its inputs undriven still settles the part that is determined,
  instead of stalling.

`AllInputsKnown` caches the outcome once every input has been seen to be known, so a gate that has
settled does not rescan its inputs on later rounds.

## The object graph

```
Gate ──owns──▶ OutputPin ──owns──▶ Net ──lists──▶ InputPin ──owns by──▶ Gate
  │                                                                       ▲
  └──owns──▶ InputPin[] ─────────────────────────────────────────────────┘
```

A `Net` is the wire: one driver, many sinks. The cycle in that diagram is the point — an
`InputPin` knows the gate it feeds, which is how a value change turns into a scheduling decision
at the exact moment it is known to be a change.

`Port` is the odd one. It derives from `InputPin` but owns an `OutputPin` of its own and
re-drives whatever comes in, which is what lets a `Module` present a boundary without exposing
the gates behind it. An input port and an output port are the same type; the difference is only
which list of the module it was added to.

## Modules are not simulated

`Module` has no behaviour at all — no `Evaluate`, nothing in the queue. It is a naming and
grouping device: ports on the outside, a graph of gates on the inside, and a `ModuleLayout` that
maps signal names to port indices. At run time a `RippleCarryAdder` is indistinguishable from the
loose gates that make it up.

That is why the library has no "primitive block" tier. `JKFlipFlop` is eight NANDs, an AND and
two inverters, and it behaves like one because it *is* one.

## What is deliberately absent

- **Time.** No delays, no event calendar, no timing model. `Run` gives you the settled result, so
  the transient states a real circuit glitches through are invisible. A ripple counter's
  intermediate values are real and this simulator will never show them to you.
- **Multiple drivers.** One driver per input. No bus contention, no high-impedance state, no
  resolution function.
- **Strength.** No pull-ups, no open collector.

Each of those is a deliberate omission rather than an unfinished feature: adding any of them
changes the propagation rule from "settle" to "schedule", which is a different simulator.
