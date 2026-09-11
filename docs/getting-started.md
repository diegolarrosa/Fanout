# Getting started

Three things happen in every Fanout program: you build a circuit, you drive its inputs, and you
tell it to settle. This page covers all three.

## Building

A `Circuit` is the top of the tree. It owns the work queue, so a port only schedules work if the
circuit knows about it.

```csharp
using Fanout;
using Fanout.Gates;

var circuit = new Circuit();
var gate = new And();

Port a = circuit.AddInput();
Port b = circuit.AddInput();

a.ConnectTo(gate, 0);
b.ConnectTo(gate, 1);
```

`ConnectTo` always reads driver-first: *this output goes to that input*. A driver can connect to
as many inputs as you like — that is what a `Net` is — but an input can only be driven by one
thing. Wiring two drivers to the same input is not an error and not a short circuit; the second
one simply overwrites the first as it propagates, which is almost never what you want.

## Driving and settling

```csharp
a.SetState(LogicState.One);
b.SetState(LogicState.One);

int rounds = circuit.Run();

Console.WriteLine(gate.Output.State);   // One
```

`SetState` on a top-level port queues whatever the port reaches. `Run` then works through the
queue until nothing changes and returns how many rounds it took. Calling `Run` again with nothing
pending returns `0` and does no work, so it is cheap to call defensively.

Driving a port with the value it already holds is not a change and schedules nothing. This is the
rule the whole engine rests on, and it is worth internalising early: **the simulator responds to
differences, not to writes.**

## Modules and signal names

Anything more than a couple of gates should be a `Module`. Modules have named ports, so a circuit
reads like a schematic rather than like array indexing:

```csharp
using Fanout.Combinational;

var adder = new RippleCarryAdder(width: 8);

Port carryIn = circuit.AddInput();
carryIn.ConnectTo(adder, "CIN");

Port bit3 = circuit.AddInput();
bit3.ConnectTo(adder, "A", 3);      // bit 3 of the A bus
```

Reading back works the same way:

```csharp
LogicState top = adder.Output("SUM", 7).State;
LogicState carry = adder.Output("COUT").State;
```

Bit 0 is always the least significant. Asking for a name a module does not have throws a
`KeyNotFoundException` that lists the names it does have.

## Unknown is a real answer

A freshly built circuit is entirely `LogicState.Unknown`, and parts of it may stay that way:

```csharp
var latch = new NorSRLatch();
// ... wire R and S, drive both low, run ...

latch.Output("Q").State;   // Unknown — and that is correct
```

A cross-coupled latch with both inputs inactive genuinely has no defined state until something
forces one. Fanout says so rather than picking a value for you. In practice this means **pulse
the clear line before you clock anything sequential**:

```csharp
clear.SetState(LogicState.Zero);   // active low
circuit.Run();
clear.SetState(LogicState.One);
circuit.Run();
```

If a sequential circuit reads `Unknown` where you expected a zero, a missing clear pulse is the
first thing to check.

## Clocking

There is no clock generator, because there is no time. A clock edge is just a value change
followed by a settle:

```csharp
clock.SetState(LogicState.Zero);
circuit.Run();
clock.SetState(LogicState.One);    // the rising edge
circuit.Run();
```

Settle after each transition. Driving low and high without a `Run` in between collapses them into
one event and the edge never happens.

## Writing your own gate

Derive from `Gate`, and implement `Evaluate`:

```csharp
public sealed class Majority : Gate
{
    public Majority() : base(3) { }

    protected override void Evaluate(GateQueue queue)
    {
        int high = 0;

        for (int i = 0; i < InputCount; i++)
        {
            if (GetInputState(i) == LogicState.One)
            {
                high++;
            }
        }

        Output.SetState(high >= 2 ? LogicState.One : LogicState.Zero, queue);
    }
}
```

`Evaluate` is called only when the gate has enough information, so it never has to handle
`Unknown`. If your gate has a controlling value — a majority gate does, once two inputs agree —
override `HasUnknownInputs` as well and it will resolve earlier. See
[How it works](architecture.md) for what that buys.

## Writing your own module

Declare a layout, build the internals in the constructor, and forward the two index lookups:

```csharp
public sealed class HalfAdder : Module
{
    public static readonly ModuleLayout Layout = new ModuleLayout()
        .AddInputGroup("A", 0)
        .AddInputGroup("B", 1)
        .AddOutputGroup("SUM", 0)
        .AddOutputGroup("COUT", 1);

    public HalfAdder()
    {
        Port a = new(), b = new(), sum = new(), carry = new();
        Xor xor = new();
        And and = new();

        AddInput(a);
        AddInput(b);
        AddOutput(sum);
        AddOutput(carry);

        a.ConnectTo(xor, 0);
        a.ConnectTo(and, 0);
        b.ConnectTo(xor, 1);
        b.ConnectTo(and, 1);
        xor.ConnectTo(sum);
        and.ConnectTo(carry);
    }

    public override int InputIndex(string name) => Layout.InputIndex(name);

    public override int OutputIndex(string name) => Layout.OutputIndex(name);
}
```

The order you call `AddInput` and `AddOutput` in **is** the port order, and the layout has to
agree with it. That is the one place where a mistake is silent, so it is worth a test.

For circuits generated from data rather than written by hand, use `CustomModule`, which takes its
layout as a constructor argument instead of needing a type.
