using System.Collections.Concurrent;
using Fanout.Gates;

namespace Fanout.Combinational;

/// <summary>
/// An n-bit ripple-carry adder: a chain of <see cref="FullAdder"/> cells, carry out to carry in.
/// </summary>
/// <remarks>
/// <para>Ports, in order: <c>A</c> (n bits, LSB first), <c>B</c> (n bits), <c>CIN</c>.
/// Outputs: <c>SUM</c> (n bits), <c>COUT</c>.</para>
/// <para>
/// The carry chain is the whole story of this adder: the top bit cannot settle until the carry
/// has walked every stage, so propagation depth grows linearly with width. That shows up
/// directly in <see cref="Circuit.LastRunRounds"/>.
/// </para>
/// </remarks>
public sealed class RippleCarryAdder : Module
{
    private static readonly ConcurrentDictionary<int, ModuleLayout> Layouts = new();

    /// <summary>Builds an adder <paramref name="width"/> bits wide.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="width"/> is less than one.</exception>
    public RippleCarryAdder(int width = 2)
    {
        if (width < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(width), width, "An adder needs at least one bit.");
        }

        Width = width;
        Layout = Layouts.GetOrAdd(width, CreateLayout);

        Port[] a = Port.Create(width);
        Port[] b = Port.Create(width);
        Port[] sum = Port.Create(width);
        Port carryIn = new();
        Port carryOut = new();

        FullAdder[] cells = new FullAdder[width];

        for (int i = 0; i < width; i++)
        {
            cells[i] = new FullAdder();
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

        AddInput(carryIn);
        AddOutput(carryOut);

        carryIn.ConnectTo(cells[0], 2);

        for (int i = 0; i < width; i++)
        {
            a[i].ConnectTo(cells[i], 0);
            b[i].ConnectTo(cells[i], 1);
            cells[i].ConnectTo(0, sum[i]);

            if (i >= 1)
            {
                cells[i - 1].ConnectTo(1, cells[i], 2);
            }
        }

        cells[width - 1].ConnectTo(1, carryOut);
    }

    /// <summary>How many bits wide the adder is.</summary>
    public int Width { get; }

    /// <summary>This width's port layout.</summary>
    public ModuleLayout Layout { get; }

    /// <summary>Index of the first <c>A</c> input.</summary>
    public int IndexA => 0;

    /// <summary>Index of the first <c>B</c> input.</summary>
    public int IndexB => Width;

    /// <summary>Index of the <c>CIN</c> input.</summary>
    public int IndexCarryIn => Width * 2;

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
            .AddInputGroup("CIN", width * 2)
            .AddOutputGroup("SUM", 0, width)
            .AddOutputGroup("COUT", width);
}
