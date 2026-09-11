namespace Fanout;

/// <summary>
/// Thrown when a circuit never reaches a stable state.
/// </summary>
/// <remarks>
/// The usual cause is a real oscillator — an odd number of inverters in a loop — in which case
/// there is no stable state to reach and the exception is the correct answer.
/// </remarks>
public sealed class CircuitOscillationException : Exception
{
    /// <summary>Creates the exception with a message.</summary>
    public CircuitOscillationException(string message)
        : base(message)
    {
    }

    /// <summary>Creates the exception with a message and an inner exception.</summary>
    public CircuitOscillationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
