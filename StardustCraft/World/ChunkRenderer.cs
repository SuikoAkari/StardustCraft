using StardustCraft.Graphics;
using StardustCraft.Shaders;
using StardustCraft.World.Blocks;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace StardustCraft.World
{
    public class ChunkRenderer : IDisposable
    {
        // Cambia struttura: ora abbiamo mesh separate per ogni texture
        private Dictionary<int, (int vao, int vbo, int vertexCount)> meshByTexture = new();
        private bool disposed = false;

        public enum BlockFace
        {
            Top,
            Bottom,
            North,
            South,
            West,
            East
        }

        public struct BlockTextureData
        {
            public string TopTexturePath;
            public string BottomTexturePath;
            public string NorthTexturePath;
            public string SouthTexturePath;
            public string WestTexturePath;
            public string EastTexturePath;
        }

        // Cache per texture e dati
        private static Dictionary<(BlockType type, BlockFace face), int> textureCache = new();
        private static Dictionary<BlockType, BlockTextureData> blockTextureData = new();

        // Memorizza texture attualmente bound
        private static int currentBoundTexture = -1;

        public static void RegisterBlockTextures(BlockType type, BlockTextureData textureData)
        {
            blockTextureData[type] = textureData;

            LoadTexture(type, BlockFace.Top, textureData.TopTexturePath);
            LoadTexture(type, BlockFace.Bottom, textureData.BottomTexturePath);
            LoadTexture(type, BlockFace.North, textureData.NorthTexturePath);
            LoadTexture(type, BlockFace.South, textureData.SouthTexturePath);
            LoadTexture(type, BlockFace.West, textureData.WestTexturePath);
            LoadTexture(type, BlockFace.East, textureData.EastTexturePath);
        }

        private static void LoadTexture(BlockType type, BlockFace face, string texturePath)
        {
            if (string.IsNullOrEmpty(texturePath))
                return;

            try
            {
                int textureId = TextureLoader.GetTexture(texturePath);
                textureCache[(type, face)] = textureId;
                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Texture Loading Error {texturePath}: {ex.Message}");
            }
        }

        public static int GetTextureId(BlockType type, BlockFace face)
        {
            if (textureCache.TryGetValue((type, face), out int textureId))
                return textureId;

             
            return GenerateFallbackTexture();
        }
        private static int fallbackTexture = -1;
        private static int GenerateFallbackTexture()
        {
            if (fallbackTexture == -1)
            {
                int textureId = GL.GenTexture();
                GL.BindTexture(TextureTarget.Texture2D, textureId);
               
                byte[] pixels = new byte[4 * 4 * 4];
                for (int i = 0; i < 4 * 4; i++)
                {
                    int x = i % 4;
                    int y = i / 4;
                    bool isBlack = (x < 2) ^ (y < 2);

                    pixels[i * 4 + 0] = isBlack ? (byte)0 : (byte)255;
                    pixels[i * 4 + 1] = isBlack ? (byte)0 : (byte)255;
                    pixels[i * 4 + 2] = isBlack ? (byte)0 : (byte)255;
                    pixels[i * 4 + 3] = 255;
                }

                GL.TexImage2D(TextureTarget.Texture2D, 0,
                    PixelInternalFormat.Rgba, 4, 4, 0,
                    PixelFormat.Rgba, PixelType.UnsignedByte, pixels);

                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
                fallbackTexture = textureId;
                
            }
            return fallbackTexture;
        }

        private bool IsAir(Chunk chunk, int x, int y, int z)
        {
            // Fuori altezza verticale
            if (y < 0 || y >= Chunk.SizeY)
                return true;

            // Dentro il chunk
            if (x >= 0 && x < Chunk.Size && z >= 0 && z < Chunk.Size)
                return !chunk.GetBlock(x, y, z).IsSolid;

            // Calcola chunk adiacente
            int chunkX = chunk.X;
            int chunkZ = chunk.Z;
            int localX = x;
            int localZ = z;

            // Aggiusta coordinate se fuori dal chunk
            if (x < 0)
            {
                chunkX--;
                localX = Chunk.Size - 1;
            }
            else if (x >= Chunk.Size)
            {
                chunkX++;
                localX = 0;
            }

            if (z < 0)
            {
                chunkZ--;
                localZ = Chunk.Size - 1;
            }
            else if (z >= Chunk.Size)
            {
                chunkZ++;
                localZ = 0;
            }

            // Ottieni chunk adiacente
            if (Game.world == null)
            {
                return false;
            }
            var neighborChunk = Game.world.GetChunkAt(chunkX, chunkZ);
            if (neighborChunk != null)
            {
                return !neighborChunk.GetBlock(localX, y, localZ).IsSolid;
            }

            // Chunk non caricato → aria (true) per non mostrare facce verso chunk non esistenti
            return false;
        }

        public void PreBuildMesh(Chunk chunk)
        {
            verticesByTexture.Clear();
            for (int x = 0; x < Chunk.Size; x++)
                for (int y = 0; y < Chunk.SizeY; y++)
                    for (int z = 0; z < Chunk.Size; z++)
                    {
                        var block = chunk.GetBlock(x, y, z);
                        if (!block.IsSolid)
                            continue;

                        // Per ogni faccia visibile
                        AddFaceIfVisible(verticesByTexture, chunk, block.Type, x, y, z);
                    }

        }
        Dictionary<int, List<float>> verticesByTexture = new();
        public void BuildMesh(Chunk chunk)
        {
            DisposeBuffers();

            // Crea mesh separate per ogni texture
            foreach (var kvp in verticesByTexture)
            {
                int textureId = kvp.Key;
                List<float> vertices = kvp.Value;
                int vertexCount = vertices.Count / 9;

                if (vertexCount == 0)
                    continue;

                int vao = GL.GenVertexArray();
                int vbo = GL.GenBuffer();
                int stride = 9 * sizeof(float);
                GL.BindVertexArray(vao);
                GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
                GL.BufferData(BufferTarget.ArrayBuffer,
                    vertices.Count * sizeof(float),
                    vertices.ToArray(),
                    BufferUsageHint.StaticDraw);

                GL.VertexAttribPointer(
                0, 3,
     VertexAttribPointerType.Float,
     false,
     stride,
     0);
                GL.EnableVertexAttribArray(0);

                GL.VertexAttribPointer(
                    1, 2,
                    VertexAttribPointerType.Float,
                    false,
                    stride,
                    3 * sizeof(float));
                GL.EnableVertexAttribArray(1);

                GL.VertexAttribPointer(
                    2, 4,
                    VertexAttribPointerType.Float,
                    false,
                    stride,
                    5 * sizeof(float));
                GL.EnableVertexAttribArray(2);

                GL.BindVertexArray(0);
                GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

                meshByTexture[textureId] = (vao, vbo, vertexCount);
            }
        }

        private void AddFaceIfVisible(Dictionary<int, List<float>> verticesByTexture, Chunk chunk,
                                     BlockType type, int x, int y, int z)
        {
            void AddFace(BlockFace face, int textureId)
            {
                if (!verticesByTexture.ContainsKey(textureId))
                    verticesByTexture[textureId] = new List<float>();

                AddFaceToBuffer(verticesByTexture[textureId], type, face, x, y, z,chunk);
            }

            // Controlla visibilità e aggiungi faccia con texture corretta
            if (IsAir(chunk, x, y + 1, z))
                AddFace(BlockFace.Top, GetTextureId(type, BlockFace.Top));

            if (IsAir(chunk, x, y - 1, z))
                AddFace(BlockFace.Bottom, GetTextureId(type, BlockFace.Bottom));

            if (IsAir(chunk, x, y, z - 1))
                AddFace(BlockFace.North, GetTextureId(type, BlockFace.North));

            if (IsAir(chunk, x, y, z + 1))
                AddFace(BlockFace.South, GetTextureId(type, BlockFace.South));

            if (IsAir(chunk, x - 1, y, z))
                AddFace(BlockFace.West, GetTextureId(type, BlockFace.West));

            if (IsAir(chunk, x + 1, y, z))
                AddFace(BlockFace.East, GetTextureId(type, BlockFace.East));
        }
        public static float GetDirectionalBrightnessOld(BlockFace face)
        {
            float brightness = 1;
            if (face == BlockFace.South)
            {
                brightness = 0.9f;
            }
            else if(face == BlockFace.East)
            {
                brightness = 0.95f;
                    
            }
            else if (face == BlockFace.West)
            {
                brightness = 0.85f;
                   
            }
            else if (face == BlockFace.Top)
            {
                brightness = 1;
                    
            }
            else if (face == BlockFace.Bottom)
            {
                brightness = 0.75f;
                    
            }
            else if (face == BlockFace.North)
            {
                brightness = 0.8f;
                    
            }
            return brightness;
        }
        void AddFaceToBuffer(List<float> v, BlockType type, BlockFace face, int x, int y, int z, Chunk chunk)
        {
            var uv = GetFaceUV();
            float dynamicLight = chunk.GetFaceLight(x, y, z, face);
            
            float ambient = 0.2f; 
            float directional = GetDirectionalBrightnessOld(face);
            dynamicLight = Math.Clamp(dynamicLight, 0.1f, 1.0f);
            float finalLight = (directional*dynamicLight);
            
            void V(float px, float py, float pz, int uvIndex)
            {
                v.Add(px); v.Add(py); v.Add(pz);
                v.Add(uv[uvIndex].X); v.Add(uv[uvIndex].Y);
                v.Add(1);
                v.Add(1);
                v.Add(1);
                v.Add(finalLight);
            }

            switch (face)
            {
                case BlockFace.Top:
                    V(x, y + 1, z + 1, 0);
                    V(x + 1, y + 1, z + 1, 1);
                    V(x + 1, y + 1, z, 2);

                    V(x, y + 1, z + 1, 0);
                    V(x + 1, y + 1, z, 2);
                    V(x, y + 1, z, 3);
                    break;

                case BlockFace.Bottom:
                    // Primo triangolo invertito
                    V(x + 1, y, z, 2);
                    V(x + 1, y, z + 1, 1);
                    V(x, y, z + 1, 0);

                    // Secondo triangolo invertito
                    V(x, y, z, 3);
                    V(x + 1, y, z, 2);
                    V(x, y, z + 1, 0);
                    break;

                case BlockFace.North:
                    V(x + 1, y, z, 0);
                    V(x, y, z, 1);
                    V(x, y + 1, z, 2);
                    V(x + 1, y, z, 0);
                    V(x, y + 1, z, 2);
                    V(x + 1, y + 1, z, 3);
                    break;

                case BlockFace.South:
                    V(x, y, z + 1, 0);
                    V(x + 1, y, z + 1, 1);
                    V(x + 1, y + 1, z + 1, 2);
                    V(x, y, z + 1, 0);
                    V(x + 1, y + 1, z + 1, 2);
                    V(x, y + 1, z + 1, 3);
                    break;

                case BlockFace.West:
                    V(x, y, z, 0);
                    V(x, y, z + 1, 1);
                    V(x, y + 1, z + 1, 2);
                    V(x, y, z, 0);
                    V(x, y + 1, z + 1, 2);
                    V(x, y + 1, z, 3);
                    break;

                case BlockFace.East:
                    V(x + 1, y, z + 1, 0);
                    V(x + 1, y, z, 1);
                    V(x + 1, y + 1, z, 2);
                    V(x + 1, y, z + 1, 0);
                    V(x + 1, y + 1, z, 2);
                    V(x + 1, y + 1, z + 1, 3);
                    break;
            }
        }

        static Vector2[] GetFaceUV()
        {
            return new Vector2[]
            {
                new Vector2(0.0f, 0.0f),
                new Vector2(1.0f, 0.0f),
                new Vector2(1.0f, 1.0f),
                new Vector2(0.0f, 1.0f)
            };
        }

        public void Render(Shader shader)
        {
            foreach (var kvp in meshByTexture)
            {
                int textureId = kvp.Key;
                var (vao, _, vertexCount) = kvp.Value;

                // Bind della texture corretta
                BindTexture(textureId);

                // Passa al shader se necessario
                shader.SetInt("tex", 0); // Texture unit 0

                // Disegna la mesh
                GL.BindVertexArray(vao);
                GL.DrawArrays(PrimitiveType.Triangles, 0, vertexCount);
                GL.BindVertexArray(0);
            }
        }

        private static void BindTexture(int textureId)
        {
            // Ottimizzazione: bind solo se cambia texture
            if (currentBoundTexture != textureId)
            {
                GL.ActiveTexture(TextureUnit.Texture0);
                GL.BindTexture(TextureTarget.Texture2D, textureId);
                currentBoundTexture = textureId;
            }
        }

        private void DisposeBuffers()
        {
            foreach (var kvp in meshByTexture)
            {
                var (vao, vbo, _) = kvp.Value;

                if (vao != 0)
                    GL.DeleteVertexArray(vao);

                if (vbo != 0)
                    GL.DeleteBuffer(vbo);
            }

            meshByTexture.Clear();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                DisposeBuffers();
                disposed = true;
            }
        }

        ~ChunkRenderer()
        {
            Dispose(false);
        }

        public static void CleanupTextures()
        {
            foreach (var textureId in textureCache.Values)
            {
                GL.DeleteTexture(textureId);
            }
            textureCache.Clear();
            blockTextureData.Clear();
            currentBoundTexture = -1;
        }

        // DEBUG: Metodo per verificare le texture caricate
        public static void PrintLoadedTextures()
        {
            Console.WriteLine($"Textures caricate: {textureCache.Count}");
            foreach (var kvp in textureCache)
            {
                var (type, face) = kvp.Key;
                Console.WriteLine($"  {type}.{face} -> ID: {kvp.Value}");
            }
        }
    }
}