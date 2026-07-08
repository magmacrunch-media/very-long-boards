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

        float camH = 5f + sf * 2f;
        float camD = 7f + sf * 1f;
        _main.CameraMount.Position = new Vector3(0, camH, -camD);

        var cam = _main.CameraMount.GetNode<Camera3D>("Camera3D");
        cam.LookAt(new Vector3(player.PosX, groundY + 0.3f, 10f), Vector3.Up);

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

    public void UpdateCharSelect()
    {
        _main.CameraMount.Position = new Vector3(1.5f, 3f, -5f);
        var cam = _main.CameraMount.GetNode<Camera3D>("Camera3D");
        cam.LookAt(new Vector3(0f, 0.8f, 0f), Vector3.Up);
        cam.Fov = 55f;
    }

    public void UpdateBoardSelect()
    {
        _main.CameraMount.Position = new Vector3(-1.5f, 2f, -4f);
        var cam = _main.CameraMount.GetNode<Camera3D>("Camera3D");
        cam.LookAt(new Vector3(0f, 0.3f, 0f), Vector3.Up);
        cam.Fov = 50f;
    }

    public void UpdateLevelSelect(float titleTime)
    {
        float sway = Mathf.Sin(titleTime * 0.3f) * 0.5f;
        _main.CameraMount.Position = new Vector3(sway, 8f, -12f);
        var cam = _main.CameraMount.GetNode<Camera3D>("Camera3D");
        cam.LookAt(new Vector3(sway * 0.5f, 0f, 30f), Vector3.Up);
        cam.Fov = 70f;
    }
}
