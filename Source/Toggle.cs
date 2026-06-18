using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Lens;

public class Toggle : CheckBox
{
   private Rectangle _figureBounds;
   private GraphicsPath _figurePath = null!;
   private GraphicsPath _focusPath  = null!;

   private bool _isMouseOver;
   private int _toggleSize;

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
      var rect = this._figureBounds;
      var toggleSize = this._toggleSize;

      e.Graphics.Clear(this.Parent?.BackColor ?? this.BackColor);

      e.Graphics.PixelOffsetMode = PixelOffsetMode.Half;
      e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

      if (this.Focused)
      {
         using var focusPen = new Pen(Colors.Focus, 2f);
         e.Graphics.DrawPath(focusPen, this._focusPath);
      }

      var thumbColor = this._isMouseOver ? Colors.ThumbHover : Colors.Thumb;
      if (this.Checked)
      {
         using var trackBrush = new SolidBrush(Colors.TrackActive);
         using var thumbBrush = new SolidBrush(thumbColor);
         e.Graphics.FillPath(trackBrush, this._figurePath);
         e.Graphics.FillEllipse(thumbBrush,
            new Rectangle(this.Width - rect.Height - 3, rect.Y, toggleSize, toggleSize));
      }
      else
      {
         using var trackBrush = new LinearGradientBrush(rect, Colors.TrackBase,
            Color.FromArgb(0x33, Colors.TrackBase), LinearGradientMode.Vertical);
         using var thumbBrush = new SolidBrush(thumbColor);
         e.Graphics.FillPath(trackBrush, this._figurePath);
         e.Graphics.FillEllipse(thumbBrush, new Rectangle(rect.X, rect.Y, toggleSize, toggleSize));
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
      this._figurePath = this.BuildFigurePath();
      this._focusPath = this.BuildFocusPath();
      this._figureBounds = Rectangle.Round(this._figurePath.GetBounds());
      this._toggleSize = this._figureBounds.Height;
   }

   private void HandleMouseEnter(object? sender, EventArgs e)
   {
      this._isMouseOver = true;
      this.Invalidate();
   }

   private void HandleMouseLeave(object? sender, EventArgs e)
   {
      this._isMouseOver = false;
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
   }
}