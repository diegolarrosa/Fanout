namespace Fanout;

/// <summary>
/// A reusable block of circuitry, wired from gates and from other modules.
/// </summary>
/// <remarks>
/// A module has no behaviour of its own. It is a boundary: ports on the outside, a graph of
/// gates on the inside, and a <see cref="ModuleLayout"/> that gives the ports names. Everything
/// in <c>Fanout.Combinational</c> and <c>Fanout.Sequential</c> is built this way, out of nothing
/// but the eight primitive gates.
/// </remarks>
public abstract class Module
{
    private readonly List<Port> _inputs = new();
    private readonly List<Port> _outputs = new();

    /// <summary>How many input ports this module has.</summary>
    public int InputCount => _inputs.Count;

    /// <summary>How many output ports this module has.</summary>
    public int OutputCount => _outputs.Count;

    /// <summary>Appends an input port.</summary>
    public virtual void AddInput(Port port) => _inputs.Add(port);

    /// <summary>Appends an output port.</summary>
    public void AddOutput(Port port) => _outputs.Add(port);

    /// <summary>The index of the first port of a named input group.</summary>
    public abstract int InputIndex(string name);

    /// <summary>The index of the first port of a named output group.</summary>
    public abstract int OutputIndex(string name);

    /// <summary>The input port at <paramref name="index"/>.</summary>
    public Port Input(int index) => _inputs[index];

    /// <summary>The first input port of a named group.</summary>
    public Port Input(string name) => _inputs[InputIndex(name)];

    /// <summary>One bit of a named input group.</summary>
    public Port Input(string name, int offset) => _inputs[InputIndex(name) + offset];

    /// <summary>The output port at <paramref name="index"/>.</summary>
    public Port Output(int index) => _outputs[index];

    /// <summary>The first output port of a named group.</summary>
    public Port Output(string name) => _outputs[OutputIndex(name)];

    /// <summary>One bit of a named output group.</summary>
    public Port Output(string name, int offset) => _outputs[OutputIndex(name) + offset];

    /// <summary>Connects one of this module's outputs to an input terminal.</summary>
    public void ConnectTo(int outputIndex, InputPin pin) => Output(outputIndex).ConnectTo(pin);

    /// <summary>Connects one of this module's outputs to an input of a gate.</summary>
    public void ConnectTo(int outputIndex, Gate gate, int inputIndex)
        => Output(outputIndex).ConnectTo(gate, inputIndex);

    /// <summary>Connects one of this module's outputs to an input of another module.</summary>
    public void ConnectTo(int outputIndex, Module module, int inputIndex)
        => Output(outputIndex).ConnectTo(module, inputIndex);

    /// <summary>Connects one of this module's outputs to a named input of another module.</summary>
    public void ConnectTo(int outputIndex, Module module, string inputName)
        => Output(outputIndex).ConnectTo(module, inputName);

    /// <summary>Connects one of this module's outputs to one bit of a named input group.</summary>
    public void ConnectTo(int outputIndex, Module module, string inputName, int inputOffset)
        => Output(outputIndex).ConnectTo(module, inputName, inputOffset);

    /// <summary>Connects a named output of this module to an input terminal.</summary>
    public void ConnectTo(string outputName, InputPin pin) => Output(outputName).ConnectTo(pin);

    /// <summary>Connects one bit of a named output group to an input terminal.</summary>
    public void ConnectTo(string outputName, int outputOffset, InputPin pin)
        => Output(outputName, outputOffset).ConnectTo(pin);

    /// <summary>Connects a named output of this module to an input of a gate.</summary>
    public void ConnectTo(string outputName, Gate gate, int inputIndex)
        => Output(outputName).ConnectTo(gate, inputIndex);

    /// <summary>Connects a named output of this module to a named input of another module.</summary>
    public void ConnectTo(string outputName, Module module, string inputName)
        => Output(outputName).ConnectTo(module, inputName);

    /// <summary>Connects one bit of a named output group to one bit of a named input group.</summary>
    public void ConnectTo(string outputName, int outputOffset, Module module, string inputName, int inputOffset)
        => Output(outputName, outputOffset).ConnectTo(module, inputName, inputOffset);
}
