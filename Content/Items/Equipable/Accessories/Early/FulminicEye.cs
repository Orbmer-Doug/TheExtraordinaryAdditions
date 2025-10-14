using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Classless.Early;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Globals.ItemGlobal;
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
        player.GetModPlayer<FulminicEyePlayer>().Equipped = true;
    }
}

public sealed class FulminicEyePlayer : ModPlayer
{
    public bool Equipped;
    public override void ResetEffects() => Equipped = false;
    public override void PostUpdateMiscEffects()
    {
        if (!Equipped)
            return;

        NPC n = NPCTargeting.GetClosestNPC(new(Player.Center, 400, true));
        if (n.CanHomeInto() && Player.Additions().GlobalTimer % SecondsToFrames(1.25f) == 0f)
        {
            SoundEngine.PlaySound(SoundID.DD2_LightningAuraZap, Player.Center);
            Vector2 vel = Player.SafeDirectionTo(n.Center) * 10f;
            if (Main.myPlayer == Player.whoAmI)
                Player.NewPlayerProj(Player.Center, n.Center.ToRectangle(6, 6).RandomRectangle(), ModContent.ProjectileType<FulminicSpark>(), 30, 1f, Player.whoAmI);

            for (int i = 0; i < 12; i++)
                ParticleRegistry.SpawnSparkParticle(Player.Center, vel.RotatedByRandom(.22f) * Main.rand.NextFloat(.4f, .8f), 20, Main.rand.NextFloat(.3f, .5f), Color.Purple);
        }
    }
}