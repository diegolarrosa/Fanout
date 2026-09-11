using Fanout.Gates;

namespace Fanout.Combinational;

/// <summary>
/// A one-bit full adder: <c>A + B + CIN</c>, giving <c>SUM</c> and <c>COUT</c>.
/// </summary>
/// <remarks>
/// Built from two XORs, two ANDs and an OR — the textbook nine-gate cell. Every wider adder
/// in this library is a chain of these.
/// </remarks>
public sealed class FullAdder : Module
{
    /// <summary>The port layout shared by every instance.</summary>
    public static readonly ModuleLayout Layout = CreateLayout();

    /// <summary>Builds the cell.</summary>
    public FullAdder()
    {
        Port a = new();
        Port b = new();
        Port carryIn = new();
        Port sum = new();
        Port carryOut = new();

        Xor xor1 = new();
        Xor xor2 = new();
        And and1 = new();
        And and2 = new();
        Or or = new();

        AddInput(a);
        AddInput(b);
        AddInput(carryIn);
        AddOutput(sum);
        AddOutput(carryOut);

        a.ConnectTo(xor1, 0);
        a.ConnectTo(and1, 0);
        b.ConnectTo(xor1, 1);
        b.ConnectTo(and1, 1);
        carryIn.ConnectTo(xor2, 1);
        carryIn.ConnectTo(and2, 1);
        xor1.ConnectTo(xor2, 0);
        xor1.ConnectTo(and2, 0);
        and2.ConnectTo(or, 0);
        and1.ConnectTo(or, 1);
        xor2.ConnectTo(sum);
        or.ConnectTo(carryOut);
    }

    /// <inheritdoc />
    public override int InputIndex(string name) => Layout.InputIndex(name);

    /// <inheritdoc />
    public override int OutputIndex(string name) => Layout.OutputIndex(name);

    private static ModuleLayout CreateLayout()
        => new ModuleLayout()
            .AddInputGroup("A", 0)
            .AddInputGroup("B", 1)
            .AddInputGroup("CIN", 2)
            .AddOutputGroup("SUM", 0)
            .AddOutputGroup("COUT", 1);
}
