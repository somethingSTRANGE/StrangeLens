namespace Lens;

/// <summary>Raw SVG path data for a single icon. Duotone icons supply both <see cref="Primary"/> and <see cref="Secondary"/>.</summary>
internal readonly record struct SvgData(string ViewBox, string[] Primary, string[]? Secondary = null);
