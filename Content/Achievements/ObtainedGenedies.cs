using Terraria.Achievements;
using Terraria.GameContent.Achievements;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Items.Weapons.Cynosure;

namespace TheExtraordinaryAdditions.Content.Achievements;

public class ObtainedGenedies : ModAchievement
{
    public override string TextureName => AssetRegistry.GetTexturePath(AdditionsTexture.ObtainedGenedies);
    public ItemCraftCondition Condition { get; private set; }
    public override bool Hidden => !Condition.IsCompleted;
    public override void SetStaticDefaults()
    {
        Achievement.SetCategory(AchievementCategory.Collector);
        Condition = AddItemCraftCondition("OBTAINED_GENEDIES", ModContent.ItemType<Exingenedies>());
    }
}