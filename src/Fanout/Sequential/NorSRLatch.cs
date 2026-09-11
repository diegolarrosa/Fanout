using Fanout.Gates;

namespace Fanout.Sequential;

/// <summary>
/// A set-reset latch built from two cross-coupled NOR gates. Both inputs are active high.
/// </summary>
/// <remarks>
/// <para>Inputs: <c>R</c> (reset), <c>S</c> (set). Outputs: <c>Q</c>, <c>QN</c>.</para>
/// <para>
/// Driving <c>S</c> high sets <c>Q</c>; driving <c>R</c> high clears it; with both low the latch
/// holds. Both high at once is the forbidden state and drives both outputs low.
/// </para>
/// <para>
/// With both inputs low from the start the latch has no defined state, and <c>Q</c> stays
/// <see cref="LogicState.Unknown"/> until one of them is pulsed.
/// </para>
/// </remarks>
public sealed class NorSRLatch : Module
{
    /// <summary>The port layout shared by every instance.</summary>
    public static readonly ModuleLayout Layout = CreateLayout();

    /// <summary>Builds the latch.</summary>
    public NorSRLatch()
    {
        Port reset = new();
        Port set = new();
        Port q = new();
        Port qn = new();

        Nor nor1 = new();
        Nor nor2 = new();

        AddInput(reset);
        AddInput(set);
        AddOutput(q);
        AddOutput(qn);

        reset.ConnectTo(nor1, 0);
        set.ConnectTo(nor2, 1);
        nor2.ConnectTo(nor1, 1);
        nor1.ConnectTo(nor2, 0);

        nor1.ConnectTo(q);
        nor2.ConnectTo(qn);
    }

    /// <inheritdoc />
    public override int InputIndex(string name) => Layout.InputIndex(name);

    /// <inheritdoc />
    public override int OutputIndex(string name) => Layout.OutputIndex(name);

    private static ModuleLayout CreateLayout()
        => new ModuleLayout()
            .AddInputGroup("R", 0)
            .AddInputGroup("S", 1)
            .AddOutputGroup("Q", 0)
            .AddOutputGroup("QN", 1);
}
