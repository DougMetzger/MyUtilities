using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompIdxOverUnderDriver
{
    public class ReviewCurrentPosition
    {
        public bool OverBoughtPositionExists { get; private set; }
        public bool MidpointPositionExists { get; private set; }
        public bool OverSoldPositionExists { get; private set; }

        private readonly bool enableDebugLogging;

        public ReviewCurrentPosition(bool enableDebugLogging)
        {
            this.enableDebugLogging = enableDebugLogging;
        }

        public void Analyze(Func<string, bool> positionChecker)
        {
            OverSoldPositionExists = positionChecker("Buy @ OverSold");
            OverBoughtPositionExists = positionChecker("Buy @ OverBought");
            MidpointPositionExists = positionChecker("Buy @ MidPoint");

            if (enableDebugLogging)
            {
                WLLogger.Write("Reviewing current positions: OverSold, OverBought, Midpoint.");
                WLLogger.Write($"Oversold: {OverSoldPositionExists}, Overbought: {OverBoughtPositionExists}, Midpoint: {MidpointPositionExists}");
            }
        }
    }
}
