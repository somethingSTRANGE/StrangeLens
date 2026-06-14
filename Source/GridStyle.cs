using System.ComponentModel;

namespace Lens
{
   public enum GridStyleOptions
   {
      [Description("None")] None = 0,
      [Description("Solid")] Solid = 1,
      [Description("Dash")] Dash = 2,
      [Description("Dot")] Dot = 3,
      [Description("Dash, Dot")] DashDot = 4,
      [Description("Dash, Dot, Dot")] DashDotDot = 5
   }
}
