using CalamityMod;
using CalamityMod.Buffs.StatBuffs;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using ReLogic.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Terraria;
using Terraria.Audio;
using Terraria.Chat;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.UI.BigProgressBar;
using Terraria.GameInput;
using Terraria.Graphics.Light;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.Core;
using TheExtraordinaryAdditions.Core.DataStructures;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Netcode;
using static TheExtraordinaryAdditions.Core.Graphics.Animators;

namespace TheExtraordinaryAdditions.Core.Utilities;

public static partial class Utility
{
    public static bool RunServer(this ModProjectile mod) => Main.netMode != NetmodeID.MultiplayerClient;
    public static bool RunLocal(this ModProjectile mod) => Main.myPlayer == mod.Projectile.owner;
    public static void Sync(this ModProjectile mod)
    {
        mod.Projectile.netUpdate = true;
        mod.Projectile.netSpam = 0;
    }

    /// <summary>
    /// Spawning projectiles or npcs, randomness (remember to sync under randoms)
    /// </summary>
    public static bool RunServer(this ModNPC mod) => Main.netMode != NetmodeID.MultiplayerClient;

    /// <summary>
    /// e.g. this npc dying, adding a buff to the player...
    /// </summary>
    public static bool RunClient(this ModNPC mod) => Main.netMode != NetmodeID.Server;

    /// <summary>
    /// Sudden shifts in position, state changes/variable updates, persistent position movements (like a dash) <br></br>
    /// <b>The server is in charge of NPCs, changes to NPC data should only happen on the server in multiplayer</b>
    /// </summary>
    public static void Sync(this ModNPC mod)
    {
        mod.NPC.netUpdate = true;
        mod.NPC.netSpam = 0;
    }

    public static int EstimateLightRadius(Vector3 lightColor, LightMaskMode medium = LightMaskMode.None,
    float minIntensityThreshold = 0.0185f, int maxRadius = 15)
    {
        float maxIntensity = Math.Max(Math.Max(lightColor.X, lightColor.Y), lightColor.Z);
        if (maxIntensity <= 0)
            return 0;

        var decayRate = medium switch
        {
            LightMaskMode.Solid => 0.56f, // LightDecayThroughSolid
            LightMaskMode.Water => 0.88f * 0.91f, // Min of LightDecayThroughWater (R channel), avg random factor ~0.99
            LightMaskMode.Honey => 0.6f * 0.91f, // Min of LightDecayThroughHoney (B channel)
            _ => 0.91f, // LightDecayThroughAir
        };

        // Calculate steps for two-pass blur: intensity * decay^(2r) = threshold
        int steps = (int)Math.Ceiling(Math.Log(minIntensityThreshold / maxIntensity) / Math.Log(decayRate));
        int radius = steps / 2; // Two passes, so radius is half the total steps

        return Math.Min(Math.Max(0, radius), maxRadius);
    }

    public static float CalculateIntensityForRadius(float desiredRadius, LightMaskMode medium = LightMaskMode.None, float edgeIntensity = 0.5f)
    {
        float decayRate = medium switch
        {
            LightMaskMode.None => 0.91f,
            LightMaskMode.Solid => 0.56f,
            LightMaskMode.Water => 0.93f,
            LightMaskMode.Honey => 0.66f,
            _ => 0.91f
        };

        return edgeIntensity / (float)Math.Pow(decayRate, desiredRadius);
    }

    public static string GetTerrariaItem(this int id) => "Terraria/Images/Item_" + id;
    public static string GetTerrariaProj(this int id) => "Terraria/Images/Projectile_" + id;
    public static string GetTerrariaNPC(this int id) => "Terraria/Images/NPC_" + id;
    public static string GetTerrariaItem(this short id) => "Terraria/Images/Item_" + id;
    public static string GetTerrariaProj(this short id) => "Terraria/Images/Projectile_" + id;
    public static string GetTerrariaNPC(this short id) => "Terraria/Images/NPC_" + id;

    public static T GetEnumValue<T>(int index) where T : Enum
    {
        Array values = Enum.GetValues(typeof(T));
        return (T)values.GetValue(index - 1);
    }

    public static T GetLastEnumValue<T>() where T : Enum
    {
        Array values = Enum.GetValues(typeof(T));
        return (T)values.GetValue(values.Length - 1);
    }

    public static SlotId Play(this AdditionsSound sound, Vector2 position, float volume = 1f, float pitch = 0f,
        float pitchVariance = 0f, int maxInstances = 1, string identifier = null, PauseBehavior behavior = PauseBehavior.KeepPlaying)
        => Play(AssetRegistry.GetSound(sound), position, volume, pitch, pitchVariance, null, maxInstances, identifier);

    public static SlotId Play(this SoundStyle style, Vector2 position, float volume = 1f, float pitch = 0f,
        float pitchVariance = 0f, (float, float)? pitchRange = null, int maxInstances = 1, string identifier = null, PauseBehavior behavior = PauseBehavior.KeepPlaying)
    {
        SoundStyle sound = style;
        sound.Volume = volume;
        sound.Pitch = pitch;
        if (pitchRange != null)
            sound.PitchRange = (pitchRange.Value.Item1, pitchRange.Value.Item2);
        sound.PitchVariance = pitchVariance;
        sound.MaxInstances = maxInstances;
        sound.Identifier = identifier;
        sound.PauseBehavior = behavior;

        return SoundEngine.PlaySound(sound, position);
    }

    public static SlotId Play(this Dictionary<AdditionsSound, float> styles, Vector2 position, float volume = 1f, float pitch = 0f,
    float pitchVariance = 0f, (float, float)? pitchRange = null, int maxInstances = 1, string identifier = null, PauseBehavior behavior = PauseBehavior.KeepPlaying)
    {
        SoundStyle sound = AssetRegistry.GetSound(new WeightedDict<AdditionsSound>(styles).GetRandom());
        sound.Volume = volume;
        sound.Pitch = pitch;
        if (pitchRange != null)
            sound.PitchRange = (pitchRange.Value.Item1, pitchRange.Value.Item2);
        sound.PitchVariance = pitchVariance;
        sound.MaxInstances = maxInstances;
        sound.Identifier = identifier;
        sound.PauseBehavior = behavior;
        return SoundEngine.PlaySound(sound, position);
    }

    public static float UsedMinions(this Player player, int? ofType = null)
    {
        float usedMinions = 0;
        foreach (Projectile p in Main.ActiveProjectiles)
        {
            if (ofType != null)
            {
                if (p.type != ofType.Value)
                    continue;
            }
            if (p != null && p.minion && p.owner == player.whoAmI)
                usedMinions += p.minionSlots;
        }
        return usedMinions;
    }

    /// <summary>
    /// why was it private
    /// </summary>
    /// <param name="proj">The projectile</param>
    /// <param name="index">The index of this projectile in the group</param>
    /// <param name="totalIndexesInGroup">The total amount of projectiles in the group</param>
    public static void AI_GetMyGroupIndex(this Projectile proj, out int index, out int totalIndexesInGroup)
    {
        index = 0;
        totalIndexesInGroup = 0;
        for (int i = 0; i < Main.maxProjectiles; i++)
        {
            Projectile projectile = Main.projectile[i];
            if (projectile != null && projectile.active && projectile.owner == proj.owner && projectile.type == proj.type)
            {
                if (proj.whoAmI > i)
                    index++;

                totalIndexesInGroup++;
            }
        }
    }

    public static bool ShouldConsumeAmmo(this Player player, Item item) => player.IsAmmoFreeThisShot(item, player.ChooseAmmo(item), player.ChooseAmmo(item).type);
    public static bool Available(this Player player) => player != null && player.active && !player.dead && !player.ghost && !player.CCed && !player.noItems;

    private class ExplosionProjectile : ModProjectile
    {
        public override string Texture => AssetRegistry.Invis;
        public override void SetDefaults()
        {
            Projectile.timeLeft = 10;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.damage = 0;
            Projectile.knockBack = 0f;
            Projectile.penetrate = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.ignoreWater = false;
            Projectile.tileCollide = false;
            Projectile.extraUpdates = 0;
            Projectile.netImportant = true;
        }

        public Color Light;
        public Vector2 Size;
        public Vector2? ToSize;
        public bool Friendly;
        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.WriteRGB(Light);
            writer.WriteVector2(Size);
            if (ToSize.HasValue && ToSize != null)
                writer.WriteVector2(ToSize.Value);
        }
        public override void ReceiveExtraAI(BinaryReader reader)
        {
            Light = reader.ReadRGB();
            Size = reader.ReadVector2();
            if (ToSize.HasValue && ToSize != null)
                ToSize = reader.ReadVector2();
        }
        public ref float Lifetime => ref Projectile.ai[0];
        public override void AI()
        {
            if (Friendly)
            {
                Projectile.friendly = true;
                Projectile.hostile = false;
            }
            else
            {
                Projectile.friendly = false;
                Projectile.hostile = true;
            }

            float completion = Circ.OutFunction(1f - InverseLerp(0f, Lifetime, Projectile.timeLeft));

            if (ToSize.HasValue && ToSize != null)
            {
                Projectile.Resize((int)MathHelper.Lerp(Size.X, ToSize.Value.X, completion), (int)MathHelper.Lerp(Size.Y, ToSize.Value.Y, completion));
            }
            else
            {
                Projectile.Resize((int)Size.X, (int)Size.Y);
            }

            Lighting.AddLight(Projectile.Center, (new Color(Light.R, Light.G, Light.B) * Light.A * completion).ToVector3());
        }
    }

    public static void CreateExplosion(IEntitySource source, DamageClass dmgClass, Vector2 position, Vector2 size, int damage, float kb, int lifetime, int iframes, int owner = -1, bool friendly = true, Vector2? toSize = null, Color light = default, string name = "")
    {
        Projectile proj = Main.projectile[Projectile.NewProjectile(source, position, Vector2.Zero, ModContent.ProjectileType<ExplosionProjectile>(), damage, kb, owner)];
        ExplosionProjectile explosion = proj.As<ExplosionProjectile>();

        explosion.Friendly = friendly;
        explosion.Lifetime = lifetime;
        explosion.Light = light;
        explosion.Size = size;
        explosion.ToSize = toSize;

        proj.Name = name + " " + proj.ModProjectile.GetLocalization("DisplayName");
        proj.localNPCHitCooldown = iframes;
        proj.timeLeft = lifetime;
        proj.DamageType = dmgClass;
        proj.netUpdate = true;
    }

    public static void CreateFriendlyExplosion(this Projectile proj, Vector2 pos, Vector2 size, int dmg, float kb, int life, int iframes, Vector2? toSize = null, Color light = default)
    {
        if (Main.LocalPlayer == Main.player[proj.owner])
            CreateExplosion(proj.GetSource_FromThis(), proj.DamageType, pos, size, dmg, kb, life, iframes, proj.owner, true, toSize, light, proj.Name);
    }

    public static string ToHexRGB(this Color color) => BitConverter.ToString([color.R, color.G, color.B]).Replace("-", "");
    public static string ToHexRGBA(this Color color) => BitConverter.ToString([color.R, color.G, color.B, color.A]).Replace("-", "");
    public static string ColoredText(this string text, Color color)
        => $"[c/{color.ToHexRGB()}:{text}]";

    public static bool OnGround(this Player player)
        => player.velocity.Y == 0f;
    public static bool WasOnGround(this Player player)
        => player.oldVelocity.Y == 0f;

    public static bool GetKey(this Keys key)
        => !PlayerInput.WritingText && Main.hasFocus && Main.keyState.IsKeyDown(key);

    public static bool GetKeyDown(this Keys key)
        => !PlayerInput.WritingText && Main.hasFocus && Main.keyState.IsKeyDown(key) && !Main.oldKeyState.IsKeyDown(key);

    public static bool GetKeyUp(this Keys key)
        => !PlayerInput.WritingText && Main.hasFocus && !Main.keyState.IsKeyDown(key) && Main.oldKeyState.IsKeyDown(key);

    public static ILCursor HijackIncomingLabels(this ILCursor cursor)
    {
        ILLabel[] array = cursor.IncomingLabels.ToArray();
        cursor.Emit(OpCodes.Nop);
        ILLabel[] array2 = array;
        for (int i = 0; i < array2.Length; i++)
            array2[i].Target = cursor.Prev;
        return cursor;
    }

    public static void Kill(this Item item)
    {
        item.active = false;
        item.type = 0;
        item.stack = 0;
        if (Main.netMode != NetmodeID.SinglePlayer)
            NetMessage.SendData(MessageID.SyncItem, -1, -1, null, item.whoAmI);
    }

    public static void Kill(this NPC npc)
    {
        if (npc.ModNPC?.CheckDead() == false)
            return;

        npc.life = 0;
        npc.checkDead();
        npc.HitEffect();
        npc.active = false;
        if (Main.netMode != NetmodeID.SinglePlayer)
            NetMessage.SendData(MessageID.SyncNPC, number: npc.whoAmI);
    }

    public static void Log(this string message) => AdditionsMain.Instance?.Logger.Info(" " + message);
    public static void Warn(this string message) => AdditionsMain.Instance?.Logger.Warn(" " + message);
    public static void ServerLog(this string message)
    {
        DateTime time = System.DateTime.Now;
        Console.WriteLine($"[TEA] [{time.Hour}.{time.Minute}.{time.Second}.{time.Millisecond}.{time.Microsecond}]: {message}");
    }

    public static bool CheckManaBetter(this Item item, Player player, int amount = -1, bool pay = false, bool blockQuickMana = false)
    {
        if (amount <= -1)
            amount = player.GetManaCost(item);

        if (player.statMana >= amount)
        {
            if (pay)
            {
                CombinedHooks.OnConsumeMana(player, item, amount);
                player.statMana -= amount;
                player.manaRegenDelay = (int)player.maxRegenDelay;
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
                player.manaRegenDelay = (int)player.maxRegenDelay;
            }

            return true;
        }

        return false;
    }

    public static int GetFreeInventorySlot(Player plr)
    {
        for (int k = 0; k < 49; k++)
        {
            Item item = plr.inventory[k];

            if (item is null || item.IsAir)
                return k;
        }

        return -1;
    }

    #region Tooltips
    public static void ColorLocalization(this List<TooltipLine> tooltips, Color col, int lineToStart = 0)
    {
        var tooltiped = tooltips.Where(x => x.Name.Contains("Tooltip") && x.Mod == "Terraria");
        foreach (var tooltip in tooltiped)
        {
            int tooltipLineIndex = (int)char.GetNumericValue(tooltip.Name.Last());
            if (tooltipLineIndex >= lineToStart)
                tooltip.OverrideColor = col;
        }
    }
    public static void ModifyTooltip(this List<TooltipLine> tooltips, TooltipLine[] NewTooltips, bool hideNormalTooltip = false)
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

    public static void DrawHeldShiftTooltip(this List<TooltipLine> tooltips, TooltipLine[] holdShiftTooltips, bool hideNormalTooltip = false)
    {
        // Do not override anything if the Left Shift key is not being held.
        if (!Main.keyState.IsKeyDown(Keys.LeftShift))
            return;
        ModifyTooltip(tooltips, holdShiftTooltips, hideNormalTooltip);
    }

    public static void AddTooltips(ModItem item, string[] tooltips)
    {
        string supertip = "";
        for (int i = 0; i < tooltips.Length; i++)
        {
            supertip = supertip + tooltips[i] + ((i == tooltips.Length - 1) ? "" : "\n");
        }
    }

    public static void DeleteTooltips(this List<TooltipLine> lines) => lines.RemoveAll(l => l.Name.Contains("Tooltip"));

    public static void FindAndReplace(this List<TooltipLine> tooltips, string replacedKey, string newKey)
    {
        TooltipLine line = tooltips.FirstOrDefault((TooltipLine x) => x.Mod == "Terraria" && x.Text.Contains(replacedKey));
        if (line != null)
        {
            line.Text = line.Text.Replace(replacedKey, newKey);
        }
    }

    public static string TooltipHotkeyString(this ModKeybind mhk)
    {
        if (Main.dedServ || mhk == null)
        {
            return "";
        }
        List<string> keys = mhk.GetAssignedKeys(0);
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
        if (!Main.dedServ && mhk != null)
        {
            string finalKey = mhk.TooltipHotkeyString();
            tooltips.FindAndReplace(whatToFindToReplaceWith, finalKey);
        }
    }

    #endregion Tooltips

    /// <summary>
    /// A simple utility that gets an <see cref="Projectile"/>s <see cref="Projectile.ModProjectile"/> instance
    /// </summary>
    /// <typeparam name="T">The ModProjectile type to convert to</typeparam>
    /// <param name="p">The Projectile to access the ModProjectile from</param>
    public static T As<T>(this Projectile p) where T : ModProjectile
    {
        return p.ModProjectile as T;
    }

    /// <summary>
    /// A simple utility that gets an <see cref="NPC"/>s <see cref="NPC.ModNPC"/> instance
    /// </summary>
    /// <typeparam name="T">The ModNPC type to convert to</typeparam>
    /// <param name="npc">The NPC to access the ModNPC from</param>
    public static T As<T>(this NPC npc) where T : ModNPC
    {
        return npc?.ModNPC as T;
    }

    /// <summary>
    /// A simple utility that gets an <see cref="Item"/>s <see cref="Item.ModItem"/> instance
    /// </summary>
    /// <typeparam name="T">The ModNPC type to convert to</typeparam>
    /// <param name="item">The NPC to access the ModNPC from</param>
    public static T As<T>(this Item item) where T : ModItem
    {
        return item.ModItem as T;
    }

    /// <summary>
    /// Completely hide a npc from the bestiary
    /// </summary>
    public static void ExcludeFromBestiary(this ModNPC npc)
    {
        NPCID.Sets.NPCBestiaryDrawModifiers value = new()
        {
            Hide = true
        };
        NPCID.Sets.NPCBestiaryDrawOffset.Add(npc.Type, value);
    }

    /// <summary>
    /// Must be used every frame to sustain the close
    /// </summary>
    public static void MakeCalamityBossBarClose(this NPC npc)
    {
        if (Main.gameMenu)
            return;
        npc.Calamity().ShouldCloseHPBar = true;
    }

    public static void GrantBossEffectsBuff(this Player p) => p.AddBuff(ModContent.BuffType<BossEffects>(), 2);
    public static void GrantInfiniteFlight(this Player p) => p.Calamity().infiniteFlight = true;

    #region Spawning

    /// <summary>
    /// Make a new projectile from a source of a player
    /// </summary>
    public static int NewPlayerProj(this Player player, Vector2 center, Vector2 velocity, int type, int damage, float knockback, int owner = -1,
        float ai0 = 0f, float ai1 = 0f, float ai2 = 0f, float extra0 = 0f, float extra1 = 0f)
    {
        IEntitySource source = player.GetSource_FromThis();
        int index = Projectile.NewProjectile(source, center, velocity, type, damage, knockback, owner, ai0, ai1, ai2);
        Projectile projectile = Main.projectile[index];
        if (index >= 0 && index < Main.maxProjectiles)
            projectile.netUpdate = true;

        if (projectile.ModProjectile != null && projectile.ModProjectile.Mod == AdditionsMain.Instance)
        {
            projectile.AdditionsInfo().ExtraAI[0] = extra0;
            projectile.AdditionsInfo().ExtraAI[1] = extra1;
        }
        return index;
    }

    /// <summary>
    /// Make a new projectile from a source of a projectile
    /// </summary>
    public static int NewProj(this Projectile proj, Vector2 center, Vector2 velocity, int type, int damage, float knockback, int owner = -1,
        float ai0 = 0f, float ai1 = 0f, float ai2 = 0f, float extra0 = 0f, float extra1 = 0f)
    {
        IEntitySource source = proj.GetSource_FromThis();
        int index = Projectile.NewProjectile(source, center, velocity, type, damage, knockback, owner, ai0, ai1, ai2);
        Projectile projectile = Main.projectile[index];
        if (index >= 0 && index < Main.maxProjectiles)
            projectile.netUpdate = true;

        if (projectile.ModProjectile != null && projectile.ModProjectile.Mod == AdditionsMain.Instance)
        {
            projectile.AdditionsInfo().ExtraAI[0] = extra0;
            projectile.AdditionsInfo().ExtraAI[1] = extra1;
        }
        return index;
    }

    /// <summary>
    /// Spawns a projectile from this NPC <br></br>
    /// Automatically assigns the relationship between the NPC and projectile, assuming it is a <see cref="ProjOwnedByNPC{T}"/>
    /// </summary>
    /// <param name="damage">Automatically fixes damage from current difficulty</param>
    /// <returns>The index within <see cref="Main.projectile"/></returns>
    public static int NewNPCProj(this NPC npc, Vector2 position, Vector2 velocity, int type, int damage, float knockback,
        float ai0 = 0f, float ai1 = 0f, float ai2 = 0f, float extra0 = 0f, float extra1 = 0f)
    {
        damage = FixDamageFromDifficulty(damage);

        int index = Projectile.NewProjectile(npc.GetSpawnSource_ForProjectile(), position.X, position.Y,
            velocity.X, velocity.Y, type, damage, knockback, Main.myPlayer, ai0, ai1, ai2);
        if (index >= 0 && index < Main.maxProjectiles)
        {
            Projectile projectile = Main.projectile[index];
            if (projectile.ModProjectile != null && projectile.ModProjectile.Mod == AdditionsMain.Instance)
            {
                projectile.AdditionsInfo().ExtraAI[0] = extra0;
                projectile.AdditionsInfo().ExtraAI[1] = extra1;
            }

            projectile.localAI[0] = npc.whoAmI;
            if (Main.netMode != NetmodeID.SinglePlayer)
                NetMessage.SendData(MessageID.SyncProjectile, -1, -1, null, index);
        }

        return index;
    }

    /// <summary>
    /// Spawns a new projectile from this NPC <br></br>
    /// Use <see cref="NewNPCProj(NPC, Vector2, Vector2, int, int, float, float, float, float, float, float)"/> if the projectile should have an owner
    /// </summary>
    public static int Shoot(this NPC proj, Vector2 center, Vector2 velocity, int type, int damage, float knockback, int owner = -1, float ai0 = 0f, float ai1 = 0f, float ai2 = 0f)
    {
        IEntitySource source = proj.GetSource_FromThis();
        int projectile = Projectile.NewProjectile(source, center, velocity, type, damage, knockback, owner, ai0, ai1, ai2);
        Projectile p = Main.projectile[projectile];
        if (projectile >= 0 && projectile < Main.maxProjectiles)
            p.netUpdate = true;
        return projectile;
    }

    public static int NewNPCBetter(this NPC npc, Vector2 pos, Vector2 vel, int type, int start = 0, float ai0 = 0f, float ai1 = 0f, float ai2 = 0f, float ai3 = 0f, int target = -1)
    {
        int index = NPC.NewNPC(npc.GetSpawnSourceForNPCFromNPCAI(), (int)pos.X, (int)pos.Y, type, start, ai0, ai1, ai2, ai3, target);

        if (index >= 0 && index < Main.maxNPCs)
        {
            NPC n = Main.npc[index];
            n.velocity = vel;

            if (Main.netMode == NetmodeID.MultiplayerClient)
                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, index);
            n.netUpdate = true;
        }

        return index;
    }

    #endregion

    public static Entity GetTarget(this NPC npc)
    {
        if (!npc.HasValidTarget)
            return null;

        return npc.HasPlayerTarget ? Main.player[npc.target] : Main.npc[npc.target - 300];
    }

    /// <summary>
    /// Set the animation for a projectile
    /// </summary>
    /// <param name="proj">This projectile</param>
    /// <param name="frames">Total frames this projectile has and what should be cycled through</param>
    /// <param name="ticksPerFrame">How many frames to wait before going to the next frame</param>
    /// <param name="pingPong">Goes back and forth</param>
    /// <returns>The current frame</returns>
    public static int SetAnimation(this Projectile proj, int frames, int ticksPerFrame, bool pingPong = false)
    {
        proj.frameCounter++;

        if (!pingPong)
        {
            if (proj.frameCounter % ticksPerFrame == ticksPerFrame - 1)
                proj.frame = (proj.frame + 1) % frames;
            return proj.frame;
        }

        // forward + backward
        int cycleLength = (frames * 2 - 2) * ticksPerFrame;

        if (proj.frameCounter >= cycleLength)
            proj.frameCounter = 0;

        if (proj.frameCounter % ticksPerFrame == ticksPerFrame - 1)
        {
            int cyclePosition = proj.frameCounter / ticksPerFrame;

            // Forward movement
            if (cyclePosition < frames)
                proj.frame = cyclePosition;

            // Backward movement
            else
                proj.frame = frames * 2 - 2 - cyclePosition;

            proj.frame = Math.Clamp(proj.frame, 0, frames - 1);
        }

        return proj.frame;
    }

    public static IBigProgressBar HideBossBar(NPC npc)
    {
        return npc.BossBar = Main.BigBossProgressBar.NeverValid;
    }

    public static NetworkText GetNetworkText(string key, params object[] substitutions) =>
        NetworkText.FromKey("Mods.TheExtraordinaryAdditions." + key, substitutions);

    public static LocalizedText GetText(string key) =>
         Language.GetOrRegister("Mods.TheExtraordinaryAdditions." + key, null);

    public static string GetTextValue(string key) =>
         Language.GetTextValue("Mods.TheExtraordinaryAdditions." + key);

    public static Texture2D ThisNPCTexture(this NPC NPC) =>
         TextureAssets.Npc[NPC.type].Value;

    public static Texture2D ThisProjectileTexture(this Projectile Projectile) =>
         TextureAssets.Projectile[Projectile.type].Value;

    public static Texture2D ThisItemTexture(this Item Item) =>
         TextureAssets.Item[Item.type].Value;

    /// <summary>
    /// Determines if an NPC is "fleshy" based on it's hit sound
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    public static bool IsFleshy(this NPC target)
    {
        return target.HitSound != SoundID.NPCHit4 && target.HitSound != SoundID.NPCHit41 && target.HitSound != SoundID.NPCHit2 &&
                target.HitSound != SoundID.NPCHit5 && target.HitSound != SoundID.NPCHit11 && target.HitSound != SoundID.NPCHit30 &&
                target.HitSound != SoundID.NPCHit34 && target.HitSound != SoundID.NPCHit36 && target.HitSound != SoundID.NPCHit42 &&
                target.HitSound != SoundID.NPCHit49 && target.HitSound != SoundID.NPCHit52 && target.HitSound != SoundID.NPCHit53 &&
                target.HitSound != SoundID.NPCHit54 && target.HitSound != null;
    }

    public static bool PressingShift(this KeyboardState kb)
    {
        if (!kb.IsKeyDown(Keys.LeftShift))
            return kb.IsKeyDown(Keys.RightShift);
        return true;
    }

    public static bool PressingControl(this KeyboardState kb)
    {
        if (!kb.IsKeyDown(Keys.LeftControl))
            return kb.IsKeyDown(Keys.RightControl);
        return true;
    }

    public static void DirectlyDisplayText(string text, Color? color = null)
    {
        Color col = color ?? Color.White;
        Main.chatMonitor.NewText(text, col.R, col.G, col.B);
    }

    public static void DisplayText(string text, Color? color = null)
    {
        if (Main.netMode == NetmodeID.SinglePlayer)
            Main.NewText(text, color ?? Color.White);
        else if (Main.dedServ)
            ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral(text), color ?? Color.White);
    }

    public static Item HeldMouseItem(this Player player)
    {
        if (!Main.mouseItem.IsAir)
            return Main.mouseItem;

        return player.HeldItem;
    }

    public static void StartRain()
    {
        int ticks = 86400;
        int rand = ticks / 24;
        Main.rainTime = Main.rand.Next(rand * 8, ticks);
        if (Utils.NextBool(Main.rand, 3))
            Main.rainTime += Main.rand.Next(0, rand);
        if (Utils.NextBool(Main.rand, 4))
            Main.rainTime += Main.rand.Next(0, rand * 2);
        if (Utils.NextBool(Main.rand, 5))
            Main.rainTime += Main.rand.Next(0, rand * 2);
        if (Utils.NextBool(Main.rand, 6))
            Main.rainTime += Main.rand.Next(0, rand * 3);
        if (Utils.NextBool(Main.rand, 7))
            Main.rainTime += Main.rand.Next(0, rand * 4);
        if (Utils.NextBool(Main.rand, 8))
            Main.rainTime += Main.rand.Next(0, rand * 5);

        float mult = 1f;
        if (Utils.NextBool(Main.rand, 2))
            mult += 0.05f;
        if (Utils.NextBool(Main.rand, 3))
            mult += 0.1f;
        if (Utils.NextBool(Main.rand, 4))
            mult += 0.15f;
        if (Utils.NextBool(Main.rand, 5))
            mult += 0.2f;

        Main.rainTime = (int)(Main.rainTime * (double)mult);
        Main.raining = true;
        AdditionsNetcode.SyncWorld();
    }

    public static NPCShop AddWithCustomValue(this NPCShop shop, int itemType, int customValue, params Condition[] conditions)
    {
        Item item = new(itemType, 1, 0)
        {
            shopCustomPrice = customValue
        };
        return shop.Add(item, conditions);
    }

    public static readonly BindingFlags UniversalBindingFlags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

    public static IEnumerable<Type> GetEveryTypeDerivedFrom(Type baseType, Assembly assemblyToSearch)
    {
        foreach (Type type in AssemblyManager.GetLoadableTypes(assemblyToSearch))
        {
            if (!type.IsSubclassOf(baseType) || type.IsAbstract)
                continue;

            yield return type;
        }
    }

    public static IEnumerable<Type> GetEveryTypeDerivedFrom<T>(Assembly assemblyToSearch)
    {
        foreach (Type type in AssemblyManager.GetLoadableTypes(assemblyToSearch))
        {
            if (typeof(T).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
                yield return type;
        }
    }

    public static Delegate ConvertToDelegate(this MethodInfo method, object instance)
    {
        List<Type> paramTypes = method.GetParameters().Select(parameter => parameter.ParameterType).ToList();
        paramTypes.Add(method.ReturnType);

        Type delegateType = Expression.GetDelegateType([.. paramTypes]);
        return Delegate.CreateDelegate(delegateType, instance, method);
    }

    #region Color Utils
    public static string ColorMessage(string msg, Color color)
    {
        StringBuilder sb;
        if (!msg.Contains('\n'))
        {
            sb = new StringBuilder(msg.Length + 12);
            sb.Append("[c/").Append(Utils.Hex3(color)).Append(':')
                .Append(msg)
                .Append(']');
        }
        else
        {
            sb = new StringBuilder();
            string[] array = msg.Split('\n');
            foreach (string newlineSlice in array)
            {
                sb.Append("[c/").Append(Utils.Hex3(color)).Append(':')
                    .Append(newlineSlice)
                    .Append(']')
                    .Append('\n');
            }
        }
        return sb.ToString();
    }

    public static Color MulticolorLerp(float increment, params Color[] colors)
    {
        if (colors.Length <= 1)
            return colors[0];

        increment = MathHelper.Clamp(increment, 0f, 1f);

        float segmentLength = 1f / (colors.Length - 1);
        int segmentIndex = (int)(increment / segmentLength);

        if (segmentIndex >= colors.Length - 1)
            return colors[^1];

        // Calculate the blend factor for the current segment
        float segmentT = (increment - (segmentIndex * segmentLength)) / segmentLength;

        // Get the two colors to interpolate between
        Color start = colors[segmentIndex];
        Color end = colors[segmentIndex + 1];

        // Perform the interpolation for each color channel
        byte r = (byte)(start.R + (end.R - start.R) * segmentT);
        byte g = (byte)(start.G + (end.G - start.G) * segmentT);
        byte b = (byte)(start.B + (end.B - start.B) * segmentT);
        byte a = (byte)(start.A + (end.A - start.A) * segmentT);

        return new Color(r, g, b, a);
    }

    public static Color Lerp(this Color color, Color color2, float amount) => Color.Lerp(color, color2, amount);

    public static Color ColorSwap(Color firstColor, Color secondColor, float seconds)
    {
        float colorMe = (float)((Math.Sin((double)(MathHelper.Pi * 2f / seconds) * Main.GlobalTimeWrappedHourly) + 1.0) * 0.5);
        return Color.Lerp(firstColor, secondColor, colorMe);
    }

    public delegate void ChromaAberrationDelegate(Vector2 offset, Color colorMult);
    public static void DrawChromaticAberration(Vector2 direction, float strength, ChromaAberrationDelegate drawCall)
    {
        for (int i = -1; i <= 1; i++)
        {
            Color aberrationColor = Color.White;
            switch (i)
            {
                case -1:
                    aberrationColor = new Color(255, 0, 0, 0);
                    break;
                case 0:
                    aberrationColor = new Color(0, 255, 0, 0);
                    break;
                case 1:
                    aberrationColor = new Color(0, 0, 255, 0);
                    break;
            }
            Vector2 offset = Utils.RotatedBy(direction, MathHelper.PiOver2, default) * i;
            offset *= strength;
            drawCall(offset, aberrationColor);
        }
    }
    #endregion Color Utils

    public static void CleanHoldStyle(Player player, float desiredRotation, Vector2 desiredPosition, Vector2 spriteSize, Vector2? rotationOriginFromCenter = null, bool noSandstorm = false, bool flipAngle = false, bool stepDisplace = true)
    {
        if (noSandstorm)
            player.sandStorm = false;

        if (rotationOriginFromCenter == null)
            rotationOriginFromCenter = new Vector2?(Vector2.Zero);

        Vector2 origin = rotationOriginFromCenter.Value;
        origin.X *= player.direction;
        origin.Y *= player.gravDir;
        player.itemRotation = desiredRotation;
        if (flipAngle)
            player.itemRotation *= player.direction;
        else if (player.direction < 0)
            player.itemRotation += MathHelper.Pi;

        Vector2 consistentAnchor = Utils.ToRotationVector2(player.itemRotation) * (spriteSize.X / -2f - 10f) * player.direction - Utils.RotatedBy(origin, player.itemRotation, default);
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

    public static void BossAwakenMessage(int npcIndex)
    {
        string typeName = Main.npc[npcIndex].TypeName;
        if (Main.netMode == NetmodeID.SinglePlayer)
            Main.NewText(Language.GetTextValue("Announcement.HasAwoken", typeName), (Color?)new Color(175, 75, 255));
        else if (Main.dedServ)
            ChatHelper.BroadcastChatMessage(NetworkText.FromKey("Announcement.HasAwoken", [Main.npc[npcIndex].GetTypeNetName()]), new Color(175, 75, 255), -1);
    }

    public static bool IsOffscreen(this Projectile p)
    {
        // Check whether the projectile's hitbox intersects the screen, accounting for the screen fluff setting.
        int fluff = ProjectileID.Sets.DrawScreenCheckFluff[p.type];
        Rectangle screenArea = new((int)Main.Camera.ScaledPosition.X - fluff, (int)Main.Camera.ScaledPosition.Y - fluff, (int)Main.Camera.ScaledSize.X + fluff * 2, (int)Main.Camera.ScaledSize.Y + fluff * 2);
        return !screenArea.Intersects(p.Hitbox);
    }

    public static void ProjAntiClump(this Projectile projectile, float pushForce = 0.05f, bool minionsOnly = true)
    {
        for (int i = 0; i < Main.maxProjectiles; i++)
        {
            Projectile otherProj = Main.projectile[i];
            if (!otherProj.active || otherProj.owner != projectile.owner || i == projectile.whoAmI)
                continue;

            if (minionsOnly && !otherProj.minion)
                continue;

            bool num = otherProj.type == projectile.type;
            float taxicabDist = Math.Abs(projectile.position.X - otherProj.position.X) + Math.Abs(projectile.position.Y - otherProj.position.Y);
            if (num && taxicabDist < projectile.width)
            {
                if (projectile.position.X < otherProj.position.X)
                    projectile.velocity.X -= pushForce;
                else
                    projectile.velocity.X += pushForce;

                if (projectile.position.Y < otherProj.position.Y)
                    projectile.velocity.Y -= pushForce;
                else
                    projectile.velocity.Y += pushForce;
            }
        }
    }

    public static bool WithinBounds(this int index, int cap)
    {
        if (index >= 0)
            return index < cap;
        return false;
    }

    public static bool StandingStill(this Player player, float velocity = 0.05f) => player.velocity.Length() < velocity;

    public static bool IsUnderwater(this Player player) => Collision.DrownCollision(player.position, player.width, player.height, player.gravDir, false);

    public static bool InSpace(this Player player)
    {
        float x = Main.maxTilesX / 4200f;
        x *= x;
        return (float)((double)(player.position.Y / 16f - (60f + 10f * x)) / (Main.worldSurface / 6.0)) < 1f;
    }

    public static bool GiveIFrames(this Player player, int frames, bool blink = false)
    {
        bool anyIFramesWouldBeGiven = false;
        for (int j = 0; j < player.hurtCooldowns.Length; j++)
        {
            if (player.hurtCooldowns[j] < frames)
            {
                anyIFramesWouldBeGiven = true;
            }
        }

        if (!anyIFramesWouldBeGiven)
        {
            return false;
        }

        player.immune = true;
        player.immuneNoBlink = !blink;
        player.immuneTime = frames;
        for (int i = 0; i < player.hurtCooldowns.Length; i++)
        {
            if (player.hurtCooldowns[i] < frames)
                player.hurtCooldowns[i] = frames;
        }
        return true;
    }

    public static void RemoveAllIFrames(this Player player)
    {
        player.immune = false;
        player.immuneNoBlink = false;
        player.immuneTime = 0;
        for (int i = 0; i < player.hurtCooldowns.Length; i++)
        {
            player.hurtCooldowns[i] = 0;
        }
    }

    public static void HideAccessories(this Player player, bool hideHeadAccs = true, bool hideBodyAccs = true, bool hideLegAccs = true, bool hideShield = true)
    {
        if (hideHeadAccs)
        {
            player.face = -1;
        }
        if (hideBodyAccs)
        {
            player.handon = -1;
            player.handoff = -1;
            player.back = -1;
            player.front = -1;
            player.neck = -1;
        }
        if (hideLegAccs)
        {
            player.shoe = -1;
            player.waist = -1;
        }
        if (hideShield)
        {
            player.shield = -1;
        }
    }

    public static bool InventoryHas(this Player player, params int[] items) => player.inventory.Any((Item item) => items.Contains(item.type));

    public static DamageClass GetBestClass(this Player player)
    {
        float bestDamage = 1f;
        DamageClass bestClass = DamageClass.Generic;
        StatModifier totalDamage = player.GetTotalDamage<MeleeDamageClass>();
        float melee = totalDamage.Additive;
        if (melee > bestDamage)
        {
            bestDamage = melee;
            bestClass = DamageClass.Melee;
        }
        totalDamage = player.GetTotalDamage<RangedDamageClass>();
        float ranged = totalDamage.Additive;
        if (ranged > bestDamage)
        {
            bestDamage = ranged;
            bestClass = DamageClass.Ranged;
        }
        totalDamage = player.GetTotalDamage<MagicDamageClass>();
        float magic = totalDamage.Additive;
        if (magic > bestDamage)
        {
            bestDamage = magic;
            bestClass = DamageClass.Magic;
        }
        totalDamage = player.GetTotalDamage<SummonDamageClass>();
        float summon = totalDamage.Additive;
        if (summon > bestDamage)
        {
            bestDamage = summon;
            bestClass = DamageClass.Summon;
        }
        return bestClass;
    }
}