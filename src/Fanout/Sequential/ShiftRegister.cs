using System.Collections.Concurrent;

namespace Fanout.Sequential;

/// <summary>
/// A serial-in, parallel-out shift register: a chain of D flip-flops, each feeding the next.
/// </summary>
/// <remarks>
/// <para>Inputs: <c>D</c> (serial in), <c>CLK</c>, <c>PRE</c>, <c>CLR</c>.
/// Outputs: <c>Q</c> (n bits; <c>Q[0]</c> is the stage nearest the input).</para>
/// <para>Pulse <c>CLR</c> low once before clocking so every stage starts at zero.</para>
/// </remarks>
public sealed class ShiftRegister : Module
{
    private static readonly ConcurrentDictionary<int, ModuleLayout> Layouts = new();

    /// <summary>Builds a shift register with <paramref name="width"/> stages.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="width"/> is less than one.</exception>
    public ShiftRegister(int width = 2, ClockEdge edge = ClockEdge.Rising)
    {
        if (width < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(width), width, "A shift register needs at least one stage.");
        }

        Width = width;
        Layout = Layouts.GetOrAdd(width, CreateLayout);

        Port[] outputs = Port.Create(width);
        Port serialIn = new();
        Port clock = new();
        Port preset = new();
        Port clear = new();

        DFlipFlop[] stages = new DFlipFlop[width];

        for (int i = 0; i < width; i++)
        {
            stages[i] = new DFlipFlop(edge);
        }

        for (int i = 0; i < width; i++)
        {
            AddOutput(outputs[i]);
        }

        AddInput(serialIn);
        AddInput(clock);
        AddInput(preset);
        AddInput(clear);

        serialIn.ConnectTo(stages[0], 0);

        for (int i = 0; i < width; i++)
        {
            if (i < (width - 1))
            {
                stages[i].ConnectTo(0, stages[i + 1], 0);
            }

            stages[i].ConnectTo(0, outputs[i]);
            clock.ConnectTo(stages[i], 1);
            preset.ConnectTo(stages[i], 2);
            clear.ConnectTo(stages[i], 3);
        }
    }

    /// <summary>How many stages the register has.</summary>
    public int Width { get; }

    /// <summary>This width's port layout.</summary>
    public ModuleLayout Layout { get; }

    /// <inheritdoc />
    public override int InputIndex(string name) => Layout.InputIndex(name);

    /// <inheritdoc />
    public override int OutputIndex(string name) => Layout.OutputIndex(name);

    private static ModuleLayout CreateLayout(int width)
        => new ModuleLayout()
            .AddInputGroup("D", 0)
            .AddInputGroup("CLK", 1)
            .AddInputGroup("PRE", 2)
            .AddInputGroup("CLR", 3)
            .AddOutputGroup("Q", 0, width);
}
