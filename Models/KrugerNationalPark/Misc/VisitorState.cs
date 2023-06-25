using KrugerNationalPark.Agents;

namespace KrugerNationalPark.Misc;

/// <summary>
/// The <see cref="VisitorState"/> enumerates the states that <see cref="Visitor"/> agents can be in.
/// </summary>
public enum VisitorState
{
    Driving,
    Braking,
    Looking
}