using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pickings_For_Kurtulmak
{
    public class PFKColorPalette
    {
        public Color background;
        public Color entryNormal;
        public Color entryNormalDark;
        public Color entryBatched;
        public Color text;

        public Color buttonText;
        public Color buttonBackground;
        public Color buttonIdle;
        public Color buttonHovered;
        public Color buttonPressed;

        public Color exitButtonBackground;
        public Color exitButtonIdle;
        public Color exitButtonHovered;
        public Color exitButtonPressed;

        public Color minimizeButtonBackground;
        public Color minimizeButtonIdle;
        public Color minimizeButtonHovered;
        public Color minimizeButtonPressed;

        public Color border;
        public Color graphBars;
        public Color graphBackground;
        public string fontName;
        public int fontSizeOverride;

        public PFKColorPalette(Color background, Color entryNormal, Color entryNormalDark,
                                Color entryBatched, Color text,
                                Color buttonText, Color buttonBackground, Color buttonIdle, Color buttonHovered, Color buttonPressed,
                                string fontName, int fontSizeOverride,
                                Color border, Color graphBars, Color graphBackground,
                                Color exitButtonBackground, Color exitButtonIdle, Color exitButtonHovered, Color exitButtonPressed,
                                Color minimizeButtonBackground, Color minimizeButtonIdle, Color minimizeButtonHovered, Color minimizeButtonPressed)
        {
            this.background = background;
            this.entryNormal = entryNormal;
            this.entryNormalDark = entryNormalDark;
            this.entryBatched = entryBatched;
            this.text = text;

            this.buttonText = buttonText;
            this.buttonBackground = buttonBackground;
            this.buttonIdle = buttonIdle;
            this.buttonHovered = buttonHovered;
            this.buttonPressed = buttonPressed;

            this.exitButtonBackground = exitButtonBackground;
            this.exitButtonIdle = exitButtonIdle;
            this.exitButtonHovered = exitButtonHovered;
            this.exitButtonPressed = exitButtonPressed;

            this.minimizeButtonBackground = minimizeButtonBackground;
            this.minimizeButtonIdle = minimizeButtonIdle;
            this.minimizeButtonHovered = minimizeButtonHovered;
            this.minimizeButtonPressed = minimizeButtonPressed;

            this.fontName = fontName;
            this.fontSizeOverride = fontSizeOverride;
            this.border = border;
            this.graphBars = graphBars;
            this.graphBackground = graphBackground;
        }
    }
}
