namespace Fanout.Gates;

/// <summary>OR: the output is high when any input is high.</summary>
/// <remarks>One is the controlling value, so one high input settles the output.</remarks>
public sealed class Or : Gate
{
    /// <summary>Creates an OR gate with <paramref name="inputCount"/> inputs.</summary>
    public Or(int inputCount = 2)
        : base(inputCount)
    {
    }

    /// <summary>Creates an OR gate with one input per supplied polarity.</summary>
    public Or(params PinPolarity[] polarities)
        : base(polarities)
    {
    }

    /// <inheritdoc />
    protected override void Evaluate(GateQueue queue)
    {
        for (int i = 0; i < InputCount; i++)
        {
            if (GetInputState(i) == LogicState.One)
            {
                Output.SetState(LogicState.One, queue);
                return;
            }
        }

        Output.SetState(LogicState.Zero, queue);
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

            if (state == LogicState.One)
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
