namespace Fanout;

/// <summary>
/// One input terminal of a <see cref="Gate"/>.
/// </summary>
/// <remarks>
/// A pin is the point where propagation turns into scheduling: when its value actually changes,
/// it puts the gate that owns it on the work queue. A write that does not change the value is
/// dropped here, which is what stops the simulation from cycling forever on stable nets.
/// </remarks>
public class InputPin
{
    /// <summary>The value currently driven onto this pin.</summary>
    public LogicState State { get; protected set; } = LogicState.Unknown;

    /// <summary>The gate this pin feeds, or <c>null</c> for a module boundary port.</summary>
    public Gate? Owner { get; internal set; }

    /// <summary>
    /// Drives a new value onto the pin and, if the value changed, queues the owning gate
    /// for re-evaluation.
    /// </summary>
    /// <param name="value">The value to drive.</param>
    /// <param name="queue">
    /// The work queue to schedule into, or <c>null</c> to change the value without scheduling
    /// anything. Passing <c>null</c> is how a circuit is seeded before the first run.
    /// </param>
    public virtual void SetState(LogicState value, GateQueue? queue)
    {
        if (State == value)
        {
            return;
        }

        State = value;

        if (queue is not null && Owner is not null)
        {
            queue.Enqueue(Owner);
        }
    }
}
