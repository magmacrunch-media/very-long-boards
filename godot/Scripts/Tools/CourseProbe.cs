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
        GD.Print($"  worst climb   {c.TotalRelief():F1} m   (budget ~22 m)");

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
