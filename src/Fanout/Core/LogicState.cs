namespace Fanout;

/// <summary>
/// The three-valued logic level carried by a wire.
/// </summary>
/// <remarks>
/// <see cref="Unknown"/> is not a fourth state bolted on for convenience: it is what the
/// simulator uses to decide whether a gate can be evaluated at all. A gate whose inputs are
/// still unknown is skipped and re-queued when one of them changes.
/// </remarks>
public enum LogicState
{
    /// <summary>Logic low.</summary>
    Zero = 0,

    /// <summary>Logic high.</summary>
    One = 1,

    /// <summary>Not yet driven by anything.</summary>
    Unknown = 2,
}
