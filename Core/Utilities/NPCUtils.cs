using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Chat;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.UI.BigProgressBar;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.DataStructures;
using TheExtraordinaryAdditions.Core.Globals;

namespace TheExtraordinaryAdditions.Core.Utilities;

public static class NPCUtils
{
    extension(ModNPC mod)
    {
        /// <summary>
        /// Spawning projectiles or npcs, randomness (remember to sync under randoms)
        /// </summary>
        public static bool RunServer() => Main.netMode != NetmodeID.MultiplayerClient;

        /// <summary>
        /// e.g. this npc dying, adding a buff to the player...
        /// </summary>
        public static bool RunClient() => Main.netMode != NetmodeID.Server;

        /// <summary>
        /// Sudden shifts in position, state changes/variable updates, persistent position movements (like a dash) <br />
        /// <b>The server is in charge of NPCs, changes to NPC data should only happen on the server in multiplayer</b>
        /// </summary>
        public void Sync()
        {
            mod.NPC.netUpdate = true;
            mod.NPC.netSpam = 0;
        }

        /// <summary>
        /// Completely hide a npc from the bestiary
        /// </summary>
        public void ExcludeFromBestiary()
        {
            NPCID.Sets.NPCBestiaryDrawModifiers value = new()
            {
                Hide = true
            };
            NPCID.Sets.NPCBestiaryDrawOffset.Add(mod.Type, value);
        }
    }

    /// <param name="npc">The NPC to access the ModNPC from</param>
    extension(NPC npc)
    {
        public Texture2D ThisNPCTexture() =>
            TextureAssets.Npc[npc.type].Value;

        public bool IsAnEnemy(bool allowStatues = true, bool checkDead = true, bool checkDamage = true)
        {
            if (npc is null || (!npc.active && (!checkDead || npc.life > 0)) || npc.townNPC || npc.friendly)
                return false;
            if (!allowStatues && npc.SpawnedFromStatue)
                return false;
            if (npc.lifeMax <= 5 || (npc.defDamage <= 5 && checkDamage && npc.lifeMax <= 5))
                return false;
            return true;
        }

        public void Kill()
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

        /// <summary>
        /// A simple utility that gets an <see cref="NPC"/>s <see cref="NPC.ModNPC"/> instance
        /// </summary>
        /// <typeparam name="T">The ModNPC type to convert to</typeparam>
        public T As<T>() where T : ModNPC
        {
            return npc?.ModNPC as T;
        }

        /// <summary>
        /// Spawns a projectile from this NPC <br />
        /// Automatically assigns the relationship between the NPC and projectile, assuming it is a <see cref="ProjOwnedByNPC{T}"/>
        /// </summary>
        /// <param name="damage">Automatically fixes damage from current difficulty</param>
        /// <returns>The index within <see cref="Main.projectile"/></returns>
        public int CreateNPCProj(Vector2 position, Vector2 velocity, int type, int damage,
            float knockback,
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
        /// Spawns a new projectile from this NPC <br />
        /// Use <see cref="NPCUtils.CreateNPCProj"/> if the projectile should have an owner
        /// </summary>
        public int CreateNPCProjAlt(Vector2 center, Vector2 velocity, int type, int damage, float knockback,
            int owner = -1, float ai0 = 0f, float ai1 = 0f, float ai2 = 0f)
        {
            IEntitySource source = npc.GetSource_FromThis();
            int projectile =
                Projectile.NewProjectile(source, center, velocity, type, damage, knockback, owner, ai0, ai1, ai2);
            Projectile p = Main.projectile[projectile];
            if (projectile >= 0 && projectile < Main.maxProjectiles)
                p.netUpdate = true;
            return projectile;
        }

        public int NewNPCBetter(Vector2 pos, Vector2 vel, int type, int start = 0, float ai0 = 0f,
            float ai1 = 0f, float ai2 = 0f, float ai3 = 0f, int target = -1)
        {
            int index = NPC.NewNPC(npc.GetSpawnSourceForNPCFromNPCAI(), (int) pos.X, (int) pos.Y, type, start, ai0, ai1,
                ai2,
                ai3, target);

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

        public Entity GetTarget()
        {
            if (!npc.HasValidTarget)
                return null;

            return npc.HasPlayerTarget ? Main.player[npc.target] : Main.npc[npc.target - 300];
        }


        /// <summary>
        /// Determines if an NPC is "fleshy" based on it's hit sound
        /// </summary>
        /// <returns></returns>
        public bool IsFleshy()
        {
            return npc.HitSound != SoundID.NPCHit4 && npc.HitSound != SoundID.NPCHit41 &&
                   npc.HitSound != SoundID.NPCHit2 &&
                   npc.HitSound != SoundID.NPCHit5 && npc.HitSound != SoundID.NPCHit11 &&
                   npc.HitSound != SoundID.NPCHit30 &&
                   npc.HitSound != SoundID.NPCHit34 && npc.HitSound != SoundID.NPCHit36 &&
                   npc.HitSound != SoundID.NPCHit42 &&
                   npc.HitSound != SoundID.NPCHit49 && npc.HitSound != SoundID.NPCHit52 &&
                   npc.HitSound != SoundID.NPCHit53 &&
                   npc.HitSound != SoundID.NPCHit54 && npc.HitSound != null;
        }
    }

    public static IBigProgressBar HideBossBar(NPC npc)
    {
        return npc.BossBar = Main.BigBossProgressBar.NeverValid;
    }

    public static NPCShop AddWithCustomValue(this NPCShop shop, int itemType, int customValue,
        params Condition[] conditions)
    {
        Item item = new(itemType)
        {
            shopCustomPrice = customValue
        };
        return shop.Add(item, conditions);
    }

    public static void BossAwakenMessage(int npcIndex)
    {
        string typeName = Main.npc[npcIndex].TypeName;
        if (Main.netMode == NetmodeID.SinglePlayer)
            Main.NewText(Language.GetTextValue("Announcement.HasAwoken", typeName), new Color(175, 75, 255));
        else if (Main.dedServ)
            ChatHelper.BroadcastChatMessage(
                NetworkText.FromKey("Announcement.HasAwoken", Main.npc[npcIndex].GetTypeNetName()),
                new Color(175, 75, 255));
    }
}
