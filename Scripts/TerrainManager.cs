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
        return Mathf.Sin(z * 0.002f) * 0.18f
             + Mathf.Sin(z * 0.0008f) * 0.25f
             + Mathf.Sin(z * 0.005f) * 0.08f
             + Mathf.Sin(z * 0.012f) * 0.04f;
    }

    public float HillAt(float z)
    {
        // Flat start area, then rolling hills
        if (z < 20f) return 0f;
        float adjustedZ = z - 20f;
        float baseHill = Mathf.Sin(adjustedZ * 0.004f) * 10f
                       + Mathf.Sin(adjustedZ * 0.009f) * 5f
                       + Mathf.Sin(adjustedZ * 0.02f) * 2f;
        float downhill = -adjustedZ * 0.02f;
        return baseHill + downhill;
    }

    public void Create()
    {
        // Asphalt road — darker, more realistic
        var roadMat = new StandardMaterial3D();
        roadMat.AlbedoColor = new Color(0.25f, 0.25f, 0.28f);
        roadMat.Roughness = 0.92f;
        roadMat.SpecularMode = BaseMaterial3D.SpecularModeEnum.Disabled;
        _roadMesh = new MeshInstance3D();
        _roadMesh.MaterialOverride = roadMat;

        // Road markings — bright white-yellow
        var lineMat = new StandardMaterial3D();
        lineMat.AlbedoColor = new Color(0.92f, 0.92f, 0.78f);
        lineMat.EmissionEnabled = true;
        lineMat.Emission = new Color(0.2f, 0.2f, 0.15f);
        lineMat.SpecularMode = BaseMaterial3D.SpecularModeEnum.Disabled;
        _lineCenterMesh = new MeshInstance3D();
        _lineCenterMesh.MaterialOverride = lineMat;

        var edgeMat = new StandardMaterial3D();
        edgeMat.AlbedoColor = new Color(0.88f, 0.88f, 0.72f);
        edgeMat.SpecularMode = BaseMaterial3D.SpecularModeEnum.Disabled;
        _lineEdgeLMesh = new MeshInstance3D();
        _lineEdgeLMesh.MaterialOverride = edgeMat;
        _lineEdgeRMesh = new MeshInstance3D();
        _lineEdgeRMesh.MaterialOverride = edgeMat;

        // Dirt shoulder
        var shoulderMat = new StandardMaterial3D();
        shoulderMat.AlbedoColor = new Color(0.45f, 0.38f, 0.28f);
        shoulderMat.Roughness = 0.95f;
        shoulderMat.SpecularMode = BaseMaterial3D.SpecularModeEnum.Disabled;
        _shoulderLMesh = new MeshInstance3D();
        _shoulderLMesh.MaterialOverride = shoulderMat;
        _shoulderRMesh = new MeshInstance3D();
        _shoulderRMesh.MaterialOverride = shoulderMat;

        // Summer grass — rich green
        var grassMat = new StandardMaterial3D();
        grassMat.AlbedoColor = new Color(0.18f, 0.45f, 0.12f);
        grassMat.Roughness = 0.95f;
        grassMat.SpecularMode = BaseMaterial3D.SpecularModeEnum.Disabled;
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
        _roadMesh.Mesh = BuildRibbon(RoadW, 0f);
        _lineCenterMesh.Mesh = BuildRibbon(0.12f, 0.015f);
        _lineEdgeLMesh.Mesh = BuildRibbon(0.1f, 0.015f, -RoadW / 2f + 0.3f);
        _lineEdgeRMesh.Mesh = BuildRibbon(0.1f, 0.015f, RoadW / 2f - 0.3f);
        _shoulderLMesh.Mesh = BuildRibbon(2f, -0.05f, -RoadW / 2f - 1f);
        _shoulderRMesh.Mesh = BuildRibbon(2f, -0.05f, RoadW / 2f + 1f);
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
