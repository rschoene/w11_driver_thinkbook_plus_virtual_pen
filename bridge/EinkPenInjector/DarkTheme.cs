using System.Drawing;
using System.Runtime.InteropServices;

namespace EinkPenInjector;

// Always-dark theme built from plain WinForms controls (no extra libraries).
internal static class DarkTheme
{
    public static readonly Color Background = Color.FromArgb(32, 32, 32);
    public static readonly Color Surface = Color.FromArgb(45, 45, 45);
    public static readonly Color SurfaceHover = Color.FromArgb(62, 62, 62);
    public static readonly Color Border = Color.FromArgb(85, 85, 85);
    public static readonly Color Text = Color.FromArgb(240, 240, 240);
    public static readonly Color TextMuted = Color.FromArgb(160, 160, 160);
    public static readonly Color Accent = Color.FromArgb(0, 140, 255);

    public static void ApplyTitleBar(IntPtr handle)
    {
        int enabled = 1;
        // 20 = DWMWA_USE_IMMERSIVE_DARK_MODE (19 on early Windows 10 builds).
        if (DwmSetWindowAttribute(handle, 20, ref enabled, sizeof(int)) != 0)
        {
            DwmSetWindowAttribute(handle, 19, ref enabled, sizeof(int));
        }
    }

    public static void Apply(Control root)
    {
        root.BackColor = Background;
        root.ForeColor = Text;
        foreach (Control control in root.Controls)
        {
            switch (control)
            {
                case Button button:
                    StyleButton(button);
                    break;
                case ComboBox combo:
                    StyleCombo(combo);
                    break;
                case LinkLabel link:
                    link.LinkColor = Accent;
                    link.ActiveLinkColor = Color.White;
                    link.VisitedLinkColor = Accent;
                    link.BackColor = Background;
                    break;
                default:
                    control.BackColor = Background;
                    control.ForeColor = Text;
                    break;
            }

            if (control.HasChildren)
            {
                Apply(control);
            }
        }
    }

    private static void StyleButton(Button button)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.UseVisualStyleBackColor = false;
        button.BackColor = Surface;
        button.ForeColor = Text;
        button.FlatAppearance.BorderColor = Border;
        button.FlatAppearance.MouseOverBackColor = SurfaceHover;
        button.FlatAppearance.MouseDownBackColor = Accent;
        button.Padding = new Padding(10, 3, 10, 3);
        button.EnabledChanged += (_, _) => button.ForeColor = button.Enabled ? Text : TextMuted;
        button.ForeColor = button.Enabled ? Text : TextMuted;
    }

    private static void StyleCombo(ComboBox combo)
    {
        combo.FlatStyle = FlatStyle.Flat;
        combo.BackColor = Surface;
        combo.ForeColor = Text;
        combo.DrawMode = DrawMode.OwnerDrawFixed;
        combo.ItemHeight = (int)(combo.Font.Height * 1.5);
        combo.DrawItem += (_, e) =>
        {
            if (e.Index < 0)
            {
                return;
            }

            bool selected = (e.State & DrawItemState.Selected) != 0 &&
                (e.State & DrawItemState.ComboBoxEdit) == 0;
            using var back = new SolidBrush(selected ? Accent : Surface);
            e.Graphics.FillRectangle(back, e.Bounds);
            TextRenderer.DrawText(e.Graphics, combo.Items[e.Index]?.ToString(), combo.Font,
                new Rectangle(e.Bounds.X + 4, e.Bounds.Y, e.Bounds.Width - 4, e.Bounds.Height),
                Text, TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
        };
    }

    public sealed class MenuColors : ProfessionalColorTable
    {
        public override Color MenuItemSelected => SurfaceHover;
        public override Color MenuItemBorder => Border;
        public override Color ToolStripDropDownBackground => Surface;
        public override Color ImageMarginGradientBegin => Surface;
        public override Color ImageMarginGradientMiddle => Surface;
        public override Color ImageMarginGradientEnd => Surface;
        public override Color MenuBorder => Border;
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);
}
