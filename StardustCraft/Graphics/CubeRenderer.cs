using StardustCraft.Shaders;

using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

public class CubeRenderer
{
    private readonly float[] vertices = {
        // pos            // uv
        // Front face
        -0.5f, -0.5f,  0.5f, 0f, 0f,
         0.5f, -0.5f,  0.5f, 1f, 0f,
         0.5f,  0.5f,  0.5f, 1f, 1f,
        -0.5f,  0.5f,  0.5f, 0f, 1f,
        // Back face
        -0.5f, -0.5f, -0.5f, 1f, 0f,
         0.5f, -0.5f, -0.5f, 0f, 0f,
         0.5f,  0.5f, -0.5f, 0f, 1f,
        -0.5f,  0.5f, -0.5f, 1f, 1f,
    };

    private readonly uint[] indices = {
        // Front
        0, 1, 2, 2, 3, 0,
        // Right
        1, 5, 6, 6, 2, 1,
        // Back
        5, 4, 7, 7, 6, 5,
        // Left
        4, 0, 3, 3, 7, 4,
        // Top
        3, 2, 6, 6, 7, 3,
        // Bottom
        4, 5, 1, 1, 0, 4
    };
    private int vaoUI, vboUI, eboUI;
    private readonly uint[] uiIndices =
{
     0, 1, 2, 2, 3, 0,
     4, 5, 6, 6, 7, 4,
     8, 9,10,10,11, 8,
    12,13,14,14,15,12,
    16,17,18,18,19,16,
    20,21,22,22,23,20
};
    private readonly float[] uiVertices =
    {
    // FRONT
    -0.5f,-0.5f, 0.5f, 0,0, 0,
     0.5f,-0.5f, 0.5f, 1,0, 0,
     0.5f, 0.5f, 0.5f, 1,1, 0,
    -0.5f, 0.5f, 0.5f, 0,1, 0,

    // BACK (UV flipped)
     0.5f,-0.5f,-0.5f, 0,0, 1,
    -0.5f,-0.5f,-0.5f, 1,0, 1,
    -0.5f, 0.5f,-0.5f, 1,1, 1,
     0.5f, 0.5f,-0.5f, 0,1, 1,

    // LEFT
    -0.5f,-0.5f,-0.5f, 0,0, 2,
    -0.5f,-0.5f, 0.5f, 1,0, 2,
    -0.5f, 0.5f, 0.5f, 1,1, 2,
    -0.5f, 0.5f,-0.5f, 0,1, 2,

    // RIGHT
     0.5f,-0.5f, 0.5f, 0,0, 3,
     0.5f,-0.5f,-0.5f, 1,0, 3,
     0.5f, 0.5f,-0.5f, 1,1, 3,
     0.5f, 0.5f, 0.5f, 0,1, 3,

    // TOP
    -0.5f, 0.5f, 0.5f, 0,0, 4,
     0.5f, 0.5f, 0.5f, 1,0, 4,
     0.5f, 0.5f,-0.5f, 1,1, 4,
    -0.5f, 0.5f,-0.5f, 0,1, 4,

    // BOTTOM
    -0.5f,-0.5f,-0.5f, 0,0, 5,
     0.5f,-0.5f,-0.5f, 1,0, 5,
     0.5f,-0.5f, 0.5f, 1,1, 5,
    -0.5f,-0.5f, 0.5f, 0,1, 5,
};
    private int vao, vbo, ebo;
    private Shader shader;
    private Shader shaderCubeUI;
    private Shader outlineShader;
    public void Initialize()
    {
        vao = GL.GenVertexArray();
        vbo = GL.GenBuffer();
        ebo = GL.GenBuffer();

        GL.BindVertexArray(vao);

        GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
        GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);

        GL.BindBuffer(BufferTarget.ElementArrayBuffer, ebo);
        GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.StaticDraw);

        // Posizione
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);

        // UV
        GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 3 * sizeof(float));
        GL.EnableVertexAttribArray(1);

        GL.BindVertexArray(0);
        InitUIBuffers();
        // Shader (minimal)
        shader = new Shader("cube.vert", "cube.frag");
        shader.Use();
        outlineShader = new Shader("cube.vert", "outline.frag");
        outlineShader.Use();
        shaderCubeUI = new Shader("uicube.vert", "uicube.frag");
    }
    public void InitUIBuffers()
    {
        vaoUI = GL.GenVertexArray();
        vboUI = GL.GenBuffer();
        eboUI = GL.GenBuffer();

        GL.BindVertexArray(vaoUI);

        GL.BindBuffer(BufferTarget.ArrayBuffer, vboUI);
        GL.BufferData(BufferTarget.ArrayBuffer, uiVertices.Length * sizeof(float), uiVertices, BufferUsageHint.StaticDraw);

        GL.BindBuffer(BufferTarget.ElementArrayBuffer, eboUI);
        GL.BufferData(BufferTarget.ElementArrayBuffer, uiIndices.Length * sizeof(uint), uiIndices, BufferUsageHint.StaticDraw);

        int stride = 6 * sizeof(float);

        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, 0);
        GL.EnableVertexAttribArray(0);

        GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, stride, 3 * sizeof(float));
        GL.EnableVertexAttribArray(1);

        GL.VertexAttribPointer(
    2, 1,
    VertexAttribPointerType.Float,
    false,
    stride,
    5 * sizeof(float)
);
        GL.EnableVertexAttribArray(2);

        GL.BindVertexArray(0);
    }
    public void Render(Vector3 position, Matrix4 view, Matrix4 projection)
    {
        position += new Vector3(0.5f, 0.5f, 0.5f); // centrare il cubo
        float outlineScale = 1.05f; // scala per l'outline

        // --- 1️⃣ Disegna outline nero ---
        GL.CullFace(CullFaceMode.Front); // back faces visibili → outline
        outlineShader.Use();

        Matrix4 modelOutline = Matrix4.CreateScale(outlineScale) * Matrix4.CreateTranslation(position);
        outlineShader.SetMatrix4("model", modelOutline);
        outlineShader.SetMatrix4("view", view);
        outlineShader.SetMatrix4("projection", projection);

        GL.BindVertexArray(vao);
        GL.DrawElements(PrimitiveType.Triangles, indices.Length, DrawElementsType.UnsignedInt, 0);
        GL.BindVertexArray(0);

        // --- 2️⃣ Disegna cubo arancio trasparente ---
        GL.CullFace(CullFaceMode.Back); // back faces cull → normale
        shader.Use();

        Matrix4 model = Matrix4.CreateTranslation(position);
        shader.SetMatrix4("model", model);
        shader.SetMatrix4("view", view);
        shader.SetMatrix4("projection", projection);
        shader.SetFloat("alpha", 0.5f); // trasparenza 50%

        GL.BindVertexArray(vao);
        GL.DrawElements(PrimitiveType.Triangles, indices.Length, DrawElementsType.UnsignedInt, 0);
        GL.BindVertexArray(0);
    }
    public void RenderUI(
    Vector2 screenPos,
    float size,
    Vector3 rotation,
    int[] textures,
    Vector2 screenSize
)
    {
        shaderCubeUI.Use();

        // bind texture facce
        for (int i = 0; i < 6; i++)
        {
            GL.ActiveTexture(TextureUnit.Texture0 + i);
            GL.BindTexture(TextureTarget.Texture2D, textures[i]);
        }

        shaderCubeUI.SetIntArray("uTextures", new[] { 0, 1, 2, 3, 4, 5 });
        shaderCubeUI.SetFloat("alpha", 1f);

        Matrix4 model =
            Matrix4.CreateScale(size) *
            Matrix4.CreateRotationX(MathHelper.DegreesToRadians(rotation.X)) *
            Matrix4.CreateRotationY(MathHelper.DegreesToRadians(rotation.Y)) *
            Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(rotation.Z)) *
            Matrix4.CreateTranslation(screenPos.X, screenPos.Y, 0);

        Matrix4 view = Matrix4.Identity;
        Matrix4 projection = Matrix4.CreateOrthographicOffCenter(
            0, screenSize.X,
            screenSize.Y, 0,
            -size, size
        );

        shaderCubeUI.SetMatrix4("model", model);
        shaderCubeUI.SetMatrix4("view", view);
        shaderCubeUI.SetMatrix4("projection", projection);

        GL.Enable(EnableCap.DepthTest);
        GL.DepthFunc(DepthFunction.Lequal);

        GL.BindVertexArray(vaoUI);
        GL.DrawElements(PrimitiveType.Triangles, 36, DrawElementsType.UnsignedInt, 0);
        GL.BindVertexArray(0);

        GL.Disable(EnableCap.DepthTest);
    }
}
