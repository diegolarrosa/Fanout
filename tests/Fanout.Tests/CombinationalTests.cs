using Fanout.Combinational;
using Xunit;

namespace Fanout.Tests;

public class CombinationalTests
{
    [Theory]
    [InlineData(0, 0, 0, 0, 0)]
    [InlineData(0, 0, 1, 1, 0)]
    [InlineData(0, 1, 0, 1, 0)]
    [InlineData(0, 1, 1, 0, 1)]
    [InlineData(1, 0, 0, 1, 0)]
    [InlineData(1, 0, 1, 0, 1)]
    [InlineData(1, 1, 0, 0, 1)]
    [InlineData(1, 1, 1, 1, 1)]
    public void Full_adder_covers_every_input_combination(int a, int b, int carryIn, int sum, int carryOut)
    {
        Circuit circuit = new();
        FullAdder adder = new();

        Port pa = Bench.Drive(circuit, adder, "A");
        Port pb = Bench.Drive(circuit, adder, "B");
        Port pc = Bench.Drive(circuit, adder, "CIN");

        pa.SetState(Bench.Bit(a));
        pb.SetState(Bench.Bit(b));
        pc.SetState(Bench.Bit(carryIn));
        circuit.Run();

        Assert.Equal(Bench.Bit(sum), Bench.Read(adder, "SUM"));
        Assert.Equal(Bench.Bit(carryOut), Bench.Read(adder, "COUT"));
    }

    [Fact]
    public void Four_bit_adder_is_exhaustively_correct()
    {
        const int Width = 4;

        Circuit circuit = new();
        RippleCarryAdder adder = new(Width);

        Port[] a = Bench.DriveBus(circuit, adder, "A", Width);
        Port[] b = Bench.DriveBus(circuit, adder, "B", Width);
        Port carryIn = Bench.Drive(circuit, adder, "CIN");

        for (int x = 0; x < 16; x++)
        {
            for (int y = 0; y < 16; y++)
            {
                for (int c = 0; c < 2; c++)
                {
                    Bench.SetBus(a, x);
                    Bench.SetBus(b, y);
                    carryIn.SetState(Bench.Bit(c));
                    circuit.Run();

                    int expected = x + y + c;
                    int actual = Bench.ReadBus(adder, "SUM", Width)
                                 + (Bench.Read(adder, "COUT") == LogicState.One ? 16 : 0);

                    Assert.Equal(expected, actual);
                }
            }
        }
    }

    [Fact]
    public void Add_subtractor_adds_when_sub_is_low()
    {
        const int Width = 4;

        Circuit circuit = new();
        AddSubtractor unit = new(Width);

        Port[] a = Bench.DriveBus(circuit, unit, "A", Width);
        Port[] b = Bench.DriveBus(circuit, unit, "B", Width);
        Port subtract = Bench.Drive(circuit, unit, "SUB");

        subtract.SetState(LogicState.Zero);

        for (int x = 0; x < 16; x++)
        {
            for (int y = 0; y < 16; y++)
            {
                Bench.SetBus(a, x);
                Bench.SetBus(b, y);
                circuit.Run();

                Assert.Equal((x + y) & 15, Bench.ReadBus(unit, "SUM", Width));
            }
        }
    }

    [Fact]
    public void Add_subtractor_subtracts_when_sub_is_high()
    {
        const int Width = 4;

        Circuit circuit = new();
        AddSubtractor unit = new(Width);

        Port[] a = Bench.DriveBus(circuit, unit, "A", Width);
        Port[] b = Bench.DriveBus(circuit, unit, "B", Width);
        Port subtract = Bench.Drive(circuit, unit, "SUB");

        subtract.SetState(LogicState.One);

        for (int x = 0; x < 16; x++)
        {
            for (int y = 0; y < 16; y++)
            {
                Bench.SetBus(a, x);
                Bench.SetBus(b, y);
                circuit.Run();

                Assert.Equal((x - y) & 15, Bench.ReadBus(unit, "SUM", Width));

                // The carry out of a two's-complement subtraction is the "no borrow" flag.
                Assert.Equal(x >= y, Bench.Read(unit, "COUT") == LogicState.One);
            }
        }
    }

    [Fact]
    public void Multiplexer_routes_the_selected_input()
    {
        Circuit circuit = new();
        Multiplexer mux = new(4);

        Port[] select = Bench.DriveBus(circuit, mux, "SEL", 2);
        Port[] data = Bench.DriveBus(circuit, mux, "IN", 4);

        // A distinct pattern, so a wrong selection cannot pass by accident.
        int[] pattern = [1, 0, 0, 1];

        for (int i = 0; i < 4; i++)
        {
            data[i].SetState(Bench.Bit(pattern[i]));
        }

        for (int choice = 0; choice < 4; choice++)
        {
            Bench.SetBus(select, choice);
            circuit.Run();

            Assert.Equal(Bench.Bit(pattern[choice]), Bench.Read(mux, "OUT"));
        }
    }

    [Fact]
    public void Multiplexer_follows_the_selected_input_when_it_changes()
    {
        Circuit circuit = new();
        Multiplexer mux = new(2);

        Port[] select = Bench.DriveBus(circuit, mux, "SEL", 1);
        Port[] data = Bench.DriveBus(circuit, mux, "IN", 2);

        data[0].SetState(LogicState.Zero);
        data[1].SetState(LogicState.Zero);
        Bench.SetBus(select, 1);
        circuit.Run();

        Assert.Equal(LogicState.Zero, Bench.Read(mux, "OUT"));

        data[1].SetState(LogicState.One);
        circuit.Run();

        Assert.Equal(LogicState.One, Bench.Read(mux, "OUT"));
    }

    [Fact]
    public void Demultiplexer_drives_only_the_addressed_output()
    {
        Circuit circuit = new();
        Demultiplexer demux = new(4);

        Port[] select = Bench.DriveBus(circuit, demux, "SEL", 2);
        Port enable = Bench.Drive(circuit, demux, "EN");

        enable.SetState(LogicState.One);

        for (int choice = 0; choice < 4; choice++)
        {
            Bench.SetBus(select, choice);
            circuit.Run();

            Assert.Equal(1 << choice, Bench.ReadBus(demux, "OUT", 4));
        }
    }

    [Fact]
    public void Demultiplexer_holds_every_output_low_when_disabled()
    {
        Circuit circuit = new();
        Demultiplexer demux = new(4);

        Port[] select = Bench.DriveBus(circuit, demux, "SEL", 2);
        Port enable = Bench.Drive(circuit, demux, "EN");

        Bench.SetBus(select, 2);
        enable.SetState(LogicState.Zero);
        circuit.Run();

        Assert.Equal(0, Bench.ReadBus(demux, "OUT", 4));
    }

    [Fact]
    public void An_adder_reports_its_port_layout_by_name()
    {
        RippleCarryAdder adder = new(8);

        Assert.Equal(0, adder.InputIndex("A"));
        Assert.Equal(8, adder.InputIndex("B"));
        Assert.Equal(16, adder.InputIndex("CIN"));
        Assert.Equal(0, adder.OutputIndex("SUM"));
        Assert.Equal(8, adder.OutputIndex("COUT"));
    }

    [Fact]
    public void Asking_for_an_unknown_signal_name_says_which_names_exist()
    {
        FullAdder adder = new();

        KeyNotFoundException error = Assert.Throws<KeyNotFoundException>(() => adder.InputIndex("CARRY"));
        Assert.Contains("CIN", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_multiplexer_needs_at_least_two_inputs()
        => Assert.Throws<ArgumentOutOfRangeException>(() => new Multiplexer(1));

    [Fact]
    public void An_adder_needs_at_least_one_bit()
        => Assert.Throws<ArgumentOutOfRangeException>(() => new RippleCarryAdder(0));
}
