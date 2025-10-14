using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace TheExtraordinaryAdditions.Content.Items.Materials.Middle;

public class CracklingFragments : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.CracklingFragments);

    public override void SetStaticDefaults()
    {
        Item.ResearchUnlockCount = 20;
        Main.RegisterItemAnimation(Item.type, new DrawAnimationVertical(3, 19, false));

        ItemID.Sets.AnimatesAsSoul[Type] = true;
    }

    public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
    {
        DrawInventoryCustomScale(spriteBatch, TextureAssets.Item[Type].Value, position, frame, drawColor, itemColor, origin, scale, 1f, new Vector2(0f, 0f));
        return false;
    }

    public override void SetDefaults()
    {
        Item.width = 37;
        Item.height = 32;
        Item.rare = ItemRarityID.Pink;

        Item.maxStack = Item.CommonMaxStack;
        Item.value = Item.buyPrice(silver: 60);
    }
}
