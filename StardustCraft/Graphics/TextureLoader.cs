using StardustCraft.Bundles.BundleSystem;
using OpenTK.Graphics.OpenGL4;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Image = SixLabors.ImageSharp.Image;

namespace StardustCraft.Graphics
{
    public static class TextureLoader
    {
        public static Dictionary<string,int> loadedTextures= new Dictionary<string,int>();

        public static int GetTexture(string path)
        {
            if (loadedTextures.ContainsKey(path))
            {
                return loadedTextures[path];
            }
            int tex = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, tex);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            byte[] file=BundleManager.Instance.LoadAsset<byte[]>(path);
            using (Image<Rgba32> image = Image.Load<Rgba32>(file))
            {
                image.Mutate(x => x.Flip(FlipMode.Vertical));

                var pixels = new byte[image.Width * image.Height * 4];
                image.CopyPixelDataTo(pixels);

                GL.TexImage2D(
                    TextureTarget.Texture2D,
                    0,
                    PixelInternalFormat.Rgba,
                    image.Width,
                    image.Height,
                    0,
                    PixelFormat.Rgba,
                    PixelType.UnsignedByte,
                    pixels
                );
            }

            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
            loadedTextures.Add(path, tex);
            return tex;
        }
    }
}
