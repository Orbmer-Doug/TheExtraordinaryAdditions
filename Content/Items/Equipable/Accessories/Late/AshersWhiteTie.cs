using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Buffs.Debuff;
using TheExtraordinaryAdditions.Content.Projectiles.Classless.Late;
using TheExtraordinaryAdditions.Content.Rarities.AdditionRarities;
using TheExtraordinaryAdditions.Core.Globals.ItemGlobal;

namespace TheExtraordinaryAdditions.Content.Items.Equipable.Accessories.Late;

[AutoloadEquip(EquipType.Neck)]
public class AshersWhiteTie : ModItem, ILocalizedModType, IModType
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.AshersWhiteTie);
    public override void SetDefaults()
    {
        Item.width = 16;
        Item.height = 14;
        Item.accessory = true;
        Item.value = AdditionsGlobalItem.UniqueRarityPrice;
        Item.rare = ModContent.RarityType<UniqueRarity>();
    }

    public override void PostUpdate()
    {
        Lighting.AddLight(Item.position, Color.White.ToVector3() * 2f);
    }

    public override void UpdateAccessory(Player player, bool hideVisual)
    {
        if (!player.GetModPlayer<TungstenTiePlayer>().Equipped)
            player.GetModPlayer<AshersWhiteTiePlayer>().Equipped = true;

        if (!Main.rand.NextBool(15))
            return;

        int tie = ModContent.ProjectileType<SharpTie>();
        int owned = player.CountOwnerProjectiles(tie);
        if (owned >= 10)
            return;

        for (int j = 0; j < 50; j++)
        {
            int area = Main.rand.Next(200 - j * 2, 400 + j * 2);
            Vector2 center = player.Center;
            center.X += Main.rand.Next(-area, area + 1) + 12;
            center.Y += Main.rand.Next(-area, area + 1) + 12;
            if (!Collision.CanHit(new Vector2(player.Center.X, player.position.Y), 1, 1, center, 1, 1)
                && !Collision.CanHit(new Vector2(player.Center.X, player.position.Y - 50f), 1, 1, center, 1, 1))
                continue;

            if (Main.myPlayer == player.whoAmI)
            {
                int damage = (int)player.GetDamage(DamageClass.Generic).ApplyTo(250f);
                Projectile.NewProjectileDirect(player.GetSource_Accessory(Item, null), center, Vector2.Zero,
                    tie, damage, 3f, player.whoAmI);
                for (int i = 0; i < 40; i++)
                {
                    float offsetAngle = Main.rand.NextFloat(MathHelper.TwoPi);
                    Vector2 shootVelocity = (MathHelper.TwoPi * i / 10f + offsetAngle).ToRotationVector2() * 4f;
                    Dust dust = Dust.NewDustPerfect(center, DustID.AncientLight, shootVelocity, default, default, 1.6f);
                    dust.noGravity = true;
                }
                break;
            }
        }
    }
}

public sealed class AshersWhiteTiePlayer : ModPlayer
{
    public bool Equipped;
    public override void ResetEffects() => Equipped = false;

    public override bool PreKill(double damage, int hitDirection, bool pvp, ref bool playSound, ref bool genDust, ref PlayerDeathReason damageSource)
    {
        if (Equipped && !Player.HasBuff<TheTiesCooldown>() && !Player.GetModPlayer<TungstenTiePlayer>().Equipped)
        {
            AdditionsSound.etherealNuhUh.Play(Player.Center);
            for (int l = 0; l < 50; l++)
            {
                Vector2 vel = Main.rand.NextVector2CircularLimited(10f, 10f, .7f, 1f) * Main.rand.NextFloat(.6f, 1f);
                ParticleRegistry.SpawnGlowParticle(Player.Center, vel, 30, Main.rand.NextFloat(.5f, .8f), Color.FloralWhite);
            }
            ParticleRegistry.SpawnThunderParticle(Player.Center, 134, 1f, new(1f), 0f, Color.WhiteSmoke);

            Player.Heal(100);
            if (Player.statLife > Player.statLifeMax2)
                Player.statLife = Player.statLifeMax2;

            Player.AddBuff(ModContent.BuffType<TheTiesCooldown>(), SecondsToFrames(270));
            return false;
        }

        return base.PreKill(damage, hitDirection, pvp, ref playSound, ref genDust, ref damageSource);
    }
}