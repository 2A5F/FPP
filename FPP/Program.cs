using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using FPP;

var world = new World(Vector64.Create(40, 30), Vector64.Create(20, 15));
world.Init();

var render = new ConsoleRender(world);
render.Init();

await Task.Delay(100);

for (;;)
{
    render.Render();

    Console.Write("command: ");
    var command = Console.ReadLine();
    switch (command)
    {
        case "re":
            world.Init();
            break;
        case "set":
            Console.Write("pos: ");
            var pos = Console.ReadLine()!.Split(",").Select(s => s.Trim()).ToArray();
            var x = int.Parse(pos[0]);
            var y = int.Parse(pos[1]);
            world.SetBlock(x - 1, y - 1);
            break;
        case "pass":
            for (;;)
            {
                render.Render();

                await Task.Delay(100);

                if (!world.Tick()) break;
            }
            break;
        case "quit": break;
    }
}

#region Press any key to continue...

if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
{
    Process.Start("cmd", "/C pause");
}
else
{
    Console.WriteLine("Press any key to continue...");
    Console.ReadKey();
}

#endregion
