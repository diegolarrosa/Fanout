using Fanout.Gates;

namespace Fanout.Sequential;

/// <summary>
/// A JK flip-flop without preset or clear.
/// </summary>
/// <remarks>
/// <para>Inputs: <c>J</c>, <c>K</c>, <c>CLK</c>. Outputs: <c>Q</c>, <c>QN</c>.</para>
/// <para>The constructor seeds the output pair to <c>Q = 0</c>, as <see cref="SimpleTFlipFlop"/> does.</para>
/// </remarks>
public sealed class SimpleJKFlipFlop : Module
{
    /// <summary>The port layout shared by every instance.</summary>
    public static readonly ModuleLayout Layout = CreateLayout();

    /// <summary>Builds a JK flip-flop triggered on <paramref name="edge"/>.</summary>
    public SimpleJKFlipFlop(ClockEdge edge = ClockEdge.Falling)
    {
        Port j = new();
        Port k = new();
        Port clock = new();
        Port q = new();
        Port qn = new();

        Nand nand1 = new(3);
        Nand nand2 = new(3);
        Nand nand3 = new();
        Nand nand4 = new();
        Nand nand5 = new();
        Nand nand6 = new();
        Nand nand7 = new();
        Nand nand8 = new();
        Not invert = new();

        AddInput(j);
        AddInput(k);
        AddInput(clock);
        AddOutput(q);
        AddOutput(qn);

        j.ConnectTo(nand1, 0);
        k.ConnectTo(nand2, 2);
        clock.ConnectTo(invert, 0);

        if (edge == ClockEdge.Falling)
        {
            clock.ConnectTo(nand1, 2);
            clock.ConnectTo(nand2, 0);
            invert.ConnectTo(nand5, 1);
            invert.ConnectTo(nand6, 0);
        }
        else
        {
            clock.ConnectTo(nand5, 1);
            clock.ConnectTo(nand6, 0);
            invert.ConnectTo(nand1, 2);
            invert.ConnectTo(nand2, 0);
        }

        nand1.ConnectTo(nand3, 0);
        nand2.ConnectTo(nand4, 1);
        nand3.ConnectTo(nand4, 0);
        nand4.ConnectTo(nand3, 1);
        nand3.ConnectTo(nand5, 0);
        nand4.ConnectTo(nand6, 1);
        nand5.ConnectTo(nand7, 0);
        nand6.ConnectTo(nand8, 1);
        nand7.ConnectTo(nand8, 0);
        nand8.ConnectTo(nand7, 1);
        nand7.ConnectTo(nand2, 1);
        nand8.ConnectTo(nand1, 1);

        nand7.ConnectTo(q);
        nand8.ConnectTo(qn);

        nand7.ForceOutput(LogicState.Zero);
        nand8.ForceOutput(LogicState.One);
    }

    /// <inheritdoc />
    public override int InputIndex(string name) => Layout.InputIndex(name);

    /// <inheritdoc />
    public override int OutputIndex(string name) => Layout.OutputIndex(name);

    private static ModuleLayout CreateLayout()
        => new ModuleLayout()
            .AddInputGroup("J", 0)
            .AddInputGroup("K", 1)
            .AddInputGroup("CLK", 2)
            .AddOutputGroup("Q", 0)
            .AddOutputGroup("QN", 1);
}
