# Component reference

Every block here is assembled from the eight primitive gates and from other blocks. Bit 0 of any
bus is the least significant.

## Gates — `Fanout.Gates`

| Type | Inputs | Behaviour |
|---|---|---|
| `And` | 2 by default, any number | High when every input is high |
| `Nand` | 2 by default, any number | Low when every input is high |
| `Or` | 2 by default, any number | High when any input is high |
| `Nor` | 2 by default, any number | Low when any input is high |
| `Xor` | 2 by default, any number | High on odd parity |
| `Xnor` | 2 by default, any number | High on even parity |
| `Not` | 1 | Inverts |
| `BufferGate` | 1 | Passes through |

Any gate can take inverted inputs without a separate inverter:

```csharp
var gate = new And(PinPolarity.Inverted, PinPolarity.Normal);   // (¬a) ∧ b
```

## Combinational — `Fanout.Combinational`

### `FullAdder`

| | Signals |
|---|---|
| Inputs | `A`, `B`, `CIN` |
| Outputs | `SUM`, `COUT` |

Two XORs, two ANDs, one OR.

### `RippleCarryAdder(int width = 2)`

| | Signals |
|---|---|
| Inputs | `A` (width), `B` (width), `CIN` |
| Outputs | `SUM` (width), `COUT` |

A chain of `FullAdder` cells: the top bit cannot settle until the carry has walked the whole
chain. Note that `Circuit.LastRunRounds` does **not** measure that depth — see
[How it works](architecture.md#rounds-are-not-depth).

### `AddSubtractor(int width = 2)`

| | Signals |
|---|---|
| Inputs | `A` (width), `B` (width), `SUB` |
| Outputs | `SUM` (width), `COUT` |

`SUB` low adds; `SUB` high subtracts. One XOR per bit inverts `B` and the same signal feeds the
carry-in, so the adder sees the two's complement of `B`. On a subtraction `COUT` is the
*no-borrow* flag: high when `A ≥ B`.

### `Multiplexer(int inputCount = 2)`

| | Signals |
|---|---|
| Inputs | `SEL` (ceil(log₂ n)), `IN` (n) |
| Outputs | `OUT` |

One AND per data input decoding the select lines, into a wide OR. `inputCount` must be at least
two; it need not be a power of two, though the unreachable inputs of a non-power-of-two
multiplexer are simply never selected.

### `Demultiplexer(int outputCount = 2)`

| | Signals |
|---|---|
| Inputs | `SEL` (ceil(log₂ n)), `EN` |
| Outputs | `OUT` (n) |

`EN` is routed to the addressed output; every other output is held low. Tie `EN` high and it is an
address decoder.

## Sequential — `Fanout.Sequential`

> **Read this first.** Everything in this section is built on a cross-coupled pair, which has no
> defined state until something forces one. A block with a `CLR` line needs one low pulse before
> it will do anything; a block without one seeds itself in the constructor. If a sequential output
> reads `Unknown`, a missing clear pulse is almost always why.

### `NorSRLatch`

| | Signals |
|---|---|
| Inputs | `R`, `S` (both active high) |
| Outputs | `Q`, `QN` |

`S` high sets, `R` high clears, both low holds. Both high is the forbidden state and drives both
outputs low.

### `NandSRLatch`

| | Signals |
|---|---|
| Inputs | `SN`, `RN` (both active **low**) |
| Outputs | `Q`, `QN` |

`SN` low sets, `RN` low clears, both high holds. Both low is the forbidden state and drives both
outputs high. The original code named these pins `R` and `S`, which was backwards — a NAND latch
is active low, so the pin it called `R` is the one that sets.

### `DLatch`

| | Signals |
|---|---|
| Inputs | `D`, `CLK` |
| Outputs | `Q`, `QN` |

Four NANDs. Level sensitive: transparent while `CLK` is high, holding while it is low.

### `DLatchPrimitive`

A `Gate`, not a `Module`. Inputs by index: `0` = D, `1` = enable. Outputs: `Output` (Q) and
`OutputQN`. It is the worked example of writing behaviour in code instead of wiring it out of
gates — compare it with `DLatch`, which is the same element the long way round.

### `DFlipFlop(ClockEdge edge = Falling, bool settlePresetAndClear = true)`

| | Signals |
|---|---|
| Inputs | `D`, `CLK`, `PRE`, `CLR` (both active low) |
| Outputs | `Q`, `QN` |

Master-slave, edge triggered. `PRE` and `CLR` act regardless of the clock. With
`settlePresetAndClear` left at its default they start inactive, so a circuit that never wires them
still settles — but the flip-flop still needs one `CLR` pulse to leave the unknown state.

### `SimpleDFlipFlop(ClockEdge edge = Falling)`

`D`, `CLK` → `Q`, `QN`. The same thing without preset and clear, so nothing can force an initial
state; `Q` stays unknown until the first edge resolves the pair.

### `TFlipFlop(ClockEdge edge = Falling, bool settlePresetAndClear = true)`

| | Signals |
|---|---|
| Inputs | `T`, `PRE`, `CLR` |
| Outputs | `Q`, `QN` |

`T` is both the toggle input and the clock: each selected edge on `T` flips the output. That is
what makes a chain of these a counter.

### `SimpleTFlipFlop(ClockEdge edge = Falling)`

`T` → `Q`, `QN`. With no clear to force a state, the constructor seeds the output pair to `Q = 0`
directly. That is a construction-time shortcut with no physical counterpart — a real flip-flop
powers up in whichever state it lands in.

### `JKFlipFlop(ClockEdge edge = Falling, bool settlePresetAndClear = true)`

| | Signals |
|---|---|
| Inputs | `J`, `K`, `CLK`, `PRE`, `CLR` |
| Outputs | `Q`, `QN` |

On the selected edge: `00` holds, `10` sets, `01` clears, `11` toggles.

### `SimpleJKFlipFlop(ClockEdge edge = Falling)`

`J`, `K`, `CLK` → `Q`, `QN`. Seeds itself to `Q = 0` as `SimpleTFlipFlop` does.

### `ShiftRegister(int width = 2, ClockEdge edge = Rising)`

| | Signals |
|---|---|
| Inputs | `D` (serial), `CLK`, `PRE`, `CLR` |
| Outputs | `Q` (width) |

Serial in, parallel out. `Q[0]` is the stage nearest the input.

### `ParallelRegister(int width = 2, ClockEdge edge = Rising)`

| | Signals |
|---|---|
| Inputs | `D` (width), `CLK`, `PRE`, `CLR` |
| Outputs | `Q` (width) |

One flip-flop per bit, all clocked together.

### `ShiftLoadRegister(int width = 2, ClockEdge edge = Rising)`

| | Signals |
|---|---|
| Inputs | `D` (width), `SIN`, `LOAD`, `CLK`, `PRE`, `CLR` |
| Outputs | `Q` (width) |

`LOAD` high takes the parallel inputs on the next edge; `LOAD` low shifts, bringing `SIN` in at
bit 0.

### `RippleCounter(int width = 2, ClockEdge edge = Rising, CountDirection direction = Up)`

| | Signals |
|---|---|
| Inputs | `CLK`, `PRE`, `CLR` |
| Outputs | `Q` (width) |

A chain of `TFlipFlop`s, each clocking the next. Which output feeds the next stage is not a
preference: a stage must hand its neighbour a triggering transition exactly when it returns to
zero, so counting up with a rising edge means chaining `QN`, and counting up with a falling edge
means chaining `Q`. `direction` and `edge` decide it together and the caller never has to know.

Because it is asynchronous, the stages do not change together in a real circuit — the top bit
settles only after the carry has walked the chain. `Circuit.Run` always reports the settled value,
so those transients are not visible here.
