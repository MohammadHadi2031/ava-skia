using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nikrotek.Electrophysiology.Core.DataGenerators
{
    interface IDataGenerator
    {
        public double[] GetNextValues(int count);
    }
}
