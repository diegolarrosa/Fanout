namespace Fanout;

/// <summary>
/// Whether a gate input inverts the value it receives, saving an explicit <c>Not</c> gate.
/// </summary>
public enum PinPolarity
{
    /// <summary>The input is used as received.</summary>
    Normal = 0,

    /// <summary>The input is inverted before the gate evaluates it.</summary>
    Inverted = 1,
}
