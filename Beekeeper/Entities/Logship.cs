using System;

namespace Beekeeper.Entities
{
    public class Logship
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public int ItemCount { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public WarningLevel Warning { get; set; }

        public Logship()
        {
            Warning = WarningLevel.Green;
        }
    }
}
