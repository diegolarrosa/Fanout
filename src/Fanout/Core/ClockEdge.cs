namespace Fanout;

/// <summary>Which clock transition an edge-triggered element responds to.</summary>
public enum ClockEdge
{
    /// <summary>Triggered when the clock goes from <see cref="LogicState.One"/> to <see cref="LogicState.Zero"/>.</summary>
    Falling = 0,

    /// <summary>Triggered when the clock goes from <see cref="LogicState.Zero"/> to <see cref="LogicState.One"/>.</summary>
    Rising = 1,
}
