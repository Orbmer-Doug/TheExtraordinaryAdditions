using CalamityMod.Cooldowns;
using Terraria;
using Terraria.ID;
using Terraria.Localization;

namespace TheExtraordinaryAdditions.Content.Cooldowns;

public class SkullKaboomCooldown : CooldownHandler
{
    public static new string ID => "SkullKablooey";
    public override bool ShouldDisplay => true;
    public override LocalizedText DisplayName => GetText($"Cooldowns.{ID}");
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.CooldownSkullKablooey);
    public override Color OutlineColor => new(48, 47, 43);
    public override Color CooldownStartColor => new(66, 65, 47);
    public override Color CooldownEndColor => new(180, 180, 160);
    public override void OnCompleted()
    {
        SoundID.MaxMana.Play(instance.player.Center, 1.5f, -.2f, .1f, null, 1, "BoneWait");
        for (int i = 0; i < 20; i++)
        {
            ParticleRegistry.SpawnSparkParticle(instance.player.RandAreaInEntity(),
                -Vector2.UnitY * Main.rand.NextFloat(2f, 5f), Main.rand.Next(20, 30), Main.rand.NextFloat(.4f, .8f), Color.DarkGray);
        }
    }
}
