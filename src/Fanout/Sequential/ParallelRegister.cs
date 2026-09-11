using System.Collections.Concurrent;

namespace Fanout.Sequential;

/// <summary>
/// A parallel-in, parallel-out register: one D flip-flop per bit, all sharing a clock.
/// </summary>
/// <remarks>
/// <para>Inputs: <c>D</c> (n bits), <c>CLK</c>, <c>PRE</c>, <c>CLR</c>. Outputs: <c>Q</c> (n bits).</para>
/// <para>Pulse <c>CLR</c> low once before clocking so every bit starts at zero.</para>
/// </remarks>
public sealed class ParallelRegister : Module
{
    private static readonly ConcurrentDictionary<int, ModuleLayout> Layouts = new();

    /// <summary>Builds a register <paramref name="width"/> bits wide.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="width"/> is less than one.</exception>
    public ParallelRegister(int width = 2, ClockEdge edge = ClockEdge.Rising)
    {
        if (width < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(width), width, "A register needs at least one bit.");
        }

        Width = width;
        Layout = Layouts.GetOrAdd(width, CreateLayout);

        Port[] outputs = Port.Create(width);
        Port[] data = Port.Create(width);
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

        for (int i = 0; i < width; i++)
        {
            AddInput(data[i]);
        }

        AddInput(clock);
        AddInput(preset);
        AddInput(clear);

        for (int i = 0; i < width; i++)
        {
            data[i].ConnectTo(stages[i], 0);
            stages[i].ConnectTo(0, outputs[i]);
            clock.ConnectTo(stages[i], 1);
            preset.ConnectTo(stages[i], 2);
            clear.ConnectTo(stages[i], 3);
        }
    }

    /// <summary>How many bits wide the register is.</summary>
    public int Width { get; }

    /// <summary>This width's port layout.</summary>
    public ModuleLayout Layout { get; }

    /// <inheritdoc />
    public override int InputIndex(string name) => Layout.InputIndex(name);

    /// <inheritdoc />
    public override int OutputIndex(string name) => Layout.OutputIndex(name);

    private static ModuleLayout CreateLayout(int width)
        => new ModuleLayout()
            .AddInputGroup("D", 0, width)
            .AddInputGroup("CLK", width)
            .AddInputGroup("PRE", width + 1)
            .AddInputGroup("CLR", width + 2)
            .AddOutputGroup("Q", 0, width);
}
