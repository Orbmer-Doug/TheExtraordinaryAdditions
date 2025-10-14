using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Vanilla.Early;
using TheExtraordinaryAdditions.Content.Projectiles.Vanilla.Middle;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Core.Globals.ProjectileGlobal;

public class AdditionsGlobalProjectile : GlobalProjectile
{
    public delegate bool ProjectileConditionDelegate(Projectile projectile);
    public static event ProjectileConditionDelegate PreAIEvent;

    public override bool InstancePerEntity => true;

    public override void Unload()
    {
        PreAIEvent = null;
    }

    public override bool OnTileCollide(Projectile projectile, Vector2 oldVelocity)
    {
        return base.OnTileCollide(projectile, oldVelocity);
    }

    public override bool PreAI(Projectile projectile)
    {
        if (PreAIEvent is null)
            return true;

        bool result = true;
        foreach (Delegate d in PreAIEvent.GetInvocationList())
            result &= ((ProjectileConditionDelegate)d).Invoke(projectile);

        return result;
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
                        if (Main.myPlayer == player.whoAmI)
                        {
                            // Make the mini's
                            for (int i = 0; i <= 2; i++)
                            {
                                int obj = projectile.NewProj(projectile.Center, Vector2.One, mini, (int)(projectile.damage * .33f), 0f, player.whoAmI, i, projectile.whoAmI);
                                Main.projectile[obj].AdditionsInfo().ExtraAI[1] = MathHelper.TwoPi * i / 3f;
                                Main.projectile[obj].AdditionsInfo().ExtraAI[2] = Main.rand.NextFromList(-1, 1);
                            }
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
}