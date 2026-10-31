namespace Dalamud.Interface.Fonts;

/// <summary>Draws a text decoration over a span of a line.</summary>
/// <param name="span">Where to draw, and with what.</param>
/// <remarks><ul>
/// <li>Called on the main thread.</li>
/// <li>Called once per run of adjacent text on a line with the same decoration, context, and text color.</li>
/// <li>A drawer that throws is logged and not called again.</li>
/// </ul></remarks>
/// <seealso cref="IUiBuilder.RegisterTextDecoration"/>
public delegate void TextDecorationDrawer(in TextDecorationSpan span);
