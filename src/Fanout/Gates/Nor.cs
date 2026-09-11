namespace Fanout.Gates;

/// <summary>NOR: the output is high only when every input is low.</summary>
/// <remarks>One is the controlling value, so one high input settles the output at low.</remarks>
public sealed class Nor : Gate
{
    /// <summary>Creates a NOR gate with <paramref name="inputCount"/> inputs.</summary>
    public Nor(int inputCount = 2)
        : base(inputCount)
    {
    }

    /// <summary>Creates a NOR gate with one input per supplied polarity.</summary>
    public Nor(params PinPolarity[] polarities)
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
