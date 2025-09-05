using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Assets;
using TheExtraordinaryAdditions.Content.Buffs.Debuff;
using TheExtraordinaryAdditions.Content.Items.Weapons.Classless;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Utilities;
using static Terraria.ModLoader.BackupIO;

namespace TheExtraordinaryAdditions.UI.CrossUI;

public class ElementalBalance : ModPlayer
{
    public static SoundStyle OverloadSound = AssetRegistry.GetSound(AdditionsSound.AdrenalineMajorLoss);

    public int ElementalResourceCurrent; // Current value
    public const int DefaultElementalResourceMax = 200; // Default maximum value
    public int ElementalResourceMax; // Buffer variable that is used to reset maximum resource to default value in ResetDefaults().
    public int ElementalResourceMax2; // Maximum amount
    public float ElementalResourceRegenRate; // increase/decrease regeneration rate of our resource
    internal int ElementalResourceRegenTimer = 0; // required for timer
    public bool HoldingDisc;

    public float ElementalBarAlpha = 1f;
    public float ElementCompletion => InverseLerp(0f, ElementalResourceMax2, ElementalResourceCurrent);

    // - Multiplayer Syncing: The current Elemental doesn't require MP code, but pretty much any additional functionality will require this. ModPlayer.SendClientChanges and CopyClientState will be necessary, as well as SyncPlayer if you allow the user to increase ElementalResourceMax.
    // - Save/Load permanent changes to max resource: You'll need to implement Save/Load to remember increases to your ElementalResourceMax cap.
    // - Resouce replenishment item: Use GlobalNPC.OnKill to drop the item. ModItem.OnPickup and ModItem.ItemSpace will allow it to behave like Mana Star or Heart. Use code similar to Player.HealEffect to spawn (and sync) a colored number suitable to your resource.

    public override void Initialize()
    {
        ElementalResourceCurrent = 0;
        ElementalResourceMax = DefaultElementalResourceMax;
    }

    public override void ResetEffects()
    {
        ResetVariables();
    }

    public override void UpdateDead()
    {
        if (ElementalBarAlpha > 0f)
        {
            ElementalBarAlpha -= 0.035f;
            ElementalBarAlpha = MathHelper.Clamp(ElementalBarAlpha, 0f, 1f);
        }

        ResetVariables();
    }

    // We need this to ensure that regeneration rate and maximum amount are reset to default values after increasing when conditions are no longer satisfied (e.g. we unequip an accessory that increaces our recource)
    private void ResetVariables()
    {
        ElementalResourceRegenRate = 1f;
        ElementalResourceMax2 = ElementalResourceMax;
    }

    public override void PostUpdateMiscEffects()
    {
        if (ElementalBarAlpha < 1f)
        {
            ElementalBarAlpha = MathHelper.Lerp(ElementalBarAlpha, 1f, 0.035f);
        }

        if (Player.Additions().CircuitOverload > 0)
            Player.Additions().CircuitOverload--;

        ModItem p = Main.CurrentPlayer.HeldItem.ModItem;
        if (p != null && p is CrossDisc)
        {
            HoldingDisc = true;
        }

        UpdateResource();
    }

    public override void PostUpdate()
    {
        CapResourceGodMode();
    }

    // Lets do all our logic for the custom resource here, such as limiting it, increasing it and so on.
    private void UpdateResource()
    {
        // For our resource lets make it regen slowly over time to keep it simple, let's use ElementalResourceRegenTimer to count up to whatever value we want, then increase currentResource.
        ElementalResourceRegenTimer++; // Increase it by 60 per second, or 1 per tick.
        // A simple timer that goes up to 1 second, increases the ElementalResourceCurrent by 1 and then resets back to 0.
        if (ElementalResourceRegenTimer > 10 / ElementalResourceRegenRate)
        {
            ElementalResourceCurrent -= 1;
            ElementalResourceRegenTimer = 0;
        }

        // Limit
        ElementalResourceCurrent = Utils.Clamp(ElementalResourceCurrent, 0, ElementalResourceMax2);
    }

    private void CapResourceGodMode()
    {
        if (Main.myPlayer == Player.whoAmI && Player.creativeGodMode)
            ElementalResourceCurrent = 0;
    }
}