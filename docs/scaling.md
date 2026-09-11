# Scale and memory

This library holds a circuit as a graph of .NET objects. That is the right call for the circuits
it is meant for, and it is also the thing that decides how large a circuit can get. This page
works the number out rather than leaving it implied.

## What one gate costs

A two-input `And`, on 64-bit .NET, is not one object. It is nine:

| Object | Approximate bytes |
|---|---|
| The `And` itself (header, three references, a flag) | 48 |
| `List<InputPin>` and its backing array | 72 |
| `List<bool>` and its backing array | 56 |
| Two `InputPin` instances | 64 |
| `OutputPin` | 32 |
| `Net` | 24 |
| `List<InputPin>` inside the net, and its array | 88 |
| **Total** | **≈ 380 bytes across 9–11 objects** |

Those are estimates from the field layout, not measurements — allocator rounding and list growth
move them. The order of magnitude is what matters: **a few hundred bytes and about ten objects per
gate.**

## Where that puts the ceiling

| Gates | Approximate memory | Live objects |
|---|---|---|
| 10 thousand | 4 MB | 100 thousand |
| 1 million | 380 MB | 10 million |
| 10 million | 3.8 GB | 100 million |

Memory is not the binding constraint; the object count is. A hundred million live objects means
every generation-2 collection has to trace a hundred million references, and the graph is full of
cross-links, so none of it is cheap to scan. Long before the bytes run out, the collector does.

In practice this design is comfortable into the **hundreds of thousands of gates** and painful
above that. Everything the library actually ships — an adder, a register file, a counter, a small
datapath — lives far below the line.

## What a packed representation would buy

The alternative is to stop using objects for circuit elements and put them in flat arrays, with
integer handles instead of references. Roughly:

| Element | Packed size |
|---|---|
| A gate: type, input count, first-input handle, output handle, flags | 16 bytes |
| An input: state, polarity, owning-gate handle | 6 bytes |
| An output: first-fanout handle, count | 8 bytes |
| One fanout entry | 5 bytes |

A two-input gate with an average fan-out of two comes to **about 50 bytes and zero GC objects** —
call it six to eight times less memory, and, more importantly, nothing at all for the collector to
trace. Ten million gates becomes a few hundred megabytes of arrays that the runtime never walks.

That technique — an arena with typed handles — is common in C++ and Rust and unusual in .NET,
where the base class library is index-limited to `int` anyway. [Holdfast](https://www.nuget.org/packages/Holdfast) is one .NET implementation of it, by the same author as
this library.

## Why Fanout does not do that

Two reasons, and the second is the real one.

**It would not make simulation faster.** Propagation is pointer chasing over a graph: each step
reads a value, follows a link, reads the next. That is bound by memory latency, which has barely
moved in fifteen years — roughly 50–80 nanoseconds for a miss, whatever the representation.
Packing the data makes a large circuit *fit*; it does not make a small one *quick*.

**It would cost this library what makes it worth reading.** `FullAdder`'s constructor is twelve
lines of `ConnectTo` calls that look exactly like a schematic. Under an arena those become handle
arithmetic, and the person who came here to see how a full adder is wired would leave no wiser.
The audience for a readable gate-level simulator and the audience for a ten-million-gate engine
are not the same people, and trying to serve both in one type would serve neither.

So: if you need circuits at that scale, the honest answer is that this is not the library, and
the packed engine is a different project that shares this one's semantics — three states,
controlling-value evaluation, breadth-first settling — and not its API.

## Making the most of what is here

A few things do help within the object-graph design:

- **Reuse a circuit across stimuli.** Building it is the expensive part; driving it again is not.
  Every exhaustive test in this repository builds one circuit and loops over inputs.
- **Treat `Circuit.LastRunRounds` as a cost signal only.** It is cheap to read, but it is not
  logic depth — see [How it works](architecture.md#rounds-are-not-depth).
- **Give wide gates their width up front.** `new And(8)` allocates once; adding eight inputs one
  at a time grows a list.
- **Do not create a `Module` per bit** where a single wide block will do. The per-module overhead
  is a handful of ports, but it adds up across a wide datapath.
