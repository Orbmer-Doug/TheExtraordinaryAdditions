
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Common.Particles;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Systems;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.NPCs.Misc;

public class GodDummyNPC : ModNPC
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.GodDummyNPC);
    public override void SetStaticDefaults()
    {
        this.ExcludeFromBestiary();
        Main.npcFrameCount[NPC.type] = 11;
        NPCID.Sets.CantTakeLunchMoney[Type] = true;
    }
    public static Player Owner => Main.LocalPlayer;
    public override void SetDefaults()
    {
        NPC.width = 18;
        NPC.height = 48;
        NPC.damage = 0;
        NPC.defense = 0;
        NPC.lifeMax = 1000;
        NPC.HitSound = SoundID.NPCHit15;
        NPC.DeathSound = null;
        NPC.value = 0f;
        NPC.knockBackResist = 0f;
        NPC.netAlways = true;
        NPC.aiStyle = -1;
        NPC.gfxOffY -= 2;
    }
    public bool ThereIsABossAndItsNotMe()
    {
        for (int i = 0; i < Main.maxNPCs; i++)
        {
            if (Main.npc[i] is null)
                continue;
            NPC npc = Main.npc[i];
            if ((npc.boss || npc.IsEater()) && npc.type != NPC.type && npc.active)
                return false;
        }

        return true;
    }
    public override bool? CanBeHitByItem(Player player, Item item)
    {
        if (ThereIsABossAndItsNotMe() == false) return false;
        return null;
    }
    public override bool CanBeHitByNPC(NPC attacker)
    {
        return false;
    }
    public override bool? CanBeHitByProjectile(Projectile projectile)
    {
        if (ThereIsABossAndItsNotMe() == false) return false;
        return null;
    }
    public ref float MovingSpeed => ref NPC.AdditionsInfo().ExtraAI[1];
    public ref Player Player => ref Main.player[(int)NPC.AdditionsInfo().ExtraAI[2]];
    public ref float BasicTimer => ref NPC.AdditionsInfo().ExtraAI[3];
    public GlobalPlayer GlobalPlayer => Player.Additions();

    public override void AI()
    {
        // Manually set-up the dummy whenever it was called on netcode
        if (NPC.localAI[0] == 0f && NPC.ai[0] == 1f)
        {
            Player = Main.player[(int)NPC.AdditionsInfo().ExtraAI[0]];
            NPC.AdditionsInfo().ExtraAI[1] = GlobalPlayer.DummyMoveSpeed;
            NPC.AdditionsInfo().ExtraAI[2] = Player.whoAmI;
            NPC.boss = GlobalPlayer.DummyBoss;
            NPC.life = GlobalPlayer.DummyMaxLife;
            NPC.lifeMax = GlobalPlayer.DummyMaxLife;
            NPC.defense = GlobalPlayer.DummyDefense;
            NPC.scale = GlobalPlayer.DummyScale;
            NPC.direction = (Player.Center.X < NPC.Center.X).ToDirectionInt();

            NPC.localAI[0] = 1f;
        }
        if (MovingSpeed > 0f)
        {
            BasicTimer++;
            NPC.noGravity = true;
            Vector2 destination = Owner.Top - new Vector2(-145f * Owner.direction, MathHelper.Lerp(50f, 80f, Cos01(BasicTimer / 24f)));
            NPC.Center = Vector2.Lerp(NPC.Center, destination, 0.025f);
            NPC.Center = NPC.Center + (destination - NPC.Center).SafeNormalize(Vector2.Zero) * MathF.Min(MovingSpeed, NPC.Distance(destination));
        }

        if (ThereIsABossAndItsNotMe() == false)
        {
            NPC.immortal = true;
            NPC.dontTakeDamage = true;
        }
        else
        {
            NPC.immortal = false;
            NPC.dontTakeDamage = false;
        }

        NPC.timeLeft = 7200;
        int oldWidth = NPC.width;
        int idealWidth = (int)(NPC.scale * 18);
        int idealHeight = (int)(NPC.scale * 48);
        if (idealWidth != oldWidth)
        {
            NPC.position.X += NPC.width / 2;
            NPC.position.Y += NPC.height / 2;
            NPC.width = idealWidth;
            NPC.height = idealHeight;
            NPC.position.X -= NPC.width / 2;
            NPC.position.Y -= NPC.height / 2;
        }

        if (Immortal())
        {
            NPC.lifeMax = int.MaxValue;
            NPC.life = NPC.lifeMax;
        }

        if (NPC.noGravity)
        {
            NPC.Center += new Vector2(0f, MathF.Sin(TimeSystem.UpdateCount * .01f) * .5f);
            if (TimeSystem.UpdateCount % 2 == 1)
                ParticleRegistry.SpawnGlowParticle(Vector2.Lerp(NPC.RotHitbox().BottomLeft, NPC.RotHitbox().BottomRight, Main.rand.NextFloat()), Main.rand.NextVector2Circular(1f, 1f), Main.rand.Next(20, 30), Main.rand.NextFloat(.2f, .4f), ThereIsABossAndItsNotMe() == false ? Color.Red : Color.SaddleBrown);
        }
    }
    public override bool? CanCollideWithPlayerMeleeAttack(Player player, Item item, Rectangle meleeAttackHitbox)
    {
        return meleeAttackHitbox.ToRotated(0f).Intersects(NPC.RotHitbox(NPC.rotation));
    }
    public bool Immortal()
    {
        return NPC.life >= 200000;
    }
    public override void UpdateLifeRegen(ref int damage)
    {
        if (Immortal())
            NPC.lifeRegen += 2000000;
    }

    public override bool? DrawHealthBar(byte hbPosition, ref float scale, ref Vector2 position)
    {
        return false;
    }

    public override void HitEffect(NPC.HitInfo hit)
    {
        NPC.AdditionsInfo().ExtraAI[4] = hit.HitDirection * NPC.direction;
    }

    public override void FindFrame(int frameHeight)
    {
        if (NPC.justHit || NPC.frameCounter > 0.0 || NPC.frame.Y != 0 && NPC.frame.Y != frameHeight * 4)
        {
            NPC.frameCounter += 1.0;
        }
        if (NPC.frameCounter > 6.0)
        {
            NPC.frameCounter = 0.0;
            NPC.frame.Y += frameHeight;
        }
        if (NPC.AdditionsInfo().ExtraAI[4] == -1f)
        {
            if (NPC.justHit && NPC.frame.Y > frameHeight * 2)
            {
                NPC.frame.Y = frameHeight;
            }
            else if (NPC.frame.Y > frameHeight * 3)
            {
                NPC.frame.Y = 0;
            }
        }
        else if (NPC.justHit && NPC.frame.Y > frameHeight * 7)
        {
            NPC.frame.Y = frameHeight * 5;
        }
        else if (NPC.frame.Y > frameHeight * 10 || NPC.frame.Y < frameHeight * 4)
        {
            NPC.frame.Y = frameHeight * 4;
        }
    }

    public override bool CheckDead()
    {
        if (NPC.lifeRegen < 0)
        {
            NPC.life = NPC.lifeMax;
            return false;
        }
        return true;
    }

    public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
    {
        Color mainColor = Color.White;
        if (ThereIsABossAndItsNotMe() == false)
            mainColor = Color.Red;

        Texture2D texture = TextureAssets.Npc[NPC.type].Value;
        Vector2 position = NPC.Center - screenPos;
        Vector2 origin = NPC.frame.Size() * 0.5f;

        // Squash out crustiness
        if (NPC.scale >= 2.5f)
            Main.spriteBatch.SetBlendState(BlendState.NonPremultiplied);

        SpriteEffects dir = NPC.direction.ToSpriteDirection();
        if (NPC.boss)
            NPC.DrawNPCBackglow(Color.Tan * .3f, 5f, dir, NPC.frame);
        spriteBatch.Draw(texture, position, (Rectangle?)NPC.frame, NPC.GetAlpha(mainColor), NPC.rotation, origin, NPC.scale, dir, 0f);

        if (NPC.scale >= 2.5f)
            Main.spriteBatch.ResetBlendState();

        return false;
    }
}
