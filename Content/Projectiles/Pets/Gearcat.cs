using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Buffs.Summon;
using TheExtraordinaryAdditions.Core.Globals;

namespace TheExtraordinaryAdditions.Content.Projectiles.Pets;

public class Gearcat : ModProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.GearCat);
    public Player Owner => Main.player[Projectile.owner];
    public GlobalPlayer Modded => Owner.Additions();
    public int Time
    {
        get => (int)Projectile.ai[0];
        set => Projectile.ai[0] = value;
    }

    public enum PetState
    {
        Walking,
        Flying,
    }

    public PetState State
    {
        get => (PetState)Projectile.ai[1];
        set => Projectile.ai[1] = (int)value;
    }

    public int JumpCounter
    {
        get => (int)Projectile.ai[2];
        set => Projectile.ai[2] = value;
    }

    public override void SetStaticDefaults()
    {
        Main.projFrames[Projectile.type] = 4;
        Main.projPet[Projectile.type] = true;
        ProjectileID.Sets.CharacterPreviewAnimations[Projectile.type] = ProjectileID.Sets.SimpleLoop(0, Main.projFrames[Projectile.type], 5);
    }

    public override void SetDefaults()
    {
        Projectile.width = 36;
        Projectile.height = 46;
        Projectile.friendly = Projectile.tileCollide = Projectile.netImportant = true;
        Projectile.penetrate = -1;
        Projectile.timeLeft *= 5;
    }

    public override void AI()
    {
        if (Owner.Available() && Owner.HasBuff(ModContent.BuffType<GearcatBuff>()))
            Projectile.timeLeft = 2;

        float distanceToPlayer = Projectile.Distance(Owner.Center);
        if (distanceToPlayer > 2000)
            Projectile.Center = Owner.Center;

        float verticalDistanceToPlayer = Math.Abs(Owner.Center.Y - Projectile.Center.Y);
        switch (State)
        {
            case PetState.Walking:
                Projectile.tileCollide = true;
                Projectile.frameCounter += (int)MathHelper.Clamp(MathF.Abs(Projectile.velocity.X) * .5f, 0f, 2f);
                if (Projectile.frameCounter > 3)
                {
                    Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Type];
                    Projectile.frameCounter = 0;
                }

                Vector2 center = Owner.MountedCenter;
                Vector2 expected = center - Vector2.UnitX * 50f * Owner.direction;
                Vector2 dest = RaytraceTiles(center, expected) ?? expected;
                int direction = (Owner.Center.X - Projectile.Center.X).NonZeroSign();
                float acceleration = Utils.Remap(Projectile.Distance(dest), 0f, 100f, .09f, .4f);

                if (Projectile.velocity.X.NonZeroSign() != direction)
                    acceleration *= 2f;

                Collision.StepUp(ref Projectile.position, ref Projectile.velocity, Projectile.width, Projectile.height, ref Projectile.stepSpeed, ref Projectile.gfxOffY);

                // Only jump when on the ground
                if (Projectile.velocity.Y == 0f && !Collision.CanHitLine(direction == -1 ? Projectile.Left : Projectile.Right, 1, 1, dest, 1, 1))
                {
                    int start = (int)Projectile.Center.X;
                    int end = start + (80 * direction);
                    int y = (int)Projectile.Bottom.Y - 1;
                    Vector2 start2 = new(start, y);
                    Vector2 end2 = new(end, y);
                    Vector2 ray = RaytraceTiles(start2, end2) ?? start2;

                    int obstacleHeight = 0;
                    for (int i = 0; i < 5; i++)
                    {
                        Tile tile = ParanoidTileRetrieval((int)(ray.X + (8 * direction)) / 16, ((int)ray.Y / 16) - i);
                        if (tile.HasUnactuatedTile && Main.tileSolid[tile.TileType])
                            obstacleHeight++;
                        else
                            break; // Stop counting when we hit air or a non-solid tile
                    }

                    if (obstacleHeight > 1 && JumpCounter < 3)
                    {
                        Projectile.velocity.Y = -(5f + obstacleHeight * 1.3f);
                        JumpCounter++;
                    }
                }
                else if (distanceToPlayer < 100f || Projectile.velocity.Y == 0f)
                {
                    // Reset jump counter when near player or after landing
                    JumpCounter = 0;
                }

                Projectile.velocity.X = MathHelper.Lerp(Projectile.velocity.X, Projectile.SafeDirectionTo(dest).X * 4f, acceleration);
                Projectile.velocity.Y = MathHelper.Clamp(Projectile.velocity.Y + .3f, -20f, 20f);

                if (distanceToPlayer > 1000 || verticalDistanceToPlayer > 300 || Owner.rocketDelay2 > 0)
                {
                    State = PetState.Flying;
                    Projectile.frame = Projectile.frameCounter = 0;
                }

                Projectile.rotation = direction == 1 ? 0f : MathHelper.Pi;
                break;
            case PetState.Flying:
                Projectile.tileCollide = false;

                Projectile.SetAnimation(3, 4);

                Vector2 idealPosition = Owner.MountedCenter - Vector2.UnitY * MathHelper.Lerp(10f, 20f, Sin01(Time * .06f)) - Vector2.UnitX * Owner.direction * 40;
                Vector2 goalVelocity = (idealPosition - Projectile.Center) * 0.03f;
                float approachAcceleration = 0.1f + MathF.Pow(InverseLerp(70, 0, distanceToPlayer), 2f) * 0.34f;
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, goalVelocity, approachAcceleration);
                Projectile.velocity *= 0.98f;

                if (distanceToPlayer < 200f && Owner.velocity.Y == 0f && Projectile.Bottom.Y <= Owner.Bottom.Y
                    && !Collision.SolidCollision(Projectile.position, Projectile.width, Projectile.height))
                    State = PetState.Walking;

                Vector2 pos = Projectile.Center + Projectile.velocity + PolarVector(21f * Projectile.direction, Projectile.rotation + MathHelper.PiOver2);
                Dust d = Dust.NewDustPerfect(pos + Main.rand.NextVector2Circular(3f, 3f), DustID.Torch, -Projectile.velocity * .01f, 0, default, Main.rand.NextFloat(.8f, 1.2f));
                d.noGravity = true;

                Projectile.rotation = Projectile.velocity.ToRotation();
                break;
        }
        Projectile.direction = Projectile.velocity.X.NonZeroSign();

        Time++;
    }

    public override bool PreDraw(ref Color lightColor)
    {
        int frames = 4;
        Texture2D texture = Projectile.ThisProjectileTexture();
        if (State == PetState.Flying)
        {
            frames = 3;
            texture = AssetRegistry.GetTexture(AdditionsTexture.GearCat_Fly);
        }

        float rotation = Projectile.rotation;
        Vector2 drawPosition = Projectile.Center + Vector2.UnitY * Projectile.gfxOffY - Main.screenPosition;
        SpriteEffects effects = MathF.Cos(Projectile.rotation) < 0f ? SpriteEffects.FlipVertically : SpriteEffects.None;
        Rectangle frame = texture.Frame(1, frames, 0, Projectile.frame);
        Vector2 origin = frame.Size() * .5f;

        Main.spriteBatch.Draw(texture, drawPosition, frame, Projectile.GetAlpha(lightColor), rotation, origin, Projectile.scale, effects, 0f);
        return false;
    }
}