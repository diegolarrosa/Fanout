using System.Collections.Concurrent;
using Fanout.Gates;

namespace Fanout.Combinational;

/// <summary>
/// A multiplexer: the select lines choose which data input reaches the single output.
/// </summary>
/// <remarks>
/// <para>Ports, in order: <c>SEL</c> (ceil(log2 n) bits, LSB first), <c>IN</c> (n bits).
/// Output: <c>OUT</c>.</para>
/// <para>
/// Built the direct way — one AND per data input, decoding the select lines, feeding a wide OR.
/// </para>
/// </remarks>
public sealed class Multiplexer : Module
{
    private static readonly ConcurrentDictionary<int, ModuleLayout> Layouts = new();

    /// <summary>Builds a multiplexer with <paramref name="inputCount"/> data inputs.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="inputCount"/> is below two or above 2^31.</exception>
    public Multiplexer(int inputCount = 2)
    {
        int selectBits = SelectBitsFor(inputCount, "multiplexer");

        DataInputCount = inputCount;
        SelectCount = selectBits;
        Layout = Layouts.GetOrAdd(inputCount, _ => CreateLayout(inputCount, selectBits));

        Port[] select = Port.Create(selectBits);
        Port[] data = Port.Create(inputCount);
        Port output = new();

        Not[] invert = new Not[selectBits];

        for (int i = 0; i < selectBits; i++)
        {
            invert[i] = new Not();
        }

        And[] decode = new And[inputCount];

        for (int i = 0; i < inputCount; i++)
        {
            decode[i] = new And(selectBits + 1);
        }

        Or combine = new(inputCount);

        for (int i = 0; i < selectBits; i++)
        {
            AddInput(select[i]);
            select[i].ConnectTo(invert[i], 0);
        }

        for (int i = 0; i < inputCount; i++)
        {
            AddInput(data[i]);
        }

        AddOutput(output);

        for (int bit = 0; bit < selectBits; bit++)
        {
            for (int block = 0; block < inputCount; block += 2 << bit)
            {
                for (int offset = 0; offset < (1 << bit); offset++)
                {
                    if ((block + offset + (1 << bit)) < inputCount)
                    {
                        select[bit].ConnectTo(decode[block + offset + (1 << bit)], bit);
                    }

                    if ((block + offset) < inputCount)
                    {
                        invert[bit].ConnectTo(decode[block + offset], bit);
                    }
                }
            }
        }

        for (int i = 0; i < inputCount; i++)
        {
            data[i].ConnectTo(decode[i], selectBits);
            decode[i].ConnectTo(combine, i);
        }

        combine.ConnectTo(output);
    }

    /// <summary>How many data inputs the multiplexer has.</summary>
    public int DataInputCount { get; }

    /// <summary>How many select lines it takes to address them.</summary>
    public int SelectCount { get; }

    /// <summary>This size's port layout.</summary>
    public ModuleLayout Layout { get; }

    /// <summary>Index of the first <c>SEL</c> input.</summary>
    public int IndexSelect => 0;

    /// <summary>Index of the first <c>IN</c> input.</summary>
    public int IndexData => SelectCount;

    /// <summary>Index of the <c>OUT</c> output.</summary>
    public int IndexOutput => 0;

    /// <inheritdoc />
    public override int InputIndex(string name) => Layout.InputIndex(name);

    /// <inheritdoc />
    public override int OutputIndex(string name) => Layout.OutputIndex(name);

    internal static int SelectBitsFor(int count, string what)
    {
        int bits = 0;

        while (bits < 32 && (1 << bits) < count)
        {
            bits++;
        }

        if (bits == 0 || bits == 32)
        {
            throw new ArgumentOutOfRangeException(
                nameof(count), count, $"Cannot build a {what} with {count} lines; it needs at least two.");
        }

        return bits;
    }

    private static ModuleLayout CreateLayout(int inputCount, int selectBits)
        => new ModuleLayout()
            .AddInputGroup("SEL", 0, selectBits)
            .AddInputGroup("IN", selectBits, inputCount)
            .AddOutputGroup("OUT", 0);
}
