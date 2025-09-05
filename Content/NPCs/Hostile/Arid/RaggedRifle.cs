
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.NPCs.Hostile.Arid;

public class RaggedRifle : ModNPC
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.RaggedRifle);
    public override void SetDefaults()
    {
        NPC.aiStyle = -1;
        AIType = -1;
        NPC.npcSlots = 1f;
        NPC.damage = 0;
        NPC.width = 72;
        NPC.height = 23;
        NPC.noGravity = true;
        NPC.lavaImmune = true;
        NPC.ShowNameOnHover = false;
        NPC.dontTakeDamage = true;
        NPC.lifeMax = 100;
    }
    public ref float AttackTimer => ref NPC.ai[0];
    public ref float BurstWait => ref NPC.ai[2];

    public static float GetAdjustedRotation(NPC npc, Player target, float baseAngle, bool adjustDirection = false)
    {
        float idealAngle = baseAngle;
        if (adjustDirection)
            npc.spriteDirection = (npc.Center.X > target.Center.X).ToDirectionInt();

        if (npc.spriteDirection == 1)
            idealAngle += MathHelper.Pi;
        return idealAngle;
    }
    public override void AI()
    {
        Player target = Main.player[NPC.target];
        if (NPC.target < 0 || NPC.target == 255 || Main.player[NPC.target].dead || !Main.player[NPC.target].active)
        {
            target = AcquireNewTarget(NPC);
            NPC.netUpdate = true;
        }
        BuffImmune();
        bool canhit = Collision.CanHit(NPC.Center, 1, 1, target.Center, 1, 1);
        float predictivenessFactor = Utils.GetLerpValue(80f, 360f, NPC.Distance(target.Center), true) * 4f;

        NPC.velocity = NPC.SafeDirectionTo(target.Center + target.velocity * predictivenessFactor);
        NPC.rotation = GetAdjustedRotation(NPC, target, NPC.velocity.ToRotation());

        if (BurstWait < 10)
        {
            BurstWait = 10;
        }
        if (BurstWait == 10)
        {
            AttackTimer++;
        }
        if (BurstWait > 10)
        {
            AttackTimer = 0f;
        }
        BurstWait--;
        if (Vector2.Distance(NPC.Center, target.Center) < 2000 && !Main.player[NPC.target].dead && Main.player[NPC.target].active && BurstWait <= 10)
        {
            if (AttackTimer == 1f)
            {
                FireBullet();
            }
            if (AttackTimer == 10f)
            {
                FireBullet();
            }
            if (AttackTimer == 20f)
            {
                FireBullet();
                BurstWait = 70;
            }
        }
        FindBase();


        void FireBullet()
        {
            int damage = Main.masterMode ? 26 : Main.expertMode ? 21 : 13;
            Vector2 velocity = NPC.rotation.ToRotationVector2() * 40f;
            Vector2 position = NPC.Center + NPC.rotation.ToRotationVector2() * 20f;
            int type = ModContent.ProjectileType<RifleBullet>();

            for (int i = 0; i < 8; i++)
            {
            }
            NPC.Shoot(position, velocity, type, damage, 1f, Main.myPlayer);
            NPC.netUpdate = true;
        }
    }
    public static Player AcquireNewTarget(NPC npc, bool changeDirection = true)
    {
        npc.TargetClosest(changeDirection);
        return Main.player[npc.target];
    }

    private void FindBase()
    {
        int find = ModContent.NPCType<DuneProwlerAssault>();

        int type = (int)NPC.ai[1];
        NPC npc = Main.npc[(int)NPC.ai[1]];
        if (npc.type == find && npc is not null && npc.active && type.WithinBounds(Main.maxNPCs))
        {
            NPC.Center = Vector2.Lerp(NPC.Center, npc.Center - (npc.direction == 1 ? Vector2.UnitX * -21f : Vector2.UnitX * 21), .6f);
        }
        if (npc is null || !npc.active || !type.WithinBounds(Main.maxNPCs) || type == -1)
        {
            die();
        }

        void die()
        {
            NPC.checkDead();
            NPC.life = 0;
            NPC.HitEffect();
            NPC.active = false;
        }
    }
    private void BuffImmune()
    {
        NPC.buffImmune[BuffID.Slow] = true;
        NPC.buffImmune[BuffID.Confused] = true;
        NPC.buffImmune[BuffID.Frostburn] = true;
        NPC.buffImmune[BuffID.Frostburn2] = true;
        NPC.buffImmune[BuffID.Venom] = true;
        NPC.buffImmune[BuffID.WeaponImbueVenom] = true;
        NPC.buffImmune[BuffID.Poisoned] = true;
        NPC.buffImmune[BuffID.WeaponImbuePoison] = true;
    }
    public override bool? CanFallThroughPlatforms()
    {
        return true;
    }
    public override bool CanHitPlayer(Player target, ref int cooldownSlot)
    {
        return false;
    }
    public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
    {
        Texture2D texture = TextureAssets.Npc[NPC.type].Value;

        Player target = Main.player[NPC.target];

        float laserRotation = -NPC.rotation + MathHelper.Pi;
        if (NPC.spriteDirection == -1)
            laserRotation += MathHelper.Pi;

        float maxrange = 99999999f;
        int find = ModContent.NPCType<DuneProwlerAssault>();
        Vector2 drawPosition = NPC.Center - Main.screenPosition;
        SpriteEffects direction = 0;
        NPC n = Main.npc[(int)NPC.ai[1]];
        if (n.type == find && n.active && n.WithinRange(NPC.Center, maxrange))
        {
            if (n.direction == -1)
            {
                drawPosition = NPC.Center - Main.screenPosition;
                direction = n.direction == -1 ? SpriteEffects.FlipVertically : SpriteEffects.None;
            }
        }
        Main.EntitySpriteDraw(texture, drawPosition, texture.Frame(), drawColor, NPC.rotation, texture.Size() * 0.5f, NPC.scale, direction, 0);

        return false;
    }
}
