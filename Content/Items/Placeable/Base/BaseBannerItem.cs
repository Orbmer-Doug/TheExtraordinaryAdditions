using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Tiles;

namespace TheExtraordinaryAdditions.Content.Items.Placeable.Base;

public abstract class BaseBannerItem : ModItem, ILocalizedModType, IModType
{
    public virtual int BannerTileID => ModContent.TileType<MonsterBanner>();

    public virtual int BannerTileStyle => 0;

    public virtual int BannerKillRequirement => ItemID.Sets.DefaultKillsForBannerNeeded;

    public virtual int BonusNPCID => MonsterBanner.GetBannerNPC(BannerTileStyle);

    public override string LocalizationCategory => "Tiles.Placeables";

    public virtual LocalizedText NPCName => NPCLoader.GetNPC(BonusNPCID).DisplayName;

    public override void SetStaticDefaults()
    {
        ItemID.Sets.KillsToBanner[Type] = BannerKillRequirement;
    }

    public override void SetDefaults()
    {
        Item.DefaultToPlaceableTile(BannerTileID, BannerTileStyle);
        Item.width = 10;
        Item.height = 24;
        Item.rare = ItemRarityID.Blue;
        Item.value = Item.sellPrice(0, 0, 2, 0);
    }
}
