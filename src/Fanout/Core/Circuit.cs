namespace Fanout;

/// <summary>
/// The top-level module. It owns the work queue and runs the simulation to a stable state.
/// </summary>
/// <remarks>
/// <para>
/// The algorithm is event-driven and has exactly one moving part. Driving an input port puts
/// the gates it reaches on a pending queue. <see cref="Run"/> then repeats one step until the
/// queue is empty: take the whole current round, evaluate each gate, and collect whatever
/// changed into the next round. A gate whose inputs are still unknown is simply dropped — it
/// will come back when one of its inputs moves.
/// </para>
/// <para>
/// The loop terminates because a gate is only queued when a value actually changed. A circuit
/// with no stable state — a ring oscillator, say — never stops changing, and is reported as
/// <see cref="CircuitOscillationException"/> rather than hanging.
/// </para>
/// </remarks>
public sealed class Circuit : Module
{
    private readonly GateQueue _pending = new();
    private readonly GateQueue _bufferA = new();
    private readonly GateQueue _bufferB = new();

    /// <summary>
    /// How many propagation rounds <see cref="Run"/> will do before deciding the circuit does
    /// not settle. Raise it for very deep circuits.
    /// </summary>
    public int MaxRounds { get; set; } = 100_000;

    /// <summary>
    /// How many rounds the last <see cref="Run"/> needed.
    /// </summary>
    /// <remarks>
    /// This is a cost signal, <b>not</b> a measure of logic depth. The order in which gates are
    /// evaluated within a round is unspecified, and a gate evaluated later in a round already sees
    /// the values written earlier in that same round — so a favourable order can collapse an
    /// entire carry chain into one round and an unfavourable one cannot. The settled result never
    /// depends on that order; this number does.
    /// </remarks>
    public int LastRunRounds { get; private set; }

    /// <inheritdoc />
    /// <remarks>Also binds the port to this circuit's queue, so driving it schedules work here.</remarks>
    public override void AddInput(Port port)
    {
        port.DefaultQueue = _pending;
        base.AddInput(port);
    }

    /// <summary>
    /// Runs propagation until nothing changes.
    /// </summary>
    /// <returns>The number of rounds it took.</returns>
    /// <exception cref="CircuitOscillationException">The circuit did not settle within <see cref="MaxRounds"/>.</exception>
    public int Run()
    {
        GateQueue current = _bufferA;
        GateQueue next = _bufferB;

        current.Clear();
        next.Clear();
        current.UnionWith(_pending);
        _pending.Clear();

        int rounds = 0;

        while (current.Count > 0)
        {
            if (++rounds > MaxRounds)
            {
                throw new CircuitOscillationException(
                    $"The circuit did not settle after {MaxRounds} propagation rounds. "
                    + "This usually means a combinational loop with an odd number of inversions. "
                    + $"Raise {nameof(MaxRounds)} if the circuit is simply very deep.");
            }

            next.Clear();

            foreach (Gate gate in current)
            {
                gate.TryEvaluate(next);
            }

            (current, next) = (next, current);
        }

        LastRunRounds = rounds;
        return rounds;
    }

    /// <summary>
    /// Adds an input port and returns it, which is the common case at the top level.
    /// </summary>
    public Port AddInput()
    {
        Port port = new();
        AddInput(port);
        return port;
    }

    /// <summary>Adds an output port and returns it.</summary>
    public Port AddOutput()
    {
        Port port = new();
        AddOutput(port);
        return port;
    }

    /// <inheritdoc />
    /// <exception cref="NotSupportedException">A circuit has no named port layout.</exception>
    public override int InputIndex(string name)
        => throw new NotSupportedException("A Circuit has no named port layout; address its ports by index.");

    /// <inheritdoc />
    /// <exception cref="NotSupportedException">A circuit has no named port layout.</exception>
    public override int OutputIndex(string name)
        => throw new NotSupportedException("A Circuit has no named port layout; address its ports by index.");
}
