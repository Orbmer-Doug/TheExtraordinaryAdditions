using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Assets.Audio;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Classless.Late.Cynosure;

public class BlackHole : ModProjectile, ILocalizedModType, IModType, IHasScreenShader
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.BlackHoleSwirl);

    public Player Owner => Main.player[Projectile.owner];

    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.DrawScreenCheckFluff[Type] = 10000;
    }

    public override void SetDefaults()
    {
        Projectile.aiStyle = -1;
        Projectile.width = 2;
        Projectile.height = 2;
        Projectile.friendly = true;
        Projectile.tileCollide = false;
        Projectile.penetrate = -1;
        Projectile.timeLeft = ChaseTime;
        Projectile.netImportant = true;
        Projectile.DamageType = DamageClass.Generic;

        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;

        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 2;

        Projectile.scale = StartingScale;
    }

    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.Write(Projectile.scale);
    }

    public override void ReceiveExtraAI(BinaryReader reader)
    {
        Projectile.scale = reader.ReadSingle();
    }

    public static readonly int WaitTime = SecondsToFrames(.5f);
    public static readonly int ChaseTime = SecondsToFrames(7) + WaitTime * 2;
    public const float StartingScale = 0f;
    public const float IdealScale = 4f;

    public ref float Time => ref Projectile.ai[1];
    public override bool? CanDamage() => Time >= WaitTime;
    public override void AI()
    {
        Time++;
        Projectile.Opacity = Projectile.scale = GetLerpBump(WaitTime, WaitTime * 2f, ChaseTime, ChaseTime - 90f, Time);

        // Give a slight delay as to give the black hole the emergence from the star
        if (Time > WaitTime && this.RunLocal())
        {
            Vector2 target = Owner.Additions().mouseWorld;
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, Projectile.SafeDirectionTo(target) * MathF.Min(Projectile.Distance(target), 18f), .15f);
            Projectile.netUpdate = true;
        }

        // beeeg
        Projectile.ExpandHitboxBy((int)(Projectile.scale * 380f));
        LoopSounds();
        Succ();
        Projectile.rotation += .15f;
    }

    public LoopedSoundInstance slot;
    public void LoopSounds()
    {
        slot ??= LoopedSoundManager.CreateNew(new(AdditionsSound.blackHoleSuck, () => 4f * Projectile.scale), () => AdditionsLoopedSound.ProjectileNotActive(Projectile));
        slot?.Update(Projectile.Center);
    }

    public void Succ()
    {
        for (int i = 0; i < Main.maxNPCs; i++)
        {
            NPC npc = Main.npc[i];
            if (!npc.CanBeChasedBy(Projectile, false) || !Collision.CanHit(Projectile.Center, 1, 1, npc.Center, 1, 1) || Utility.AnyBosses())
                continue;
            
            float npcCenterX = npc.position.X + npc.width / 2;
            float npcCenterY = npc.position.Y + npc.height / 2;
            if (Math.Abs(Projectile.position.X + Projectile.width / 2 - npcCenterX) + Math.Abs(Projectile.position.Y + Projectile.height / 2 - npcCenterY) < i)
            {
                float power = npc.Distance(Projectile.Center) * (0.8f + Projectile.scale * 0.5f);
                if (npc.position.X < i)
                {
                    npc.velocity.X += power;
                }
                else
                {
                    npc.velocity.X -= power;
                }
                if (npc.position.Y < i)
                {
                    npc.velocity.Y += power;
                }
                else
                {
                    npc.velocity.Y -= power;
                }
            }

        }
    }

    public ManagedScreenShader Shader { get; private set; }
    public bool HasShader { get; private set; } = false;
    public void InitializeShader()
    {
        Shader = ScreenShaderPool.GetShader("BlackHole");
        HasShader = true;
        ScreenShaderUpdates.RegisterEntity(this);
    }

    public void UpdateShader()
    {
        float width = Main.instance.GraphicsDevice.Viewport.Width;
        float height = Main.instance.GraphicsDevice.Viewport.Height;
        Vector2 aspectRatioCorrectionFactor = new(width / (float)height, 1f);
        Vector2 aspectRatioCorrectionFactor2 = new(Main.screenWidth / (float)Main.screenHeight, 1f);
        Vector2 sourcePosition = (WorldSpaceToScreenUV(Projectile.Center) - Vector2.One * 0.5f) * aspectRatioCorrectionFactor2 + Vector2.One * 0.5f;
        float blackRadius = .2f * Projectile.scale;
        Vector3 diskColor = Color.Lerp(Color.Coral, Color.OrangeRed, Sin01(Main.GlobalTimeWrappedHourly * 16f) * 0.25f).ToVector3();
        float time = Main.GlobalTimeWrappedHourly;
        float maxAngle = 28.2f;

        Shader.TrySetParameter("globalTime", time);
        Shader.TrySetParameter("aspectRatioCorrectionFactor", aspectRatioCorrectionFactor);
        Shader.TrySetParameter("maxLensingAngle", maxAngle);
        Shader.TrySetParameter("accretionDiskFadeColor", diskColor);
        Shader.TrySetParameter("sourcePosition", sourcePosition);
        Shader.TrySetParameter("blackRadius", blackRadius);
        Shader.TrySetParameter("distortionStrength", Projectile.scale);
        Shader.TrySetParameter("innerColor", Color.Wheat.ToVector3());
        Shader.TrySetParameter("outerColor", Color.LightGoldenrodYellow.ToVector3());
        Shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.VoronoiShapes), 1, SamplerState.AnisotropicWrap);
        Shader.Activate();
    }

    public void ReleaseShader()
    {
        if (HasShader)
        {
            Shader.Deactivate();
            ScreenShaderPool.ReturnShader("BlackHole", Shader);
            HasShader = false;
            Shader = null;
            ScreenShaderUpdates.UnregisterEntity(this);
        }
    }

    public bool IsEntityActive() => Projectile.active;

    public override bool PreDraw(ref Color lightColor)
    {
        if (!HasShader)
            InitializeShader();

        UpdateShader();

        return false;
    }

    public override bool PreKill(int timeLeft)
    {
        ReleaseShader();
        return true;
    }
}
