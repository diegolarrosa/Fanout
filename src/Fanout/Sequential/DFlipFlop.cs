using Fanout.Gates;

namespace Fanout.Sequential;

/// <summary>
/// An edge-triggered D flip-flop with asynchronous preset and clear, both active low.
/// </summary>
/// <remarks>
/// <para>Inputs: <c>D</c>, <c>CLK</c>, <c>PRE</c>, <c>CLR</c>. Outputs: <c>Q</c>, <c>QN</c>.</para>
/// <para>
/// The output pair is a cross-coupled NAND latch, so it has no defined state until something
/// forces one. Pulse <c>CLR</c> low once before clocking, or leave
/// <c>settlePresetAndClear</c> at its default and pulse it from the circuit; without that,
/// <c>Q</c> reads <see cref="LogicState.Unknown"/> and the flip-flop will not respond to the clock.
/// </para>
/// </remarks>
public sealed class DFlipFlop : Module
{
    /// <summary>The port layout shared by every instance.</summary>
    public static readonly ModuleLayout Layout = CreateLayout();

    /// <summary>Builds a flip-flop.</summary>
    /// <param name="edge">Which clock transition captures <c>D</c>.</param>
    /// <param name="settlePresetAndClear">
    /// When <c>true</c> (the default), <c>PRE</c> and <c>CLR</c> start at high — inactive — so a
    /// circuit that never wires them still settles. Pass <c>false</c> to drive them yourself.
    /// </param>
    public DFlipFlop(ClockEdge edge = ClockEdge.Falling, bool settlePresetAndClear = true)
    {
        Port d = new();
        Port clock = new();
        Port preset = new();
        Port clear = new();
        Port q = new();
        Port qn = new();

        Nand nand1 = new();
        Nand nand2 = new();
        Nand nand3 = new(3);
        Nand nand4 = new(3);
        Nand nand5 = new();
        Nand nand6 = new();
        Nand nand7 = new();
        Nand nand8 = new();
        And gate = new(3);
        Not invert = new();
        Not invertClock = new();

        AddInput(d);
        AddInput(clock);
        AddInput(preset);
        AddInput(clear);
        AddOutput(q);
        AddOutput(qn);

        d.ConnectTo(nand1, 0);

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
        gate.ConnectTo(nand1, 1);
        gate.ConnectTo(nand2, 1);
        invert.ConnectTo(nand5, 1);
        invert.ConnectTo(nand6, 0);

        preset.ConnectTo(gate, 0);
        preset.ConnectTo(nand3, 1);
        clear.ConnectTo(gate, 2);
        clear.ConnectTo(nand4, 1);

        nand1.ConnectTo(nand3, 0);
        nand1.ConnectTo(nand2, 0);
        nand2.ConnectTo(nand4, 2);
        nand3.ConnectTo(nand4, 0);
        nand4.ConnectTo(nand3, 2);
        nand3.ConnectTo(nand5, 0);
        nand4.ConnectTo(nand6, 1);
        nand5.ConnectTo(nand7, 0);
        nand6.ConnectTo(nand8, 1);
        nand7.ConnectTo(nand8, 0);
        nand8.ConnectTo(nand7, 1);

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
            .AddInputGroup("D", 0)
            .AddInputGroup("CLK", 1)
            .AddInputGroup("PRE", 2)
            .AddInputGroup("CLR", 3)
            .AddOutputGroup("Q", 0)
            .AddOutputGroup("QN", 1);
}
