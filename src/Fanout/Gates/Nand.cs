namespace Fanout.Gates;

/// <summary>NAND: the output is low only when every input is high.</summary>
/// <remarks>Zero is the controlling value, so one low input settles the output at high.</remarks>
public sealed class Nand : Gate
{
    /// <summary>Creates a NAND gate with <paramref name="inputCount"/> inputs.</summary>
    public Nand(int inputCount = 2)
        : base(inputCount)
    {
    }

    /// <summary>Creates a NAND gate with one input per supplied polarity.</summary>
    public Nand(params PinPolarity[] polarities)
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
