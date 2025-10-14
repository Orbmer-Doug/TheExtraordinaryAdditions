using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;
using static Terraria.ModLoader.BackupIO;

namespace TheExtraordinaryAdditions.UI.CrossUI;

public class ElementalBalance : ModPlayer
{
    public const int DefaultElementalResourceMax = 200;
    public static readonly SoundStyle OverloadSound = AssetRegistry.GetSound(AdditionsSound.AdrenalineMajorLoss);

    public int ElementalResourceCurrent;
    public int Max;
    public int MaxAmount;
    public float ElementalResourceRegenRate;
    internal int ElementalResourceRegenTimer = 0;
    public int CircuitOverload;

    public float ElementalBarAlpha = 1f;
    public float ElementCompletion => InverseLerp(0f, MaxAmount, ElementalResourceCurrent);

    public override void Initialize()
    {
        ElementalResourceCurrent = 0;
        Max = DefaultElementalResourceMax;
    }

    public override void ResetEffects()
    {
        ElementalResourceRegenRate = 1f;
        MaxAmount = Max;
    }

    public override void UpdateDead()
    {
        if (ElementalBarAlpha > 0f)
        {
            ElementalBarAlpha -= 0.035f;
            ElementalBarAlpha = MathHelper.Clamp(ElementalBarAlpha, 0f, 1f);
        }

        ElementalResourceRegenRate = 1f;
        MaxAmount = Max;
    }

    public override void PostUpdateMiscEffects()
    {
        if (ElementalBarAlpha < 1f)
            ElementalBarAlpha = MathHelper.Lerp(ElementalBarAlpha, 1f, 0.035f);

        if (CircuitOverload > 0)
            CircuitOverload--;
        if (ElementCompletion <= 0)
            CircuitOverload = 0;

        ElementalResourceRegenTimer++;
        if (ElementalResourceRegenTimer > 10 / ElementalResourceRegenRate)
        {
            ElementalResourceCurrent -= 1;
            ElementalResourceRegenTimer = 0;
        }
        ElementalResourceCurrent = Utils.Clamp(ElementalResourceCurrent, 0, MaxAmount);
    }

    public override void PostUpdate()
    {
        if (Main.myPlayer == Player.whoAmI && Player.creativeGodMode)
            ElementalResourceCurrent = 0;
    }
}