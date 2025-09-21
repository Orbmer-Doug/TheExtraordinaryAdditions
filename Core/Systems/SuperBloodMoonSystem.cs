using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using SubworldLibrary;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.Achievements;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;
using static Terraria.Main;

namespace TheExtraordinaryAdditions.Core.Systems;

public class SuperBloodMoonSystem : ModSystem
{
    public static bool SuperBloodMoon = false;
    public static bool IsSuperBloodMoon => SuperBloodMoon == true && bloodMoon == true;
    public override void ClearWorld()
    {
        SuperBloodMoon = false;
    }

    public override void SaveWorldData(TagCompound tag)
    {
        List<string> list = new List<string>();
        if (SuperBloodMoon)
            list.Add(nameof(SuperBloodMoon));
        tag[nameof(SuperBloodMoonSystem)] = list;
    }

    public override void LoadWorldData(TagCompound tag)
    {
        IList<string> list = tag.GetList<string>(nameof(SuperBloodMoonSystem));
        SuperBloodMoon = list.Contains(nameof(SuperBloodMoon));
    }
}

public class IncreaseBloodMoonSpawnRate : ModSystem
{
    public override void Load()
    {
        On_Main.UpdateTime_StartNight += IncreaseBloodMoonRate;
        On_Main.DrawSunAndMoon += ModifyMoon;
    }
    public override void Unload()
    {
        On_Main.UpdateTime_StartNight -= IncreaseBloodMoonRate;
        On_Main.DrawSunAndMoon -= ModifyMoon;
    }

    public static void IncreaseBloodMoonRate(On_Main.orig_UpdateTime_StartNight orig, ref bool stopEvents)
    {
        orig(ref stopEvents);

        if (SuperBloodMoonSystem.SuperBloodMoon && !bloodMoon)
        {
            // Literally just run the check again
            if (!WorldGen.spawnEye && moonPhase != 4 && rand.NextBool(4) && netMode != NetmodeID.MultiplayerClient)
            {
                foreach (Player p in ActivePlayers)
                {
                    if (p.ConsumedLifeCrystals > 1)
                    {
                        bloodMoon = true;
                        break;
                    }
                }

                if (bloodMoon)
                {
                    sundialCooldown = 0;
                    moondialCooldown = 0;
                    AchievementsHelper.NotifyProgressionEvent(4);
                    DisplayText(Lang.misc[8].Value, new(50, byte.MaxValue, 130));
                }
            }
        }
    }

    public static void ModifyMoon(On_Main.orig_DrawSunAndMoon orig, Main self, SceneArea sceneArea, Color moonColor, Color sunColor, float tempMushroomInfluence)
    {
        if (SuperBloodMoonSystem.IsSuperBloodMoon && !gameMenu)
        {
            // Get drawing information
            int moonType = Main.moonType;
            if (!TextureAssets.Moon.IndexInRange(moonType))
                moonType = Utils.Clamp(moonType, 0, 8);

            Asset<Texture2D> moon = TextureAssets.Moon[moonType];
            float nightCompletion = (float)(time / nightLength);

            Rectangle frame = new(0, moon.Width() * moonPhase, moon.Width(), moon.Width());
            float moonRotation = nightCompletion * 2f - 7.3f;

            int moonX = (int)(nightCompletion * (double)(sceneArea.totalWidth + moon.Value.Width * 2)) - moon.Value.Width;
            double verticalOffset;
            int moonY;
            if (nightCompletion < .5f)
            {
                verticalOffset = Math.Pow(1.0 - nightCompletion * 2.0, 2.0);
                moonY = (int)(sceneArea.bgTopY + verticalOffset * 250.0 + 180.0);
            }
            else
            {
                verticalOffset = Math.Pow((nightCompletion - 0.5) * 2.0, 2.0);
                moonY = (int)(sceneArea.bgTopY + verticalOffset * 250.0 + 180.0);
            }
            float moonScale = (float)(1.2 - verticalOffset * 0.4) * ForcedMinimumZoom;

            Vector2 moonPos = new Vector2(moonX, moonY + moonModY) + sceneArea.SceneLocalScreenPositionOffset;

            // Draw a strong glow behind the moon
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, DefaultSamplerState, DepthStencilState.None, Rasterizer, null, Matrix.Identity);
            Texture2D bloom = AssetRegistry.GetTexture(AdditionsTexture.GlowParticleSmall);

            float intensity = Convert01To010(nightCompletion);
            spriteBatch.Draw(bloom, moonPos, null, Color.Crimson * .6f * intensity, 0f, bloom.Size() / 2, 1.2f, 0, 0f);
            spriteBatch.Draw(bloom, moonPos, null, Color.Red * .35f * intensity, 0f, bloom.Size() / 2, 2.5f, 0, 0f);
            spriteBatch.Draw(bloom, moonPos, null, Color.DarkRed * .2f * intensity, 0f, bloom.Size() / 2, 3.8f, 0, 0f);

            // Draw the distorted moon
            ManagedShader distort = ShaderRegistry.HeatDistortionShader;
            distort.SetTexture(moon.Value, 1, SamplerState.LinearWrap);
            distort.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.DendriticNoise), 2, SamplerState.LinearWrap);
            distort.TrySetParameter("globalTime", GlobalTimeWrappedHourly);
            distort.TrySetParameter("intensity", Convert01To010(nightCompletion) * 1.2f);
            distort.TrySetParameter("screenZoom", GameViewMatrix.Zoom);
            distort.TrySetParameter("mainColor", moonColor.Lerp(Color.Crimson, .3f).ToVector4());

            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, DefaultSamplerState, DepthStencilState.None, Rasterizer, distort.Effect, Matrix.Identity);
            distort.Render();
            spriteBatch.Draw(moon.Value, moonPos, frame, Color.White, moonRotation, frame.Size() / 2, moonScale, 0, 0f);
            spriteBatch.ExitShaderRegion();
        }
        else
            orig(self, sceneArea, moonColor, sunColor, tempMushroomInfluence);
    }
}

public class SuperBloodMoonGlobalNPC : GlobalNPC
{
    public override void EditSpawnRate(Player player, ref int spawnRate, ref int maxSpawns)
    {
        if (SuperBloodMoonSystem.IsSuperBloodMoon)
        {
            spawnRate = (int)((double)spawnRate * 0.13f);
            maxSpawns = (int)(maxSpawns * 3f);
        }
    }

    public override bool InstancePerEntity => true;
    public override void SetDefaults(NPC npc)
    {
        if (SuperBloodMoonSystem.IsSuperBloodMoon)
        {
            switch (npc.type)
            {
                case NPCID.Drippler:
                    npc.lifeMax = 510;
                    npc.defense = 20;
                    npc.knockBackResist = .45f;
                    break;
                case NPCID.BloodZombie:
                    npc.lifeMax = 540;
                    npc.defense = 22;
                    npc.knockBackResist = .75f;
                    break;
            }
        }
    }

    public static readonly HashSet<int> EyeList = [NPCID.CataractEye, NPCID.CataractEye2, NPCID.DemonEye, NPCID.DemonEye2, NPCID.DialatedEye,
    NPCID.DialatedEye2, NPCID.GreenEye, NPCID.GreenEye2, NPCID.PurpleEye, NPCID.PurpleEye2, NPCID.SleepyEye, NPCID.SleepyEye2];

    public override void AI(NPC npc)
    {
        if (!SuperBloodMoonSystem.IsSuperBloodMoon)
            return;

        // Periodic tears erupt from all demon eyes as long as a player is in their sight
        if (EyeList.Contains(npc.type))
        {
            ref float timer = ref npc.ai[2];
            if (!npc.HasValidTarget)
                return;

            Entity target = npc.GetTarget();

            float rot = npc.velocity.ToRotation();
            const int sight = 700;
            Vector2 tip = npc.Center + PolarVector(npc.width / 2, rot);

            int wait = DifficultyBasedValue(40, 35, 30, 25, 23, 20);
            if (timer % wait == (wait - 1) && tip.IsInFieldOfView(rot, target.Center, .55f, sight))
            {
                Vector2 vel = npc.Center.SafeDirectionTo(target.Center) * 7;
                npc.Shoot(tip, vel, ModContent.ProjectileType<VermillionTear>(), npc.damage / 2, 2f);
            }

            timer++;
        }

        // Give some more cthulhu into the wandering eyes
        if (npc.type == NPCID.WanderingEye)
        {
            NPCAimedTarget target = npc.GetTargetData();

            ref float state = ref npc.ai[1];
            float lifeRatio = InverseLerp(0f, npc.lifeMax, npc.life);
            if (npc.WithinRange(target.Center, 600f) && state == 0f && lifeRatio <= 0.5)
                state = 1f;

            bool shouldCharge = state > 0f;

            ref float timer = ref npc.ai[2];

            if (shouldCharge)
            {
                bool superFast = false;
                if (expertMode && lifeRatio < 0.06f)
                    superFast = true;

                float totalTime = superFast ? 10f : 20f;
                switch (state)
                {
                    // Start dash
                    case 1f:
                        if (netMode != NetmodeID.MultiplayerClient)
                        {
                            npc.TargetClosest();
                            float speed = 11f;
                            float posX = target.Position.X + target.Width / 2 - npc.Center.X;
                            float posY = target.Position.Y + target.Height / 2 - npc.Center.Y;
                            float targetVel = Math.Abs(target.Velocity.X) + Math.Abs(target.Velocity.Y) / 4f;
                            targetVel += 10f - targetVel;
                            if (targetVel < 5f)
                                targetVel = 5f;

                            if (targetVel > 15f)
                                targetVel = 15f;

                            if (timer == -1f && !superFast)
                            {
                                targetVel *= 4f;
                                speed *= 1.3f;
                            }

                            if (superFast)
                                targetVel *= 2f;

                            posX -= target.Velocity.X * targetVel;
                            posY -= target.Velocity.Y * targetVel / 4f;
                            posX *= 1f + rand.NextFloat(-.1f, .11f);
                            posY *= 1f + rand.NextFloat(-.1f, .11f);
                            if (superFast)
                            {
                                posX *= 1f + rand.NextFloat(-.1f, .11f);
                                posY *= 1f + rand.NextFloat(-.1f, .11f);
                            }

                            float circle = (float)Math.Sqrt(posX * posX + posY * posY);
                            float prevCircle = circle;
                            circle = speed / circle;
                            npc.velocity.X = posX * circle;
                            npc.velocity.Y = posY * circle;
                            npc.velocity.X += rand.NextFloat(-2f, 2.1f);
                            npc.velocity.Y += rand.NextFloat(-2f, 2.1f);
                            if (superFast)
                            {
                                npc.velocity.X += rand.NextFloat(-5f, 5.1f);
                                npc.velocity.Y += rand.NextFloat(-5f, 5.1f);
                                float absX = Math.Abs(npc.velocity.X);
                                float absY = Math.Abs(npc.velocity.Y);
                                if (npc.Center.X > target.Center.X)
                                    absY *= -1f;

                                if (npc.Center.Y > target.Center.Y)
                                    absX *= -1f;

                                npc.velocity.X = absY + npc.velocity.X;
                                npc.velocity.Y = absX + npc.velocity.Y;
                                npc.velocity.Normalize();
                                npc.velocity *= speed;
                                npc.velocity.X += rand.NextFloat(-2f, 2.1f);
                                npc.velocity.Y += rand.NextFloat(-2f, 2.1f);
                            }
                            else if (prevCircle < 100f)
                            {
                                if (Math.Abs(npc.velocity.X) > Math.Abs(npc.velocity.Y))
                                {
                                    float absX = Math.Abs(npc.velocity.X);
                                    float absY = Math.Abs(npc.velocity.Y);
                                    if (npc.Center.X > target.Center.X)
                                        absY *= -1f;

                                    if (npc.Center.Y > target.Center.Y)
                                        absX *= -1f;

                                    npc.velocity.X = absY;
                                    npc.velocity.Y = absX;
                                }
                            }
                            else if (Math.Abs(npc.velocity.X) > Math.Abs(npc.velocity.Y))
                            {
                                float yVel = (Math.Abs(npc.velocity.X) + Math.Abs(npc.velocity.Y)) / 2f;
                                float prevYVel = yVel;
                                if (npc.Center.X > target.Center.X)
                                    prevYVel *= -1f;

                                if (npc.Center.Y > target.Center.Y)
                                    yVel *= -1f;

                                npc.velocity.X = prevYVel;
                                npc.velocity.Y = yVel;
                            }

                            state = 2f;
                            npc.netUpdate = true;
                            if (npc.netSpam > 10)
                                npc.netSpam = 10;
                        }
                        break;

                    // Slow down
                    case 2f:
                        if (timer == 0f)
                        {
                            for (int i = 0; i < 20; i++)
                            {
                                ParticleRegistry.SpawnGlowParticle(npc.RotHitbox().RandomPoint(), npc.velocity * rand.NextFloat(.2f, .8f),
                                    rand.Next(20, 30), rand.NextFloat(10f, 20f), Color.Crimson.Lerp(Color.Red, rand.NextFloat(.2f, .5f)), 1f);
                            }

                            SoundID.DD2_WyvernDiveDown.Play(npc.position, .7f, .2f, .1f, null, 20);
                        }

                        timer++;

                        if (timer == totalTime && Vector2.Distance(npc.position, target.Position) < 200f)
                            timer -= .5f;

                        if (timer >= totalTime)
                        {
                            npc.velocity *= 0.95f;
                            if (npc.velocity.X > -0.1 && npc.velocity.X < 0.1)
                                npc.velocity.X = 0f;

                            if (npc.velocity.Y > -0.1 && npc.velocity.Y < 0.1)
                                npc.velocity.Y = 0f;
                        }
                        else
                        {
                            if (rand.NextBool(3))
                                ParticleRegistry.SpawnSparkParticle(npc.RotHitbox().RandomPoint(), -npc.velocity * rand.NextFloat(.2f, .4f), rand.Next(12, 20), rand.NextFloat(.3f, .5f), Color.Red);
                            npc.rotation = npc.velocity.ToRotation() - MathHelper.PiOver2;
                        }

                        float wait = totalTime + 7f;
                        if (timer >= wait)
                        {
                            npc.netUpdate = true;
                            if (npc.netSpam > 10)
                                npc.netSpam = 10;

                            state = 0f;
                            timer = 0f;
                        }
                        break;
                }
            }
        }
    }

    public override void OnKill(NPC npc)
    {
        if (SuperBloodMoonSystem.IsSuperBloodMoon)
        {
            switch (npc.type)
            {
                case NPCID.Drippler:
                    ParticleRegistry.SpawnDetailedBlastParticle(npc.Center, Vector2.Zero, npc.Size * 1.2f, Vector2.Zero, 30, Color.Crimson);
                    for (int i = 0; i < rand.Next(2, 3); i++)
                    {
                        Vector2 pos = npc.RotHitbox().RandomPoint();
                        Vector2 vel = rand.NextVector2Circular(8f, 8f);
                        int type = rand.NextFromSet(EyeList);
                        if (halloween)
                            type = rand.NextFromList(NPCID.DemonEyeSpaceship, NPCID.DemonTaxCollector);

                        npc.NewNPCBetter(pos, vel, type, 0, 0f, 0f, 0f, 0f, npc.target);
                        ParticleRegistry.SpawnBloodStreakParticle(pos, vel.SafeNormalize(Vector2.Zero), 30, rand.NextFloat(.5f, .6f), Color.DarkRed);
                    }

                    break;
            }
        }
    }
}

public class JumpingFighterAI : GlobalNPC
{
    private float prevDistance;

    public override bool InstancePerEntity => true;

    public override bool AppliesToEntity(NPC npc, bool lateInstantiation)
        => lateInstantiation && npc.aiStyle == NPCAIStyleID.Fighter;

    public override void AI(NPC npc)
    {
        if (!SuperBloodMoonSystem.IsSuperBloodMoon)
            return;

        if (!npc.HasValidTarget)
            return;

        Entity target = npc.GetTarget();

        if (target == null)
            return;

        Vector2 npcCenter = npc.Center;
        Vector2 targetCenter = target.Center;

        // Dont leap backwards
        if (npc.direction != ((targetCenter.X - npcCenter.X) >= 0f ? 1 : -1))
            return;

        float distance = Vector2.Distance(targetCenter, npcCenter);

        if (npc.velocity.Y == 0f && distance <= 80f && prevDistance > 80f)
        {
            npc.velocity.X = 6.5f * npc.direction;
            npc.velocity.Y = -4.2f;
            for (int i = 0; i < 20; i++)
            {
                ParticleRegistry.SpawnGlowParticle(npc.RotHitbox().RandomPoint(), npc.velocity * rand.NextFloat(.4f, 1f),
                    rand.Next(20, 40), rand.NextFloat(12f, 30f), Color.DarkRed);
            }

            if (!dedServ)
                npc.IdleSounds();
        }

        prevDistance = distance;
    }
}

public class VermillionTear : ModProjectile
{
    public override string Texture => AssetRegistry.Invis;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 16;
        Projectile.friendly = false;
        Projectile.hostile = true;
        Projectile.tileCollide = true;
        Projectile.ignoreWater = false;
        Projectile.penetrate = 1;
        Projectile.MaxUpdates = 3;
    }

    public ref float Time => ref Projectile.ai[0];
    public override void AI()
    {
        if (Time == 0f)
        {
            ParticleRegistry.SpawnPulseRingParticle(Projectile.Center, Projectile.velocity.SafeNormalize(Vector2.Zero) * 3f, 30, Projectile.velocity.ToRotation(), new(.3f, 1.1f), 0f, 50f, Color.Crimson);
            for (int i = 0; i < 8; i++)
            {
                ParticleRegistry.SpawnSparkleParticle(Projectile.Center, Projectile.velocity * rand.NextFloat(.1f, .3f), rand.Next(20, 30), rand.NextFloat(.4f, .8f),
                    Color.Crimson, Color.DarkRed, .7f, rand.NextFloat(-.1f, .1f));
            }
        }
        after ??= new(30, () => Projectile.Center);

        float squish = MathHelper.Clamp(Projectile.velocity.Length() / 5f, 1f, 2f);
        after.UpdateFancyAfterimages(new(Projectile.Center, new Vector2(1f, .4f * squish) * Projectile.Opacity * 60f, Projectile.Opacity, Projectile.rotation, 0, 90, 0, 0f, null, true, -.1f));

        if (rand.NextBool(9))
            ParticleRegistry.SpawnBloomPixelParticle(Projectile.RotHitbox().RandomPoint(), -Projectile.velocity * rand.NextFloat(.2f, .5f), rand.Next(20, 30), rand.NextFloat(.2f, .5f), Color.Red, Color.Crimson, null, 1.2f, 3);

        Projectile.velocity *= .98f;
        Projectile.Opacity = InverseLerp(0f, 12f, Time) * InverseLerp(0f, 1f, Projectile.velocity.Length());
        Projectile.rotation = Projectile.velocity.ToRotation();
        if (Time > 12f && Projectile.velocity.Length() <= .05f)
            Projectile.Kill();

        Time++;
    }

    public FancyAfterimages after;
    public override bool PreDraw(ref Color lightColor)
    {
        void glow()
        {
            Texture2D soft = AssetRegistry.GetTexture(AdditionsTexture.GlowSoft);
            spriteBatch.DrawBetterRect(soft, ToTarget(Projectile.Center, Projectile.Size * 2.5f), null, Color.Crimson * Projectile.Opacity, 0f, soft.Size() / 2);

            Texture2D tex = AssetRegistry.GetTexture(AdditionsTexture.LensStar);
            after.DrawFancyAfterimages(tex, [Color.Crimson, Color.Red, Color.DarkRed], Projectile.Opacity, 1f, 0f, true);
        }
        PixelationSystem.QueueTextureRenderAction(glow, PixelationLayer.UnderProjectiles, BlendState.Additive);

        return false;
    }
}