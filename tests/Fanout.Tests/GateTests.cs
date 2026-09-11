using Fanout.Gates;
using Xunit;

namespace Fanout.Tests;

public class GateTests
{
    private static LogicState Evaluate(Gate gate, params int[] inputs)
    {
        Circuit circuit = new();

        for (int i = 0; i < inputs.Length; i++)
        {
            Port port = circuit.AddInput();
            port.ConnectTo(gate, i);
            port.SetState(Bench.Bit(inputs[i]));
        }

        circuit.Run();
        return gate.Output.State;
    }

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(0, 1, 0)]
    [InlineData(1, 0, 0)]
    [InlineData(1, 1, 1)]
    public void And_follows_its_truth_table(int a, int b, int expected)
        => Assert.Equal(Bench.Bit(expected), Evaluate(new And(), a, b));

    [Theory]
    [InlineData(0, 0, 1)]
    [InlineData(0, 1, 1)]
    [InlineData(1, 0, 1)]
    [InlineData(1, 1, 0)]
    public void Nand_follows_its_truth_table(int a, int b, int expected)
        => Assert.Equal(Bench.Bit(expected), Evaluate(new Nand(), a, b));

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(0, 1, 1)]
    [InlineData(1, 0, 1)]
    [InlineData(1, 1, 1)]
    public void Or_follows_its_truth_table(int a, int b, int expected)
        => Assert.Equal(Bench.Bit(expected), Evaluate(new Or(), a, b));

    [Theory]
    [InlineData(0, 0, 1)]
    [InlineData(0, 1, 0)]
    [InlineData(1, 0, 0)]
    [InlineData(1, 1, 0)]
    public void Nor_follows_its_truth_table(int a, int b, int expected)
        => Assert.Equal(Bench.Bit(expected), Evaluate(new Nor(), a, b));

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(0, 1, 1)]
    [InlineData(1, 0, 1)]
    [InlineData(1, 1, 0)]
    public void Xor_follows_its_truth_table(int a, int b, int expected)
        => Assert.Equal(Bench.Bit(expected), Evaluate(new Xor(), a, b));

    [Theory]
    [InlineData(0, 0, 1)]
    [InlineData(0, 1, 0)]
    [InlineData(1, 0, 0)]
    [InlineData(1, 1, 1)]
    public void Xnor_follows_its_truth_table(int a, int b, int expected)
        => Assert.Equal(Bench.Bit(expected), Evaluate(new Xnor(), a, b));

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 0)]
    public void Not_inverts(int a, int expected)
        => Assert.Equal(Bench.Bit(expected), Evaluate(new Not(), a));

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    public void Buffer_passes_its_input_through(int a, int expected)
        => Assert.Equal(Bench.Bit(expected), Evaluate(new BufferGate(), a));

    [Fact]
    public void Wide_and_needs_every_input_high()
    {
        Assert.Equal(LogicState.One, Evaluate(new And(4), 1, 1, 1, 1));
        Assert.Equal(LogicState.Zero, Evaluate(new And(4), 1, 1, 0, 1));
    }

    [Fact]
    public void Xor_of_four_inputs_is_odd_parity()
    {
        Assert.Equal(LogicState.Zero, Evaluate(new Xor(4), 1, 1, 0, 0));
        Assert.Equal(LogicState.One, Evaluate(new Xor(4), 1, 1, 1, 0));
    }

    [Fact]
    public void An_inverted_input_saves_a_not_gate()
    {
        And gate = new(PinPolarity.Inverted, PinPolarity.Normal);
        Assert.Equal(LogicState.One, Evaluate(gate, 0, 1));
    }

    [Fact]
    public void And_resolves_from_a_controlling_zero_alone()
    {
        Circuit circuit = new();
        And gate = new(3);

        Port port = circuit.AddInput();
        port.ConnectTo(gate, 0);
        port.SetState(LogicState.Zero);
        circuit.Run();

        Assert.Equal(LogicState.Zero, gate.Output.State);
        Assert.Equal(LogicState.Unknown, gate.Input(1).State);
    }

    [Fact]
    public void Or_resolves_from_a_controlling_one_alone()
    {
        Circuit circuit = new();
        Or gate = new(3);

        Port port = circuit.AddInput();
        port.ConnectTo(gate, 0);
        port.SetState(LogicState.One);
        circuit.Run();

        Assert.Equal(LogicState.One, gate.Output.State);
    }

    [Fact]
    public void Xor_waits_for_every_input_because_it_has_no_controlling_value()
    {
        Circuit circuit = new();
        Xor gate = new();

        Port port = circuit.AddInput();
        port.ConnectTo(gate, 0);
        port.SetState(LogicState.One);
        circuit.Run();

        Assert.Equal(LogicState.Unknown, gate.Output.State);
    }
}
