using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompIdxOverUnderDriver
{
    public class ReviewCurrentPosition
    {
       

        public bool OverBoughtPositionExists { get; set; }
        public bool MidpointPositionExists { get; set; }
        public bool OverSoldPositionExists { get; set; }

        #pragma warning disable IDE0290
        private readonly bool enableDebugLogging;

        public ReviewCurrentPosition(bool enableDebugLogging)
        {
            this.enableDebugLogging = enableDebugLogging;
        }

        public void Analyze()
        {
            if (enableDebugLogging)
            {
                WLLogger.Write("Review Current Positions to determe if Oversold, Overbought, Midpoint positions exist.  Only allow one of each at a time.");
            }

            // Your logic here...
        }
    }
}
