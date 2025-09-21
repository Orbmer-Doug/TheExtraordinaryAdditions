using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Classless.Late.Cynosure;
using TheExtraordinaryAdditions.Content.Projectiles.Melee.Late.Everbladed;
using TheExtraordinaryAdditions.Core.DataStructures;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.ScreenEffects;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Systems;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.NPCs.Bosses.Crater.Projectiles;

public class SoulForgedRift : ProjOwnedByNPC<Asterlin>
{
    public override string Texture => AssetRegistry.Invis;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 400;
        Projectile.friendly = true;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.timeLeft = 9000000;
    }

    public Quaternion Rotation
    {
        get;
        set;
    }

    public ref float Animation => ref Projectile.ai[0];
    public int Time
    {
        get => (int)Projectile.ai[2];
        set => Projectile.ai[2] = value;
    }
    public TranscendentSoulRay Ray;

    public override void SendAI(BinaryWriter writer)
    {
        writer.Write(Rotation.X);
        writer.Write(Rotation.Y);
        writer.Write(Rotation.Z);
        writer.Write(Rotation.W);
    }

    public override void ReceiveAI(BinaryReader reader)
    {
        float rotationX = reader.ReadSingle();
        float rotationY = reader.ReadSingle();
        float rotationZ = reader.ReadSingle();
        float rotationW = reader.ReadSingle();
        Rotation = new(rotationX, rotationY, rotationZ, rotationW);
    }

    public override void SafeAI()
    {
        Projectile.Center = Owner.Center;

        float chargeComp = InverseLerp(0f, Asterlin.Hyperbeam_PortalChargeTime, Time);
        if (chargeComp >= 1f && Ray == null)
        {
            Vector2 pos = Projectile.Center;
            Vector2 vel = Vector2.Zero;
            int type = ModContent.ProjectileType<TranscendentSoulRay>();
            int damage = Asterlin.SuperHeavyAttackDamage;
            Ray = Main.projectile[SpawnProjectile(pos, vel, type, damage, 0f)].As<TranscendentSoulRay>();
            Ray.ProjOwner = Projectile;
        }
        if (Ray != null)
            Rotation = Animators.EulerAnglesConversion(1, 0f, Ray.SideAngle - MathHelper.PiOver2);

        if (Boss.Hyperbeam_CurrentState == Asterlin.Hyperbeam_States.Fade)
        {
            Projectile.Opacity = InverseLerp(Asterlin.Hyperbeam_FadeTime, Asterlin.Hyperbeam_FadeTime - 20f, Boss.AITimer);
            if (Projectile.Opacity <= 0f)
                Projectile.Kill();
        }
        else
        {
            Projectile.Opacity = Animators.MakePoly(4f).InFunction(InverseLerp(0f, Asterlin.Hyperbeam_PortalChargeTime, Time));
            Projectile.scale = Animators.Sine.OutFunction(InverseLerp(0f, Asterlin.Hyperbeam_PortalChargeTime, Time));
        }

        Animation += .01f;
        Time++;
    }

    public override bool PreDraw(ref Color lightColor)
    {
        void portal()
        {
            VertexPositionColorTexture[] quad = GenerateQuadClockwise(Projectile.Size, Color.White, true);
            ManagedShader projectionShader = AssetRegistry.GetShader("3DPortalProjection");
            projectionShader.TrySetParameter("vertexMatrix", CalculateTextureMatrix(Projectile.Center, Rotation, Projectile.scale, 0f, 1));
            projectionShader.TrySetParameter("time", Animation);
            projectionShader.TrySetParameter("opacity", Projectile.Opacity);
            projectionShader.Render();

            GraphicsDevice gd = Main.instance.GraphicsDevice;

            RasterizerState prevRast = gd.RasterizerState;
            SamplerState prevState = gd.SamplerStates[1];
            Texture prevTex = gd.Textures[1];
            BlendState prevBlend = gd.BlendState;

            gd.RasterizerState = RasterizerState.CullNone;
            gd.SamplerStates[1] = SamplerState.PointClamp;
            gd.Textures[1] = Projectile.ThisProjectileTexture();
            gd.BlendState = BlendState.AlphaBlend;

            gd.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, quad, 0, quad.Length, TextureQuadIndices, 0, TextureQuadIndices.Length / 3);

            gd.RasterizerState = prevRast;
            gd.SamplerStates[1] = prevState;
            gd.Textures[1] = prevTex;
            gd.BlendState = prevBlend;
        }
        PixelationSystem.QueueTextureRenderAction(portal, PixelationLayer.UnderPlayers);
        return false;
    }
}