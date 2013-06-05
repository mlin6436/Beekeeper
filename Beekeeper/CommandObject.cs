using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Beekeeper
{
    public class CommandObject
    {
        public CommandOption Option { get; set; }
    }

    public enum CommandOption
    {
        CheckStatus = 1
    }
}
