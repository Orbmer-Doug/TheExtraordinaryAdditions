using Microsoft.Xna.Framework.Graphics;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;

namespace TheExtraordinaryAdditions.Content.NPCs.Hostile.Aurora;

public class EncasingGlacier : ModNPC
{
    public override string Texture => AssetRegistry.Invis;
    public override void SetStaticDefaults()
    {
        NPCID.Sets.ImmuneToRegularBuffs[Type] =
            NPCID.Sets.DontDoHardmodeScaling[Type] =
            NPCID.Sets.CantTakeLunchMoney[Type] =
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
        NPC.lifeMax = 5000;
        NPC.defense = 20;
        NPC.knockBackResist = 0f;
        NPC.width = NPC.height = 200;
        NPC.value = 0;
        NPC.noGravity = true;
        NPC.lavaImmune = true;
        NPC.HitSound = SoundID.Item50 with { Volume = 1.4f, Pitch = -.2f };
        NPC.DeathSound = null;
        NPC.aiStyle = -1;
        AIType = -1;
        NPC.npcSlots = 0f;
        NPC.noTileCollide = true;
        NPC.GravityIgnoresLiquid = true;
        NPC.GravityIgnoresSpace = true;
        NPC.GravityIgnoresType = true;
        NPC.BossBar = Main.BigBossProgressBar.NeverValid;
        Music = -1;
    }

    public override void ApplyDifficultyAndPlayerScaling(int numPlayers, float balance, float bossAdjustment)
    {
        NPC.lifeMax = (int)(NPC.lifeMax * 0.8f * balance);
    }

    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.Write((bool)NPC.dontTakeDamage);
    }
    public override void ReceiveExtraAI(BinaryReader reader)
    {
        NPC.dontTakeDamage = (bool)reader.ReadBoolean();
    }

    public int AuroraIndex
    {
        get => (int)NPC.ai[0];
        set => NPC.ai[0] = value;
    }
    public ref float Time => ref NPC.ai[1];
    public ref float BreakCompletion => ref NPC.ai[2];
    public bool Breaking
    {
        get => NPC.ai[3] == 1;
        set => NPC.ai[3] = value.ToInt();
    }
    public override void AI()
    {
        float start = InverseLerp(0f, 90f, Time);
        float life = InverseLerp(0f, NPC.lifeMax, NPC.life) * .5f + .5f;

        HideBossBar(NPC);
        NPC.Opacity = NPC.scale = start * life;
        NPC.timeLeft = 7200;

        if (BreakCompletion >= 1f)
            NPC.Kill();

        if (AuroraIndex >= 0 && AuroraIndex < Main.maxNPCs)
        {
            NPC npc = Main.npc?[AuroraIndex] ?? null;
            if (npc != null && npc.active && npc.type == ModContent.NPCType<AuroraGuard>())
            {
                AuroraGuard guard = npc.As<AuroraGuard>();
                NPC.Center = guard.GlacierPosition;
                BreakCompletion = guard.BreakCompletion;
            }
        }

        Time++;
    }

    // Prevent complete annihilation by anything with piercing
    private const int ImmuneTime = 4;
    public override void OnHitByProjectile(Projectile projectile, NPC.HitInfo hit, int damageDone)
    {
        NPC.immune[projectile.owner] = ImmuneTime;
    }
    public override void OnHitByItem(Player player, Item item, NPC.HitInfo hit, int damageDone)
    {
        NPC.immune[player.whoAmI] = ImmuneTime;
    }

    public override bool? CanFallThroughPlatforms() => true;
    public override bool CanHitPlayer(Player target, ref int cooldownSlot) => false;
    public override bool? DrawHealthBar(byte hbPosition, ref float scale, ref Vector2 position) => true;
    public override void BossLoot(ref int potionType)
    {
        potionType = ItemID.None;
    }
    public override bool CheckDead()
    {
        if (BreakCompletion >= 1f)
            return true;

        Breaking = true;
        NPC.life = 1;
        NPC.dontTakeDamage = true;
        NPC.netUpdate = true;
        return false;
    }

    public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
    {
        if (BreakCompletion >= 1f)
            return false;

        void draw()
        {
            ManagedShader shader = AssetRegistry.GetShader("RadialCrackingShader");
            shader.TrySetParameter("Completion", BreakCompletion);

            Main.spriteBatch.EnterShaderRegion(BlendState.NonPremultiplied, shader.Effect);
            Texture2D tex = AssetRegistry.GetTexture(AdditionsTexture.Glacier);

            shader.Render();
            Main.spriteBatch.DrawBetter(tex, NPC.Center, null, Lighting.GetColor(NPC.Center.ToTileCoordinates()), 0f, tex.Size() / 2, 3f);

            Main.spriteBatch.ExitShaderRegion();
        }
        LayeredDrawSystem.QueueDrawAction(draw, PixelationLayer.OverNPCs);
        return false;
    }
}
