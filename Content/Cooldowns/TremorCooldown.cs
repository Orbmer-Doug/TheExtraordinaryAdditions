using CalamityMod.Cooldowns;
using Terraria;
using Terraria.ID;
using Terraria.Localization;

namespace TheExtraordinaryAdditions.Content.Cooldowns;

public class TremorCooldown : CooldownHandler
{
    public static new string ID => "Tremor";
    public override bool ShouldDisplay => true;
    public override LocalizedText DisplayName => GetText($"Cooldowns.{ID}");
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.CooldownTremor);
    public override Color OutlineColor => new(30, 17, 25);
    public override Color CooldownStartColor => new(140, 131, 138);
    public override Color CooldownEndColor => new(177, 166, 174);
    public override void Tick()
    {
        Vector2 top = instance.player.Center + Vector2.UnitY * -10f;
        if (instance.timeLeft % 2f == 1f)
        {
            float size = 20f + instance.player.Size.Length();
            Vector2 stonePos = top + Main.rand.NextVector2CircularLimited(size, size, .5f, 1f);
            Vector2 stonevel = stonePos.SafeDirectionTo(top) * Main.rand.NextFloat(2f, 4f);
            Dust.NewDustPerfect(stonePos, DustID.Stone, stonevel, Main.rand.Next(60, 110), default, Main.rand.NextFloat(1f, 1.4f)).noGravity = true;
        }
        Lighting.AddLight(top, Color.Gray.ToVector3() * instance.Completion);
    }
}
