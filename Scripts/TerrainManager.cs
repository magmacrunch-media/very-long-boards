using Godot;

public class TerrainManager
{
    private Main _main;
    private MeshInstance3D _roadMesh;
    private MeshInstance3D _lineCenterMesh;
    private MeshInstance3D _lineEdgeLMesh;
    private MeshInstance3D _lineEdgeRMesh;
    private MeshInstance3D _shoulderLMesh;
    private MeshInstance3D _shoulderRMesh;
    private MeshInstance3D _groundMesh;

    public float ScrollOffset = 0f;
    public const float RoadW = 8f;
    public const float GroundW = 300f;
    public const int Segs = 400;
    public const int Back = 70;
    public const float SegLen = 2.5f;

    public TerrainManager(Main main)
    {
        _main = main;
    }

    public float CurveAt(float z)
    {
        // Just enough straight for the countdown and the first push, then it winds.
        if (z < 60f) return 0f;
        float adjustedZ = z - 60f;
        return Mathf.Sin(adjustedZ * 0.002f) * 0.18f
             + Mathf.Sin(adjustedZ * 0.0008f) * 0.25f
             + Mathf.Sin(adjustedZ * 0.005f) * 0.08f
             + Mathf.Sin(adjustedZ * 0.012f) * 0.04f;
    }

    /// <summary>
    /// Road height in metres. Flat start area, then rolling hills.
    ///
    /// Steepness is amplitude x frequency, so the short-wavelength terms are what make the
    /// course feel steep — the long ones only make it tall. Total relief has to stay inside
    /// what a rider can climb on carried momentum (v^2/2g), or he bogs down on every crest
    /// and the whole ride dies. Tune in physics_sim.py, which mirrors this function.
    /// </summary>
    public float HillAt(float z)
    {
        if (z < 20f) return 0f;
        float adjustedZ = z - 20f;
        float baseHill = Mathf.Sin(adjustedZ * 0.005f) * 5.5f    // landscape roll, 1257 m
                       + Mathf.Sin(adjustedZ * 0.016f) * 4.0f    // long hills,      393 m
                       + Mathf.Sin(adjustedZ * 0.042f) * 1.8f    // rollers,         150 m
                       + Mathf.Sin(adjustedZ * 0.095f) * 0.9f;   // sharp pitches,    66 m
        float downhill = -adjustedZ * 0.08f;                     // net 8% grade
        return baseHill + downhill;
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

        // Summer grass
        var grassMat = MeshKit.Mat(Colors.White, texture: TextureKit.Grass);
        _groundMesh = new MeshInstance3D();
        _groundMesh.MaterialOverride = grassMat;

        _main.AddChild(_roadMesh);
        _main.AddChild(_lineCenterMesh);
        _main.AddChild(_lineEdgeLMesh);
        _main.AddChild(_lineEdgeRMesh);
        _main.AddChild(_shoulderLMesh);
        _main.AddChild(_shoulderRMesh);
        _main.AddChild(_groundMesh);

        Update();
    }

    public void Update()
    {
        ScrollOffset = _main.PlayerMgr.Distance;
        // uTile is repeats across the width, vMetres is metres per repeat along the road.
        _roadMesh.Mesh = BuildRibbon(RoadW, 0f, uTile: 2f, vMetres: 4f);
        _lineCenterMesh.Mesh = BuildRibbon(0.12f, 0.015f);
        _lineEdgeLMesh.Mesh = BuildRibbon(0.1f, 0.015f, -RoadW / 2f + 0.3f);
        _lineEdgeRMesh.Mesh = BuildRibbon(0.1f, 0.015f, RoadW / 2f - 0.3f);
        _shoulderLMesh.Mesh = BuildRibbon(2f, -0.05f, -RoadW / 2f - 1f, uTile: 1f, vMetres: 3f);
        _shoulderRMesh.Mesh = BuildRibbon(2f, -0.05f, RoadW / 2f + 1f, uTile: 1f, vMetres: 3f);
        _groundMesh.Mesh = BuildRibbon(GroundW, -0.4f, uTile: 60f, vMetres: 5f);
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
    }

    /// <summary>
    /// One scrolling ribbon of road, shoulder, line or ground.
    ///
    /// UVs run u across the width and v along <em>world</em> z, never local z — keyed to local z
    /// the texture would slide along the tarmac as the world scrolls instead of staying stuck
    /// to it, which is glaring at speed.
    /// </summary>
    private Mesh BuildRibbon(float width, float yOffset, float xOffset = 0f,
        float uTile = 1f, float vMetres = 4f)
    {
        var st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Triangles);
        for (int i = 0; i < Segs - 1; i++)
        {
            float lz0 = (i - Back) * SegLen;
            float lz1 = (i + 1 - Back) * SegLen;
            float wz0 = lz0 + ScrollOffset;
            float wz1 = lz1 + ScrollOffset;
            float cx0 = CurveAt(wz0) * lz0 + xOffset;
            float cy0 = HillAt(wz0) + yOffset;
            float cx1 = CurveAt(wz1) * lz1 + xOffset;
            float cy1 = HillAt(wz1) + yOffset;
            float hw = width / 2f;
            float v0 = wz0 / vMetres;
            float v1 = wz1 / vMetres;

            st.SetUV(new Vector2(0f, v0));    st.AddVertex(new Vector3(cx0 - hw, cy0, lz0));
            st.SetUV(new Vector2(uTile, v0)); st.AddVertex(new Vector3(cx0 + hw, cy0, lz0));
            st.SetUV(new Vector2(0f, v1));    st.AddVertex(new Vector3(cx1 - hw, cy1, lz1));
            st.SetUV(new Vector2(uTile, v0)); st.AddVertex(new Vector3(cx0 + hw, cy0, lz0));
            st.SetUV(new Vector2(uTile, v1)); st.AddVertex(new Vector3(cx1 + hw, cy1, lz1));
            st.SetUV(new Vector2(0f, v1));    st.AddVertex(new Vector3(cx1 - hw, cy1, lz1));
        }
        st.GenerateNormals();
        return st.Commit();
    }
}
