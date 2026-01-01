namespace StardustCraft.World;

public class LightSystem
{
    private readonly int chunkSize = 16;
    private readonly int worldHeight = 256;
    
    // Array 3D per la luce (r, g, b, sunlight)
    private byte[,,] lightLevels;
    private bool[,,] lightDirty;
    
    public LightSystem()
    {
        lightLevels = new byte[chunkSize, worldHeight, chunkSize];
        lightDirty = new bool[chunkSize, worldHeight, chunkSize];
    }
}