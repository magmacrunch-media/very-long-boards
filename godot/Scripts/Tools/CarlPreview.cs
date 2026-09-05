using Godot;

/// <summary>
/// Editor-only workbench for Carl. Nothing in the game loads this — open
/// <c>Scenes/CarlPreview.tscn</c>, drop <c>Resources/Design/Carl.tres</c> into the Design slot,
/// and he rebuilds in the viewport every time you touch a slider.
///
/// Children are added without an Owner, so Godot never serialises them into the scene file no
/// matter how many times you save.
/// </summary>
[Tool]
public partial class CarlPreview : Node3D
{
    private CarlDesign _design;
    private BoardDesign _boardLook;

    [Export]
    public CarlDesign Design
    {
        get { return _design; }
        set { Rebind(ref _design, value); }
    }

    [Export]
    public BoardDesign BoardLook
    {
        get { return _boardLook; }
        set { Rebind(ref _boardLook, value); }
    }

    private Main.CarlType _outfit = Main.CarlType.Office;
    [Export]
    public Main.CarlType Outfit
    {
        get { return _outfit; }
        set { _outfit = value; Rebuild(); }
    }

    private Main.BoardType _board = Main.BoardType.Classic;
    [Export]
    public Main.BoardType Board
    {
        get { return _board; }
        set { _board = value; Rebuild(); }
    }

    private bool _showBoard = true;
    [Export]
    public bool ShowBoard
    {
        get { return _showBoard; }
        set { _showBoard = value; Rebuild(); }
    }

    /// <summary>Spins him so you can check the silhouette from every side.</summary>
    private float _turntable = 0f;
    [Export(PropertyHint.Range, "-180,180,1")]
    public float Turntable
    {
        get { return _turntable; }
        set { _turntable = value; Rebuild(); }
    }

    [ExportToolButton("Rebuild")]
    public Callable RebuildButton => Callable.From(Rebuild);

    private Node3D _rig;
    private int _signature;

    public override void _Ready()
    {
        Rebuild();
    }

    /// <summary>
    /// Catches Inspector edits the Changed signal misses — see <see cref="DesignWatcher"/>.
    /// Editor only; at runtime nothing edits these resources.
    /// </summary>
    public override void _Process(double delta)
    {
        if (!Engine.IsEditorHint()) return;

        int now = DesignWatcher.Signature(Design) * 31 + DesignWatcher.Signature(BoardLook);
        if (now != _signature) Rebuild();
    }

    /// <summary>
    /// Swap the resource in the slot and follow its Changed signal. Godot fires that whenever
    /// an exported property is edited in the Inspector, which is what makes a slider drag
    /// reshape him live rather than on the next manual rebuild.
    /// </summary>
    private void Rebind<T>(ref T slot, T next) where T : Resource
    {
        if (slot != null) slot.Changed -= Rebuild;
        slot = next;
        if (slot != null)
            slot.Changed += Rebuild;
        Rebuild();
    }

    private void Rebuild()
    {
        if (!IsInsideTree()) return;

        _signature = DesignWatcher.Signature(Design) * 31 + DesignWatcher.Signature(BoardLook);

        if (_rig != null)
        {
            RemoveChild(_rig);
            _rig.QueueFree();
            _rig = null;
        }

        _rig = new Node3D();
        _rig.Rotation = new Vector3(0, Mathf.DegToRad(_turntable), 0);
        AddChild(_rig);

        var design = Design ?? new CarlDesign();
        float deckTop = 0f;

        if (_showBoard)
        {
            var boardLook = BoardLook ?? new BoardDesign();
            var board = BoardBuilder.Build(_board, boardLook);
            _rig.AddChild(board);
            deckTop = boardLook.GripTopY;
        }

        var carl = CarlBuilder.Build(_outfit, design, out var joints);
        CarlBuilder.PoseStanding(joints, design);
        carl.Position = new Vector3(0, deckTop, 0);
        _rig.AddChild(carl);
    }
}
