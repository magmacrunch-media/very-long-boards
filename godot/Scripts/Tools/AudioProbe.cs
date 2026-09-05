using Godot;

/// <summary>
/// Measures what AudioKit generated. Nothing at runtime touches this; it is the audio
/// equivalent of physics_sim.py, and exists because the two ways a synthesized stream goes
/// wrong are both silent to the compiler and to the eye.
///
/// The column that matters for a bed is <c>dSeam/dRms</c>: the size of the jump from the last
/// sample back to the first, divided by the size of an ordinary sample-to-sample step. At 1
/// the join is indistinguishable from the signal either side of it, which is the whole claim
/// NoiseLoop makes. Anything above about 5 is a click you will hear once per loop, forever.
///
/// For a one-shot the columns that matter are <c>first</c> and <c>last</c> — both must be 0,
/// or the sound clicks on and off — and <c>peak</c>, which must stay under 1.0 or the stream
/// is clipped before the mix ever gets a say.
///
/// Run it headless, from the project folder:
///
///   godot --headless --path godot --scene res://Scenes/AudioProbe.tscn
/// </summary>
public partial class AudioProbe : Node
{
    public override void _Ready()
    {
        GD.Print("name          frames   peak    rms     seam    dSeam/dRms  centroidHz");
        Bed("Rumble", AudioKit.Rumble);
        Bed("Gravel", AudioKit.Gravel);
        Bed("Wind", AudioKit.Wind);
        Bed("Rattle", AudioKit.Rattle);
        Bed("Scrub", AudioKit.Scrub);
        Bed("Ambience", AudioKit.Ambience);
        GD.Print("");
        GD.Print("name          frames   peak    first   last    centroidHz");
        Shot("Kick", AudioKit.Kick);
        Shot("Crash", AudioKit.Crash);
        Shot("Finish", AudioKit.Finish);
        Shot("Beep", AudioKit.Beep);
        Shot("Go", AudioKit.Go);
        Shot("Move", AudioKit.Move);
        Shot("Confirm", AudioKit.Confirm);
        Shot("Back", AudioKit.Back);
        Shot("Deny", AudioKit.Deny);
        Shot("Bird", AudioKit.Bird);
        GetTree().Quit();
    }

    private static float[] Decode(AudioStreamWav w)
    {
        var d = w.Data;
        var s = new float[d.Length / 2];
        for (int i = 0; i < s.Length; i++)
            s[i] = (short)(d[i * 2] | (d[i * 2 + 1] << 8)) / 32768f;
        return s;
    }

    private static void Bed(string name, AudioStreamWav w)
    {
        float[] s = Decode(w);
        float peak = 0f, sum = 0f, dsum = 0f;
        for (int i = 0; i < s.Length; i++)
        {
            peak = Mathf.Max(peak, Mathf.Abs(s[i]));
            sum += s[i] * s[i];
            if (i > 0) dsum += (s[i] - s[i - 1]) * (s[i] - s[i - 1]);
        }
        float rms = Mathf.Sqrt(sum / s.Length);
        float drms = Mathf.Sqrt(dsum / (s.Length - 1));
        float seam = Mathf.Abs(s[0] - s[s.Length - 1]);
        GD.Print($"{name,-12} {s.Length,7}  {peak,6:F3}  {rms,6:F3}  {seam,6:F4}  {seam / drms,8:F2}    {Centroid(s),8:F0}  loop={w.LoopMode} end={w.LoopEnd}");
    }

    private static void Shot(string name, AudioStreamWav w)
    {
        float[] s = Decode(w);
        float peak = 0f;
        for (int i = 0; i < s.Length; i++) peak = Mathf.Max(peak, Mathf.Abs(s[i]));
        GD.Print($"{name,-12} {s.Length,7}  {peak,6:F3}  {s[0],6:F3}  {s[s.Length - 1],6:F3}  {Centroid(s),8:F0}");
    }

    /// <summary>
    /// Spectral centroid by the zero-crossing proxy: for a band-limited signal it tracks the
    /// centre of energy closely enough to tell "40-700 Hz" from "500-6500 Hz", which is all
    /// this needs to catch a filter wired the wrong way round.
    /// </summary>
    private static float Centroid(float[] s)
    {
        int crossings = 0;
        for (int i = 1; i < s.Length; i++)
            if ((s[i - 1] < 0f) != (s[i] < 0f)) crossings++;
        return crossings * 0.5f * AudioKit.Rate / s.Length;
    }
}
