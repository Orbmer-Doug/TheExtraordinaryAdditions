using CalamityMod.Cooldowns;
using Terraria;
using Terraria.Localization;

namespace TheExtraordinaryAdditions.Content.Cooldowns;

public class MimicryCooldown : CooldownHandler
{
    public static new string ID => "Mimicry";
    public override bool ShouldDisplay => true;
    public override LocalizedText DisplayName => GetText($"Cooldowns.{ID}");
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.CooldownMimicry);
    public override Color OutlineColor => new(22, 3, 3);
    public override Color CooldownStartColor => new(68, 11, 11);
    public override Color CooldownEndColor => new(207, 166, 126);
    public override void Tick()
    {
        if (Main.rand.NextBool(6))
        {
            ParticleRegistry.SpawnPulseRingParticle(instance.player.RandAreaInEntity(), instance.player.velocity, 20, RandomRotation(), new(Main.rand.NextFloat(.4f, .8f), 1f), 0f, 60f, Color.Crimson, true);
        }
    }
}
