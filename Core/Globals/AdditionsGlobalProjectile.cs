using System;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using TheExtraordinaryAdditions.Content.Projectiles.Vanilla.Early;
using TheExtraordinaryAdditions.Content.Projectiles.Vanilla.Middle;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Core.Globals;

public class AdditionsGlobalProjectile : GlobalProjectile
{
    public override bool InstancePerEntity => true;
    
    public override bool AppliesToEntity(Projectile entity, bool lateInstantiation)
    {
        if (entity.ModProjectile == null || entity.type < ProjectileID.Count)
            return true;

        return entity.ModProjectile.Mod.Name == Mod.Name;
    }

    public const int TotalExtraAISlots = 20;
    public float[] ExtraAI = new float[TotalExtraAISlots];
    public override void SetDefaults(Projectile projectile)
    {
        for (int i = 0; i < ExtraAI.Length; i++)
            ExtraAI[i] = 0f;
    }

    public override bool OnTileCollide(Projectile projectile, Vector2 oldVelocity)
    {
        return base.OnTileCollide(projectile, oldVelocity);
    }

    public override void AI(Projectile projectile)
    {
        Player player = Main.player[projectile.owner];
        switch (projectile.type)
        {
            case ProjectileID.Kraken:
                {
                    int ten = ModContent.ProjectileType<KrakenTentacle>();

                    // Only do so once
                    if (projectile.localAI[2] == 0f)
                    {
                        // Make the suspicious tentacles
                        if (player.ownedProjectileCounts[ten] <= 2 && Main.myPlayer == player.whoAmI && projectile.ai[0] != -1f)
                        {
                            Vector2 vel = new Vector2(Main.rand.Next(-13, 14), Main.rand.Next(-13, 14)) * 0.25f;
                            for (int n = 0; n < 3; n++)
                            {
                                projectile.NewProj(projectile.Center, vel, ten, projectile.damage, 2f, projectile.owner, 0f, 0f, projectile.whoAmI);
                            }
                        }
                        projectile.localAI[2] = 1f;
                    }
                }
                break;
            case ProjectileID.Valor:
                int mini = ModContent.ProjectileType<MiniValor>();

                // Only do so once
                if (projectile.localAI[2] == 0f)
                {
                    if (player.ownedProjectileCounts[mini] <= 0)
                    {
                        // Make the mini's
                        for (int i = 0; i <= 2; i++)
                        {
                            int obj = projectile.NewProj(projectile.Center, Vector2.One, mini, (int)(projectile.damage * .33f), 0f, player.whoAmI, i, projectile.whoAmI);
                            Main.projectile[obj].Additions().ExtraAI[1] = MathHelper.TwoPi * i / 3f;
                            Main.projectile[obj].Additions().ExtraAI[2] = Main.rand.NextFromList(-1, 1);
                        }
                    }
                    projectile.localAI[2] = 1f;
                }
                break;
        }
    }

    public override void ModifyHitNPC(Projectile projectile, NPC target, ref NPC.HitModifiers modifiers)
    {
        Player player = Main.player[projectile.owner];
    }

    public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
    {
        Player player = Main.player[projectile.owner];
        GlobalPlayer modPlayer = player.GetModPlayer<GlobalPlayer>();

        switch (projectile.type)
        {
            case ProjectileID.TheEyeOfCthulhu:
                {
                    if (Main.myPlayer == projectile.owner && Main.rand.NextBool())
                        projectile.NewProj(projectile.Center, Main.rand.NextVector2CircularEdge(4f, 4f),
                            ModContent.ProjectileType<TinyServant>(), (int)(projectile.damage * .25f), 0f, projectile.owner);
                }
                break;
        }
    }

    public override void SendExtraAI(Projectile projectile, BitWriter bitWriter, BinaryWriter binaryWriter)
    {
        for (int i = 0; i < TotalExtraAISlots; i++)
            binaryWriter.Write((float)projectile.Additions().ExtraAI[i]);
    }

    public override void ReceiveExtraAI(Projectile projectile, BitReader bitReader, BinaryReader binaryReader)
    {
        for (int i = 0; i < TotalExtraAISlots; i++)
            projectile.Additions().ExtraAI[i] = (float)binaryReader.ReadSingle();
    }
}