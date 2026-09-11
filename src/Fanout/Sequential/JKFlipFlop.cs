using Fanout.Gates;

namespace Fanout.Sequential;

/// <summary>
/// A JK flip-flop with asynchronous preset and clear, both active low.
/// </summary>
/// <remarks>
/// <para>Inputs: <c>J</c>, <c>K</c>, <c>CLK</c>, <c>PRE</c>, <c>CLR</c>. Outputs: <c>Q</c>, <c>QN</c>.</para>
/// <para>
/// On the selected clock edge: <c>J=0 K=0</c> holds, <c>J=1 K=0</c> sets, <c>J=0 K=1</c> clears,
/// <c>J=1 K=1</c> toggles.
/// </para>
/// <para>Pulse <c>CLR</c> low before clocking, or <c>Q</c> stays <see cref="LogicState.Unknown"/>.</para>
/// </remarks>
public sealed class JKFlipFlop : Module
{
    /// <summary>The port layout shared by every instance.</summary>
    public static readonly ModuleLayout Layout = CreateLayout();

    /// <summary>Builds a JK flip-flop.</summary>
    /// <param name="edge">Which clock transition acts on <c>J</c> and <c>K</c>.</param>
    /// <param name="settlePresetAndClear">
    /// When <c>true</c> (the default), <c>PRE</c> and <c>CLR</c> start inactive.
    /// </param>
    public JKFlipFlop(ClockEdge edge = ClockEdge.Falling, bool settlePresetAndClear = true)
    {
        Port j = new();
        Port k = new();
        Port clock = new();
        Port preset = new();
        Port clear = new();
        Port q = new();
        Port qn = new();

        Nand nand1 = new(3);
        Nand nand2 = new(3);
        Nand nand3 = new(3);
        Nand nand4 = new(3);
        Nand nand5 = new();
        Nand nand6 = new();
        Nand nand7 = new();
        Nand nand8 = new();
        And gate = new(3);
        Not invert = new();
        Not invertClock = new();

        AddInput(j);
        AddInput(k);
        AddInput(clock);
        AddInput(preset);
        AddInput(clear);
        AddOutput(q);
        AddOutput(qn);

        j.ConnectTo(nand1, 0);
        k.ConnectTo(nand2, 2);

        if (edge == ClockEdge.Falling)
        {
            clock.ConnectTo(gate, 1);
        }
        else
        {
            clock.ConnectTo(invertClock, 0);
            invertClock.ConnectTo(gate, 1);
        }

        gate.ConnectTo(invert, 0);
        gate.ConnectTo(nand1, 2);
        gate.ConnectTo(nand2, 0);
        invert.ConnectTo(nand5, 1);
        invert.ConnectTo(nand6, 0);

        preset.ConnectTo(gate, 0);
        preset.ConnectTo(nand3, 1);
        clear.ConnectTo(gate, 2);
        clear.ConnectTo(nand4, 1);

        nand1.ConnectTo(nand3, 0);
        nand2.ConnectTo(nand4, 2);
        nand3.ConnectTo(nand4, 0);
        nand4.ConnectTo(nand3, 2);
        nand3.ConnectTo(nand5, 0);
        nand4.ConnectTo(nand6, 1);
        nand5.ConnectTo(nand7, 0);
        nand6.ConnectTo(nand8, 1);
        nand7.ConnectTo(nand8, 0);
        nand8.ConnectTo(nand7, 1);
        nand7.ConnectTo(nand2, 1);
        nand8.ConnectTo(nand1, 1);

        nand7.ConnectTo(q);
        nand8.ConnectTo(qn);

        if (settlePresetAndClear)
        {
            preset.SetState(LogicState.One, null);
            clear.SetState(LogicState.One, null);
        }
    }

    /// <inheritdoc />
    public override int InputIndex(string name) => Layout.InputIndex(name);

    /// <inheritdoc />
    public override int OutputIndex(string name) => Layout.OutputIndex(name);

    private static ModuleLayout CreateLayout()
        => new ModuleLayout()
            .AddInputGroup("J", 0)
            .AddInputGroup("K", 1)
            .AddInputGroup("CLK", 2)
            .AddInputGroup("PRE", 3)
            .AddInputGroup("CLR", 4)
            .AddOutputGroup("Q", 0)
            .AddOutputGroup("QN", 1);
}
