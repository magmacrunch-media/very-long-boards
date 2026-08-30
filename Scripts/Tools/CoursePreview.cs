using Godot;

/// <summary>
/// Editor-only workbench for the course. Nothing in the game loads this — open
/// <c>Scenes/CoursePreview.tscn</c>, drop <c>Resources/Design/Frogwood.tres</c> into the Design
/// slot, then drag Distance to fly down the road while you tune the hills under yourself.
///
/// It drives the real <see cref="TerrainManager"/> and <see cref="SceneryManager"/>, so what
/// you shape here is exactly what the game builds. Children are added without an Owner, so
/// Godot never serialises the road into the scene file.
/// </summary>
[Tool]
public partial class CoursePreview : Node3D
{
    private CourseDesign _design;

    [Export]
    public CourseDesign Design
    {
        get { return _design; }
        set
        {
            if (_design != null) _design.Changed -= Rebuild;
            _design = value;
            if (_design != null) _design.Changed += Rebuild;
            Rebuild();
        }
    }

    /// <summary>Metres from the start line. Scrubbing this is how you inspect the whole course.</summary>
    private float _distance = 0f;
    [Export(PropertyHint.Range, "0,4000,5,or_greater")]
    public float Distance
    {
        get { return _distance; }
        set { _distance = value; Scroll(); }
    }

    /// <summary>
    /// Off by default — the forest is a few thousand meshes and rebuilding it on every slider
    /// tick makes the Inspector feel like treacle. Turn it on to check sightlines.
    /// </summary>
    private bool _showScenery = false;
    [Export]
    public bool ShowScenery
    {
        get { return _showScenery; }
        set { _showScenery = value; Rebuild(); }
    }

    [ExportToolButton("Rebuild")]
    public Callable RebuildButton => Callable.From(Rebuild);

    private Node3D _world;
    private TerrainManager _terrain;
    private SceneryManager _scenery;
    private int _signature;

    public override void _Ready()
    {
        Rebuild();
    }

    /// <summary>
    /// Catches Inspector edits the Changed signal misses, including edits to a SineLayer
    /// nested inside the HillLayers array — see <see cref="DesignWatcher"/>. Editor only.
    /// </summary>
    public override void _Process(double delta)
    {
        if (!Engine.IsEditorHint()) return;

        int now = DesignWatcher.Signature(Design);
        if (now != _signature) Rebuild();
    }

    private void Rebuild()
    {
        if (!IsInsideTree()) return;

        _signature = DesignWatcher.Signature(Design);

        if (_world != null)
        {
            RemoveChild(_world);
            _world.QueueFree();
        }
        _terrain = null;
        _scenery = null;

        _world = new Node3D();
        AddChild(_world);

        var design = Design ?? new CourseDesign();
        _terrain = new TerrainManager(_world, design);
        _terrain.Create();

        if (_showScenery)
        {
            _scenery = new SceneryManager(_world, _terrain, design);
            _scenery.Create();
        }

        Scroll();
    }

    /// <summary>Move the window without rebuilding materials or re-scattering the props.</summary>
    private void Scroll()
    {
        if (_terrain == null) return;
        _terrain.Update(_distance);
        if (_scenery != null) _scenery.UpdatePositions(_terrain.ScrollOffset);
    }

    /// <summary>
    /// Relief and peak grade for the current design, so you can sanity-check a reshape without
    /// leaving the editor. Total relief over about 22 m and the rider bogs down on every crest;
    /// physics_sim.py runs the full integration.
    /// </summary>
    public string Report()
    {
        var d = Design ?? new CourseDesign();
        float steepest = 0f;
        if (d.HillLayers != null)
            foreach (var layer in d.HillLayers)
                if (layer != null) steepest = Mathf.Max(steepest, layer.PeakGrade);

        return string.Format("relief {0:0.0} m (budget ~22 m) | steepest layer {1:0.0}% | net grade {2:0.0}%",
            d.TotalRelief(), steepest * 100f, d.Grade * 100f);
    }
}
