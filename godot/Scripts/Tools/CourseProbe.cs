using Godot;

/// <summary>
/// Reads a course resource back and reports what the ride will actually do on it.
///
/// Sibling to AudioProbe: it exists because a course is numbers until somebody rides it, and
/// the two ways a course fails - too gentle to trigger anything, or a climb the rider cannot
/// carry momentum through - are both invisible in the Inspector.
///
///   godot --headless --path godot --scene res://Scenes/CourseProbe.tscn
/// </summary>
public partial class CourseProbe : Node
{
    public override void _Ready()
    {
        Report("Frogwood", GD.Load<CourseDesign>("res://Resources/Design/Frogwood.tres"));
        Report("Block Island", GD.Load<CourseDesign>("res://Resources/Design/BlockIsland.tres"));
        GetTree().Quit();
    }

    private static void Report(string name, CourseDesign c)
    {
        if (c == null) { GD.Print(name + ": FAILED TO LOAD"); return; }

        GD.Print("");
        GD.Print("== " + name + " (" + c.GetType().Name + ") ==");
        GD.Print($"  length        {c.Length:F0} m");
        GD.Print($"  start height  {c.HillAt(0f):F2} m");
        GD.Print($"  end height    {c.HillAt(c.Length):F2} m");
        GD.Print($"  net drop      {c.HillAt(0f) - c.HillAt(c.Length):F1} m");
        GD.Print($"  average grade {(c.HillAt(0f) - c.HillAt(c.Length)) / c.Length * 100f:F2}%");
        // The budget is the slowest rider's, not a round number: it is v^2/2g at HIS top
        // speed, and the three Carls do not share one. 22 m was the figure for a 3-pip
        // baseline rider who does not exist - every Carl in the game has SPD 4 or 5.
        int slowest = 5;
        foreach (var s in Main.CarlStats) slowest = Mathf.Min(slowest, s.Speed);
        float top = 21f * Mathf.Lerp(0.88f, 1.12f, (slowest - 1) / 4f);
        GD.Print($"  worst climb   {c.TotalRelief():F1} m   (tightest rider carries {top * top / (2f * 9.81f):F1} m)");

        // The longest stretch the rider cannot hold speed on.
        //
        // The climb budget above is necessary and not sufficient, and Frogwood is the proof:
        // ridden the other way round its worst climb was 3.3 m against a 25.3 m budget, every
        // simulation target passed, and a ridden run still ground down to the 9 km/h floor a
        // fifth of the way in. What did that is not a climb at all - it is 200 m of road
        // between -1% and +3% starting at the 450 m mark, and drag needs about 4% of grade
        // just to HOLD 40 km/h. A course can clear every climb it has and stall on the flat
        // between them. That is what reversing the route fixed, and this is what named it.
        const float hold = 40f / 3.6f;                     // the speed worth holding, m/s
        float need = hold * hold * 0.0032f / 9.81f;        // grade that balances drag there
        float worstRun = 0f, runStart = 0f, runAt = 0f;
        bool inRun = false;
        for (float z = 0; z + 10f <= c.Length; z += 10f)
        {
            float grade = (c.HillAt(z) - c.HillAt(z + 10f)) / 10f;
            if (grade < need)
            {
                if (!inRun) { inRun = true; runStart = z; }
                if (z + 10f - runStart > worstRun) { worstRun = z + 10f - runStart; runAt = runStart; }
            }
            else inRun = false;
        }
        GD.Print($"  cannot hold 40 {worstRun:F0} m from {runAt:F0} m   (needs {need * 100f:F1}% to hold it)");

        float maxCurve = 0f;
        for (float z = 0; z <= c.Length; z += 5f) maxCurve = Mathf.Max(maxCurve, Mathf.Abs(c.CurveAt(z)));
        GD.Print($"  peak heading  {maxCurve:F2} rad");

        // Steepest sustained stretches, and what the ride settles at on each.
        foreach (int w in new[] { 200, 400, 600 })
        {
            float best = 0f;
            for (float z = 0; z + w <= c.Length; z += 5f)
                best = Mathf.Max(best, c.HillAt(z) - c.HillAt(z + w));
            float grade = best / w;
            GD.Print($"  steepest {w,4} m  {grade * 100f:F2}%  -> {Mathf.Sqrt(grade * 9.81f / 0.0032f) * 3.6f:F0} km/h");
        }
        GD.Print("  wobble onset  57 km/h");
    }
}
