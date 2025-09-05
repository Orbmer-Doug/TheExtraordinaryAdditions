using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Common.Particles.Metaball;
using TheExtraordinaryAdditions.Content.Buffs.Debuff;
using TheExtraordinaryAdditions.Content.Items.Weapons.Melee.Late;
using TheExtraordinaryAdditions.Content.Projectiles.Base;
using TheExtraordinaryAdditions.Content.Projectiles.Magic.Late.Zenith;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using static Terraria.ModLoader.BackupIO;

namespace TheExtraordinaryAdditions.Content.Projectiles.Summoner.Late;

public class DelugeWhip : BaseWhip
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.TidalWhip);
    public override int SegmentSkip => 8;

    public override void SafeAI()
    {
        if (Time == 0)
        {
            if (this.RunLocal())
            {
                int x = (int)MathHelper.Clamp((int)Projectile.Center.Distance(Modded.mouseWorld), 100, 700);
                Projectile.Size = new(x, (int)Utils.Remap(x, 100, 700, 80, 250));
                this.Sync();
            }
        }
        Visuals();
    }

    public override void CrackEffects()
    {
        for (int i = 0; i < 20; i++)
            MetaballRegistry.SpawnAbyssalMetaball(Tip, Main.rand.NextVector2Circular(5f, 5f), Main.rand.Next(30, 40), Main.rand.Next(30, 50));

        SoundID.Item153.Play(Tip, 1f, .14f);
    }

    public override void NPCHitEffects(NPC target, NPC.HitInfo hit, in Vector2 pos, in Vector2 vel, in int index)
    {
        if (index > (WhipPoints.Count - 3))
        {
            target.AddBuff(ModContent.BuffType<Wavebroken>(), SecondsToFrames(4));
        }

        Projectile.damage = (int)(Projectile.damage * .8f);
        return;
    }

    public override void ModifyNPCEffects(NPC target, ref NPC.HitModifiers modifiers, in Vector2 pos, in int index)
    {

    }

    public void Visuals()
    {
        Projectile.scale = MathHelper.Lerp(.9f, 1.2f, GetLerpBump(0f, .4f, 1f, .6f, Completion)) * GetThin(GetCompletion());
    }

    public override float LineWidth(float completion)
    {
        return 8f;
    }

    public override Color LineColor(SystemVector2 completion, Vector2 position)
    {
        return MulticolorLerp(Cos01(Main.GlobalTimeWrappedHourly), AbyssalCurrents.BrackishPalette);
    }

    public override void DrawLine()
    {
        if (Line != null && !Line._disposed)
        {
            ManagedShader shader = ShaderRegistry.WaterCurrent;
            shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.DarkTurbulentNoise), 1, SamplerState.LinearWrap);
            Line.DrawTrail(shader, WhipPoints.Points);
        }
    }

    public override void DrawSegments()
    {
        Texture2D texture = Projectile.ThisProjectileTexture();

        Rectangle hiltFrame = new(0, 0, 18, 26);
        Rectangle seg1Frame = new(0, 26, 18, 20);
        Rectangle seg2Frame = new(0, 46, 18, 14);
        Rectangle seg3Frame = new(0, 60, 18, 12);
        Rectangle tipFrame = new(0, 72, 18, 28);

        int len = WhipPoints.Points.Length - 1;
        for (int i = 0; i < len; i++)
        {
            Vector2 pos = WhipPoints.Points[i];
            Vector2 next = WhipPoints.Points[i + 1];

            Rectangle frame;
            bool hilt = i == 0;
            bool tip = i == len - 1;
            bool shouldDraw = i % SegmentSkip == (SegmentSkip - 1);
            if (hilt || tip)
                shouldDraw = true;

            if (hilt)
                frame = hiltFrame;
            else if (i < (len / 3))
                frame = seg1Frame;
            else if (i < (len / 2))
                frame = seg2Frame;
            else if (i < (len - 1))
                frame = seg3Frame;
            else
                frame = tipFrame;

            if (shouldDraw)
            {
                Vector3 light = Lighting.GetSubLight(pos);
                Color color = Projectile.GetAlpha(new(light.X, light.Y, light.Z));
                float rotation = (next - pos).ToRotation() - MathHelper.PiOver2;
                Vector2 orig = frame.Size() / 2;
                SpriteEffects flip = Owner.direction < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

                Main.spriteBatch.DrawBetter(texture, pos, frame, color, rotation, orig, tip ? Projectile.scale : 1f, flip);
            }
        }
    }
}
