using Fanout.Gates;

namespace Fanout.Sequential;

/// <summary>
/// A gated D latch built from four NAND gates: transparent while <c>CLK</c> is high, holding
/// while it is low.
/// </summary>
/// <remarks>
/// <para>Inputs: <c>D</c>, <c>CLK</c>. Outputs: <c>Q</c>, <c>QN</c>.</para>
/// <para>
/// This is level-sensitive, not edge-triggered: while the clock is high the output follows the
/// input continuously. For edge behaviour use <see cref="DFlipFlop"/>.
/// </para>
/// </remarks>
public sealed class DLatch : Module
{
    /// <summary>The port layout shared by every instance.</summary>
    public static readonly ModuleLayout Layout = CreateLayout();

    /// <summary>Builds the latch.</summary>
    public DLatch()
    {
        Port d = new();
        Port clock = new();
        Port q = new();
        Port qn = new();

        Nand nand1 = new();
        Nand nand2 = new();
        Nand nand3 = new();
        Nand nand4 = new();

        AddInput(d);
        AddInput(clock);
        AddOutput(q);
        AddOutput(qn);

        d.ConnectTo(nand1, 0);

        clock.ConnectTo(nand1, 1);
        clock.ConnectTo(nand2, 1);

        nand1.ConnectTo(nand3, 0);
        nand1.ConnectTo(nand2, 0);
        nand2.ConnectTo(nand4, 1);
        nand3.ConnectTo(nand4, 0);
        nand4.ConnectTo(nand3, 1);

        nand3.ConnectTo(q);
        nand4.ConnectTo(qn);
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
