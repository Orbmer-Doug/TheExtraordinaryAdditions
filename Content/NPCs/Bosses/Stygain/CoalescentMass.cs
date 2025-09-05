using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Buffs.Debuff;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.NPCs.Bosses.Stygain;

[AutoloadBossHead]
public class CoalescentMass : ModNPC
{
    public override string BossHeadTexture => AssetRegistry.GetTexturePath(AdditionsTexture.CoalescentMass_Head_Boss);
    public override string Texture => AssetRegistry.Invis;
    public static readonly int Life = DifficultyBasedValue(15000, 30000, 43000, 56000, 65000, 80000);
    public override void SetStaticDefaults()
    {
        NPCID.Sets.ImmuneToRegularBuffs[Type] = 
            NPCID.Sets.DontDoHardmodeScaling[Type] = 
            NPCID.Sets.CantTakeLunchMoney[Type] = 
            NPCID.Sets.MPAllowedEnemies[Type] = 
            NPCID.Sets.ProjectileNPC[Type] = 
            NPCID.Sets.TeleportationImmune[Type] = 
            NPCID.Sets.DoesntDespawnToInactivityAndCountsNPCSlots[Type] = true;

        NPCID.Sets.NPCBestiaryDrawModifiers bestiaryData = new NPCID.Sets.NPCBestiaryDrawModifiers()
        {
            Hide = true
        };
        NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, bestiaryData);
    }

    public override void SetDefaults()
    {
        NPC.lifeMax = Life;
        NPC.defense = 20;
        NPC.knockBackResist = 0f;
        NPC.width = NPC.height = 230;
        NPC.value = 0;
        NPC.noGravity = true;
        NPC.lavaImmune = true;
        NPC.HitSound = SoundID.DD2_WitherBeastCrystalImpact with { Volume = 1.4f, Pitch = -.3f };
        NPC.DeathSound = null;
        NPC.aiStyle = -1;
        AIType = -1;
        NPC.npcSlots = 0f;
        NPC.noTileCollide = true;
        NPC.GravityIgnoresLiquid = true;
        NPC.GravityIgnoresSpace = true;
        NPC.GravityIgnoresType = true;
        NPC.boss = true;
        NPC.BossBar = Main.BigBossProgressBar.NeverValid;
        Music = -1;

        NPC.netAlways = true;
    }

    public override void ApplyDifficultyAndPlayerScaling(int numPlayers, float balance, float bossAdjustment)
    {
        NPC.lifeMax = (int)(NPC.lifeMax * 0.8f * balance);
    }

    public NPC Owner => Main.npc[(int)NPC.ai[0]];
    public ref float Time => ref NPC.ai[1];

    public override void AI()
    {
        /*
        if (Owner == null)
        {
            NPC.Kill();
            return;
        }*/
        if (Time == 0f)
        {
            NPC.position.Y += NPC.height / 2;
        }

        float start = Utils.GetLerpValue(0f, 90f, Time, true);
        float life = InverseLerp(0f, Life, NPC.life) * .5f + .5f;

        HideBossBar(NPC);
        NPC.Opacity = NPC.scale = start * life;
        NPC.timeLeft = 7200;

        foreach (Player p in Main.ActivePlayers)
        {
            if (p != null && p.active && !p.dead)
            {
                p.AddBuff(ModContent.BuffType<HemorrhageTransfer>(), 2);
            }
        }

        Time++;
    }

    /*
    // Prevent complete annihilation by anything with piercing
    private const int ImmuneTime = 14;
    public override void OnHitByProjectile(Projectile projectile, NPC.HitInfo hit, int damageDone)
    {
        NPC.immune[projectile.owner] = ImmuneTime;
    }
    public override void OnHitByItem(Player player, Item item, NPC.HitInfo hit, int damageDone)
    {
        NPC.immune[player.whoAmI] = ImmuneTime;
    }
    */

    public override bool? CanFallThroughPlatforms() => true;
    public override bool CanHitPlayer(Player target, ref int cooldownSlot) => false;
    public override bool? DrawHealthBar(byte hbPosition, ref float scale, ref Vector2 position) => false;
    public override void BossLoot(ref int potionType)
    {
        potionType = ItemID.None;
    }

    public override void OnKill()
    {
        AdditionsSound.BlackHoleExplosion.Play(NPC.Center, .9f, -.3f);
        NPC.NewNPCProj(NPC.Center, Vector2.Zero, ModContent.ProjectileType<MassExplosion>(), 5000, 6f);
    }

    public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
    {
        Main.spriteBatch.PrepareForShaders();
        DrawField(NPC, NPC.Center - Main.screenPosition);
        Main.spriteBatch.ExitShaderRegion();

        return false;
    }

    public static void DrawField(NPC NPC, Vector2 pos, float size = 1f)
    {
        float time = Main.GlobalTimeWrappedHourly;

        ManagedShader shader = AssetRegistry.GetShader("StygainMass");

        shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.WavyBlotchNoise), 1, SamplerState.LinearWrap);
        shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.FractalNoise), 2, SamplerState.LinearWrap);
        shader.TrySetParameter("pixelationFactor", new Vector2(200f, 200f));
        shader.TrySetParameter("posterizationPrecision", 14f);
        shader.TrySetParameter("globalTime", time * 1f);
        shader.Render();

        // Squish the shield in random direction to be almost fluid like and account for health
        float scaleX = MathHelper.Lerp(0.5f, .68f, AperiodicSin(time * .2f) * .5f + .5f) * NPC.scale;
        float scaleY = MathHelper.Lerp(0.5f, .68f, AperiodicSin(time * .35f) * .5f + .5f) * NPC.scale;
        Vector2 scale = new Vector2(scaleX, scaleY) * size;
        float interpolant = Utils.GetLerpValue(0f, Life, NPC.life, true);

        Texture2D pixel = AssetRegistry.GetTexture(AdditionsTexture.Pixel);
        Main.spriteBatch.Draw(pixel, pos, null, Color.Crimson * NPC.Opacity, 0f, pixel.Size() / 2, NPC.Size * (scale * 3f), 0, 0f);
    }
}

public enum MapStyle
{
    Fullscreen = 0,
    Minimap = 1,
    Overlay = 2,
}

public class HeadDetour : ModSystem
{
    public override void Load()
    {
        On_Main.DrawNPCHeadBoss += On_Main_DrawNPCHeadBoss;
    }
    public override void Unload()
    {
        On_Main.DrawNPCHeadBoss -= On_Main_DrawNPCHeadBoss;
    }

    public static void On_Main_DrawNPCHeadBoss(On_Main.orig_DrawNPCHeadBoss orig, Entity theNPC,
        byte alpha, float headScale, float rotation, SpriteEffects effects, int bossHeadId, float x, float y)
    {
        if (theNPC != null && NPCHeadLoader.GetBossHeadSlot(AssetRegistry.GetTexturePath(AdditionsTexture.CoalescentMass_Head_Boss)) == bossHeadId)
        {
            Vector2 pos = new(x, y);

            float mapScale = 1f;
            switch (Main.mapStyle)
            {
                case (int)MapStyle.Fullscreen:
                    mapScale = Main.mapFullscreenScale * .1f;
                    break;
                case (int)MapStyle.Minimap:
                    mapScale = Main.mapMinimapScale * .1f;
                    break;
                case (int)MapStyle.Overlay:
                    mapScale = Main.mapOverlayScale * .05f;
                    break;
            }

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, null, null, null, null, Main.UIScaleMatrix);

            CoalescentMass.DrawField(theNPC as NPC, pos, mapScale);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, null, Main.UIScaleMatrix);
            return;
        }

        orig(theNPC, alpha, headScale, rotation, effects, bossHeadId, x, y);
    }
}