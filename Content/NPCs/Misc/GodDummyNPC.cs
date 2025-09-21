
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using TheExtraordinaryAdditions.Common.Particles;
using TheExtraordinaryAdditions.Content.Items.Tools;
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
        NPC.aiStyle = AIType = 0;
        NPC.npcSlots = .1f;
        NPC.gfxOffY -= 2;
    }

    public bool NotABossAround()
    {
        foreach (NPC npc in Main.ActiveNPCs)
        {
            if (npc.type != NPC.type && (npc.boss || npc.IsEater()))
                return false;
        }

        return true;
    }

    public override bool? CanBeHitByItem(Player player, Item item) => NotABossAround() ? null : false;
    public override bool CanBeHitByNPC(NPC attacker) => false;
    public override bool? CanBeHitByProjectile(Projectile projectile) => NotABossAround() ? null : false;

    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.Write(NPC.dontTakeDamage);
        writer.Write(NPC.lifeMax);
        writer.Write(NPC.width);
        writer.Write(NPC.height);
        writer.Write(NPC.scale);
        writer.Write(NPC.rotation);
        writer.Write(NPC.noGravity);
    }

    public override void ReceiveExtraAI(BinaryReader reader)
    {
        NPC.dontTakeDamage = reader.ReadBoolean();
        NPC.lifeMax = reader.ReadInt32();
        NPC.width = reader.ReadInt32();
        NPC.height = reader.ReadInt32();
        NPC.scale = reader.ReadSingle();
        NPC.rotation = reader.ReadSingle();
        NPC.noGravity = reader.ReadBoolean();
    }

    public bool Immortal()
    {
        return NPC.lifeMax >= GodDummy.MaxLifeAmount;
    }

    public override void AI()
    {
        if (NotABossAround() == false)
        {
            if (!NPC.dontTakeDamage)
            {
                NPC.dontTakeDamage = true;
                this.Sync();
            }
        }
        else
        {
            if (NPC.dontTakeDamage)
            {
                NPC.dontTakeDamage = false;
                this.Sync();
            }
        }

        NPC.timeLeft = 7200;

        if (this.RunServer())
        {
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
                this.Sync();
            }
        }

        if (NPC.noGravity)
        {
            NPC.Center += new Vector2(0f, MathF.Sin(TimeSystem.UpdateCount * .01f) * .5f);
            if (TimeSystem.UpdateCount % 2 == 1)
                ParticleRegistry.SpawnGlowParticle(Vector2.Lerp(NPC.RotHitbox().BottomLeft, NPC.RotHitbox().BottomRight, Main.rand.NextFloat()),
                    Main.rand.NextVector2Circular(1f, 1f), Main.rand.Next(20, 30), Main.rand.NextFloat(.2f, .4f), NotABossAround() == false ? Color.Red : Color.SaddleBrown);
        }
    }


    public override void UpdateLifeRegen(ref int damage)
    {
        if (Immortal())
            NPC.lifeRegen += 200000;
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
            NPC.frameCounter += 1.0;
        
        if (NPC.frameCounter > 6.0)
        {
            NPC.frameCounter = 0.0;
            NPC.frame.Y += frameHeight;
        }
        if (NPC.AdditionsInfo().ExtraAI[4] == -1f)
        {
            if (NPC.justHit && NPC.frame.Y > frameHeight * 2)
                NPC.frame.Y = frameHeight;
            else if (NPC.frame.Y > frameHeight * 3)
                NPC.frame.Y = 0;
        }
        else if (NPC.justHit && NPC.frame.Y > frameHeight * 7)
            NPC.frame.Y = frameHeight * 5;
        else if (NPC.frame.Y > frameHeight * 10 || NPC.frame.Y < frameHeight * 4)
            NPC.frame.Y = frameHeight * 4;
    }

    public override bool CheckDead()
    {
        if (Immortal())
        {
            NPC.life = NPC.lifeMax;
            NPC.netUpdate = true;
            return false;
        }
        return true;
    }

    public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
    {
        Color mainColor = Color.White;
        if (NotABossAround() == false)
            mainColor = Color.Red;

        Texture2D texture = TextureAssets.Npc[NPC.type].Value;
        Vector2 position = NPC.Center - screenPos;
        Vector2 origin = NPC.frame.Size() * 0.5f;

        // Squash out crustiness
        if (NPC.scale >= 2.5f)
            Main.spriteBatch.SetBlendState(BlendState.NonPremultiplied);

        SpriteEffects dir = (-NPC.direction).ToSpriteDirection();
        if (NPC.boss)
            NPC.DrawNPCBackglow(Color.Tan * .3f, 5f, dir, NPC.frame);
        spriteBatch.Draw(texture, position, (Rectangle?)NPC.frame, NPC.GetAlpha(mainColor), NPC.rotation, origin, NPC.scale, dir, 0f);

        if (NPC.scale >= 2.5f)
            Main.spriteBatch.ResetBlendState();

        return false;
    }
}
