using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoProCSharpDev.Utils
{
    class BluetoothUtils
    {
        public static string GetUuid128(string CharacteristicUuid)
        {
            return Consts.GoProUuidBase128.Replace("xxxx", CharacteristicUuid);
        }
    }
}
