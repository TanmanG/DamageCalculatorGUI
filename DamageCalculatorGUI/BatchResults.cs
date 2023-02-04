using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static DamageCalculatorGUI.CalculatorWindow;
using static DamageCalculatorGUI.DamageCalculator;

namespace Pickings_For_Kurtulmak
{
    public class BatchResults
    {
        /// <summary>
        /// N-Dimensional array of the batch size
        /// </summary>
        public Array raw_data = null;

        /// <summary>
        /// Processed 2D array for display
        /// </summary>
        public double[,] processed_data;

        /// <summary>
        /// Maximum width of the batch
        /// </summary>
        public int max_width = 0;

        /// <summary>
        /// Dimensions of each layer.
        /// </summary>
        public List<int> dimensions = new();

        /// <summary>
        /// A list containing the values and respective variable at each tick.
        /// </summary>
        public List<Dictionary<EncounterSetting, int>> tick_values = new();

        /// <summary>
        /// Highest encounter in all of the simulated batch layers
        /// </summary>
        public int highest_encounter_count = 0;
    }
}
