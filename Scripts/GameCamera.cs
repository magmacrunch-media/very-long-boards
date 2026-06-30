using Godot;

public class GameCamera
{
    private Main _main;
    private float _tilt = 0f;

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

        float camH = 4.5f + sf * 3f - speedFactor * 0.5f;
        float camD = 6f + sf * 2f - speedFactor * 1f;
        _main.CameraMount.Position = new Vector3(0, camH, -camD);

        var cam = _main.CameraMount.GetNode<Camera3D>("Camera3D");
        float curve = terrain.CurveAt(player.Distance);
        cam.LookAt(new Vector3(player.PosX + curve * 30f, groundY + 0.5f, 12f), Vector3.Up);

        float targetTilt = -curve * 15f;
        _tilt = Mathf.Lerp(_tilt, targetTilt, 3f * (float)_main.GetProcessDeltaTime());
        cam.Rotation = new Vector3(cam.Rotation.X, cam.Rotation.Y, _tilt);

        // Speed wobble
        if (speedFactor > 0.85f)
        {
            float wobbleIntensity = (speedFactor - 0.85f) / 0.15f;
            float time = (float)Time.GetTicksMsec() * 0.01f;
            float wobble = Mathf.Sin(time * 12f) * wobbleIntensity * 0.003f;
            cam.Rotation = new Vector3(cam.Rotation.X + wobble, cam.Rotation.Y, cam.Rotation.Z);
        }

        cam.Fov = 65f + speedFactor * 5f;
    }

    public void UpdateTitle(float titleTime)
    {
        float sway = Mathf.Sin(titleTime * 0.5f) * 0.3f;
        _main.CameraMount.Position = new Vector3(sway, 5f, -7f);
        var cam = _main.CameraMount.GetNode<Camera3D>("Camera3D");
        cam.LookAt(new Vector3(sway * 2f, 0f, 15f), Vector3.Up);
        cam.Fov = 65f;
    }
}
