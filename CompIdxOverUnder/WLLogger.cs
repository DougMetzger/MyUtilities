using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompIdxOverUnderDriver
{
    public static class WLLogger
    {
        public static Action<string> LogAction { get; set; }

        public static void Write(string msg)
        {
            LogAction?.Invoke(msg);
        }
    }


}
