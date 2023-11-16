using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Security.Cryptography;

namespace FPP;

public struct Tile
{
    public TileKind kind;
    private float cost;
    public float new_cost;
    public int pass;
    public char? dir;

    public float CostInc(bool incline) => kind switch
    {
        TileKind.Ground => incline ? 1.5f : 1,
        TileKind.Target => 0,
        TileKind.Block => 10000,
        TileKind.Wall => float.PositiveInfinity,
        _ => throw new ArgumentOutOfRangeException()
    };

    public float Cost() => kind switch
    {
        TileKind.Target => 0,
        TileKind.Wall => float.PositiveInfinity,
        _ => cost,
    };

    public void SetCost(float v) => cost = v;

    // ReSharper disable once CompareOfFloatsByEqualityOperator
    public bool IsChange() => cost != new_cost;
}

public enum TileKind
{
    Ground,
    Target,
    Block,
    Wall,
}

public class World(Vector64<int> size, Vector64<int> target)
{
    public readonly Tile[,] tiles = new Tile[size[0], size[1]];

    private static RandomNumberGenerator rng = RandomNumberGenerator.Create();

    // private ConcurrentDictionary<Vector64<int>, byte> queue = new();
    // private ConcurrentDictionary<Vector64<int>, byte> back_queue = new();

    private int pass;

    public void Init()
    {
        // queue.TryAdd(target, 0);

        var count = size[0] * size[1];
        ParallelEnumerable.Range(0, count).ForAll(i =>
        {
            var y = i / tiles.GetLength(0);
            var x = i % tiles.GetLength(0);

            ref var tile = ref tiles[x, y];

            tile = default;
            tile.SetCost(float.PositiveInfinity);
            tile.new_cost = float.PositiveInfinity;

            if (Vector64.Create(x, y) == target)
            {
                tile.kind = TileKind.Target;
                return;
            }

            if (y >= 3 && y < size[1] - 3)
            {
                if (x >= 3 && x < size[0] - 3)
                {
                    if (y != 15)
                    {
                        tile.kind = TileKind.Wall;
                        return;
                    }
                }
            }

            Span<byte> bytes = stackalloc byte[sizeof(uint)];
            rng.GetBytes(bytes);
            var n = MemoryMarshal.Cast<byte, uint>(bytes)[0];
            if (n < 100000000)
            {
                tile.kind = TileKind.Block;
                return;
            }
        });
    }

    public void SetBlock(int x, int y)
    {
        ref var tile = ref tiles[x, y];
        tile = default;
        tile.SetCost(float.PositiveInfinity);
        tile.kind = TileKind.Block;
        // queue.TryAdd(Vector64.Create(x, y), 0);
    }

    private ParallelQuery<Vector64<int>> Query() => ParallelEnumerable.Range(0, size[0] * size[1]).Select(i =>
    {
        var y = i / tiles.GetLength(0);
        var x = i % tiles.GetLength(0);
        return Vector64.Create(x, y);
    });

    public bool Tick()
    {
        Query()
            .ForAll(pos =>
            {
                ref var tile = ref tiles[pos[0], pos[1]];
                if (tile.kind is TileKind.Wall or TileKind.Target) return;
                var min = float.PositiveInfinity;
                var incline = false;
                char dir = '⭮';
                for (var y = pos[1] - 1; y <= pos[1] + 1; y++)
                {
                    for (var x = pos[0] - 1; x <= pos[0] + 1; x++)
                    {
                        if (!(x == pos[0] && y == pos[1]) && x >= 0 && y >= 0 && x < size[0] && y < size[1])
                        {
                            ref var t = ref tiles[x, y];
                            // if (t.pass <= pass)
                            // {
                            //     if (t.kind is TileKind.Ground or TileKind.Block)
                            //     {
                            //         back_queue.TryAdd(Vector64.Create(x, y), 0);
                            //     }
                            // }
                            var cost = t.Cost();
                            if (cost < min)
                            {
                                incline = x != pos[0] && y != pos[1];
                                min = cost;
                                if (tile.kind is TileKind.Ground)
                                {
                                    if (x == pos[0] && y == pos[1]) dir = '⭮';
                                    else if (x == pos[0] && y < pos[1]) dir = '↑';
                                    else if (x == pos[0] && y > pos[1]) dir = '↓';
                                    else if (x < pos[0] && y == pos[1]) dir = '←';
                                    else if (x > pos[0] && y == pos[1]) dir = '→';
                                    else if (x < pos[0] && y < pos[1]) dir = '↖';
                                    else if (x < pos[0] && y > pos[1]) dir = '↙';
                                    else if (x > pos[0] && y < pos[1]) dir = '↗';
                                    else if (x > pos[0] && y > pos[1]) dir = '↘';

                                    // dir = cost.ToString()[0];
                                }
                            }
                        }
                    }
                }
                if (float.IsPositiveInfinity(min)) return;
                if (tile.kind is TileKind.Ground or TileKind.Block)
                {
                    tile.new_cost = min + tile.CostInc(incline);
                }
                if (tile.kind is TileKind.Ground)
                {
                    tile.dir = dir;
                }
            });

        var change_count = 0;

        Query()
            .ForAll(pos =>
            {
                ref var tile = ref tiles[pos[0], pos[1]];
                if (tile.IsChange())
                {
                    Interlocked.Increment(ref change_count);
                }
                tile.SetCost(tile.new_cost);
                tile.pass = pass + 1;
            });

        // queue.Clear();
        // (queue, back_queue) = (back_queue, queue);
        return change_count > 0;
    }
}
