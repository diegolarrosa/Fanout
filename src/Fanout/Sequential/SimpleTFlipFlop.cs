using Fanout.Gates;

namespace Fanout.Sequential;

/// <summary>
/// A toggle flip-flop without preset or clear.
/// </summary>
/// <remarks>
/// <para>Input: <c>T</c>. Outputs: <c>Q</c>, <c>QN</c>.</para>
/// <para>
/// With no preset or clear the cross-coupled output pair would never resolve, so the
/// constructor seeds it directly to <c>Q = 0</c>. That is a construction-time shortcut, not
/// something a real circuit can do — a real one powers up in whichever state it lands.
/// </para>
/// </remarks>
public sealed class SimpleTFlipFlop : Module
{
    /// <summary>The port layout shared by every instance.</summary>
    public static readonly ModuleLayout Layout = CreateLayout();

    /// <summary>Builds a toggle flip-flop triggered on <paramref name="edge"/>.</summary>
    public SimpleTFlipFlop(ClockEdge edge = ClockEdge.Falling)
    {
        Port toggle = new();
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

        AddInput(toggle);
        AddOutput(q);
        AddOutput(qn);

        toggle.ConnectTo(invert, 0);

        if (edge == ClockEdge.Falling)
        {
            toggle.ConnectTo(nand1, 1);
            toggle.ConnectTo(nand2, 0);
            invert.ConnectTo(nand5, 1);
            invert.ConnectTo(nand6, 0);
        }
        else
        {
            toggle.ConnectTo(nand5, 1);
            toggle.ConnectTo(nand6, 0);
            invert.ConnectTo(nand1, 1);
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
        nand8.ConnectTo(nand1, 0);

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
            .AddInputGroup("T", 0)
            .AddOutputGroup("Q", 0)
            .AddOutputGroup("QN", 1);
}
