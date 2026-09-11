using System.Collections.Concurrent;

namespace Fanout.Sequential;

/// <summary>
/// An asynchronous (ripple) binary counter: a chain of toggle flip-flops, each clocking the next.
/// </summary>
/// <remarks>
/// <para>Inputs: <c>CLK</c>, <c>PRE</c>, <c>CLR</c>. Outputs: <c>Q</c> (n bits, <c>Q[0]</c> the least
/// significant). Pulse <c>CLR</c> low once before counting.</para>
/// <para>
/// Which output feeds the next stage is not a preference: for the chain to advance, a stage must
/// hand the next one a triggering transition exactly when it returns to zero. With rising-edge
/// toggling that means chaining <c>QN</c> to count up; with falling-edge toggling it means
/// chaining <c>Q</c>. <see cref="CountDirection"/> and <see cref="ClockEdge"/> together decide it,
/// so the caller never has to.
/// </para>
/// <para>
/// This is a ripple counter, so the stages do not change together — the top bit settles only
/// after the carry has walked the chain. <see cref="Circuit.Run"/> always reports the settled
/// value, which means the transient states a real counter glitches through are not visible here.
/// </para>
/// </remarks>
public sealed class RippleCounter : Module
{
    private static readonly ConcurrentDictionary<int, ModuleLayout> Layouts = new();

    /// <summary>Builds a counter with <paramref name="width"/> bits.</summary>
    /// <param name="width">How many bits the counter holds.</param>
    /// <param name="edge">Which clock transition advances it.</param>
    /// <param name="direction">Whether it counts up or down.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="width"/> is less than one.</exception>
    public RippleCounter(
        int width = 2,
        ClockEdge edge = ClockEdge.Rising,
        CountDirection direction = CountDirection.Up)
    {
        if (width < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(width), width, "A counter needs at least one bit.");
        }

        Width = width;
        Edge = edge;
        Direction = direction;
        Layout = Layouts.GetOrAdd(width, CreateLayout);

        bool chainInverted = (direction == CountDirection.Up) == (edge == ClockEdge.Rising);
        int chainOutput = chainInverted ? 1 : 0;

        Port[] outputs = Port.Create(width);
        Port clock = new();
        Port preset = new();
        Port clear = new();

        TFlipFlop[] stages = new TFlipFlop[width];

        for (int i = 0; i < width; i++)
        {
            stages[i] = new TFlipFlop(edge);
        }

        for (int i = 0; i < width; i++)
        {
            AddOutput(outputs[i]);
        }

        AddInput(clock);
        AddInput(preset);
        AddInput(clear);

        clock.ConnectTo(stages[0], 0);

        for (int i = 0; i < (width - 1); i++)
        {
            stages[i].ConnectTo(chainOutput, stages[i + 1], 0);
        }

        for (int i = 0; i < width; i++)
        {
            stages[i].ConnectTo(0, outputs[i]);
            preset.ConnectTo(stages[i], 1);
            clear.ConnectTo(stages[i], 2);
        }
    }

    /// <summary>How many bits the counter holds.</summary>
    public int Width { get; }

    /// <summary>Which clock transition advances the counter.</summary>
    public ClockEdge Edge { get; }

    /// <summary>Whether the counter counts up or down.</summary>
    public CountDirection Direction { get; }

    /// <summary>This width's port layout.</summary>
    public ModuleLayout Layout { get; }

    /// <inheritdoc />
    public override int InputIndex(string name) => Layout.InputIndex(name);

    /// <inheritdoc />
    public override int OutputIndex(string name) => Layout.OutputIndex(name);

    private static ModuleLayout CreateLayout(int width)
        => new ModuleLayout()
            .AddInputGroup("CLK", 0)
            .AddInputGroup("PRE", 1)
            .AddInputGroup("CLR", 2)
            .AddOutputGroup("Q", 0, width);
}
