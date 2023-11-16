using System.Runtime.InteropServices;
using System.Text;

namespace FPP;

public class ConsoleRender(World world)
{
    public Tile[,] Tiles => world.tiles;

    public void Init()
    {
        var width = (Tiles.GetLength(0) + 1) * 3;
        var height = Tiles.GetLength(1);
        Console.OutputEncoding = Encoding.UTF8;
        Console.SetWindowSize(width, height);
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            [DllImport("user32.dll")]
            static extern IntPtr GetForegroundWindow();

            [DllImport("user32.dll")]
            static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

            const int SW_MAXIMIZE = 3;
            const int SW_MINIMIZE = 6;

            var hwnd = GetForegroundWindow();
            ShowWindow(hwnd, SW_MINIMIZE);
            ShowWindow(hwnd, SW_MAXIMIZE);
        }
    }

    public void Render()
    {
        Clear();
        RenderTiles();
    }

    public void Clear()
    {
        Console.Clear();
        Console.WriteLine("\x1b[3J");
        Console.SetCursorPosition(0, 0);
    }

    private void RenderTiles()
    {
        var sb = new StringBuilder();

        sb.Append("  ");
        for (var i = 0; i < Tiles.GetLength(0); i++)
        {
            sb.Append($" {i + 1:00}");
        }
        sb.Append('\n');

        for (var y = 0; y < Tiles.GetLength(1); y++)
        {
            sb.Append($"{y + 1:00}");
            for (var x = 0; x < Tiles.GetLength(0); x++)
            {
                var c = Tiles[x, y].kind switch
                {
                    TileKind.Block => '▢',
                    TileKind.Wall => '▨',
                    TileKind.Target => '◉',
                    TileKind.Ground => Tiles[x, y].dir ?? ' ',
                    _ => ' '
                };
                sb.Append("  ");
                sb.Append(c);
            }
            sb.Append('\n');
        }

        Console.WriteLine(sb.ToString());
    }
}
