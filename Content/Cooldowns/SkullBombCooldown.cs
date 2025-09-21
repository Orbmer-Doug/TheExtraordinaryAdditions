using CalamityMod.Cooldowns;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Ranged.Early;

namespace TheExtraordinaryAdditions.Content.Cooldowns;

public class SkullBombCooldown : CooldownHandler
{
    public static new string ID => "SkullBomb";
    public override bool ShouldDisplay => true;
    public override LocalizedText DisplayName => GetText($"Cooldowns.{ID}");
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.CooldownSkullBomb);
    public override Color OutlineColor => new(45, 45, 29);
    public override Color CooldownStartColor => new(150, 150, 111);
    public override Color CooldownEndColor => new(221, 221, 188);
    public override void OnCompleted()
    {
        SoundID.MaxMana.Play(instance.player.Center, 1.5f, -.2f, .1f, null, 1, "BoneWait");
        for (int i = 0; i < 20; i++)
        {
            ParticleRegistry.SpawnSparkParticle(instance.player.RotHitbox().RandomPoint(),
                -Vector2.UnitY * Main.rand.NextFloat(2f, 4f), Main.rand.Next(20, 30), Main.rand.NextFloat(.3f, .5f), Color.Gray);
        }
    }

    public override bool CanTickDown => instance.player.ownedProjectileCounts[ModContent.ProjectileType<CalciumBomb>()] <= 0;
}
