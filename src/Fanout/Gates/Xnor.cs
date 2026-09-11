namespace Fanout.Gates;

/// <summary>XNOR: the output is high when an even number of inputs are high.</summary>
public sealed class Xnor : Gate
{
    /// <summary>Creates an XNOR gate with <paramref name="inputCount"/> inputs.</summary>
    public Xnor(int inputCount = 2)
        : base(inputCount)
    {
    }

    /// <summary>Creates an XNOR gate with one input per supplied polarity.</summary>
    public Xnor(params PinPolarity[] polarities)
        : base(polarities)
    {
    }

    /// <inheritdoc />
    protected override void Evaluate(GateQueue queue)
    {
        int ones = 0;

        for (int i = 0; i < InputCount; i++)
        {
            if (GetInputState(i) == LogicState.One)
            {
                ones++;
            }
        }

        Output.SetState((ones % 2) == 0 ? LogicState.One : LogicState.Zero, queue);
    }
}
