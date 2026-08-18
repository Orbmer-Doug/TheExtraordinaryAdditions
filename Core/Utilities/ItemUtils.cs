using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace TheExtraordinaryAdditions.Core.Utilities;

public static class ItemUtils
{
    extension(Item item)
    {
        /// <summary>
        /// A simple utility that gets an <see cref="Item"/>s <see cref="Item.ModItem"/> instance
        /// </summary>
        public T As<T>() where T : ModItem
        {
            return item.ModItem as T;
        }

        public Texture2D ThisItemTexture() =>
            TextureAssets.Item[item.type].Value;

        public Rectangle GetCurrentFrame(ref int frame, ref int frameCounter, int frameDelay,
            int frameAmt, bool frameCounterUp = true)
        {
            if (frameCounter >= frameDelay)
            {
                frameCounter = -1;
                frame = frame == frameAmt - 1 ? 0 : frame + 1;
            }

            if (frameCounterUp)
                frameCounter++;
            return new Rectangle(0, item.height * frame, item.width, item.height);
        }

        public void TreasureBagLightAndDust()
        {
            Lighting.AddLight(item.Center, Color.White.ToVector3() * 0.4f);

            if (item.timeSinceItemSpawned % 12 == 0)
            {
                Vector2 center = item.Center + new Vector2(0f, item.height * -0.1f);
                Vector2 direction = Main.rand.NextVector2CircularEdge(item.width * 0.6f, item.height * 0.6f);
                float distance = 0.3f + Main.rand.NextFloat() * 0.5f;
                Vector2 velocity = new Vector2(0f, -Main.rand.NextFloat() * 0.3f - 1.5f);

                Dust dust = Dust.NewDustPerfect(center + direction * distance, DustID.SilverFlame, velocity);
                dust.scale = 0.5f;
                dust.fadeIn = 1.1f;
                dust.noGravity = true;
                dust.noLight = true;
                dust.alpha = 0;
            }
        }

        public void Kill()
        {
            item.active = false;
            item.type = 0;
            item.stack = 0;
            if (Main.netMode != NetmodeID.SinglePlayer)
                NetMessage.SendData(MessageID.SyncItem, -1, -1, null, item.whoAmI);
        }

        public bool CheckManaBetter(Player player, int amount = -1, bool pay = false,
            bool blockQuickMana = false)
        {
            if (amount <= -1)
                amount = player.GetManaCost(item);

            if (player.statMana >= amount)
            {
                if (pay)
                {
                    CombinedHooks.OnConsumeMana(player, item, amount);
                    player.statMana -= amount;
                    player.manaRegenDelay = (int) player.maxRegenDelay;
                    player.manaRegen = 0;
                }

                return true;
            }

            if (blockQuickMana)
                return false;

            CombinedHooks.OnMissingMana(player, item, amount);
            if (player.statMana < amount && player.manaFlower)
                player.QuickMana();

            if (player.statMana >= amount)
            {
                if (pay)
                {
                    CombinedHooks.OnConsumeMana(player, item, amount);
                    player.statMana -= amount;
                    player.manaRegenDelay = (int) player.maxRegenDelay;
                }

                return true;
            }

            return false;
        }
    }

    #region Tooltips

    extension(List<TooltipLine> tooltips)
    {
        public void ColorLocalization(Color col, int lineToStart = 0)
        {
            var tooltiped = tooltips.Where(x => x.Name.Contains("Tooltip") && x.Mod == "Terraria");
            foreach (var tooltip in tooltiped)
            {
                int tooltipLineIndex = (int) char.GetNumericValue(tooltip.Name.Last());
                if (tooltipLineIndex >= lineToStart)
                    tooltip.OverrideColor = col;
            }
        }

        public void ModifyTooltip(TooltipLine[] NewTooltips,
            bool hideNormalTooltip = false)
        {
            int firstTooltipIndex = -1;
            int lastTooltipIndex = -1;
            int standardTooltipCount = 0;
            for (int i = 0; i < tooltips.Count; i++)
            {
                if (tooltips[i].Name.StartsWith("Tooltip"))
                {
                    if (firstTooltipIndex == -1)
                    {
                        firstTooltipIndex = i;
                    }

                    lastTooltipIndex = i;
                    standardTooltipCount++;
                }
            }

            // Replace tooltips.
            if (firstTooltipIndex != -1)
            {
                if (hideNormalTooltip)
                {
                    tooltips.RemoveRange(firstTooltipIndex, standardTooltipCount);
                    lastTooltipIndex -= standardTooltipCount;
                }

                tooltips.InsertRange(lastTooltipIndex + 1, NewTooltips);
            }
        }

        public void DrawHeldShiftTooltip(TooltipLine[] holdShiftTooltips,
            bool hideNormalTooltip = false)
        {
            // Do not override anything if the Left Shift key is not being held.
            if (!Main.keyState.IsKeyDown(Keys.LeftShift))
                return;
            tooltips.ModifyTooltip(holdShiftTooltips, hideNormalTooltip);
        }
    }

    public static void AddTooltips(ModItem item, string[] tooltips)
    {
        string supertip = "";
        for (int i = 0; i < tooltips.Length; i++)
        {
            supertip = supertip + tooltips[i] + ((i == tooltips.Length - 1) ? "" : "\n");
        }
    }

    extension(List<TooltipLine> lines)
    {
        public void DeleteTooltips() => lines.RemoveAll(l => l.Name.Contains("Tooltip"));

        public void FindAndReplace(string replacedKey, string newKey)
        {
            TooltipLine line = lines.FirstOrDefault(x => x.Mod == "Terraria" && x.Text.Contains(replacedKey));
            line?.Text = line.Text.Replace(replacedKey, newKey);
        }
    }

    public static string TooltipHotkeyString(this ModKeybind mhk)
    {
        if (Main.dedServ || mhk == null)
        {
            return "";
        }

        List<string> keys = mhk.GetAssignedKeys();
        if (keys.Count == 0)
        {
            return "[NONE]";
        }

        StringBuilder sb = new StringBuilder(16);
        sb.Append(keys[0]);
        for (int i = 1; i < keys.Count; i++)
        {
            sb.Append(" / ").Append(keys[i]);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Integrate a hotkey to let players know if they need to bind something.
    /// Put [KEY] or [KEY2] in the localization file to indicate the location
    /// </summary>
    /// <param name="tooltips">The tooltips</param>
    /// <param name="mhk">The keybind</param>
    /// <param name="whatToFindToReplaceWith">Typically something like [KEY]</param>
    public static void IntegrateHotkey(this List<TooltipLine> tooltips, ModKeybind mhk, string whatToFindToReplaceWith)
    {
        if (Main.dedServ || mhk == null)
            return;

        string finalKey = mhk.TooltipHotkeyString();
        tooltips.FindAndReplace(whatToFindToReplaceWith, finalKey);
    }

    #endregion Tooltips

    public static void CleanHoldStyle(Player player, float desiredRotation, Vector2 desiredPosition, Vector2 spriteSize,
        Vector2? rotationOriginFromCenter = null, bool noSandstorm = false, bool flipAngle = false,
        bool stepDisplace = true)
    {
        if (noSandstorm)
            player.sandStorm = false;

        rotationOriginFromCenter ??= Vector2.Zero;

        Vector2 origin = rotationOriginFromCenter.Value;
        origin.X *= player.direction;
        origin.Y *= player.gravDir;
        player.itemRotation = desiredRotation;
        if (flipAngle)
            player.itemRotation *= player.direction;
        else if (player.direction < 0)
            player.itemRotation += MathHelper.Pi;

        Vector2 consistentAnchor =
            player.itemRotation.ToRotationVector2() * (spriteSize.X / -2f - 10f) * player.direction -
            origin.RotatedBy(player.itemRotation);
        Vector2 offsetAgain = spriteSize * -0.5f;
        Vector2 finalPosition = desiredPosition + offsetAgain + consistentAnchor;
        if (stepDisplace)
        {
            int frame = player.bodyFrame.Y / player.bodyFrame.Height;
            if ((frame > 6 && frame < 10) || (frame > 13 && frame < 17))
            {
                finalPosition -= Vector2.UnitY * 2f;
            }
        }

        player.itemLocation = finalPosition + new Vector2(spriteSize.X * 0.5f, 0f);
    }
}
