using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI.Chat;
using TheExtraordinaryAdditions.Content.Items.Weapons.Ranged.Middle;
using TheExtraordinaryAdditions.Content.Projectiles.Base;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Ranged.Middle;

public class SmartPistolGlobalNPC : GlobalNPC
{
    public override bool InstancePerEntity => true;

    public const int TimeForLock = 60;
    public int[] LockIn = new int[3];
    public Vector2[] LockPositions = new Vector2[3];

    public void InitializeBossLockPoints(NPC npc)
    {
        if (!npc.boss)
        {
            LockPositions[0] = Vector2.Zero;
            return;
        }

        for (int i = 0; i < 3; i++)
        {
            float xOffset = Main.rand.NextFloat(-npc.width / 2f, npc.width / 2f);
            float yOffset = Main.rand.NextFloat(-npc.height / 2f, npc.height / 2f);
            LockPositions[i] = new Vector2(xOffset, yOffset);
            LockIn[i] = 0;
        }
    }
}

public class SmartPistolGlobalPlayer : ModPlayer
{
    public int Shots;
    public const int MaxShots = 12;
    public override void UpdateDead()
    {
        Shots = 0;
    }
}

public class SmartPistolLayer : PlayerDrawLayer
{
    public override Position GetDefaultPosition()
    {
        return new AfterParent(PlayerDrawLayers.IceBarrier);
    }

    public static bool Drawing = false;
    public override void Draw(ref PlayerDrawSet drawInfo)
    {
        Player drawPlayer = drawInfo.drawPlayer;

        Drawing = false;
        if (drawPlayer != null && FindItem(out Item item, drawPlayer, ModContent.ItemType<SmartPistolMK6>()) && drawInfo.shadow == 0f && Main.myPlayer == drawPlayer.whoAmI)
        {
            if (item != Main.mouseItem)
            {
                int shot = (int)MathHelper.Clamp(12 - drawPlayer.GetModPlayer<SmartPistolGlobalPlayer>().Shots, 0, 12);
                string text = $"{shot}/12";
                ChatManager.DrawColorCodedString(Main.spriteBatch, FontAssets.CombatText[1].Value, text, drawPlayer.Center - Vector2.UnitY * 60f - Vector2.UnitX * 22f - Main.screenPosition, Color.White, 0f, Vector2.Zero, Vector2.One * .7f);

                Drawing = true;
            }
        }
    }
}

public class SmartPistolHeld : BaseIdleHoldoutProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.SmartPistolMK6);
    public override int AssociatedItemID => ModContent.ItemType<SmartPistolMK6>();
    public override int IntendedProjectileType => ModContent.ProjectileType<SmartPistolHeld>();

    public override void Defaults()
    {
        Projectile.width = 48;
        Projectile.height = 30;
        Projectile.DamageType = DamageClass.Ranged;
    }


    public List<NPC> Targets = [];
    public List<NPC> StruckTargets = [];

    public int Dir => Projectile.velocity.X.NonZeroSign();
    public Vector2 Tip => Projectile.Center + PolarVector(22f, Projectile.rotation) + PolarVector(12f * Dir, Projectile.rotation - MathHelper.PiOver2);
    public Vector2 LaserTip => Projectile.Center + PolarVector(14f, Projectile.rotation) + PolarVector(8f * Dir, Projectile.rotation - MathHelper.PiOver2);
    public Vector2 CartridgePos => Projectile.Center + PolarVector(-22f, Projectile.rotation) + PolarVector(8f * Dir, Projectile.rotation - MathHelper.PiOver2);

    public const float SightDist = 1000f;
    public const int MaxReloadTime = 130;

    public int ShootTime
    {
        get => (int)Projectile.ai[0];
        set => Projectile.ai[0] = value;
    }
    public bool Shooting
    {
        get => Projectile.ai[1] == 1f;
        set => Projectile.ai[1] = value.ToInt();
    }
    public int NPCIndex
    {
        get => (int)Projectile.ai[2];
        set => Projectile.ai[2] = value;
    }
    public bool Reloading
    {
        get => Projectile.AdditionsInfo().ExtraAI[0] == 1f;
        set => Projectile.AdditionsInfo().ExtraAI[0] = value.ToInt();
    }
    public int ReloadTime
    {
        get => (int)Projectile.AdditionsInfo().ExtraAI[1];
        set => Projectile.AdditionsInfo().ExtraAI[1] = value;
    }
    public int ShotIndex
    {
        get => (int)Projectile.AdditionsInfo().ExtraAI[2];
        set => Projectile.AdditionsInfo().ExtraAI[2] = value;
    }
    public ref int Shots => ref Owner.GetModPlayer<SmartPistolGlobalPlayer>().Shots;

    public override void SafeAI()
    {
        if (this.RunLocal())
        {
            Projectile.velocity = Vector2.SmoothStep(Projectile.velocity, Center.SafeDirectionTo(Mouse), .4f);
            if (Projectile.velocity != Projectile.oldVelocity)
                this.Sync();
        }

        Owner.heldProj = Projectile.whoAmI;
        Owner.ChangeDir(Dir);
        Owner.itemRotation = Projectile.rotation;

        float rotation = Projectile.velocity.ToRotation() * Owner.gravDir + MathHelper.PiOver2;
        float reloadProgress = InverseLerp(0f, MaxReloadTime, ReloadTime);

        if (reloadProgress == .5f)
        {
            SoundEngine.PlaySound(SoundID.Item149 with { Pitch = .3f, Volume = 1.2f }, Projectile.Center);

            if (Main.netMode != NetmodeID.Server)
            {
                Gore.NewGorePerfect(Projectile.GetSource_FromThis(), CartridgePos, -Projectile.velocity,
                    Mod.Find<ModGore>("SmartPistolCartridge").Type);
            }
        }

        float reloadAmt = GetLerpBump(0f, .2f, 1f, .7f, reloadProgress).Squared();
        rotation += 0.55f * reloadAmt * Owner.direction;

        Owner.SetCompositeArmFront(true, 0, rotation - MathHelper.Pi);
        Owner.SetCompositeArmBack(true, 0, 0f);

        Projectile.rotation = Owner.compositeFrontArm.rotation + MathHelper.PiOver2 * Owner.gravDir;
        Projectile.Center = Center + PolarVector(25f, Projectile.rotation);

        if (Shots >= SmartPistolGlobalPlayer.MaxShots && !Reloading)
            Reloading = true;

        if (Reloading)
        {
            Targets.Clear();
            StruckTargets.Clear();
            Shooting = false;
            NPCIndex = ShootTime = 0;
            ShotIndex = 0;
            if (ReloadTime > MaxReloadTime)
            {
                ReloadTime = 0;
                Shots = 0;
                Reloading = false;
            }
            ReloadTime++;
            this.Sync();
            return;
        }

        if (!Shooting)
            SearchForTargets();
        UpdateTargets();

        if (this.RunLocal() && Modded.SafeMouseLeft.Current && !Shooting)
        {
            Shooting = true;
            ShotIndex = 0;
            this.Sync();
        }
        if (Shooting)
        {
            if (NPCIndex >= Targets.Count)
            {
                StruckTargets.Clear();
                Shooting = false;
                NPCIndex = ShootTime = 0;
                ShotIndex = 0;
                return;
            }

            if (ShootTime % 5 == 4)
            {
                NPC n = Targets[NPCIndex];
                SmartPistolGlobalNPC global = n.GetGlobalNPC<SmartPistolGlobalNPC>();
                int maxPoints = n.boss ? 3 : 1;

                if (ShotIndex < maxPoints && Shots < SmartPistolGlobalPlayer.MaxShots)
                {
                    if (global.LockIn[ShotIndex] >= SmartPistolGlobalNPC.TimeForLock)
                    {
                        Vector2 targetPos = n.boss ? n.Center + global.LockPositions[ShotIndex] : n.Center;
                        Vector2 vel = Projectile.velocity.SafeNormalize(Vector2.Zero);

                        ParticleRegistry.SpawnGlowParticle(Tip, Vector2.Zero, 8, Main.rand.NextFloat(.7f, 1.2f) * 50f,
                            Color.White.Lerp(Color.OrangeRed, .3f));
                        for (int j = 0; j < 15; j++)
                            ParticleRegistry.SpawnSparkParticle(Tip, vel.RotatedByRandom(.3f) * Main.rand.NextFloat(10f, 28f),
                                Main.rand.Next(19, 30), Main.rand.NextFloat(.7f, 1f), Color.Orange, true, true);

                        Projectile.CreateFriendlyExplosion(targetPos, Vector2.One, Projectile.damage, Projectile.knockBack, 2, 10);
                        SoundID.Item11.Play(Tip, 1.6f, .5f);
                        global.LockIn[ShotIndex] = 0;
                        Shots++;
                        StruckTargets.Add(n);
                    }

                    ShotIndex++;
                }

                // Move to the next NPC if all shots for this NPC are done or ammo is depleted
                if (ShotIndex >= maxPoints || Shots >= SmartPistolGlobalPlayer.MaxShots)
                {
                    NPCIndex++;
                    ShotIndex = 0; // Reset for the next NPC
                }
            }

            ShootTime++;
        }
    }

    public void SearchForTargets()
    {
        // Collect all eligible NPCs
        List<NPC> potentialTargets = [];
        foreach (NPC npc in Main.ActiveNPCs)
        {
            if (npc == null || !LaserTip.IsInFieldOfView(Projectile.rotation, npc.Center, MathHelper.PiOver2, SightDist)
                || !npc.CanHomeInto() || !Collision.CanHit(LaserTip, 2, 2, npc.Center, 1, 1) || StruckTargets.Contains(npc))
                continue;

            potentialTargets.Add(npc);
        }

        // Get the 12 closest NPCs
        List<NPC> newTargets = potentialTargets
            .OrderBy(n => Vector2.DistanceSquared(n.Center, Projectile.Center))
            .Take(12)
            .ToList();

        // Reset LockIn for NPCs that are no longer in Targets and initialize new boss targets
        foreach (NPC npc in Targets)
        {
            if (!newTargets.Contains(npc))
            {
                SmartPistolGlobalNPC global = npc.GetGlobalNPC<SmartPistolGlobalNPC>();
                for (int i = 0; i < (npc.boss ? 3 : 1); i++)
                    global.LockIn[i] = 0;
            }
        }

        foreach (NPC npc in newTargets)
        {
            if (!Targets.Contains(npc) && npc.boss)
            {
                npc.GetGlobalNPC<SmartPistolGlobalNPC>().InitializeBossLockPoints(npc);
            }
        }

        // Update Targets list
        Targets = newTargets;
    }

    public void UpdateTargets()
    {
        // Increment LockIn for valid targets
        foreach (NPC npc in Targets)
        {
            SmartPistolGlobalNPC global = npc.GetGlobalNPC<SmartPistolGlobalNPC>();
            int points = npc.boss ? 3 : 1;
            for (int i = 0; i < points; i++)
            {
                if (global.LockIn[i] < SmartPistolGlobalNPC.TimeForLock)
                    global.LockIn[i]++;
            }
        }
    }

    public override bool PreKill(int timeLeft)
    {
        foreach (NPC npc in Targets.Concat(StruckTargets).Distinct())
        {
            SmartPistolGlobalNPC global = npc.GetGlobalNPC<SmartPistolGlobalNPC>();
            for (int i = 0; i < (npc.boss ? 3 : 1); i++)
                global.LockIn[i] = 0;
        }
        Targets.Clear();
        StruckTargets.Clear();
        return base.PreKill(timeLeft);
    }

    public override bool PreDraw(ref Color lightColor)
    {
        void draw()
        {
            if (Reloading)
                return;

            foreach (NPC target in Targets)
            {
                if (StruckTargets.Contains(target))
                    continue;

                SmartPistolGlobalNPC global = target.GetGlobalNPC<SmartPistolGlobalNPC>();
                int c = target.boss ? 3 : 1;

                for (int i = 0; i < c; i++)
                {
                    Vector2 targetPos = target.boss ? target.Center + global.LockPositions[i] : target.Center;
                    float completion = InverseLerp(0f, SmartPistolGlobalNPC.TimeForLock, global.LockIn[i]);
                    Color col = Color.Lerp(Color.Yellow, Color.Red, completion);

                    OptimizedPrimitiveTrail trail = new(c => 2f, (c, pos) => col, null, 40);
                    TrailPoints points = new(40);
                    Vector2 mid = (LaserTip + targetPos) / 2;
                    Vector2 b = ClosestPointOnLineSegment(mid, LaserTip, LaserTip + Projectile.velocity * SightDist);
                    for (int j = 0; j < 40; j++)
                        points.SetPoint(j, QuadraticBezier(LaserTip, b, targetPos, InverseLerp(0f, 40, j)));
                    trail.DrawTrail(ShaderRegistry.StandardPrimitiveShader, points.Points, 200, true);
                }
            }
        }
        PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.OverNPCs);

        void reticle()
        {
            if (Reloading)
                return;

            foreach (NPC target in Targets)
            {
                if (StruckTargets.Contains(target))
                    continue;

                SmartPistolGlobalNPC global = target.GetGlobalNPC<SmartPistolGlobalNPC>();
                int points = target.boss ? 3 : 1;

                for (int i = 0; i < points; i++)
                {
                    Vector2 targetPos = target.boss ? target.Center + global.LockPositions[i] : target.Center;
                    float completion = InverseLerp(0f, SmartPistolGlobalNPC.TimeForLock, global.LockIn[i]);
                    Texture2D reticleTexture = AssetRegistry.GetTexture(AdditionsTexture.scope);
                    Vector2 position = targetPos - Main.screenPosition;
                    float rotation = Main.GlobalTimeWrappedHourly + target.whoAmI * .5f * (target.whoAmI % 2 == 0).ToDirectionInt() + i * 0.1f;
                    Vector2 origin = reticleTexture.Size() / 2;
                    Color color = Color.Lerp(Color.Yellow, Color.Red.Lerp(Color.Red * 1.6f, Sin01(Main.GlobalTimeWrappedHourly * 2)), completion) * completion;
                    float size = MathHelper.Lerp(50f, 30f, completion);
                    Main.spriteBatch.DrawBetterRect(reticleTexture, ToTarget(targetPos, Vector2.One * size), null, color, rotation, origin);
                }
            }
        }
        LayeredDrawSystem.QueueDrawAction(reticle, PixelationLayer.OverNPCs);

        Texture2D texture = Projectile.ThisProjectileTexture();
        Vector2 drawPosition = Projectile.Center - Main.screenPosition;
        SpriteEffects effects = Projectile.direction == -1 ? SpriteEffects.FlipVertically : SpriteEffects.None;
        Main.spriteBatch.Draw(texture, drawPosition, null, Projectile.GetAlpha(Color.White), Projectile.rotation, texture.Size() * 0.5f, Projectile.scale, effects, 0f);
        return false;
    }
}