using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BubbleGauntlet.Bosses {
    public interface IMinorBoss {
        void Install();
        string Name { get; }

        public void Begin();
        public void Reset();

        public AreaMap Map { get; }
    }
}
