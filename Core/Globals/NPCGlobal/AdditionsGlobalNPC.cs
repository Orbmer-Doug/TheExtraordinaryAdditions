using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Common.Particles.Shader;
using TheExtraordinaryAdditions.Content.Items.Equipable.Pets;
using TheExtraordinaryAdditions.Content.Items.Weapons.Melee.Middle;
using TheExtraordinaryAdditions.Core.Netcode;
using TheExtraordinaryAdditions.Core.Systems;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Core.Globals.NPCGlobal;

public class AdditionsGlobalNPC : GlobalNPC
{
    public override bool InstancePerEntity => true;

    #region Debuffs
    public bool DentedBySpoon;
    public int PlasmaIncineration;
    #endregion Debuffs

    #region Whip
    public int VoidEnergy;
    public int Eclipsed;
    public int Wavebreaked;
    public int StarKunai;
    public int Cursed;
    #endregion Whip

    public delegate void EditSpawnRateDelegate(Player player, ref int spawnRate, ref int maxSpawns);

    public static event EditSpawnRateDelegate EditSpawnRateEvent;

    public delegate void EditSpawnPoolDelegate(IDictionary<int, float> pool, NPCSpawnInfo spawnInfo);

    public static event EditSpawnPoolDelegate EditSpawnPoolEvent;

    public delegate void NPCActionDelegate(NPC npc);

    public static event NPCActionDelegate OnKillEvent;

    public delegate void NPCSpawnDelegate(NPC npc, IEntitySource source);

    public static event NPCSpawnDelegate OnSpawnEvent;

    public override void EditSpawnRate(Player player, ref int spawnRate, ref int maxSpawns)
    {
        // Apply spawn rate alterations in accordance with the event.
        EditSpawnRateEvent?.Invoke(player, ref spawnRate, ref maxSpawns);
    }

    public override void EditSpawnPool(IDictionary<int, float> pool, NPCSpawnInfo spawnInfo)
    {
        // Apply spawn pool alterations in accordance with the event.
        EditSpawnPoolEvent?.Invoke(pool, spawnInfo);
    }

    public override GlobalNPC Clone(NPC npc, NPC npcClone)
    {
        AdditionsGlobalNPC myClone = (AdditionsGlobalNPC)Clone(npc, npcClone);
        myClone.PlasmaIncineration = PlasmaIncineration;
        myClone.VoidEnergy = VoidEnergy;
        myClone.Wavebreaked = Wavebreaked;
        myClone.Eclipsed = Eclipsed;
        myClone.Cursed = Cursed;
        myClone.StarKunai = StarKunai;

        return myClone;
    }

    public override void UpdateLifeRegen(NPC npc, ref int damage)
    {
        void ApplyDPSDebuff(int lifeRegenValue, int damageValue, ref int damage)
        {
            // Negate positive life regen
            if (npc.lifeRegen > 0)
                npc.lifeRegen = 0;

            // Half life per second
            npc.lifeRegen -= lifeRegenValue;

            if (damage < damageValue)
                damage = damageValue;
        }

        if (PlasmaIncineration > 0)
        {
            int PlasmaIncineration = (int)250.0;
            ApplyDPSDebuff(PlasmaIncineration, PlasmaIncineration, ref damage);
        }

        if (Wavebreaked > 0)
        {
            int Torrential = (int)180.0;
            ApplyDPSDebuff(Torrential, Torrential, ref damage);
        }
    }

    public override void PostAI(NPC npc)
    {
        if (PlasmaIncineration > 0)
            PlasmaIncineration--;

        if (VoidEnergy > 0)
            VoidEnergy--;

        if (Wavebreaked > 0)
            Wavebreaked--;

        if (StarKunai > 0)
            StarKunai--;

        if (Eclipsed > 0)
            Eclipsed--;

        if (Cursed > 0)
            Cursed--;
    }

    public override void ResetEffects(NPC npc)
    {
        DentedBySpoon = false;
    }

    public override void ModifyHitByProjectile(NPC npc, Projectile projectile, ref NPC.HitModifiers modifiers)
    {
        if (projectile.npcProj || projectile.trap || !projectile.IsMinionOrSentryRelated)
            return;

        Player owner = Main.player[projectile.owner];
        ref AddableFloat scaling = ref modifiers.ScalingBonusDamage;

        if (Eclipsed > 0)
        {
            scaling += 0.1f;
            modifiers.FlatBonusDamage += 9;
        }

        if (VoidEnergy > 0)
        {
            scaling += 0.35f;
            modifiers.FlatBonusDamage += 20;
            if (Main.rand.NextBool(5))
                modifiers.SetCrit();
        }

        if (Wavebreaked > 0)
        {
            scaling += 0.15f;
            modifiers.FlatBonusDamage += 35;
        }

        if (StarKunai > 0)
        {
            scaling += 0.06f;
            modifiers.FlatBonusDamage += 7;
        }
    }

    public override void ModifyIncomingHit(NPC npc, ref NPC.HitModifiers modifiers)
    {
        if (DentedBySpoon)
        {
            modifiers.CritDamage += .1f;
            modifiers.ScalingArmorPenetration += 1f;
        }

        if (Cursed > 0)
        {
            modifiers.FinalDamage *= 1.2f;
        }
    }

    public override void DrawEffects(NPC npc, ref Color drawColor)
    {
        if (Cursed > 0)
        {
            if (Main.rand.Next(5) < 4)
            {
                Color curseColor = MulticolorLerp(Main.rand.NextFloat(0.2f, 0.8f), Color.Black, new Color(16, 0, 89), new Color(21, 0, 74));
                float scale = Main.rand.NextFloat(.47f, .61f);
                float rot = MathHelper.ToRadians(3f);
                Vector2 pos = Main.rand.NextVector2FromRectangle(npc.Hitbox);
                Vector2 vel = Main.rand.NextVector2CircularEdge(2f, 2f);
                ParticleRegistry.SpawnHeavySmokeParticle(pos, vel, Main.rand.Next(10, 40), scale, curseColor, .6f, true, rot);
            }
        }

        if (PlasmaIncineration > 0)
        {
            Color fireColor = MulticolorLerp(Main.rand.NextFloat(0.2f, 0.8f), Color.Red, Color.OrangeRed, Color.IndianRed, Color.DarkRed, Color.OrangeRed * 1.2f, Color.OrangeRed * 1.6f);
            Vector2 vel = Vector2.UnitY.RotatedByRandom(0.69) * Main.rand.NextFloat(1f, 1.2f);
            vel.Y -= 6f;
            float scale = Main.rand.NextFloat(.47f, .6f);
            int lifetime = 10 + Main.rand.Next(10, 40);

            ParticleRegistry.SpawnHeavySmokeParticle(npc.RandAreaInEntity(), vel, lifetime, scale, fireColor, .325f, true, .05f);
            ParticleRegistry.SpawnHeavySmokeParticle(npc.RandAreaInEntity(), vel * .2f, lifetime, scale * 1.1f, fireColor, .385f, true, .1f);

            Lighting.AddLight(npc.position, Color.OrangeRed.ToVector3());
        }

        if (VoidEnergy > 0)
        {
            if (Main.rand.Next(8) < 4)
            {
                Color fireColor = MulticolorLerp(Main.rand.NextFloat(0.2f, 0.8f), Color.Black, new Color(16, 0, 89), new Color(21, 0, 74));
                float scale = Main.rand.NextFloat(.47f, .61f);
                float rot = MathHelper.ToRadians(3f);
                Vector2 pos = Main.rand.NextVector2FromRectangle(npc.Hitbox);
                ParticleRegistry.SpawnHeavySmokeParticle(pos, new Vector2(0f, Main.rand.NextFloat(-6f, 6f)), 10 + Main.rand.Next(10, 40), scale, fireColor, 1.2f, true, rot);
                ParticleRegistry.SpawnHeavySmokeParticle(pos, new Vector2(Main.rand.NextFloat(-6f, 6f), 0f), 10 + Main.rand.Next(10, 40), scale, fireColor, 1.4f, true, rot);
            }
        }

        if (DentedBySpoon)
        {
            drawColor.G = 66;
            drawColor.R = 86;
            drawColor.B = 86;
        }
    }

    public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
    {
        Texture2D texture = TextureAssets.Npc[npc.type].Value;

        if (Eclipsed > 0)
        {
            spriteBatch.SetBlendState(BlendState.Additive);
            Texture2D bloomTexture = AssetRegistry.GetTexture(AdditionsTexture.GlowParticleSmall);
            float properBloomSize = bloomTexture.Height / (float)bloomTexture.Height;
            Color color = Color.Lerp(Color.OrangeRed, Color.Gray, (float)MathF.Sin(Main.GlobalTimeWrappedHourly * 2f));
            Vector2 sparkCenter = npc.Center - Main.screenPosition;
            Main.EntitySpriteDraw(bloomTexture, sparkCenter, null, color * 0.6f, 0f, bloomTexture.Size() / 2f, 2f * properBloomSize / 2, 0, 0f);
            spriteBatch.ResetBlendState();
        }

        return base.PreDraw(npc, spriteBatch, screenPos, drawColor);
    }

    public override void OnSpawn(NPC npc, IEntitySource source)
    {
        OnSpawnEvent?.Invoke(npc, source);
    }

    public override void OnKill(NPC npc)
    {
        OnKillEvent?.Invoke(npc);

        switch (npc.type)
        {
            case NPCID.Golem:
                if (!NPC.downedGolemBoss)
                {
                    if (!Main.LocalPlayer.dead && Main.LocalPlayer.active)
                        AdditionsSound.heartbeat.Play(npc.Center, 7f);

                    for (int i = 0; i < 120; i++)
                    {
                        Vector2 pos = npc.RotHitbox().RandomPoint();
                        Vector2 vel = -Vector2.UnitY * Main.rand.NextFloat(2f, 5f);
                        float scale = Main.rand.NextFloat(120f, 180f);
                        ShaderParticleRegistry.SpawnStygainParticle(pos, vel, scale);
                    }

                    DisplayText(GetText(Name + ".SuperBloodMoonBegin").Value, Color.Crimson);
                    SuperBloodMoonSystem.SuperBloodMoon = true;
                    AdditionsNetcode.SyncAdditionsBloodMoon(Main.myPlayer);
                }
                break;
        }
    }

    public override void ModifyShop(NPCShop shop)
    {
        switch (shop.NpcType)
        {
            case NPCID.PartyGirl:
                shop.Add(ModContent.ItemType<SillyPinkHammer>(), [Condition.Hardmode]);
                break;
            case NPCID.Wizard:
                shop.Add(ItemID.DemonScythe, [Condition.InUnderworld]);
                shop.Add(ItemID.UnholyTrident, [Condition.InUnderworld, Condition.DownedPlantera]);
                break;
            case NPCID.Painter:
                shop.Add(ModContent.ItemType<PaintCoveredCamera>(), []);
                break;
        }
    }
}