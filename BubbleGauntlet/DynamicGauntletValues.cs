using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BubbleGauntlet {
    public delegate int DynamicGauntletValue(int floor);
    public static class DynamicGauntletValues {
        public static int Floor(this DynamicGauntletValue value) => value(GauntletController.Floor.Level);
    }
}
