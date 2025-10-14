using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Items.Weapons.Magic.Middle;
using TheExtraordinaryAdditions.Content.Projectiles.Base;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Magic.Middle;

public class StarlessHoldout : BaseIdleHoldoutProjectile
{
    public override string Texture => AssetRegistry.Invis;
    public override int AssociatedItemID => ModContent.ItemType<StarlessSea>();
    public override int IntendedProjectileType => ModContent.ProjectileType<StarlessHoldout>();
    public override void Defaults()
    {
        Projectile.width = 38;
        Projectile.height = 48;
        Projectile.DamageType = DamageClass.Magic;
    }
    public ref float Time => ref Projectile.ai[0];
    public bool Released
    {
        get => Projectile.ai[1] == 1f;
        set => Projectile.ai[1] = value.ToInt();
    }
    public ref float Counter => ref Projectile.ai[2];
    public ref float Offset => ref Projectile.AdditionsInfo().ExtraAI[0];
    public int?[] CurrentLances = new int?[4];
    private const int OffsetMax = 200;
    public override void SafeAI()
    {
        if (this.RunLocal())
        {
            float interpolant = Utils.GetLerpValue(5f, 20f, Projectile.Distance(Modded.mouseWorld), true);
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, Projectile.SafeDirectionTo(Modded.mouseWorld), interpolant);
            if (Projectile.velocity != Projectile.oldVelocity)
                this.Sync();
        }
        Projectile.rotation = Projectile.velocity.ToRotation();
        Owner.ChangeDir(Projectile.velocity.X.NonZeroSign());
        Owner.SetFrontHandBetter(0, Projectile.rotation);
        Owner.SetBackHandBetter(0, Projectile.rotation);
        Projectile.Center = Center + Projectile.velocity * Projectile.width * .5f;

        if (Main.bloodMoon)
        {
            int type = ModContent.ProjectileType<TheStarsAreAfraid>();

            if (Time % Item.useTime == Item.useTime - 1 && Counter < 4 && TryUseMana())
            {
                for (int i = 0; i < 4; i++)
                {
                    int? proj = CurrentLances[i];
                    if (proj.HasValue)
                        continue;

                    int p = Projectile.NewProj(Owner.Center, Vector2.Zero, type, Projectile.damage, Projectile.knockBack, Owner.whoAmI, 0f, i, 0f, 0f, Projectile.whoAmI);
                    CurrentLances[i] = Main.projectile[p].whoAmI;
                    break;
                }
            }

            if ((this.RunLocal() && Modded.SafeMouseLeft.Current) && !Released)
            {
                AdditionsSound.MagicStrike.Play(Projectile.Center, 1.2f, 0f, .14f);
                for (int i = 0; i < 4; i++)
                {
                    int? proj = CurrentLances[i];
                    if (proj.HasValue)
                    {
                        Projectile projectile = Main.projectile[CurrentLances[i].Value];
                        if (projectile != null && projectile.active && projectile.owner == Owner.whoAmI && projectile.ai[2] == 0 && projectile.ai[0] >= TheStarsAreAfraid.ChargeTime)
                        {
                            CurrentLances[i] = null;
                            projectile.ai[2] = 1;
                            projectile.netUpdate = true;
                            projectile.netSpam = 0;
                        }
                    }
                }

                Released = true;
                this.Sync();
            }

            Offset = (Offset + .01f) % MathHelper.TwoPi;
            Time++;
        }
        else
        {
            int type = ModContent.ProjectileType<StarWater>();
            int amt = Owner.CountOwnerProjectiles(type);

            float wait = Item.useTime * .1f;
            if (Time % wait == wait - 1 && amt < 30 && TryUseMana())
            {
                StarWater water = Main.projectile[Projectile.NewProj(Owner.Center, Vector2.Zero, type,
                    Projectile.damage, Projectile.knockBack, Owner.whoAmI)].As<StarWater>();
                water.Offset = Main.rand.NextVector2Circular(OffsetMax, OffsetMax);
            }

            List<Projectile> waters = Utility.AllProjectilesFromOwner(type, Owner);
            if ((this.RunLocal() && Modded.SafeMouseLeft.Current) && !Released && waters.Count > 0)
            {
                AdditionsSound.MagicStrike.Play(Projectile.Center, 1.2f, .2f, .1f, 20, Name);

                foreach (Projectile proj in waters)
                {
                    StarWater water = proj.As<StarWater>();
                    if (water != null && !water.Released && water.Completion >= 1f && water.Projectile.owner == Owner.whoAmI)
                    {
                        proj.ai[1] = 1f; // Release
                        proj.netUpdate = true;
                        proj.netSpam = 0;
                    }
                }
                Released = true;
                this.Sync();
            }
            Time++;
        }

        if (this.RunLocal() && !Modded.MouseLeft.Current)
        {
            Released = false;
            this.Sync();
        }
    }
    public override bool? CanDamage() => false;
    public override bool? CanCutTiles() => false;
    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D tex = Main.bloodMoon ? StarlessSea.Fracture : StarlessSea.Starless;
        Vector2 pos = Projectile.Center - Main.screenPosition;
        Vector2 orig = tex.Size() / 2;
        Main.spriteBatch.Draw(tex, pos, null, lightColor, Projectile.rotation, orig, Projectile.scale, FixedDirection(), 0f);
        return false;
    }
}
