using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeFlow.UWP
{
    class PlatformHelper: IPlatformHelper
    {
        public PlatformHelper() { }

        public string Version => App.Version;
    }
}
