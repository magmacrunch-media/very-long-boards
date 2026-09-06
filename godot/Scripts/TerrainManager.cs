using Godot;

/// <summary>
/// The road itself — seven scrolling ribbons rebuilt every physics frame from the rider's
/// distance. The shape comes entirely from a <see cref="CourseDesign"/>, so this class knows
/// how to draw a course but nothing about which one.
///
/// It takes a plain parent node rather than <see cref="Main"/> so the editor preview at
/// <c>Scenes/CoursePreview.tscn</c> can drive it without booting the game.
/// </summary>
public class TerrainManager
{
    private readonly Node3D _parent;
    private CourseDesign _design;

    private MeshInstance3D _roadMesh;
    private MeshInstance3D _lineCenterMesh;
    private MeshInstance3D _lineEdgeLMesh;
    private MeshInstance3D _lineEdgeRMesh;
    private MeshInstance3D _shoulderLMesh;
    private MeshInstance3D _shoulderRMesh;
    private MeshInstance3D _groundMesh, _groundSeaMesh;

    public float ScrollOffset = 0f;

    public CourseDesign Design { get { return _design; } }
    public float RoadW { get { return _design.RoadWidth; } }
    public float GroundW { get { return _design.GroundWidth; } }
    public int Segs { get { return _design.Segments; } }
    public int Back { get { return _design.SegmentsBehind; } }
    public float SegLen { get { return _design.SegmentLength; } }

    /// <summary>
    /// Ride a different course. Nothing has to be rebuilt: Update() re-derives every ribbon
    /// from the design each frame anyway, so the next frame is already the new road.
    /// </summary>
    public void SetDesign(CourseDesign design)
    {
        _design = design ?? new CourseDesign();
        if (_groundMesh != null) ApplyGroundMaterial();
    }

    /// <summary>
    /// The ground in this course's own colours. Frogwood's New Hampshire green and Block
    /// Island's dry moraine are the same noise at different tints.
    /// </summary>
    private void ApplyGroundMaterial()
    {
        var mat = MeshKit.Mat(
            Colors.White, texture: TextureKit.GroundFor(_design.GroundLo, _design.GroundHi));
        _groundMesh.MaterialOverride = mat;
        _groundSeaMesh.MaterialOverride = mat;
    }

    public TerrainManager(Node3D parent, CourseDesign design)
    {
        _parent = parent;
        _design = design ?? new CourseDesign();
    }

    /// <summary>Lateral drift per metre of look-ahead. Delegates to the course resource.</summary>
    public float CurveAt(float z)
    {
        return _design.CurveAt(z);
    }

    /// <summary>
    /// Road height in metres. Flat start area, then rolling hills.
    ///
    /// Steepness is amplitude x frequency, so the short-wavelength terms are what make the
    /// course feel steep — the long ones only make it tall. Total relief has to stay inside
    /// what a rider can climb on carried momentum (v^2/2g), or he bogs down on every crest
    /// and the whole ride dies. Tune the layers in the CourseDesign; physics_sim.py reads
    /// the same resource and reports whether the ride survives them.
    /// </summary>
    public float HillAt(float z)
    {
        return _design.HillAt(z);
    }

    public void Create()
    {
        // Asphalt road. Textured materials use a white albedo — the texture already carries
        // the colour, and tinting it again just muddies everything.
        var roadMat = MeshKit.Mat(Colors.White, texture: TextureKit.Asphalt);
        _roadMesh = new MeshInstance3D();
        _roadMesh.MaterialOverride = roadMat;

        // Road markings
        var lineMat = new StandardMaterial3D();
        lineMat.AlbedoColor = new Color(1f, 1f, 0.85f);
        lineMat.EmissionEnabled = true;
        lineMat.Emission = new Color(0.4f, 0.4f, 0.28f);
        lineMat.SpecularMode = BaseMaterial3D.SpecularModeEnum.Disabled;
        _lineCenterMesh = new MeshInstance3D();
        _lineCenterMesh.MaterialOverride = lineMat;

        var edgeMat = new StandardMaterial3D();
        edgeMat.AlbedoColor = new Color(0.98f, 0.98f, 0.82f);
        edgeMat.SpecularMode = BaseMaterial3D.SpecularModeEnum.Disabled;
        _lineEdgeLMesh = new MeshInstance3D();
        _lineEdgeLMesh.MaterialOverride = edgeMat;
        _lineEdgeRMesh = new MeshInstance3D();
        _lineEdgeRMesh.MaterialOverride = edgeMat;

        // Dirt shoulder
        var shoulderMat = MeshKit.Mat(Colors.White, texture: TextureKit.Dirt);
        _shoulderLMesh = new MeshInstance3D();
        _shoulderLMesh.MaterialOverride = shoulderMat;
        _shoulderRMesh = new MeshInstance3D();
        _shoulderRMesh.MaterialOverride = shoulderMat;

        _groundMesh = new MeshInstance3D();
        _groundSeaMesh = new MeshInstance3D();
        ApplyGroundMaterial();

        _parent.AddChild(_roadMesh);
        _parent.AddChild(_lineCenterMesh);
        _parent.AddChild(_lineEdgeLMesh);
        _parent.AddChild(_lineEdgeRMesh);
        _parent.AddChild(_shoulderLMesh);
        _parent.AddChild(_shoulderRMesh);
        _parent.AddChild(_groundMesh);
        _parent.AddChild(_groundSeaMesh);

        Update(0f);
    }

    /// <summary>Rebuild every ribbon for a rider standing <paramref name="distance"/> metres in.</summary>
    public void Update(float distance)
    {
        ScrollOffset = distance;
        float roadW = _design.RoadWidth;
        float shoulderX = roadW / 2f + _design.ShoulderWidth / 2f;
        float lineX = roadW / 2f - _design.EdgeLineInset;

        // uTile is repeats across the width, vMetres is metres per repeat along the road.
        _roadMesh.Mesh = BuildRibbon(roadW, 0f, uTile: 2f, vMetres: 4f);
        _lineCenterMesh.Mesh = BuildRibbon(_design.CenterLineWidth, 0.015f);
        _lineEdgeLMesh.Mesh = BuildRibbon(_design.EdgeLineWidth, 0.015f, -lineX);
        _lineEdgeRMesh.Mesh = BuildRibbon(_design.EdgeLineWidth, 0.015f, lineX);
        _shoulderLMesh.Mesh = BuildRibbon(_design.ShoulderWidth, -0.05f, -shoulderX, uTile: 1f, vMetres: 3f);
        _shoulderRMesh.Mesh = BuildRibbon(_design.ShoulderWidth, -0.05f, shoulderX, uTile: 1f, vMetres: 3f);
        // Ground. Inland it is one ribbon centred on the road, the full width either side.
        //
        // On a coast it has to be two, because the land does not go on forever in both
        // directions: it runs out at the shore. One centred ribbon 300 m wide put the water's
        // edge 150 m away and 26 m down, which from a camera five metres off the road is
        // simply not visible over the grass - the sea was there the whole time and could not
        // be seen from the road it runs beside.
        if (_design.HasSea)
        {
            _groundSeaMesh.Visible = true;
            // 5 m per repeat, which is what the landward ribbon's 60 repeats over 300 m
            // comes to - so the two halves of the same field match across the road.
            _groundSeaMesh.Mesh = BuildRibbon(0f, -0.4f, 0f, uTile: 5f, vMetres: 5f,
                outerAt: wz => _design.SeaSide * _design.ShoreAt(wz));
            _groundMesh.Mesh = BuildRibbon(_design.GroundWidth, -0.4f,
                                           -_design.SeaSide * _design.GroundWidth / 2f,
                                           uTile: 60f, vMetres: 5f);
        }
        else
        {
            _groundSeaMesh.Visible = false;
            _groundMesh.Mesh = BuildRibbon(_design.GroundWidth, -0.4f, uTile: 60f, vMetres: 5f);
        }
    }

    public void SetMeshesVisible(bool visible)
    {
        _roadMesh.Visible = visible;
        _lineCenterMesh.Visible = visible;
        _lineEdgeLMesh.Visible = visible;
        _lineEdgeRMesh.Visible = visible;
        _shoulderLMesh.Visible = visible;
        _shoulderRMesh.Visible = visible;
        _groundMesh.Visible = visible;
        _groundSeaMesh.Visible = visible && _design.HasSea;
    }

    /// <summary>
    /// One scrolling ribbon of road, shoulder, line or ground.
    ///
    /// UVs run u across the width and v along <em>world</em> z, never local z — keyed to local z
    /// the texture would slide along the tarmac as the world scrolls instead of staying stuck
    /// to it, which is glaring at speed.
    /// </summary>
    /// <summary>
    /// One scrolling ribbon. <paramref name="outerAt"/> turns it into a strip running from the
    /// road centreline out to a distance that varies along the course, which is what a shore
    /// is: the sea arrives and recedes, and a fixed-width strip cannot do that.
    ///
    /// In that mode <paramref name="uTile"/> changes meaning from "repeats across the width"
    /// to "metres per repeat", because the width is no longer fixed. A constant repeat count
    /// over a strip that runs from 40 m wide to 320 m wide stretches the texture by eight
    /// times along its length, and against the neighbouring ground at its own fixed density
    /// the seam reads as two different materials rather than as one field.
    /// </summary>
    private Mesh BuildRibbon(float width, float yOffset, float xOffset = 0f,
        float uTile = 1f, float vMetres = 4f, System.Func<float, float> outerAt = null)
    {
        int segs = _design.Segments;
        int back = _design.SegmentsBehind;
        float segLen = _design.SegmentLength;

        var st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Triangles);
        for (int i = 0; i < segs - 1; i++)
        {
            float lz0 = (i - back) * segLen;
            float lz1 = (i + 1 - back) * segLen;
            float wz0 = lz0 + ScrollOffset;
            float wz1 = lz1 + ScrollOffset;
            float cx0 = CurveAt(wz0) * lz0 + xOffset;
            float cy0 = HillAt(wz0) + yOffset;
            float cx1 = CurveAt(wz1) * lz1 + xOffset;
            float cy1 = HillAt(wz1) + yOffset;
            float v0 = wz0 / vMetres;
            float v1 = wz1 / vMetres;

            // Inner and outer edge for each end of the segment. A plain ribbon is symmetric
            // about its offset; a shore strip runs from the centreline to wherever the water
            // currently is.
            float span0 = outerAt != null ? outerAt(wz0) : width;
            float span1 = outerAt != null ? outerAt(wz1) : width;
            float in0 = outerAt != null ? cx0 : cx0 - width / 2f;
            float out0 = outerAt != null ? cx0 + span0 : cx0 + width / 2f;
            float in1 = outerAt != null ? cx1 : cx1 - width / 2f;
            float out1 = outerAt != null ? cx1 + span1 : cx1 + width / 2f;
            float u0 = outerAt != null ? Mathf.Abs(span0) / uTile : uTile;
            float u1 = outerAt != null ? Mathf.Abs(span1) / uTile : uTile;

            st.SetUV(new Vector2(0f, v0));  st.AddVertex(new Vector3(in0, cy0, lz0));
            st.SetUV(new Vector2(u0, v0));  st.AddVertex(new Vector3(out0, cy0, lz0));
            st.SetUV(new Vector2(0f, v1));  st.AddVertex(new Vector3(in1, cy1, lz1));
            st.SetUV(new Vector2(u0, v0));  st.AddVertex(new Vector3(out0, cy0, lz0));
            st.SetUV(new Vector2(u1, v1));  st.AddVertex(new Vector3(out1, cy1, lz1));
            st.SetUV(new Vector2(0f, v1));  st.AddVertex(new Vector3(in1, cy1, lz1));
        }
        st.GenerateNormals();
        return st.Commit();
    }
}
