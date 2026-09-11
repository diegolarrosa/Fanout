using Fanout.Combinational;
using Fanout.Gates;
using Fanout.Sequential;

namespace Fanout.Demo;

/// <summary>
/// Three short circuits, each printed as it settles, so the library can be seen working
/// without reading any of it.
/// </summary>
internal static class Program
{
    private static void Main()
    {
        Console.WriteLine("Fanout - gate-level digital logic simulation");
        Console.WriteLine();

        ShowAdder();
        Console.WriteLine();
        ShowCounter();
        Console.WriteLine();
        ShowShiftRegister();
        Console.WriteLine();
        ShowThreeValuedLogic();
    }

    private static void ShowAdder()
    {
        const int Width = 4;

        Console.WriteLine($"A {Width}-bit ripple-carry adder, built from {Width} full adders");
        Console.WriteLine(new string('-', 60));

        Circuit circuit = new();
        RippleCarryAdder adder = new(Width);

        Port[] a = DriveBus(circuit, adder, "A", Width);
        Port[] b = DriveBus(circuit, adder, "B", Width);
        Port carryIn = Drive(circuit, adder, "CIN");

        carryIn.SetState(LogicState.Zero);

        Console.WriteLine("    A    +    B    =  COUT SUM   rounds");

        foreach ((int x, int y) in new[] { (3, 5), (9, 6), (15, 1), (12, 12) })
        {
            SetBus(a, x);
            SetBus(b, y);
            int rounds = circuit.Run();

            int sum = ReadBus(adder, "SUM", Width);
            int carryOut = adder.Output("COUT").State == LogicState.One ? 1 : 0;

            Console.WriteLine(
                $"  {Binary(x, Width)} + {Binary(y, Width)} =    {carryOut}  {Binary(sum, Width)}"
                + $"    {rounds,3}     ({x} + {y} = {(carryOut << Width) | sum})");
        }

        Console.WriteLine();
        Console.WriteLine("  Rounds vary because bit positions that generate or kill a carry");
        Console.WriteLine("  settle at once, while positions that propagate one have to wait.");
        Console.WriteLine("  It is a cost signal, not a measure of logic depth - the order");
        Console.WriteLine("  gates are evaluated in within a round is unspecified.");
    }

    private static void ShowCounter()
    {
        const int Width = 4;

        Console.WriteLine($"A {Width}-bit ripple counter, built from {Width} toggle flip-flops");
        Console.WriteLine(new string('-', 60));

        Circuit circuit = new();
        RippleCounter counter = new(Width, ClockEdge.Rising, CountDirection.Up);

        Port clock = Drive(circuit, counter, "CLK");
        Port preset = Drive(circuit, counter, "PRE");
        Port clear = Drive(circuit, counter, "CLR");

        preset.SetState(LogicState.One);
        clock.SetState(LogicState.Zero);
        circuit.Run();

        // Until the clear is pulsed the cross-coupled outputs have no defined state at all.
        Console.WriteLine($"  before clear : {Describe(counter, "Q", Width)}");

        clear.SetState(LogicState.Zero);
        circuit.Run();
        clear.SetState(LogicState.One);
        circuit.Run();

        Console.WriteLine($"  after clear  : {Describe(counter, "Q", Width)}");
        Console.Write("  counting     :");

        for (int pulse = 0; pulse < 17; pulse++)
        {
            clock.SetState(LogicState.Zero);
            circuit.Run();
            clock.SetState(LogicState.One);
            circuit.Run();

            Console.Write($" {ReadBus(counter, "Q", Width)}");
        }

        Console.WriteLine();
    }

    private static void ShowShiftRegister()
    {
        const int Width = 5;

        Console.WriteLine($"A {Width}-stage shift register walking a single one along");
        Console.WriteLine(new string('-', 60));

        Circuit circuit = new();
        ShiftRegister register = new(Width, ClockEdge.Rising);

        Port serialIn = Drive(circuit, register, "D");
        Port clock = Drive(circuit, register, "CLK");
        Port preset = Drive(circuit, register, "PRE");
        Port clear = Drive(circuit, register, "CLR");

        preset.SetState(LogicState.One);
        clock.SetState(LogicState.Zero);
        serialIn.SetState(LogicState.Zero);
        circuit.Run();

        clear.SetState(LogicState.Zero);
        circuit.Run();
        clear.SetState(LogicState.One);
        circuit.Run();

        for (int step = 0; step < 7; step++)
        {
            serialIn.SetState(step == 0 ? LogicState.One : LogicState.Zero);
            circuit.Run();

            clock.SetState(LogicState.Zero);
            circuit.Run();
            clock.SetState(LogicState.One);
            circuit.Run();

            Console.WriteLine($"  step {step + 1}: {Binary(ReadBus(register, "Q", Width), Width)}");
        }
    }

    private static void ShowThreeValuedLogic()
    {
        Console.WriteLine("Unknown is a real answer: what settles, and what cannot");
        Console.WriteLine(new string('-', 60));

        // An AND gate with one input low is decided, whatever the others do.
        Circuit decided = new();
        And wideAnd = new(4);
        Port low = decided.AddInput();
        low.ConnectTo(wideAnd, 0);
        low.SetState(LogicState.Zero);
        decided.Run();

        Console.WriteLine(
            $"  AND(0, ?, ?, ?)     -> {Show(wideAnd.Output.State)}"
            + "   one controlling zero decides it");

        // XOR has no controlling value, so it waits for every input.
        Circuit waiting = new();
        Xor parity = new(4);
        Port high = waiting.AddInput();
        high.ConnectTo(parity, 0);
        high.SetState(LogicState.One);
        waiting.Run();

        Console.WriteLine(
            $"  XOR(1, ?, ?, ?)     -> {Show(parity.Output.State)}"
            + "   no controlling value, so it waits");

        // A cross-coupled latch genuinely has no state until something forces one.
        Circuit latched = new();
        NorSRLatch latch = new();
        Port reset = Drive(latched, latch, "R");
        Port set = Drive(latched, latch, "S");

        reset.SetState(LogicState.Zero);
        set.SetState(LogicState.Zero);
        latched.Run();

        Console.WriteLine($"  NOR latch, R=0 S=0  -> Q = {Show(latch.Output("Q").State)}"
            + "   metastable: no state yet");

        set.SetState(LogicState.One);
        latched.Run();
        set.SetState(LogicState.Zero);
        latched.Run();

        Console.WriteLine($"  after pulsing S     -> Q = {Show(latch.Output("Q").State)}"
            + "   and now it holds");

        Console.WriteLine();
        Console.WriteLine("  This is why the simulator can settle a circuit that is only");
        Console.WriteLine("  half driven, and why it reports a latch as undefined instead");
        Console.WriteLine("  of quietly inventing a zero for it.");
    }

    private static string Show(LogicState state) => state switch
    {
        LogicState.Zero => "0",
        LogicState.One => "1",
        _ => "?",
    };

    private static Port Drive(Circuit circuit, Module target, string name)
    {
        Port port = circuit.AddInput();
        port.ConnectTo(target, name);
        return port;
    }

    private static Port[] DriveBus(Circuit circuit, Module target, string name, int width)
    {
        Port[] ports = new Port[width];

        for (int i = 0; i < width; i++)
        {
            ports[i] = circuit.AddInput();
            ports[i].ConnectTo(target, name, i);
        }

        return ports;
    }

    private static void SetBus(Port[] ports, int value)
    {
        for (int i = 0; i < ports.Length; i++)
        {
            ports[i].SetState(((value >> i) & 1) == 0 ? LogicState.Zero : LogicState.One);
        }
    }

    private static int ReadBus(Module target, string name, int width)
    {
        int value = 0;

        for (int i = 0; i < width; i++)
        {
            if (target.Output(name, i).State == LogicState.One)
            {
                value |= 1 << i;
            }
        }

        return value;
    }

    private static string Describe(Module target, string name, int width)
    {
        char[] characters = new char[width];

        for (int i = 0; i < width; i++)
        {
            characters[width - 1 - i] = target.Output(name, i).State switch
            {
                LogicState.Zero => '0',
                LogicState.One => '1',
                _ => '?',
            };
        }

        return new string(characters);
    }

    private static string Binary(int value, int width)
    {
        char[] characters = new char[width];

        for (int i = 0; i < width; i++)
        {
            characters[width - 1 - i] = ((value >> i) & 1) == 0 ? '0' : '1';
        }

        return new string(characters);
    }
}
