using System;
using System.IO;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Globals;

namespace TheExtraordinaryAdditions.Core.Utilities;

public static class ProjectileUtils
{
    extension(ModProjectile mod)
    {
        public static bool RunServer() => Main.netMode != NetmodeID.MultiplayerClient;
        public bool RunLocal() => Main.myPlayer == mod.Projectile.owner;

        public void Sync()
        {
            mod.Projectile.netUpdate = true;
            mod.Projectile.netSpam = 0;
        }
    }

    /// <param name="proj">The projectile</param>
    extension(Projectile proj)
    {
        /// <summary>
        /// A simple utility that gets an <see cref="Projectile"/>s <see cref="Projectile.ModProjectile"/> instance
        /// </summary>
        public T As<T>() where T : ModProjectile
        {
            return proj.ModProjectile as T;
        }

        public Texture2D ThisProjectileTexture() =>
            TextureAssets.Projectile[proj.type].Value;

        public bool FinalExtraUpdate() => proj.numUpdates == -1;

        public void ExpandHitboxBy(int width, int height)
        {
            proj.position = proj.Center;
            proj.width = width;
            proj.height = height;
            proj.position -= proj.Size * 0.5f;
        }
        
        public void VelocityBasedRotation(float power = .03f) => proj.rotation +=
            (Math.Abs(proj.velocity.X) + Math.Abs(proj.velocity.Y)) * power * proj.direction;

        public float FacingUpRight() => proj.rotation = proj.velocity.ToRotation() - MathHelper.PiOver4;
        public float FacingUp() => proj.rotation = proj.velocity.ToRotation() + MathHelper.PiOver2;
        public float FacingDown() => proj.rotation = proj.velocity.ToRotation() - MathHelper.PiOver2;
        public float FacingRight() => proj.rotation = proj.velocity.ToRotation();
        public float FacingLeft() => proj.rotation = proj.velocity.ToRotation() + 3f * MathHelper.Pi / 2f;

        public float FacingDirectionLiteral(bool flip = false)
        {
            float dir1 = -proj.velocity.ToRotation();
            float dir2 = proj.velocity.ToRotation();
            if (proj.direction < 0)
                return proj.rotation = flip ? dir2 : dir1;
            return proj.rotation = flip ? dir1 : dir2;
        }

        public void ExpandHitboxBy(int newSize) =>
            proj.ExpandHitboxBy(newSize, newSize);

        public void ExpandHitboxBy(Vector2 newSize) =>
            proj.ExpandHitboxBy((int) newSize.X, (int) newSize.Y);

        public void ExpandHitboxBy(float expandRatio) =>
            proj.ExpandHitboxBy((int) (proj.width * expandRatio), (int) (proj.height * expandRatio));

        /// <summary>
        /// why was it private
        /// </summary>
        /// <param name="index">The index of this projectile in the group</param>
        /// <param name="totalIndexesInGroup">The total amount of projectiles in the group</param>
        public void AI_GetMyGroupIndex(out int index, out int totalIndexesInGroup)
        {
            index = 0;
            totalIndexesInGroup = 0;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];
                if (projectile != null && projectile.active && projectile.owner == proj.owner &&
                    projectile.type == proj.type)
                {
                    if (proj.whoAmI > i)
                        index++;

                    totalIndexesInGroup++;
                }
            }
        }

        public void CreateFriendlyExplosion(Vector2 pos, Vector2 size, int dmg, float kb,
            int life, int iframes, Vector2? toSize = null, Color light = default)
        {
            if (Main.LocalPlayer == Main.player[proj.owner])
                CreateExplosion(proj.GetSource_FromThis(), proj.DamageType, pos, size, dmg, kb, life, iframes,
                    proj.owner,
                    true, toSize, light, proj.Name);
        }

        /// <summary>
        /// Make a new projectile from a source of a projectile
        /// </summary>
        public int NewProj(Vector2 center, Vector2 velocity, int type, int damage,
            float knockback, int owner = -1,
            float ai0 = 0f, float ai1 = 0f, float ai2 = 0f, float extra0 = 0f, float extra1 = 0f)
        {
            IEntitySource source = proj.GetSource_FromThis();
            int index = Projectile.NewProjectile(source, center, velocity, type, damage, knockback, owner, ai0, ai1,
                ai2);
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
        /// Set the animation for a projectile
        /// </summary>
        /// <param name="frames">Total frames this projectile has and what should be cycled through</param>
        /// <param name="ticksPerFrame">How many frames to wait before going to the next frame</param>
        /// <param name="pingPong">Goes back and forth</param>
        /// <returns>The current frame</returns>
        public int SetAnimation(int frames, int ticksPerFrame, bool pingPong = false)
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

        public bool IsOffscreen()
        {
            // Check whether the projectile's hitbox intersects the screen, accounting for the screen fluff setting
            int fluff = ProjectileID.Sets.DrawScreenCheckFluff[proj.type];
            Rectangle screenArea = new((int) Main.Camera.ScaledPosition.X - fluff,
                (int) Main.Camera.ScaledPosition.Y - fluff,
                (int) Main.Camera.ScaledSize.X + fluff * 2, (int) Main.Camera.ScaledSize.Y + fluff * 2);
            return !screenArea.Intersects(proj.Hitbox);
        }

        public void ProjAntiClump(float pushForce = 0.05f, bool minionsOnly = true)
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile otherProj = Main.projectile[i];
                if (!otherProj.active || otherProj.owner != proj.owner || i == proj.whoAmI)
                    continue;

                if (minionsOnly && !otherProj.minion)
                    continue;

                bool num = otherProj.type == proj.type;
                float taxicabDist = Math.Abs(proj.position.X - otherProj.position.X) +
                                    Math.Abs(proj.position.Y - otherProj.position.Y);
                if (num && taxicabDist < proj.width)
                {
                    if (proj.position.X < otherProj.position.X)
                        proj.velocity.X -= pushForce;
                    else
                        proj.velocity.X += pushForce;

                    if (proj.position.Y < otherProj.position.Y)
                        proj.velocity.Y -= pushForce;
                    else
                        proj.velocity.Y += pushForce;
                }
            }
        }

        public void StickyProjAI(int timeLeft, bool findNewNPC = false)
        {
            if ((int) proj.ai[0] == 1)
            {
                bool killProj = false;
                bool spawnDust = false;

                //the projectile follows the NPC, even if it goes into blocks
                proj.tileCollide = false;

                //timer for triggering hit effects
                proj.localAI[0]++;
                if (proj.localAI[0] % 30f == 0f)
                {
                    spawnDust = true;
                }

                //So AI knows what NPC it is sticking to
                int npcIndex = (int) proj.ai[1];
                NPC npc = Main.npc[npcIndex];

                //Kill projectile after so many seconds or if the NPC it is stuck to no longer exists
                if (proj.localAI[0] >= 60 * timeLeft)
                {
                    killProj = true;
                }
                else if (!npcIndex.WithinBounds(Main.maxNPCs))
                {
                    killProj = true;
                }

                else if (npc.active && !npc.dontTakeDamage)
                {
                    //follow the NPC
                    proj.Center = npc.Center - proj.velocity * 2f;
                    proj.gfxOffY = npc.gfxOffY;

                    //if attached to npc, trigger npc hit effects every half a second
                    if (spawnDust)
                    {
                        npc.HitEffect(0, 1.0);
                    }
                }
                else
                {
                    killProj = true;
                }

                //Kill the projectile or reset stats if needed
                if (!killProj) return;

                if (findNewNPC)
                    proj.ai[0] = 0f;
                else
                    proj.Kill();
            }
        }

        public void ModifyHitNPCSticky(int maxStick)
        {
            Player player = Main.player[proj.owner];
            Rectangle myRect = proj.Hitbox;

            if (proj.owner != Main.myPlayer)
                return;

            for (int npcIndex = 0; npcIndex < Main.maxNPCs; npcIndex++)
            {
                NPC npc = Main.npc[npcIndex];

                //covers most edge cases like voodoo dolls
                if (npc.active && !npc.dontTakeDamage &&
                    ((proj.friendly && (!npc.friendly ||
                                        (npc.type == NPCID.Guide && proj.owner < Main.maxPlayers &&
                                         player.killGuide) || (npc.type == NPCID.Clothier &&
                                                               proj.owner < Main.maxPlayers &&
                                                               player.killClothier))) ||
                     (proj.hostile && npc.friendly && !npc.dontTakeDamageFromHostiles)) && (proj.owner < 0 ||
                        npc.immune[proj.owner] == 0 || proj.maxPenetrate == 1))
                {
                    if (!npc.noTileCollide && proj.ownerHitCheck)
                        continue;

                    bool stickingToNPC;
                    //Solar Crawltipede tail has special collision
                    if (npc.type == NPCID.SolarCrawltipedeTail)
                    {
                        Rectangle rect = npc.Hitbox;
                        const int crawltipedeHitboxMod = 8;
                        rect.X -= crawltipedeHitboxMod;
                        rect.Y -= crawltipedeHitboxMod;
                        rect.Width += crawltipedeHitboxMod * 2;
                        rect.Height += crawltipedeHitboxMod * 2;
                        stickingToNPC = proj.Colliding(myRect, rect);
                    }
                    else
                    {
                        stickingToNPC = proj.Colliding(myRect, npc.Hitbox);
                    }

                    if (!stickingToNPC)
                        continue;

                    // reflect projectile if the npc can reflect it (like Selenians)
                    if (npc.reflectsProjectiles && proj.CanBeReflected())
                    {
                        npc.ReflectProjectile(proj);
                        return;
                    }

                    // let the projectile know it is sticking and the npc it is sticking too
                    proj.ai[0] = 1f;
                    proj.ai[1] = npcIndex;

                    // follow the NPC
                    proj.velocity = (npc.Center - proj.Center);

                    proj.netUpdate = true;

                    // Count how many projectiles are attached, delete as necessary
                    Point[] array2 = new Point[maxStick];
                    int projCount = 0;
                    for (int projIndex = 0; projIndex < Main.maxProjectiles; projIndex++)
                    {
                        Projectile proj1 = Main.projectile[projIndex];
                        if (projIndex != proj.whoAmI && proj1.active && proj1.owner == Main.myPlayer &&
                            proj1.type == proj.type && proj1.ai[0] == 1f && proj1.ai[1] == npcIndex)
                        {
                            array2[projCount++] = new Point(projIndex, proj1.timeLeft);
                            if (projCount >= array2.Length)
                                break;
                        }
                    }

                    if (projCount >= array2.Length)
                    {
                        int stuckProjAmt = 0;
                        for (int m = 1; m < array2.Length; m++)
                        {
                            if (array2[m].Y < array2[stuckProjAmt].Y)
                            {
                                stuckProjAmt = m;
                            }
                        }

                        Main.projectile[array2[stuckProjAmt].X].Kill();
                    }
                }
            }
        }
    }


    private class ExplosionProjectile : ModProjectile
    {
        public override string Texture => AssetRegistry.GennedTextures.Invisible.Path;

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
            if (ToSize != null)
                writer.WriteVector2(ToSize.Value);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            Light = reader.ReadRGB();
            Size = reader.ReadVector2();
            if (ToSize != null)
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

            if (ToSize != null)
            {
                Projectile.Resize((int) MathHelper.Lerp(Size.X, ToSize.Value.X, completion),
                    (int) MathHelper.Lerp(Size.Y, ToSize.Value.Y, completion));
            }
            else
            {
                Projectile.Resize((int) Size.X, (int) Size.Y);
            }

            Lighting.AddLight(Projectile.Center,
                (new Color(Light.R, Light.G, Light.B) * Light.A * completion).ToVector3());
        }
    }

    public static void CreateExplosion(IEntitySource source, DamageClass dmgClass, Vector2 position, Vector2 size,
        int damage, float kb, int lifetime, int iframes, int owner = -1, bool friendly = true, Vector2? toSize = null,
        Color light = default, string name = "")
    {
        Projectile proj =
            Main.projectile[
                Projectile.NewProjectile(source, position, Vector2.Zero,
                    ModContent.ProjectileType<ExplosionProjectile>(), damage, kb, owner)];
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
}
