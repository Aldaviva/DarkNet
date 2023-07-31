using System.Drawing;

namespace Dark.Net;

/// <summary>
/// Extra parameters that override the non-client area colors of a window in Windows 11 or later. On earlier versions of Windows, these have no effect.
/// </summary>
public class ThemeOptions {

    /// <summary>
    /// <para>Override the background color of the title bar in Windows 11 or later.</para>
    /// <para>If <see langword="null"/>, the title bar color will be left unchanged, although it will still be affected by the chosen <see cref="Theme"/> and the previous value of this property.</para>
    /// <para>If you previously set this property and want to undo it, setting this to <see cref="DefaultColor"/> will revert it to the standard OS color for your chosen <see cref="Theme"/>.</para>
    /// <para>Setting this property has no effect on Windows 10 and earlier versions.</para>
    /// </summary>
    public Color? TitleBarBackgroundColor { get; set; }

    /// <summary>
    /// <para>Override the text color of the title bar in Windows 11 or later. Does not affect the minimize, maximize, or close buttons, just the caption text.</para>
    /// <para>If <see langword="null"/>, the title bar will be left unchanged, although it will still be affected by the chosen <see cref="Theme"/> and the previous value of this property.</para>
    /// <para>If you previously set this property and want to undo it, setting this to <see cref="DefaultColor"/> will revert it to the standard OS color for your chosen <see cref="Theme"/>.</para>
    /// <para>Setting this property has no effect on Windows 10 and earlier versions.</para>
    /// </summary>
    public Color? TitleBarTextColor { get; set; }

    /// <summary>
    /// <para>Override the border color of the window in Windows 11 or later. The border goes all the way around the entire window, not just around the title bar.</para>
    /// <para>To remove the window border entirely, set this to <see cref="NoWindowBorder"/>.</para>
    /// <para>If <see langword="null"/>, the window's border color will be left unchanged, although it will still be affected by the chosen <see cref="Theme"/> and the previous value of this property.</para>
    /// <para>If you previously set this property and want to undo it, setting this to <see cref="DefaultColor"/> will revert it to the standard OS color for your chosen <see cref="Theme"/>.</para>
    /// <para>Setting this property has no effect on Windows 10 and earlier versions.</para>
    /// </summary>
    public Color? WindowBorderColor { get; set; }

    /// <summary>
    /// When set as the value of <see cref="WindowBorderColor"/>, removes the window border. Windows 11 or later only.
    /// </summary>
    public static readonly Color NoWindowBorder = Color.FromArgb(0xFF, 0xFE, 0xFF, 0xFF);

    /// <summary>
    /// When set as the value of <see cref="TitleBarTextColor"/>, <see cref="TitleBarBackgroundColor"/>, or <see cref="WindowBorderColor"/>, reverts the color to the standard Os light or dark color for the active <see cref="Theme"/>. Useful if you previously set a custom color, and then want to reset it. 
    /// </summary>
    public static readonly Color DefaultColor = Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF);

}