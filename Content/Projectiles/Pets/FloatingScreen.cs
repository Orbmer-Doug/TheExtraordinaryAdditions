using Microsoft.Xna.Framework.Graphics;
using SubworldLibrary;
using System;
using System.Reflection;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Buffs.Summon;
using TheExtraordinaryAdditions.Content.NPCs.Misc;
using TheExtraordinaryAdditions.Content.World.Subworlds;
using TheExtraordinaryAdditions.Core.CrossCompatibility;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using static TheExtraordinaryAdditions.Core.Graphics.ManagedRenderTarget;

namespace TheExtraordinaryAdditions.Content.Projectiles.Pets;

public class FloatingScreen : ModProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.AsterlinFacingForward);
    public Player Owner => Main.player[Projectile.owner];
    public GlobalPlayer Modded => Owner.Additions();
    public ref float Time => ref Projectile.ai[0];

    public override void SetStaticDefaults()
    {
        Main.projPet[Projectile.type] = true;
        ProjectileID.Sets.CanDistortWater[Type] = false;
    }

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 1;
        Projectile.friendly = Projectile.netImportant = true;
        Projectile.penetrate = -1;
        Projectile.timeLeft *= 5;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
    }

    public override void AI()
    {
        if (Owner.Available() && Owner.HasBuff(ModContent.BuffType<JudgingAsterlin>()))
            Projectile.timeLeft = 2;

        float dist = Projectile.Center.Distance(Owner.Center);
        Vector2 idealPosition = Owner.MountedCenter - Vector2.UnitY * 120f;
        Vector2 goalVelocity = (idealPosition - Projectile.Center) * 0.06f;

        float approachAcceleration = 0.1f + MathF.Pow(InverseLerp(70, 0, dist), 2f) * 0.3f;
        Projectile.velocity = Vector2.Lerp(Projectile.velocity, goalVelocity, approachAcceleration);
        Projectile.velocity *= 0.98f;

        if (!Projectile.Center.WithinRange(Owner.Center, 5000f))
            Projectile.Center = Owner.Center;

        Time++;
    }

    public override bool PreDraw(ref Color lightColor) => false;
}

public class FloatingScreenManager : ModSystem
{
    private ManagedRenderTarget crtTarget;
    private ManagedShader crtShader;
    private static readonly RenderTargetInitializationAction TargetInitializer = (width, height) => new RenderTarget2D(Main.instance.GraphicsDevice, width, height);

    public override void Load()
    {
        if (Main.netMode == NetmodeID.Server)
            return;

        Main.QueueMainThreadAction(() =>
        {
            crtTarget = new ManagedRenderTarget(true, TargetInitializer, true);

            // Initialize target
            GraphicsDevice device = Main.instance.GraphicsDevice;
            device.SetRenderTarget(crtTarget);
            device.Clear(Color.Transparent);
            device.SetRenderTarget(null);
        });

        RenderTargetManager.RenderTargetUpdateLoopEvent += DrawToTarget;
        On_Main.DrawProjectiles += DrawTheTarget;
    }

    public override void Unload()
    {
        Main.QueueMainThreadAction(() =>
        {
            crtTarget?.Dispose();
            crtTarget = null;
        });

        RenderTargetManager.RenderTargetUpdateLoopEvent -= DrawToTarget;
        On_Main.DrawProjectiles -= DrawTheTarget;
    }

    // LUCILLE WHERE IS   THE   MOD   CALLS
    private bool InEternalGarden()
    {
        if (ModLoader.TryGetMod("NoxusBoss", out Mod knocksus))
        {
            Type eternalGardenUpdateSystem = knocksus.Code?.GetType("NoxusBoss.Core.World.Subworlds.EternalGardenUpdateSystem");
            if (eternalGardenUpdateSystem == null)
                return false;
            PropertyInfo eternalProperty = eternalGardenUpdateSystem.GetProperty("WasInSubworldLastUpdateFrame", BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
            if (eternalProperty == null)
                return false;

            return (bool)eternalProperty.GetValue(null);
        }
        return false;
    }
    private bool HiAvatar()
    {
        if (ModLoader.TryGetMod("NoxusBoss", out Mod knocksus) && knocksus.TryFind<ModNPC>("AvatarOfEmptiness", out ModNPC npc))
        {
            return NPC.AnyNPCs(npc.Type);
        }
        return false;
    }

    private void DrawToTarget()
    {
        if (!AssetRegistry.HasFinishedLoading || Main.gameMenu || Main.netMode == NetmodeID.Server)
            return;

        GraphicsDevice device = Main.instance.GraphicsDevice;
        Vector2 resolution = new(Main.screenWidth, Main.screenHeight);

        device.SetRenderTarget(crtTarget);
        device.Clear(Color.Transparent);

        Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, Main.Rasterizer, null, Matrix.Identity);

        foreach (Projectile projectile in Main.ActiveProjectiles)
        {
            if (projectile == null || projectile.type != ModContent.ProjectileType<FloatingScreen>())
                continue;
            FloatingScreen screen = projectile.As<FloatingScreen>();
            Player player = screen.Owner;
            bool inSulphur = false;
            bool inAstral = false; // Astral Infection credit: https://www.deviantart.com/mp-24/art/The-Astral-Infection-873382367
            bool inCrags = false;
            bool inSunkenSea = false;
            bool inAbyss = false;
            if (ModReferences.BaseCalamity != null)
            {
                inSulphur = (bool)ModReferences.BaseCalamity.Call("GetInZone", player, "sulfur");
                inAstral = (bool)ModReferences.BaseCalamity.Call("GetInZone", player, "astral");
                inCrags = (bool)ModReferences.BaseCalamity.Call("GetInZone", player, "crag");
                inSunkenSea = (bool)ModReferences.BaseCalamity.Call("GetInZone", player, "sunkensea");
                inAbyss = (bool)ModReferences.BaseCalamity.Call("GetInZone", player, "abyss");
            }
            bool inCrater = SubworldSystem.IsActive<CloudedCrater>();
            bool avatar = HiAvatar();
            bool garden = InEternalGarden();

            Texture2D background = AssetRegistry.GetTexture(AdditionsTexture.Background_Purity);
            Color color = Color.White;

            // Get the correct background based on whats happening and where the player is at
            // A lot of this is calculations from how the full map chooses its background

            if (!avatar)
            {
                int wall = Main.tile[(int)(player.Center.X / 16f), (int)(player.Center.Y / 16f)].wall;
                if (inCrater)
                    background = AssetRegistry.GetTexture(AdditionsTexture.Background_CloudedCrater);
                else if (garden)
                    background = AssetRegistry.GetTexture(AdditionsTexture.Background_EternalGarden);
                else if (inCrags)
                    background = AssetRegistry.GetTexture(AdditionsTexture.Background_Brimstone);
                else if (inAstral)
                    background = AssetRegistry.GetTexture(AdditionsTexture.Background_AstralInfection);
                else if (inSunkenSea)
                    background = AssetRegistry.GetTexture(AdditionsTexture.Background_SunkenSea);
                else if (inAbyss)
                {
                    background = AssetRegistry.GetTexture(AdditionsTexture.Pixel);
                    color = Color.Black;
                }
                else if (Main.screenPosition.Y > (float)((Main.maxTilesY - 232) * 16))
                    background = AssetRegistry.GetTexture(AdditionsTexture.Background_Underworld);
                else if (player.ZoneDungeon)
                    background = AssetRegistry.GetTexture(AdditionsTexture.Background_Dungeon);
                else if (wall == WallID.LihzahrdBrickUnsafe)
                    background = AssetRegistry.GetTexture(AdditionsTexture.Background_JungleTemple);
                else if (player.ZoneShimmer)
                    background = AssetRegistry.GetTexture(AdditionsTexture.Background_Space);
                else if ((double)Main.screenPosition.Y > Main.worldSurface * 16.0)
                {
                    switch (wall)
                    {
                        case WallID.HiveUnsafe:
                        case WallID.Hive:
                            background = AssetRegistry.GetTexture(AdditionsTexture.Background_BEES);
                            break;
                        case WallID.GraniteUnsafe:
                        case WallID.Granite:
                            background = AssetRegistry.GetTexture(AdditionsTexture.Background_NotPurpleGranite);
                            break;
                        case WallID.MarbleUnsafe:
                        case WallID.Marble:
                            background = AssetRegistry.GetTexture(AdditionsTexture.Background_Marble);
                            break;
                        case WallID.SpiderUnsafe:
                        case WallID.Spider:
                            background = AssetRegistry.GetTexture(AdditionsTexture.Background_SpiderNest);
                            break;
                        default:
                            // vanilla shenanigans
                            background = player.ZoneGemCave ? AssetRegistry.GetTexture(AdditionsTexture.Background_GemCave) : (player.ZoneGlowshroom ? AssetRegistry.GetTexture(AdditionsTexture.Background_GlowingShrooms)
                                : (player.ZoneCorrupt ? (player.ZoneDesert ? AssetRegistry.GetTexture(AdditionsTexture.Background_Corruption)
                                : ((!player.ZoneSnow) ? AssetRegistry.GetTexture(AdditionsTexture.Background_Corruption) : AssetRegistry.GetTexture(AdditionsTexture.Background_Corruption)))
                                : (player.ZoneCrimson ? (player.ZoneDesert ? AssetRegistry.GetTexture(AdditionsTexture.Background_Crimson)
                                : ((!player.ZoneSnow) ? AssetRegistry.GetTexture(AdditionsTexture.Background_Crimson) : AssetRegistry.GetTexture(AdditionsTexture.Background_Crimson)))
                                : (player.ZoneHallow ? (player.ZoneDesert ? AssetRegistry.GetTexture(AdditionsTexture.Background_Hallow)
                                : ((!player.ZoneSnow) ? AssetRegistry.GetTexture(AdditionsTexture.Background_Hallow) : AssetRegistry.GetTexture(AdditionsTexture.Background_Hallow)))
                                : (player.ZoneSnow ? AssetRegistry.GetTexture(AdditionsTexture.Background_Snow) : (player.ZoneJungle ? AssetRegistry.GetTexture(AdditionsTexture.Background_Jungle)
                                : (player.ZoneDesert ? AssetRegistry.GetTexture(AdditionsTexture.Background_Desert)
                                : ((!player.ZoneRockLayerHeight) ? AssetRegistry.GetTexture(AdditionsTexture.Background_Undergound) : AssetRegistry.GetTexture(AdditionsTexture.Background_Cavern)))))))));
                            break;
                    }
                }

                else if (player.ZoneTowerSolar)
                    background = AssetRegistry.GetTexture(AdditionsTexture.Background_SolarPillar);
                else if (player.ZoneTowerVortex)
                    background = AssetRegistry.GetTexture(AdditionsTexture.Background_VortexPillar);
                else if (player.ZoneTowerNebula)
                    background = AssetRegistry.GetTexture(AdditionsTexture.Background_NebularPillar);
                else if (player.ZoneTowerStardust)
                    background = AssetRegistry.GetTexture(AdditionsTexture.Background_StardustPillar);
                else if (Main.invasionType == InvasionID.GoblinArmy)
                    background = AssetRegistry.GetTexture(AdditionsTexture.Background_Goblin);
                else if (Main.invasionType == InvasionID.PirateInvasion)
                    background = AssetRegistry.GetTexture(AdditionsTexture.Background_Pirates);
                else if (Main.invasionType == InvasionID.MartianMadness)
                    background = AssetRegistry.GetTexture(AdditionsTexture.Background_Martian);
                else if (player.ZoneOldOneArmy)
                    background = AssetRegistry.GetTexture(AdditionsTexture.Background_OldOnesArmy);
                else if (Main.snowMoon)
                    background = AssetRegistry.GetTexture(AdditionsTexture.Background_FrostMoon);
                else if (Main.pumpkinMoon)
                    background = AssetRegistry.GetTexture(AdditionsTexture.Background_PumpkinMoon);

                else if (player.ZoneGlowshroom)
                    background = AssetRegistry.GetTexture(AdditionsTexture.Background_GlowingShrooms);
                else
                {
                    int centerTile = (int)((Main.screenPosition.X + (float)(Main.screenWidth / 2)) / 16f);
                    if (player.ZoneSkyHeight)
                        background = AssetRegistry.GetTexture(AdditionsTexture.Background_Space);
                    else if (player.ZoneCorrupt)
                        background = AssetRegistry.GetTexture(AdditionsTexture.Background_Corruption);
                    else if (player.ZoneCrimson)
                        background = AssetRegistry.GetTexture(AdditionsTexture.Background_Crimson);
                    else if (player.ZoneHallow)
                        background = AssetRegistry.GetTexture(AdditionsTexture.Background_Hallow);
                    else if (inSulphur)
                        background = AssetRegistry.GetTexture(AdditionsTexture.Background_Sulphur);
                    else if ((double)(Main.screenPosition.Y / 16f) < Main.worldSurface + 10.0 && (centerTile < 380 || centerTile > Main.maxTilesX - 380))
                        background = AssetRegistry.GetTexture(AdditionsTexture.Background_Ocean);
                    else if (player.ZoneSnow)
                        background = AssetRegistry.GetTexture(AdditionsTexture.Background_Snow);
                    else if (player.ZoneJungle)
                        background = AssetRegistry.GetTexture(AdditionsTexture.Background_Jungle);
                    else if (player.ZoneSandstorm)
                        background = AssetRegistry.GetTexture(AdditionsTexture.Background_Sandstorm);
                    else if (player.ZoneDesert)
                        background = AssetRegistry.GetTexture(AdditionsTexture.Background_Desert);
                    else if (Main.bloodMoon)
                        background = AssetRegistry.GetTexture(AdditionsTexture.Background_BloodMoon);
                    else if (Main.eclipse)
                        background = AssetRegistry.GetTexture(AdditionsTexture.Background_SolarEclipse);
                    else if (player.ZoneGraveyard)
                        background = AssetRegistry.GetTexture(AdditionsTexture.Background_Graveyard);
                    else if (player.ZoneMeteor)
                        background = AssetRegistry.GetTexture(AdditionsTexture.Background_Meteor);
                    else if (player.ZoneRain)
                        background = AssetRegistry.GetTexture(AdditionsTexture.Background_Rain);
                }
                if (NPC.AnyNPCs(ModContent.NPCType<TheGiantSnailFromAncientTimes>()))
                    background = AssetRegistry.GetTexture(AdditionsTexture.Background_Snail);
                Main.spriteBatch.DrawBetterRect(background, ToTarget(Main.screenPosition, resolution), null, color, 0f, Vector2.Zero, SpriteEffects.None, false);
            }
            else
            {
                background = AssetRegistry.GetTexture(AdditionsTexture.Pixel);
                Vector2 size = new(resolution.X / 2f, resolution.Y);

                // Similar to the item rarities of avatars drops
                Color a = new Color(255, 0, 0);
                Color b = new Color(105, 255, 255);
                float baseInterpolant = Cos01(Main.GlobalTimeWrappedHourly * 2.1f);
                float colorInterpolant = Animators.MakePoly(3f).InOutFunction(baseInterpolant);

                Main.spriteBatch.DrawBetterRect(background, ToTarget(Main.screenPosition + Vector2.UnitX * resolution.X / 2f, size), null, Color.Lerp(a, b, 1f - colorInterpolant), 0f, Vector2.Zero, SpriteEffects.None, false);
                Main.spriteBatch.DrawBetterRect(background, ToTarget(Main.screenPosition, size), null, Color.Lerp(a, b, colorInterpolant), 0f, Vector2.Zero, SpriteEffects.None, false);
            }

            Texture2D asterlin = AssetRegistry.GetTexture(AdditionsTexture.AsterlinFacingForward);
            Main.spriteBatch.Draw(asterlin, new Vector2(Main.screenWidth / 2f, Main.screenHeight) - projectile.velocity * 4f, null, Color.White, 0f, asterlin.Size() / 2f, 6f, 0, 0f);
        }

        Main.spriteBatch.End();
    }

    private void DrawTheTarget(On_Main.orig_DrawProjectiles orig, Main self)
    {
        orig(self);

        if (!AssetRegistry.HasFinishedLoading || Main.gameMenu || Main.netMode == NetmodeID.Server)
            return;

        Vector2 res = new Vector2(300f, 200f);
        crtShader = AssetRegistry.GetShader("AsterlinScreen");
        crtShader.TrySetParameter("time", Main.GlobalTimeWrappedHourly);
        crtShader.TrySetParameter("findingChannel", false);
        crtShader.TrySetParameter("resolution", res);

        Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, Main.Rasterizer, crtShader.Effect, Main.GameViewMatrix.TransformationMatrix);
        foreach (Projectile projectile in Main.ActiveProjectiles)
        {
            if (projectile == null || projectile.type != ModContent.ProjectileType<FloatingScreen>())
                continue;
            FloatingScreen screen = projectile.As<FloatingScreen>();
            Player player = screen.Owner;

            Main.spriteBatch.DrawBetterRect(crtTarget, ToTarget(projectile.Center, res), null, Color.White, 0f, crtTarget.Size() / 2f, SpriteEffects.None, false);
        }
        Main.spriteBatch.End();
    }
}