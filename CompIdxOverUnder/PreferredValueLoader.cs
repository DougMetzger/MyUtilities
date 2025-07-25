using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CompIdxOverUnderDriver
{
    public class PreferredValueLoader
    {
        private Dictionary<string, Dictionary<string, double>> symbolValues = new();

        public PreferredValueLoader(string strategyName, string strategyFolder = "")
        {
            string basePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "WealthLab8",
                "Strategies");

            if (!string.IsNullOrWhiteSpace(strategyFolder))
                basePath = Path.Combine(basePath, strategyFolder);

            string path = Path.Combine(basePath, strategyName + ".xml");


            if (!File.Exists(path))
                throw new FileNotFoundException($"Strategy XML not found: {path}");

            XDocument doc = XDocument.Load(path);

            foreach (var item in doc.Descendants("item"))
            {
                string symbol = item.Element("key")?.Element("string")?.Value;
                if (string.IsNullOrEmpty(symbol))
                    continue;

                var paramDict = new Dictionary<string, double>();

                foreach (var prefVal in item.Descendants("PreferredValue"))
                {
                    string name = prefVal.Element("Name")?.Value;
                    string valueStr = prefVal.Element("Value")?.Value;

                    if (!string.IsNullOrEmpty(name) && double.TryParse(valueStr, out double value))
                    {
                        paramDict[name] = value;
                                                
                        WLLogger.Write($"[PreferredValueLoader] {symbol} → {name} = {value}");
                        
                        System.Diagnostics.Debug.WriteLine($"[PreferredValueLoader] {symbol} → {name} = {value}");

                    }
                }

                symbolValues[symbol] = paramDict;
            }
        }

        public double Get(string symbol, string paramName, double fallback = 0.0)
        {
            if (symbolValues.TryGetValue(symbol, out var paramDict) &&
                paramDict.TryGetValue(paramName, out double value))
            {
                return value;
            }

            return fallback;
        }
    }

}
