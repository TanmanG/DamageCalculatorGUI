using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;

namespace Pickings_For_Kurtulmak
{
    public class PFKSettings
    {
        public PFKColorPalette CurrentColorPallete { get; set; }

        public PFKSettings()
        {
            CurrentColorPallete = null;
        }
    }

    public class PFKColorPalette
    {
        public int FontSizeOverride { get; set; }
        public string FontName { get; set; }
        public string PaletteName { get; set; }
        public Color Background { get; set; }
        public Color EntryNormal { get; set; }
        public Color EntryDisabled { get; set; }
        public Color EntryBatched { get; set; }
        public Color Text { get; set; }

        public Color CheckBoxUnticked { get; set; }
        public Color CheckboxTicked { get; set; }
        public Color CheckBoxPressed { get; set; }
        public Color CheckBoxCheck { get; set; }

        public Color ButtonText { get; set; }
        public Color ButtonBackground { get; set; }

        public Color ExitButtonBackground { get; set; }
        public Color ExitButtonIdle { get; set; }
        public Color ExitButtonHovered { get; set; }
        public Color ExitButtonPressed { get; set; }

        public Color MinimizeButtonBackground { get; set; }
        public Color MinimizeButtonIdle { get; set; }
        public Color MinimizeButtonHovered { get; set; }
        public Color MinimizeButtonPressed { get; set; }

        public Color Border { get; set; }
        public Color GraphBars { get; set; }
        public Color GraphBackground { get; set; }

        public PFKColorPalette(string palleteName, 
                                Color background, 
                                Color entryNormal, Color entryNormalDark, Color entryBatched, Color text,
                                Color checkboxUnchecked, Color checkboxChecked, Color checkboxPressed, Color checkboxCheck,
                                Color buttonText, Color buttonBackground,
                                string fontName, int fontSizeOverride,
                                Color border, Color graphBars, Color graphBackground,
                                Color exitButtonBackground, Color exitButtonIdle, Color exitButtonHovered, Color exitButtonPressed,
                                Color minimizeButtonBackground, Color minimizeButtonIdle, Color minimizeButtonHovered, Color minimizeButtonPressed)
        {
            this.PaletteName = palleteName;
            this.Background = background;
            this.EntryNormal = entryNormal;
            this.EntryDisabled = entryNormalDark;
            this.EntryBatched = entryBatched;
            this.Text = text;

            this.CheckBoxCheck = checkboxCheck;
            this.CheckboxTicked = checkboxChecked;
            this.CheckBoxUnticked = checkboxUnchecked;
            this.CheckBoxPressed = checkboxPressed;

            this.ButtonText = buttonText;
            this.ButtonBackground = buttonBackground;

            this.ExitButtonBackground = exitButtonBackground;
            this.ExitButtonIdle = exitButtonIdle;
            this.ExitButtonHovered = exitButtonHovered;
            this.ExitButtonPressed = exitButtonPressed;

            this.MinimizeButtonBackground = minimizeButtonBackground;
            this.MinimizeButtonIdle = minimizeButtonIdle;
            this.MinimizeButtonHovered = minimizeButtonHovered;
            this.MinimizeButtonPressed = minimizeButtonPressed;

            this.FontName = fontName;
            this.FontSizeOverride = fontSizeOverride;
            this.Border = border;
            this.GraphBars = graphBars;
            this.GraphBackground = graphBackground;
        }

        public PFKColorPalette(PFKColorPalette palette)
        {
            FontSizeOverride = palette.FontSizeOverride;
            FontName = palette.FontName;
            PaletteName = palette.PaletteName;
            Background = palette.Background;
            EntryNormal = palette.EntryNormal;
            EntryDisabled = palette.EntryDisabled;
            EntryBatched = palette.EntryBatched;
            Text = palette.Text;

            CheckBoxUnticked = palette.CheckBoxUnticked;
            CheckboxTicked = palette.CheckboxTicked;
            CheckBoxPressed = palette.CheckBoxPressed;
            CheckBoxCheck = palette.CheckBoxCheck;

            ButtonText = palette.ButtonText;
            ButtonBackground = palette.ButtonBackground;

            ExitButtonBackground = palette.ExitButtonBackground;
            ExitButtonIdle = palette.ExitButtonIdle;
            ExitButtonHovered = palette.ExitButtonHovered;
            ExitButtonPressed = palette.ExitButtonPressed;

            MinimizeButtonBackground = palette.MinimizeButtonBackground;
            MinimizeButtonIdle = palette.MinimizeButtonIdle;
            MinimizeButtonHovered = palette.MinimizeButtonHovered;
            MinimizeButtonPressed = palette.MinimizeButtonPressed;

            Border = palette.Border;
            GraphBars = palette.GraphBars;
            GraphBackground = palette.GraphBackground;
        }

        public PFKColorPalette()
        {
            PFKColorPalette defaultPalette = GetDefaultPalette("light");
            FontSizeOverride = defaultPalette.FontSizeOverride;
            FontName = defaultPalette.FontName;
            PaletteName = defaultPalette.PaletteName;
            Background = defaultPalette.Background;
            EntryNormal = defaultPalette.EntryNormal;
            EntryDisabled = defaultPalette.EntryDisabled;
            EntryBatched = defaultPalette.EntryBatched;
            Text = defaultPalette.Text;

            CheckBoxUnticked = defaultPalette.CheckBoxUnticked;
            CheckboxTicked = defaultPalette.CheckboxTicked;
            CheckBoxPressed = defaultPalette.CheckBoxPressed;
            CheckBoxCheck = defaultPalette.CheckBoxCheck;

            ButtonText = defaultPalette.ButtonText;
            ButtonBackground = defaultPalette.ButtonBackground;

            ExitButtonBackground = defaultPalette.ExitButtonBackground;
            ExitButtonIdle = defaultPalette.ExitButtonIdle;
            ExitButtonHovered = defaultPalette.ExitButtonHovered;
            ExitButtonPressed = defaultPalette.ExitButtonPressed;

            MinimizeButtonBackground = defaultPalette.MinimizeButtonBackground;
            MinimizeButtonIdle = defaultPalette.MinimizeButtonIdle;
            MinimizeButtonHovered = defaultPalette.MinimizeButtonHovered;
            MinimizeButtonPressed = defaultPalette.MinimizeButtonPressed;

            Border = defaultPalette.Border;
            GraphBars = defaultPalette.GraphBars;
            GraphBackground = defaultPalette.GraphBackground;
        }

        public static PFKColorPalette GetDefaultPalette(string type, string name = null)
        {
            return type switch
            {
                "pf2e" => new(palleteName: name ?? "pf2e",
                                          background: Color.FromArgb(alpha: 255, red: 242, green: 233, blue: 231),
                                          entryNormal: Color.FromArgb(alpha: 255, red: 233, green: 227, blue: 222),
                                          entryNormalDark: Color.FromArgb(alpha: 255, red: 213, green: 207, blue: 202),
                                          entryBatched: Color.FromArgb(alpha: 255, red: 234, green: 190, blue: 178),
                                          text: Color.Black,
                                          buttonText: Color.White,
                                          buttonBackground: Color.FromArgb(alpha: 255, red: 150, green: 124, blue: 105),
                                          checkboxUnchecked: Color.FromArgb(alpha: 255, red: 233, green: 227, blue: 222),
                                          checkboxChecked: Color.FromArgb(alpha: 255, red: 150, green: 124, blue: 105),
                                          checkboxPressed: Color.FromArgb(alpha: 255, red: 90, green: 69, blue: 53),
                                          checkboxCheck: Color.FromArgb(alpha: 255, red: 213, green: 207, blue: 202),
                                          exitButtonBackground: Color.FromArgb(alpha: 255, red: 150, green: 124, blue: 105),
                                          exitButtonHovered: Color.Crimson,
                                          exitButtonPressed: Color.DarkRed,
                                          exitButtonIdle: Color.FromArgb(alpha: 255, red: 120, green: 99, blue: 83),
                                          minimizeButtonBackground: Color.FromArgb(alpha: 255, red: 150, green: 124, blue: 105),
                                          minimizeButtonHovered: Color.LightSlateGray,
                                          minimizeButtonPressed: Color.SlateGray,
                                          minimizeButtonIdle: Color.FromArgb(alpha: 255, red: 120, green: 99, blue: 83),
                                          fontName: "cambria",
                                          fontSizeOverride: 0,
                                          border: Color.FromArgb(alpha: 255, red: 157, green: 151, blue: 148),
                                          graphBackground: Color.FromArgb(alpha: 255, red: 249, green: 240, blue: 238),
                                          graphBars: Color.FromArgb(alpha: 255, red: 214, green: 170, blue: 158)),
                "light" => new(palleteName: name ?? "light",
                                          background: SystemColors.Window,
                                          entryNormal: SystemColors.ControlLightLight,
                                          entryNormalDark: SystemColors.ControlLight,
                                          entryBatched: Color.Lavender,
                                          text: Color.Black,
                                          buttonText: Color.Black,
                                          buttonBackground: SystemColors.ButtonFace,
                                          checkboxChecked: SystemColors.Control,
                                          checkboxUnchecked: SystemColors.ControlLightLight,
                                          checkboxPressed: SystemColors.ControlDark,
                                          checkboxCheck: SystemColors.ControlLight,
                                          exitButtonBackground: Color.FromArgb(alpha: 255, red: 150, green: 124, blue: 105),
                                          exitButtonHovered: Color.Crimson,
                                          exitButtonPressed: Color.DarkRed,
                                          exitButtonIdle: Color.FromArgb(alpha: 255, red: 120, green: 99, blue: 83),
                                          minimizeButtonBackground: Color.FromArgb(alpha: 255, red: 150, green: 124, blue: 105),
                                          minimizeButtonHovered: Color.LightSlateGray,
                                          minimizeButtonPressed: Color.SlateGray,
                                          minimizeButtonIdle: Color.FromArgb(alpha: 255, red: 120, green: 99, blue: 83),
                                          fontName: "segoe ui",
                                          fontSizeOverride: 0,
                                          border: SystemColors.InactiveBorder,
                                          graphBackground: SystemColors.Window,
                                          graphBars: SystemColors.ButtonHighlight),
                "dark" => new(palleteName: name ?? "dark",
                                          background: Color.FromArgb(alpha: 255, red: 43, green: 42, blue: 51),
                                          entryNormal: Color.FromArgb(alpha: 255, red: 66, green: 65, blue: 77),
                                          entryNormalDark: Color.FromArgb(alpha: 255, red: 46, green: 45, blue: 57),
                                          entryBatched: Color.FromArgb(alpha: 255, red: 66, green: 78, blue: 77),
                                          text: Color.LightGray,
                                          buttonText: Color.LightGray,
                                          buttonBackground: Color.FromArgb(alpha: 255, red: 59, green: 58, blue: 68),
                                          checkboxUnchecked: Color.FromArgb(alpha: 255, red: 66, green: 65, blue: 77),
                                          checkboxChecked: Color.FromArgb(alpha: 255, red: 255, green: 255, blue: 255),
                                          checkboxPressed: Color.FromArgb(alpha: 255, red: 46, green: 45, blue: 53),
                                          checkboxCheck: Color.DarkGray,
                                          exitButtonBackground: Color.FromArgb(alpha: 255, red: 150, green: 124, blue: 105),
                                          exitButtonHovered: Color.Crimson,
                                          exitButtonPressed: Color.DarkRed,
                                          exitButtonIdle: Color.FromArgb(alpha: 255, red: 120, green: 99, blue: 83),
                                          minimizeButtonBackground: Color.FromArgb(alpha: 255, red: 150, green: 124, blue: 105),
                                          minimizeButtonHovered: Color.LightSlateGray,
                                          minimizeButtonPressed: Color.SlateGray,
                                          minimizeButtonIdle: Color.FromArgb(alpha: 255, red: 120, green: 99, blue: 83),
                                          fontName: "segoe ui",
                                          fontSizeOverride: 0,
                                          border: Color.FromArgb(alpha: 255, red: 25, green: 25, blue: 30),
                                          graphBackground: Color.FromArgb(alpha: 255, red: 63, green: 62, blue: 71),
                                          graphBars: Color.FromArgb(alpha: 255, red: 33, green: 32, blue: 41)),
                _ => throw new Exception("Default color not provided!"),
            };
        }
    }
}
