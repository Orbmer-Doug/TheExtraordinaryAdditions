
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.NPCs.Hostile.Arid;

public class GlassFocusedSniper : ModNPC
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.GlassFocusedSniper);
    public override void SetDefaults()
    {
        NPC.aiStyle = -1;
        AIType = -1;
        NPC.npcSlots = 1f;
        NPC.damage = 0;
        NPC.width = 192 / 2;
        NPC.height = 36 / 2;
        NPC.scale = .5f;

        NPC.noGravity = true;
        NPC.lavaImmune = true;
        NPC.ShowNameOnHover = false;
        NPC.dontTakeDamage = true;
        NPC.lifeMax = 100;
    }
    public ref float AttackTimer => ref NPC.ai[0];
    private int attackCounter;

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
        float predictivenessFactor = Utils.GetLerpValue(10f, 460f, NPC.Distance(target.Center), true) * 13f;

        NPC.velocity = NPC.SafeDirectionTo(target.Center + target.velocity * predictivenessFactor);
        NPC.rotation = GetAdjustedRotation(NPC, target, NPC.velocity.ToRotation());

        attackCounter++;
        if (canhit == false)
        {
            attackCounter = 0;
        }
        if (Vector2.Distance(NPC.Center, target.Center) < 2000 && canhit == true && !Main.player[NPC.target].dead && Main.player[NPC.target].active)
        {
            if (attackCounter == 90)
            {
                SoundEngine.PlaySound(SoundID.Item149, (Vector2?)NPC.Center, null);
            }
            if (attackCounter == 180)
            {
                attackCounter = 0;
                FireBullet();
                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, new Vector2(5 * -NPC.direction, -5f), ModContent.ProjectileType<EmptyRound>(), 0, 0f, -1, 0f, 0f, 0f);
            }
        }
        FindBase();


        void FireBullet()
        {
            SoundEngine.PlaySound(SoundID.Item40, NPC.Center);

            int damage = Main.masterMode ? 36 : Main.expertMode ? 24 : 19;
            Vector2 velocity = NPC.rotation.ToRotationVector2() * 11f;
            Vector2 position = NPC.Center + NPC.rotation.ToRotationVector2() * 20f;
            int type = ModContent.ProjectileType<GlassShell>();

            int projectile = Projectile.NewProjectile(NPC.GetSource_FromThis(), position, velocity, type, damage, 1f, Main.myPlayer);
            Main.projectile[projectile].friendly = false;
            Main.projectile[projectile].hostile = true;
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
        float maxrange = 99999999f;
        int find = ModContent.NPCType<DuneProwlerSniper>();

        int type = (int)NPC.ai[1];
        NPC n = Main.npc[(int)NPC.ai[1]];
        if (n.type == find && n.active && n.WithinRange(NPC.Center, maxrange))
        {
            NPC.Center = n.Center - (n.direction == 1 ? Vector2.UnitX * -21f : Vector2.UnitX * 21);
            NPC.alpha = n.alpha;
        }
        if (n.type == find && !n.active || !n.WithinRange(NPC.Center, maxrange) || type == -1)
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
        int find = ModContent.NPCType<DuneProwlerSniper>();
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
