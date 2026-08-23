using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Globals.PlayerGlobal;
using TheExtraordinaryAdditions.Core.Graphics.Meshes;
using TheExtraordinaryAdditions.Core.Graphics.Resources;
using TheExtraordinaryAdditions.Core.Graphics.Systems;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Magic.Late.Zenith;

public class SeamstressDraw : ModProjectile
{
    public enum ShapeType
    {
        Line,
        Triangle,
        ClosedLoop,
        Unknown
    }

    public override string Texture => AssetRegistry.GennedTextures.Invisible.Path;

    public override void SetDefaults()
    {
        Projectile.friendly = true;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.penetrate = -1;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
    }

    public Player Owner => Main.player[Projectile.owner];
    public PlayerMouse Modded => Owner.AdditionsMouse();
    public Item Item => Owner.HeldItem;

    public int Time
    {
        get => (int) Projectile.ai[0];
        set => Projectile.ai[0] = value;
    }

    public ShapeType CurrentType;

    public override void AI()
    {
        Vector2 uppos = Modded.MouseScreen;
        if (Time == 0)
        {
            points.Update(uppos);
        }

        if (trail == null || trail.Disposed)
            trail = new(WidthFunct, ColorFunct);

        if (uppos.Distance(points.Points[1]) > 20f)
            points.Update(uppos);
        CurrentType = Classify(points.Points);

        if (!Modded.SafeMouseLeft.Current)
        {
            if (EstimateArea(points.Points, CurrentType) > 10000f || CurrentType == ShapeType.Line)
            {
                if (Item.CheckManaBetter(Owner, 14, true) && CurrentType == ShapeType.Triangle)
                {
                    AssetRegistry.GennedSounds.Laser4.Play(Projectile.Center, .7f, -.4f, .1f, 10);
                    Vector2 pos = EstimateCenter(points.Points, ShapeType.Triangle) + Main.screenPosition;
                    List<Vector2> corners = DouglasPeucker(points.Points, SimplifyEpsilon);
                    corners.RemoveAt(corners.Count - 1);
                    foreach (Vector2 screenPoint in corners)
                    {
                        Vector2 point = screenPoint + Main.screenPosition;
                        if (this.RunLocal())
                        {
                            Vector2 vel = pos.SafeDirectionTo(point) * 5f;
                            Projectile.CreateProj(point, vel, ModContent.ProjectileType<ConcentratedEnergy>(),
                                Projectile.damage, 0f, Owner.whoAmI);
                        }

                        for (int i = 0; i < 5; i++)
                        {
                            ParticleRegistry.SpawnGlowParticle(pos, Vector2.Zero, 50, 80f, Color.Crimson);
                        }

                        const int amt = 30;
                        for (int i = 0; i < amt; i++)
                        {
                            Vector2 sparkPos = pos.Lerp(point, InverseLerp(0, amt, i));
                            Vector2 sparkVel = pos.SafeDirectionTo(point) * 3f;
                            Color col = Color.Crimson.Lerp(Color.Red, Main.rand.NextFloat(0f, .2f));
                            ParticleRegistry.SpawnSparkParticle(sparkPos, sparkVel, 40, .5f, col);
                        }
                    }
                }
                else
                {
                    if (CurrentType == ShapeType.ClosedLoop)
                    {
                        if (!Item.CheckManaBetter(Owner, 16, true))
                            return;
                        AssetRegistry.GennedSounds.etherealSwordAttackBasic1.Play(Projectile.Center, .6f, -.1f, .2f,
                            10);
                    }
                    else
                    {
                        if (!Item.CheckManaBetter(Owner, 6, true))
                            return;
                        AssetRegistry.GennedSounds.etherealSwordAttackBasic2.Play(Projectile.Center, .5f, .1f, .3f, 10);
                    }

                    CollisionShape shape = CollisionShape.Build(points.Points, CurrentType);
                    foreach (NPC npc in Main.ActiveNPCs)
                    {
                        if (!npc.IsAnEnemy() || !npc.Hitbox.Intersects(new Rectangle((int) Main.screenPosition.X,
                                (int) Main.screenPosition.Y, Main.screenWidth, Main.screenHeight)))
                            continue;

                        Rectangle rect = npc.Hitbox;
                        CollisionShape npcShape = CollisionShape.Build(
                        [
                            rect.TopLeft() - Main.screenPosition, rect.TopRight() - Main.screenPosition,
                            rect.BottomRight() - Main.screenPosition, rect.BottomLeft() - Main.screenPosition
                        ], ShapeType.ClosedLoop);
                        switch (CurrentType)
                        {
                            case ShapeType.Line:
                                if (this.RunLocal() && shape.Intersects(npcShape))
                                {
                                    Vector2 pos = npc.RandAreaInEntity();
                                    Vector2 spawnOffset = RandomRotation().ToRotationVector2() *
                                                          Main.rand.NextFloatDirection();
                                    Vector2 sliceVelocity = spawnOffset.SafeNormalize(Vector2.UnitY) * 0.1f;

                                    Projectile.CreateProj(pos, sliceVelocity,
                                        ModContent.ProjectileType<Seams>(), Projectile.damage, 0f,
                                        Projectile.owner);
                                }

                                break;
                            case ShapeType.Triangle:
                                break;
                            case ShapeType.ClosedLoop:
                                if (this.RunLocal() && shape.Intersects(npcShape))
                                {
                                    for (int i = 0; i < 3; i++)
                                    {
                                        Vector2 pos = npc.Center + PolarVector(npc.Size.Length() * 1.4f + 200f,
                                            RandomRotation());
                                        Vector2 vel = pos.SafeDirectionTo(npc.Center) * Main.rand.NextFloat(14f, 22f);
                                        Projectile.CreateProj(pos, vel, ModContent.ProjectileType<NeedleStar>(),
                                            Projectile.damage, Projectile.knockBack, Owner.whoAmI);
                                    }
                                }

                                break;
                            case ShapeType.Unknown:
                            default:
                                break;
                        }
                    }
                }
            }

            List<Vector2> deduplicate = [];
            for (int i = 0; i < points.Count; i++)
            {
                if (points.Points[i].Distance(points.Points[^1]) > 20f)
                    deduplicate.Add(points.Points[i]);
            }

            for (int j = 0; j < deduplicate.Count; j++)
            {
                Vector2 current = deduplicate[j] + Main.screenPosition;
                switch (CurrentType)
                {
                    case ShapeType.Line:
                        Vector2 next = deduplicate[(j + 1) % deduplicate.Count] + Main.screenPosition;
                        for (int i = -1; i <= 1; i += 2)
                        {
                            Vector2 vel = current.SafeDirectionTo(next);
                            Vector2 perp = new Vector2(-vel.Y, vel.X) * 4f * i;
                            ParticleRegistry.SpawnSquishyPixelParticle(current, perp, 60, 2.6f, Color.BlueViolet,
                                Color.DarkViolet, 2);
                        }

                        ParticleRegistry.SpawnSparkleParticle(current, Vector2.Zero, 20, 1.2f, Color.Violet,
                            Color.BlueViolet);
                        break;
                    case ShapeType.Triangle:
                        ParticleRegistry.SpawnDustParticle(current,
                            Main.rand.NextVector2CircularLimited(5f, 5f, .2f, 1f), Main.rand.Next(30, 50),
                            Main.rand.NextFloat(.6f, .9f), Color.Crimson.Lerp(Color.Red, Main.rand.NextFloat(0f, .5f)),
                            .1f, false, true, true, false);
                        break;
                    case ShapeType.ClosedLoop:
                        ParticleRegistry.SpawnSquishyLightParticle(current,
                            Main.rand.NextVector2Circular(2f, 2f) + Main.rand.NextVector2Circular(4f, 4f),
                            Main.rand.Next(20, 40), Main.rand.NextFloat(.4f, .8f),
                            Color.Goldenrod.Lerp(Color.Gold, Main.rand.NextFloat(0f, .5f)));
                        break;
                }
            }

            Projectile.Kill();
        }
        else
        {
            Projectile.timeLeft = 2;
            Owner.SetDummyItemTime(2);

            Vector2 center = Owner.RotatedRelativePoint(Owner.MountedCenter);
            Projectile.Center = center;
            Owner.ChangeDir((Modded.MouseWorld.X > center.X).ToDirectionInt());
            Owner.SetFrontHandBetter(0, center.AngleTo(Modded.MouseWorld));

            if (this.RunLocal())
            {
                Projectile.velocity = center.SafeDirectionTo(Modded.MouseWorld);
                if (Projectile.velocity != Projectile.oldVelocity)
                    this.Sync();
            }

            Vector2 pos = Owner.GetFrontHandPositionImproved();
            if (Time % 2 == 1)
            {
                Vector2 vel = Projectile.velocity.SafeNormalize(Vector2.Zero).RotatedByRandom(.24f) *
                              Main.rand.NextFloat(2f, 5f);
                float size = Main.rand.NextFloat(.3f, .65f);
                int life = Main.rand.Next(12, 25);
                Color col = Color.Lerp(Color.Magenta, Color.DarkViolet, Main.rand.NextFloat(0f, .3f));

                ParticleRegistry.SpawnSquishyLightParticle(pos, vel, life, size, col, 1f, 1.4f);
                ParticleRegistry.SpawnSparkParticle(pos, vel * 2, life, size, Color.Violet);
            }
        }

        Time++;
    }

    public Color ColorFunct(SystemVector2 uv, Vector2 pos)
    {
        switch (CurrentType)
        {
            case ShapeType.Line:
                return Color.BlueViolet;
            case ShapeType.Triangle:
                return Color.Crimson;
            case ShapeType.ClosedLoop:
                return Color.Goldenrod;
            case ShapeType.Unknown:
            default:
                return Color.DarkGray;
        }
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        return false;
    }

    public float WidthFunct(float completion) => 20f;

    private Trail trail;
    private TrailPoints points = new(300);

    public override bool PreDraw(ref Color lightColor)
    {
        // so other players don't just see a random line going around on their screen
        if (this.RunLocal())
        {
            PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.OverProjectiles);
            Texture2D tex = AssetRegistry.GennedTextures.LensStar;
            Vector2 orig = tex.Size() / 2;

            Vector2 offset = Main.ScreenDelta;
            for (int i = 0; i < 4; i++)
                SpriteBatch.DrawRectPixelated(PixelationLayer.OverProjectiles, BlendState.Additive, tex,
                    ToTarget(points.Points[0] + Main.screenPosition - offset, new(50f)), null,
                    Color.Violet.Lerp(Color.White, InverseLerp(0f, 4f, i)), 0f, orig);

            for (int i = 0; i < 4; i++)
                SpriteBatch.DrawRectPixelated(PixelationLayer.OverProjectiles, BlendState.Additive, tex,
                    ToTarget(points.Points[^1] + Main.screenPosition - offset, new(50f)), null,
                    Color.Violet.Lerp(Color.White, InverseLerp(0f, 4f, i)), 0f, orig);
        }

        return false;

        void draw()
        {
            if (trail == null || trail.Disposed || points == null)
                return;

            ManagedShader shader = AssetRegistry.GennedShaders.RealityTearShader;
            shader.SetTexture(AssetRegistry.GennedTextures.Cosmos2, 0, SamplerState.LinearWrap);
            shader.SetTexture(AssetRegistry.GennedTextures.Cosmos, 1, SamplerState.LinearWrap);
            
            trail.DrawTrail(shader, points.Points, Matrix.CreateOrthographicOffCenter(0, Main.screenWidth, Main.screenHeight, 0, -1000f, 1000f), 300, true);
        }
    }

    #region Classifier

    public const float ClosureThreshold = 100f; // how close to start = "closed"
    public const float LineDeviationMax = 40f; // max perpendicular drift for a "line"
    public const float SimplifyEpsilon = 60f; // Douglas-Peucker tolerance for triangle detection
    public const float CollisionEpsilon = 8f; // collision geometry

    public static ShapeType Classify(ReadOnlySpan<Vector2> points)
    {
        if (points.Length < 2)
            return ShapeType.Unknown;

        // Closure is now pixel-scale
        bool isClosed = Vector2.Distance(points[0], points[^1]) < ClosureThreshold;

        ReadOnlySpan<Vector2> unique = isClosed ? points[..^1] : points;

        if (IsApproximateLine(unique, LineDeviationMax))
            return ShapeType.Line;

        if (!isClosed)
            return ShapeType.Unknown;

        if (IsApproximateTriangle(unique, SimplifyEpsilon))
            return ShapeType.Triangle;

        return ShapeType.ClosedLoop;
    }

    /// <summary>
    /// Checks if all points fall within <paramref name="maxDeviation"/> pixels of the line defined by the first and last point
    /// </summary>
    private static bool IsApproximateLine(ReadOnlySpan<Vector2> pts, float maxDeviation)
    {
        if (pts.Length <= 2)
            return true;

        Vector2 start = pts[0];
        Vector2 end = pts[^1];
        Vector2 dir = end - start;
        float len = dir.Length();

        if (len < 1f)
            return false; // degenerate point cloud

        foreach (Vector2 pt in pts[1..^1])
        {
            // Dividing by len normalizes it to actual pixel distance
            float dist = MathF.Abs((pt.X - start.X) * dir.Y - (pt.Y - start.Y) * dir.X) / len;
            if (dist > maxDeviation)
                return false;
        }

        return true;
    }

    /// <summary>
    /// If it reduces to exactly 3 points, the player drew a triangle
    /// </summary>
    private static bool IsApproximateTriangle(ReadOnlySpan<Vector2> pts, float simplifyEpsilon)
    {
        List<Vector2> simplified = DouglasPeucker(pts, simplifyEpsilon);

        if (simplified.Count == 3)
            return true;

        // D-P on a closed triangle path gives [A, B, C, near-A]
        // where the first and last are the same physical corner
        if (simplified.Count == 4 &&
            Vector2.Distance(simplified[0], simplified[^1]) < ClosureThreshold)
            return true;

        return false;
    }

    private static List<Vector2> DouglasPeucker(ReadOnlySpan<Vector2> pts, float epsilon)
    {
        if (pts.Length <= 2)
            return [.. pts.ToArray()];

        // Find the point furthest from the start to the end of the line
        float maxDist = 0f;
        int maxIdx = 0;
        Vector2 start = pts[0], end = pts[^1];
        Vector2 dir = end - start;
        float len = dir.Length();

        for (int i = 1; i < pts.Length - 1; i++)
        {
            float dist = len > 1f
                ? MathF.Abs((pts[i].X - start.X) * dir.Y - (pts[i].Y - start.Y) * dir.X) / len
                : Vector2.Distance(pts[i], start);

            if (!(dist > maxDist))
                continue;

            maxDist = dist;
            maxIdx = i;
        }

        if (!(maxDist > epsilon))
            return [start, end];

        // Recursively simplify both halves
        List<Vector2> left = DouglasPeucker(pts[..(maxIdx + 1)], epsilon);
        List<Vector2> right = DouglasPeucker(pts[maxIdx..], epsilon);

        left.RemoveAt(left.Count - 1);
        left.AddRange(right);
        return left;
    }

    #endregion

    #region Collision

    public readonly ref struct CollisionShape
    {
        public readonly ShapeType Type;

        // Triangle & ClosedLoop - one or more convex triangles for SAT
        public readonly List<Vector2[]> Triangles;

        // Line - raw sequential segments
        public readonly Vector2[] Segments;

        private readonly Vector2[] _polygon;

        public static CollisionShape Build(ReadOnlySpan<Vector2> points, ShapeType type)
        {
            switch (type)
            {
                case ShapeType.ClosedLoop:
                {
                    List<Vector2> simplified = DouglasPeucker(points[..^1], CollisionEpsilon);
                    List<Vector2[]> tris = PolygonTriangulator.Triangulate(simplified);
                    return new CollisionShape(type, tris, null, simplified.ToArray());
                }

                case ShapeType.Triangle:
                {
                    List<Vector2> corners = DouglasPeucker(points, SimplifyEpsilon);
                    Vector2[] tri = [corners[0], corners[1], corners[2]];
                    return new CollisionShape(type, [tri], null, tri);
                }

                case ShapeType.Line:
                    return new CollisionShape(type, null, points.ToArray(), null);

                case ShapeType.Unknown:
                default:
                    return new CollisionShape(type, null, null, null);
            }
        }

        private CollisionShape(ShapeType type, List<Vector2[]> triangles, Vector2[] segments, Vector2[] polygon)
        {
            Type = type;
            Triangles = triangles;
            Segments = segments;
            _polygon = polygon;
        }

        private static bool IsPointInsidePolygon(Vector2 point, Vector2[] polygon)
        {
            if (polygon == null || polygon.Length < 3)
                return false;

            bool inside = false;
            int n = polygon.Length;

            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                Vector2 a = polygon[i];
                Vector2 b = polygon[j];

                // Does this edge cross the horizontal ray from point going right?
                if (a.Y > point.Y != b.Y > point.Y &&
                    point.X < (b.X - a.X) * (point.Y - a.Y) / (b.Y - a.Y) + a.X)
                {
                    inside = !inside;
                }
            }

            return inside;
        }

        /// <summary>
        /// Handles all cross type combinations
        /// </summary>
        public bool Intersects(CollisionShape other)
        {
            // Both polygon-like shapes - SAT on all triangle pairs
            if (Triangles != null && other.Triangles != null)
            {
                // catches shapes that overlap the loop's edge
                foreach (Vector2[] a in Triangles)
                foreach (Vector2[] b in other.Triangles)
                    if (IsIntersecting(a, b))
                        return true;

                // catches shapes fully inside a closed loop
                // If ANY vertex of one shape is inside the other, it's a hit
                if (Type == ShapeType.ClosedLoop)
                    foreach (Vector2[] tri in other.Triangles)
                    foreach (Vector2 pt in tri)
                        if (IsPointInsidePolygon(pt, _polygon))
                            return true;

                if (other.Type == ShapeType.ClosedLoop)
                    foreach (Vector2[] tri in Triangles)
                    foreach (Vector2 pt in tri)
                        if (IsPointInsidePolygon(pt, other._polygon))
                            return true;

                return false;
            }

            // Line vs polygon
            if (Type == ShapeType.Line && other.Triangles != null)
                return LineIntersectsPolygonShape(Segments, other);

            if (other.Type == ShapeType.Line && Triangles != null)
                return LineIntersectsPolygonShape(other.Segments, this);

            // Line vs line
            if (Type == ShapeType.Line && other.Type == ShapeType.Line)
                return LineIntersectsLine(Segments, other.Segments);

            return false;
        }

        private static bool LineIntersectsPolygonShape(Vector2[] lineSegs, CollisionShape poly)
        {
            // Test each line segment against every triangle in the polygon shape
            // A hit occurs if the segment crosses any triangle edge OR either endpoint is inside any triangle
            for (int i = 0; i < lineSegs.Length - 1; i++)
            {
                Vector2 a = lineSegs[i], b = lineSegs[i + 1];
                foreach (Vector2[] tri in poly.Triangles)
                {
                    if (SegmentIntersectsTriangle(a, b, tri))
                        return true;
                }
            }

            return false;
        }

        private static bool LineIntersectsLine(Vector2[] segsA, Vector2[] segsB)
        {
            for (int i = 0; i < segsA.Length - 1; i++)
            for (int j = 0; j < segsB.Length - 1; j++)
                if (SegmentsIntersect(segsA[i], segsA[i + 1], segsB[j], segsB[j + 1]))
                    return true;
            return false;
        }

        private static bool SegmentIntersectsTriangle(Vector2 a, Vector2 b, Vector2[] tri)
        {
            // Check if segment crosses any triangle edge
            for (int i = 0; i < 3; i++)
                if (SegmentsIntersect(a, b, tri[i], tri[(i + 1) % 3]))
                    return true;

            // Check if either endpoint is inside the triangle
            return IsPointInTriangle(a, tri[0], tri[1], tri[2]) ||
                   IsPointInTriangle(b, tri[0], tri[1], tri[2]);
        }

        /// <summary>Cross-product sign test for segment intersection.</summary>
        private static bool SegmentsIntersect(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
        {
            float d1 = Wedge(d - c, a - c);
            float d2 = Wedge(d - c, b - c);
            float d3 = Wedge(b - a, c - a);
            float d4 = Wedge(b - a, d - a);

            if (((d1 > 0 && d2 < 0) || (d1 < 0 && d2 > 0)) &&
                ((d3 > 0 && d4 < 0) || (d3 < 0 && d4 > 0)))
                return true;

            // Collinear / endpoint cases
            if (MathF.Abs(d1) < 1e-5f && OnSegment(c, d, a))
                return true;
            if (MathF.Abs(d2) < 1e-5f && OnSegment(c, d, b))
                return true;
            if (MathF.Abs(d3) < 1e-5f && OnSegment(a, b, c))
                return true;
            if (MathF.Abs(d4) < 1e-5f && OnSegment(a, b, d))
                return true;

            return false;
        }

        private static bool IsPointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float d1 = Wedge(p - a, b - a);
            float d2 = Wedge(p - b, c - b);
            float d3 = Wedge(p - c, a - c);
            return !((d1 > 0 || d2 > 0 || d3 > 0) && (d1 < 0 || d2 < 0 || d3 < 0));
        }

        private static bool OnSegment(Vector2 p, Vector2 q, Vector2 r) =>
            r.X <= MathF.Max(p.X, q.X) && r.X >= MathF.Min(p.X, q.X) &&
            r.Y <= MathF.Max(p.Y, q.Y) && r.Y >= MathF.Min(p.Y, q.Y);
    }

    public static class PolygonTriangulator
    {
        /// <summary>
        /// Decomposes a simple (non-self-intersecting) polygon into triangles using the ear-clipping algorithm
        /// </summary>
        public static List<Vector2[]> Triangulate(IList<Vector2> polygon)
        {
            List<Vector2[]> result = [];
            List<int> indices = new List<int>(Enumerable.Range(0, polygon.Count));

            // Ensure the polygon is wound counter-clockwise
            // Ear detection relies on the cross product sign being consistent
            if (ComputeSignedArea(polygon) < 0)
                indices.Reverse();

            while (indices.Count > 3)
            {
                bool earFound = false;

                for (int i = 0; i < indices.Count; i++)
                {
                    int prevIdx = indices[(i - 1 + indices.Count) % indices.Count];
                    int currIdx = indices[i];
                    int nextIdx = indices[(i + 1) % indices.Count];

                    Vector2 prev = polygon[prevIdx];
                    Vector2 curr = polygon[currIdx];
                    Vector2 next = polygon[nextIdx];

                    // An ear must form a convex (left-turning) vertex in a CCW polygon
                    if (!IsConvexVertex(prev, curr, next))
                        continue;

                    // The triangle is only an ear if no other vertex lies inside it
                    if (ContainsAnyOtherVertex(prev, curr, next, indices, polygon, prevIdx, currIdx, nextIdx))
                        continue;

                    // Clip this ear
                    result.Add([prev, curr, next]);
                    indices.RemoveAt(i);
                    earFound = true;
                    break;
                }

                // Guard against degenerate polygons (e.g. all collinear points)
                if (!earFound)
                    break;
            }

            // The remaining 3 indices form the final triangle
            if (indices.Count == 3)
            {
                result.Add([
                    polygon[indices[0]],
                    polygon[indices[1]],
                    polygon[indices[2]]
                ]);
            }

            return result;
        }

        // Signed area via the shoelace formula
        // Positive = CCW, Negative = CW
        private static float ComputeSignedArea(IList<Vector2> pts)
        {
            float area = 0f;
            int n = pts.Count;
            for (int i = 0; i < n; i++)
            {
                Vector2 a = pts[i], b = pts[(i + 1) % n];
                area += (b.X - a.X) * (b.Y + a.Y);
            }

            return area;
        }

        // Cross product of (curr-prev) x (next-curr)
        private static bool IsConvexVertex(Vector2 prev, Vector2 curr, Vector2 next)
        {
            float cross = (curr.X - prev.X) * (next.Y - curr.Y)
                          - (curr.Y - prev.Y) * (next.X - curr.X);
            return cross > 0f;
        }

        // Returns true if any polygon vertex (other than the ear's own three) falls strictly inside the candidate ear triangle
        private static bool ContainsAnyOtherVertex(
            Vector2 a, Vector2 b, Vector2 c,
            List<int> indices, IList<Vector2> polygon,
            int idxA, int idxB, int idxC)
        {
            foreach (int idx in indices)
            {
                if (idx == idxA || idx == idxB || idx == idxC)
                    continue;
                if (IsPointInTriangle(polygon[idx], a, b, c))
                    return true;
            }

            return false;
        }

        // Barycentric sign test - point is inside if all three cross products have the same sign (all positive for a CCW triangle)
        private static bool IsPointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float d1 = Wedge(p - a, b - a);
            float d2 = Wedge(p - b, c - b);
            float d3 = Wedge(p - c, a - c);

            bool hasNeg = d1 < 0 || d2 < 0 || d3 < 0;
            bool hasPos = d1 > 0 || d2 > 0 || d3 > 0;
            return !(hasNeg && hasPos);
        }
    }

    #endregion

    #region Estimations

    public static Vector2 EstimateCenter(ReadOnlySpan<Vector2> points, ShapeType type)
    {
        switch (type)
        {
            case ShapeType.Line:
                return (points[0] + points[^1]) / 2f;

            case ShapeType.Triangle:
                // Use the 3 simplified corners, not all the raw points
                List<Vector2> corners = DouglasPeucker(points, SimplifyEpsilon);
                return (corners[0] + corners[1] + corners[2]) / 3f;

            case ShapeType.ClosedLoop:
                return PolygonCentroid(points);

            default:
                return points[points.Length / 2];
        }
    }

    public static float EstimateArea(ReadOnlySpan<Vector2> points, ShapeType type)
    {
        switch (type)
        {
            case ShapeType.Line:
                return 0f;

            case ShapeType.Triangle:
                List<Vector2> corners = DouglasPeucker(points, SimplifyEpsilon);
                Vector2 ab = corners[1] - corners[0];
                Vector2 ac = corners[2] - corners[0];
                return MathF.Abs(ab.X * ac.Y - ab.Y * ac.X) / 2f;

            case ShapeType.ClosedLoop:
                return MathF.Abs(ComputeSignedArea(points));

            default:
                return 0f;
        }
    }

    /// <summary>
    /// Area-weighted centroid via the shoelace formula
    /// A plain vertex average drifts toward dense regions of the path, which for free-hand drawing is wherever the mouse slowed down
    /// </summary>
    private static Vector2 PolygonCentroid(ReadOnlySpan<Vector2> pts)
    {
        float cx = 0f, cy = 0f, area = 0f;
        int n = pts.Length;

        for (int i = 0; i < n; i++)
        {
            Vector2 a = pts[i];
            Vector2 b = pts[(i + 1) % n];
            float cross = a.X * b.Y - b.X * a.Y;
            cx += (a.X + b.X) * cross;
            cy += (a.Y + b.Y) * cross;
            area += cross;
        }

        area *= 0.5f;
        float inv = 1f / (6f * area);
        return new Vector2(cx * inv, cy * inv);
    }

    private static float ComputeSignedArea(ReadOnlySpan<Vector2> pts)
    {
        float area = 0f;
        int n = pts.Length;
        for (int i = 0; i < n; i++)
        {
            Vector2 a = pts[i], b = pts[(i + 1) % n];
            area += (b.X - a.X) * (b.Y + a.Y);
        }

        return area * 0.5f;
    }

    #endregion
}

public class Seams : ModProjectile
{
    public override string Texture => AssetRegistry.GennedTextures.SeamStrike.Path;

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 1;
        Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Magic;
        Projectile.ignoreWater = true;
        Projectile.tileCollide = false;
        Projectile.penetrate = 3;
        Projectile.Opacity = 1f;
        Projectile.timeLeft = MaxTime;
        Projectile.MaxUpdates = 2;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
        Projectile.noEnchantmentVisuals = true;
        Projectile.stopsDealingDamageAfterPenetrateHits = true;
    }

    public ref float Time => ref Projectile.ai[0];
    public const int MaxTime = 25;
    public const int MaxWidth = 1400;
    public float Interpolant => InverseLerp(0f, MaxTime, Time);
    public Point Size;

    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.Write(Size.X);
        writer.Write(Size.Y);
    }

    public override void ReceiveExtraAI(BinaryReader reader)
    {
        Size.X = reader.ReadInt32();
        Size.Y = reader.ReadInt32();
    }

    public override void AI()
    {
        Projectile.rotation = Projectile.velocity.ToRotation();

        int width = (int) MakePoly(3f).InOutFunction.Evaluate(70f, MaxWidth, Interpolant);
        int height = (int) MakePoly(3f).OutFunction.Evaluate(100f, 10f, Interpolant);
        Size = new(width, height);
        Projectile.Opacity = MakePoly(2f)
            .InFunction(InverseLerp(0f, 5f * Projectile.MaxUpdates, Projectile.timeLeft));

        Time++;
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        Vector2 size = new(Size.X / 2f, 10);
        return new RotatedRectangle(Projectile.Center - size / 2, size, Projectile.rotation, Vector2.Zero)
            .Intersects(targetHitbox);
    }

    public override bool ShouldUpdatePosition() => false;

    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D tex = AssetRegistry.GennedTextures.GlowParticleSmall;

        Vector2 origin = tex.Size() * 0.5f;
        Color col = Color.Lerp(Color.Magenta, Color.LightCoral, Projectile.identity / 7f % 1f) * Projectile.Opacity;

        for (float i = .5f; i < 1f; i += .1f)
        {
            SpriteBatch.DrawRectPixelated(PixelationLayer.Dusts, BlendState.Additive, tex,
                ToTarget(Projectile.Center, Size.ToVector2() * i * .4f * Projectile.Opacity), null,
                Color.White * Projectile.Opacity, Projectile.rotation, origin);
            SpriteBatch.DrawRectPixelated(PixelationLayer.Dusts, BlendState.Additive, tex,
                ToTarget(Projectile.Center, Size.ToVector2() * i), null, col,
                Projectile.rotation, origin);
            SpriteBatch.DrawRectPixelated(PixelationLayer.Dusts, BlendState.Additive, tex,
                ToTarget(Projectile.Center, Size.ToVector2() * i * 1.3f), null,
                new Color(77, 0, 110) * Projectile.Opacity * .4f, Projectile.rotation, origin);
        }

        return false;
    }
}

public class NeedleStar : ModProjectile
{
    public override string Texture => AssetRegistry.GennedTextures.Invisible.Path;

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 25;
        Projectile.friendly = true;
        Projectile.tileCollide = false;
        Projectile.DamageType = DamageClass.Magic;
        Projectile.penetrate = 2;
        Projectile.extraUpdates = 5;
        Projectile.timeLeft = 120;
        Projectile.localNPCHitCooldown = 20;
        Projectile.usesLocalNPCImmunity = true;
    }

    public ref float Time => ref Projectile.ai[0];

    public override void AI()
    {
        if (trail == null || trail.Disposed)
            trail = new(WidthFunction, ColorFunction, null, 30);

        cache ??= new(20);
        cache.Update(Projectile.Center);

        if (Projectile.numHits > 0 || Projectile.timeLeft < 20)
        {
            Projectile.velocity *= .96f;
            Projectile.timeLeft = 20;
            if (cache.Points.AllPointsEqual())
                Projectile.Kill();
        }

        Projectile.Opacity = InverseLerp(0f, 5f * Projectile.MaxUpdates, Time) *
                             InverseLerp(0f, 2f, Projectile.velocity.Length());
        Time++;
    }

    internal Color ColorFunction(SystemVector2 completionRatio, Vector2 position)
    {
        float fadeToEnd = MathHelper.Lerp(0.65f, 1f, Cos01((0f - Main.GlobalTimeWrappedHourly) * 3f));
        float fadeOpacity = Utils.GetLerpValue(1f, 0.64f, completionRatio.X, true) * Projectile.Opacity;
        Color endColor = Color.Lerp(Color.Cyan, Color.Magenta,
            Sin01(completionRatio.X * (float) Math.PI * 1.6f - Main.GlobalTimeWrappedHourly * 4f));
        return Color.Lerp(Color.White, endColor, fadeToEnd) * fadeOpacity;
    }

    internal float WidthFunction(float c)
    {
        return Trail.HemisphereWidthFunct(c, MathHelper.SmoothStep(Projectile.height * .75f, 0f, c));
    }

    public TrailPoints cache;
    public Trail trail;
    public override bool? CanHitNPC(NPC target) => Projectile.numHits <= 0 ? null : false;

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        SoundID.DD2_WitherBeastCrystalImpact.Play(Projectile.Center, .7f, 0f, .1f);
    }

    public override bool PreDraw(ref Color lightColor)
    {
        void draw()
        {
            if (trail != null)
            {
                ManagedShader shader = AssetRegistry.GennedShaders.FadedStreak;
                shader.SetTexture(AssetRegistry.GennedTextures.StreakMagma, 1);
                shader.SetTexture(AssetRegistry.GennedTextures.WavyBlotchNoise, 2);
                trail.DrawTrail(shader, cache.Points, 100);
            }
        }

        PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.UnderProjectiles);

        Texture2D starTexture = AssetRegistry.GennedTextures.CritSpark;
        Texture2D bloomTexture = AssetRegistry.GennedTextures.GlowParticleSmall;
        Color color = ColorFunction(SystemVector2.Zero, Vector2.Zero);
        float rotation = Main.GlobalTimeWrappedHourly * 8f;

        SpriteBatch.DrawRectPixelated(PixelationLayer.UnderProjectiles, BlendState.Additive, bloomTexture,
            ToTarget(Projectile.Center, new Vector2(50)), null,
            color * .6f, 0f, bloomTexture.Size() / 2);
        SpriteBatch.DrawRectPixelated(PixelationLayer.UnderProjectiles, BlendState.Additive, bloomTexture,
            ToTarget(Projectile.Center, new Vector2(90)), null,
            color * .4f, 0f, bloomTexture.Size() / 2);
        SpriteBatch.DrawAltPixelated(PixelationLayer.UnderProjectiles, BlendState.Additive, starTexture,
            Projectile.Center, null, Color.White * Projectile.Opacity,
            rotation, starTexture.Size() / 2, Projectile.scale * 2.3f);
        SpriteBatch.DrawAltPixelated(PixelationLayer.UnderProjectiles, BlendState.Additive, starTexture,
            Projectile.Center, null, Color.White * Projectile.Opacity,
            -rotation + MathHelper.PiOver4, starTexture.Size() / 2, Projectile.scale * 1.6f);

        return false;
    }
}

public class ConcentratedEnergy : ModProjectile
{
    public override string Texture => AssetRegistry.GennedTextures.ConcentratedEnergy.Path;

    public override void SetDefaults()
    {
        Projectile.width = 74;
        Projectile.height = 30;
        Projectile.friendly = true;
        Projectile.tileCollide = false;
        Projectile.penetrate = -1;
        Projectile.timeLeft = 200;
        Projectile.DamageType = DamageClass.Magic;
        Projectile.tileCollide = false;
        Projectile.localNPCHitCooldown = 10;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.Opacity = 0f;
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        return targetHitbox.LineCollision(Projectile.BaseRotHitbox().Left, Projectile.BaseRotHitbox().Right,
            Projectile.height);
    }

    public override bool? CanHitNPC(NPC target) => HasHitTarget ? false : null;

    public bool HasHitTarget
    {
        get => Projectile.ai[0] == 1f;
        set => Projectile.ai[0] = value.ToInt();
    }

    public int Time
    {
        get => (int) Projectile.ai[1];
        set => Projectile.ai[1] = value;
    }

    public override void AI()
    {
        if (trail == null || trail.Disposed)
            trail = new(WidthFunction, ColorFunction, null, 10);

        Lighting.AddLight(Projectile.Center, Color.Fuchsia.ToVector3() * 1.2f * Projectile.scale);

        if (Time > SecondsToFrames(.5f) && !HasHitTarget)
        {
            if (NPCTargeting.TryGetClosestNPC(new(Projectile.Center, 1400, false, true), out NPC target))
                Projectile.velocity = Vector2.SmoothStep(Projectile.velocity,
                    Projectile.SafeDirectionTo(target.Center) * 30f, .22f);
        }

        if (HasHitTarget)
        {
            int type = (Projectile.identity % 2f == 0f).ToDirectionInt();
            Projectile.velocity = Projectile.velocity.RotatedBy(MathHelper.Pi / 120f * type) * 0.9f;
            Projectile.Center += Main.rand.NextVector2CircularEdge(3f, 3f);
            Projectile.Opacity = InverseLerp(0f, 40f, Projectile.timeLeft);
        }
        else
        {
            Projectile.Opacity = Projectile.scale = MakePoly(2f).InFunction(InverseLerp(0f, 15f, Time)) *
                                                    InverseLerp(0f, 20f, Projectile.timeLeft);
        }

        Projectile.FacingRight();

        cache ??= new(10);
        cache.Update(Projectile.RotHitbox().Left);

        Time++;
    }

    internal Color ColorFunction(SystemVector2 completionRatio, Vector2 position)
    {
        float fadeToEnd = MathHelper.Lerp(0.65f, 1f, Cos01((0f - Main.GlobalTimeWrappedHourly) * 3f));
        float fadeOpacity = Utils.GetLerpValue(1f, 0.64f, completionRatio.X, true) * InverseLerp(0f, 8f, Time) *
                            Projectile.Opacity;
        Color endColor = Color.Lerp(Color.DarkMagenta, Color.Cyan,
            Sin01(completionRatio.X * (float) Math.PI * 1.6f - Main.GlobalTimeWrappedHourly * 4f));
        return Color.Lerp(Color.White, endColor, fadeToEnd) * fadeOpacity;
    }

    internal float WidthFunction(float completionRatio)
    {
        return Projectile.width * 0.4f *
               MathHelper.SmoothStep(0.2f, 1f, Utils.GetLerpValue(0f, 0.3f, completionRatio, true)) *
               Projectile.Opacity;
    }

    public TrailPoints cache;
    public Trail trail;

    public override bool PreDraw(ref Color lightColor)
    {
        void draw()
        {
            if (trail != null)
            {
                ManagedShader shader = AssetRegistry.GennedShaders.FadedStreak;
                shader.SetTexture(AssetRegistry.GennedTextures.ShadowTrail, 1);
                trail.DrawTrail(shader, cache.Points, 40, true);
            }
        }

        PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.UnderProjectiles);

        Texture2D texture = Projectile.ThisProjectileTexture();
        Main.spriteBatch.DrawBetter(texture, Projectile.Center, null,
            Color.Lerp(lightColor, Color.White, 0.5f) * Projectile.Opacity,
            Projectile.rotation, texture.Size() / 2f, Projectile.scale);

        return false;
    }

    public override void OnKill(int timeLeft)
    {
        SoundID.DD2_WitherBeastDeath.Play(Projectile.Center, 1.1f, 0f, .1f);

        if (NPCTargeting.TryGetClosestNPC(new(Projectile.Center, 900, false, true), out NPC target))
        {
            Vector2 targ = target.RandAreaInEntity();
            Vector2 pos = Projectile.RotHitbox().Left + Projectile.velocity.SafeNormalize(Vector2.Zero) * 22f;
            if (target.CanHomeInto() && this.RunLocal())
            {
                Vector2 vel = Projectile.SafeDirectionTo(targ) * Main.rand.NextFloat(9f, 14f);
                Projectile.CreateProj(pos, vel, ModContent.ProjectileType<NeedleStar>(), Projectile.damage / 2,
                    Projectile.knockBack, Projectile.owner);
                SoundEngine.PlaySound(SoundID.Item105 with { Volume = .8f, MaxInstances = 20, Pitch = .4f },
                    Projectile.Center);
                ParticleRegistry.SpawnDetailedBlastParticle(pos, Vector2.Zero, new Vector2(.45f, 1f) * 60f,
                    Vector2.Zero, 30, Color.Magenta, vel.ToRotation(), null, true);
                ParticleRegistry.SpawnDetailedBlastParticle(pos, Vector2.Zero, new Vector2(.45f, 1f) * 90f,
                    Vector2.Zero, 30, Color.Magenta, vel.ToRotation(), null, true);
            }

            const int amount = 30;
            for (int i = 0; i < amount; i++)
            {
                Vector2 vel = NextVector2Ellipse(5f, 10f, Projectile.AngleTo(targ));
                float scale = Main.rand.NextFloat(.3f, .6f);
                ParticleRegistry.SpawnSparkParticle(pos, vel, 40, scale,
                    Color.DarkViolet.Lerp(Color.Violet, Main.rand.NextFloat(.2f, .9f)));
            }
        }
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (!HasHitTarget)
        {
            Projectile.velocity *= 2f;
            Projectile.timeLeft = 40;
            HasHitTarget = true;
        }

        Vector2 pos = Projectile.BaseRotHitbox().Right;
        for (int i = 0; i < 12; i++)
        {
            Vector2 vel = Projectile.velocity.SafeNormalize(Vector2.Zero).RotatedByRandom(.2f) *
                          -Main.rand.NextFloat(2f, 12f);
            int life = Main.rand.Next(20, 40);
            float scale = Main.rand.NextFloat(.5f, .78f);
            Color col = Color.Magenta.Lerp(Color.DodgerBlue, Main.rand.NextFloat());
            ParticleRegistry.SpawnBloomPixelParticle(pos, vel, life, scale * 1.2f, col, Color.White, null, 1.1f);
            ParticleRegistry.SpawnSparkleParticle(pos, vel, life, scale * 1.3f, col, Color.White, 1.4f, .14f);
        }
    }
}
