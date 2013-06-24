using Args;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Beekeeper
{
    public class CommandObject
    {
        [ArgsMemberSwitch("a", "action")]
        public CommandOption Action { get; set; }

        [ArgsMemberSwitch("d", "directory")]
        public string Directory { get; set; }
    }
}
