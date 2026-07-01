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

        // Camera position: behind and above player
        float camH = 5f + sf * 2f;
        float camD = 7f + sf * 1f;
        _main.CameraMount.Position = new Vector3(0, camH, -camD);

        // Look at a point ahead of the player
        var cam = _main.CameraMount.GetNode<Camera3D>("Camera3D");
        float curve = terrain.CurveAt(player.Distance);
        cam.LookAt(new Vector3(player.PosX + curve * 20f, groundY + 0.3f, 10f), Vector3.Up);

        // Slight FOV increase at speed
        cam.Fov = 65f + speedFactor * 4f;
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
