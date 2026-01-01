using StardustCraft.Physics;
using StardustCraft.Shaders;
using StardustCraft.World.Blocks;
using OpenTK.Mathematics;
using System;
using static StardustCraft.World.World;

namespace StardustCraft.World
{
    public class Chunk : IDisposable
    {
        public const int Size = 16;
        public const int SizeY = 256;
        public int X { get; private set; }
        public int Z { get; private set; }
        public BlockType[,,] Blocks { get; private set; }
        public ChunkRenderer renderer;

        private List<AABB> collisionBoxes = new List<AABB>();
        public bool collisionDirty = true;
        public Matrix4 ModelMatrix { get; private set; }
        public bool rebuildChunk = false;
        public ChunkLightCalculator lightCalculator;
        int maxY = 0;
        public void BuildCollision()
        {
            collisionBoxes.Clear();
            bool[,,] visited = new bool[Size, SizeY + 1, Size];

            for (int z = 0; z < Size; z++)
            {
                for (int y = 0; y <= SizeY; y++)
                {
                    for (int x = 0; x < Size; x++)
                    {
                        if (visited[x, y, z])
                            continue;

                        var block = GetBlock(x, y, z);
                        if (!block.IsSolid)
                            continue;

                        // 1️⃣ Espandi lungo X
                        int maxX = x;
                        while (maxX + 1 < Size && !visited[maxX + 1, y, z] && GetBlock(maxX + 1, y, z).IsSolid)
                            maxX++;

                        // 2️⃣ Espandi lungo Y
                        int maxY = y;
                        bool canExpandY = true;
                        while (canExpandY && maxY + 1 <= SizeY)
                        {
                            for (int ix = x; ix <= maxX; ix++)
                                if (!GetBlock(ix, maxY + 1, z).IsSolid || visited[ix, maxY + 1, z])
                                {
                                    canExpandY = false;
                                    break;
                                }
                            if (canExpandY) maxY++;
                        }

                        // 3️⃣ Espandi lungo Z
                        int maxZ = z;
                        bool canExpandZ = true;
                        while (canExpandZ && maxZ + 1 < Size)
                        {
                            for (int ix = x; ix <= maxX; ix++)
                                for (int iy = y; iy <= maxY; iy++)
                                {
                                    if (!GetBlock(ix, iy, maxZ + 1).IsSolid || visited[ix, iy, maxZ + 1])
                                    {
                                        canExpandZ = false;
                                        break;
                                    }
                                }
                            if (canExpandZ) maxZ++;
                        }

                        // Segna tutti i blocchi della AABB come visitati
                        for (int ix = x; ix <= maxX; ix++)
                            for (int iy = y; iy <= maxY; iy++)
                                for (int iz = z; iz <= maxZ; iz++)
                                    visited[ix, iy, iz] = true;

                        // Crea la collision box unificata
                        collisionBoxes.Add(AABB.FromBlockRange(x, y, z, maxX, maxY, maxZ));
                    }
                }
            }

            collisionDirty = false;
        }

        // Verifica collisione con un AABB nel mondo
        public bool CheckCollision(ref AABB box, out Vector3 normal, out float penetration)
        {
            normal = Vector3.Zero;
            penetration = 0;

            if (collisionDirty)
            {

                BuildCollision();
                return false;
            }
                

            bool collided = false;
            float minPenetration = float.MaxValue;
            Vector3 bestNormal = Vector3.Zero;
            if(collisionBoxes.Count < 1)
            {
                return true;
            }
            foreach (var chunkBox in collisionBoxes)
            {
                // Converti box del chunk in coordinate mondiali
                AABB worldBox = new AABB
                {
                    Min = chunkBox.Min + new Vector3(X * Size, 0, Z * Size),
                    Max = chunkBox.Max + new Vector3(X * Size, 0, Z * Size)
                };

                if (box.Intersects(ref worldBox))
                {
                    // Calcola penetrazione su ogni asse
                    float penX = Math.Min(
                        box.Max.X - worldBox.Min.X,
                        worldBox.Max.X - box.Min.X
                    );

                    float penY = Math.Min(
                        box.Max.Y - worldBox.Min.Y,
                        worldBox.Max.Y - box.Min.Y
                    );

                    float penZ = Math.Min(
                        box.Max.Z - worldBox.Min.Z,
                        worldBox.Max.Z - box.Min.Z
                    );

                    // Trova l'asse con minore penetrazione (asse di collisione)
                    if (penX < penY && penX < penZ)
                    {
                        if (penX < minPenetration)
                        {
                            minPenetration = penX;
                            bestNormal = new Vector3(
                                box.Center.X < worldBox.Center.X ? -1 : 1, 0, 0);
                        }
                    }
                    else if (penY < penZ)
                    {
                        if (penY < minPenetration)
                        {
                            minPenetration = penY;
                            bestNormal = new Vector3(0,
                                box.Center.Y < worldBox.Center.Y ? -1 : 1, 0);
                        }
                    }
                    else
                    {
                        if (penZ < minPenetration)
                        {
                            minPenetration = penZ;
                            bestNormal = new Vector3(0, 0,
                                box.Center.Z < worldBox.Center.Z ? -1 : 1);
                        }
                    }

                    collided = true;
                }
            }

            if (collided)
            {
                normal = bestNormal;
                penetration = minPenetration;
            }

            return collided;
        }

        // Versione semplice per checking rapido
        public bool Intersects(ref AABB box)
        {
            if (collisionDirty)
                BuildCollision();

            foreach (var chunkBox in collisionBoxes)
            {
                AABB worldBox = new AABB
                {
                    Min = chunkBox.Min + new Vector3(X * Size, 0, Z * Size),
                    Max = chunkBox.Max + new Vector3(X * Size, 0, Z * Size)
                };

                if (box.Intersects(ref worldBox))
                    return true;
            }

            return false;
        }
        public Chunk(int x, int z)
        {
            X = x;
            Z = z;
            Blocks = new BlockType[Size, SizeY, Size];
            ModelMatrix = Matrix4.CreateTranslation(
                X * Size,
                0,
                Z * Size
            );
            renderer = new ChunkRenderer();
            lightCalculator = new ChunkLightCalculator(this);
        }

        public void RecalculateLight()
        {
            lightCalculator.CalculateAllLight();
        }
        public float GetFaceLight(int x, int y, int z, ChunkRenderer.BlockFace face)
        {
            return lightCalculator.GetLightForFace(x, y, z, face);
        }
        public void GenerateTerrain()
        {
            new Thread(() =>
            {
                float terrainScale = 0.002f;
                float heightScale = 80f;
                int baseHeight = 64;
                float biomeScale = 0.0005f;

                int regionSize = Size * 3;

                float[,] heightMap = new float[regionSize, regionSize];
                float[,] moistureMap = new float[regionSize, regionSize];
                float[,] temperatureMap = new float[regionSize, regionSize];
                BiomeType[,] biomeMap = new BiomeType[regionSize, regionSize];

                #region REGIONAL MAPS
                for (int rx = 0; rx < regionSize; rx++)
                {
                    for (int rz = 0; rz < regionSize; rz++)
                    {
                        int worldX = (X - 1) * Size + rx;
                        int worldZ = (Z - 1) * Size + rz;

                        float continental = PerlinNoise.OctaveNoise(
                            worldX, worldZ, 4, 0.6f, terrainScale * 0.1f);

                        float rugged = PerlinNoise.RidgedNoise(
                            worldX, worldZ, terrainScale) * 0.5f;

                        float detail = PerlinNoise.OctaveNoise(
                            worldX * 2, worldZ * 2, 2, 0.8f, terrainScale * 2f) * 0.2f;

                        heightMap[rx, rz] = continental + rugged + detail;

                        moistureMap[rx, rz] = PerlinNoise.OctaveNoise(
                            worldX + 5000, worldZ + 5000, 2, 0.5f, biomeScale);

                        float latitude = 1f - Math.Abs(worldZ) / 15000f;
                        temperatureMap[rx, rz] =
                            latitude +
                            PerlinNoise.Noise(worldX * 0.0001f, worldZ * 0.0001f) * 0.3f;
                    }
                }

                heightMap = GaussianBlur(heightMap, 3);
                moistureMap = GaussianBlur(moistureMap, 3);
                #endregion

                #region BIOME MAP
                for (int rx = 0; rx < regionSize; rx++)
                {
                    for (int rz = 0; rz < regionSize; rz++)
                    {
                        biomeMap[rx, rz] = DetermineBiome(
                            heightMap[rx, rz],
                            moistureMap[rx, rz],
                            temperatureMap[rx, rz]);
                    }
                }
                #endregion

                #region CHUNK GENERATION
                for (int x = 0; x < Size; x++)
                {
                    for (int z = 0; z < Size; z++)
                    {
                        int rx = x + Size;
                        int rz = z + Size;

                        float blendedOffset = 0f;
                        float blendedMultiplier = 0f;
                        float weightSum = 0f;

                        for (int dx = -1; dx <= 1; dx++)
                        {
                            for (int dz = -1; dz <= 1; dz++)
                            {
                                int nx = rx + dx;
                                int nz = rz + dz;

                                float dist = MathF.Sqrt(dx * dx + dz * dz);
                                float weight = 1f / (1f + dist);

                                var p = GetBiomeParams(biomeMap[nx, nz]);

                                blendedOffset += p.heightOffset * weight;
                                blendedMultiplier += p.heightMultiplier * weight;
                                weightSum += weight;
                            }
                        }

                        blendedOffset /= weightSum;
                        blendedMultiplier /= weightSum;

                        float baseNoiseHeight = heightMap[rx, rz] * heightScale;

                        int finalHeight = (int)MathF.Round(
     baseHeight +
     baseNoiseHeight * blendedMultiplier +
     blendedOffset
 );

                        BiomeType surfaceBiome = biomeMap[rx, rz];

                        GenerateGeologicalLayers(x, z, finalHeight, surfaceBiome);
                    }
                }
                #endregion

                AddTerrainDetails();
                RecalculateLight();
                renderer.PreBuildMesh(this);
                collisionDirty = true;
                
                Thread.Sleep(400);
                rebuildChunk = true;

            }).Start();
        }
        struct BiomeParams
        {
            public float heightOffset;
            public float heightMultiplier;
        }
        BiomeParams GetBiomeParams(BiomeType biome)
        {
            return biome switch
            {
                BiomeType.Mountains => new BiomeParams { heightOffset = 30, heightMultiplier = 1.4f },
                BiomeType.Plains => new BiomeParams { heightOffset = 0, heightMultiplier = 1.0f },
                BiomeType.Beach => new BiomeParams { heightOffset = -5, heightMultiplier = 0.9f },
                BiomeType.Ocean => new BiomeParams { heightOffset = -12, heightMultiplier = 0.9f },
                _ => new BiomeParams { heightOffset = 0, heightMultiplier = 1 }
            };
        }
        public void PreBuildMesh()
        {
            
            renderer.PreBuildMesh(this);
            collisionDirty = true;
        }
        private float CalculateBlendedHeight(int x, int y, float[,] heightMap, BiomeType[,] biomeMap)
        {
            float totalHeight = 0;
            float totalWeight = 0;

            // Blending 3x3
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    int nx = x + dx;
                    int ny = y + dy;

                    if (nx >= 0 && nx < heightMap.GetLength(0) &&
                        ny >= 0 && ny < heightMap.GetLength(1))
                    {
                        // Peso maggiore per il centro
                        float weight = dx == 0 && dy == 0 ? 1.0f : 0.125f;

                        // Se i biomi sono simili, blending più forte
                        if (biomeMap[nx, ny] == biomeMap[x, y])
                            weight *= 2.0f;

                        totalHeight += heightMap[nx, ny] * weight;
                        totalWeight += weight;
                    }
                }
            }

            return totalHeight / totalWeight;
        }

        private BiomeType CalculateBlendedBiome(int x, int y, BiomeType[,] biomeMap, float height, float moisture)
        {
            // Conta i biomi vicini
            Dictionary<BiomeType, int> neighborCount = new Dictionary<BiomeType, int>();

            for (int dx = -2; dx <= 2; dx++)
            {
                for (int dy = -2; dy <= 2; dy++)
                {
                    int nx = x + dx;
                    int ny = y + dy;

                    if (nx >= 0 && nx < biomeMap.GetLength(0) &&
                        ny >= 0 && ny < biomeMap.GetLength(1))
                    {
                        BiomeType neighborBiome = biomeMap[nx, ny];
                        if (!neighborCount.ContainsKey(neighborBiome))
                            neighborCount[neighborBiome] = 0;
                        neighborCount[neighborBiome]++;
                    }
                }
            }

            // Trova il biome più comune
            BiomeType mostCommon = biomeMap[x, y];
            int maxCount = 0;

            foreach (var kvp in neighborCount)
            {
                if (kvp.Value > maxCount)
                {
                    maxCount = kvp.Value;
                    mostCommon = kvp.Key;
                }
            }

            // Transizioni speciali basate su condizioni
            if (mostCommon == BiomeType.Mountains && height < 0.55f && moisture > 0.6f)
            {
                return BiomeType.Forest; // Montagne con umidità diventano colline boscose
            }
            else if (mostCommon == BiomeType.Plains && height > 0.5f)
            {
                return BiomeType.Hills; // Pianure in salita diventano colline
            }

            return mostCommon;
        }

        private int CalculateFinalHeight(int x, int z, float baseHeight, BiomeType biome, float rawHeight)
        {
            int worldX = X * Size + x;
            int worldZ = Z * Size + z;

            // Altezza base del biome
            int biomeBase = GetBiomeBaseHeight(biome);

            // Noise specifico per il biome
            float biomeNoise = 0;

            switch (biome)
            {
                case BiomeType.Mountains:
                    // Montagne realistiche con catene
                    float mountainRidge = PerlinNoise.RidgedNoise(worldX * 0.8f, worldZ * 0.8f, 0.002f) * 80f;
                    float mountainDetail = PerlinNoise.OctaveNoise(worldX * 3, worldZ * 3, 2, 0.7f, 0.008f) * 15f;
                    float mountainValleys = Math.Abs(PerlinNoise.SimplexNoise(worldX * 0.003f, worldZ * 0.003f)) * 20f;
                    biomeNoise = mountainRidge + mountainDetail - mountainValleys;
                    break;

                case BiomeType.SnowyMountains:
                    // Montagne innevate più alte
                    float snowRidge = PerlinNoise.RidgedNoise(worldX * 0.7f, worldZ * 0.7f, 0.0015f) * 100f;
                    float snowPlateaus = Math.Clamp(PerlinNoise.Noise(worldX * 0.002f, worldZ * 0.002f) + 0.5f, 0, 1) * 40f;
                    biomeNoise = snowRidge + snowPlateaus;
                    break;

                case BiomeType.Hills:
                    // Colline dolci
                    float hillLarge = PerlinNoise.OctaveNoise(worldX, worldZ, 2, 0.6f, 0.005f) * 30f;
                    float hillSmall = PerlinNoise.OctaveNoise(worldX * 2, worldZ * 2, 1, 0.3f, 0.02f) * 8f;
                    biomeNoise = hillLarge + hillSmall;
                    break;

                case BiomeType.Forest:
                    // Foresta con leggere variazioni
                    biomeNoise = PerlinNoise.OctaveNoise(worldX * 1.5f, worldZ * 1.5f, 1, 0.4f, 0.01f) * 6f;
                    break;

                case BiomeType.Swamp:
                    // Palude piatta con depressioni
                    biomeNoise = (PerlinNoise.Noise(worldX * 0.03f, worldZ * 0.03f) - 0.5f) * 2f;
                    break;

                default:
                    biomeNoise = PerlinNoise.OctaveNoise(worldX, worldZ, 1, 0.3f, 0.01f) * 10f;
                    break;
            }

            // Aggiungi rumore di dettaglio generale
            float detailNoise = PerlinNoise.OctaveNoise(worldX * 4, worldZ * 4, 1, 0.9f, 0.05f) * 2f;

            // Calcola altezza finale
            float biomeHeightMultiplier = GetBiomeHeightMultiplier(biome);
            int height = biomeBase +
                       (int)(baseHeight * 60f * biomeHeightMultiplier) +
                       (int)biomeNoise +
                       (int)detailNoise;

            // Riveri e laghi (solo per alcuni biomi)
            if (biome != BiomeType.Ocean && biome != BiomeType.Beach)
            {
                float riverNoise = Math.Abs(PerlinNoise.SimplexNoise(worldX * 0.001f, worldZ * 0.001f));
                if (riverNoise < 0.02f && height > 60 && height < 75)
                {
                    // Forma valle fluviale
                    height = 58 + (int)(Math.Sin(worldX * 0.01f) * 3f);
                    biomeNoise = (height - biomeBase) * 0.5f;
                }
            }

            return Math.Clamp(height, 0, SizeY - 1);
        }

        private void GenerateGeologicalLayers(int x, int z, int surfaceHeight, BiomeType biome)
        {
            for (int y = 0; y < SizeY; y++)
            {
                BlockType blockType = BlockType.Air;

                // Strati geologici
                if (y == 0)
                {
                    blockType = BlockType.Bedrock;
                }
                else if (y < 5)
                {
                    // Bedrock irregolare
                    if (PerlinNoise.Noise(x * 0.5f, z * 0.5f, y * 0.5f) > 0.3f)
                        blockType = BlockType.Bedrock;
                    else
                        blockType = BlockType.Stone;
                }
                else if (y < surfaceHeight - 20)
                {
                    // Strato di pietra profonda
                    blockType = BlockType.Stone;

                    // Ore con distribuzione realistica
                    if (y < 32)
                    {
                        float oreDensity = (32 - y) / 32f; // Più comune in profondità

                        // Ferro (strati medi)
                        if (y > 16 && y < 64 && PerlinNoise.Noise(x * 0.8f, z * 0.8f, y * 0.8f) > 0.8f * oreDensity)
                        {
                            blockType = BlockType.IronOre;
                        }
                        // Carbone (superficiale)
                        else if (y < 128 && PerlinNoise.Noise(x * 0.7f, z * 0.7f, y * 0.7f) > 0.75f * oreDensity)
                        {
                            blockType = BlockType.CoalOre;
                        }
                    }
                }
                else if (y < surfaceHeight - 6)
                {
                    // Strato di transizione
                    float transition = (y - (surfaceHeight - 20)) / 14f;
                    if (PerlinNoise.Noise(x * 0.6f, z * 0.6f, y * 0.6f) > transition * 1.2f)
                        blockType = BlockType.Dirt;
                    else
                        blockType = BlockType.Stone;
                }
                else if (y < surfaceHeight - 1)
                {
                    // Strato di terra
                    blockType = BlockType.Dirt;
                }
                else if (y == surfaceHeight - 1)
                {
                    // Superficie (dipende dal biome e dall'umidità)
                    blockType = GetSurfaceBlock(biome, surfaceHeight, x, z);
                }
                else if (y < 62 && y >= surfaceHeight)
                {
                    // Acqua solo se sotto il livello del mare
                    if (surfaceHeight < 62)
                    {
                        blockType = BlockType.Water;
                    }
                }

                // Grotte (escludendo gli strati superficiali)
                if (y > 10 && y < surfaceHeight - 10 && blockType != BlockType.Air && blockType != BlockType.Water)
                {
                    if (IsCave(x, y, z, surfaceHeight))
                    {
                        blockType = BlockType.Air;
                    }
                }

                Blocks[x, y, z] = blockType;
            }
        }

        private bool IsCave(int x, int y, int z, int surfaceHeight)
        {
            int worldX = X * Size + x;
            int worldY = y;
            int worldZ = Z * Size + z;

            // Noise 3D per le grotte
            float cave1 = PerlinNoise.OctaveNoise3D(worldX, worldY, worldZ, 2, 0.5f, 0.03f);
            float cave2 = PerlinNoise.OctaveNoise3D(worldX * 1.5f, worldY * 1.5f, worldZ * 1.5f, 1, 0.8f, 0.05f);

            // Le grotte sono più comuni in profondità
            float depthFactor = 1.0f - (float)(y - 10) / (surfaceHeight - 20);
            depthFactor = Math.Clamp(depthFactor, 0.1f, 1.0f);

            // Grotte più grandi in profondità
            float threshold = 0.65f - depthFactor * 0.3f;

            return (cave1 > threshold && cave2 > 0.4f);
        }

        private BiomeType DetermineBiome(float height, float moisture, float temperature)
        {
            // Sistema di transizione fluida tra biomi
            float h = height;
            float m = moisture;
            float t = temperature;

            // Aree estreme (montagne e oceani)
            if (h > 0.7f)
            {
                if (t < 0.3f)
                    return BiomeType.SnowyMountains;
                else if (t < 0.6f)
                    return BiomeType.Mountains;
                else
                    return BiomeType.Hills; // Montagne più basse in climi caldi
            }
            else if (h < 0.2f)
            {
                if (h < 0.1f)
                    return BiomeType.Ocean;
                else
                    return BiomeType.Beach;
            }

            // Biomimain basati su temperatura e umidità
            if (t < 0.3f) // Freddo
            {
                if (m < 0.33f)
                    return BiomeType.Plains;
                else if (m < 0.66f)
                    return BiomeType.Forest;
                else
                    return BiomeType.Swamp;
            }
            else if (t < 0.6f) // Temperato
            {
                if (m < 0.33f)
                    return BiomeType.Plains;
                else if (m < 0.66f)
                    return BiomeType.Forest;
                else
                {
                    if (h > 0.4f)
                        return BiomeType.Hills;
                    else
                        return BiomeType.Swamp;
                }
            }
            else // Caldo
            {
                if (m < 0.33f)
                    return BiomeType.Plains;
                else if (m < 0.66f)
                    return BiomeType.Forest;
                else
                {
                    if (h < 0.4f)
                        return BiomeType.Swamp;
                    else
                        return BiomeType.Hills;
                }
            }
        }

        private int GetBiomeBaseHeight(BiomeType biome)
        {
            return biome switch
            {
                BiomeType.Ocean => 48,
                BiomeType.Beach => 62,
                BiomeType.Plains => 64,
                BiomeType.Forest => 66,
                BiomeType.Hills => 68,  // Rimosso salto netto
                BiomeType.Mountains => 72, // Base più bassa per transizioni
                BiomeType.SnowyMountains => 80,
                BiomeType.Swamp => 62,
                BiomeType.River => 58,
                _ => 64
            };
        }

        private float GetBiomeHeightMultiplier(BiomeType biome)
        {
            return biome switch
            {
                BiomeType.Mountains => 2.0f, // Maggiore variabilità
                BiomeType.SnowyMountains => 2.2f,
                BiomeType.Hills => 1.3f,
                BiomeType.Forest => 0.8f,
                BiomeType.Swamp => 0.5f,
                BiomeType.Plains => 0.6f,
                _ => 1.0f
            };
        }

        private BlockType GetSurfaceBlock(BiomeType biome, int height, int x, int z)
        {
            int worldX = X * Size + x;
            int worldZ = Z * Size + z;
            float noise = PerlinNoise.Noise(worldX * 0.1f, worldZ * 0.1f);

            switch (biome)
            {
                case BiomeType.SnowyMountains:
                    // Neve sopra una certa altezza
                    if (height > 90)
                        return BlockType.SnowGrass;
                    else if (height > 80)
                        return noise > 0.6f ? BlockType.SnowGrass : BlockType.Stone;
                    else
                        return BlockType.Stone;

                case BiomeType.Mountains:
                    // Rocce esposte in cima, erba sui pendii
                    if (height > 100)
                        return BlockType.Stone;
                    else if (height > 85)
                        return noise > 0.7f ? BlockType.Stone : BlockType.Grass;
                    else
                        return BlockType.Grass;

                case BiomeType.Hills:
                    // Colline con affioramenti rocciosi
                    if (noise > 0.8f && height > 75)
                        return BlockType.Stone;
                    return BlockType.Grass;

                case BiomeType.Forest:
                    // Foresta con occasionali patch di podzol
                    if (noise > 0.9f)
                        return BlockType.Podzol;
                    return BlockType.Grass;

                case BiomeType.Swamp:
                    // Palude con acqua e erba
                    if (noise > 0.7f && height < 64)
                        return BlockType.Water;
                    return BlockType.Grass;

                case BiomeType.Beach:
                    return BlockType.Sand;

                case BiomeType.Ocean:
                    return BlockType.Gravel;

                case BiomeType.River:
                    return height < 60 ? BlockType.Gravel : BlockType.Grass;

                default:
                    return BlockType.Grass;
            }
        }

        private void AddTerrainDetails()
        {
            // Aggiungi alberi, fiori, rocce, ecc.
            Random rand = new Random(X * 1000 + Z);

            for (int x = 2; x < Size - 2; x++)
            {
                for (int z = 2; z < Size - 2; z++)
                {
                    int worldX = X * Size + x;
                    int worldZ = Z * Size + z;

                    // Trova la superficie
                    int surfaceY = -1;
                    for (int y = SizeY - 1; y >= 0; y--)
                    {
                        if (Blocks[x, y, z] != BlockType.Air &&
                            Blocks[x, y, z] != BlockType.Water)
                        {
                            surfaceY = y;
                            break;
                        }
                    }

                    if (surfaceY > 0 && surfaceY < SizeY - 10)
                    {
                        BlockType surfaceBlock = Blocks[x, surfaceY, z];
                        BiomeType biome = DetermineBiomeForDetails(x, z);

                        // Alberi nella foresta
                        if (biome == BiomeType.Forest && surfaceBlock == BlockType.Grass)
                        {
                            float treeNoise = PerlinNoise.Noise(worldX * 0.3f, worldZ * 0.3f);
                            if (treeNoise > 0.7f && HasSpaceForTree(x, surfaceY, z))
                            {
                                GenerateTree(x, surfaceY + 1, z, rand);
                            }
                        }

                        // Fiori nelle pianure
                        else if (biome == BiomeType.Plains && surfaceBlock == BlockType.Grass)
                        {
                            float flowerNoise = PerlinNoise.Noise(worldX * 0.5f, worldZ * 0.5f);
                            if (flowerNoise > 0.8f)
                            {
                                Blocks[x, surfaceY + 1, z] = BlockType.Rose;
                            }
                            else if (flowerNoise < 0.2f)
                            {
                                Blocks[x, surfaceY + 1, z] = BlockType.Dandelion;
                            }
                        }
                    }
                }
            }
        }

        private bool HasSpaceForTree(int x, int y, int z)
        {
            // Controlla se c'è spazio per un albero
            for (int dy = 1; dy <= 6; dy++)
            {
                for (int dx = -2; dx <= 2; dx++)
                {
                    for (int dz = -2; dz <= 2; dz++)
                    {
                        int nx = x + dx;
                        int nz = z + dz;

                        if (nx >= 0 && nx < Size && nz >= 0 && nz < Size)
                        {
                            if (Blocks[nx, y + dy, nz] != BlockType.Air)
                                return false;
                        }
                    }
                }
            }
            return true;
        }

        private void GenerateTree(int x, int y, int z, Random rand)
        {
            // Tronco
            int trunkHeight = 4 + rand.Next(3);
            for (int dy = 0; dy < trunkHeight; dy++)
            {
                if (y + dy < SizeY)
                    Blocks[x, y + dy, z] = BlockType.OakLog;
            }

            // Foglie
            int leafY = y + trunkHeight;
            for (int ly = 0; ly < 3; ly++)
            {
                int radius = ly == 0 ? 2 : ly == 1 ? 3 : 2;
                for (int dx = -radius; dx <= radius; dx++)
                {
                    for (int dz = -radius; dz <= radius; dz++)
                    {
                        if (Math.Abs(dx) + Math.Abs(dz) <= radius * 1.5f)
                        {
                            int nx = x + dx;
                            int nz = z + dz;
                            int ny = leafY - 1 + ly;

                            if (nx >= 0 && nx < Size && nz >= 0 && nz < Size && ny >= 0 && ny < SizeY)
                            {
                                if (Blocks[nx, ny, nz] == BlockType.Air)
                                    Blocks[nx, ny, nz] = BlockType.OakLeaves;
                            }
                        }
                    }
                }
            }
        }

        private BiomeType DetermineBiomeForDetails(int x, int z)
        {
            int worldX = X * Size + x;
            int worldZ = Z * Size + z;

            float height = PerlinNoise.OctaveNoise(worldX, worldZ, 3, 0.5f, 0.002f);
            float moisture = PerlinNoise.OctaveNoise(worldX + 500, worldZ + 500, 2, 0.5f, 0.001f);
            float temp = 1.0f - Math.Abs(worldZ) / 15000f;

            return DetermineBiome(height, moisture, temp);
        }

        private float[,] GaussianBlur(float[,] input, int radius)
        {
            int width = input.GetLength(0);
            int height = input.GetLength(1);
            float[,] output = new float[width, height];

            for (int x = radius; x < width - radius; x++)
            {
                for (int y = radius; y < height - radius; y++)
                {
                    float sum = 0;
                    float weight = 0;

                    for (int dx = -radius; dx <= radius; dx++)
                    {
                        for (int dy = -radius; dy <= radius; dy++)
                        {
                            float w = 1.0f / (dx * dx + dy * dy + 1);
                            sum += input[x + dx, y + dy] * w;
                            weight += w;
                        }
                    }

                    output[x, y] = sum / weight;
                }
            }

            return output;
        }

        public void RebuildMesh()
        {
            renderer.BuildMesh(this);
        }

        public void Render(Shader shader)
        {
            shader.SetMatrix4("model", ModelMatrix);
            if (rebuildChunk)
            {
                rebuildChunk = false;
                RebuildMesh();
            }
            renderer.Render(shader);
        }

        public Block GetBlock(int x, int y, int z)
        {
            if (x >= 0 && x < Size && y >= 0 && y < 256 && z >= 0 && z < Size)
                return BlockManager.GetBlock(Blocks[x, y, z]);
            return BlockManager.GetBlock(BlockType.Air);
        }

        public void SetBlock(int x, int y, int z, BlockType block)
        {
            if (x >= 0 && x < Size && y >= 0 && y < 256 && z >= 0 && z < Size)
                Blocks[x, y, z] = block;
            RecalculateLight();

        }

        public void Dispose()
        {
            renderer.Dispose();
            
        }

        public float DistanceTo(Vector3 position)
        {
            Vector3 chunkCenter = new Vector3(
                X * Size + Size / 2,
                0,
                Z * Size + Size / 2
            );
            return Vector3.DistanceSquared(chunkCenter, position);
        }
    }

    public struct Vector2i
    {
        public int X;
        public int Z;

        public Vector2i(int x, int z)
        {
            X = x;
            Z = z;
        }
    }
}