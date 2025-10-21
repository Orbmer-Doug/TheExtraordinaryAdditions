using System;
using System.Reflection;
using Terraria;
using Terraria.ModLoader;

namespace TheExtraordinaryAdditions.Content.Projectiles.Vanilla;

/// <summary>
/// thanks red
/// </summary>
public static class ToolModifierUtils
{
    public static Point GetTileTarget(Player player)
    {
        return Main.SmartCursorWanted ? new Point(Main.SmartCursorX, Main.SmartCursorY) : new Point(Player.tileTargetX, Player.tileTargetY);
    }

    internal static MethodInfo MineMethod => typeof(Player).GetMethod("ItemCheck_UseMiningTools_ActuallyUseMiningTool", BindingFlags.NonPublic | BindingFlags.Instance);
    internal static MethodInfo WallMethod => typeof(Player).GetMethod("ItemCheck_UseMiningTools_TryFindingWallToHammer", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Instance);
    public static void Mine(Player player, Item tool, bool hammer = false, bool right = false, Point? overrideTileTarget = null)
    {
        if (tool.pick <= 0 && tool.axe <= 0 && tool.hammer <= 0)
            return;

        bool flag = player.IsTargetTileInItemRange(tool);
        if (player.noBuilding)
            flag = false;

        if (!flag)
            return;

        Point tileTarget = GetTileTarget(player);

        object[] miningParams = new object[4];
        miningParams[0] = tool;
        miningParams[2] = overrideTileTarget.HasValue ? overrideTileTarget.Value.X : tileTarget.X;
        miningParams[3] = overrideTileTarget.HasValue ? overrideTileTarget.Value.Y : tileTarget.Y;

        MineMethod.Invoke(player, miningParams);

        if (hammer)
        {
            bool canHitWalls = (bool)miningParams[1];
            if (canHitWalls == true)
            {
                // This WOULD have gone to jackhammers (if there were more than one and was a item id set for them)
                if (right)
                {
                    for (int wx = -1; wx <= 1; wx++)
                    {
                        for (int wy = -1; wy <= 1; wy++)
                        {
                            FindWall(wx, wy);
                        }
                    }
                }
                else
                    FindWall(0, 0);

                void FindWall(int wx, int wy)
                {
                    // Try breaking walls
                    object[] wallCoords = new object[2];

                    WallMethod.Invoke(player, wallCoords);

                    int wallX = (int)wallCoords[0] + wx;
                    int wallY = (int)wallCoords[1] + wy;

                    // The usual method you would use for this checks for itemAnimation and toolTime, both of which are absent in this case AND IS WHY WE CANT PUBLICIZE IT
                    bool wall = Player.CanPlayerSmashWall(wallX, wallY);

                    Tile tile = Main.tile[wallX, wallY];
                    if (tile.WallType > 0 && (!tile.Active() ||
                        wallX != tileTarget.X || wallY != tileTarget.Y || (!Main.tileHammer[tile.TileType] && !player.poundRelease))
                        /*&& player.controlUseItem*/ && tool.hammer > 0 && wall)
                        player.PickWall(wallX, wallY, (int)(tool.hammer * 1.5f));
                }
            }

            // Reset the hammer
            player.poundRelease = true;
        }
    }
}