using StardustCraft.Bundles.BundleSystem;
using StardustCraft.Graphics;
using StardustCraft.Shaders;
using StardustCraft.UI;
using StardustCraft.World;
using StardustCraft.World.Blocks;
using StardustCraft.World.Entities;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System.Diagnostics;
using System.Reflection;

public class Game : GameWindow
{
    public static World world;
    public static UserInterface UI;
    public static Game Instance;
    public CubeRenderer cubeRenderer;
    public static string ClientVersion = "1.1";
    Shader shader;
    private Lighting lighting;
    Matrix4 projection;
    Matrix4 view;
    public Vector3 cameraPos = new Vector3(8, 10, 8);
    public float yaw = -90f;
    public float pitch = 0f;
    public Ray Ray;
    float speed = 10f;
    float sensitivity = 0.2f;

    bool firstMove = true;
    Vector2 lastMousePos;

    double fpsTime = 0;
    int fpsFrames = 0;
    public int currentFps = 0;
    public Game(GameWindowSettings gws, NativeWindowSettings nws)
        : base(gws, nws) { }
    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);

        // Aggiorna il viewport OpenGL
        GL.Viewport(0, 0, e.Width, e.Height);

        CreatePerspectiveField();
        UI.UpdateSize(e.Size);
    }
    public void CreatePerspectiveField()
    {
        projection = Matrix4.CreatePerspectiveFieldOfView(
            MathHelper.DegreesToRadians(70f),
            Size.X / (float)Size.Y,
            0.1f,
            1000f
        );
    }
    protected override void OnLoad()
    {
        Instance = this;
        BundleManager.Instance.Initialize("game_data");
        CursorState = CursorState.Grabbed;
        GL.ClearColor(0.1f, 0.1f, 0.1f, 1f);
        GL.Enable(EnableCap.DepthTest);
        GL.DepthFunc(DepthFunction.Lequal);
        
        shader = new Shader("shader.vert", "shader.frag");

        CreatePerspectiveField();
        lighting = new Lighting();
        BlockManager.Initialize();
        world = new World();
        world.Start();
        cubeRenderer = new();
        cubeRenderer.Initialize();
        UI = new();
        UI.Initialize(this);
        new Thread(() =>
        {
            UpdateSecondThread();
        }).Start();
    }
    public void UpdateSecondThread()
    {
        const double FIXED_DT = 1.0 / 20.0; 
        var stopwatch = Stopwatch.StartNew();
        double previousTime = stopwatch.Elapsed.TotalSeconds;
        double accumulator = 0.0;

        while (!IsExiting)
        {
            double currentTime = stopwatch.Elapsed.TotalSeconds;
            double frameTime = currentTime - previousTime;
            previousTime = currentTime;

            if (frameTime > 0.25)
                frameTime = 0.25;

            accumulator += frameTime;

            while (accumulator >= FIXED_DT)
            {
                world?.Update((float)FIXED_DT);
                // aggiorna posizione del player nel world
                world.UpdatePlayerPosition();
                accumulator -= FIXED_DT;
            }

            Thread.Sleep(1);
        }
    }

    protected override void OnMouseMove(MouseMoveEventArgs e)
    {
        if (firstMove)
        {
            lastMousePos = e.Position;
            firstMove = false;
            return;
        }

        var deltaX = e.Position.X - lastMousePos.X;
        var deltaY = e.Position.Y - lastMousePos.Y;
        lastMousePos = e.Position;

        yaw += deltaX * sensitivity;
        pitch -= deltaY * sensitivity;

        pitch = Math.Clamp(pitch, -89f, 89f);
    }
    public void UpdateClientPlayer(FrameEventArgs args)
    {
        var player = world.GetClientEntity();
        if (player != null)
        {
            // gestione input
            if (Game.Instance.KeyboardState.IsKeyPressed(Keys.Space))
                world.JumpPressed = true;
            player.RenderPos = Vector3.Lerp(player.RenderPos, player.FinalPosition, (float)args.Time * 20);
            cameraPos = new Vector3(player.RenderPos.X, player.RenderPos.Y + 1.7f / 2f, player.RenderPos.Z);

            RayCast(cameraPos);
            if (KeyboardState.IsKeyPressed(Keys.D1))
            {
                player.selectedInventorySlot = 0;
            }
            if (KeyboardState.IsKeyPressed(Keys.D2))
            {
                player.selectedInventorySlot = 1;
            }
            if (KeyboardState.IsKeyPressed(Keys.D3))
            {
                player.selectedInventorySlot = 2;
            }
            if (KeyboardState.IsKeyPressed(Keys.D4))
            {
                player.selectedInventorySlot = 3;
            }
            if (KeyboardState.IsKeyPressed(Keys.D5))
            {
                player.selectedInventorySlot = 4;
            }
            if (KeyboardState.IsKeyPressed(Keys.D6))
            {
                player.selectedInventorySlot = 5;
            }
            if (KeyboardState.IsKeyPressed(Keys.D7))
            {
                player.selectedInventorySlot = 6;
            }
            if (KeyboardState.IsKeyPressed(Keys.D8))
            {
                player.selectedInventorySlot = 7;
            }
            if (KeyboardState.IsKeyPressed(Keys.D9))
            {
                player.selectedInventorySlot = 8;
            }
            
            if (Ray.Block != null)
            {
                if (MouseState.IsButtonPressed(MouseButton.Left))
                    world.SetBlockAt(Ray.Position.X, Ray.Position.Y, Ray.Position.Z, BlockType.Air);

                if (MouseState.IsButtonPressed(MouseButton.Right))
                    world.SetBlockAt(Ray.Position.X + Ray.Normal.X,Ray.Position.Y + Ray.Normal.Y,Ray.Position.Z + Ray.Normal.Z, player.inventory[player.selectedInventorySlot]);
            }
        }
    }
    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        if (!IsFocused)
            return;

        float dt = (float)args.Time;

        UpdateClientPlayer(args);
        
       
        lighting.Update(dt, cameraPos);
    }

    public void RayCast(Vector3 position)
    {
        float maxDistance = 12f;
        Vector3 dir = new(
                MathF.Cos(MathHelper.DegreesToRadians(yaw)) * MathF.Cos(MathHelper.DegreesToRadians(pitch)),
                MathF.Sin(MathHelper.DegreesToRadians(pitch)),
                MathF.Sin(MathHelper.DegreesToRadians(yaw)) * MathF.Cos(MathHelper.DegreesToRadians(pitch))
            );
        dir = Vector3.Normalize(dir);

        int x = (int)MathF.Floor(position.X);
        int y = (int)MathF.Floor(position.Y);
        int z = (int)MathF.Floor(position.Z);

        int stepX = dir.X >= 0 ? 1 : -1;
        int stepY = dir.Y >= 0 ? 1 : -1;
        int stepZ = dir.Z >= 0 ? 1 : -1;

        float tDeltaX = MathF.Abs(1f / dir.X);
        float tDeltaY = MathF.Abs(1f / dir.Y);
        float tDeltaZ = MathF.Abs(1f / dir.Z);

        // Gestione precisa dei confini iniziali
        float tMaxX = (dir.X > 0) ? (MathF.Floor(position.X) + 1 - position.X) * tDeltaX
                                 : (position.X - MathF.Floor(position.X)) * tDeltaX;
        float tMaxY = (dir.Y > 0) ? (MathF.Floor(position.Y) + 1 - position.Y) * tDeltaY
                                 : (position.Y - MathF.Floor(position.Y)) * tDeltaY;
        float tMaxZ = (dir.Z > 0) ? (MathF.Floor(position.Z) + 1 - position.Z) * tDeltaZ
                                 : (position.Z - MathF.Floor(position.Z)) * tDeltaZ;

        // Se la posizione è esattamente sul bordo, tMaxX sarà 0. 
        // Per evitare loop infiniti o salti, forziamo i valori infiniti se dir è 0.
        if (dir.X == 0) tMaxX = float.PositiveInfinity;
        if (dir.Y == 0) tMaxY = float.PositiveInfinity;
        if (dir.Z == 0) tMaxZ = float.PositiveInfinity;

        Vector3i lastNormal = Vector3i.Zero;
        float t = 0;

        while (t <= maxDistance)
        {
            var block = world.GetBlockAt(x, y, z);
            if (block.Type != BlockType.Air)
            {
                Vector3 hitPos = position + dir * t;
                // La normale è semplicemente l'opposto dello step sull'ultimo asse che si è mosso
                Ray = new Ray(block, (x, y, z), lastNormal, hitPos);
                return;
            }

            if (tMaxX < tMaxY)
            {
                if (tMaxX < tMaxZ)
                {
                    t = tMaxX;
                    tMaxX += tDeltaX;
                    x += stepX;
                    lastNormal = new Vector3i(-stepX, 0, 0);
                }
                else
                {
                    t = tMaxZ;
                    tMaxZ += tDeltaZ;
                    z += stepZ;
                    lastNormal = new Vector3i(0, 0, -stepZ);
                }
            }
            else
            {
                if (tMaxY < tMaxZ)
                {
                    t = tMaxY;
                    tMaxY += tDeltaY;
                    y += stepY;
                    lastNormal = new Vector3i(0, -stepY, 0);
                }
                else
                {
                    t = tMaxZ;
                    tMaxZ += tDeltaZ;
                    z += stepZ;
                    lastNormal = new Vector3i(0, 0, -stepZ);
                }
            }
        }
        Ray = new Ray();
    }
    int textureAtlas = 0;
    protected override void OnRenderFrame(FrameEventArgs args)
    {
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        GL.ClearColor(new Color4(127, 172, 255,255));
        GL.Enable(EnableCap.CullFace);
        GL.CullFace(TriangleFace.Back);
        GL.Enable(EnableCap.Blend);
        GL.Enable(EnableCap.DepthTest);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        Vector3 front;
        front.X = MathF.Cos(MathHelper.DegreesToRadians(yaw)) * MathF.Cos(MathHelper.DegreesToRadians(pitch));
        front.Y = MathF.Sin(MathHelper.DegreesToRadians(pitch));
        front.Z = MathF.Sin(MathHelper.DegreesToRadians(yaw)) * MathF.Cos(MathHelper.DegreesToRadians(pitch));
        front = Vector3.Normalize(front);

        view = Matrix4.LookAt(
            cameraPos,
            cameraPos + front,
            Vector3.UnitY
        );
        if(textureAtlas==0)
            textureAtlas=TextureLoader.GetTexture("atlas.png");
        shader.Use();
        shader.SetMatrix4("projection", projection);
        shader.SetMatrix4("view", view);
        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2D, textureAtlas);
        shader.SetInt("tex", 0);
        //lighting.ApplyToShader(shader, cameraPos);

        world.Render(shader);
        if (cubeRenderer != null)
        {
            if (Ray.Block!=null)
            {
                cubeRenderer.Render(Ray.Position.ToVector3(), view, projection);
            }
        }
        GL.Disable(EnableCap.DepthTest);
        GL.Disable(EnableCap.CullFace);
       
        GL.Clear(ClearBufferMask.DepthBufferBit);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        UI.Render((float)args.Time);
        GL.Disable(EnableCap.Blend);
        SwapBuffers();
       
        fpsTime += args.Time;
        fpsFrames++;

        if (fpsTime >= 1.0)
        {
            currentFps = fpsFrames;
            fpsFrames = 0;
            fpsTime = 0;

            Title = $"FPS: {currentFps}";
        }
    }

}
