
namespace Fanout.Tests;

/// <summary>
/// Small helpers for wiring a module up to a circuit, driving it, and reading it back as integers.
/// </summary>
internal static class Bench
{
    public static LogicState Bit(int value) => value == 0 ? LogicState.Zero : LogicState.One;

    public static int Bit(LogicState state) => state switch
    {
        LogicState.Zero => 0,
        LogicState.One => 1,
        _ => throw new InvalidOperationException("Signal is still Unknown."),
    };

    /// <summary>Adds a top-level input port and wires it to a named input of the module.</summary>
    public static Port Drive(Circuit circuit, Module target, string inputName)
    {
        Port port = circuit.AddInput();
        port.ConnectTo(target, inputName);
        return port;
    }

    /// <summary>Adds a top-level input port per bit and wires them to a named input group.</summary>
    public static Port[] DriveBus(Circuit circuit, Module target, string inputName, int width)
    {
        Port[] ports = new Port[width];

        for (int i = 0; i < width; i++)
        {
            ports[i] = circuit.AddInput();
            ports[i].ConnectTo(target, inputName, i);
        }

        return ports;
    }

    /// <summary>Drives a bus with the binary representation of <paramref name="value"/>, LSB first.</summary>
    public static void SetBus(Port[] ports, int value)
    {
        for (int i = 0; i < ports.Length; i++)
        {
            ports[i].SetState(Bit((value >> i) & 1));
        }
    }

    /// <summary>Reads a named output group of a module back as an integer, LSB first.</summary>
    public static int ReadBus(Module target, string outputName, int width)
    {
        int value = 0;

        for (int i = 0; i < width; i++)
        {
            LogicState state = target.Output(outputName, i).State;

            if (state == LogicState.Unknown)
            {
                throw new InvalidOperationException($"Output {outputName}[{i}] is still Unknown.");
            }

            if (state == LogicState.One)
            {
                value |= 1 << i;
            }
        }

        return value;
    }

    /// <summary>Reads a single named output of a module.</summary>
    public static LogicState Read(Module target, string outputName) => target.Output(outputName).State;

    /// <summary>Takes a clock line low then high, settling the circuit after each transition.</summary>
    public static void RisingPulse(Circuit circuit, Port clock)
    {
        clock.SetState(LogicState.Zero);
        circuit.Run();
        clock.SetState(LogicState.One);
        circuit.Run();
    }

    /// <summary>Takes a clock line high then low, settling the circuit after each transition.</summary>
    public static void FallingPulse(Circuit circuit, Port clock)
    {
        clock.SetState(LogicState.One);
        circuit.Run();
        clock.SetState(LogicState.Zero);
        circuit.Run();
    }

    /// <summary>Pulses an active-low asynchronous clear and returns it to its inactive level.</summary>
    public static void AssertClear(Circuit circuit, Port clear)
    {
        clear.SetState(LogicState.Zero);
        circuit.Run();
        clear.SetState(LogicState.One);
        circuit.Run();
    }
}
