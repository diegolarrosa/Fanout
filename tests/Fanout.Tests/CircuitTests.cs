using Fanout.Combinational;
using Fanout.Gates;
using Xunit;

namespace Fanout.Tests;

public class CircuitTests
{
    [Fact]
    public void A_settled_circuit_does_no_work_when_nothing_changes()
    {
        Circuit circuit = new();
        And gate = new();

        Port a = circuit.AddInput();
        Port b = circuit.AddInput();
        a.ConnectTo(gate, 0);
        b.ConnectTo(gate, 1);

        a.SetState(LogicState.One);
        b.SetState(LogicState.One);

        Assert.True(circuit.Run() > 0);
        Assert.Equal(0, circuit.Run());

        // Driving a port with the value it already holds is not a change.
        a.SetState(LogicState.One);
        Assert.Equal(0, circuit.Run());
    }

    [Fact]
    public void The_settled_result_does_not_depend_on_the_order_inputs_are_driven()
    {
        // Evaluation order within a round is unspecified, so the round count varies. The fixed
        // point the circuit settles to does not, and that is the property that matters.
        int lsbFirst = AddDrivingInOrder(Enumerable.Range(0, 8));
        int msbFirst = AddDrivingInOrder(Enumerable.Range(0, 8).Reverse());

        Assert.Equal(0b1_0110_1010, lsbFirst);
        Assert.Equal(lsbFirst, msbFirst);
    }

    [Fact]
    public void A_ring_oscillator_is_reported_rather_than_hanging()
    {
        Circuit circuit = new();

        Nand nand = new();
        Not first = new();
        Not second = new();

        Port enable = circuit.AddInput();
        enable.ConnectTo(nand, 0);

        nand.ConnectTo(first, 0);
        first.ConnectTo(second, 0);
        second.ConnectTo(nand, 1);

        circuit.MaxRounds = 200;

        enable.SetState(LogicState.Zero);
        circuit.Run();

        Assert.Equal(LogicState.One, nand.Output.State);

        enable.SetState(LogicState.One);

        Assert.Throws<CircuitOscillationException>(() => circuit.Run());
    }

    [Fact]
    public void A_module_can_be_assembled_at_run_time_without_declaring_a_type()
    {
        ModuleLayout layout = new ModuleLayout()
            .AddInputGroup("IN", 0, 2)
            .AddOutputGroup("OUT", 0);

        CustomModule block = new("and2", layout);

        Port inA = new();
        Port inB = new();
        Port output = new();
        And gate = new();

        block.AddInput(inA);
        block.AddInput(inB);
        block.AddOutput(output);

        inA.ConnectTo(gate, 0);
        inB.ConnectTo(gate, 1);
        gate.ConnectTo(output);

        Circuit circuit = new();
        Port driveA = Bench.Drive(circuit, block, "IN");
        Port driveB = circuit.AddInput();
        driveB.ConnectTo(block, "IN", 1);

        driveA.SetState(LogicState.One);
        driveB.SetState(LogicState.One);
        circuit.Run();

        Assert.Equal(LogicState.One, Bench.Read(block, "OUT"));

        driveB.SetState(LogicState.Zero);
        circuit.Run();

        Assert.Equal(LogicState.Zero, Bench.Read(block, "OUT"));
    }

    [Fact]
    public void A_gate_is_queued_once_even_when_several_of_its_inputs_change()
    {
        GateQueue queue = new();
        And gate = new(3);

        Assert.True(queue.Enqueue(gate));
        Assert.False(queue.Enqueue(gate));
        Assert.Single(queue);
        Assert.True(queue.Contains(gate));

        queue.Clear();
        Assert.Empty(queue);
    }

    [Fact]
    public void A_net_drives_every_input_attached_to_it()
    {
        Circuit circuit = new();

        Not source = new();
        And sinkA = new();
        And sinkB = new();

        Port input = circuit.AddInput();
        input.ConnectTo(source, 0);
        source.ConnectTo(sinkA, 0);
        source.ConnectTo(sinkB, 0);

        Assert.Equal(2, source.Output.Net.FanOut);

        input.SetState(LogicState.Zero);
        circuit.Run();

        Assert.Equal(LogicState.One, sinkA.Input(0).State);
        Assert.Equal(LogicState.One, sinkB.Input(0).State);
    }

    [Fact]
    public void A_circuit_has_no_named_ports()
    {
        Circuit circuit = new();

        Assert.Throws<NotSupportedException>(() => circuit.InputIndex("CLK"));
        Assert.Throws<NotSupportedException>(() => circuit.OutputIndex("Q"));
    }

    /// <summary>
    /// Adds 0xA5 + 0xC4 + 1 on an 8-bit adder, driving the input bits in the given order.
    /// Returns SUM with COUT as bit 8.
    /// </summary>
    private static int AddDrivingInOrder(IEnumerable<int> order)
    {
        const int Width = 8;
        const int X = 0xA5;
        const int Y = 0xC4;

        Circuit circuit = new();
        RippleCarryAdder adder = new(Width);

        Port[] a = Bench.DriveBus(circuit, adder, "A", Width);
        Port[] b = Bench.DriveBus(circuit, adder, "B", Width);
        Port carryIn = Bench.Drive(circuit, adder, "CIN");

        foreach (int i in order)
        {
            a[i].SetState(Bench.Bit((X >> i) & 1));
            b[i].SetState(Bench.Bit((Y >> i) & 1));
        }

        carryIn.SetState(LogicState.One);
        circuit.Run();

        return Bench.ReadBus(adder, "SUM", Width)
               + (Bench.Read(adder, "COUT") == LogicState.One ? 1 << Width : 0);
    }
}
