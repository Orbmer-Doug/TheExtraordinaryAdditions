using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;

namespace TheExtraordinaryAdditions.Core.DataStructures;

// Entire credit goes to Calamity Fables (and a little sebastion lague)

public static partial class AStarPathfinding
{
    public static bool OffsetPositionsToValidNavigation(TileNavigableDelegate navigation, Vector2 start, Vector2 end, int startIterations, int endIterations, out Point startPoint, out Point endPoint)
    {
        startPoint = start.ToSafeTileCoordinates();
        endPoint = end.ToSafeTileCoordinates();

        return OffsetPositionsToValidNavigation(navigation, ref startPoint, ref endPoint, startIterations, endIterations);
    }

    public static bool OffsetPositionsToValidNavigation(TileNavigableDelegate navigation, ref Point start, ref Point end, int startIterations, int endIterations)
    {
        int maxIterations = startIterations;
        start = OffsetUntilNavigable(start, new Point(0, 1), navigation, ref maxIterations);
        if (maxIterations < 0)
            return false;

        maxIterations = endIterations;
        end = OffsetUntilNavigable(end, new Point(0, 1), navigation, ref maxIterations);
        if (maxIterations < 0)
            return false;

        return true;
    }

    #region Premade navigation checks
    public static int SolidCreatureNavigHeight = 1;
    public static int SolidCreatureNavigWallClimbCheckDown = -1;

    /// <summary>
    /// Simulates the movement of a hypothetical 1x1 creature as being able to walk around on tiles, climb walls, and fall down (including through platforms)  <br/>
    /// Does not check for any space larger than 1x1, nor does it check for the reachability of the goal from the origin point <br/>
    /// Can climb walls infinitely high
    /// </summary>
    public static bool SolidCreatureNavigationSimple(Point p, Point? from, out bool universallyUnnavigable)
    {
        universallyUnnavigable = true;
        Tile t = Main.tile[p];
        bool solidTile = Main.tileSolid[t.TileType];
        bool platform = TileID.Sets.Platforms[t.TileType];

        //Can't navigate inside solid tiles
        if (t.HasUnactuatedTile && !t.IsHalfBlock && !platform && solidTile)
            return false;

        //Can navigate on half tiles and platforms just fine
        if (t.HasUnactuatedTile && (t.IsHalfBlock || platform) && solidTile)
            return true;

        universallyUnnavigable = false;
        for (int i = -1; i <= 1; i++)
            for (int j = 0; j <= 1; j++)
            {
                //Only cardinal directions here
                if (j * i != 0 || (j == 0 && i == 0))
                    continue;

                //IF a neighboring tile is solid we can go on it
                Tile adjacentTile = Main.tile[p.X + i, p.Y + j];
                if (adjacentTile.HasUnactuatedTile && !adjacentTile.IsHalfBlock && (Main.tileSolid[adjacentTile.TileType] || (Main.tileSolidTop[adjacentTile.TileType] && adjacentTile.TileFrameY == 0)))
                    return true;
            }

        //Can fall straight down just fine
        if (from != null && p.X == from.Value.X && p.Y > from.Value.Y)
            return true;

        return false;
    }

    /// <summary>
    /// Simulates the movement of a hypothetical 1xX creature as being able to walk around on tiles, climb walls, and fall down (including through platforms) <br/>
    /// Checks for clearance of a certain height using the value of <see cref="SolidCreatureNavigHeight"/> for the height <br/>
    /// Can crawl up walls as long as there is floor Y tiles below the point it's at. <see cref="SolidCreatureNavigWallClimbCheckDown"/> is used as Y<br/>
    /// Set that value to 1 to prevent wall crawling altogether, set it to 0 to let it crawl up walls indefinitely <br/>
    /// Does not check for the reachability of the goal from the origin point
    /// </summary>
    public static bool SolidCreatureNavigation(Point p, Point? from, out bool universallyUnnavigable)
    {
        universallyUnnavigable = true;
        Tile t = Main.tile[p];
        bool solidTile = Main.tileSolid[t.TileType];
        bool platform = TileID.Sets.Platforms[t.TileType];

        // Can't navigate inside solid tiles
        if (t.HasUnactuatedTile && !t.IsHalfBlock && !platform && solidTile)
            return false;

        // Can't navigate if you don't have the height clearance
        for (int i = 1; i < SolidCreatureNavigHeight; i++)
        {
            Tile aboveTile = Main.tile[p + new Point(0, -i)];
            if (aboveTile.HasUnactuatedTile && !TileID.Sets.Platforms[aboveTile.TileType] && Main.tileSolid[aboveTile.TileType])
                return false;
        }


        if (BasicNavigationChecks(p, from, ref universallyUnnavigable))
            return true;

        return false;
    }

    /// <summary>
    /// Simulates the movement of a hypothetical 1xX creature through raycasting to see if it can reach the point from where it is<br/>
    /// If no origin is provided (aka, when used to solely determine validty of the position), acts like <see cref="SolidCreatureNavigation(Point, Point?)"/>
    /// Can crawl up walls as long as there is floor Y tiles below the point it's at. <see cref="SolidCreatureNavigWallClimbCheckDown"/> is used as Y<br/>
    /// Set that value to 1 to prevent wall crawling altogether, set it to 0 to let it crawl up walls indefinitely <br/>
    /// </summary>
    public static bool SolidCreatureNavigationRaycast(Point p, Point? from, out bool universallyUnnavigable)
    {
        universallyUnnavigable = true;
        if (!from.HasValue)
            return SolidCreatureNavigation(p, from, out universallyUnnavigable);


        //Can't navigate if you don't have the height clearance
        for (int i = 0; i < SolidCreatureNavigHeight; i++)
        {
            if (!RaytraceTo(from.Value.X, from.Value.Y - i, p.X, p.Y - i, i == 0))
            {
                universallyUnnavigable = false;
                return false;
            }
        }

        if (BasicNavigationChecks(p, from, ref universallyUnnavigable))
            return true;

        return false;
    }

    private static bool CheckFloorAndWalls(Point p, Point? from)
    {
        if (SolidCreatureNavigWallClimbCheckDown <= 0)
        {
            for (int i = -1; i <= 1; i++)
                for (int j = 0; j <= 1; j++)
                {
                    //Only cardinal directions here
                    if (j * i != 0 || (j == 0 && i == 0))
                        continue;

                    //IF a neighboring tile is solid we can go on it
                    Tile adjacentTile = Main.tile[p.X + i, p.Y + j];
                    if (adjacentTile.HasUnactuatedTile && !adjacentTile.IsHalfBlock && (Main.tileSolid[adjacentTile.TileType] || (Main.tileSolidTop[adjacentTile.TileType] && adjacentTile.TileFrameY == 0)))
                        return true;
                }
        }
        else
        {
            //Check for floor directly below, if there is its fine
            Tile adjacentTile = Main.tile[p.X, p.Y + 1];
            if (adjacentTile.HasUnactuatedTile && !adjacentTile.IsHalfBlock && (Main.tileSolid[adjacentTile.TileType] || (Main.tileSolidTop[adjacentTile.TileType] && adjacentTile.TileFrameY == 0)))
                return true;

            //if no wall to crawl on, return false
            bool anyWall = false;
            for (int j = -1; j <= 1; j += 2)
            {
                adjacentTile = Main.tile[p.X + j, p.Y];
                if (adjacentTile.HasUnactuatedTile && !adjacentTile.IsHalfBlock && (Main.tileSolid[adjacentTile.TileType] || (Main.tileSolidTop[adjacentTile.TileType] && adjacentTile.TileFrameY == 0)))
                {
                    anyWall = true;
                    break;
                }
            }

            if (!anyWall)
                return false;

            //Can crawl DOWN walls easy
            if (from != null && from.Value.Y < p.Y)
                return true;

            //if crawling on wall, check down for floor
            for (int i = 1; i < SolidCreatureNavigWallClimbCheckDown; i++)
            {
                adjacentTile = Main.tile[p.X, p.Y + 1 + i];
                if (adjacentTile.HasUnactuatedTile && !adjacentTile.IsHalfBlock && (Main.tileSolid[adjacentTile.TileType] || (Main.tileSolidTop[adjacentTile.TileType] && adjacentTile.TileFrameY == 0)))
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks for a floor / wall to crawl on , or if we can fall straight down, etc.
    /// </summary>
    private static bool BasicNavigationChecks(Point p, Point? from, ref bool universallyUnnavigable)
    {
        Tile t = Main.tile[p];
        bool solidTile = Main.tileSolid[t.TileType];
        bool platform = TileID.Sets.Platforms[t.TileType];

        //Can navigate on half tiles and platforms just fine
        if (t.HasUnactuatedTile && (t.IsHalfBlock || platform) && solidTile)
            return true;

        universallyUnnavigable = false;

        //If there's a floor to stand on / Walls to crawl on, we're fine
        if (CheckFloorAndWalls(p, from))
            return true;

        //Can fall straight down just fine
        if (from != null && p.X == from.Value.X && p.Y > from.Value.Y)
            return true;

        return false;
    }
    #endregion

    public class PathfindingTraceback
    {
    }
}

public static partial class AStarPathfinding
{
    public static bool AirNavigable(Point p, Point? origin, out bool universallyUnnavigable)
    {
        universallyUnnavigable = true;
        Tile t = Main.tile[p];
        return !t.HasUnactuatedTile || !Main.tileSolid[t.TileType] || Main.tileSolidTop[t.TileType];
    }

    public static bool FloorNavigable(Point p, Point? origin, out bool universallyUnnavigable)
    {
        universallyUnnavigable = true;
        Tile t = Main.tile[p];
        return t.HasUnactuatedTile && (Main.tileSolid[t.TileType] || (Main.tileSolidTop[t.TileType] && t.TileFrameY == 0));
    }

    public static bool EdgeRunner(Point p, Point? origin, out bool universallyUnnavigable)
    {
        universallyUnnavigable = true;
        Tile t = Main.tile[p];
        bool solidTile = Main.tileSolid[t.TileType];
        bool platform = TileID.Sets.Platforms[t.TileType];

        //Can't navigate inside solid tiles
        if (t.HasUnactuatedTile && !t.IsHalfBlock && !platform && solidTile)
            return false;

        //Can navigate on half tiles and platforms just fine
        if (t.HasUnactuatedTile && (t.IsHalfBlock || platform) && solidTile)
            return true;

        for (int i = -1; i <= 1; i++)
            for (int j = -1; j <= 1; j++)
            {
                //Only cardinal directions here
                if (j * i != 0 || (j == 0 && i == 0))
                    continue;

                //IF a neighboring tile is solid we can go on it
                Tile adjacentTile = Main.tile[p.X + i, p.Y + j];
                if (adjacentTile.HasUnactuatedTile && !adjacentTile.IsHalfBlock && (Main.tileSolid[adjacentTile.TileType] || (Main.tileSolidTop[adjacentTile.TileType] && adjacentTile.TileFrameY == 0)))
                    return true;
            }

        return false;
    }

    public static Point OffsetUntilNavigable(Point point, Point offset, TileNavigableDelegate navigable)
    {
        int iterations = 50;
        return OffsetUntilNavigable(point, offset, navigable, ref iterations);
    }

    public static Point OffsetUntilNavigable(Point point, Point offset, TileNavigableDelegate navigable, ref int iterations)
    {
        if (navigable(point, null, out _))
            return point;
        while (!navigable(point, null, out _))
        {
            point += offset;
            iterations--;
            if (iterations < 0)
                return Point.Zero;
        }
        return point;
    }

    public static List<Point> Pathfind(Vector2 start, Vector2 end, List<AStarNeighbour> neighborConfiguration, TileNavigableDelegate tileNavigable, int maxIterations = 1000)
    {
        return Pathfind(start.ToTileCoordinates(), end.ToTileCoordinates(), neighborConfiguration, tileNavigable, maxIterations);
    }

    public static List<Point> Pathfind(Point start, Point end, List<AStarNeighbour> neighborConfiguration, TileNavigableDelegate tileNavigable, int maxIterations = 1000)
    {
        _maxNodeIndex = 0;
        MeshNode startNode = GetNode(start, 0, 0);
        MeshNode endNode = GetNode(end, 0, 0);

        if (!tileNavigable(start, null, out _) || !tileNavigable(endNode.origin, null, out _))
            return [start];

        Heap<MeshNode> openNodes = new();
        HashSet<Point> closedNodes = [];
        openNodes.Add(startNode);

        int iterations = 0;
        while (openNodes.Count > 0)
        {
            MeshNode current = openNodes.PopFirst();
            closedNodes.Add(current.origin);

            if (current.origin == endNode.origin)
            {
                endNode = current;
                break;
            }

            foreach (AStarNeighbour neighborNode in neighborConfiguration)
            {
                Point newOrigin = current.origin + neighborNode.offset;
                if (closedNodes.Contains(newOrigin))
                    continue;

                //If we can't navigate to the new tile, add it to the list of closed tiles
                if (!tileNavigable(newOrigin, current.origin, out bool universallyUnnavigable))
                {
                    if (universallyUnnavigable)
                        closedNodes.Add(newOrigin);
                    continue;
                }

                float newPathlength = current.gCost + neighborNode.travelCost;
                float distanceToEndPoint = GetShortestDistance(newOrigin, endNode.origin);
                bool alreadyInOpen = openNodes.TryFind(n => n != null && n.origin == newOrigin, out MeshNode adjNode);

                //Create a new node if it doesnt exist already
                if (!alreadyInOpen)
                {
                    adjNode = GetNode(newOrigin, newPathlength, distanceToEndPoint);
                    adjNode.parent = current;
                    openNodes.Add(adjNode);
                }
                //Update the node if it already exists and we found a shoter path
                else if (adjNode.gCost > newPathlength)
                {
                    adjNode.gCost = newPathlength;
                    adjNode.parent = current;
                    openNodes.UpdateItem(adjNode);
                }
            }

            iterations++;
            if (iterations > maxIterations)
            {
                endNode = openNodes.PopFirst();
                break;
            }
        }

        return RetraceSteps(endNode);
    }

    private static float GetShortestDistance(Point start, Point end)
    {
        int xDistance = Math.Abs(start.X - end.X);
        int yDistance = Math.Abs(start.Y - end.Y);

        int min = Math.Min(xDistance, yDistance);
        int max = Math.Max(xDistance, yDistance);
        return min * AStarNeighbour.SquareRootOfTwo + (max - min);
    }

    private static List<Point> RetraceSteps(MeshNode endPoint)
    {
        List<Point> path = [];
        MeshNode currentNode = endPoint;
        while (currentNode.parent != null)
        {
            path.Add(currentNode.origin);
            currentNode = currentNode.parent;
        }
        path.Add(currentNode.origin);
        return path;
    }

    public static bool IsThereAPath(Vector2 start, Vector2 end, List<AStarNeighbour> neighborConfiguration, TileNavigableDelegate tileNavigable, float distanceTreshold)
    {
        return IsThereAPath(start.ToTileCoordinates(), end.ToTileCoordinates(), neighborConfiguration, tileNavigable, distanceTreshold);
    }

    public delegate bool TileNavigableDelegate(Point tileCandidate, Point? fromTile, out bool universallyUnnavigable);

    public static bool IsThereAPath(Point start, Point end, List<AStarNeighbour> neighborConfiguration, TileNavigableDelegate tileNavigable, float distanceTreshold)
    {
        bool debug = false;

        //Reset the max pooled node
        _maxNodeIndex = 0;

        MeshNode startNode = GetNode(start, 0, 0);
        MeshNode endNode = GetNode(end, 0, 0);
        if (!tileNavigable(start, null, out _) || !tileNavigable(endNode.origin, null, out _))
            return false;

        distanceTreshold += start.ToWorldCoordinates().Distance(end.ToWorldCoordinates());

        Heap<MeshNode> openNodes = new();
        HashSet<Point> closedNodes = [];
        openNodes.Add(startNode);

        if (debug)
            Dust.QuickDust(endNode.origin, Color.Red);

        int iterations = 0;

        while (openNodes.Count > 0)
        {
            MeshNode current = openNodes.PopFirst();
            closedNodes.Add(current.origin);
            if (current.origin == endNode.origin)
                return true;

            if (debug)
                Dust.QuickDust(current.origin, Color.Orange);
            if (current.gCost * 16f > distanceTreshold)
                return false;


            foreach (AStarNeighbour neighborNode in neighborConfiguration)
            {
                Point newOrigin = current.origin + neighborNode.offset;
                if (closedNodes.Contains(newOrigin))
                    continue;

                //If we can't navigate to the new tile, add it to the list of closed tiles
                if (!tileNavigable(newOrigin, current.origin, out bool universallyUnnavigable))
                {
                    if (universallyUnnavigable)
                        closedNodes.Add(newOrigin);
                    continue;
                }

                float newPathlength = current.gCost + neighborNode.travelCost;
                float distanceToEndPoint = GetShortestDistance(newOrigin, endNode.origin);
                bool alreadyInOpen = openNodes.TryFind(n => n != null && n.origin == newOrigin, out MeshNode adjNode);

                //Create a new node if it doesnt exist already
                if (!alreadyInOpen)
                {
                    if (neighborNode.offset == new Point(1, 0))
                        current.gCost *= 1f;
                    adjNode = GetNode(newOrigin, newPathlength, distanceToEndPoint);
                    openNodes.Add(adjNode);
                }
                //Update the node if it already exists and we found a shoter path
                else if (adjNode.gCost > newPathlength)
                {
                    adjNode.gCost = newPathlength;
                    openNodes.UpdateItem(adjNode);
                }
            }

            iterations++;
            if (iterations > 2000)
                return false;
        }

        if (debug)
        {
            Dust d = Dust.QuickDust(end, Color.Blue);
            d.position.Y -= 5;
        }

        //Main.NewText("hCost x 16 : " + (endNode.gCost * 16f).ToString() + " - Treshold : " + distanceTreshold.ToString() + " - Deviation: " + (endNode.gCost * 16f - start.ToWorldCoordinates().Distance(end.ToWorldCoordinates())).ToString() );

        //if (endNode.gCost == 0)
        //    Main.NewText("Couldn't find a path towards goal shorter than: " + distanceTreshold.ToString());
        //else
        //    Main.NewText("Deviation from straight line to goal: " + (endNode.gCost * 16f - start.ToWorldCoordinates().Distance(end.ToWorldCoordinates())).ToString());

        return endNode.gCost != 0;
    }
}

public static partial class AStarPathfinding
{
    private static readonly List<MeshNode> _nodePool = new(200);
    private static int _maxNodeIndex;
    private static MeshNode GetNode(Point origin, float gCost, float hCost, int createMore = 10)
    {
        if (_maxNodeIndex < _nodePool.Count)
        {
            MeshNode poolNode = _nodePool[_maxNodeIndex++];
            poolNode.origin = origin;
            poolNode.gCost = gCost;
            poolNode.hCost = hCost;
            return poolNode;
        }

        //Create a bunch of new nodes for the future
        for (int i = 0; i < createMore; i++)
        {
            _nodePool.Add(new MeshNode());
        }

        MeshNode node = _nodePool[_maxNodeIndex++];
        node.origin = origin;
        node.gCost = gCost;
        node.hCost = hCost;
        return node;
    }

}

/// <summary>
/// Represents a combination of offset and cost data for a given position
/// </summary>
public class AStarNeighbour
{
    public readonly Point offset;
    public readonly float travelCost;

    public AStarNeighbour(Point offset, float travelCost)
    {
        this.offset = offset;
        this.travelCost = travelCost;
    }
    public AStarNeighbour(int x, int y, float travelCost)
    {
        this.offset = new Point(x, y);
        this.travelCost = travelCost;
    }

    internal const float SquareRootOfTwo = 1.41421f;
    public static readonly List<AStarNeighbour> BasicCardinalOrdinal =
    [
        new AStarNeighbour(-1, -1, SquareRootOfTwo),
        new AStarNeighbour(0, -1, 1),
        new AStarNeighbour(1, -1, SquareRootOfTwo),

        new AStarNeighbour(-1, 0, 1),
        new AStarNeighbour(1, 0, 1),

        new AStarNeighbour(-1, 1, SquareRootOfTwo),
        new AStarNeighbour(0, 1, 1),
        new AStarNeighbour(1, 1, SquareRootOfTwo),
    ];

    public static readonly List<AStarNeighbour> DoubleStride =
    [
            new AStarNeighbour(-1, -1, SquareRootOfTwo),
            new AStarNeighbour(0, -1, 1),
            new AStarNeighbour(1, -1, SquareRootOfTwo),

            new AStarNeighbour(-1, 0, 1),
            new AStarNeighbour(1, 0, 1),
            new AStarNeighbour(-2, 0, 2),
            new AStarNeighbour(2, 0, 2),

            new AStarNeighbour(-1, 1, SquareRootOfTwo),
            new AStarNeighbour(0, 1, 1),
            new AStarNeighbour(1, 1, SquareRootOfTwo)
    ];

    public static List<AStarNeighbour> BigStride(int stride)
    {
        List<AStarNeighbour> movementPattern = new(6 + 2 * stride)
        {
            new AStarNeighbour(-1, -1, SquareRootOfTwo),
            new AStarNeighbour(0, -1, 1),
            new AStarNeighbour(1, -1, SquareRootOfTwo)
        };

        for (int i = 1; i <= stride; i++)
        {
            movementPattern.Add(new AStarNeighbour(-i, 0, i));
            movementPattern.Add(new AStarNeighbour(i, 0, i));
        }

        movementPattern.Add(new AStarNeighbour(-1, 1, SquareRootOfTwo));
        movementPattern.Add(new AStarNeighbour(0, 1, 1));
        movementPattern.Add(new AStarNeighbour(1, 1, SquareRootOfTwo));

        return movementPattern;
    }

    public static List<AStarNeighbour> BigOmniStride(int strideX, int strideY)
    {
        List<AStarNeighbour> movementPattern = new(6 + 2 * strideX + 2 * strideY)
        {
            new AStarNeighbour(-1, -1, SquareRootOfTwo),
            new AStarNeighbour(0, -1, 1),
            new AStarNeighbour(1, -1, SquareRootOfTwo)
        };

        for (int i = 1; i <= strideX; i++)
        {
            movementPattern.Add(new AStarNeighbour(-i, 0, i));
            movementPattern.Add(new AStarNeighbour(i, 0, i));
        }

        for (int i = 1; i <= strideY; i++)
        {
            movementPattern.Add(new AStarNeighbour(0, -i, i));
            movementPattern.Add(new AStarNeighbour(0, i, i));
        }

        movementPattern.Add(new AStarNeighbour(-1, 1, SquareRootOfTwo));
        movementPattern.Add(new AStarNeighbour(0, 1, 1));
        movementPattern.Add(new AStarNeighbour(1, 1, SquareRootOfTwo));

        return movementPattern;
    }

    //Not done fully . L
    public static List<AStarNeighbour> UStride(int strideWidth, int strideHeight)
    {
        List<AStarNeighbour> movementPattern = new(3 + 2 * strideHeight * strideWidth);

        for (int i = 1; i <= strideWidth; i++)
        {
            movementPattern.Add(new AStarNeighbour(-i, 0, i));
            movementPattern.Add(new AStarNeighbour(i, 0, i));

            for (int j = 1; j <= strideHeight; j++)
            {
                movementPattern.Add(new AStarNeighbour(-i, -j, i));
                movementPattern.Add(new AStarNeighbour(i, -j, i));
            }
        }

        movementPattern.Add(new AStarNeighbour(-1, 1, SquareRootOfTwo));
        movementPattern.Add(new AStarNeighbour(0, 1, 1));
        movementPattern.Add(new AStarNeighbour(1, 1, SquareRootOfTwo));

        return movementPattern;
    }

}

public class MeshNode : IHeapItem<MeshNode>
{
    /// <summary>
    /// The length of the path we took to reach this node, starting from the original node
    /// May get updated as we find shorter paths to reach this node
    /// </summary>
    public float gCost;

    /// <summary>
    /// The heuristic guess of how much distance is between the current node and the target node
    /// Remains constant, as neither the node itself or the target moves
    /// </summary>
    public float hCost;

    /// <summary>
    /// The combined cost of both the length of the path from the starting node, and the estimated length of the path to the target node
    /// </summary>
    public float FCost => gCost + hCost;

    public Point origin;
    public MeshNode parent;

    public int HeapIndex { get; set; }
    public int CompareTo(MeshNode other)
    {
        int compare = FCost.CompareTo(other.FCost);

        //If the 2 nodes have the same F cost, pick the one with the smaller H cost
        if (compare == 0)
            compare = hCost.CompareTo(other.hCost);
        return -compare;
    }

    public MeshNode(Point origin, float gCost, float hCost)
    {
        this.origin = origin;
        this.gCost = gCost;
        this.hCost = hCost;
    }

    public MeshNode()
    {
    }
}