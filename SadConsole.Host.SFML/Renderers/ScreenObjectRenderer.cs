﻿using System;
using SFML.Graphics;
using Color = SFML.Graphics.Color;
using SadConsole.Host;
using SadRogue.Primitives;

namespace SadConsole.Renderers
{
    /// <summary>
    /// Draws a <see cref="IScreenSurface"/>.
    /// </summary>
    /// <remarks>
    /// This renderer caches the entire drawing of the surface's cells, including the tint of the object.
    /// </remarks>
    public class ScreenObjectRenderer : IRenderer
    {
        public RenderTexture BackingTexture;

        protected IntRect[] _renderRects;

        public virtual void Attach(IScreenSurface screen)
        {
        }

        public virtual void Detatch(IScreenSurface screen)
        {
            BackingTexture.Dispose();
            BackingTexture = null;
        }

        public virtual void Render(IScreenSurface screen)
        {
            // Draw call for texture
            GameHost.Instance.DrawCalls.Enqueue(new DrawCalls.DrawCallTexture(BackingTexture.Texture, new SFML.System.Vector2i(screen.AbsoluteArea.Position.X, screen.AbsoluteArea.Position.Y)));
        }

        public virtual void Refresh(IScreenSurface screen, bool force = false)
        {
            if (!force && !screen.IsDirty && BackingTexture != null) return;

            // Update texture if something is out of size.
            if (BackingTexture == null || screen.AbsoluteArea.Width != (int)BackingTexture.Size.X || screen.AbsoluteArea.Height != (int)BackingTexture.Size.Y)
            {
                BackingTexture?.Dispose();
                BackingTexture = new RenderTexture((uint)screen.AbsoluteArea.Width, (uint)screen.AbsoluteArea.Height);
            }

            // Update cached drawing rectangles if something is out of size.
            if (_renderRects == null || _renderRects.Length != screen.Surface.View.Width * screen.Surface.View.Height || _renderRects[0].Width != screen.FontSize.X || _renderRects[0].Height != screen.FontSize.Y)
            {
                _renderRects = new IntRect[screen.Surface.View.Width * screen.Surface.View.Height];

                for (int i = 0; i < _renderRects.Length; i++)
                {
                    var position = SadRogue.Primitives.Point.FromIndex(i, screen.Surface.View.Width);
                    _renderRects[i] = screen.Font.GetRenderRect(position.X, position.Y, screen.FontSize).ToIntRect();
                }
            }

            // Render parts of the surface
            RefreshBegin(screen);
            if (screen.Tint.A != 255)
                RefreshCells(screen.Surface, screen.Font);
            RefreshTint(screen);
            RefreshEnd(screen);

            screen.IsDirty = false;
        }


        protected virtual void RefreshBegin(IScreenSurface surface)
        {
            BackingTexture.Clear(Color.Transparent);
            Host.Global.SharedSpriteBatch.Reset(BackingTexture, RenderStates.Default, Transform.Identity);
        }

        protected virtual void RefreshEnd(IScreenSurface surface)
        {
            Host.Global.SharedSpriteBatch.End();
            BackingTexture.Display();
        }

        protected virtual void RefreshCells(CellSurface cellSurface, Font font)
        {
            if (cellSurface.DefaultBackground.A != 0)
                Host.Global.SharedSpriteBatch.DrawQuad(new IntRect(0, 0, (int)BackingTexture.Size.X, (int)BackingTexture.Size.Y), font.GlyphRects[font.SolidGlyphIndex].ToIntRect(), cellSurface.DefaultBackground.ToSFMLColor(), ((SadConsole.Host.GameTexture)font.Image).Texture);

            int rectIndex = 0;

            for (int y = 0; y < cellSurface.View.Height; y++)
            {
                int i = ((y + cellSurface.ViewPosition.Y) * cellSurface.BufferWidth) + cellSurface.ViewPosition.X;

                for (int x = 0; x < cellSurface.View.Width; x++)
                {
                    ColoredGlyph cell = cellSurface.Cells[i];

                    if (!cell.IsVisible) continue;

                    Host.Global.SharedSpriteBatch.DrawCell(cell, _renderRects[rectIndex], !cell.Background.Equals(Color.Transparent) && cell.Background != cellSurface.DefaultBackground, font);

                    cell.IsDirty = false;

                    i++;
                    rectIndex++;
                }
            }
        }

        protected virtual void RefreshTint(IScreenSurface surface)
        {
            if (surface.Tint.A != 0)
                GameHost.Instance.DrawCalls.Enqueue(new DrawCalls.DrawCallColor(surface.Tint.ToSFMLColor(), ((SadConsole.Host.GameTexture)surface.Font.Image).Texture, surface.AbsoluteArea.ToIntRect(), surface.Font.SolidGlyphRectangle.ToIntRect()));
        }


        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _renderRects = null;
                }

                BackingTexture?.Dispose();
                BackingTexture = null;

                disposedValue = true;
            }
        }

         ~ScreenObjectRenderer()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(false);
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            Dispose(true);
             GC.SuppressFinalize(this);
        }
        #endregion
    }
}
