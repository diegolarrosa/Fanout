namespace Fanout;

/// <summary>
/// The single output terminal of a <see cref="Gate"/>, together with the net it drives.
/// </summary>
public sealed class OutputPin
{
    private readonly Net _net = new();

    /// <summary>The value this output is currently driving.</summary>
    public LogicState State { get; private set; } = LogicState.Unknown;

    /// <summary>The net driven by this output.</summary>
    public Net Net => _net;

    /// <summary>Connects this output to an input, so the input follows it.</summary>
    public void ConnectTo(InputPin pin) => _net.Attach(pin);

    /// <summary>
    /// Drives a new value. If the value did not change, nothing propagates and nothing is queued.
    /// </summary>
    public void SetState(LogicState value, GateQueue? queue)
    {
        if (State == value)
        {
            return;
        }

        State = value;
        _net.Propagate(value, queue);
    }
}
