using System.Collections.Concurrent;
using Fanout.Gates;

namespace Fanout.Combinational;

/// <summary>
/// An n-bit adder/subtractor: <c>SUB = 0</c> computes <c>A + B</c>, <c>SUB = 1</c> computes <c>A - B</c>.
/// </summary>
/// <remarks>
/// <para>Ports, in order: <c>A</c> (n bits, LSB first), <c>B</c> (n bits), <c>SUB</c>.
/// Outputs: <c>SUM</c> (n bits), <c>COUT</c>.</para>
/// <para>
/// One XOR per bit and a carry-in, which is the whole trick: with <c>SUB</c> high, every
/// <c>B</c> bit is inverted and a one is injected at the bottom, so the adder sees the two's
/// complement of <c>B</c> and no separate subtractor is needed.
/// </para>
/// </remarks>
public sealed class AddSubtractor : Module
{
    private static readonly ConcurrentDictionary<int, ModuleLayout> Layouts = new();

    /// <summary>Builds an adder/subtractor <paramref name="width"/> bits wide.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="width"/> is less than one.</exception>
    public AddSubtractor(int width = 2)
    {
        if (width < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(width), width, "An adder/subtractor needs at least one bit.");
        }

        Width = width;
        Layout = Layouts.GetOrAdd(width, CreateLayout);

        Port[] a = Port.Create(width);
        Port[] b = Port.Create(width);
        Port[] sum = Port.Create(width);
        Port subtract = new();
        Port carryOut = new();

        FullAdder[] cells = new FullAdder[width];
        Xor[] invert = new Xor[width];

        for (int i = 0; i < width; i++)
        {
            cells[i] = new FullAdder();
            invert[i] = new Xor();
        }

        for (int i = 0; i < width; i++)
        {
            AddInput(a[i]);
            AddOutput(sum[i]);
        }

        for (int i = 0; i < width; i++)
        {
            AddInput(b[i]);
        }

        AddInput(subtract);
        AddOutput(carryOut);

        subtract.ConnectTo(cells[0], 2);

        for (int i = 0; i < width; i++)
        {
            a[i].ConnectTo(cells[i], 0);
            b[i].ConnectTo(invert[i], 0);
            invert[i].ConnectTo(cells[i], 1);
            subtract.ConnectTo(invert[i], 1);
            cells[i].ConnectTo(0, sum[i]);

            if (i >= 1)
            {
                cells[i - 1].ConnectTo(1, cells[i], 2);
            }
        }

        cells[width - 1].ConnectTo(1, carryOut);
    }

    /// <summary>How many bits wide the block is.</summary>
    public int Width { get; }

    /// <summary>This width's port layout.</summary>
    public ModuleLayout Layout { get; }

    /// <summary>Index of the first <c>A</c> input.</summary>
    public int IndexA => 0;

    /// <summary>Index of the first <c>B</c> input.</summary>
    public int IndexB => Width;

    /// <summary>Index of the <c>SUB</c> control input.</summary>
    public int IndexSubtract => Width * 2;

    /// <summary>Index of the first <c>SUM</c> output.</summary>
    public int IndexSum => 0;

    /// <summary>Index of the <c>COUT</c> output.</summary>
    public int IndexCarryOut => Width;

    /// <inheritdoc />
    public override int InputIndex(string name) => Layout.InputIndex(name);

    /// <inheritdoc />
    public override int OutputIndex(string name) => Layout.OutputIndex(name);

    private static ModuleLayout CreateLayout(int width)
        => new ModuleLayout()
            .AddInputGroup("A", 0, width)
            .AddInputGroup("B", width, width)
            .AddInputGroup("SUB", width * 2)
            .AddOutputGroup("SUM", 0, width)
            .AddOutputGroup("COUT", width);
}
