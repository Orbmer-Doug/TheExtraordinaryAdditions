using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.Graphics.Light;
using TheExtraordinaryAdditions.Core.Graphics;

namespace TheExtraordinaryAdditions.Common.Particles;

public partial class ParticleRegistry
{
    private struct DetailedBlastData
    {
        public Vector2 From;
        public Vector2 To;
        public Color? AuraCol;
        public bool AltTex;
    }

    private readonly struct DetailedBlastParticleDefinition
    {
        static DetailedBlastParticleDefinition()
        {
            RegisterDefinition(new ParticleDefinition(
                type: ParticleTypes.DetailedBlast,
                texture: AssetRegistry.GetTexture(AdditionsTexture.DetailedBlast),
                blendState: BlendState.Additive,
                update: static (ref ParticleData p) =>
                {
                    ref DetailedBlastData custom = ref p.GetCustomData<DetailedBlastData>();
                    float progress = Animators.Circ.OutFunction(p.TimeRatio);
                    Vector2 scale = Vector2.Lerp(custom.From, custom.To, progress);
                    p.Scale = scale.Length();
                    p.Opacity = GetLerpBump(0f, 0.1f, 1f, 0.5f, p.TimeRatio);

                    float hitboxTiles = p.Scale / 16f;
                    float desiredRadius = hitboxTiles;
                    float intensity = CalculateIntensityForRadius(desiredRadius, LightMaskMode.None, 0.5f);
                    Vector3 lightColor = p.Color.ToVector3() * intensity * p.Opacity;
                    Lighting.AddLight(p.Position, lightColor);

                    p.Velocity *= 0.95f;
                },
                draw: static (ref ParticleData p, SpriteBatch sb) =>
                {
                    ref DetailedBlastData custom = ref p.GetCustomData<DetailedBlastData>();
                    Texture2D tex = custom.AltTex ? AssetRegistry.GetTexture(AdditionsTexture.DetailedBlast2) : TypeDefinitions[(byte)ParticleTypes.DetailedBlast].Texture;
                    Vector2 scale = Vector2.Lerp(custom.From, custom.To, Animators.Circ.OutFunction(p.TimeRatio));
                    Rectangle target = ToTarget(p.Position, scale);
                    if (custom.AuraCol.HasValue)
                    {
                        Texture2D aura = AssetRegistry.GetTexture(AdditionsTexture.HollowCircleHighRes);
                        Vector2 orig = aura.Size() / 2;
                        sb.DrawBetterRect(aura, target, null, custom.AuraCol.Value * p.Opacity, p.Rotation, orig, 0);
                        sb.DrawBetterRect(aura, target, null, custom.AuraCol.Value * p.Opacity, p.Rotation, orig, 0);
                    }
                    sb.DrawBetterRect(tex, target, null, p.Color * p.Opacity, p.Rotation, tex.Size() / 2f, 0);
                },
                drawType: DrawTypes.Pixelize,
                canCull: false
                ));
        }
    }

    public static void SpawnDetailedBlastParticle(Vector2 position, Vector2 fromSize, Vector2 toSize, Vector2 velocity, int life, Color color, float? rotation = null, Color? col = null, bool altTex = false)
    {
        ParticleData particle = new()
        {
            Position = position,
            Velocity = velocity,
            Lifetime = life,
            Scale = fromSize.Length(),
            Color = color,
            Opacity = 1f,
            Rotation = rotation ?? RandomRotation(),
            Type = ParticleTypes.DetailedBlast,
            Width = 2,
            Height = 2
        };
        ref DetailedBlastData custom = ref particle.GetCustomData<DetailedBlastData>();
        custom.From = fromSize;
        custom.To = toSize;
        custom.AuraCol = col;
        custom.AltTex = altTex;
        SafeSpawn(particle);
    }
}
