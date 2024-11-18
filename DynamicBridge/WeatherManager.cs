using Lumina.Excel.Sheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicBridge
{
    public unsafe class WeatherManager
    {
        public Dictionary<uint, string> Weathers = [];
        public Dictionary<string, HashSet<uint>> WeatherNames = [];
        internal byte* TrueWeatherPtr;

        public WeatherManager()
        {
            foreach(var x in Svc.Data.GetExcelSheet<TerritoryType>())
            {
                var weathers = Utils.ParseLvb((ushort)x.RowId);
                if(weathers.WeatherList != null)
                {
                    foreach(var w in weathers.WeatherList)
                    {
                        var t = Svc.Data.GetExcelSheet<Weather>().GetRow(w).Name.ExtractText();
                        if(!Weathers.ContainsKey(w))
                        {
                            Weathers[w] = t;
                        }
                        if(!WeatherNames.TryGetValue(t, out var set))
                        {
                            set = [];
                            WeatherNames.Add(t, set);
                        }
                        set.Add(w);
                    }
                }
            }
            TrueWeatherPtr = (byte*)(*(IntPtr*)Svc.SigScanner.GetStaticAddressFromSig("48 8B 05 ?? ?? ?? ?? 48 83 C1 10 48 89 74 24") + 0x26);
        }

        public uint GetWeather() => *TrueWeatherPtr;
    }
}
