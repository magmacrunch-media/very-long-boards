using Godot;

    public class GameCamera
    {
        private Main _main;

        public GameCamera(Main main)
        {
            _main = main;
        }

        public void Create() { }

        public void Update()
        {
            var terrain = _main.Terrain;
            var player = _main.PlayerMgr;
            float groundY = terrain.HillAt(player.Distance);
            float slope = (terrain.HillAt(player.Distance + 3f) - terrain.HillAt(player.Distance)) / 3f;
            float sf = Mathf.Clamp(-slope / 2f, 0f, 1f);
            float speedFactor = Mathf.Clamp(player.Speed / PlayerManager.MaxSpeed, 0f, 1f);

            // Dynamic camera distance — close at high speed, far at low speed
            float camH = Mathf.Lerp(5.5f, 3.8f, speedFactor) + sf * 1.5f;
            float camD = Mathf.Lerp(8f, 5f, speedFactor) + sf * 1f;

            // Terrain-reactive: dip lower on downhill, raise on uphill
            float terrainPitch = -slope * 1.5f;
            camH += terrainPitch * 0.8f;

            float time = (float)Time.GetTicksMsec() * 0.001f;

            // Camera shake — high frequency, proportional to speed
            float shakeAmt = speedFactor * 0.04f;
            float shakeX = Mathf.Sin(time * 47f) * shakeAmt;
            float shakeY = Mathf.Cos(time * 53f) * shakeAmt * 0.5f;

        _main.CameraMount.Position = new Vector3(shakeX, camH + shakeY, -camD);

        var cam = _main.CameraMount.GetNode<Camera3D>("Camera3D");
        float curve = terrain.CurveAt(player.Distance);

        // Camera lean on steering — roll slightly in carve direction
        float steerLean = player.SteerSmooth * 0.03f * speedFactor;

        // Look-at target follows curve and speed
        float lookAhead = Mathf.Lerp(10f, 18f, speedFactor);
        float lookX = player.PosX + curve * 25f + shakeX;
        float lookY = groundY + 0.3f + terrainPitch * 0.3f;

        cam.LookAt(new Vector3(lookX, lookY, lookAhead), Vector3.Up);

        // Apply roll for camera lean
        var currentRot = cam.Rotation;
        cam.Rotation = new Vector3(currentRot.X, currentRot.Y, steerLean);

        // Aggressive FOV scaling — much wider at high speed
        cam.Fov = Mathf.Lerp(55f, 78f, speedFactor);
    }

    public void UpdateTitle(float titleTime)
    {
        float sway = Mathf.Sin(titleTime * 0.5f) * 0.3f;
        _main.CameraMount.Position = new Vector3(sway, 5f, -7f);
        var cam = _main.CameraMount.GetNode<Camera3D>("Camera3D");
        cam.LookAt(new Vector3(sway * 2f, 0f, 15f), Vector3.Up);
        cam.Rotation = Vector3.Zero;
        cam.Fov = 65f;
    }

    public void UpdateCharSelect()
    {
        _main.CameraMount.Position = new Vector3(1.5f, 3f, -5f);
        var cam = _main.CameraMount.GetNode<Camera3D>("Camera3D");
        cam.LookAt(new Vector3(0f, 0.8f, 0f), Vector3.Up);
        cam.Rotation = Vector3.Zero;
        cam.Fov = 55f;
    }

    public void UpdateBoardSelect()
    {
        _main.CameraMount.Position = new Vector3(-1.5f, 2f, -4f);
        var cam = _main.CameraMount.GetNode<Camera3D>("Camera3D");
        cam.LookAt(new Vector3(0f, 0.3f, 0f), Vector3.Up);
        cam.Rotation = Vector3.Zero;
        cam.Fov = 50f;
    }

    public void UpdateLevelSelect(float titleTime)
    {
        float sway = Mathf.Sin(titleTime * 0.3f) * 0.5f;
        _main.CameraMount.Position = new Vector3(sway, 8f, -12f);
        var cam = _main.CameraMount.GetNode<Camera3D>("Camera3D");
        cam.LookAt(new Vector3(sway * 0.5f, 0f, 30f), Vector3.Up);
        cam.Rotation = Vector3.Zero;
        cam.Fov = 70f;
    }
}
