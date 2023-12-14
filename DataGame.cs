using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KingOfExplosionsServer
{
    public class DataGame
    {
        public int UserNumber { get; set; }
        public string User { get; set; }
        public String PicName { get; set; }
        public string Action { get; set; }
        public String Direction { get; set; }

        public Tuple<int, int> Position { get; set; }
    }
}
