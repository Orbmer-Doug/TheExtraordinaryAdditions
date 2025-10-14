using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.NPCs.Misc;

namespace TheExtraordinaryAdditions;

// Note: DONT LEAVE THIS IN ON RELEASE
public class BossTimes : GlobalNPC
{
    public override bool InstancePerEntity => true;
    public override bool AppliesToEntity(NPC entity, bool lateInstantiation)
    {
        return entity.boss && entity.type != ModContent.NPCType<GodDummyNPC>() && lateInstantiation;
    }

    public Stopwatch watch;
    public List<int> damages = [];

    public override void OnSpawn(NPC npc, IEntitySource source)
    {
        watch ??= new();
        watch.Start();
    }

    public override void OnHitByItem(NPC npc, Player player, Item item, NPC.HitInfo hit, int damageDone)
    {
        damages.Add(damageDone);
    }

    public override void OnHitByProjectile(NPC npc, Projectile projectile, NPC.HitInfo hit, int damageDone)
    {
        damages.Add(damageDone);
    }

    public override void Load()
    {
        On_NPC.StrikeNPC_HitInfo_bool_bool += On_NPC_StrikeNPC_HitInfo_bool_bool;
        On_NPC.StrikeNPCNoInteraction += On_NPC_StrikeNPCNoInteraction;
        On_NPC.SimpleStrikeNPC += On_NPC_SimpleStrikeNPC;
    }

    public override void Unload()
    {
        On_NPC.StrikeNPC_HitInfo_bool_bool -= On_NPC_StrikeNPC_HitInfo_bool_bool;
        On_NPC.StrikeNPCNoInteraction -= On_NPC_StrikeNPCNoInteraction;
        On_NPC.SimpleStrikeNPC -= On_NPC_SimpleStrikeNPC;
    }

    private static int On_NPC_SimpleStrikeNPC(On_NPC.orig_SimpleStrikeNPC orig, NPC self, int damage, int hitDirection, bool crit, float knockBack, DamageClass damageType, bool damageVariation, float luck, bool noPlayerInteraction)
    {
        if (self.boss && self.type != ModContent.NPCType<GodDummyNPC>())
            self.GetGlobalNPC<BossTimes>().damages.Add(damage);
        return orig(self, damage, hitDirection, crit, knockBack, damageType, damageVariation, luck, noPlayerInteraction);
    }

    private static int On_NPC_StrikeNPCNoInteraction(On_NPC.orig_StrikeNPCNoInteraction orig, NPC self, int Damage, float knockBack, int hitDirection)
    {
        if (self.boss && self.type != ModContent.NPCType<GodDummyNPC>())
            self.GetGlobalNPC<BossTimes>().damages.Add(Damage);
        return orig(self, Damage, knockBack, hitDirection);
    }

    private static int On_NPC_StrikeNPC_HitInfo_bool_bool(On_NPC.orig_StrikeNPC_HitInfo_bool_bool orig, NPC self, NPC.HitInfo hit, bool fromNet, bool noPlayerInteraction)
    {
        if (self.boss && self.type != ModContent.NPCType<GodDummyNPC>())
            self.GetGlobalNPC<BossTimes>().damages.Add(hit.Damage);
        return orig(self, hit, fromNet, noPlayerInteraction);
    }

    public override void OnKill(NPC npc)
    {
        watch?.Stop();

        if (npc.type == ModContent.NPCType<TheGiantSnailFromAncientTimes>())
        {
            DirectlyDisplayText("The snail will not reveal its secrets.", Color.Crimson);
            return;
        }

        if (npc == null)
            DirectlyDisplayText("honestly quite incredible the npc doesn't even exist");
        else
        {
            string silly = Main.rand.Next(0, 4) switch
            {
                0 => "The elusive",
                1 => "The atrocious",
                2 => "The stinky",
                3 => "The majestic",
                _ => ""
            };
            DirectlyDisplayText($"{silly} {npc.FullName} (Max Life: {npc.lifeMax} | Defense: {npc.defense}) lasted for {watch?.Elapsed.TotalSeconds} seconds", Color.LimeGreen);
            if (damages.Count == 0 || damages == null)
                DirectlyDisplayText($"somehow this dude died??? whatever i dont know why. no averages for YOU!", Color.Red);
            else
                DirectlyDisplayText($"Average damage amount was {Math.Round(damages.Average())} (Average per second was {Math.Round(damages.Sum() / watch.Elapsed.TotalSeconds)})", Color.LimeGreen);
        }

        watch?.Restart();
        watch = null;
    }

    public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
    {
        if (watch != null)
        {
            string content = $"{watch.Elapsed.Minutes}:{watch.Elapsed.Seconds}:{watch.Elapsed.Milliseconds}";
            //Utility.DrawText(spriteBatch, content, 2, Main.LocalPlayer.Center - Vector2.UnitY * 200f - Main.screenPosition,
            //  Color.White, Color.Black, new(ChatManager.GetStringSize(FontAssets.MouseText.Value, content, Vector2.One).X / 2, 0f), 1f);
        }
        return base.PreDraw(npc, spriteBatch, screenPos, drawColor);
    }
}
