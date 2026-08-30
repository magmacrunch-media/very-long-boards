using Godot;

/// <summary>
/// One sine term in a terrain function.
///
/// Stored as a wavelength in metres rather than the raw radians-per-metre the maths wants,
/// because "a 66 m roller" is something you can picture and "0.095" isn't. The conversion
/// happens once, in <see cref="AngularFrequency"/>.
/// </summary>
[Tool]
[GlobalClass]
public partial class SineLayer : Resource
{
    /// <summary>Peak height in metres for hills; lateral curve units for turns.</summary>
    [Export(PropertyHint.Range, "0,20,0.05,or_greater")]
    public float Amplitude { get; set; } = 1f;

    /// <summary>Distance along the road for one full up-and-down, in metres.</summary>
    [Export(PropertyHint.Range, "10,10000,1,or_greater")]
    public float WavelengthMetres { get; set; } = 500f;

    public float AngularFrequency
    {
        get { return WavelengthMetres <= 0f ? 0f : Mathf.Tau / WavelengthMetres; }
    }

    /// <summary>
    /// Steepness is amplitude x frequency, not amplitude — a 0.9 m roll over 66 m is steeper
    /// than a 5.5 m roll over 1257 m. Surfaced so tools can rank the layers by what they
    /// actually do to the ride.
    /// </summary>
    public float PeakGrade
    {
        get { return Amplitude * AngularFrequency; }
    }

    public SineLayer() { }

    public SineLayer(float amplitude, float wavelengthMetres)
    {
        Amplitude = amplitude;
        WavelengthMetres = wavelengthMetres;
    }
}
