using OpenTK.Windowing.Desktop;
using OpenTK.Mathematics;

class Program
{
    static void Main()
    {
        var settings = GameWindowSettings.Default;
        var native = new NativeWindowSettings()
        {
            Size = new Vector2i(1280, 720),
            Title = "StardustCraft"
        };

        using var game = new Game(settings, native);
        game.Run();
    }
}
