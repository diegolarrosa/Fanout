namespace Fanout;

/// <summary>
/// A named, contiguous run of ports on a module — for example the eight <c>A</c> inputs of an adder.
/// </summary>
public sealed class PortGroup
{
    /// <summary>Creates a group.</summary>
    public PortGroup(string name, int index, int count)
    {
        Name = name;
        Index = index;
        Count = count;
    }

    /// <summary>The group's name.</summary>
    public string Name { get; }

    /// <summary>The index of the group's first port.</summary>
    public int Index { get; }

    /// <summary>How many ports the group covers.</summary>
    public int Count { get; }
}
