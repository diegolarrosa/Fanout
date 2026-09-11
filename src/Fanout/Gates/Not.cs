namespace Fanout.Gates;

/// <summary>NOT: a single-input inverter.</summary>
public sealed class Not : Gate
{
    /// <summary>Creates an inverter.</summary>
    public Not()
        : base(1)
    {
    }

    /// <inheritdoc />
    protected override void Evaluate(GateQueue queue)
        => Output.SetState(
            GetInputState(0) == LogicState.Zero ? LogicState.One : LogicState.Zero,
            queue);
}
