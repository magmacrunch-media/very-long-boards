using Godot;

public class GarageManager
{
    private Main _main;
    private Node3D _garageRoot;

    // Display models
    private Node3D _charDisplay;
    private Node3D _boardDisplay;

    // Level posters
    private MeshInstance3D _posterLeft;
    private MeshInstance3D _posterRight;
    private Label3D _posterLeftText;
    private Label3D _posterRightText;
    private Label3D _posterLeftSubtext;
    private Label3D _posterRightSubtext;
    private StandardMaterial3D _posterSelectedMat;
    private StandardMaterial3D _posterDimmedMat;

    // Camera lerp state
    private Vector3 _camPos;
    private Vector3 _camLookAt;
    private bool _camInitialized = false;

    private const float CamLerpSpeed = 3f;

    public GarageManager(Main main)
    {
        _main = main;
    }

    public void Create()
    {
        _garageRoot = new Node3D();
        _garageRoot.Visible = false;
        _main.AddChild(_garageRoot);

        BuildRoom();
        BuildWorkbench();
        BuildPegboard();
        BuildShelf();
        BuildWindow();
        BuildNeonSign();
        BuildLeaningBoard();
        BuildLight();
        BuildLevelPosters();

        // Character and board displays
        _charDisplay = new Node3D();
        _charDisplay.Position = new Vector3(-2f, 0f, 0.5f);
        _garageRoot.AddChild(_charDisplay);

        _boardDisplay = new Node3D();
        _boardDisplay.Position = new Vector3(2f, 0.6f, 0.5f);
        _garageRoot.AddChild(_boardDisplay);

        _posterSelectedMat = new StandardMaterial3D();
        _posterSelectedMat.AlbedoColor = new Color(0.95f, 0.92f, 0.85f);
        _posterSelectedMat.EmissionEnabled = true;
        _posterSelectedMat.Emission = new Color(0.15f, 0.12f, 0.08f);

        _posterDimmedMat = new StandardMaterial3D();
        _posterDimmedMat.AlbedoColor = new Color(0.55f, 0.52f, 0.48f);
        _posterDimmedMat.SpecularMode = BaseMaterial3D.SpecularModeEnum.Disabled;

        UpdateDisplayModel();
        UpdatePosterHighlight();
    }

    // ═══════════════════════════════════════════
    //  ROOM STRUCTURE
    // ═══════════════════════════════════════════

    private void BuildRoom()
    {
        // Back wall
        AddBox(new Vector3(12f, 5f, 0.3f), new Color(0.23f, 0.23f, 0.23f),
            new Vector3(0, 2.5f, -3f));

        // Cinder block texture — horizontal grooves
        var grooveMat = new StandardMaterial3D();
        grooveMat.AlbedoColor = new Color(0.2f, 0.2f, 0.2f);
        grooveMat.SpecularMode = BaseMaterial3D.SpecularModeEnum.Disabled;
        for (int row = 0; row < 6; row++)
        {
            float y = row * 0.8f + 0.4f;
            AddBox(new Vector3(12f, 0.02f, 0.01f), grooveMat.AlbedoColor,
                new Vector3(0, y, -2.83f));
        }
        // Vertical grooves
        for (int col = 0; col < 8; col++)
        {
            float x = -5f + col * 1.5f;
            int offset = col % 2;
            for (int row = 0; row < 6; row++)
            {
                float y = row * 0.8f + 0.4f;
                float xOffset = (row % 2 == 0) ? 0f : 0.75f;
                AddBox(new Vector3(0.02f, 0.8f, 0.01f), grooveMat.AlbedoColor,
                    new Vector3(x + xOffset, y, -2.83f));
            }
        }

        // Floor
        AddBox(new Vector3(12f, 0.1f, 8f), new Color(0.33f, 0.33f, 0.33f),
            new Vector3(0, -0.05f, 1f));

        // Floor perspective lines
        var lineMat = new StandardMaterial3D();
        lineMat.AlbedoColor = new Color(0.3f, 0.3f, 0.3f);
        lineMat.SpecularMode = BaseMaterial3D.SpecularModeEnum.Disabled;
        for (int i = 0; i < 6; i++)
        {
            float z = -2f + i * 1.5f;
            AddBox(new Vector3(12f, 0.005f, 0.02f), lineMat.AlbedoColor,
                new Vector3(0, 0.01f, z));
        }

        // Oil stain
        var stainMat = new StandardMaterial3D();
        stainMat.AlbedoColor = new Color(0.18f, 0.18f, 0.18f, 0.4f);
        stainMat.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
        stainMat.SpecularMode = BaseMaterial3D.SpecularModeEnum.Disabled;
        var stain = new MeshInstance3D();
        var stainMesh = new CylinderMesh();
        stainMesh.TopRadius = 0.5f;
        stainMesh.BottomRadius = 0.5f;
        stainMesh.Height = 0.01f;
        stainMesh.RadialSegments = 6;
        stain.Mesh = stainMesh;
        stain.MaterialOverride = stainMat;
        stain.Position = new Vector3(1f, 0.01f, 1.5f);
        _garageRoot.AddChild(stain);

        // Ceiling
        AddBox(new Vector3(12f, 0.15f, 8f), new Color(0.2f, 0.2f, 0.2f),
            new Vector3(0, 5.05f, 1f));
    }

    private void BuildWorkbench()
    {
        var woodMat = new Color(0.43f, 0.3f, 0.16f);
        var darkWoodMat = new Color(0.35f, 0.24f, 0.12f);

        // Table top
        AddBox(new Vector3(2.5f, 0.1f, 0.8f), woodMat,
            new Vector3(-3.5f, 0.85f, -1.5f));

        // Legs
        AddBox(new Vector3(0.08f, 0.85f, 0.08f), darkWoodMat,
            new Vector3(-4.6f, 0.42f, -1.5f));
        AddBox(new Vector3(0.08f, 0.85f, 0.08f), darkWoodMat,
            new Vector3(-2.4f, 0.42f, -1.5f));

        // Shelf
        AddBox(new Vector3(2.2f, 0.06f, 0.6f), darkWoodMat,
            new Vector3(-3.5f, 0.35f, -1.5f));

        // Items on workbench
        AddBox(new Vector3(0.15f, 0.12f, 0.12f), new Color(0.55f, 0.55f, 0.55f),
            new Vector3(-4.2f, 0.97f, -1.5f)); // box
        AddBox(new Vector3(0.1f, 0.08f, 0.1f), new Color(0.4f, 0.4f, 0.4f),
            new Vector3(-3.8f, 0.94f, -1.5f)); // can
        AddBox(new Vector3(0.3f, 0.1f, 0.15f), new Color(0.6f, 0.6f, 0.6f),
            new Vector3(-3.2f, 0.95f, -1.5f)); // toolbox
        AddBox(new Vector3(0.15f, 0.04f, 0.06f), new Color(1f, 0.42f, 0.21f),
            new Vector3(-3.2f, 1.01f, -1.5f)); // toolbox handle
    }

    private void BuildPegboard()
    {
        // Pegboard panel
        var boardMat = new Color(0.28f, 0.28f, 0.28f);
        AddBox(new Vector3(1.8f, 1.2f, 0.05f), boardMat,
            new Vector3(-1.5f, 3f, -2.8f));

        // Hammer
        AddBox(new Vector3(0.04f, 0.5f, 0.04f), new Color(0.43f, 0.3f, 0.16f),
            new Vector3(-2f, 3.2f, -2.7f)); // handle
        AddBox(new Vector3(0.2f, 0.08f, 0.08f), new Color(0.55f, 0.55f, 0.55f),
            new Vector3(-2f, 3.5f, -2.7f)); // head

        // Wrench
        AddBox(new Vector3(0.04f, 0.5f, 0.04f), new Color(0.6f, 0.6f, 0.6f),
            new Vector3(-1.5f, 3.1f, -2.7f)); // handle
        AddBox(new Vector3(0.14f, 0.08f, 0.08f), new Color(0.6f, 0.6f, 0.6f),
            new Vector3(-1.5f, 3.4f, -2.7f)); // head

        // Screwdriver
        AddBox(new Vector3(0.03f, 0.35f, 0.03f), new Color(1f, 0.18f, 0.61f),
            new Vector3(-1f, 3.15f, -2.7f)); // handle
        AddBox(new Vector3(0.02f, 0.15f, 0.02f), new Color(0.8f, 0.8f, 0.8f),
            new Vector3(-1f, 3.4f, -2.7f)); // shaft
    }

    private void BuildShelf()
    {
        var woodMat = new Color(0.35f, 0.24f, 0.12f);

        // Shelf planks
        AddBox(new Vector3(2f, 0.06f, 0.4f), woodMat,
            new Vector3(3f, 3.5f, -2.5f));
        // Vertical supports
        AddBox(new Vector3(0.06f, 0.8f, 0.06f), woodMat,
            new Vector3(2.1f, 3.1f, -2.5f));
        AddBox(new Vector3(0.06f, 0.8f, 0.06f), woodMat,
            new Vector3(3.9f, 3.1f, -2.5f));

        // Items on shelf
        AddBox(new Vector3(0.12f, 0.22f, 0.12f), new Color(0.29f, 0.56f, 0.85f),
            new Vector3(2.4f, 3.65f, -2.5f)); // blue bottle
        AddBox(new Vector3(0.12f, 0.18f, 0.12f), new Color(1f, 0.27f, 0.27f),
            new Vector3(2.8f, 3.63f, -2.5f)); // red can
        AddBox(new Vector3(0.14f, 0.2f, 0.14f), new Color(0.27f, 0.8f, 0.27f),
            new Vector3(3.2f, 3.64f, -2.5f)); // green bottle
        AddBox(new Vector3(0.2f, 0.14f, 0.14f), new Color(1f, 0.67f, 0f),
            new Vector3(3.6f, 3.6f, -2.5f)); // orange box
    }

    private void BuildWindow()
    {
        // Window frame
        var frameMat = new Color(0.33f, 0.33f, 0.33f);
        AddBox(new Vector3(1.2f, 1.4f, 0.08f), frameMat,
            new Vector3(4f, 3.2f, -2.8f));

        // Window glass
        var glassMat = new StandardMaterial3D();
        glassMat.AlbedoColor = new Color(0.48f, 0.68f, 0.8f, 0.85f);
        glassMat.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
        AddBox(new Vector3(1f, 1.2f, 0.02f), glassMat.AlbedoColor,
            new Vector3(4f, 3.2f, -2.76f));

        // Cross bars
        AddBox(new Vector3(0.04f, 1.2f, 0.04f), frameMat,
            new Vector3(4f, 3.2f, -2.74f)); // vertical
        AddBox(new Vector3(1f, 0.04f, 0.04f), frameMat,
            new Vector3(4f, 3.2f, -2.74f)); // horizontal

        // Light glow from window (on floor)
        var glowMat = new StandardMaterial3D();
        glowMat.AlbedoColor = new Color(0.48f, 0.68f, 0.8f, 0.06f);
        glowMat.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
        glowMat.SpecularMode = BaseMaterial3D.SpecularModeEnum.Disabled;
        AddBox(new Vector3(1.5f, 0.005f, 2f), glowMat.AlbedoColor,
            new Vector3(4f, 0.01f, 0.5f));
    }

    private void BuildNeonSign()
    {
        // VLB sign using Label3D
        var sign = new Label3D();
        sign.Text = "VLB";
        sign.FontSize = 20;
        sign.OutlineSize = 0;
        var signMat = new StandardMaterial3D();
        signMat.AlbedoColor = new Color(1f, 0.18f, 0.61f);
        signMat.EmissionEnabled = true;
        signMat.Emission = new Color(1f, 0.18f, 0.61f);
        signMat.EmissionEnergyMultiplier = 2f;
        sign.MaterialOverride = signMat;
        sign.Position = new Vector3(-0.5f, 4.2f, -2.8f);
        _garageRoot.AddChild(sign);

        // Tagline
        var tagline = new Label3D();
        tagline.Text = "DOWNHILL SKATEBOARDS";
        tagline.FontSize = 6;
        var tagMat = new StandardMaterial3D();
        tagMat.AlbedoColor = new Color(1f, 0.18f, 0.61f, 0.5f);
        tagMat.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
        tagMat.EmissionEnabled = true;
        tagMat.Emission = new Color(0.3f, 0.05f, 0.18f);
        tagline.MaterialOverride = tagMat;
        tagline.Position = new Vector3(-0.5f, 3.9f, -2.8f);
        _garageRoot.AddChild(tagline);
    }

    private void BuildLeaningBoard()
    {
        // A simple board leaning against the right wall
        var deckMat = new StandardMaterial3D();
        deckMat.AlbedoColor = new Color(0.55f, 0.27f, 0.1f);
        deckMat.SpecularMode = BaseMaterial3D.SpecularModeEnum.Disabled;

        var deck = new MeshInstance3D();
        var deckMesh = new BoxMesh();
        deckMesh.Size = new Vector3(0.4f, 0.03f, 1.2f);
        deck.Mesh = deckMesh;
        deck.MaterialOverride = deckMat;
        deck.Position = new Vector3(5.3f, 0.8f, -1f);
        deck.Rotation = new Vector3(-0.15f, 0f, -0.15f);
        _garageRoot.AddChild(deck);

        // Wheels
        var wheelMat = new StandardMaterial3D();
        wheelMat.AlbedoColor = new Color(0.12f, 0.12f, 0.12f);
        foreach (var pos in new[] {
            new Vector3(-0.15f, -0.03f, 0.35f),
            new Vector3(0.15f, -0.03f, 0.35f),
            new Vector3(-0.15f, -0.03f, -0.35f),
            new Vector3(0.15f, -0.03f, -0.35f) })
        {
            var wheel = new MeshInstance3D();
            var wMesh = new CylinderMesh();
            wMesh.TopRadius = 0.04f;
            wMesh.BottomRadius = 0.04f;
            wMesh.Height = 0.05f;
            wMesh.RadialSegments = 6;
            wheel.Mesh = wMesh;
            wheel.MaterialOverride = wheelMat;
            wheel.Position = deck.Position + pos;
            wheel.Rotation = new Vector3(0, 0, Mathf.Pi / 2f);
            _garageRoot.AddChild(wheel);
        }
    }

    private void BuildLight()
    {
        // Fluorescent light fixture on ceiling
        AddBox(new Vector3(1.5f, 0.06f, 0.2f), new Color(0.85f, 0.85f, 0.85f),
            new Vector3(-1f, 5f, -1f));

        // Light cone (semi-transparent box)
        var coneMat = new StandardMaterial3D();
        coneMat.AlbedoColor = new Color(1f, 0.98f, 0.94f, 0.08f);
        coneMat.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
        coneMat.SpecularMode = BaseMaterial3D.SpecularModeEnum.Disabled;
        AddBox(new Vector3(2f, 4.9f, 3f), coneMat.AlbedoColor,
            new Vector3(-1f, 2.5f, -0.5f));

        // Actual light
        var light = new OmniLight3D();
        light.Position = new Vector3(-1f, 4.8f, -1f);
        light.LightEnergy = 1.2f;
        light.LightColor = new Color(1f, 0.98f, 0.94f);
        light.OmniRange = 10f;
        light.OmniAttenuation = 0.8f;
        _garageRoot.AddChild(light);
    }

    // ═══════════════════════════════════════════
    //  LEVEL POSTERS
    // ═══════════════════════════════════════════

    private void BuildLevelPosters()
    {
        // Left poster — Frogwood, NH
        _posterLeft = AddBox(new Vector3(1.4f, 1.8f, 0.05f),
            new Color(0.95f, 0.92f, 0.85f),
            new Vector3(0.8f, 3f, -2.78f));

        // Left poster header strip
        AddBox(new Vector3(1.3f, 0.2f, 0.01f), new Color(0.18f, 0.52f, 0.18f),
            new Vector3(0.8f, 3.75f, -2.74f));

        _posterLeftText = new Label3D();
        _posterLeftText.Text = "FROGWOOD";
        _posterLeftText.FontSize = 10;
        var leftTextMat = new StandardMaterial3D();
        leftTextMat.AlbedoColor = new Color(0.15f, 0.15f, 0.15f);
        _posterLeftText.MaterialOverride = leftTextMat;
        _posterLeftText.Position = new Vector3(0.8f, 3.4f, -2.73f);
        _garageRoot.AddChild(_posterLeftText);

        _posterLeftSubtext = new Label3D();
        _posterLeftSubtext.Text = "NH";
        _posterLeftSubtext.FontSize = 7;
        _posterLeftSubtext.MaterialOverride = leftTextMat;
        _posterLeftSubtext.Position = new Vector3(0.8f, 3.15f, -2.73f);
        _garageRoot.AddChild(_posterLeftSubtext);

        // Right poster — Block Island
        _posterRight = AddBox(new Vector3(1.4f, 1.8f, 0.05f),
            new Color(0.55f, 0.52f, 0.48f),
            new Vector3(2.8f, 3f, -2.78f));

        // Right poster header strip
        AddBox(new Vector3(1.3f, 0.2f, 0.01f), new Color(0.3f, 0.4f, 0.6f),
            new Vector3(2.8f, 3.75f, -2.74f));

        _posterRightText = new Label3D();
        _posterRightText.Text = "BLOCK ISLAND";
        _posterRightText.FontSize = 8;
        var rightTextMat = new StandardMaterial3D();
        rightTextMat.AlbedoColor = new Color(0.35f, 0.35f, 0.35f);
        _posterRightText.MaterialOverride = rightTextMat;
        _posterRightText.Position = new Vector3(2.8f, 3.4f, -2.73f);
        _garageRoot.AddChild(_posterRightText);

        _posterRightSubtext = new Label3D();
        _posterRightSubtext.Text = "COMING SOON";
        _posterRightSubtext.FontSize = 4;
        var comingSoonMat = new StandardMaterial3D();
        comingSoonMat.AlbedoColor = new Color(0.5f, 0.3f, 0.3f);
        comingSoonMat.EmissionEnabled = true;
        comingSoonMat.Emission = new Color(0.2f, 0.1f, 0.1f);
        _posterRightSubtext.MaterialOverride = comingSoonMat;
        _posterRightSubtext.Position = new Vector3(2.8f, 3.15f, -2.73f);
        _garageRoot.AddChild(_posterRightSubtext);
    }

    public void UpdatePosterHighlight()
    {
        if (_posterLeft == null || _posterRight == null) return;

        bool leftSelected = _main.Level == Main.LevelType.FrogwoodNH;
        _posterLeft.MaterialOverride = leftSelected ? _posterSelectedMat : _posterDimmedMat;
        _posterRight.MaterialOverride = leftSelected ? _posterDimmedMat : _posterSelectedMat;

        // Update text colors based on selection
        if (_posterLeftText?.MaterialOverride is StandardMaterial3D lt)
            lt.AlbedoColor = leftSelected ? new Color(0.1f, 0.1f, 0.1f) : new Color(0.4f, 0.4f, 0.4f);
        if (_posterLeftSubtext?.MaterialOverride is StandardMaterial3D ls)
            ls.AlbedoColor = leftSelected ? new Color(0.1f, 0.1f, 0.1f) : new Color(0.4f, 0.4f, 0.4f);
        if (_posterRightText?.MaterialOverride is StandardMaterial3D rt)
            rt.AlbedoColor = leftSelected ? new Color(0.4f, 0.4f, 0.4f) : new Color(0.1f, 0.1f, 0.1f);
    }

    // ═══════════════════════════════════════════
    //  DISPLAY MODELS
    // ═══════════════════════════════════════════

    public void UpdateDisplayModel()
    {
        BuildCharDisplay();
        BuildBoardDisplay();
    }

    private void BuildCharDisplay()
    {
        // Clear old
        foreach (var child in _charDisplay.GetChildren())
            child.QueueFree();

        var skinMat = new StandardMaterial3D();
        skinMat.AlbedoColor = new Color(0.9f, 0.78f, 0.6f);
        skinMat.SpecularMode = BaseMaterial3D.SpecularModeEnum.Disabled;

        var shirtMat = new StandardMaterial3D();
        shirtMat.AlbedoColor = Main.CarlShirtColors[(int)_main.Carl];
        shirtMat.SpecularMode = BaseMaterial3D.SpecularModeEnum.Disabled;

        var pantsMat = new StandardMaterial3D();
        pantsMat.AlbedoColor = Main.CarlPantsColors[(int)_main.Carl];
        pantsMat.SpecularMode = BaseMaterial3D.SpecularModeEnum.Disabled;

        var shoeMat = new StandardMaterial3D();
        shoeMat.AlbedoColor = new Color(0.14f, 0.14f, 0.14f);

        var hairMat = new StandardMaterial3D();
        hairMat.AlbedoColor = new Color(0.3f, 0.18f, 0.08f);

        // Body group (rotated for sideways stance)
        var bodyGroup = new Node3D();
        bodyGroup.Rotation = new Vector3(0, -Mathf.Pi / 2f, 0);
        _charDisplay.AddChild(bodyGroup);

        // Hip
        var hip = new Node3D();
        hip.Position = new Vector3(0, 0.2f, 0);
        bodyGroup.AddChild(hip);

        // Spine (torso)
        var spine = new Node3D();
        spine.Position = new Vector3(0, 0.35f, 0);
        hip.AddChild(spine);

        AddCylinderTo(spine, 0.13f, 0.11f, 0.18f, shirtMat, new Vector3(0, 0.09f, 0));
        AddCylinderTo(spine, 0.15f, 0.13f, 0.22f, shirtMat, new Vector3(0, 0.29f, -0.02f));
        AddCylinderTo(spine, 0.16f, 0.15f, 0.06f, shirtMat, new Vector3(0, 0.42f, -0.02f));

        // Neck + head
        var neck = new Node3D();
        neck.Position = new Vector3(0, 0.46f, -0.02f);
        spine.AddChild(neck);
        AddCylinderTo(neck, 0.05f, 0.06f, 0.08f, skinMat, new Vector3(0, 0.04f, 0));

        var head = new MeshInstance3D();
        var headMesh = new SphereMesh();
        headMesh.Radius = 0.12f;
        headMesh.Height = 0.24f;
        headMesh.Rings = 6;
        headMesh.RadialSegments = 8;
        head.Mesh = headMesh;
        head.MaterialOverride = skinMat;
        head.Position = new Vector3(0, 0.14f, -0.02f);
        neck.AddChild(head);

        var hair = new MeshInstance3D();
        var hairMesh = new CylinderMesh();
        hairMesh.TopRadius = 0.11f;
        hairMesh.BottomRadius = 0.13f;
        hairMesh.Height = 0.06f;
        hairMesh.RadialSegments = 8;
        hair.Mesh = hairMesh;
        hair.MaterialOverride = hairMat;
        hair.Position = new Vector3(0, 0.24f, -0.02f);
        neck.AddChild(hair);

        // Arms
        var armL = new Node3D();
        armL.Position = new Vector3(-0.17f, 0.4f, -0.02f);
        armL.Rotation = new Vector3(0.1f, 0, -0.6f); // arms out
        spine.AddChild(armL);
        AddCylinderTo(armL, 0.035f, 0.03f, 0.2f, skinMat, new Vector3(0, -0.1f, 0));
        var forearmL = new Node3D();
        forearmL.Position = new Vector3(0, -0.2f, 0);
        armL.AddChild(forearmL);
        AddCylinderTo(forearmL, 0.03f, 0.025f, 0.18f, skinMat, new Vector3(0, -0.09f, 0));

        var armR = new Node3D();
        armR.Position = new Vector3(0.17f, 0.4f, -0.02f);
        armR.Rotation = new Vector3(0.1f, 0, 0.6f); // arms out
        spine.AddChild(armR);
        AddCylinderTo(armR, 0.035f, 0.03f, 0.2f, skinMat, new Vector3(0, -0.1f, 0));
        var forearmR = new Node3D();
        forearmR.Position = new Vector3(0, -0.2f, 0);
        armR.AddChild(forearmR);
        AddCylinderTo(forearmR, 0.03f, 0.025f, 0.18f, skinMat, new Vector3(0, -0.09f, 0));

        // Legs
        var legL = new Node3D();
        legL.Position = new Vector3(-0.08f, 0f, 0);
        hip.AddChild(legL);
        AddCylinderTo(legL, 0.06f, 0.055f, 0.22f, pantsMat, new Vector3(0, -0.11f, 0));
        var kneeL = new Node3D();
        kneeL.Position = new Vector3(0, -0.22f, 0);
        legL.AddChild(kneeL);
        AddCylinderTo(kneeL, 0.05f, 0.045f, 0.2f, pantsMat, new Vector3(0, -0.1f, 0));
        var shoeL = new MeshInstance3D();
        var shoeLMesh = new BoxMesh();
        shoeLMesh.Size = new Vector3(0.1f, 0.06f, 0.22f);
        shoeL.Mesh = shoeLMesh;
        shoeL.MaterialOverride = shoeMat;
        shoeL.Position = new Vector3(0, -0.22f, -0.02f);
        kneeL.AddChild(shoeL);

        var legR = new Node3D();
        legR.Position = new Vector3(0.08f, 0f, 0);
        hip.AddChild(legR);
        AddCylinderTo(legR, 0.06f, 0.055f, 0.22f, pantsMat, new Vector3(0, -0.11f, 0));
        var kneeR = new Node3D();
        kneeR.Position = new Vector3(0, -0.22f, 0);
        legR.AddChild(kneeR);
        AddCylinderTo(kneeR, 0.05f, 0.045f, 0.2f, pantsMat, new Vector3(0, -0.1f, 0));
        var shoeR = new MeshInstance3D();
        var shoeRMesh = new BoxMesh();
        shoeRMesh.Size = new Vector3(0.1f, 0.06f, 0.22f);
        shoeR.Mesh = shoeRMesh;
        shoeR.MaterialOverride = shoeMat;
        shoeR.Position = new Vector3(0, -0.22f, -0.02f);
        kneeR.AddChild(shoeR);
    }

    private void BuildBoardDisplay()
    {
        foreach (var child in _boardDisplay.GetChildren())
            child.QueueFree();

        var deckMat = new StandardMaterial3D();
        deckMat.AlbedoColor = Main.BoardDeckColors[(int)_main.Board];
        deckMat.SpecularMode = BaseMaterial3D.SpecularModeEnum.Disabled;

        var gripMat = new StandardMaterial3D();
        gripMat.AlbedoColor = Main.BoardGripColors[(int)_main.Board];

        var truckMat = new StandardMaterial3D();
        truckMat.AlbedoColor = new Color(0.62f, 0.62f, 0.65f);

        var wheelMat = new StandardMaterial3D();
        wheelMat.AlbedoColor = new Color(0.12f, 0.12f, 0.12f);

        // Deck center
        AddBoxTo(_boardDisplay, new Vector3(0.62f, 0.045f, 1.4f), deckMat,
            new Vector3(0, 0, 0));
        // Nose
        AddBoxTo(_boardDisplay, new Vector3(0.48f, 0.04f, 0.4f), deckMat,
            new Vector3(0, 0, 0.9f));
        // Tail
        AddBoxTo(_boardDisplay, new Vector3(0.48f, 0.04f, 0.35f), deckMat,
            new Vector3(0, 0, -0.88f));
        // Grip
        AddBoxTo(_boardDisplay, new Vector3(0.58f, 0.015f, 1.3f), gripMat,
            new Vector3(0, 0.03f, 0));

        // Trucks
        AddBoxTo(_boardDisplay, new Vector3(0.18f, 0.04f, 0.14f), truckMat,
            new Vector3(0, -0.05f, 0.55f));
        AddBoxTo(_boardDisplay, new Vector3(0.18f, 0.04f, 0.14f), truckMat,
            new Vector3(0, -0.05f, -0.55f));

        // Wheels
        foreach (var pos in new[] {
            new Vector3(-0.30f, -0.08f, 0.55f),
            new Vector3(0.30f, -0.08f, 0.55f),
            new Vector3(-0.30f, -0.08f, -0.55f),
            new Vector3(0.30f, -0.08f, -0.55f) })
        {
            var wheel = new MeshInstance3D();
            var wMesh = new CylinderMesh();
            wMesh.TopRadius = 0.055f;
            wMesh.BottomRadius = 0.055f;
            wMesh.Height = 0.07f;
            wMesh.RadialSegments = 6;
            wheel.Mesh = wMesh;
            wheel.MaterialOverride = wheelMat;
            wheel.Position = pos;
            wheel.Rotation = new Vector3(0, 0, Mathf.Pi / 2f);
            _boardDisplay.AddChild(wheel);
        }
    }

    // ═══════════════════════════════════════════
    //  VISIBILITY
    // ═══════════════════════════════════════════

    public void Show()
    {
        _garageRoot.Visible = true;
        // Hide terrain meshes
        _main.Terrain.SetMeshesVisible(false);
        // Hide scenery
        _main.Scenery.SetItemsVisible(false);
        // Dim sun
        _main.GetNode<DirectionalLight3D>("Sun").LightEnergy = 0.1f;
    }

    public void Hide()
    {
        _garageRoot.Visible = false;
        _main.Terrain.SetMeshesVisible(true);
        _main.Scenery.SetItemsVisible(true);
        _main.GetNode<DirectionalLight3D>("Sun").LightEnergy = 1.4f;
    }

    // ═══════════════════════════════════════════
    //  CAMERA
    // ═══════════════════════════════════════════

    public void UpdateCamera(float dt, bool isCharSelect, bool isBoardSelect, bool isLevelSelect)
    {
        Vector3 targetPos;
        Vector3 targetLook;

        if (isCharSelect)
        {
            targetPos = new Vector3(3f, 2.5f, 3.5f);
            targetLook = new Vector3(-1.5f, 1.2f, -1f);
        }
        else if (isBoardSelect)
        {
            targetPos = new Vector3(-3f, 2f, 3.5f);
            targetLook = new Vector3(1.5f, 0.6f, -1f);
        }
        else // level select
        {
            targetPos = new Vector3(0f, 2.8f, 4f);
            targetLook = new Vector3(1.5f, 3f, -2.5f);
        }

        if (!_camInitialized)
        {
            _camPos = targetPos;
            _camLookAt = targetLook;
            _camInitialized = true;
        }

        _camPos = _camPos.Lerp(targetPos, CamLerpSpeed * dt);
        _camLookAt = _camLookAt.Lerp(targetLook, CamLerpSpeed * dt);

        _main.CameraMount.Position = _camPos;
        var cam = _main.CameraMount.GetNode<Camera3D>("Camera3D");
        cam.LookAt(_camLookAt, Vector3.Up);
        cam.Rotation = Vector3.Zero;
        cam.Fov = 55f;
    }

    // ═══════════════════════════════════════════
    //  MESH HELPERS
    // ═══════════════════════════════════════════

    private MeshInstance3D AddBox(Vector3 size, Color color, Vector3 pos)
    {
        var m = new MeshInstance3D();
        var mesh = new BoxMesh();
        mesh.Size = size;
        m.Mesh = mesh;
        var mat = new StandardMaterial3D();
        mat.AlbedoColor = color;
        mat.SpecularMode = BaseMaterial3D.SpecularModeEnum.Disabled;
        m.MaterialOverride = mat;
        m.Position = pos;
        _garageRoot.AddChild(m);
        return m;
    }

    private MeshInstance3D AddBox(Node3D parent, Vector3 size, Color color, Vector3 pos)
    {
        var m = new MeshInstance3D();
        var mesh = new BoxMesh();
        mesh.Size = size;
        m.Mesh = mesh;
        var mat = new StandardMaterial3D();
        mat.AlbedoColor = color;
        mat.SpecularMode = BaseMaterial3D.SpecularModeEnum.Disabled;
        m.MaterialOverride = mat;
        m.Position = pos;
        parent.AddChild(m);
        return m;
    }

    private void AddBoxTo(Node3D parent, Vector3 size, StandardMaterial3D mat, Vector3 pos)
    {
        var m = new MeshInstance3D();
        var mesh = new BoxMesh();
        mesh.Size = size;
        m.Mesh = mesh;
        m.MaterialOverride = mat;
        m.Position = pos;
        parent.AddChild(m);
    }

    private void AddCylinder(float topR, float height, Color color, Vector3 pos)
    {
        var m = new MeshInstance3D();
        var mesh = new CylinderMesh();
        mesh.TopRadius = topR;
        mesh.BottomRadius = topR;
        mesh.Height = height;
        mesh.RadialSegments = 6;
        m.Mesh = mesh;
        var mat = new StandardMaterial3D();
        mat.AlbedoColor = color;
        mat.SpecularMode = BaseMaterial3D.SpecularModeEnum.Disabled;
        m.MaterialOverride = mat;
        m.Position = pos;
        _garageRoot.AddChild(m);
    }

    private void AddCylinderTo(Node3D parent, float topR, float bottomR, float height, StandardMaterial3D mat, Vector3 pos)
    {
        var m = new MeshInstance3D();
        var mesh = new CylinderMesh();
        mesh.TopRadius = topR;
        mesh.BottomRadius = bottomR;
        mesh.Height = height;
        mesh.RadialSegments = 8;
        m.Mesh = mesh;
        m.MaterialOverride = mat;
        m.Position = pos;
        parent.AddChild(m);
    }
}
