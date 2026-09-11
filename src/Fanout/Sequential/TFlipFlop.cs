using Fanout.Gates;

namespace Fanout.Sequential;

/// <summary>
/// A toggle flip-flop with asynchronous preset and clear, both active low.
/// </summary>
/// <remarks>
/// <para>Inputs: <c>T</c>, <c>PRE</c>, <c>CLR</c>. Outputs: <c>Q</c>, <c>QN</c>.</para>
/// <para>
/// <c>T</c> is both the toggle enable and the clock: every selected edge on <c>T</c> flips the
/// output. That is what makes a chain of these a ripple counter — see <see cref="RippleCounter"/>.
/// </para>
/// <para>Pulse <c>CLR</c> low before use, or <c>Q</c> stays <see cref="LogicState.Unknown"/>.</para>
/// </remarks>
public sealed class TFlipFlop : Module
{
    /// <summary>The port layout shared by every instance.</summary>
    public static readonly ModuleLayout Layout = CreateLayout();

    /// <summary>Builds a toggle flip-flop.</summary>
    /// <param name="edge">Which transition on <c>T</c> toggles the output.</param>
    /// <param name="settlePresetAndClear">
    /// When <c>true</c> (the default), <c>PRE</c> and <c>CLR</c> start inactive.
    /// </param>
    public TFlipFlop(ClockEdge edge = ClockEdge.Falling, bool settlePresetAndClear = true)
    {
        Port toggle = new();
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
        Not invertToggle = new();

        AddInput(toggle);
        AddInput(preset);
        AddInput(clear);
        AddOutput(q);
        AddOutput(qn);

        if (edge == ClockEdge.Falling)
        {
            toggle.ConnectTo(gate, 1);
        }
        else
        {
            toggle.ConnectTo(invertToggle, 0);
            invertToggle.ConnectTo(gate, 1);
        }

        gate.ConnectTo(invert, 0);
        gate.ConnectTo(nand1, 1);
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
        nand8.ConnectTo(nand1, 0);

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
            .AddInputGroup("T", 0)
            .AddInputGroup("PRE", 1)
            .AddInputGroup("CLR", 2)
            .AddOutputGroup("Q", 0)
            .AddOutputGroup("QN", 1);
}
