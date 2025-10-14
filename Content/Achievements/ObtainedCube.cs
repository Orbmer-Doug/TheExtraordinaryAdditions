using Terraria.Achievements;
using Terraria.GameContent.Achievements;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Items.Equipable.Accessories.Early;

namespace TheExtraordinaryAdditions.Content.Achievements;

public class ObtainedCube : ModAchievement
{
    public override string TextureName => AssetRegistry.GetTexturePath(AdditionsTexture.ObtainedCube);
    public ItemCraftCondition Condition { get; private set; }
    public override bool Hidden => !Condition.IsCompleted;
    public override void SetStaticDefaults()
    {
        Achievement.SetCategory(AchievementCategory.Collector);
        Condition = AddItemCraftCondition("OBTAINED_CUBE", ModContent.ItemType<TungstenCube>());
    }
    public override Position GetDefaultPosition() => new After("DUNGEON_HEIST");
}