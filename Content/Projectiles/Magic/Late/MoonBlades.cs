using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using CalamityMod.Graphics.Primitives;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.Events;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Items.Weapons.Magic.Late;
using TheExtraordinaryAdditions.Content.Projectiles.Base;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;
using static Microsoft.Xna.Framework.MathHelper;
using ParticleRegistry = TheExtraordinaryAdditions.Common.Particles.Particle.ParticleRegistry;

namespace TheExtraordinaryAdditions.Content.Projectiles.Magic.Late;

public class MoonBlades : BaseHoldoutProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.MoonBlade);

    public override void Defaults()
    {
        Projectile.width = Projectile.height = 1;
        Projectile.DamageType = DamageClass.Magic;
    }

    public int OpacTime
    {
        get => (int)Projectile.ai[0];
        set => Projectile.ai[0] = value;
    }

    public int MaxOpacTime = 14;

    public ref float BackRot => ref Projectile.ai[1];
    public ref float FrontRot => ref Projectile.ai[2];

    public int AimTime
    {
        get => (int)Projectile.AdditionsInfo().ExtraAI[0];
        set => Projectile.AdditionsInfo().ExtraAI[0] = value;
    }

    public int MaxAimTime = 24;

    public int ShootTime
    {
        get => (int)Projectile.AdditionsInfo().ExtraAI[1];
        set => Projectile.AdditionsInfo().ExtraAI[1] = value;
    }

    public int ShootWait = 4;

    public bool Swap
    {
        get => (int)Projectile.AdditionsInfo().ExtraAI[2] == 1;
        set => Projectile.AdditionsInfo().ExtraAI[2] = value.ToInt();
    }

    public override void OnSpawn(IEntitySource source)
    {
        Projectile.velocity = Vector2.UnitY;
        FrontRot = BackRot = PiOver2;
        this.Sync();
    }

    public override void SafeAI()
    {
        Projectile.Opacity = InverseLerp(0f, MaxOpacTime, OpacTime);
        
        NPC closest = NPCTargeting.GetClosestNPC(new(Mouse, 4000, false, false));
        
        if (OpacTime < MaxOpacTime)
            OpacTime++;
        
        if (this.RunLocal() && Modded.SafeMouseLeft.Current && closest.CanHomeInto())
        {
            if (AimTime < MaxAimTime)
                AimTime++;

            if (AimTime == MaxAimTime && TryUseMana(true) && ShootTime == 0)
            {
                Vector2 pos = closest.Center;
                Projectile.NewProj(pos, Vector2.Zero, ModContent.ProjectileType<MoonPortal>(), Projectile.damage,
                    Projectile.knockBack, Owner.whoAmI, 0f, pos.X, pos.Y);
                ShootTime = ShootWait;
                Swap = !Swap;

                Texture2D tex = Projectile.ThisProjectileTexture();
                Vector2 off = PolarVector(tex.Width, FrontRot) +
                              PolarVector(tex.Height / 2f, FrontRot - (PiOver2 * Owner.direction));

                Vector2 frontTip = (Swap ? Owner.GetFrontHandPositionImproved() : Owner.GetBackHandPositionImproved()) +
                                   off;

                ParticleRegistry.SpawnSparkleParticle(frontTip, Vector2.Zero, ShootWait,
                    Main.rand.NextFloat(2.1f, 2.4f),
                    Color.White, MoonPortal.StripColor, 1.2f);
                for (int i = 0; i < 10; i++)
                {
                    ParticleRegistry.SpawnGlowParticle(frontTip, Main.rand.NextVector2Circular(6f, 6f),
                        Main.rand.Next(12, 14), Main.rand.NextFloat(12f, 21f),
                        MoonPortal.StripColor.Lerp(Color.White, Main.rand.NextFloat(0f, .4f)),
                        Main.rand.NextFloat(.8f, 1.4f));
                }
            }

            if (ShootTime > 0)
                ShootTime--;
        }
        else
        {
            if (AimTime > 0)
                AimTime--;
        }

        Projectile.Center = Owner.Center;
        Projectile.velocity = Projectile.Center.SafeDirectionTo(Mouse);
        
        float interpol = Animators.MakePoly(4f).OutFunction(InverseLerp(0f, MaxAimTime, AimTime));
        float target = PiOver2.AngleLerp(Projectile.velocity.ToRotation(), interpol);
        BackRot = BackRot.AngleLerp(target, .14f);
        FrontRot = FrontRot.AngleLerp(target, .14f);
        Owner.ChangeDir(closest == null ? Projectile.velocity.X.NonZeroSign() : BackRot.ToRotationVector2().X.NonZeroSign());

        Owner.SetBackHandBetter(Player.CompositeArmStretchAmount.Full, BackRot);
        Owner.SetFrontHandBetter(Player.CompositeArmStretchAmount.Full, FrontRot);

    }

    public override bool ShouldDie()
    {
        return !Owner.Available() || (AimTime == 0 && (this.RunLocal() && !Modded.SafeMouseLeft.Current));
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D tex = Projectile.ThisProjectileTexture();
        Vector2 orig = new(0f, Owner.direction == -1 ? 0f : tex.Height);

        PixelationSystem.QueuePrimitiveRenderAction(DrawPortal, PixelationLayer.UnderPlayers);
        LayeredDrawSystem.QueueDrawAction(behind, PixelationLayer.UnderPlayers);
        LayeredDrawSystem.QueueDrawAction(over, PixelationLayer.OverPlayers);

        return false;

        void over()
        {
            Vector2 pos = Owner.GetFrontHandPositionImproved();
            Color col = (Color.Cyan with { A = 0 }).Lerp(Lighting.GetColor(pos.ToTileCoordinates()),
                Projectile.Opacity);
            Main.spriteBatch.DrawBetter(tex, pos, null, col,
                FrontRot, orig, 1f,
                FixedDirection());
        }

        void behind()
        {
            Vector2 pos = Owner.GetBackHandPositionImproved();
            Color col = (Color.Cyan with { A = 0 }).Lerp(Lighting.GetColor(pos.ToTileCoordinates()),
                Projectile.Opacity);
            Main.spriteBatch.DrawBetter(tex, pos, null, col,
                BackRot, orig, 1f,
                FixedDirection());
        }
    }

    public void DrawPortal()
    {
        float to = BackRot;
        Quaternion portalRot = Animators.EulerAnglesConversion(1, to + PiOver2, 0f);
        Vector2 start = Vector2.Transform(Projectile.Center - Main.screenPosition + PolarVector(150f, to),
            Matrix.Invert(Main.GameViewMatrix?.ZoomMatrix ?? Matrix.Identity));
        start += Main.screenPosition;

        float interpol = Animators.MakePoly(4f).OutFunction(InverseLerp(0f, MaxAimTime, AimTime));
        float scale = 400f * interpol;
        VertexPositionColorTexture[] quad = GenerateQuadClockwise(new(scale, scale / 2), Color.White, true);

        ManagedShader portalShader = AssetRegistry.GetShader("MoonPortalBack");
        portalShader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.OrganicNoise), 1, SamplerState.LinearWrap);
        portalShader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.NeuronNoise), 2,
            SamplerState.LinearWrap);
        portalShader.TrySetParameter("globalTime", -Main.GlobalTimeWrappedHourly * .9f);
        portalShader.TrySetParameter("scale", interpol);
        portalShader.TrySetParameter("vertexMatrix", Get3DTextureMatrix(start, portalRot, Projectile.scale, 0f, 1));
        portalShader.Effect.CurrentTechnique.Passes[ManagedShader.DefaultPassName].Apply();

        GraphicsDevice gd = Main.graphics.GraphicsDevice;
        DepthStencilState prevStencil = gd.DepthStencilState;
        BlendState prevBlend = gd.BlendState;
        gd.DepthStencilState = DepthStencilState.Default;
        gd.BlendState = BlendState.AlphaBlend;
        gd.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, quad, 0, quad.Length, TextureQuadIndices, 0,
            TextureQuadIndices.Length / 3);
        gd.DepthStencilState = prevStencil;
        gd.BlendState = prevBlend;
    }
}

public class MoonPortal : ModProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.GlowRing);

    public static readonly Color StripColor = new(16, 254, 254);
    public static readonly Color OuterColor = new(39, 78, 255);
    public static readonly Color InnerColor = new(164, 84, 255);
    public static readonly Color CenterColor = new(4, 64, 50);

    public Quaternion Rotation { get; set; }

    public int Time
    {
        get => (int)Projectile.ai[0];
        set => Projectile.ai[0] = value;
    }

    public Vector2 Target
    {
        get => new Vector2((int)Projectile.ai[1], (int)Projectile.ai[2]);
        set
        {
            Projectile.ai[1] = value.X;
            Projectile.ai[2] = value.Y;
        }
    }

    /// <summary>
    /// The position of the portal in 3D space
    /// </summary>
    public Vector3 Position;

    public const int Lifetime = 40;

    public override void SetDefaults()
    {
        Projectile.width = 20;
        Projectile.height = 20;
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.friendly = true;
        Projectile.stopsDealingDamageAfterPenetrateHits = true;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
        Projectile.timeLeft = Lifetime;
    }

    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.Write(Rotation.X);
        writer.Write(Rotation.Y);
        writer.Write(Rotation.Z);
        writer.Write(Rotation.W);

        writer.Write(Position.X);
        writer.Write(Position.Y);
        writer.Write(Position.Z);
    }

    public override void ReceiveExtraAI(BinaryReader reader)
    {
        float x = reader.ReadSingle();
        float y = reader.ReadSingle();
        float z = reader.ReadSingle();
        float w = reader.ReadSingle();
        Rotation = new(x, y, z, w);

        float xv = reader.ReadSingle();
        float yv = reader.ReadSingle();
        float zv = reader.ReadSingle();
        Position = new(xv, yv, zv);
    }

    public override void AI()
    {
        if (Time == 0)
        {
            if (Projectile is { active: true, timeLeft: > 0 })
                MoonPortalDrawSystem.RegisterPortal(this);
            
            Position = new Vector3(Target, 0f) + RandomInSphere(800f, .5f, 1f);

            AdditionsSound.MachinaBlast.Play(new Vector2(Position.X, Position.Y), .4f, 0f, .2f, 50);

            Quaternion offset = Quaternion.CreateFromAxisAngle(Vector3.Right, ToRadians(-90f));
            Rotation = LookAt(
                           Position,
                           new Vector3(Target, 0f), Vector3.Up) *
                       offset;

            Vector2 start = new Vector2(Position.X, Position.Y);
            Vector2 strikePos = RaytraceNPCs(start, Target) ?? Vector2.Zero;
            for (int i = 0; i < 20; i++)
            {
                float rand = Main.rand.NextFloat();
                int life = (int)Lerp(20, 50, rand);
                float speed = Lerp(20f, 4f, rand);
                Vector2 vel = Main.rand.NextVector2Circular(speed, speed);
                Color col = Color.Lerp(StripColor, OuterColor, Main.rand.NextFloat());
                ParticleRegistry.SpawnBloomLineParticle(strikePos, vel, life, Main.rand.NextFloat(.2f, .5f), col);

                ParticleRegistry.SpawnGlowParticle(strikePos, Vector2.Zero, 12, Main.rand.NextFloat(90f, 120f),
                    StripColor.Lerp(Color.White, Main.rand.NextFloat(0f, .4f)), Main.rand.NextFloat(.7f, 1.2f));
            }

            ParticleRegistry.SpawnSparkleParticle(strikePos, Vector2.Zero, Main.rand.Next(20, 30),
                Main.rand.NextFloat(2.4f, 3.2f), Color.White, StripColor, 1.4f);
        }

        float bump = Animators.MakePoly(2f).InOutFunction(InverseLerp(0, 10, Time))
                     * Animators.MakePoly(3f).OutFunction(InverseLerp(Lifetime, Lifetime - 10, Time));
        Projectile.scale = bump;

        Projectile.rotation = Time * .02f;

        Time++;
    }

    private const int widthSegs = 40;
    private const int heightSegs = 40;

    /// <summary>
    /// Draws the weakly glowing cylinder for the magic circle.
    /// </summary>
    /// <param name="drawOffset">The draw offset of the cylinder.</param>
    /// <param name="rotation">The cylinder's rotation.</param>
    /// <param name="color">The color of the cylinder.</param>
    public void DrawBackglowCylinder(Vector2 drawOffset, Quaternion rotation, Color color, float extraScale)
    {
        short[] indices = GenerateCylinderIndices(widthSegs, heightSegs);
        Matrix rotationMatrix = Matrix.CreateFromQuaternion(rotation);
        Vertex3D[] vertices =
            GenerateCylinderVertices(widthSegs, heightSegs, 100f * extraScale * Projectile.scale, 600f, rotationMatrix,
                Position, color);

        ManagedShader ringShader = AssetRegistry.GetShader("MoonPortalGlowShader");
        ringShader.TrySetParameter("projection", Get3DPerspectivePrimitiveMatrix());
        ringShader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.DendriticNoiseZoomedOut), 1,
            SamplerState.LinearWrap);
        ringShader.Render();

        var gd = Main.instance.GraphicsDevice;
        BlendState prev = gd.BlendState;
        gd.RasterizerState = RasterizerState.CullNone;
        gd.BlendState = BlendState.AlphaBlend;
        gd.DepthStencilState = DepthStencilState.Default;

        gd.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, vertices, 0, vertices.Length, indices, 0,
            vertices.Length / 3);
        gd.BlendState = prev;
    }

    /// <summary>
    /// Draws the depthed right with the symbol text.
    /// </summary>
    /// <param name="drawOffset">The draw offset of the ring.</param>
    /// <param name="rotation">The ring's rotation.</param>
    /// <param name="ringColor">The color of the ring.</param>
    public void DrawRing(Vector3 drawOffset, Quaternion rotation, Color ringColor, float extraScale)
    {
        short[] indices = GenerateCylinderIndices(widthSegs, heightSegs);
        Matrix rotationMatrix = Matrix.CreateFromQuaternion(rotation);
        Vertex3D[] vertices =
            GenerateCylinderVertices(widthSegs, heightSegs, 100f * extraScale * Projectile.scale,
                200f * Projectile.scale, rotationMatrix,
                Position + drawOffset, ringColor);

        ManagedShader ringShader = AssetRegistry.GetShader("MoonPortalShader");
        ringShader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.TechyNoise), 1, SamplerState.LinearWrap);
        ringShader.TrySetParameter("spinScrollOffset", Projectile.rotation * -0.75f);
        ringShader.TrySetParameter("projection", Get3DPerspectivePrimitiveMatrix());
        ringShader.Render();

        var gd = Main.instance.GraphicsDevice;
        BlendState prev = gd.BlendState;
        gd.RasterizerState = RasterizerState.CullNone;
        gd.BlendState = BlendState.Additive;
        gd.DepthStencilState = DepthStencilState.Default;

        gd.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, vertices, 0, vertices.Length, indices, 0,
            vertices.Length / 3);
        gd.BlendState = prev;
    }

    public void DrawToTarget()
    {
        DrawBackglowCylinder(Vector2.Zero, Rotation, StripColor, 1f);
        DrawRing(Vector3.Zero, Rotation, StripColor, 1f);

        Matrix rotMat = Matrix.CreateFromQuaternion(Rotation);
        Vector3 forward = new(rotMat.M21, rotMat.M22, rotMat.M23);
        DrawRing(forward * 30f, Rotation, StripColor * .75f, .7f);
    }

    public override bool PreDraw(ref Color lightColor)
    {
        return false;
    }

    #region Utils

    private static Quaternion LookAt(Vector3 from, Vector3 to, Vector3 up)
    {
        Vector3 forward = Vector3.Normalize(to - from);
        Matrix rotMatrix = Matrix.CreateWorld(Vector3.Zero, forward, up);
        return Quaternion.CreateFromRotationMatrix(rotMatrix);
    }

    private static Vector3 RandomInSphere(float radius, float minPercent, float maxPercent)
    {
        float theta = RandomRotation();
        float phi = (float)Math.Acos(2.0 * Main.rand.NextDouble() - 1.0);

        float x = (float)(Math.Sin(phi) * Math.Cos(theta));
        float y = (float)(Math.Sin(phi) * Math.Sin(theta));
        float z = (float)Math.Cos(phi);

        float t = (float)Math.Pow(Main.rand.NextDouble(), 1.0 / 3.0);
        float r = radius * Lerp(minPercent, maxPercent, t);

        return new Vector3(x, y, z) * r;
    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public readonly struct Vertex3D(Vector3 position, Color color, Vector3 texCoord) : IVertexType
    {
        public static readonly VertexDeclaration VertexDeclaration2D = new(
        [
            new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
            new VertexElement(12, VertexElementFormat.Color, VertexElementUsage.Color, 0),
            new VertexElement(16, VertexElementFormat.Vector3, VertexElementUsage.TextureCoordinate, 0)
        ]);

        public static readonly VertexDeclaration VertexDeclaration = VertexDeclaration2D;

        VertexDeclaration IVertexType.VertexDeclaration
        {
            get => VertexDeclaration;
        }

        public readonly Vector3 position = position;
        public readonly Color color = color;
        public readonly Vector3 texCoord = texCoord;
    }

    private static Vertex3D[] GenerateCylinderVertices(int widthSegments, int heightSegments,
        float radius, float length, Matrix rotation, Vector3 start, Color color)
    {
        int numVertices = (widthSegments + 1) * (heightSegments + 1);
        Vertex3D[] vertices = new Vertex3D[numVertices];

        for (int i = 0; i <= heightSegments; i++)
        {
            float v = (float)i / heightSegments;
            float y = v * length;

            for (int j = 0; j <= widthSegments; j++)
            {
                float u = (float)j / widthSegments;
                float angle = u * TwoPi;

                float x = MathF.Cos(angle) * radius;
                float z = MathF.Sin(angle) * radius;

                Vector3 localPosition = new Vector3(x, y, z);
                Vector3 transformedPosition = Vector3.Transform(localPosition, rotation) + start;

                int index = i * (widthSegments + 1) + j;
                float angleCosine = MathF.Cos(angle);
                vertices[index] = new Vertex3D(transformedPosition, color, new Vector3(u, v, angleCosine));
            }
        }

        return vertices;
    }

    private static short[] GenerateCylinderIndices(int widthSegments, int heightSegments)
    {
        int numIndices = widthSegments * heightSegments * 6;
        short[] indices = new short[numIndices];

        int idx = 0;
        for (int i = 0; i < heightSegments; i++)
        {
            for (int j = 0; j < widthSegments; j++)
            {
                int bottomLeft = i * (widthSegments + 1) + j;
                int bottomRight = bottomLeft + 1;
                int topLeft = bottomLeft + (widthSegments + 1);
                int topRight = topLeft + 1;

                // First triangle
                indices[idx++] = (short)bottomLeft;
                indices[idx++] = (short)topLeft;
                indices[idx++] = (short)bottomRight;

                // Second triangle
                indices[idx++] = (short)bottomRight;
                indices[idx++] = (short)topLeft;
                indices[idx++] = (short)topRight;
            }
        }

        return indices;
    }

    #endregion
}

public class MoonPortalDrawSystem : ModSystem
{
    private static readonly List<MoonPortal> ActivePortals = [];
    private static ManagedRenderTarget _beforeRenderTarget;
    private static ManagedRenderTarget _afterRenderTarget;

    public override void PostUpdateProjectiles()
    {
        for (int i = ActivePortals.Count - 1; i >= 0; i--)
        {
            MoonPortal portal = ActivePortals[i];
            if (portal == null || !portal.Projectile.active)
                ActivePortals.RemoveAt(i);
        }
    }

    public static void RegisterPortal(MoonPortal portal)
    {
        if (!ActivePortals.Contains(portal) && ActivePortals.Count < Main.maxProjectiles)
            ActivePortals.Add(portal);
    }

    public override void OnModLoad()
    {
        if (Main.netMode == NetmodeID.Server)
            return;

        Main.QueueMainThreadAction(static () =>
        {
            GraphicsDevice gd = Main.instance.GraphicsDevice;
            _beforeRenderTarget = new ManagedRenderTarget(true, (w, h) => new RenderTarget2D(gd, w / 2, h / 2), true);
            gd.SetRenderTarget(_beforeRenderTarget);
            gd.Clear(Color.Transparent);
            _afterRenderTarget = new ManagedRenderTarget(true, (w, h) => new RenderTarget2D(gd, w / 2, h / 2), true);
            gd.SetRenderTarget(_afterRenderTarget);
            gd.Clear(Color.Transparent);
            gd.SetRenderTarget(null);

            On_Main.DoDraw_WallsTilesNPCs += BeforeAnything;
            On_ScreenObstruction.Draw += AfterAnything;
        });

        RenderTargetManager.RenderTargetUpdateLoopEvent += DrawToTargets;
    }

    public override void OnModUnload()
    {
        if (Main.netMode == NetmodeID.Server)
            return;

        Main.QueueMainThreadAction(static () =>
        {
            _beforeRenderTarget = null;
            _afterRenderTarget = null;
            On_Main.DoDraw_WallsTilesNPCs -= BeforeAnything;
            On_ScreenObstruction.Draw -= AfterAnything;
        });
    }

    private static void DrawToTargets()
    {
        if (!AssetRegistry.HasFinishedLoading || Main.gameMenu || Main.netMode == NetmodeID.Server)
            return;

        GraphicsDevice device = Main.instance.GraphicsDevice;

        device.SetRenderTarget(_beforeRenderTarget);
        device.Clear(Color.Transparent);
        List<Projectile> projs = AllProjectilesByID(ModContent.ProjectileType<MoonPortal>());
        foreach (Projectile proj in projs)
        {
            MoonPortal portal = proj.As<MoonPortal>();
            if (portal.Position.Z >= 0f)
            {
                portal.DrawToTarget();
            }
        }

        device.SetRenderTarget(_afterRenderTarget);
        device.Clear(Color.Transparent);
        foreach (Projectile proj in projs)
        {
            MoonPortal portal = proj.As<MoonPortal>();
            if (float.IsNegative(portal.Position.Z))
            {
                portal.DrawToTarget();
            }
        }

        device.SetRenderTarget(null);
    }

    private static void BeforeAnything(On_Main.orig_DoDraw_WallsTilesNPCs orig, Main self)
    {
        if (ActivePortals.Count != 0 && AssetRegistry.HasFinishedLoading &&
            Main.netMode != NetmodeID.Server)
        {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.PointClamp,
                DepthStencilState.None, Main.Rasterizer, null, Matrix.CreateScale(2f));

            Main.spriteBatch.Draw(_beforeRenderTarget, Vector2.Zero, null, Color.White, 0f, Vector2.Zero, 1f,
                SpriteEffects.None, 0f);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, Main.Rasterizer, null, Main.Transform);
        }

        orig(self);
    }

    private static void AfterAnything(On_ScreenObstruction.orig_Draw orig, SpriteBatch sb)
    {
        if (ActivePortals.Count != 0 && AssetRegistry.HasFinishedLoading && !Main.gameMenu &&
            Main.netMode != NetmodeID.Server)
        {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.PointClamp,
                DepthStencilState.None, Main.Rasterizer, null, Matrix.CreateScale(2f));
            Main.spriteBatch.Draw(_afterRenderTarget, Vector2.Zero, null, Color.White, 0f, Vector2.Zero, 1f,
                SpriteEffects.None, 0f);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, Main.Rasterizer, null, Main.Transform);
        }

        orig(sb);
    }
}