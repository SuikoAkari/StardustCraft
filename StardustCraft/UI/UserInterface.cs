using FontStashSharp;
using StardustCraft.Bundles.BundleSystem;
using StardustCraft.Graphics;
using Newtonsoft.Json;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace StardustCraft.UI
{
    public class UserInterface
    {
        public static Vector2 Size;
        private static int quadVAO;
        private static int quadVBO;
        private static int quadEBO;
        private static int shaderProgram;
        public static Text textRenderer;
        public static FontSystem _fontSystem;
        public List<UILayout> layouts = new();

        public void LoadAndAddLayout(string path)
        {
            var settings = new JsonSerializerSettings();
            settings.Converters.Add(new UIElementConverter());

            UILayout v = JsonConvert.DeserializeObject<UILayout>(
                BundleManager.Instance.GetFileText(path),
            settings
            );
            v.Compute(Size);
            v.Init();
            
            layouts.Add(v);
        }
        public void UpdateSize(Vector2i size)
        {
            Size= size;
            foreach (var item in layouts)
            {
                item.Compute(size);
            }
        }
        public void Initialize(Game game)
        {
            Size = game.Size;
            textRenderer = new();
            var settings = new FontSystemSettings
            {
                FontResolutionFactor = 2,
                KernelWidth = 2,
                KernelHeight = 2
            };

            _fontSystem = new FontSystem(settings);
            _fontSystem.AddFont(BundleManager.Instance.GetFileBytes("fonts/BitmapMc.ttf"));
            // Quad unitario (0..1)
            float[] vertices =
            {
                // pos      // uv
                0f, 0f,     0f, 0f,
                1f, 0f,     1f, 0f,
                1f, 1f,     1f, 1f,
                0f, 1f,     0f, 1f
            };

            uint[] indices = { 0, 1, 2, 2, 3, 0 };

            quadVAO = GL.GenVertexArray();
            quadVBO = GL.GenBuffer();
            quadEBO = GL.GenBuffer();

            GL.BindVertexArray(quadVAO);

            GL.BindBuffer(BufferTarget.ArrayBuffer, quadVBO);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, quadEBO);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.StaticDraw);

            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);

            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 2 * sizeof(float));
            GL.EnableVertexAttribArray(1);

            GL.BindVertexArray(0);

            shaderProgram = CreateShader();
            LoadAndAddLayout("data/ui/ui_game_hud.json");
        }

        public void Render(float deltaTime)
        {
            // RenderQuad(new Vector2(Size.X/2 - 6, Size.Y / 2 - 6), new Vector2(12, 12), Vector4.One, TextureLoader.GetTexture("ui/small_center_cursor.png"));
            //RenderQuad(new Vector2(10, 10), new Vector2(100, 100), Vector4.One, TextureLoader.GetTexture("ui/small_panel_test.png"));
            //var font = _fontSystem.GetFont(24);
            // textRenderer.Begin(new Vector2i((int)Size.X, (int)Size.Y));
            // font.DrawText(textRenderer, "Hello world", new System.Numerics.Vector2(5f, 5f), FSColor.White, scale: new System.Numerics.Vector2(0.7f, 0.7f), effect: FontSystemEffect.Stroked, effectAmount: 4);
            //  textRenderer.End();
            foreach (var l in layouts)
            {
                l.Render();
                l.Update(deltaTime);
            }
        }

        public static void RenderQuad(Vector2 position, Vector2 size, Vector4 color, int texture = 0)
        {
            GL.UseProgram(shaderProgram);

            GL.Uniform2(GL.GetUniformLocation(shaderProgram, "uPos"), position);
            GL.Uniform2(GL.GetUniformLocation(shaderProgram, "uSize"), size);
            GL.Uniform2(GL.GetUniformLocation(shaderProgram, "uScreen"), Size);
            GL.Uniform4(GL.GetUniformLocation(shaderProgram, "uColor"), color);

            if (texture != 0)
            {
                GL.ActiveTexture(TextureUnit.Texture0);
                GL.BindTexture(TextureTarget.Texture2D, texture);
                GL.Uniform1(GL.GetUniformLocation(shaderProgram, "uTex"), 0);
                GL.Uniform1(GL.GetUniformLocation(shaderProgram, "uUseTex"), 1);
            }
            else
            {
                GL.Uniform1(GL.GetUniformLocation(shaderProgram, "uUseTex"), 0);
            }

            GL.BindVertexArray(quadVAO);
            GL.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, 0);
            GL.BindVertexArray(0);
        }

        private int CreateShader()
        {
            string vertexShader = @"
#version 330 core
layout(location = 0) in vec2 aPos;
layout(location = 1) in vec2 aUV;

uniform vec2 uPos;
uniform vec2 uSize;
uniform vec2 uScreen;

out vec2 vUV;

void main()
{
    vUV = aUV;

    vec2 pixelPos = uPos + aPos * uSize;

    vec2 ndc;
    ndc.x = (pixelPos.x / uScreen.x) * 2.0 - 1.0;
    ndc.y = 1.0 - (pixelPos.y / uScreen.y) * 2.0;

    gl_Position = vec4(ndc, -1.0, 1.0);
}";

            string fragmentShader = @"
#version 330 core
in vec2 vUV;
out vec4 FragColor;

uniform sampler2D uTex;
uniform vec4 uColor;
uniform int uUseTex;

void main()
{
    if (uUseTex == 1)
        FragColor = texture(uTex, vUV);
    else
        FragColor = uColor;
}";

            int vs = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(vs, vertexShader);
            GL.CompileShader(vs);

            int fs = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(fs, fragmentShader);
            GL.CompileShader(fs);

            int program = GL.CreateProgram();
            GL.AttachShader(program, vs);
            GL.AttachShader(program, fs);
            GL.LinkProgram(program);

            GL.DeleteShader(vs);
            GL.DeleteShader(fs);

            return program;
        }
    }
}