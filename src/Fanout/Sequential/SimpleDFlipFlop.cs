using Fanout.Gates;

namespace Fanout.Sequential;

/// <summary>
/// An edge-triggered D flip-flop without preset or clear: eight NANDs and an inverter.
/// </summary>
/// <remarks>
/// <para>Inputs: <c>D</c>, <c>CLK</c>. Outputs: <c>Q</c>, <c>QN</c>.</para>
/// <para>
/// With nothing to force an initial state, <c>Q</c> is <see cref="LogicState.Unknown"/> until
/// the first clock edge resolves the master-slave pair. Use <see cref="DFlipFlop"/> when a
/// circuit needs a defined power-on state.
/// </para>
/// </remarks>
public sealed class SimpleDFlipFlop : Module
{
    /// <summary>The port layout shared by every instance.</summary>
    public static readonly ModuleLayout Layout = CreateLayout();

    /// <summary>Builds a flip-flop triggered on <paramref name="edge"/>.</summary>
    public SimpleDFlipFlop(ClockEdge edge = ClockEdge.Falling)
    {
        Port d = new();
        Port clock = new();
        Port q = new();
        Port qn = new();

        Nand nand1 = new();
        Nand nand2 = new();
        Nand nand3 = new();
        Nand nand4 = new();
        Nand nand5 = new();
        Nand nand6 = new();
        Nand nand7 = new();
        Nand nand8 = new();
        Not invert = new();

        AddInput(d);
        AddInput(clock);
        AddOutput(q);
        AddOutput(qn);

        d.ConnectTo(nand1, 0);
        clock.ConnectTo(invert, 0);

        if (edge == ClockEdge.Falling)
        {
            clock.ConnectTo(nand1, 1);
            clock.ConnectTo(nand2, 1);
            invert.ConnectTo(nand5, 1);
            invert.ConnectTo(nand6, 0);
        }
        else
        {
            clock.ConnectTo(nand5, 1);
            clock.ConnectTo(nand6, 0);
            invert.ConnectTo(nand1, 1);
            invert.ConnectTo(nand2, 1);
        }

        nand1.ConnectTo(nand3, 0);
        nand1.ConnectTo(nand2, 0);
        nand2.ConnectTo(nand4, 1);
        nand3.ConnectTo(nand4, 0);
        nand4.ConnectTo(nand3, 1);
        nand3.ConnectTo(nand5, 0);
        nand4.ConnectTo(nand6, 1);
        nand5.ConnectTo(nand7, 0);
        nand6.ConnectTo(nand8, 1);
        nand7.ConnectTo(nand8, 0);
        nand8.ConnectTo(nand7, 1);

        nand7.ConnectTo(q);
        nand8.ConnectTo(qn);
    }

    /// <inheritdoc />
    public override int InputIndex(string name) => Layout.InputIndex(name);

    /// <inheritdoc />
    public override int OutputIndex(string name) => Layout.OutputIndex(name);

    private static ModuleLayout CreateLayout()
        => new ModuleLayout()
            .AddInputGroup("D", 0)
            .AddInputGroup("CLK", 1)
            .AddOutputGroup("Q", 0)
            .AddOutputGroup("QN", 1);
}
