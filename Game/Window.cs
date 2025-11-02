using Raylib_cs;

namespace halloween.Game;

public static class Window
{
    #region SetupInfo

    private readonly static string Title = "Jam-O'-Ween";
    const int WIDTH = 600;
    const int HEIGHT = 600;

    #endregion

    public delegate void OnWindowClose();

    public static void InitWindow(OnWindowClose onWindowClose)
    {
        Raylib.SetTraceLogLevel(TraceLogLevel.Error);
        Raylib.InitWindow(WIDTH, HEIGHT, Title);

        while (!Raylib.WindowShouldClose())
        {
            Raylib.ClearBackground(Color.Beige);
            Raylib.BeginDrawing();
            {
                Raylib.DrawFPS(0, 0);

            }
            Raylib.EndDrawing();
        }

        Raylib.CloseWindow();
        onWindowClose();
    }

}