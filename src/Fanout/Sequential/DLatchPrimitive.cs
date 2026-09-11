namespace Fanout.Sequential;

/// <summary>
/// A D latch written as a single primitive with behaviour in code, rather than assembled
/// from gates.
/// </summary>
/// <remarks>
/// <para>Inputs: <c>0</c> = D, <c>1</c> = enable. Outputs: <see cref="Gate.Output"/> = Q,
/// <see cref="OutputQN"/> = Q inverted.</para>
/// <para>
/// It is here as the worked example of extending <see cref="Gate"/> directly: one class, no
/// internal net, state held implicitly by doing nothing when the enable is low. Compare it with
/// <see cref="DLatch"/>, which is the same element built from four NANDs, to see what the gate
/// level costs and what it buys.
/// </para>
/// </remarks>
public sealed class DLatchPrimitive : Gate
{
    /// <summary>Creates the latch.</summary>
    public DLatchPrimitive()
        : base(2)
    {
        OutputQN = new OutputPin();
    }

    /// <summary>The inverted output.</summary>
    public OutputPin OutputQN { get; }

    /// <inheritdoc />
    protected override void Evaluate(GateQueue queue)
    {
        if (GetInputState(1) != LogicState.One)
        {
            return;
        }

        if (GetInputState(0) == LogicState.One)
        {
            Output.SetState(LogicState.One, queue);
            OutputQN.SetState(LogicState.Zero, queue);
        }
        else
        {
            Output.SetState(LogicState.Zero, queue);
            OutputQN.SetState(LogicState.One, queue);
        }
    }
}
