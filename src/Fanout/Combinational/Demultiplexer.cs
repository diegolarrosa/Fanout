using System.Collections.Concurrent;
using Fanout.Gates;

namespace Fanout.Combinational;

/// <summary>
/// A demultiplexer: the enable input is routed to whichever output the select lines address,
/// and every other output is held low.
/// </summary>
/// <remarks>
/// <para>Ports, in order: <c>SEL</c> (ceil(log2 n) bits, LSB first), <c>EN</c>.
/// Outputs: <c>OUT</c> (n bits).</para>
/// <para>With <c>EN</c> tied high this is an address decoder.</para>
/// </remarks>
public sealed class Demultiplexer : Module
{
    private static readonly ConcurrentDictionary<int, ModuleLayout> Layouts = new();

    /// <summary>Builds a demultiplexer with <paramref name="outputCount"/> outputs.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="outputCount"/> is below two or above 2^31.</exception>
    public Demultiplexer(int outputCount = 2)
    {
        int selectBits = Multiplexer.SelectBitsFor(outputCount, "demultiplexer");

        DataOutputCount = outputCount;
        SelectCount = selectBits;
        Layout = Layouts.GetOrAdd(outputCount, _ => CreateLayout(outputCount, selectBits));

        Port[] select = Port.Create(selectBits);
        Port[] outputs = Port.Create(outputCount);
        Port enable = new();

        Not[] invert = new Not[selectBits];

        for (int i = 0; i < selectBits; i++)
        {
            invert[i] = new Not();
        }

        And[] decode = new And[outputCount];

        for (int i = 0; i < outputCount; i++)
        {
            decode[i] = new And(selectBits + 1);
        }

        for (int i = 0; i < selectBits; i++)
        {
            AddInput(select[i]);
            select[i].ConnectTo(invert[i], 0);
        }

        for (int i = 0; i < outputCount; i++)
        {
            AddOutput(outputs[i]);
        }

        AddInput(enable);

        for (int bit = 0; bit < selectBits; bit++)
        {
            for (int block = 0; block < outputCount; block += 2 << bit)
            {
                for (int offset = 0; offset < (1 << bit); offset++)
                {
                    if ((block + offset + (1 << bit)) < outputCount)
                    {
                        select[bit].ConnectTo(decode[block + offset + (1 << bit)], bit);
                    }

                    if ((block + offset) < outputCount)
                    {
                        invert[bit].ConnectTo(decode[block + offset], bit);
                    }
                }
            }
        }

        for (int i = 0; i < outputCount; i++)
        {
            enable.ConnectTo(decode[i], selectBits);
            decode[i].ConnectTo(outputs[i]);
        }
    }

    /// <summary>How many outputs the demultiplexer has.</summary>
    public int DataOutputCount { get; }

    /// <summary>How many select lines it takes to address them.</summary>
    public int SelectCount { get; }

    /// <summary>This size's port layout.</summary>
    public ModuleLayout Layout { get; }

    /// <summary>Index of the first <c>SEL</c> input.</summary>
    public int IndexSelect => 0;

    /// <summary>Index of the <c>EN</c> input.</summary>
    public int IndexEnable => SelectCount;

    /// <summary>Index of the first <c>OUT</c> output.</summary>
    public int IndexOutputs => 0;

    /// <inheritdoc />
    public override int InputIndex(string name) => Layout.InputIndex(name);

    /// <inheritdoc />
    public override int OutputIndex(string name) => Layout.OutputIndex(name);

    private static ModuleLayout CreateLayout(int outputCount, int selectBits)
        => new ModuleLayout()
            .AddInputGroup("SEL", 0, selectBits)
            .AddInputGroup("EN", selectBits)
            .AddOutputGroup("OUT", 0, outputCount);
}
