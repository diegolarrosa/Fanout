namespace Fanout.Gates;

/// <summary>AND: the output is high only when every input is high.</summary>
/// <remarks>
/// Zero is AND's controlling value, so a single low input settles the output no matter what
/// the others are doing. This gate evaluates as soon as it sees one.
/// </remarks>
public sealed class And : Gate
{
    /// <summary>Creates an AND gate with <paramref name="inputCount"/> inputs.</summary>
    public And(int inputCount = 2)
        : base(inputCount)
    {
    }

    /// <summary>Creates an AND gate with one input per supplied polarity.</summary>
    public And(params PinPolarity[] polarities)
        : base(polarities)
    {
    }

    /// <inheritdoc />
    protected override void Evaluate(GateQueue queue)
    {
        for (int i = 0; i < InputCount; i++)
        {
            if (GetInputState(i) == LogicState.Zero)
            {
                Output.SetState(LogicState.Zero, queue);
                return;
            }
        }

        Output.SetState(LogicState.One, queue);
    }

    /// <inheritdoc />
    protected override bool HasUnknownInputs()
    {
        if (AllInputsKnown)
        {
            return false;
        }

        bool sawUnknown = false;

        for (int i = 0; i < InputCount; i++)
        {
            LogicState state = GetInputState(i);

            if (state == LogicState.Zero)
            {
                return false;
            }

            if (state == LogicState.Unknown)
            {
                sawUnknown = true;
            }
        }

        if (sawUnknown)
        {
            return true;
        }

        AllInputsKnown = true;
        return false;
    }
}
