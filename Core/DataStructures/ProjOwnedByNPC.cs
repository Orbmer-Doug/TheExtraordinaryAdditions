using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Core.DataStructures;

/// <summary>
/// Defines a projectile that can be 'owned' by a NPC, automatically being destroyed if its owner ceases to exist
/// </summary>
/// <remarks>
/// Utilizes <see cref="Projectile.localAI"/>[0]
/// </remarks>
/// <typeparam name="T"></typeparam>
public abstract class ProjOwnedByNPC<T> : ModProjectile, ILocalizedModType, IModType where T : ModNPC
{
    /// <summary>
    /// The index of the owner within <see cref="Main.npc"/>
    /// </summary>
    public int OwnerIndex
    {
        get => (int)Projectile.localAI[0];
        set => Projectile.localAI[0] = value;
    }

    /// <summary>
    /// The NPC this projectile is owned by
    /// </summary>
    protected NPC Owner;

    /// <summary>
    /// The <see cref="Owner"/>'s ModNPC instance
    /// </summary>
    protected T ModOwner;

    /// <summary>
    /// The current target, either as a <see cref="Player"/> or <see cref="NPC"/>, of this <see cref="Owner"/>
    /// </summary>
    /// <remarks>
    /// As always, null checking is recommended before accessing
    /// </remarks>
    protected Entity Target;

    public virtual bool IgnoreOwnerActivity => false;

    public sealed override void SendExtraAI(BinaryWriter writer)
    {
        writer.Write((int)Projectile.localAI[0]);
        SendAI(writer);
    }
    public virtual void SendAI(BinaryWriter writer) { }

    public sealed override void ReceiveExtraAI(BinaryReader reader)
    {
        Projectile.localAI[0] = (int)reader.ReadInt32();
        ReceiveAI(reader);
    }
    public virtual void ReceiveAI(BinaryReader reader) { }

    /// <summary>
    /// Destroys all projectiles owned by a specified npc
    /// </summary>
    /// <param name="type">If only a specific projectile type should be destroyed at once. Defaults to sentinel <see cref="int.MinValue"/>, which destroys everything</param>
    public static void KillAll(int type = int.MinValue)
    {
        foreach (Projectile p in Main.ActiveProjectiles)
        {
            if (p.ModProjectile is not ProjOwnedByNPC<T>)
                continue;

            if (type != int.MinValue)
            {
                if (p.type != type)
                    continue;
            }
            (p as ProjOwnedByNPC<T>)?.Kill();
        }
    }

    /// <summary>
    /// Spawns a new projectile that inherits the owner of this projectile <br></br>
    /// </summary>
    /// <param name="damage">Automatically fixes damage from current difficulty</param>
    /// <returns>The index within <see cref="Main.projectile"/></returns>
    public int SpawnProjectile(Vector2 position, Vector2 velocity, int type, int damage, float knockback,
        float ai0 = 0f, float ai1 = 0f, float ai2 = 0f, float extra0 = 0f, float extra1 = 0f)
    {
        if (Owner == null || !Owner.active || Projectile == null || !Projectile.active || OwnerIndex == 0)
            return 0;

        damage = FixDamageFromDifficulty(damage);

        int index = Projectile.NewProjectile(Projectile.GetSource_FromThis(), position.X, position.Y,
            velocity.X, velocity.Y, type, damage, knockback, Main.myPlayer, ai0, ai1, ai2);
        if (index >= 0 && index < Main.maxProjectiles)
        {
            Projectile projectile = Main.projectile[index];
            if (projectile.ModProjectile != null && projectile.ModProjectile.Mod == AdditionsMain.Instance)
            {
                projectile.AdditionsInfo().ExtraAI[0] = extra0;
                projectile.AdditionsInfo().ExtraAI[1] = extra1;
            }

            projectile.localAI[0] = Owner.whoAmI;
            if (Main.netMode != NetmodeID.SinglePlayer)
                NetMessage.SendData(MessageID.SyncProjectile, -1, -1, null, index);
        }

        return index;
    }

    public sealed override bool PreAI()
    {
        // Instead of relying on reference return values we will manually set up the relationship
        // Before any AI is allowed to run

        NPC owner = null;
        if (OwnerIndex >= 0 && OwnerIndex < Main.maxNPCs) // Check if the index is within the bounds of the array
        {
            NPC npc = Main.npc[OwnerIndex]; // Get the npc
            if (npc != null && npc.active && npc.ModNPC is T) // Check if they are available
                owner = npc;
        }
        Owner = owner;

        // Assign the helper if necessary
        if (Owner != null)
            ModOwner = Owner?.As<T>() ?? null;

        // And update the shared target
        if (Owner != null && Owner.active && Owner.HasValidTarget)
            Target = (Owner.HasPlayerTarget || !NPCID.Sets.UsesNewTargetting[Owner.type]) ? Main.player[Owner.target] : Main.npc[Owner.target - 300];
        else
            Target = null;

        return base.PreAI();
    }

    public sealed override void AI()
    {
        // Die immediately if the owner is no longer available
        if (!IgnoreOwnerActivity)
        {
            if (Owner == null || !Owner.active)
            {
                Kill();
                return;
            }
        }

        SafeAI();
    }

    public virtual void SafeAI() { }

    public virtual void Kill()
    {
        // Generally one would use Projectile.Kill(), but if we were to say have the usage of SpawnProjectile in the OnKill method
        // then an error will be thrown if the projectile was being killed because the Owner is no longer available
        // So in this case if a projectile needs to do something special in OnKill, it must handle it itself
        Projectile.active = false;
    }
}