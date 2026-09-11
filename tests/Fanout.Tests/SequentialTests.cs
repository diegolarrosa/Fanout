using Fanout.Sequential;
using Xunit;

namespace Fanout.Tests;

public class SequentialTests
{
    [Fact]
    public void Nor_latch_sets_resets_and_holds()
    {
        Circuit circuit = new();
        NorSRLatch latch = new();

        Port reset = Bench.Drive(circuit, latch, "R");
        Port set = Bench.Drive(circuit, latch, "S");

        set.SetState(LogicState.One);
        reset.SetState(LogicState.Zero);
        circuit.Run();

        Assert.Equal(LogicState.One, Bench.Read(latch, "Q"));
        Assert.Equal(LogicState.Zero, Bench.Read(latch, "QN"));

        set.SetState(LogicState.Zero);
        circuit.Run();

        Assert.Equal(LogicState.One, Bench.Read(latch, "Q"));

        reset.SetState(LogicState.One);
        circuit.Run();

        Assert.Equal(LogicState.Zero, Bench.Read(latch, "Q"));
        Assert.Equal(LogicState.One, Bench.Read(latch, "QN"));

        reset.SetState(LogicState.Zero);
        circuit.Run();

        Assert.Equal(LogicState.Zero, Bench.Read(latch, "Q"));
    }

    [Fact]
    public void Nand_latch_inputs_are_active_low()
    {
        Circuit circuit = new();
        NandSRLatch latch = new();

        Port setN = Bench.Drive(circuit, latch, "SN");
        Port resetN = Bench.Drive(circuit, latch, "RN");

        setN.SetState(LogicState.Zero);
        resetN.SetState(LogicState.One);
        circuit.Run();

        Assert.Equal(LogicState.One, Bench.Read(latch, "Q"));

        setN.SetState(LogicState.One);
        circuit.Run();

        Assert.Equal(LogicState.One, Bench.Read(latch, "Q"));

        resetN.SetState(LogicState.Zero);
        circuit.Run();

        Assert.Equal(LogicState.Zero, Bench.Read(latch, "Q"));
    }

    [Fact]
    public void A_cross_coupled_latch_has_no_state_until_something_forces_one()
    {
        Circuit circuit = new();
        NorSRLatch latch = new();

        Port reset = Bench.Drive(circuit, latch, "R");
        Port set = Bench.Drive(circuit, latch, "S");

        reset.SetState(LogicState.Zero);
        set.SetState(LogicState.Zero);
        circuit.Run();

        Assert.Equal(LogicState.Unknown, Bench.Read(latch, "Q"));
    }

    [Fact]
    public void D_latch_is_transparent_while_the_clock_is_high()
    {
        Circuit circuit = new();
        DLatch latch = new();

        Port d = Bench.Drive(circuit, latch, "D");
        Port clock = Bench.Drive(circuit, latch, "CLK");

        d.SetState(LogicState.One);
        clock.SetState(LogicState.One);
        circuit.Run();

        Assert.Equal(LogicState.One, Bench.Read(latch, "Q"));

        d.SetState(LogicState.Zero);
        circuit.Run();

        Assert.Equal(LogicState.Zero, Bench.Read(latch, "Q"));

        clock.SetState(LogicState.Zero);
        circuit.Run();
        d.SetState(LogicState.One);
        circuit.Run();

        Assert.Equal(LogicState.Zero, Bench.Read(latch, "Q"));
    }

    [Fact]
    public void D_flip_flop_clears_asynchronously_and_then_captures_on_the_edge()
    {
        Circuit circuit = new();
        DFlipFlop flipFlop = new(ClockEdge.Falling);

        Port d = Bench.Drive(circuit, flipFlop, "D");
        Port clock = Bench.Drive(circuit, flipFlop, "CLK");
        Port preset = Bench.Drive(circuit, flipFlop, "PRE");
        Port clear = Bench.Drive(circuit, flipFlop, "CLR");

        preset.SetState(LogicState.One);
        d.SetState(LogicState.Zero);
        clock.SetState(LogicState.One);
        circuit.Run();

        Bench.AssertClear(circuit, clear);

        Assert.Equal(LogicState.Zero, Bench.Read(flipFlop, "Q"));
        Assert.Equal(LogicState.One, Bench.Read(flipFlop, "QN"));

        d.SetState(LogicState.One);
        circuit.Run();

        Assert.Equal(LogicState.Zero, Bench.Read(flipFlop, "Q"));

        clock.SetState(LogicState.Zero);
        circuit.Run();

        Assert.Equal(LogicState.One, Bench.Read(flipFlop, "Q"));
        Assert.Equal(LogicState.Zero, Bench.Read(flipFlop, "QN"));
    }

    [Fact]
    public void D_flip_flop_presets_asynchronously()
    {
        Circuit circuit = new();
        DFlipFlop flipFlop = new(ClockEdge.Falling);

        Port d = Bench.Drive(circuit, flipFlop, "D");
        Port clock = Bench.Drive(circuit, flipFlop, "CLK");
        Port preset = Bench.Drive(circuit, flipFlop, "PRE");
        Port clear = Bench.Drive(circuit, flipFlop, "CLR");

        clear.SetState(LogicState.One);
        d.SetState(LogicState.Zero);
        clock.SetState(LogicState.One);
        circuit.Run();

        preset.SetState(LogicState.Zero);
        circuit.Run();

        Assert.Equal(LogicState.One, Bench.Read(flipFlop, "Q"));

        preset.SetState(LogicState.One);
        circuit.Run();

        Assert.Equal(LogicState.One, Bench.Read(flipFlop, "Q"));
    }

    [Fact]
    public void Jk_flip_flop_holds_sets_clears_and_toggles()
    {
        Circuit circuit = new();
        JKFlipFlop flipFlop = new(ClockEdge.Falling);

        Port j = Bench.Drive(circuit, flipFlop, "J");
        Port k = Bench.Drive(circuit, flipFlop, "K");
        Port clock = Bench.Drive(circuit, flipFlop, "CLK");
        Port preset = Bench.Drive(circuit, flipFlop, "PRE");
        Port clear = Bench.Drive(circuit, flipFlop, "CLR");

        preset.SetState(LogicState.One);
        j.SetState(LogicState.Zero);
        k.SetState(LogicState.Zero);
        clock.SetState(LogicState.One);
        circuit.Run();

        Bench.AssertClear(circuit, clear);
        Assert.Equal(LogicState.Zero, Bench.Read(flipFlop, "Q"));

        // J = 0, K = 0 holds.
        Bench.FallingPulse(circuit, clock);
        Assert.Equal(LogicState.Zero, Bench.Read(flipFlop, "Q"));

        // J = 1, K = 0 sets.
        j.SetState(LogicState.One);
        circuit.Run();
        Bench.FallingPulse(circuit, clock);
        Assert.Equal(LogicState.One, Bench.Read(flipFlop, "Q"));

        // J = 0, K = 1 clears.
        j.SetState(LogicState.Zero);
        k.SetState(LogicState.One);
        circuit.Run();
        Bench.FallingPulse(circuit, clock);
        Assert.Equal(LogicState.Zero, Bench.Read(flipFlop, "Q"));

        // J = 1, K = 1 toggles.
        j.SetState(LogicState.One);
        circuit.Run();
        Bench.FallingPulse(circuit, clock);
        Assert.Equal(LogicState.One, Bench.Read(flipFlop, "Q"));

        Bench.FallingPulse(circuit, clock);
        Assert.Equal(LogicState.Zero, Bench.Read(flipFlop, "Q"));
    }

    [Fact]
    public void Parallel_register_loads_on_the_clock_edge_and_holds_between_edges()
    {
        const int Width = 4;

        Circuit circuit = new();
        ParallelRegister register = new(Width, ClockEdge.Rising);

        Port[] data = Bench.DriveBus(circuit, register, "D", Width);
        Port clock = Bench.Drive(circuit, register, "CLK");
        Port preset = Bench.Drive(circuit, register, "PRE");
        Port clear = Bench.Drive(circuit, register, "CLR");

        preset.SetState(LogicState.One);
        clock.SetState(LogicState.Zero);
        Bench.SetBus(data, 0);
        circuit.Run();

        Bench.AssertClear(circuit, clear);
        Assert.Equal(0, Bench.ReadBus(register, "Q", Width));

        Bench.SetBus(data, 0b1011);
        circuit.Run();
        Assert.Equal(0, Bench.ReadBus(register, "Q", Width));

        Bench.RisingPulse(circuit, clock);
        Assert.Equal(0b1011, Bench.ReadBus(register, "Q", Width));

        Bench.SetBus(data, 0b0110);
        circuit.Run();
        Assert.Equal(0b1011, Bench.ReadBus(register, "Q", Width));

        Bench.RisingPulse(circuit, clock);
        Assert.Equal(0b0110, Bench.ReadBus(register, "Q", Width));
    }

    [Fact]
    public void Shift_register_walks_a_one_along_its_stages()
    {
        const int Width = 4;

        Circuit circuit = new();
        ShiftRegister register = new(Width, ClockEdge.Rising);

        Port serialIn = Bench.Drive(circuit, register, "D");
        Port clock = Bench.Drive(circuit, register, "CLK");
        Port preset = Bench.Drive(circuit, register, "PRE");
        Port clear = Bench.Drive(circuit, register, "CLR");

        preset.SetState(LogicState.One);
        clock.SetState(LogicState.Zero);
        serialIn.SetState(LogicState.Zero);
        circuit.Run();

        Bench.AssertClear(circuit, clear);
        Assert.Equal(0, Bench.ReadBus(register, "Q", Width));

        serialIn.SetState(LogicState.One);
        circuit.Run();
        Bench.RisingPulse(circuit, clock);
        Assert.Equal(0b0001, Bench.ReadBus(register, "Q", Width));

        serialIn.SetState(LogicState.Zero);
        circuit.Run();
        Bench.RisingPulse(circuit, clock);
        Assert.Equal(0b0010, Bench.ReadBus(register, "Q", Width));

        Bench.RisingPulse(circuit, clock);
        Assert.Equal(0b0100, Bench.ReadBus(register, "Q", Width));

        Bench.RisingPulse(circuit, clock);
        Assert.Equal(0b1000, Bench.ReadBus(register, "Q", Width));

        Bench.RisingPulse(circuit, clock);
        Assert.Equal(0b0000, Bench.ReadBus(register, "Q", Width));
    }

    [Fact]
    public void Shift_load_register_takes_the_parallel_inputs_when_load_is_high()
    {
        const int Width = 4;

        Circuit circuit = new();
        ShiftLoadRegister register = new(Width, ClockEdge.Rising);

        Port[] data = Bench.DriveBus(circuit, register, "D", Width);
        Port serialIn = Bench.Drive(circuit, register, "SIN");
        Port load = Bench.Drive(circuit, register, "LOAD");
        Port clock = Bench.Drive(circuit, register, "CLK");
        Port preset = Bench.Drive(circuit, register, "PRE");
        Port clear = Bench.Drive(circuit, register, "CLR");

        preset.SetState(LogicState.One);
        clock.SetState(LogicState.Zero);
        serialIn.SetState(LogicState.Zero);
        load.SetState(LogicState.One);
        Bench.SetBus(data, 0b1101);
        circuit.Run();

        Bench.AssertClear(circuit, clear);
        Assert.Equal(0, Bench.ReadBus(register, "Q", Width));

        Bench.RisingPulse(circuit, clock);
        Assert.Equal(0b1101, Bench.ReadBus(register, "Q", Width));

        load.SetState(LogicState.Zero);
        circuit.Run();
        Bench.RisingPulse(circuit, clock);
        Assert.Equal(0b1010, Bench.ReadBus(register, "Q", Width));
    }

    [Fact]
    public void Ripple_counter_starts_at_zero_and_advances_by_one()
    {
        Circuit circuit = new();
        RippleCounter counter = new(3, ClockEdge.Rising, CountDirection.Up);

        Port clock = Bench.Drive(circuit, counter, "CLK");
        Port preset = Bench.Drive(circuit, counter, "PRE");
        Port clear = Bench.Drive(circuit, counter, "CLR");

        preset.SetState(LogicState.One);
        clock.SetState(LogicState.Zero);
        circuit.Run();

        Bench.AssertClear(circuit, clear);
        Assert.Equal(0, Bench.ReadBus(counter, "Q", 3));

        Bench.RisingPulse(circuit, clock);
        Assert.Equal(1, Bench.ReadBus(counter, "Q", 3));
    }

    [Fact]
    public void Ripple_counter_counts_up_through_a_full_cycle()
    {
        const int Width = 3;

        Circuit circuit = new();
        RippleCounter counter = new(Width, ClockEdge.Rising, CountDirection.Up);

        Port clock = Bench.Drive(circuit, counter, "CLK");
        Port preset = Bench.Drive(circuit, counter, "PRE");
        Port clear = Bench.Drive(circuit, counter, "CLR");

        preset.SetState(LogicState.One);
        clock.SetState(LogicState.Zero);
        circuit.Run();

        Bench.AssertClear(circuit, clear);

        for (int expected = 1; expected <= 8; expected++)
        {
            Bench.RisingPulse(circuit, clock);
            Assert.Equal(expected % 8, Bench.ReadBus(counter, "Q", Width));
        }
    }

    [Fact]
    public void Ripple_counter_counts_down_when_asked_to()
    {
        const int Width = 3;

        Circuit circuit = new();
        RippleCounter counter = new(Width, ClockEdge.Rising, CountDirection.Down);

        Port clock = Bench.Drive(circuit, counter, "CLK");
        Port preset = Bench.Drive(circuit, counter, "PRE");
        Port clear = Bench.Drive(circuit, counter, "CLR");

        preset.SetState(LogicState.One);
        clock.SetState(LogicState.Zero);
        circuit.Run();

        Bench.AssertClear(circuit, clear);

        for (int step = 1; step <= 8; step++)
        {
            Bench.RisingPulse(circuit, clock);
            Assert.Equal((8 - step) % 8, Bench.ReadBus(counter, "Q", Width));
        }
    }

    [Fact]
    public void A_latch_written_as_a_primitive_behaves_like_the_one_built_from_gates()
    {
        Circuit circuit = new();
        DLatchPrimitive latch = new();

        Port d = circuit.AddInput();
        Port enable = circuit.AddInput();
        d.ConnectTo(latch, 0);
        enable.ConnectTo(latch, 1);

        d.SetState(LogicState.One);
        enable.SetState(LogicState.One);
        circuit.Run();

        Assert.Equal(LogicState.One, latch.Output.State);
        Assert.Equal(LogicState.Zero, latch.OutputQN.State);

        enable.SetState(LogicState.Zero);
        circuit.Run();
        d.SetState(LogicState.Zero);
        circuit.Run();

        Assert.Equal(LogicState.One, latch.Output.State);

        enable.SetState(LogicState.One);
        circuit.Run();

        Assert.Equal(LogicState.Zero, latch.Output.State);
    }
}
