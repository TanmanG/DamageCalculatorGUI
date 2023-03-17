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
        public Color entryBatched;
        public Color text;
        public Color buttonBackground;
        public Color buttonIdle;
        public Color buttonHovered;
        public Color buttonPressed;
        public Color border;
        public string fontName;
        public int fontSizeOverride;

        public PFKColorPalette(Color background, Color entryNormal,
                                Color entryBatched, Color text,
                                Color buttonBackground, Color buttonIdle, Color buttonHovered, Color buttonPressed,
                                string fontName, int fontSizeOverride,
                                Color border)
        {
            this.background = background;
            this.entryNormal = entryNormal;
            this.entryBatched = entryBatched;
            this.text = text;
            this.buttonBackground = buttonBackground;
            this.buttonIdle = buttonIdle;
            this.buttonHovered = buttonHovered;
            this.buttonPressed = buttonPressed;
            this.fontName = fontName;
            this.fontSizeOverride = fontSizeOverride;
            this.border = border;
        }
    }
}
