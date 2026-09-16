using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace GbfRightClickBack;

/// <summary>Mode tema UI.</summary>
internal enum UiTheme
{
    Light = 0,
    Dark = 1,
}

/// <summary>
/// Palet warna dan helper tema untuk jendela UI.
/// Apply() mengubah warna semua kontrol anak secara rekursif.
/// </summary>
internal static class Theme
{
    public static Color Window { get; private set; } = SystemColors.Control;
    public static Color Card { get; private set; } = Color.White;
    public static Color Text { get; private set; } = SystemColors.ControlText;
    public static Color SubText { get; private set; } = Color.FromArgb(110, 110, 110);
    public static Color Accent { get; private set; } = Color.FromArgb(0, 120, 215);
    public static Color AccentHover { get; private set; } = Color.FromArgb(0, 95, 184);
    public static Color AccentText { get; private set; } = Color.White;
    public static Color Border { get; private set; } = Color.FromArgb(204, 204, 204);
    public static Color Field { get; private set; } = Color.White;
    public static Color FieldText { get; private set; } = SystemColors.ControlText;
    public static Color FieldBorder { get; private set; } = Color.FromArgb(204, 204, 204);
    public static Color StatusOn { get; private set; } = Color.FromArgb(46, 160, 67);
    public static Color StatusOff { get; private set; } = Color.FromArgb(150, 150, 150);

    /// <summary>Terapkan palet tema ke sebuah form (rekursif ke semua kontrol).</summary>
    public static void Apply(Control root, UiTheme theme)
    {
        if (theme == UiTheme.Dark)
        {
            Window = Color.FromArgb(32, 32, 36);
            Card = Color.FromArgb(44, 44, 50);
            Text = Color.FromArgb(235, 235, 235);
            SubText = Color.FromArgb(165, 165, 170);
            Accent = Color.FromArgb(76, 160, 255);
            AccentHover = Color.FromArgb(56, 132, 220);
            AccentText = Color.White;
            Border = Color.FromArgb(64, 64, 72);
            Field = Color.FromArgb(30, 30, 34);
            FieldText = Color.FromArgb(235, 235, 235);
            FieldBorder = Color.FromArgb(70, 70, 78);
            StatusOn = Color.FromArgb(63, 185, 80);
            StatusOff = Color.FromArgb(140, 140, 148);
        }
        else
        {
            Window = Color.FromArgb(243, 243, 245);
            Card = Color.White;
            Text = Color.FromArgb(28, 28, 32);
            SubText = Color.FromArgb(110, 110, 118);
            Accent = Color.FromArgb(0, 120, 215);
            AccentHover = Color.FromArgb(0, 95, 184);
            AccentText = Color.White;
            Border = Color.FromArgb(214, 214, 220);
            Field = Color.White;
            FieldText = Color.FromArgb(28, 28, 32);
            FieldBorder = Color.FromArgb(190, 190, 198);
            StatusOn = Color.FromArgb(46, 160, 67);
            StatusOff = Color.FromArgb(150, 150, 150);
        }

        ApplyRecursive(root);
    }

    private static void ApplyRecursive(Control control)
    {
        switch (control)
        {
            case Form form:
                form.BackColor = Window;
                form.ForeColor = Text;
                break;

            case Panel panel:
                panel.BackColor = panel.Tag is string t && t == "card" ? Card : Window;
                panel.ForeColor = Text;
                break;

            case Label label:
                label.BackColor = Color.Transparent;
                label.ForeColor = label.Tag is string lt && lt == "sub" ? SubText : Text;
                break;

            case Button button:
                ApplyButton(button);
                break;

            case TextBox textBox:
                // TextBox does not support Color.Transparent — use parent bg for readonly
                textBox.BackColor = textBox.ReadOnly
                    ? (textBox.Parent?.BackColor ?? Card)
                    : Field;
                textBox.ForeColor = FieldText;
                textBox.BorderStyle = BorderStyle.FixedSingle;
                break;

            case ComboBox comboBox:
                comboBox.BackColor = Field;
                comboBox.ForeColor = FieldText;
                comboBox.FlatStyle = FlatStyle.Flat;
                break;

            case CheckBox checkBox:
                checkBox.BackColor = Color.Transparent;
                checkBox.ForeColor = Text;
                break;

            case RadioButton radioButton:
                radioButton.BackColor = Color.Transparent;
                radioButton.ForeColor = Text;
                break;

            case NumericUpDown numeric:
                numeric.BackColor = Field;
                numeric.ForeColor = FieldText;
                numeric.BorderStyle = BorderStyle.FixedSingle;
                break;

            case ListBox listBox:
                listBox.BackColor = Field;
                listBox.ForeColor = FieldText;
                listBox.BorderStyle = BorderStyle.FixedSingle;
                break;

            case ListView listView:
                listView.BackColor = Field;
                listView.ForeColor = FieldText;
                listView.BorderStyle = BorderStyle.FixedSingle;
                break;
        }

        foreach (Control child in control.Controls)
            ApplyRecursive(child);
    }

    private static void ApplyButton(Button button)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 1;
        button.Cursor = Cursors.Hand;

        if (button.Tag is string tag && tag == "primary")
        {
            button.BackColor = Accent;
            button.ForeColor = AccentText;
            button.FlatAppearance.BorderColor = Accent;
            button.FlatAppearance.MouseOverBackColor = AccentHover;
        }
        else
        {
            button.BackColor = Card;
            button.ForeColor = Text;
            button.FlatAppearance.BorderColor = FieldBorder;
            button.FlatAppearance.MouseOverBackColor = Border;
        }
    }
}
