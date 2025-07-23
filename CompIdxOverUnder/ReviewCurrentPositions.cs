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
                WLLogger.Write("ReviewCurrentPosition: Starting analysis.");
            }

            // Your logic here...
        }
    }
}
