using CalamityMod.Cooldowns;
using Terraria;
using Terraria.Localization;

namespace TheExtraordinaryAdditions.Content.Cooldowns;

public class RedMistCooldown : CooldownHandler
{
    public static new string ID => "RedMist";
    public override bool ShouldDisplay => true;
    public override LocalizedText DisplayName => GetText($"Cooldowns.{ID}");
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.CooldownRedMist);
    public override Color OutlineColor => new(17, 17, 17);
    public override Color CooldownStartColor => new(104, 4, 3);
    public override Color CooldownEndColor => new(254, 38, 36);
    public override void Tick()
    {
        Vector2 pos = instance.player.Center + new Vector2(-4f * instance.player.direction, -20f);
        ParticleRegistry.SpawnBloomPixelParticle(pos + Main.rand.NextVector2Circular(60f, 60f), Main.rand.NextVector2CircularEdge(2f, 2f),
            30, Main.rand.NextFloat(.4f, .5f) * instance.Completion, Color.DarkRed, Color.Crimson, pos, 1f, 5);
    }
}
