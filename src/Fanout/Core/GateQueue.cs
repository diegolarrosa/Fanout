using System.Collections;

namespace Fanout;

/// <summary>
/// The set of gates waiting to be evaluated in the current propagation round.
/// </summary>
/// <remarks>
/// It is a set, not a list: a gate whose three inputs all change in the same round must be
/// evaluated once, not three times. Gates are compared by reference, which is what the identity
/// of a circuit element means here.
/// </remarks>
public sealed class GateQueue : IEnumerable<Gate>
{
    private readonly HashSet<Gate> _gates = new();

    /// <summary>How many gates are waiting.</summary>
    public int Count => _gates.Count;

    /// <summary>Adds a gate. Returns <c>false</c> if it was already queued.</summary>
    public bool Enqueue(Gate gate) => _gates.Add(gate);

    /// <summary>Whether the gate is already queued.</summary>
    public bool Contains(Gate gate) => _gates.Contains(gate);

    /// <summary>Empties the queue.</summary>
    public void Clear() => _gates.Clear();

    /// <summary>Adds every gate from another queue.</summary>
    public void UnionWith(GateQueue other) => _gates.UnionWith(other._gates);

    /// <summary>Returns a struct enumerator, so iterating does not allocate.</summary>
    public HashSet<Gate>.Enumerator GetEnumerator() => _gates.GetEnumerator();

    IEnumerator<Gate> IEnumerable<Gate>.GetEnumerator() => _gates.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _gates.GetEnumerator();
}
