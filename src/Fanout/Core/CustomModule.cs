namespace Fanout;

/// <summary>
/// A module assembled at run time, with a layout supplied by the caller rather than baked into a type.
/// </summary>
/// <remarks>
/// Use this when a circuit is generated from data — a netlist file, a synthesis pass, a solver —
/// and there is no point declaring a C# class per block.
/// </remarks>
public sealed class CustomModule : Module
{
    /// <summary>Creates a module with the given name and port layout.</summary>
    public CustomModule(string name, ModuleLayout layout)
    {
        Name = name;
        Layout = layout;
    }

    /// <summary>A label for this module, used in diagnostics.</summary>
    public string Name { get; }

    /// <summary>This module's port layout.</summary>
    public ModuleLayout Layout { get; }

    /// <inheritdoc />
    public override int InputIndex(string name) => Layout.InputIndex(name);

    /// <inheritdoc />
    public override int OutputIndex(string name) => Layout.OutputIndex(name);
}
