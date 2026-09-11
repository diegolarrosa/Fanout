namespace Fanout;

/// <summary>
/// A primitive logic element: any number of inputs, exactly one output, no internal structure.
/// </summary>
/// <remarks>
/// <para>
/// A gate is evaluated only once every input it needs is known. "Needs" is the interesting word:
/// the default rule waits for all of them, but a gate with a controlling value can do better.
/// An AND gate with one input already at zero has a known output no matter what the others do,
/// so <see cref="Gates.And"/> overrides <see cref="HasUnknownInputs"/> and evaluates early.
/// That is ordinary constraint propagation, and it is what lets a circuit settle with parts of
/// it still undriven.
/// </para>
/// </remarks>
public abstract class Gate
{
    private readonly List<InputPin> _inputs = new();
    private readonly List<bool> _inverted = new();

    /// <summary>
    /// Set once every input has been seen to be known, so later rounds skip the scan.
    /// Adding an input or changing a polarity clears it.
    /// </summary>
    protected bool AllInputsKnown;

    /// <summary>Creates a gate with <paramref name="inputCount"/> normal-polarity inputs.</summary>
    protected Gate(int inputCount)
    {
        Output = new OutputPin();

        for (int i = 0; i < inputCount; i++)
        {
            AddInput(new InputPin(), PinPolarity.Normal);
        }
    }

    /// <summary>Creates a gate with one input per supplied polarity.</summary>
    protected Gate(params PinPolarity[] polarities)
        : this(0)
    {
        foreach (PinPolarity polarity in polarities)
        {
            AddInput(new InputPin(), polarity);
        }
    }

    /// <summary>This gate's output terminal.</summary>
    public OutputPin Output { get; }

    /// <summary>How many inputs this gate has.</summary>
    public int InputCount => _inputs.Count;

    /// <summary>Appends an input terminal.</summary>
    public void AddInput(InputPin pin, PinPolarity polarity = PinPolarity.Normal)
    {
        pin.Owner = this;
        _inputs.Add(pin);
        _inverted.Add(polarity == PinPolarity.Inverted);
        AllInputsKnown = false;
    }

    /// <summary>Appends a fresh input terminal and returns it.</summary>
    public InputPin AddInput(PinPolarity polarity = PinPolarity.Normal)
    {
        InputPin pin = new();
        AddInput(pin, polarity);
        return pin;
    }

    /// <summary>Changes whether an existing input is inverted.</summary>
    public void SetInputPolarity(int index, PinPolarity polarity)
    {
        _inverted[index] = polarity == PinPolarity.Inverted;
        AllInputsKnown = false;
    }

    /// <summary>The input terminal at <paramref name="index"/>.</summary>
    public InputPin Input(int index) => _inputs[index];

    /// <summary>
    /// Evaluates the gate if its inputs allow it.
    /// </summary>
    /// <returns><c>true</c> if the gate ran; <c>false</c> if it is still waiting on an input.</returns>
    public bool TryEvaluate(GateQueue queue)
    {
        if (HasUnknownInputs())
        {
            return false;
        }

        Evaluate(queue);
        return true;
    }

    /// <summary>
    /// Drives the output directly, without scheduling anything.
    /// </summary>
    /// <remarks>
    /// This exists to break the start-up deadlock of a cross-coupled latch, where neither gate
    /// can resolve because each is waiting on the other. Seeding one of them settles the loop.
    /// It is a construction-time tool, not something to call during a run.
    /// </remarks>
    public void ForceOutput(LogicState state) => Output.SetState(state, null);

    /// <summary>
    /// Gives a value to the first input that is still unknown, leaving the rest alone.
    /// </summary>
    public void ResolveOneInput(LogicState state, GateQueue? queue)
    {
        for (int i = 0; i < _inputs.Count; i++)
        {
            if (_inputs[i].State == LogicState.Unknown)
            {
                _inputs[i].SetState(state, queue);
                return;
            }
        }
    }

    /// <summary>Connects this gate's output to an input terminal.</summary>
    public void ConnectTo(InputPin pin) => Output.ConnectTo(pin);

    /// <summary>Connects this gate's output to an input of another gate.</summary>
    public void ConnectTo(Gate gate, int inputIndex) => Output.ConnectTo(gate.Input(inputIndex));

    /// <summary>Connects this gate's output to an input of a module.</summary>
    public void ConnectTo(Module module, int inputIndex) => Output.ConnectTo(module.Input(inputIndex));

    /// <summary>Connects this gate's output to a named input of a module.</summary>
    public void ConnectTo(Module module, string inputName) => Output.ConnectTo(module.Input(inputName));

    /// <summary>Connects this gate's output to one bit of a named input group of a module.</summary>
    public void ConnectTo(Module module, string inputName, int offset)
        => Output.ConnectTo(module.Input(inputName, offset));

    /// <summary>
    /// The value of input <paramref name="index"/>, with this gate's polarity applied.
    /// </summary>
    protected LogicState GetInputState(int index)
    {
        LogicState state = _inputs[index].State;

        if (!_inverted[index])
        {
            return state;
        }

        return state switch
        {
            LogicState.Zero => LogicState.One,
            LogicState.One => LogicState.Zero,
            _ => LogicState.Unknown,
        };
    }

    /// <summary>
    /// Whether this gate still lacks the information it needs to produce an output.
    /// </summary>
    /// <remarks>
    /// Override this in a gate that has a controlling value, so it can resolve early.
    /// </remarks>
    protected virtual bool HasUnknownInputs()
    {
        if (AllInputsKnown)
        {
            return false;
        }

        for (int i = 0; i < _inputs.Count; i++)
        {
            if (GetInputState(i) == LogicState.Unknown)
            {
                return true;
            }
        }

        AllInputsKnown = true;
        return false;
    }

    /// <summary>Computes the output from the inputs. Called only when the inputs allow it.</summary>
    protected abstract void Evaluate(GateQueue queue);
}
