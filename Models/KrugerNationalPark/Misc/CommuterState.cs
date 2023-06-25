using KrugerNationalPark.Agents;

namespace KrugerNationalPark.Misc;

/// <summary>
/// The <see cref="CommuterState"/> enumerates the states that <see cref="Commuter"/> agents can be in.
/// </summary>
public enum CommuterState
{
    GoingToWork,
    Working,
    GoingHome
}