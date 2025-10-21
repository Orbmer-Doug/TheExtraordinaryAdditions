using Microsoft.Xna.Framework.Graphics;
using ReLogic.Reflection;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Config;

namespace TheExtraordinaryAdditions.Core.Systems;

public abstract class ProjectileOverride
{
    internal static Dictionary<int, ProjectileOverride> BehaviorOverrideSet;
    internal static void LoadAll()
    {
        BehaviorOverrideSet = new Dictionary<int, ProjectileOverride>();
        foreach (Type type in GetEveryTypeDerivedFrom(typeof(ProjectileOverride), typeof(AdditionsMain).Assembly))
        {
            if (!AdditionsConfigServer.Instance.UseCustomAI)
                continue;

            ProjectileOverride instance = (ProjectileOverride)Activator.CreateInstance(type);

            BehaviorOverrideSet.Add(instance.ProjectileOverrideType, instance);
        }
    }

    public abstract int ProjectileOverrideType { get; }
    public virtual void SetDefaults(Projectile proj) { }
    public virtual bool PreAI(Projectile proj) => true;
    public virtual void PostAI(Projectile proj) { }
    public virtual void SendExtraAI(Projectile proj, ModPacket writer) { }
    public virtual void ReceiveExtraAI(Projectile proj, BinaryReader reader) { }
    public virtual bool PreDraw(Projectile proj, SpriteBatch spriteBatch, ref Color lightColor) => true;
    public virtual bool OnTileCollide(Projectile proj, Vector2 oldVelocity) => true;
    public virtual void OnKill(Projectile proj) { }
    public virtual void OnHitNPC(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone) { }
    public virtual void ModifyHitNPC(Projectile proj, NPC target, ref NPC.HitModifiers modifiers) { }
    public virtual bool? CanDamage(Projectile proj) => null;
}