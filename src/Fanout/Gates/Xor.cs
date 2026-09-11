namespace Fanout.Gates;

/// <summary>XOR: the output is high when an odd number of inputs are high.</summary>
/// <remarks>
/// XOR has no controlling value — every input matters — so it waits for all of them. That is
/// exactly why a chain of XORs is the expensive part of a ripple-carry adder's propagation.
/// </remarks>
public sealed class Xor : Gate
{
    /// <summary>Creates an XOR gate with <paramref name="inputCount"/> inputs.</summary>
    public Xor(int inputCount = 2)
        : base(inputCount)
    {
    }

    /// <summary>Creates an XOR gate with one input per supplied polarity.</summary>
    public Xor(params PinPolarity[] polarities)
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

        Output.SetState((ones % 2) == 0 ? LogicState.Zero : LogicState.One, queue);
    }
}
