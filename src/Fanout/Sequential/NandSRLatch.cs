using Fanout.Gates;

namespace Fanout.Sequential;

/// <summary>
/// A set-reset latch built from two cross-coupled NAND gates. Both inputs are active low.
/// </summary>
/// <remarks>
/// <para>Inputs: <c>SN</c> (set, active low), <c>RN</c> (reset, active low).
/// Outputs: <c>Q</c>, <c>QN</c>.</para>
/// <para>
/// Pulling <c>SN</c> low drives <c>Q</c> high; pulling <c>RN</c> low drives <c>Q</c> low; with
/// both high the latch holds. Both low at once is the forbidden state and drives both outputs
/// high, which is what the real circuit does too.
/// </para>
/// <para>
/// With both inputs high from the start, neither NAND has a controlling value and the latch has
/// no defined state — <c>Q</c> stays <see cref="LogicState.Unknown"/> until one input is pulsed
/// low. That is not a simulator limitation; it is the metastability of a real cross-coupled pair.
/// </para>
/// </remarks>
public sealed class NandSRLatch : Module
{
    /// <summary>The port layout shared by every instance.</summary>
    public static readonly ModuleLayout Layout = CreateLayout();

    /// <summary>Builds the latch.</summary>
    public NandSRLatch()
    {
        Port setN = new();
        Port resetN = new();
        Port q = new();
        Port qn = new();

        Nand nand1 = new();
        Nand nand2 = new();

        AddInput(setN);
        AddInput(resetN);
        AddOutput(q);
        AddOutput(qn);

        setN.ConnectTo(nand1, 0);
        resetN.ConnectTo(nand2, 1);
        nand2.ConnectTo(nand1, 1);
        nand1.ConnectTo(nand2, 0);

        nand1.ConnectTo(q);
        nand2.ConnectTo(qn);
    }

    /// <inheritdoc />
    public override int InputIndex(string name) => Layout.InputIndex(name);

    /// <inheritdoc />
    public override int OutputIndex(string name) => Layout.OutputIndex(name);

    private static ModuleLayout CreateLayout()
        => new ModuleLayout()
            .AddInputGroup("SN", 0)
            .AddInputGroup("RN", 1)
            .AddOutputGroup("Q", 0)
            .AddOutputGroup("QN", 1);
}
