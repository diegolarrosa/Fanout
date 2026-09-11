namespace Fanout.Gates;

/// <summary>
/// A non-inverting buffer: the output follows the input.
/// </summary>
/// <remarks>
/// Logically it does nothing, which is the point — it exists to give a signal a named driver
/// and its own fan-out, the way a real buffer does.
/// </remarks>
public sealed class BufferGate : Gate
{
    /// <summary>Creates a buffer.</summary>
    public BufferGate()
        : base(1)
    {
    }

    /// <inheritdoc />
    protected override void Evaluate(GateQueue queue)
        => Output.SetState(
            GetInputState(0) == LogicState.Zero ? LogicState.Zero : LogicState.One,
            queue);
}
