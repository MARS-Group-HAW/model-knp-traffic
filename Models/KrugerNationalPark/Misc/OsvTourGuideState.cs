using KrugerNationalPark.Agents;

namespace KrugerNationalPark.Misc;

/// <summary>
/// The <see cref="OsvTourGuideState"/> enumerates the states that <see cref="OsvTourGuide"/> agents can be in.
/// </summary>
public enum OsvTourGuideState
{
    Idling,
    Driving,
    Braking,
    Looking
}