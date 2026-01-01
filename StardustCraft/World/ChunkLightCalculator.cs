using StardustCraft.World.Blocks;
using System.Runtime.CompilerServices;

namespace StardustCraft.World;

public class ChunkLightCalculator
{
    private const int CHUNK_SIZE = 16;
    private const int WORLD_HEIGHT = 256;
    
    private Chunk chunk;
    private byte[,,] sunlight;
    private byte[,,] blockLight;
    private bool[,,] lightCalculated;
    
    public ChunkLightCalculator(Chunk chunk)
    {
        this.chunk = chunk;
        sunlight = new byte[CHUNK_SIZE, WORLD_HEIGHT, CHUNK_SIZE];
        blockLight = new byte[CHUNK_SIZE, WORLD_HEIGHT, CHUNK_SIZE];
        lightCalculated = new bool[CHUNK_SIZE, WORLD_HEIGHT, CHUNK_SIZE];
    }
    
    // === METODI BASE ===
    
    private bool IsInBounds(int x, int y, int z)
    {
        return x >= 0 && x < CHUNK_SIZE && 
               y >= 0 && y < WORLD_HEIGHT && 
               z >= 0 && z < CHUNK_SIZE;
    }
    
    private bool IsOpaque(int x, int y, int z)
    {
        if (!IsInBounds(x, y, z))
        {
            // Fuori dal chunk, controlla nel chunk vicino se necessario
            return !GetBlockFromWorld(x + chunk.X, y, z + chunk.Z).IsTransparent;
        }
        
        Block block = chunk.GetBlock(x, y, z);
        return !block.IsTransparent;
    }
    
    private bool IsTransparent(int x, int y, int z)
    {
        if (!IsInBounds(x, y, z))
        {
            Block block = GetBlockFromWorld(x + chunk.X, y, z + chunk.Z);
            return block.IsTransparent;
        }
        
        Block bl = chunk.GetBlock(x, y, z);
        return bl.IsTransparent;
    }
    
    private byte GetTotalLight(int x, int y, int z)
    {
        if (!IsInBounds(x, y, z)) return 0;
        
        // Combinazione luce sole + luce blocchi
        return (byte)Math.Max(sunlight[x, y, z], blockLight[x, y, z]);
    }
    public byte GetSunlight(int x, int y, int z)
    {
        if (!IsInBounds(x, y, z)) return 0;
        return sunlight[x, y, z];
    }

    public byte GetBlocklight(int x, int y, int z)
    {
        if (!IsInBounds(x, y, z)) return 0;
        return blockLight[x, y, z];
    }
    private void SetLight(int x, int y, int z, byte lightValue, LightType type)
    {
        if (!IsInBounds(x, y, z)) return;
        
        switch (type)
        {
            case LightType.Sunlight:
                sunlight[x, y, z] = lightValue;
                break;
            case LightType.Torch:
                blockLight[x, y, z] = lightValue;
                break;
        }
        
        lightCalculated[x, y, z] = true;
    }
    
    private int GetHighestBlockAt(int x, int z)
    {
        if (!IsInBounds(x, 0, z)) return 0;
        
        // Scendi dall'alto fino a trovare un blocco solido
        for (int y = WORLD_HEIGHT - 1; y >= 0; y--)
        {
            Block block = chunk.GetBlock(x, y, z);
            if (block.Type != BlockType.Air && block.IsSolid)
            {
                return y;
            }
        }
        return 0;
    }

    // === SISTEMA COMPLETO DI CALCOLO LUCE ===
    bool isPropagating = false;
    public void CalculateAllLight()
    {
        if (isPropagating) return;
        isPropagating = true;

        Array.Clear(sunlight, 0, sunlight.Length);
        Array.Clear(blockLight, 0, blockLight.Length);
        Array.Clear(lightCalculated, 0, lightCalculated.Length);

        CalculateSunlight();
        InitializeLightFromNeighbors();
        PropagateSunlightHorizontally();  // buchi (Minecraft-style)
        
        FindLightSources();               // torce

        PropagateAllLight();            // BFS classico SOLO torce

        isPropagating = false;
    }
    private void InitializeLightFromNeighbors()
    {
        // Inizializza SOLO i bordi del chunk che sono trasparenti verso l'esterno
        // Questo evita di rendere tutti i bordi luminosi

        // Bordi est/ovest (x = 0 e x = CHUNK_SIZE-1)
        for (int y = 0; y < WORLD_HEIGHT; y++)
        {
            for (int z = 0; z < CHUNK_SIZE; z++)
            {
                // Bordo est (x = CHUNK_SIZE-1)
                if (IsTransparent(CHUNK_SIZE - 1, y, z))
                {
                    // Ottieni la luce dal chunk a est
                    byte neighborSunlight = GetSunlightFromNeighbor(CHUNK_SIZE, y, z);
                    byte neighborBlocklight = GetBlocklightFromNeighbor(CHUNK_SIZE, y, z);

                    if (neighborSunlight > sunlight[CHUNK_SIZE - 1, y, z])
                    {
                        sunlight[CHUNK_SIZE - 1, y, z] = neighborSunlight;
                        if (neighborSunlight > 1)
                        {
                            lightPropagationQueue.Enqueue(new LightNode(
                                CHUNK_SIZE - 1, y, z, neighborSunlight, LightType.Sunlight));
                        }
                    }

                    if (neighborBlocklight > blockLight[CHUNK_SIZE - 1, y, z])
                    {
                        blockLight[CHUNK_SIZE - 1, y, z] = neighborBlocklight;
                        if (neighborBlocklight > 1)
                        {
                            lightPropagationQueue.Enqueue(new LightNode(
                                CHUNK_SIZE - 1, y, z, neighborBlocklight, LightType.Torch));
                        }
                    }
                }

                // Bordo ovest (x = 0)
                if (IsTransparent(0, y, z))
                {
                    // Ottieni la luce dal chunk a ovest
                    byte neighborSunlight = GetSunlightFromNeighbor(-1, y, z);
                    byte neighborBlocklight = GetBlocklightFromNeighbor(-1, y, z);

                    if (neighborSunlight > sunlight[0, y, z])
                    {
                        sunlight[0, y, z] = neighborSunlight;
                        if (neighborSunlight > 1)
                        {
                            lightPropagationQueue.Enqueue(new LightNode(
                                0, y, z, neighborSunlight, LightType.Sunlight));
                        }
                    }

                    if (neighborBlocklight > blockLight[0, y, z])
                    {
                        blockLight[0, y, z] = neighborBlocklight;
                        if (neighborBlocklight > 1)
                        {
                            lightPropagationQueue.Enqueue(new LightNode(
                                0, y, z, neighborBlocklight, LightType.Torch));
                        }
                    }
                }
            }
        }

        // Bordi nord/sud (z = 0 e z = CHUNK_SIZE-1)
        for (int y = 0; y < WORLD_HEIGHT; y++)
        {
            for (int x = 0; x < CHUNK_SIZE; x++)
            {
                // Bordo sud (z = CHUNK_SIZE-1)
                if (IsTransparent(x, y, CHUNK_SIZE - 1))
                {
                    byte neighborSunlight = GetSunlightFromNeighbor(x, y, CHUNK_SIZE);
                    byte neighborBlocklight = GetBlocklightFromNeighbor(x, y, CHUNK_SIZE);

                    if (neighborSunlight > sunlight[x, y, CHUNK_SIZE - 1])
                    {
                        sunlight[x, y, CHUNK_SIZE - 1] = neighborSunlight;
                        if (neighborSunlight > 1)
                        {
                            lightPropagationQueue.Enqueue(new LightNode(
                                x, y, CHUNK_SIZE - 1, neighborSunlight, LightType.Sunlight));
                        }
                    }

                    if (neighborBlocklight > blockLight[x, y, CHUNK_SIZE - 1])
                    {
                        blockLight[x, y, CHUNK_SIZE - 1] = neighborBlocklight;
                        if (neighborBlocklight > 1)
                        {
                            lightPropagationQueue.Enqueue(new LightNode(
                                x, y, CHUNK_SIZE - 1, neighborBlocklight, LightType.Torch));
                        }
                    }
                }

                // Bordo nord (z = 0)
                if (IsTransparent(x, y, 0))
                {
                    byte neighborSunlight = GetSunlightFromNeighbor(x, y, -1);
                    byte neighborBlocklight = GetBlocklightFromNeighbor(x, y, -1);

                    if (neighborSunlight > sunlight[x, y, 0])
                    {
                        sunlight[x, y, 0] = neighborSunlight;
                        if (neighborSunlight > 1)
                        {
                            lightPropagationQueue.Enqueue(new LightNode(
                                x, y, 0, neighborSunlight, LightType.Sunlight));
                        }
                    }

                    if (neighborBlocklight > blockLight[x, y, 0])
                    {
                        blockLight[x, y, 0] = neighborBlocklight;
                        if (neighborBlocklight > 1)
                        {
                            lightPropagationQueue.Enqueue(new LightNode(
                                x, y, 0, neighborBlocklight, LightType.Torch));
                        }
                    }
                }
            }
        }

        // E i vertici/angoli? Sono già coperti dai bordi sopra
    }

    private byte GetSunlightFromNeighbor(int localX, int y, int localZ)
    {
        int worldX = chunk.X * CHUNK_SIZE + localX;
        int worldZ = chunk.Z * CHUNK_SIZE + localZ;

        // Calcola chunk vicino
        int neighborChunkX = (int)MathF.Floor((float)worldX / CHUNK_SIZE);
        int neighborChunkZ = (int)MathF.Floor((float)worldZ / CHUNK_SIZE);

        // Se è lo stesso chunk, restituisci la luce interna
        if (neighborChunkX == chunk.X && neighborChunkZ == chunk.Z)
        {

            return 0;
        }

        // Ottieni chunk vicino
        Chunk neighbor = Game.world.GetChunkAt(neighborChunkX, neighborChunkZ);
        if (neighbor == null || neighbor.lightCalculator == null)
            return 0;

        // Calcola coordinate locali nel chunk vicino
        int neighborLocalX = ((worldX % CHUNK_SIZE) + CHUNK_SIZE) % CHUNK_SIZE;
        int neighborLocalZ = ((worldZ % CHUNK_SIZE) + CHUNK_SIZE) % CHUNK_SIZE;

        return neighbor.lightCalculator.GetSunlight(neighborLocalX, y, neighborLocalZ);
    }

    private byte GetBlocklightFromNeighbor(int localX, int y, int localZ)
    {
        int worldX = chunk.X * CHUNK_SIZE + localX;
        int worldZ = chunk.Z * CHUNK_SIZE + localZ;

        int neighborChunkX = (int)MathF.Floor((float)worldX / CHUNK_SIZE);
        int neighborChunkZ = (int)MathF.Floor((float)worldZ / CHUNK_SIZE);

        if (neighborChunkX == chunk.X && neighborChunkZ == chunk.Z)
        {
            int adjX = ((localX % CHUNK_SIZE) + CHUNK_SIZE) % CHUNK_SIZE;
            int adjZ = ((localZ % CHUNK_SIZE) + CHUNK_SIZE) % CHUNK_SIZE;

            if (IsInBounds(adjX, y, adjZ))
                return blockLight[adjX, y, adjZ];
            return 0;
        }

        Chunk neighbor = Game.world.GetChunkAt(neighborChunkX, neighborChunkZ);
        if (neighbor == null || neighbor.lightCalculator == null)
            return 0;

        int neighborLocalX = ((worldX % CHUNK_SIZE) + CHUNK_SIZE) % CHUNK_SIZE;
        int neighborLocalZ = ((worldZ % CHUNK_SIZE) + CHUNK_SIZE) % CHUNK_SIZE;

        return neighbor.lightCalculator.GetBlocklight(neighborLocalX, y, neighborLocalZ);
    }
    private void CalculateSunlight()
    {
        for (int x = 0; x < CHUNK_SIZE; x++)
            for (int z = 0; z < CHUNK_SIZE; z++)
            {
                bool seesSky = true;

                for (int y = WORLD_HEIGHT - 1; y >= 0; y--)
                {
                    Block block = chunk.GetBlock(x, y, z);

                    if (seesSky && block.IsTransparent)
                    {
                        sunlight[x, y, z] = 15;
                    }
                    else
                    {
                        seesSky = false;
                        sunlight[x, y, z] = 0;
                    }
                }
            }
    }
    private void PropagateSunlightHorizontally()
    {
        Queue<(int x, int y, int z)> queue = new();

        // PARTI SOLO DAI BORDI CHE VEDONO IL CIELO
        for (int x = 0; x < CHUNK_SIZE; x++)
            for (int z = 0; z < CHUNK_SIZE; z++)
                for (int y = WORLD_HEIGHT - 1; y >= 0; y--)
                {
                    if (sunlight[x, y, z] != 15)
                        break;

                    if (!chunk.GetBlock(x, y, z).IsTransparent)
                        continue;

                    if (HasHorizontalHole(x, y, z))
                        queue.Enqueue((x, y, z));
                }

        // BFS ORIZZONTALE ONLY
        while (queue.Count > 0)
        {
            var (x, y, z) = queue.Dequeue();
            byte light = sunlight[x, y, z];
            if (light <= 1) continue;

            byte next = (byte)(light - 1);

            TrySunHorizontal(x + 1, y, z, next, queue);
            TrySunHorizontal(x - 1, y, z, next, queue);
            TrySunHorizontal(x, y, z + 1, next, queue);
            TrySunHorizontal(x, y, z - 1, next, queue);
        }
    }
    private bool HasHorizontalHole(int x, int y, int z)
    {
        return IsTransparentAndLessLight(x + 1, y, z) ||
               IsTransparentAndLessLight(x - 1, y, z) ||
               IsTransparentAndLessLight(x, y, z + 1) ||
               IsTransparentAndLessLight(x, y, z - 1);
    }
    private void TrySunHorizontal(int x, int y, int z, byte light,
                              Queue<(int x, int y, int z)> queue)
    {
        if (!IsInBounds(x, y, z))
        {
            return;
        }
        if (!IsTransparent(x, y, z)) return;
        if (sunlight[x, y, z] >= light) return;

        sunlight[x, y, z] = light;

        // CADUTA VERTICALE SENZA BFS
        int yy = y - 1;
        while (yy >= 0 && IsTransparent(x, yy, z) && sunlight[x, yy, z] < light)
        {
            sunlight[x, yy, z] = light;
            yy--;
        }

        queue.Enqueue((x, y, z));
    }

    bool IsTransparentAndLessLight(int x, int y, int z)
    {
        if (!IsInBounds(x, y, z)) return false;
        if (!IsTransparent(x, y, z)) return false;
        return sunlight[x, y, z] < 15;
    }
    private void FindLightSources()
    {
        for (int x = 0; x < CHUNK_SIZE; x++)
        {
            for (int y = 0; y < WORLD_HEIGHT; y++)
            {
                for (int z = 0; z < CHUNK_SIZE; z++)
                {
                    Block block = chunk.GetBlock(x, y, z);
                    
                    // Controlla se il blocco emette luce
                    byte lightEmission = block.LightEmission;
                    if (lightEmission > 0)
                    {
                        SetLight(x, y, z, lightEmission, LightType.Torch);
                        
                        // Aggiungi alla coda per propagazione
                        lightPropagationQueue.Enqueue(new LightNode(x, y, z, lightEmission, LightType.Torch));
                    }
                }
            }
        }
    }
    
    // === PROPAGAZIONE LUCE (BFS) ===
    
    private struct LightNode
    {
        public int X, Y, Z;
        public byte LightLevel;
        public LightType Type;
        
        public LightNode(int x, int y, int z, byte light, LightType type)
        {
            X = x; Y = y; Z = z;
            LightLevel = light;
            Type = type;
        }
    }
    
    private Queue<LightNode> lightPropagationQueue = new Queue<LightNode>();
    
    private void PropagateAllLight()
    {
        // Inizia con tutte le sorgenti di luce
        InitializePropagationQueue();
        
        while (lightPropagationQueue.Count > 0)
        {
            LightNode node = lightPropagationQueue.Dequeue();
            
            if (node.LightLevel <= 1) continue;
            
            byte nextLight = (byte)(node.LightLevel - 1);
            PropagateTo(node.X + 1, node.Y, node.Z, nextLight, node.Type);
            PropagateTo(node.X - 1, node.Y, node.Z, nextLight, node.Type);
            PropagateTo(node.X, node.Y + 1, node.Z, nextLight, node.Type);
            PropagateTo(node.X, node.Y - 1, node.Z, nextLight, node.Type);
            PropagateTo(node.X, node.Y, node.Z + 1, nextLight, node.Type);
            PropagateTo(node.X, node.Y, node.Z - 1, nextLight, node.Type);
        }
        //PropagateSunlightThroughHoles();
    }
    
    private void PropagateTo(int x, int y, int z, byte newLight, LightType type)
    {
        if (!IsInBounds(x, y, z))
        {
            
            return;
        }

        // Non propagare attraverso blocchi opachi
        if (IsOpaque(x, y, z)) return;
        
        // Controlla il livello di luce attuale
        byte currentLight = (type == LightType.Sunlight) ? sunlight[x, y, z] : blockLight[x, y, z];
        
        if (newLight > currentLight)
        {
            SetLight(x, y, z, newLight, type);
            lightPropagationQueue.Enqueue(new LightNode(x, y, z, newLight, type));
        }
    }
    
    static int FloorDiv(int a, int b)
    {
        int r = a / b;
        if ((a ^ b) < 0 && a % b != 0) r--;
        return r;
    }

    static int FloorMod(int a, int b)
    {
        return a - FloorDiv(a, b) * b;
    }
    private void InitializePropagationQueue()
    {
        lightPropagationQueue.Clear();
        
        // Aggiungi tutte le celle con luce > 0
        for (int x = 0; x < CHUNK_SIZE; x++)
        {
            for (int y = 0; y < WORLD_HEIGHT; y++)
            {
                for (int z = 0; z < CHUNK_SIZE; z++)
                {
                    if (sunlight[x, y, z] > 0)
                    {
                        lightPropagationQueue.Enqueue(new LightNode(x, y, z, sunlight[x, y, z], LightType.Sunlight));
                    }
                    
                    if (blockLight[x, y, z] > 0)
                    {
                        lightPropagationQueue.Enqueue(new LightNode(x, y, z, blockLight[x, y, z], LightType.Torch));
                    }
                }
            }
        }
    }
    public enum LightType
    {
        Sunlight = 0,  // Luce del sole (propaga dall'alto)
        Torch = 1,     // Luce artificiale
        // Aggiungi altri tipi se necessario
    }
    // === METODI PER IL RENDER ===
    
    public float GetLightForFace(int x, int y, int z, ChunkRenderer.BlockFace face)
    {
        // Determina da dove prendere la luce in base alla faccia
        int sourceX = x, sourceY = y, sourceZ = z;
        
        switch (face)
        {
            case ChunkRenderer.BlockFace.Top: sourceY += 1; break;
            case ChunkRenderer.BlockFace.Bottom: sourceY -= 1; break;
            case ChunkRenderer.BlockFace.North: sourceZ -= 1; break;
            case ChunkRenderer.BlockFace.South: sourceZ += 1; break;
            case ChunkRenderer.BlockFace.West: sourceX -= 1; break;
            case ChunkRenderer.BlockFace.East: sourceX += 1; break;
        }
        
        // Per facce esposte, prendi la luce dall'aria adiacente
        // Per facce interne, prendi la luce minima tra i due blocchi
        byte lightLevel;
        
        if (IsFaceExposedToAir(x, y, z, face))
        {
            lightLevel = GetLightAt(sourceX, sourceY, sourceZ);
        }
        else
        {
            lightLevel = Math.Min(
                GetLightAt(x, y, z),
                GetLightAt(sourceX, sourceY, sourceZ)
            );
        }
        
        // Converti da 0-15 a 0.0-1.0
        return lightLevel / 15f;
    }
    
    private byte GetLightAt(int x, int y, int z)
    {
        if (!IsInBounds(x, y, z))
        {
            return 0;
        }
        
        return GetTotalLight(x, y, z);
    }
    
    private bool IsFaceExposedToAir(int x, int y, int z, ChunkRenderer.BlockFace face)
    {
        int checkX = x, checkY = y, checkZ = z;
        
        switch (face)
        {
            case ChunkRenderer.BlockFace.Top: checkY += 1; break;
            case ChunkRenderer.BlockFace.Bottom: checkY -= 1; break;
            case ChunkRenderer.BlockFace.North: checkZ -= 1; break;
            case ChunkRenderer.BlockFace.South: checkZ += 1; break;
            case ChunkRenderer.BlockFace.West: checkX -= 1; break;
            case ChunkRenderer.BlockFace.East: checkX += 1; break;
        }
        
        if (!IsInBounds(checkX, checkY, checkZ))
        {
            // Considera come esposto all'aria se fuori dal chunk
            return true;
        }
        
        Block adjacentBlock = chunk.GetBlock(checkX, checkY, checkZ);
        return adjacentBlock.Type == BlockType.Air || adjacentBlock.IsTransparent;
    }

    // === METODI PER CHUNK VICINI ===

    private byte GetLightFromNeighborChunk(int x, int y, int z)
    {
        int worldX = x + chunk.X;
        int worldZ = z + chunk.Z;

        Chunk neighbor = Game.world.GetChunkAt(worldX, worldZ);
        if (neighbor == null)
            return 0;

        int localX = Mod(worldX, CHUNK_SIZE);
        int localZ = Mod(worldZ, CHUNK_SIZE);

        return neighbor.lightCalculator.GetTotalLight(localX, y, localZ);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Mod(int value, int size)
    {
        int m = value % size;
        return m < 0 ? m + size : m;
    }

    private Block GetBlockFromWorld(int worldX, int worldY, int worldZ)
    {
         return Game.world.GetBlockAt(worldX, worldY, worldZ);

    }
}