using System.Collections.Concurrent;
using Fanout.Combinational;
using Fanout.Gates;

namespace Fanout.Sequential;

/// <summary>
/// A register that can either shift serially or load in parallel, chosen by the <c>LOAD</c> input.
/// </summary>
/// <remarks>
/// <para>Inputs: <c>D</c> (n bits, parallel), <c>SIN</c> (serial in), <c>LOAD</c>, <c>CLK</c>,
/// <c>PRE</c>, <c>CLR</c>. Outputs: <c>Q</c> (n bits).</para>
/// <para>
/// <c>LOAD</c> high takes the parallel inputs on the next clock; <c>LOAD</c> low shifts. The
/// selection is a two-input multiplexer per stage, built here from an AND/AND/OR trio rather
/// than by instantiating a <see cref="Multiplexer"/>, so the gate count stays visible.
/// </para>
/// </remarks>
public sealed class ShiftLoadRegister : Module
{
    private static readonly ConcurrentDictionary<int, ModuleLayout> Layouts = new();

    /// <summary>Builds a register <paramref name="width"/> bits wide.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="width"/> is less than one.</exception>
    public ShiftLoadRegister(int width = 2, ClockEdge edge = ClockEdge.Rising)
    {
        if (width < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(width), width, "A register needs at least one bit.");
        }

        Width = width;
        Layout = Layouts.GetOrAdd(width, CreateLayout);

        Port[] outputs = Port.Create(width);
        Port[] data = Port.Create(width);
        Port serialIn = new();
        Port load = new();
        Port clock = new();
        Port preset = new();
        Port clear = new();

        DFlipFlop[] stages = new DFlipFlop[width];
        Or[] combine = new Or[width];
        And[] shiftPath = new And[width];
        And[] loadPath = new And[width];

        for (int i = 0; i < width; i++)
        {
            stages[i] = new DFlipFlop(edge);
            combine[i] = new Or();
            shiftPath[i] = new And();
            loadPath[i] = new And();
        }

        Not invertLoad = new();

        for (int i = 0; i < width; i++)
        {
            AddOutput(outputs[i]);
        }

        for (int i = 0; i < width; i++)
        {
            AddInput(data[i]);
        }

        AddInput(serialIn);
        AddInput(load);
        AddInput(clock);
        AddInput(preset);
        AddInput(clear);

        serialIn.ConnectTo(shiftPath[0], 0);
        load.ConnectTo(invertLoad, 0);

        for (int i = 0; i < width; i++)
        {
            if (i < (width - 1))
            {
                stages[i].ConnectTo(0, shiftPath[i + 1], 0);
            }

            data[i].ConnectTo(loadPath[i], 1);
            stages[i].ConnectTo(0, outputs[i]);
            clock.ConnectTo(stages[i], 1);
            preset.ConnectTo(stages[i], 2);
            clear.ConnectTo(stages[i], 3);
            invertLoad.ConnectTo(shiftPath[i], 1);
            load.ConnectTo(loadPath[i], 0);
            shiftPath[i].ConnectTo(combine[i], 0);
            loadPath[i].ConnectTo(combine[i], 1);
            combine[i].ConnectTo(stages[i], 0);
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
            .AddInputGroup("SIN", width)
            .AddInputGroup("LOAD", width + 1)
            .AddInputGroup("CLK", width + 2)
            .AddInputGroup("PRE", width + 3)
            .AddInputGroup("CLR", width + 4)
            .AddOutputGroup("Q", 0, width);
}
