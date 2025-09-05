using System.Collections.Generic;
using System;
using Terraria;
using Terraria.GameContent.RGB;
using Terraria.ID;
using Terraria.WorldBuilding;
using Microsoft.Xna.Framework;
using System.Linq;

namespace TheExtraordinaryAdditions.Content.World;

// abandon hope all ye who enter here
public class MeteorsGeneration
{
    // Take 2:



    // Take 1:
    /*
    public void SpawnMeteorSites()
    {
        // List to track placed meteor centers for spacing
        List<Point> placedCenters = new List<Point>();

        // Define sizes: (outer radius, chamber radius)
        (int radius, int chamberRadius)[] sizes = new[]
        {
        (15, 5),  // Small
        (25, 8),  // Medium
        (35, 12)  // Large
    };

        foreach (var (radius, chamberRadius) in sizes)
        {
            int attempts = 0;
            bool placed = false;

            while (attempts < 100 && !placed)
            {
                attempts++;

                // Step 1: Find a random surface X-coordinate
                int xSurface = WorldGen.genRand.Next(600, Main.maxTilesX - 600);
                int surfaceY = FindSurfaceY(xSurface);
                if (surfaceY == -1) continue;

                // Step 2: Define the center with a random X offset
                int offset = WorldGen.genRand.Next(-radius / 2, radius / 2);
                int centerX = xSurface + offset;
                int centerY = surfaceY + radius;

                // Ensure center is within bounds
                if (centerY >= Main.maxTilesY) continue;

                // Step 3: Check spacing from existing sites
                bool tooClose = placedCenters.Any(c => Math.Abs(c.X - centerX) < 100);
                if (tooClose) continue;

                // Step 4: Check for protected tiles
                bool suitable = IsAreaSuitable(centerX, centerY, radius);
                if (!suitable) continue;

                // Step 5: Track modified area for multiplayer sync
                int minX = centerX - radius, maxX = centerX + radius;
                int minY = centerY - radius, maxY = centerY + radius;

                // Step 6: Place meteorite tiles
                for (int x = centerX - radius; x <= centerX + radius; x++)
                {
                    for (int y = centerY - radius; y <= centerY + radius; y++)
                    {
                        if ((x - centerX) * (x - centerX) + (y - centerY) * (y - centerY) < radius * radius)
                        {
                            Tile tile = Main.tile[x, y];
                            if (tile.active() && TileID.Sets.GetsDestroyedForMeteors[tile.type])
                            {
                                tile.TileType = TileID.Meteorite;
                                tile.active(true);
                            }
                        }
                    }
                }

                // Step 7: Generate the tunnel
                Vector2 entrance = new Vector2(xSurface, surfaceY);
                Vector2 center = new Vector2(centerX, centerY);
                Vector2 direction = center - entrance;
                Vector2 perpendicular = new Vector2(-direction.Y, direction.X).Normalized();

                float frequency = 10f;
                float amplitude = radius / 3f;
                float maxRadius = radius / 5f;
                float minRadius = 1f;
                int seed = WorldGen.genRand.Next();

                for (float t = 0; t <= 1; t += 0.1f)
                {
                    Vector2 basePos = entrance + t * direction;
                    float noise = FractalBrownianMotion(t * frequency, 0, seed, 3);
                    Vector2 off = perpendicular * noise * amplitude * (1 - t);
                    Vector2 noisyPos = basePos + off;
                    float clearRadius = maxRadius * (1 - t) + minRadius * t;

                    int ix = (int)noisyPos.X;
                    int iy = (int)noisyPos.Y;

                    for (int dx = -(int)clearRadius; dx <= (int)clearRadius; dx++)
                    {
                        for (int dy = -(int)clearRadius; dy <= (int)clearRadius; dy++)
                        {
                            if (dx * dx + dy * dy <= clearRadius * clearRadius)
                            {
                                int x = ix + dx;
                                int y = iy + dy;
                                Tile tile = Main.tile[x, y];
                                if (tile.active())
                                {
                                    bool insideMeteor = (x - centerX) * (x - centerX) + (y - centerY) * (y - centerY) < radius * radius;
                                    if (insideMeteor && tile.type == TileID.Meteorite)
                                        tile.active(false);
                                    else if (!insideMeteor && TileID.Sets.GetsDestroyedForMeteors[tile.type])
                                        tile.active(false);
                                }
                            }
                        }
                    }
                    // Update bounds
                    minX = Math.Min(minX, ix - (int)clearRadius);
                    maxX = Math.Max(maxX, ix + (int)clearRadius);
                    minY = Math.Min(minY, iy - (int)clearRadius);
                    maxY = Math.Max(maxY, iy + (int)clearRadius);
                }

                // Step 8: Create inner chamber
                WorldUtils.Gen(new Point(centerX, centerY), new Shapes.Circle(chamberRadius),
                    Actions.Chain(new GenAction[] { new Actions.ClearTile() }));

                // Step 9: Clear paints and coatings
                for (int x = minX; x <= maxX; x++)
                {
                    for (int y = minY; y <= maxY; y++)
                    {
                        Main.tile[x, y].ClearBlockPaintAndCoating();
                    }
                }

                // Step 10: Multiplayer sync
                if (Main.netMode == NetmodeID.Server)
                {
                    NetMessage.SendTileSquare(-1, minX, minY, maxX - minX + 1, maxY - minY + 1);
                }

                // Step 11: Record placement
                placedCenters.Add(new Point(centerX, centerY));
                placed = true;
            }

            if (!placed)
            {
                Main.NewText("Failed to place a meteor site after 100 attempts.", Color.Red);
            }
        }
    }

    // Helper: Find surface Y-coordinate
    private int FindSurfaceY(int x)
    {
        for (int y = 0; y < Main.maxTilesY; y++)
        {
            if (Main.tile[x, y].active() && Main.tileSolid[Main.tile[x, y].type])
                return y;
        }
        return -1;
    }

    // Helper: Check if area is free of protected tiles
    private bool IsAreaSuitable(int centerX, int centerY, int radius)
    {
        for (int x = centerX - radius; x <= centerX + radius; x++)
        {
            for (int y = centerY - radius; y <= centerY + radius; y++)
            {
                if ((x - centerX) * (x - centerX) + (y - centerY) * (y - centerY) < radius * radius)
                {
                    Tile tile = Main.tile[x, y];
                    if (tile.active() && (TileID.Sets.BasicChest[tile.type] ||
                                          Main.tileDungeon[tile.type] ||
                                          TileID.Sets.AvoidedByMeteorLanding[tile.type]))
                    {
                        return false;
                    }
                }
            }
        }
        return true;
    }
    */
}
