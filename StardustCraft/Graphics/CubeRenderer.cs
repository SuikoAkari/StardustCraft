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

    private int vao, vbo, ebo;
    private Shader shader;
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

        // Shader (minimal)
        shader = new Shader("cube.vert", "cube.frag");
        shader.Use();
        outlineShader = new Shader("cube.vert", "outline.frag");
        outlineShader.Use();
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

}
