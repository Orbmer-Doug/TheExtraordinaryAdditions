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
using TheExtraordinaryAdditions.Core.Netcode;
using TheExtraordinaryAdditions.Core.Systems;
using static TheExtraordinaryAdditions.Content.World.Subworlds.CloudedCrater;

namespace TheExtraordinaryAdditions.Content.World.Subworlds;

public class CloudedCraterUpdateSystem : ModSystem
{
    public static bool WasInSubworldLastUpdateFrame
    {
        get;
        private set;
    }

    public override void OnModLoad()
    {
        AdditionsGlobalItem.CanUseItemEvent += DisableCelestialSigil;
        AdditionsGlobalItem.CanUseItemEvent += DisableProblematicItems;
        //AdditionsGlobalProjectile.PreAIEvent += KillProblematicProjectiles;
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
        bool isSponge = itemID == ItemID.SuperAbsorbantSponge || itemID == ItemID.LavaAbsorbantSponge || itemID == ItemID.HoneyAbsorbantSponge || itemID == ItemID.UltraAbsorbantSponge;
        bool isRegularBucket = itemID == ItemID.EmptyBucket || itemID == ItemID.WaterBucket || itemID == ItemID.LavaBucket || itemID == ItemID.HoneyBucket;
        bool isSpecialBucket = itemID == ItemID.BottomlessBucket || itemID == ItemID.BottomlessLavaBucket || itemID == ItemID.BottomlessHoneyBucket || itemID == ItemID.BottomlessShimmerBucket;
        return !isSponge && !isRegularBucket && !isSpecialBucket || itemID == ModContent.ItemType<MatterDisintegrationDrill>();
    }

    private bool KillProblematicProjectiles(Projectile projectile)
    {
        // Dont do anything if this event is called outside of the crater
        if (!WasInSubworldLastUpdateFrame)
            return true;

        if (projectile.type == ProjectileID.DD2ElderWins)
        {
            projectile.active = false;
            return false;
        }

        // no tombs
        if (projectile.type is ProjectileID.Tombstone or ProjectileID.Gravestone or ProjectileID.RichGravestone1 or ProjectileID.RichGravestone2 or
            ProjectileID.RichGravestone3 or ProjectileID.RichGravestone4 or ProjectileID.RichGravestone4 or ProjectileID.Headstone or ProjectileID.Obelisk or
            ProjectileID.GraveMarker or ProjectileID.CrossGraveMarker or ProjectileID.Headstone)
            projectile.active = false;

        // Prevent tile-manipulating items from working messing up tiles
        if (projectile.type == ModContent.ProjectileType<CannonHoldout>())
            projectile.active = false;
        if (projectile.type == ModContent.ProjectileType<CrystylCrusherRay>())
            projectile.active = false;
        if (projectile.type == ProjectileID.DirtBomb || projectile.type == ProjectileID.DirtStickyBomb)
            projectile.active = false;
        if (projectile.type == ProjectileID.SandBallGun || projectile.type == ProjectileID.SandBallGun)
            projectile.active = false;
        if (projectile.type == ProjectileID.SandBallFalling || projectile.type == ProjectileID.PearlSandBallFalling)
            projectile.active = false;
        if (projectile.type == ProjectileID.EbonsandBallFalling || projectile.type == ProjectileID.EbonsandBallGun)
            projectile.active = false;
        if (projectile.type == ProjectileID.CrimsandBallFalling || projectile.type == ProjectileID.CrimsandBallGun)
            projectile.active = false;

        // dirt rod
        if (projectile.type == ProjectileID.DirtBall)
            projectile.Kill();

        // No explosives
        bool dryRocket = projectile.type == ProjectileID.DryRocket || projectile.type == ProjectileID.DrySnowmanRocket;
        bool wetRocket = projectile.type == ProjectileID.WetRocket || projectile.type == ProjectileID.WetSnowmanRocket;
        bool honeyRocket = projectile.type == ProjectileID.HoneyRocket || projectile.type == ProjectileID.HoneySnowmanRocket;
        bool lavaRocket = projectile.type == ProjectileID.LavaRocket || projectile.type == ProjectileID.LavaSnowmanRocket;
        bool rocket = dryRocket || wetRocket || honeyRocket || lavaRocket ||
            projectile.type == ModContent.ProjectileType<MortarRoundProj>() || projectile.type == ModContent.ProjectileType<RubberMortarRoundProj>();

        bool dryMisc = projectile.type == ProjectileID.DryGrenade || projectile.type == ProjectileID.DryMine;
        bool wetMisc = projectile.type == ProjectileID.WetGrenade || projectile.type == ProjectileID.WetMine;
        bool honeyMisc = projectile.type == ProjectileID.HoneyGrenade || projectile.type == ProjectileID.HoneyMine;
        bool lavaMisc = projectile.type == ProjectileID.LavaGrenade || projectile.type == ProjectileID.LavaMine;
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

    private bool DisallowTileBreakage(int x, int y, int type)
    {
        return WasInSubworldLastUpdateFrame;
    }

    private bool DisallowWallBreakage(int x, int y, int type)
    {
        return WasInSubworldLastUpdateFrame;
    }

    public override void PreUpdateEntities()
    {
        // Verify whether things are in the subworld. This hook runs on both clients and the server. If for some reason this stuff needs to be determined in a different
        // hook it is necessary to ensure that property is preserved wherever you put it.
        bool inCrater = SubworldSystem.IsActive<CloudedCrater>();
        if (WasInSubworldLastUpdateFrame != inCrater)
        {
            // A major flaw with respect to subworld data transfer is the fact that Calamity's regular OnWorldLoad hooks clear everything.
            // This works well and good for Calamity's purposes, but it causes serious issues when going between subworlds. The result of this is
            // ordered as follows:

            // 1. Exit world. Store necessary data for subworld transfer.
            // 2. Load necessary stuff for subworld and wait.
            // 3. Enter subworld. Load data from step 1.
            // 4. Call OnWorldLoad, resetting everything from step 3.

            // In order to address this, a final step is introduced:
            // 5. Load data from step 3 again on the first frame of entity updating.
            if (inCrater)
            {
                if (Main.netMode != NetmodeID.Server)
                    LoadWorldDataFromTag("Client", ClientWorldDataTag);

                PlayerEnterEffects();
            }

            WasInSubworldLastUpdateFrame = inCrater;
        }

        // Everything beyond this point applies solely to the subworld
        if (!WasInSubworldLastUpdateFrame)
            return;

        SubworldSpecificUpdateBehaviors();
    }

    private static void SubworldSpecificUpdateBehaviors()
    {
        int asterlin = ModContent.NPCType<Asterlin>();
        PlayerCount(out int total, out int alive);
        if (!NPC.AnyNPCs(asterlin) && total == alive)
        {
            Point pos = FindNearestSurface(new Vector2(Main.rightWorld / 2, Main.bottomWorld / 2), true, Main.bottomWorld / 2, 1, true).Value.ToPoint();
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
                Projectile.NewProjectile(new EntitySource_WorldEvent(), p.Center, Vector2.Zero, ModContent.ProjectileType<TransmitterLightspeed>(), 0, 0f, Main.myPlayer, 0f, ai1: 1f);
            ScreenShakeSystem.New(new(7f, .6f), p.Center);
        }
    }
}