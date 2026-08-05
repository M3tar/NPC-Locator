using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace NpcLocator.UI;

/// <summary>Draws a compact inventory-style panel without stretching the shaded menu center.</summary>
internal static class NativeMenuPanel
{
    private static readonly Rectangle SourceRect = new(0, 256, 60, 60);

    private const int SliceSize = 20;
    private const int ShadowOffset = 8;

    public static void Draw(SpriteBatch b, Rectangle bounds, float opacity = 1f)
    {
        float clampedOpacity = Math.Clamp(opacity, 0f, 1f);
        DrawShadow(b, bounds, clampedOpacity);

        int slice = SliceSize;
        int middleWidth = Math.Max(1, bounds.Width - slice * 2);
        int middleHeight = Math.Max(1, bounds.Height - slice * 2);
        Color tint = Color.White * clampedOpacity;

        DrawSlice(b, new Rectangle(0, 0, slice, slice), new Rectangle(bounds.X, bounds.Y, slice, slice), tint);
        DrawSlice(b, new Rectangle(slice, 0, slice, slice), new Rectangle(bounds.X + slice, bounds.Y, middleWidth, slice), tint);
        DrawSlice(b, new Rectangle(slice * 2, 0, slice, slice), new Rectangle(bounds.Right - slice, bounds.Y, slice, slice), tint);
        DrawSlice(b, new Rectangle(0, slice, slice, slice), new Rectangle(bounds.X, bounds.Y + slice, slice, middleHeight), tint);
        DrawSlice(
            b,
            new Rectangle(slice + slice / 2, slice + slice / 2, 1, 1),
            new Rectangle(bounds.X + slice, bounds.Y + slice, middleWidth, middleHeight),
            tint
        );
        DrawSlice(b, new Rectangle(slice * 2, slice, slice, slice), new Rectangle(bounds.Right - slice, bounds.Y + slice, slice, middleHeight), tint);
        DrawSlice(b, new Rectangle(0, slice * 2, slice, slice), new Rectangle(bounds.X, bounds.Bottom - slice, slice, slice), tint);
        DrawSlice(b, new Rectangle(slice, slice * 2, slice, slice), new Rectangle(bounds.X + slice, bounds.Bottom - slice, middleWidth, slice), tint);
        DrawSlice(b, new Rectangle(slice * 2, slice * 2, slice, slice), new Rectangle(bounds.Right - slice, bounds.Bottom - slice, slice, slice), tint);
    }

    private static void DrawShadow(SpriteBatch b, Rectangle bounds, float opacity)
    {
        Color tint = Color.Black * (0.28f * opacity);
        b.Draw(
            Game1.staminaRect,
            new Rectangle(bounds.Right, bounds.Y + ShadowOffset, ShadowOffset, bounds.Height),
            tint
        );
        b.Draw(
            Game1.staminaRect,
            new Rectangle(bounds.X + ShadowOffset, bounds.Bottom, bounds.Width, ShadowOffset),
            tint
        );
    }

    private static void DrawSlice(SpriteBatch b, Rectangle relativeSource, Rectangle destination, Color tint)
    {
        Rectangle source = new(
            SourceRect.X + relativeSource.X,
            SourceRect.Y + relativeSource.Y,
            relativeSource.Width,
            relativeSource.Height
        );
        b.Draw(Game1.menuTexture, destination, source, tint);
    }
}
