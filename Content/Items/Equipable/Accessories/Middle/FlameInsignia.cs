using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Classless.Middle;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Globals.ItemGlobal;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Equipable.Accessories.Middle;

public class FlameInsignia : ModItem, ILocalizedModType, IModType
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.FlameInsignia);
    public override void SetDefaults()
    {
        Item.width = 26;
        Item.height = 46;
        Item.value = AdditionsGlobalItem.RarityLightPurpleBuyPrice;
        Item.accessory = true;
        Item.rare = ItemRarityID.LightPurple;
    }

    public override void PostUpdate()
    {
        Lighting.AddLight(Item.Center, Color.OrangeRed.ToVector3() * .7f);
    }

    public override void UpdateAccessory(Player player, bool hideVisual)
    {
        player.GetModPlayer<FlameInsigniaPlayer>().Equipped = true;
        Lighting.AddLight(player.Center, Color.OrangeRed.ToVector3() * .7f);
        player.GetCritChance(DamageClass.Generic) += 10f;
    }
}

public sealed class FlameInsigniaPlayer : ModPlayer
{
    public bool Equipped;
    public override void ResetEffects() => Equipped = false;

    private const int radius = 180;
    private const int rays = 25;
    public override void PostUpdateMiscEffects()
    {
        if (!Equipped)
            return;

        for (int j = 0; j < rays; j++)
        {
            Vector2 pos = Player.Center + ((MathHelper.TwoPi * j / rays + RandomRotation()).ToRotationVector2() * radius);
            if (Collision.CanHitLine(pos, 1, 1, Player.Center, 1, 1))
            {
                float angularVelocity = Main.rand.NextFloat(0.045f, 0.09f);
                float scale = Main.rand.NextFloat(.4f, .7f);
                Vector2 vel = Player.velocity + (Player.velocity * -.2f);
                Color fireColor = MulticolorLerp(Main.rand.NextFloat(0.2f, 0.8f), Color.Red, Color.OrangeRed, Color.IndianRed, Color.Orange, Color.DarkOrange, Color.OrangeRed * 1.6f);
                ParticleRegistry.SpawnHeavySmokeParticle(pos, vel, Main.rand.Next(20, 24), scale, fireColor, 1f, true, angularVelocity);
                ParticleRegistry.SpawnGlowParticle(pos, vel + Vector2.UnitY.RotatedByRandom(.25f) * -Main.rand.NextFloat(1f, 4f), Main.rand.Next(20, 30), 50f * scale, fireColor, 1f);
            }
        }

        if (Player.Additions().GlobalTimer % 20f == 19f)
        {
            List<NPC> targets = NPCTargeting.GetNPCsClosestToFarthest(new(Player.Center, radius, true));
            if (targets.Count != 0)
            {
                for (int i = 0; i < targets.Count; i++)
                {
                    NPC target = targets[i];

                    if (!target.CanHomeInto())
                        continue;

                    if (i < 10)
                    {
                        bool active = target.active && target != null;
                        if (active && target.WithinRange(Player.Center, radius) && target.CanBeChasedBy(Player) && target.friendly == false)
                        {
                            int dmg = (int)Player.GetTotalDamage(Player.GetBestClass()).ApplyTo(25);
                            float kb = 0f;
                            int type = ModContent.ProjectileType<InsigniaBlaze>();
                            if (Main.myPlayer == Player.whoAmI)
                                Player.NewPlayerProj(target.Center, Vector2.Zero, type, dmg, kb, Player.whoAmI, target.whoAmI);
                        }
                    }
                }
            }
        }
    }
}