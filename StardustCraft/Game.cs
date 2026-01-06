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
using StardustCraft.Auth;
using StardustCraft.Network;
using CefSharp.OffScreen;
using Keys = OpenTK.Windowing.GraphicsLibraryFramework.Keys;
using CefSharp;
using StardustCraft.Browser;
using CefSharp.SchemeHandler;

public class Game : GameWindow
{
    public static World world;
    public static UserInterface UI;
    public static Game Instance;
    public AccountState AccountState;
    public CubeRenderer cubeRenderer;
    public static string ClientVersion = "1.2a";
    public Camera Camera;
    Matrix4 projection;
    Matrix4 view;
    Shader shader;
    double fpsTime = 0;
    int fpsFrames = 0;
    public int currentFps = 0;
    public bool GamePause = true;
    public NetManager NetManager; //Null if singleplayer
    public GameBrowser browser;
    public Game(GameWindowSettings gws, NativeWindowSettings nws)
        : base(gws, nws) { }
    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);

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
        var settings = new CefSettings
        {
            WindowlessRenderingEnabled = true, // MUST per OffScreen
            MultiThreadedMessageLoop = true,
            BackgroundColor = 0x00000000 // ARGB = trasparente
        };

        settings.CefCommandLineArgs.Add("disable-web-security", "1");
        settings.CefCommandLineArgs.Add("allow-file-access-from-files", "1");
        
        Cef.Initialize(settings);
        Instance = this;
        BundleManager.Instance.Initialize("game_data");
        
        GL.ClearColor(0.1f, 0.1f, 0.1f, 1f);
        GL.Enable(EnableCap.DepthTest);
        GL.DepthFunc(DepthFunction.Lequal);
        
        shader = new Shader("shader.vert", "shader.frag");

        CreatePerspectiveField();
        BlockManager.Initialize();
        Camera= new Camera();
        //world = new World();
        // world.Start();
        cubeRenderer = new();
        cubeRenderer.Initialize();
        AccountState = new();
        AccountState.Initialize();
        UI = new();
        UI.Initialize(this);
        new Thread(() =>
        {
            UpdateSecondThread();
        }).Start();
        //browser = new GameBrowser(Size.X, Size.Y, "http://localhost:4000/");
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
                if (world != null)
                {
                    world?.Update((float)FIXED_DT);
                    world.UpdatePlayerPosition();
                }

                accumulator -= FIXED_DT;
            }

            Thread.Sleep(1);
        }
    }

    protected override void OnMouseMove(MouseMoveEventArgs e)
    {
        this.Camera.OnMouseMove(e);
    }
    public void UpdateClientPlayer(FrameEventArgs args)
    {
        if (world == null) return;
        var player = world.GetClientEntity();
        if (player != null)
        {
            // gestione input
            if (Game.Instance.KeyboardState.IsKeyPressed(Keys.Space))
                world.JumpPressed = true;
            player.RenderPos = Vector3.Lerp(player.RenderPos, player.FinalPosition, (float)args.Time * 20);
            Camera.Position = new Vector3(player.RenderPos.X, player.RenderPos.Y + 1.7f / 2f, player.RenderPos.Z);
            Camera.RayCast();
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
            
            if (Camera.Ray.Block != null)
            {
                if (MouseState.IsButtonPressed(MouseButton.Left))
                    world.SetBlockAt(Camera.Ray.Position.X, Camera.Ray.Position.Y, Camera.Ray.Position.Z, BlockType.Air);

                if (MouseState.IsButtonPressed(MouseButton.Right))
                    world.SetBlockAt(Camera.Ray.Position.X + Camera.Ray.Normal.X, Camera.Ray.Position.Y + Camera.Ray.Normal.Y, Camera.Ray.Position.Z + Camera.Ray.Normal.Z, player.inventory[player.selectedInventorySlot]);
            }
        }
    }
    protected override void OnKeyDown(OpenTK.Windowing.Common.KeyboardKeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (browser != null)
        {
            browser?.HandleKeyPress(e.Key, true);
            switch (e.Key)
            {
                case OpenTK.Windowing.GraphicsLibraryFramework.Keys.Period:
                    browser.HandleChar('.');
                    break;
                case OpenTK.Windowing.GraphicsLibraryFramework.Keys.Comma:
                    browser.HandleChar(',');
                    break;
                    // Aggiungi altri simboli se necessario
            }
        }
        
    }

    protected override void OnKeyUp(OpenTK.Windowing.Common.KeyboardKeyEventArgs e)
    {
        base.OnKeyUp(e);
        if (browser != null)
        {
            browser?.HandleKeyPress(e.Key, false);
        }
    }

    protected override void OnTextInput(OpenTK.Windowing.Common.TextInputEventArgs e)
    {
        base.OnTextInput(e);
        if (browser != null)
        {
            foreach (var c in e.AsString)
                browser?.HandleChar(c);
        }

    }
    protected override void OnUpdateFrame(FrameEventArgs args)
    {

        if (!IsFocused)
            return;

        float dt = (float)args.Time;
        
        // --- GESTIONE INPUT BROWSER ---
        if (browser != null)
        {
            var mouse = MouseState; // OpenTK Window MouseState
            var mousePos = mouse.Position; // Vector2

            // Mouse move
            browser.HandleMouseMove((int)mousePos.X, (int)mousePos.Y);

            // Mouse click
            if (mouse.IsButtonDown(MouseButton.Left))
                browser.HandleMouseClick((int)mousePos.X, (int)mousePos.Y, MouseButton.Left, false);

            if (mouse.IsButtonReleased(MouseButton.Left))
                browser.HandleMouseClick((int)mousePos.X, (int)mousePos.Y, MouseButton.Left, true);

            if (mouse.IsButtonDown(MouseButton.Right))
                browser.HandleMouseClick((int)mousePos.X, (int)mousePos.Y, MouseButton.Right, false);

            if (mouse.IsButtonReleased(MouseButton.Right))
                browser.HandleMouseClick((int)mousePos.X, (int)mousePos.Y, MouseButton.Right, true);
                browser.HandleMouseWheel((int)mouse.ScrollDelta.X * 100, (int)mouse.ScrollDelta.Y*100, (int)mousePos.X, (int)mousePos.Y);
        }
        else
        {
            UpdateClientPlayer(args);
        }
        if (!GamePause && world!=null)
        {
            Camera.IsCursorLocked = true;
        }
        else
        {
            Camera.IsCursorLocked = false;
        }
        if (world != null)
        {
            UI.GetLayoutByName("GameHUDv2").enabled = true;
            UI.GetLayoutByName("GameHUDv1").enabled = true;
            UI.GetLayoutByName("MainMenu").enabled = false;
        }
        else
        {
            UI.GetLayoutByName("GameHUDv2").enabled = false;
            UI.GetLayoutByName("GameHUDv1").enabled = false;
            UI.GetLayoutByName("MainMenu").enabled = true;
        }
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
       
        view = Camera.GetView();
        if (textureAtlas==0)
            textureAtlas=TextureLoader.GetTexture("atlas.png");
        shader.Use();
        shader.SetMatrix4("projection", projection);
        shader.SetMatrix4("view", view);
        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2D, textureAtlas);
        shader.SetInt("tex", 0);
        //lighting.ApplyToShader(shader, Camera.Position);
        if (world!=null)
        world.Render(shader);
        if (cubeRenderer != null)
        {
            if (Camera.Ray.Block!=null)
            {
                cubeRenderer.Render(Camera.Ray.Position.ToVector3(), view, projection);
            }
        }
        GL.Disable(EnableCap.DepthTest);
        GL.Disable(EnableCap.CullFace);
       
        GL.Clear(ClearBufferMask.DepthBufferBit);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        UI.Render((float)args.Time);
        if (browser != null)
        {
            browser.Render(Size.X,Size.Y-40);
        }
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
