using Lumina.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace DynamicBridge
{
    public unsafe class LvbFile : FileResource
    {
        public ushort[] weatherIds;
        public string envbFile;

        public override void LoadFile()
        {
            weatherIds = new ushort[32];

            var pos = 0xC;
            if(Data[pos] != 'S' || Data[pos + 1] != 'C' || Data[pos + 2] != 'N' || Data[pos + 3] != '1')
                pos += 0x14;
            var sceneChunkStart = pos;
            pos += 0x10;
            var settingsStart = sceneChunkStart + 8 + BitConverter.ToInt32(Data, pos);
            pos = settingsStart + 0x40;
            var weatherTableStart = settingsStart + BitConverter.ToInt32(Data, pos);
            pos = weatherTableStart;
            for(var i = 0; i < 32; i++)
                weatherIds[i] = BitConverter.ToUInt16(Data, pos + i * 2);

            if(Data.TryFindBytes("2E 65 6E 76 62 00", out pos))
            {
                var end = pos + 5;
                while(Data[pos - 1] != 0 && pos > 0)
                {
                    pos--;
                }
                envbFile = Encoding.UTF8.GetString(Data.Skip(pos).Take(end - pos).ToArray());
            }
        }
    }

}
