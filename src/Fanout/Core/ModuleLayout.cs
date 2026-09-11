namespace Fanout;

/// <summary>
/// The named map of a module's ports, so a circuit can be wired by signal name
/// (<c>"CIN"</c>, <c>"Q"</c>) instead of by position.
/// </summary>
/// <remarks>
/// A layout depends only on the shape of a module, not on any particular instance, so modules
/// with a fixed shape keep one static layout and parameterised ones cache a layout per width.
/// </remarks>
public sealed class ModuleLayout
{
    private readonly Dictionary<string, PortGroup> _inputs = new(StringComparer.Ordinal);
    private readonly Dictionary<string, PortGroup> _outputs = new(StringComparer.Ordinal);

    /// <summary>Every input group, in no particular order.</summary>
    public IReadOnlyCollection<PortGroup> Inputs => _inputs.Values;

    /// <summary>Every output group, in no particular order.</summary>
    public IReadOnlyCollection<PortGroup> Outputs => _outputs.Values;

    /// <summary>Declares an input group starting at <paramref name="index"/>.</summary>
    public ModuleLayout AddInputGroup(string name, int index, int count = 1)
    {
        _inputs.Add(name, new PortGroup(name, index, count));
        return this;
    }

    /// <summary>Declares an output group starting at <paramref name="index"/>.</summary>
    public ModuleLayout AddOutputGroup(string name, int index, int count = 1)
    {
        _outputs.Add(name, new PortGroup(name, index, count));
        return this;
    }

    /// <summary>The index of the first port of a named input group.</summary>
    public int InputIndex(string name) => Find(_inputs, name, "input").Index;

    /// <summary>How many ports a named input group covers.</summary>
    public int InputCount(string name) => Find(_inputs, name, "input").Count;

    /// <summary>The index of the first port of a named output group.</summary>
    public int OutputIndex(string name) => Find(_outputs, name, "output").Index;

    /// <summary>How many ports a named output group covers.</summary>
    public int OutputCount(string name) => Find(_outputs, name, "output").Count;

    private static PortGroup Find(Dictionary<string, PortGroup> map, string name, string kind)
    {
        if (map.TryGetValue(name, out PortGroup? group))
        {
            return group;
        }

        throw new KeyNotFoundException(
            $"This module has no {kind} group named '{name}'. Known {kind} groups: "
            + (map.Count == 0 ? "(none)" : string.Join(", ", map.Keys)) + ".");
    }
}
