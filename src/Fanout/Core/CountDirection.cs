namespace Fanout;

/// <summary>Which way a ripple counter counts.</summary>
public enum CountDirection
{
    /// <summary>Counts 0, 1, 2, 3, ...</summary>
    Up = 0,

    /// <summary>Counts 0, -1, -2, -3, ... (that is, downwards, wrapping at zero).</summary>
    Down = 1,
}
