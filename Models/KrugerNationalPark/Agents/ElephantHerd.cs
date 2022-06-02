using System.Collections.Generic;

namespace KrugerNationalPark.Agents;

/// <summary>
///     Represents an elephant herd
///     Is not an agent, but just an object to
///     store information about elephants in a herd
/// </summary>
public class ElephantHerd
{
    public readonly List<Elephant> OtherElephants;

    public ElephantHerd(Elephant leader, List<Elephant> other)
    {
        OtherElephants = other;
        LeadingElephant = leader;
    }

    public Elephant LeadingElephant { get; }
}