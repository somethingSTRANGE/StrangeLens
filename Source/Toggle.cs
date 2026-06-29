// -------------------------------------------------------------------------------------
// <copyright file="Toggle.cs">
//   Copyright (c) 2026
//   Licensed under the MIT License. See LICENSE file in the project root.
// </copyright>
// -------------------------------------------------------------------------------------

namespace Lens;

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

public sealed class Toggle : CheckBox
{
   private Rectangle figureBounds;

   private GraphicsPath figurePath = null!;

   private GraphicsPath focusPath = null!;

   private bool isMouseOver;

   private int toggleSize;

   public Toggle()
   {
      this.Size = this.MinimumSize = new Size(40, 23);
      this.BuildPaths();

      this.MouseEnter += this.HandleMouseEnter;
      this.MouseLeave += this.HandleMouseLeave;
      this.SizeChanged += this.HandleSizeChange;
   }

   protected override void OnPaint(PaintEventArgs e)
   {
      var rect = this.figureBounds;
      var size = this.toggleSize;

      e.Graphics.Clear(this.Parent?.BackColor ?? this.BackColor);

      e.Graphics.PixelOffsetMode = PixelOffsetMode.Half;
      e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

      if (this.Focused)
      {
         using var focusPen = new Pen(Colors.Focus, 2f);
         e.Graphics.DrawPath(focusPen, this.focusPath);
      }

      var thumbColor = this.isMouseOver ? Colors.ThumbHover : Colors.Thumb;
      var trackColor = this.isMouseOver ? Colors.TrackHover : Colors.TrackActive;

      if (this.Checked)
      {
         using var trackBrush = new SolidBrush(trackColor);
         using var thumbBrush = new SolidBrush(thumbColor);
         e.Graphics.FillPath(trackBrush, this.figurePath);
         e.Graphics.FillEllipse(thumbBrush, new Rectangle(this.Width - rect.Height - 3, rect.Y, size, size));
      }
      else
      {
         using var trackBrush = new LinearGradientBrush(
            rect,
            Colors.TrackBase,
            Color.FromArgb(0x33, Colors.TrackBase),
            LinearGradientMode.Vertical);
         using var thumbBrush = new SolidBrush(thumbColor);
         e.Graphics.FillPath(trackBrush, this.figurePath);
         e.Graphics.FillEllipse(thumbBrush, new Rectangle(rect.X, rect.Y, size, size));
      }
   }

   private GraphicsPath BuildFigurePath()
   {
      var arcSize = this.Height - 6;
      var leftArc = new Rectangle(3, 3, arcSize, arcSize);
      var rightArc = new Rectangle(this.Width - arcSize - 3, 3, arcSize, arcSize);

      var path = new GraphicsPath();
      path.StartFigure();
      path.AddArc(leftArc, 90, 180);
      path.AddArc(rightArc, 270, 180);
      path.CloseFigure();

      return path;
   }

   private GraphicsPath BuildFocusPath()
   {
      var arcSize = this.Height - 2;
      var leftArc = new Rectangle(1, 1, arcSize, arcSize);
      var rightArc = new Rectangle(this.Width - arcSize - 1, 1, arcSize, arcSize);

      var path = new GraphicsPath();
      path.StartFigure();
      path.AddArc(leftArc, 90, 180);
      path.AddArc(rightArc, 270, 180);
      path.CloseFigure();

      return path;
   }

   private void BuildPaths()
   {
      this.figurePath = this.BuildFigurePath();
      this.focusPath = this.BuildFocusPath();
      this.figureBounds = Rectangle.Round(this.figurePath.GetBounds());
      this.toggleSize = this.figureBounds.Height;
   }

   private void HandleMouseEnter(object? sender, EventArgs e)
   {
      this.isMouseOver = true;
      this.Invalidate();
   }

   private void HandleMouseLeave(object? sender, EventArgs e)
   {
      this.isMouseOver = false;
      this.Invalidate();
   }

   private void HandleSizeChange(object? sender, EventArgs e)
   {
      this.BuildPaths();
   }

   public static class Colors
   {
      public static Color Focus { get; set; } = Color.MediumSlateBlue;

      public static Color Thumb { get; set; } = Color.Gainsboro;

      public static Color ThumbHover { get; set; } = Color.White;

      public static Color TrackActive { get; set; } = Color.MediumSlateBlue;

      public static Color TrackBase { get; set; } = Color.Gray;

      public static Color TrackHover { get; set; } = Color.RoyalBlue;
   }
}
