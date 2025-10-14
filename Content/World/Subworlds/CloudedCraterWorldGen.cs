using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Tiles;

namespace TheExtraordinaryAdditions.Content.World.Subworlds;

public static class CloudedCraterWorldGen
{
    internal static ushort MeteorTile = (ushort)ModContent.TileType<MeteorBlockPlaced>();

    public const int DirtDepth = 100; // Minimum terrain thickness at center
    public const int CraterDepth = 200; // Height difference between center and edges
    public const int CenterWidth = 30;
    public const int MaxNoiseHeight = 15;
    public const float SurfaceMapMagnification = 0.021f;

    public static int CenterSurfaceY => Main.maxTilesY - DirtDepth; // Center surface Y
    public static int EdgeSurfaceY => CenterSurfaceY - CraterDepth; // Edge surface Y

    public static void Generate()
    {
        GenerateCraterTerrain();
        SetInitialPlayerSpawnPoint();
        PlaceTransmitter();
        SmoothenWorld();
    }

    public static void GenerateCraterTerrain()
    {
        int center = Main.maxTilesX / 2; // World center X
        float r = Main.maxTilesX / 2f; // Half world width
        float flatRadius = CenterWidth / 2f; // Flat area radius
        float a = (CenterSurfaceY - EdgeSurfaceY) / (r - flatRadius) / (r - flatRadius); // Quadratic coefficient
        int seed = WorldGen.genRand.Next(999999999);

        for (int x = 0; x < Main.maxTilesX; x++)
        {
            float distance = Math.Abs(x - center);
            float baseTopY;
            float noiseInterpolant;

            // Calculate base height with a quadratic curve for hemispherical shape
            if (distance <= flatRadius)
            {
                baseTopY = CenterSurfaceY; // Flat center
                noiseInterpolant = 0f; // No noise in flat area
            }
            else
            {
                baseTopY = CenterSurfaceY - a * (distance - flatRadius) * (distance - flatRadius); // Quadratic curve
                noiseInterpolant = 1f; // Full noise outside flat area
            }

            // Add noise for jaggedness
            float noise = FractalBrownianMotion(x * SurfaceMapMagnification, 0, seed, 3) * MaxNoiseHeight;
            int topY = (int)(baseTopY + noise * noiseInterpolant); // Final surface Y

            // Place meteor tiles from surface to bottom
            for (int y = topY; y < Main.maxTilesY; y++)
            {
                Tile tile = ParanoidTileRetrieval(x, y);
                tile.TileType = MeteorTile;
                tile.Get<TileWallWireStateData>().HasTile = true;
            }
        }
    }

    /// <summary>
    /// Sets the player spawn point in the outer right 2/3 of the world
    /// </summary>
    public static void SetInitialPlayerSpawnPoint()
    {
        int spawnX = Main.maxTilesX * 5 / 6;
        int spawnY = 0;

        // Find the surface at spawnX
        for (int y = 0; y < Main.maxTilesY; y++)
        {
            if (Main.tile[spawnX, y].HasTile)
            {
                spawnY = y - 1; // Spawn just above surface
                break;
            }
        }

        Main.spawnTileX = spawnX;
        Main.spawnTileY = spawnY;
    }

    public static void PlaceTransmitter()
    {
        Point pos = new(Main.spawnTileX, Main.spawnTileY);

        for (int dx = -TechnicTransmitterPlaced.Width / 2 - 1; dx <= TechnicTransmitterPlaced.Width / 2 + 1; dx++)
        {
            Tile tile = ParanoidTileRetrieval(pos.X + dx, pos.Y + 1);
            tile.TileType = MeteorTile;
            tile.Get<TileWallWireStateData>().HasTile = true;

            for (int dy = 0; dy <= TechnicTransmitterPlaced.Height; dy++)
            {
                tile = ParanoidTileRetrieval(pos.X + dx, pos.Y - dy);
                tile.Get<TileWallWireStateData>().HasTile = false;

                tile = ParanoidTileRetrieval(pos.X + dx, pos.Y + dy + 1);
                tile.TileType = MeteorTile;
                tile.Get<TileWallWireStateData>().HasTile = true;
            }
        }

        WorldGen.PlaceTile(pos.X, pos.Y, ModContent.TileType<TechnicTransmitterPlaced>(), true, false, -1);
    }

    public static void SmoothenWorld()
    {
        for (int x = 5; x < Main.maxTilesX - 5; x++)
        {
            for (int y = 5; y < Main.maxTilesY - 5; y++)
            {
                Tile.SmoothSlope(x, y);
            }
        }
    }
}