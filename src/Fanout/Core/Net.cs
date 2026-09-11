namespace Fanout;

/// <summary>
/// The wire between one driver and every input it reaches.
/// </summary>
/// <remarks>
/// A net has exactly one driver (the <see cref="OutputPin"/> that owns it) and any number of
/// sinks. Its length is the whole of a circuit's fan-out cost, which is why it is a bare list
/// and nothing more.
/// </remarks>
public sealed class Net
{
    private readonly List<InputPin> _sinks = new();

    /// <summary>How many inputs this net drives.</summary>
    public int FanOut => _sinks.Count;

    /// <summary>Adds an input to the set this net drives.</summary>
    public void Attach(InputPin pin) => _sinks.Add(pin);

    /// <summary>Pushes a value to every attached input.</summary>
    public void Propagate(LogicState value, GateQueue? queue)
    {
        for (int i = 0; i < _sinks.Count; i++)
        {
            _sinks[i].SetState(value, queue);
        }
    }
}
