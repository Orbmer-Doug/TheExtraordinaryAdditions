using CalamityMod.Projectiles.Melee;
using CalamityMod.Projectiles.Ranged;
using SubworldLibrary;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Events;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Items.Tools;
using TheExtraordinaryAdditions.Content.NPCs.Bosses.Crater;
using TheExtraordinaryAdditions.Content.Projectiles.Misc;
using TheExtraordinaryAdditions.Content.Tiles;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Globals.ItemGlobal;
using TheExtraordinaryAdditions.Core.Globals.ProjectileGlobal;
using TheExtraordinaryAdditions.Core.Netcode;
using TheExtraordinaryAdditions.Core.Systems;
using static TheExtraordinaryAdditions.Content.World.Subworlds.CloudedCrater;

namespace TheExtraordinaryAdditions.Content.World.Subworlds;

public class CloudedCraterUpdateSystem : ModSystem
{
    public static bool WasInSubworldLastUpdateFrame { get; private set; }

    public override void OnModLoad()
    {
        AdditionsGlobalItem.CanUseItemEvent += DisableCelestialSigil;
        AdditionsGlobalItem.CanUseItemEvent += DisableProblematicItems;
        AdditionsGlobalProjectile.PreAIEvent += KillProblematicProjectiles;
        AdditionsGlobalTile.NearbyEffectsEvent += ObliterateTom;
        AdditionsGlobalTile.IsTileUnbreakableEvent += DisallowTileBreakage;
        AdditionsGlobalWall.IsWallUnbreakableEvent += DisallowWallBreakage;
    }

    private bool DisableCelestialSigil(Item item, Player player)
    {
        if (!WasInSubworldLastUpdateFrame)
            return true;

        return item.type != ItemID.CelestialSigil;
    }

    private bool DisableProblematicItems(Item item, Player player)
    {
        if (!WasInSubworldLastUpdateFrame)
            return true;

        // Disable liquid placing/removing items
        int itemID = item.type;
        bool isSponge = itemID is ItemID.SuperAbsorbantSponge or ItemID.LavaAbsorbantSponge or ItemID.HoneyAbsorbantSponge or ItemID.UltraAbsorbantSponge;
        bool isRegularBucket = itemID is ItemID.EmptyBucket or ItemID.WaterBucket or ItemID.LavaBucket or ItemID.HoneyBucket;
        bool isSpecialBucket = itemID is ItemID.BottomlessBucket or ItemID.BottomlessLavaBucket or ItemID.BottomlessHoneyBucket or ItemID.BottomlessShimmerBucket;
        return !isSponge && !isRegularBucket && !isSpecialBucket ||
               itemID == ModContent.ItemType<MatterDisintegrationDrill>();
    }

    private bool KillProblematicProjectiles(Projectile projectile)
    {
        // Dont do anything if this event is called outside of the crater
        if (!WasInSubworldLastUpdateFrame)
            return true;

        switch (projectile.type)
        {
            case ProjectileID.DD2ElderWins:
                projectile.active = false;
                return false;
            // no tombs
            case ProjectileID.Tombstone or ProjectileID.Gravestone or ProjectileID.RichGravestone1
                or ProjectileID.RichGravestone2 or
                ProjectileID.RichGravestone3 or ProjectileID.RichGravestone4
                or ProjectileID.Headstone or ProjectileID.Obelisk or
                ProjectileID.GraveMarker or ProjectileID.CrossGraveMarker:
                projectile.active = false;
                break;
        }

        // Prevent tile-manipulating items from working messing up tiles
        if (projectile.type == ModContent.ProjectileType<CannonHoldout>())
            projectile.active = false;
        if (projectile.type == ModContent.ProjectileType<CrystylCrusherRay>())
            projectile.active = false;
        switch (projectile.type)
        {
            case ProjectileID.DirtBomb or ProjectileID.DirtStickyBomb:
            case ProjectileID.SandBallGun:
            case ProjectileID.SandBallFalling or ProjectileID.PearlSandBallFalling:
            case ProjectileID.EbonsandBallFalling or ProjectileID.EbonsandBallGun:
            case ProjectileID.CrimsandBallFalling or ProjectileID.CrimsandBallGun:
                projectile.active = false;
                break;
            // dirt rod
            case ProjectileID.DirtBall:
                projectile.Kill();
                break;
        }

        // No explosives
        bool dryRocket = projectile.type is ProjectileID.DryRocket or ProjectileID.DrySnowmanRocket;
        bool wetRocket = projectile.type is ProjectileID.WetRocket or ProjectileID.WetSnowmanRocket;
        bool honeyRocket = projectile.type is ProjectileID.HoneyRocket or ProjectileID.HoneySnowmanRocket;
        bool lavaRocket = projectile.type is ProjectileID.LavaRocket or ProjectileID.LavaSnowmanRocket;
        bool rocket = dryRocket || wetRocket || honeyRocket || lavaRocket ||
                      projectile.type == ModContent.ProjectileType<MortarRoundProj>() ||
                      projectile.type == ModContent.ProjectileType<RubberMortarRoundProj>();

        bool dryMisc = projectile.type is ProjectileID.DryGrenade or ProjectileID.DryMine;
        bool wetMisc = projectile.type is ProjectileID.WetGrenade or ProjectileID.WetMine;
        bool honeyMisc = projectile.type is ProjectileID.HoneyGrenade or ProjectileID.HoneyMine;
        bool lavaMisc = projectile.type is ProjectileID.LavaGrenade or ProjectileID.LavaMine;
        bool miscExplosive = dryMisc || wetMisc || honeyMisc || lavaMisc;

        if (rocket || miscExplosive)
            projectile.active = false;

        return true;
    }

    private void ObliterateTom(int x, int y, int type, bool closer)
    {
        if (!WasInSubworldLastUpdateFrame)
            return;

        // Erase tombstones
        if (type == TileID.Tombstones)
            Main.tile[x, y].Get<TileWallWireStateData>().HasTile = false;
    }
    
    private bool DisallowTileBreakage(int x, int y, int type) => WasInSubworldLastUpdateFrame;
    private bool DisallowWallBreakage(int x, int y, int type) => WasInSubworldLastUpdateFrame;
    

    public override void PreUpdateEntities()
    {
        // Check whether things are in the subworld
        bool inCrater = SubworldSystem.IsActive<CloudedCrater>();
        if (WasInSubworldLastUpdateFrame != inCrater)
        {
            if (inCrater)
            {
                if (Main.netMode != NetmodeID.Server)
                    LoadWorldDataFromTag("Client", ClientWorldDataTag);
            }

            PlayerEnterEffects();

            WasInSubworldLastUpdateFrame = inCrater;
        }

        // Everything beyond this point applies only to the subworld
        if (!WasInSubworldLastUpdateFrame)
            return;

        SubworldSpecificUpdateBehaviors();
    }

    public override void PreUpdateTime()
    {
        if (!WasInSubworldLastUpdateFrame)
            return;

        // Does time not normally update in subworlds????
        Main.UpdateTimeRate();
        Main.time += Main.dayRate;
        bool stopEvents = Main.ShouldNormalEventsBeAbleToStart();
        if (!Main.dayTime)
        {
            if (Main.time > 32400.0)
                Main.UpdateTime_StartDay(ref stopEvents);
        }
        else
        {
            if (Main.time > 54000.0)
                Main.UpdateTime_StartNight(ref stopEvents);
        }
    }

    private static void SubworldSpecificUpdateBehaviors()
    {
        SubworldSystem.noReturn = true;

        int asterlin = ModContent.NPCType<Asterlin>();
        PlayerCount(out int total, out int alive);
        if (!NPC.AnyNPCs(asterlin) && total == alive)
        {
            Point pos = FindNearestSurface(new Vector2(Main.rightWorld / 2, Main.bottomWorld / 2), true,
                Main.bottomWorld / 2, 1, true)!.Value.ToPoint();
            int index = NPC.NewNPC(new EntitySource_WorldEvent(), pos.X, pos.Y + 58, asterlin, 1);
            if (Main.netMode == NetmodeID.MultiplayerClient)
                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, index);
            Main.npc[index].netUpdate = true;
        }

        // Strong wind towards the west, the same direction as the background
        Main.windSpeedTarget = MathHelper.Lerp(0.88f, 1.32f, AperiodicSin(Main.GameUpdateCount * 0.02f) * 0.5f + 0.5f);
        Main.windSpeedCurrent = MathHelper.Lerp(Main.windSpeedCurrent, Main.windSpeedTarget, 0.03f);

        // nuh uh
        if (Main.bloodMoon)
        {
            Main.bloodMoon = false;
            AdditionsNetcode.SyncWorld();
        }

        if (Main.eclipse)
        {
            Main.eclipse = false;
            AdditionsNetcode.SyncWorld();
        }

        // remove the annoying stars
        foreach (Projectile proj in Main.ActiveProjectiles)
        {
            if (proj.type != ProjectileID.FallingStar)
                continue;
            proj.active = false;
        }

        // Remove usual weather
        if (Main.netMode != NetmodeID.MultiplayerClient)
        {
            if (Sandstorm.Happening)
                Sandstorm.StopSandstorm();
            Main.StopRain();
            Main.StopSlimeRain();
        }
    }

    public static void PlayerEnterEffects()
    {
        for (int i = 0; i < Main.maxPlayers; i++)
        {
            Player p = Main.player[i];
            if (!p.active || p.dead)
                continue;

            if (Main.myPlayer == i)
                Projectile.NewProjectile(new EntitySource_WorldEvent(), p.Center, Vector2.Zero,
                    ModContent.ProjectileType<TransmitterLightspeed>(), 0, 0f, Main.myPlayer, ai1: 1f);
            ScreenShakeSystem.New(new(7f, .6f), p.Center);
        }
    }
}