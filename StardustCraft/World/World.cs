using StardustCraft.Shaders;
using OpenTK.Mathematics;
using StardustCraft.World.Entities;
using StardustCraft.Physics;
using System.Runtime.CompilerServices;
using OpenTK.Windowing.GraphicsLibraryFramework;
using StardustCraft.World.Blocks;


namespace StardustCraft.World
{
    public class World
    {
        private Dictionary<(int, int), Chunk> chunks = new();
        private (int currentX, int currentZ) lastPlayerChunk = (0, 0);
        private int renderDistance = 8; // Chunk visibili in ogni direzione
        public List<Entity> entities = new List<Entity>();
        public float time;
        public bool JumpPressed;
        private bool network;
        public World()
        {
           
        }
        public void Start(bool network=false)
        {
            this.network=network;
            GenerateInitialChunks(0, 0);
            AddEntity(new PlayerEntity(new Vector3(0, 200, 0)));
        }
        public void UpdatePlayerPosition()
        {
            PlayerEntity player = GetClientEntity();
            if (player==null)
            {
                return;
            }
            int playerChunkX = (int)MathF.Floor(player.FinalPosition.X / Chunk.Size);
            int playerChunkZ = (int)MathF.Floor(player.FinalPosition.Z / Chunk.Size);

            // Se il player si è mosso in un nuovo chunk
            if (playerChunkX != lastPlayerChunk.currentX || playerChunkZ != lastPlayerChunk.currentZ)
            {
                lastPlayerChunk = (playerChunkX, playerChunkZ);
                UpdateChunks(playerChunkX, playerChunkZ);
            }
        }
        private readonly object chunksLock = new object();

        [MethodImpl(MethodImplOptions.Synchronized)]
        public bool CheckCollision(ref AABB box, out Vector3 normal, out float penetration, Entity excludeEntity = null)
        {
            normal = Vector3.Zero;
            penetration = 0;
            bool collided = false;

            float minPenetration = float.MaxValue;
            Vector3 bestNormal = Vector3.Zero;
            Chunk[] chunkArray;
            lock (chunksLock)
            {
                chunkArray = chunks.Values.ToList().ToArray(); // copia sicura
            }

            for (int i = 0; i < chunkArray.Length; i++)
            {
                Chunk chunk = chunkArray[i];
                if (chunk == null) continue;

                if (chunk.CheckCollision(ref box, out var chunkNormal, out var chunkPenetration))
                {
                    collided = true;
                    if (chunkPenetration < minPenetration)
                    {
                        minPenetration = chunkPenetration;
                        bestNormal = chunkNormal;
                    }
                }
            }


            // Collisioni con altre entità
            foreach (var entity in entities)
            {
                if (entity == excludeEntity || !entity.HasCollision)
                    continue;

                var entityBox = entity.GetAABB();
                if (box.Intersects(ref entityBox))
                {
                    // Calcola penetrazione con l'entità
                    float penX = Math.Min(
                        box.Max.X - entityBox.Min.X,
                        entityBox.Max.X - box.Min.X
                    );

                    float penY = Math.Min(
                        box.Max.Y - entityBox.Min.Y,
                        entityBox.Max.Y - box.Min.Y
                    );

                    float penZ = Math.Min(
                        box.Max.Z - entityBox.Min.Z,
                        entityBox.Max.Z - box.Min.Z
                    );

                    float entityPenetration;
                    Vector3 entityNormal;

                    if (penX < penY && penX < penZ)
                    {
                        entityPenetration = penX;
                        entityNormal = new Vector3(
                            box.Center.X < entityBox.Center.X ? -1 : 1, 0, 0);
                    }
                    else if (penY < penZ)
                    {
                        entityPenetration = penY;
                        entityNormal = new Vector3(0,
                            box.Center.Y < entityBox.Center.Y ? -1 : 1, 0);
                    }
                    else
                    {
                        entityPenetration = penZ;
                        entityNormal = new Vector3(0, 0,
                            box.Center.Z < entityBox.Center.Z ? -1 : 1);
                    }

                    if (entityPenetration < minPenetration)
                    {
                        minPenetration = entityPenetration;
                        bestNormal = entityNormal;
                        collided = true;
                    }
                }
            }

            if (collided)
            {
                normal = bestNormal;
                penetration = minPenetration;
            }

            return collided;
        }

        public void UpdateEntity(Entity entity, float deltaTime)
        {
            if (!entity.IsActive)
                return;

            // =========================
            // FORZE
            // =========================
            entity.PreviousPosition = entity.Position;
            if (entity.IsAffectedByGravity)
                entity.Velocity.Y -= 9.81f * entity.GravityScale * deltaTime;

            entity.Velocity += entity.Acceleration * deltaTime;

            // Drag
            entity.Velocity *= MathF.Max(0f, 1f - entity.Drag * deltaTime);

            Vector3 movement = entity.Velocity * deltaTime;

            entity.IsOnGround = false;

            // =========================
            // ASSE X
            // =========================
            entity.Position.X += movement.X;
            AABB aabbX = entity.GetAABB();

            if (CheckCollision(ref aabbX, out var normalX, out var penX, entity))
            {
                entity.Position.X += normalX.X * penX * 1.01f;
                entity.Velocity.X = 0;
                entity.Acceleration.X = 0;

                entity.OnCollision(normalX, penX, CollisionType.Horizontal);
            }

            // =========================
            // ASSE Z
            // =========================
            entity.Position.Z += movement.Z;
            AABB aabbZ = entity.GetAABB();

            if (CheckCollision(ref aabbZ, out var normalZ, out var penZ, entity))
            {
                entity.Position.Z += normalZ.Z * penZ * 1.01f;
                if (normalZ.Z > 0.01f)
                {
                    entity.Velocity.Z = 0;
                    entity.Acceleration.Z = 0;
                }
                

                entity.OnCollision(normalZ, penZ, CollisionType.Horizontal);
            }

            // =========================
            // ASSE Y (GRAVITÀ / SALTO)
            // =========================
            entity.Position.Y += movement.Y;
            AABB aabbY = entity.GetAABB();

            if (CheckCollision(ref aabbY, out var normalY, out var penY, entity))
            {
                entity.Position.Y += normalY.Y * penY;

                entity.Velocity.Y = 0;
                entity.Acceleration.Y = 0;

                if (normalY.Y > 0.01f)
                {
                    entity.IsOnGround = true;
                    entity.OnCollision(normalY, penY, CollisionType.Ground);
                }
                else if (normalY.Y < -0.01f)
                {
                    entity.OnCollision(normalY, penY, CollisionType.Ceiling);
                }
            }

            entity.Update(deltaTime);
            entity.FinalPosition = entity.Position;
        }
        public void Update(float deltaTime)
        {
            var player = GetClientEntity();
            if (player != null)
            {
                Vector3 input = Vector3.Zero;
                if (Game.Instance.IsKeyDown(Keys.W)) input.Z += 1;
                if (Game.Instance.IsKeyDown(Keys.S)) input.Z -= 1;
                if (Game.Instance.IsKeyDown(Keys.A)) input.X -= 1;
                if (Game.Instance.IsKeyDown(Keys.D)) input.X += 1;

                player.Move(input, Game.Instance.Camera.yaw, 20*deltaTime);

                if (JumpPressed)
                {
                    JumpPressed = false;
                    player.Jump();
                }

            }
            time += deltaTime;
            foreach (var entity in entities.ToList()) 
            {
                UpdateEntity(entity, deltaTime);
            }
           
        }
        public void AddEntity(Entity entity)
        {
            entities.Add(entity);
           
        }
        private void GenerateInitialChunks(int centerX, int centerZ)
        {
            for (int x = -renderDistance; x <= renderDistance; x++)
                for (int z = -renderDistance; z <= renderDistance; z++)
                    LoadOrCreateChunk(centerX + x, centerZ + z);
        }

        private void UpdateChunks(int centerX, int centerZ)
        {
            HashSet<(int, int)> neededChunks = new();

            for (int x = -renderDistance; x <= renderDistance; x++)
                for (int z = -renderDistance; z <= renderDistance; z++)
                    neededChunks.Add((centerX + x, centerZ + z));

            lock (chunksLock)
            {
                // Trova chunk da rimuovere
                var chunksToRemove = chunks.Keys
                    .Where(key => !neededChunks.Contains(key))
                    .ToList();

                // Rimuovi chunk vecchi
                foreach (var chunkKey in chunksToRemove)
                {
                    if (chunks.TryGetValue(chunkKey, out var chunk))
                    {
                        chunk.Dispose();
                        chunks.Remove(chunkKey);
                    }
                }

                // Aggiungi nuovi chunk
                foreach (var chunkKey in neededChunks)
                {
                    if (!chunks.ContainsKey(chunkKey))
                    {
                        LoadOrCreateChunk(chunkKey.Item1, chunkKey.Item2);
                    }
                }
            }
        }


        private void LoadOrCreateChunk(int x, int z)
        {
            // Prima prova a caricare da disco
            Chunk chunk = TryLoadChunkFromDisk(x, z);

            // Se non esiste, genera nuovo
            if (chunk == null)
            {
                chunk = new Chunk(x, z);
                if (network)
                {
                    _=Game.Instance.NetManager.SendAsync(MsgId.CsAskChunkData, new CsAskChunkData() { X = x, Z = z });
                }
                else
                {
                    chunk.GenerateTerrain();
                }
                

                // Opzionale: salva su disco
                // SaveChunkToDisk(chunk);
            }

            chunks[(x, z)] = chunk;
        }

        private Chunk TryLoadChunkFromDisk(int x, int z)
        {
            // Implementa il caricamento da file se hai un sistema di salvataggio
            // string filename = $"chunk_{x}_{z}.dat";
            // if (File.Exists(filename)) { ... }
            return null; // Per ora genera sempre
        }
        public Vector3 backgroundColor = new (127f/255f, 172f/255f, 255f/255f);

        public double LastPhysicsTickTime { get; internal set; }

        public void Render(Shader shader)
        {
            // Aggiorna posizione player e chunk
            //UpdatePlayerPosition(playerPosition);
            PlayerEntity player = GetClientEntity();
            if (player==null)
            {
                return;
            }
            shader.SetVector3("viewPos", player.FinalPosition);
            shader.SetVector3("uFogColor", backgroundColor);
            shader.SetFloat("uFogFactor", 2f); // 0 - fog off
            shader.SetFloat("uFogCurve", 5f);
            shader.SetFloat("uGamma", 0.8f);
            shader.SetFloat("uTime", (float)time);
            // Ordina chunk per distanza dal player (front-to-back o back-to-front)
            lock (chunksLock)
            {
                var chunksToRender = chunks.Values
                .OrderBy(chunk => Vector3.DistanceSquared(
                    new Vector3(chunk.X * Chunk.Size + Chunk.Size / 2, 0, chunk.Z * Chunk.Size + Chunk.Size / 2),
                    player.FinalPosition))
                .ToList();

                // Renderizza
                foreach (var chunk in chunksToRender)
                {
                    chunk.Render(shader);
                }
            }
           

            // Debug: mostra numero di chunk
            // Console.WriteLine($"Chunk attivi: {chunks.Count}");
        }

        // Metodo per cambiare render distance in runtime
        public void SetRenderDistance(int distance)
        {
            if (distance > 0 && distance != renderDistance)
            {
                renderDistance = distance;
                UpdateChunks(lastPlayerChunk.currentX, lastPlayerChunk.currentZ);
            }
        }
        public Chunk GetChunkAt(int chunkX, int chunkZ)
        {
            if (chunks.TryGetValue((chunkX, chunkZ), out var chunk))
            {
                return chunk;
            }
            return null; // chunk non caricato
        }
        // Ottieni blocco a coordinate mondiali
        public Block GetBlockAt(int worldX, int worldY, int worldZ)
        {
            // Usa il Floor per mappare correttamente le coordinate negative ai chunk
            // Esempio: -1 / 16f = -0.0625 -> Floor = -1
            int chunkX = (int)MathF.Floor((float)worldX / Chunk.Size);
            int chunkZ = (int)MathF.Floor((float)worldZ / Chunk.Size);

            // Il modulo in C# mantiene il segno. Usiamo questa formula universale
            // per ottenere coordinate locali sempre positive (0 a 15)
            int localX = ((worldX % Chunk.Size) + Chunk.Size) % Chunk.Size;
            int localZ = ((worldZ % Chunk.Size) + Chunk.Size) % Chunk.Size;

            if (worldY < 0 || worldY >= Chunk.SizeY) // Opzionale: controllo altezza
                return new Block { Type = BlockType.Air };

            if (chunks.TryGetValue((chunkX, chunkZ), out var chunk))
            {
                return chunk.GetBlock(localX, worldY, localZ);
            }

            return new Block { Type = BlockType.Air };
        }

        // Piazza blocco a coordinate mondiali
        public void SetBlockAt(int worldX, int worldY, int worldZ, BlockType block)
        {
            // Usa il Floor per mappare correttamente le coordinate negative ai chunk
            // Esempio: -1 / 16f = -0.0625 -> Floor = -1
            int chunkX = (int)MathF.Floor((float)worldX / Chunk.Size);
            int chunkZ = (int)MathF.Floor((float)worldZ / Chunk.Size);

            // Il modulo in C# mantiene il segno. Usiamo questa formula universale
            // per ottenere coordinate locali sempre positive (0 a 15)
            int localX = ((worldX % Chunk.Size) + Chunk.Size) % Chunk.Size;
            int localZ = ((worldZ % Chunk.Size) + Chunk.Size) % Chunk.Size;

            if (worldY < 0 || worldY >= Chunk.SizeY) // Opzionale: controllo altezza
                return;

            if (chunks.TryGetValue((chunkX, chunkZ), out var chunk))
            {
                chunk.SetBlock(localX, worldY, localZ, block);
               
                chunk.PreBuildMesh();
                chunk.rebuildChunk = true;
                // Se il blocco è al bordo, potremmo dover aggiornare i chunk vicini
                if (localX == 0) UpdateNeighborChunk(chunkX - 1, chunkZ);
                if (localX == Chunk.Size - 1) UpdateNeighborChunk(chunkX + 1, chunkZ);
                if (localZ == 0) UpdateNeighborChunk(chunkX, chunkZ - 1);
                if (localZ == Chunk.Size - 1) UpdateNeighborChunk(chunkX, chunkZ + 1);
            }
        }

        private void UpdateNeighborChunk(int chunkX, int chunkZ)
        {
            if (chunks.TryGetValue((chunkX, chunkZ), out var neighbor))
            {
                neighbor.RecalculateLight();
                neighbor.PreBuildMesh();
                neighbor.rebuildChunk = true;
            }
        }

        public PlayerEntity GetClientEntity()
        {
            return entities.Find(e => e.uuid == "CLIENT") as PlayerEntity;
        }
        public void Dispose()
        {
            foreach (var chunk in chunks.Values)
            {
                chunk.Dispose();
            }
            chunks.Clear();
        }
        public enum BiomeType
        {
            Ocean,
            Beach,
            Plains,
            Forest,
            Hills,
            Mountains,
            SnowyMountains,
            Swamp,
            River
        }
    }
}