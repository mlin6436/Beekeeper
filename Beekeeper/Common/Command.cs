using Args;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Beekeeper
{
    public class Command
    {
        [ArgsMemberSwitch("a", "action")]
        public CommandOption Action { get; set; }

        [ArgsMemberSwitch("d", "directory")]
        public string Directory { get; set; }

        [ArgsMemberSwitch("server")]
        public string Server { get; set; }

        [ArgsMemberSwitch("database")]
        public string Database { get; set; }
    }
}
