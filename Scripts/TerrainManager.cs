using Godot;

public class TerrainManager
{
    private Main _main;
    private MeshInstance3D _roadMesh;
    private MeshInstance3D _lineCenterMesh;
    private MeshInstance3D _lineEdgeLMesh;
    private MeshInstance3D _lineEdgeRMesh;
    private MeshInstance3D _groundMesh;

    public float ScrollOffset = 0f;
    public const float RoadW = 8f;
    public const float GroundW = 300f;
    public const int Segs = 350;
    public const int Back = 60;
    public const float SegLen = 3f;

    public TerrainManager(Main main)
    {
        _main = main;
    }

    public float CurveAt(float z)
    {
        return Mathf.Sin(z * 0.002f) * 0.15f
             + Mathf.Sin(z * 0.0008f) * 0.22f
             + Mathf.Sin(z * 0.005f) * 0.06f
             + Mathf.Sin(z * 0.012f) * 0.03f;
    }

    public float HillAt(float z)
    {
        return -z * 0.08f
             + Mathf.Sin(z * 0.003f) * 5f
             + Mathf.Sin(z * 0.008f) * 2f
             + Mathf.Sin(z * 0.015f) * 1f;
    }

    public void Create()
    {
        _roadMesh = MakeMesh(new Color(0.35f, 0.35f, 0.38f));
        _lineCenterMesh = MakeMesh(new Color(0.85f, 0.85f, 0.72f));
        _lineEdgeLMesh = MakeMesh(new Color(0.8f, 0.8f, 0.68f));
        _lineEdgeRMesh = MakeMesh(new Color(0.8f, 0.8f, 0.68f));
        _groundMesh = MakeMesh(new Color(0.24f, 0.5f, 0.18f));

        _main.AddChild(_roadMesh);
        _main.AddChild(_lineCenterMesh);
        _main.AddChild(_lineEdgeLMesh);
        _main.AddChild(_lineEdgeRMesh);
        _main.AddChild(_groundMesh);

        Update();
    }

    public void Update()
    {
        ScrollOffset = _main.PlayerMgr.Distance;
        _roadMesh.Mesh = BuildRibbon(RoadW, 0f);
        _lineCenterMesh.Mesh = BuildRibbon(0.12f, 0.015f);
        _lineEdgeLMesh.Mesh = BuildRibbon(0.1f, 0.015f, -RoadW / 2f + 0.3f);
        _lineEdgeRMesh.Mesh = BuildRibbon(0.1f, 0.015f, RoadW / 2f - 0.3f);
        _groundMesh.Mesh = BuildRibbon(GroundW, -0.4f);
    }

    private MeshInstance3D MakeMesh(Color color)
    {
        var m = new MeshInstance3D();
        var mat = new StandardMaterial3D();
        mat.AlbedoColor = color;
        mat.SpecularMode = BaseMaterial3D.SpecularModeEnum.Disabled;
        m.MaterialOverride = mat;
        return m;
    }

    private Mesh BuildRibbon(float width, float yOffset, float xOffset = 0f)
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

            st.AddVertex(new Vector3(cx0 - hw, cy0, lz0));
            st.AddVertex(new Vector3(cx0 + hw, cy0, lz0));
            st.AddVertex(new Vector3(cx1 - hw, cy1, lz1));
            st.AddVertex(new Vector3(cx0 + hw, cy0, lz0));
            st.AddVertex(new Vector3(cx1 + hw, cy1, lz1));
            st.AddVertex(new Vector3(cx1 - hw, cy1, lz1));
        }
        st.GenerateNormals();
        return st.Commit();
    }
}
