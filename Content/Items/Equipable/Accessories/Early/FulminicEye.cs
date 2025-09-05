using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Equipable.Accessories.Early;

public class FulminicEye : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.FulminicEye);
    public override void SetStaticDefaults()
    {
        Item.ResearchUnlockCount = 1;
        Main.RegisterItemAnimation(Item.type, new DrawAnimationVertical(7, 6, false));
        ItemID.Sets.ItemNoGravity[Item.type] = true;
        ItemID.Sets.AnimatesAsSoul[Type] = true;
    }
    public override void SetDefaults()
    {
        Item.width = 52;
        Item.height = 48;
        Item.value = AdditionsGlobalItem.RarityGreenBuyPrice;
        Item.rare = ItemRarityID.Green;
        Item.accessory = true;
    }
    public int frameCounter;

    public int frame;
    public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
    {
        Texture2D texture = Item.ThisItemTexture();
        spriteBatch.Draw(texture, Item.position - Main.screenPosition, (Rectangle?)Item.GetCurrentFrame(ref frame, ref frameCounter, 9, 6), lightColor, 0f, Vector2.Zero, 1f, 0, 0f);

        Texture2D texture2 = AssetRegistry.GetTexture(AdditionsTexture.FulminicEye_Glow);
        spriteBatch.Draw(texture2, Item.position - Main.screenPosition, (Rectangle?)Item.GetCurrentFrame(ref frame, ref frameCounter, 9, 6), Color.White, 0f, Vector2.Zero, 1f, 0, 0f);
        return false;
    }
    public override void PostDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, float rotation, float scale, int whoAmI)
    {
        Texture2D texture = Item.ThisItemTexture();
        spriteBatch.Draw(texture, Item.position - Main.screenPosition, Item.GetCurrentFrame(ref frame, ref frameCounter, 9, 6, frameCounterUp: false), lightColor, 0f, Vector2.Zero, 1f, 0, 0f);

        Texture2D texture2 = AssetRegistry.GetTexture(AdditionsTexture.FulminicEye_Glow);
        spriteBatch.Draw(texture2, Item.position - Main.screenPosition, Item.GetCurrentFrame(ref frame, ref frameCounter, 9, 6, frameCounterUp: false), Color.White, 0f, Vector2.Zero, 1f, 0, 0f);
    }
    public override void UpdateAccessory(Player player, bool hideVisual)
    {
        player.moveSpeed += .1f;
        player.Additions().FulminicEye = true;
    }
}