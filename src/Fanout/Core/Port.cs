namespace Fanout;

/// <summary>
/// A pin on a module's boundary: an input that immediately re-drives an output of its own.
/// </summary>
/// <remarks>
/// A port is how a module can be wired up from outside without exposing the gates inside it.
/// The same type serves as an input port and an output port — the difference is only which side
/// of the module it was added to.
/// </remarks>
public sealed class Port : InputPin
{
    /// <summary>The output side of the port, which mirrors whatever is driven in.</summary>
    public OutputPin Output { get; } = new();

    /// <summary>
    /// The queue this port schedules into when driven through <see cref="SetState(LogicState)"/>.
    /// Set by <see cref="Circuit.AddInput(Port)"/>; <c>null</c> on ports inside a submodule.
    /// </summary>
    internal GateQueue? DefaultQueue { get; set; }

    /// <inheritdoc />
    public override void SetState(LogicState value, GateQueue? queue)
    {
        if (State == value)
        {
            return;
        }

        State = value;
        Output.SetState(value, queue);
    }

    /// <summary>
    /// Drives the port, scheduling into the circuit that owns it.
    /// </summary>
    /// <remarks>
    /// This is the normal way to stimulate a circuit from a test or a program: set the inputs,
    /// then call <see cref="Circuit.Run"/>.
    /// </remarks>
    public void SetState(LogicState value) => SetState(value, DefaultQueue);

    /// <summary>
    /// Creates <paramref name="count"/> fresh ports, which is what a bus of any width needs.
    /// </summary>
    public static Port[] Create(int count)
    {
        Port[] ports = new Port[count];

        for (int i = 0; i < count; i++)
        {
            ports[i] = new Port();
        }

        return ports;
    }

    /// <summary>Connects this port's output to an input terminal.</summary>
    public void ConnectTo(InputPin pin) => Output.ConnectTo(pin);

    /// <summary>Connects this port's output to an input of a gate.</summary>
    public void ConnectTo(Gate gate, int inputIndex) => Output.ConnectTo(gate.Input(inputIndex));

    /// <summary>Connects this port's output to an input of a module.</summary>
    public void ConnectTo(Module module, int inputIndex) => Output.ConnectTo(module.Input(inputIndex));

    /// <summary>Connects this port's output to a named input of a module.</summary>
    public void ConnectTo(Module module, string inputName) => Output.ConnectTo(module.Input(inputName));

    /// <summary>Connects this port's output to one bit of a named input group of a module.</summary>
    public void ConnectTo(Module module, string inputName, int offset)
        => Output.ConnectTo(module.Input(inputName, offset));
}
