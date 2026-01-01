using OpenTK.Mathematics;
using System;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System.IO;
using System.Reflection.Metadata;
using StardustCraft.Bundles.BundleSystem;

namespace StardustCraft.Shaders;

public class Shader
{
    public int Handle;

    public Shader(string vertPath, string fragPath)
    {
        int vert = GL.CreateShader(ShaderType.VertexShader);
        string vertShaderTxt = BundleManager.Instance.GetFileText("shaders/" + vertPath);
        GL.ShaderSource(vert, vertShaderTxt);
        Console.WriteLine("Compiling shader: "+vertPath);
        GL.CompileShader(vert);
        
        int frag = GL.CreateShader(ShaderType.FragmentShader);
        string fragShaderTxt = BundleManager.Instance.GetFileText("shaders/"+fragPath);
        GL.ShaderSource(frag, fragShaderTxt);
        Console.WriteLine("Compiling shader: " + fragPath);
        GL.CompileShader(frag);

        Handle = GL.CreateProgram();
        GL.AttachShader(Handle, vert);
        GL.AttachShader(Handle, frag);
        GL.LinkProgram(Handle);

        GL.DeleteShader(vert);
        GL.DeleteShader(frag);
    }
    public void Dispose() => GL.DeleteProgram(Handle);
    public void Use() => GL.UseProgram(Handle);
    public uint GetAttribLocation(string name) => (uint)GL.GetAttribLocation(Handle, name);
    public void SetMatrix4(string name, Matrix4 mat)
    {
        int loc = GL.GetUniformLocation(Handle, name);
        GL.UniformMatrix4(loc, false, ref mat);
    }
    public void SetInt(string name, int value)
    {
        int location = GL.GetUniformLocation(Handle, name);
        GL.Uniform1(location, value);
    }
    public void SetFloat(string name, float value)
    {
        int location = GL.GetUniformLocation(Handle, name);
        GL.Uniform1(location, value);
    }

    public void SetVector2(string name, Vector2 value)
    {
        int location = GL.GetUniformLocation(Handle, name);
        GL.Uniform2(location, value);
    }

    public void SetVector3(string name, Vector3 value)
    {
        int location = GL.GetUniformLocation(Handle, name);
        GL.Uniform3(location, value);
    }
}
