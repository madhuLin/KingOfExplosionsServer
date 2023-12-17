using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KingOfExplosionsServer
{
    public class Data
    {
        public string Action { get; set; }
        public Tuple<int, int> Position { get; set; }
    }
    public class DataGame : Data
    {
        public int UserNumber { get; set; }
        public int numberBomb { get; set; }  //炸彈編號
        public String PicName { get; set; }
        public int TypeProp { get; set; }
    }

    public class DataBomb : Data
    {
        public DataBomb(string action, Tuple<int, int> position, int type)
        {
            this.Action = action;
            this.Position = position;
            this.type = type;
        }
        public int type { get; set; }
    }

    public class Attack
    {
        public string Action { get; set; }
        public int UserNumber { get; set; }
        public int Heart { get; set; }
        public Attack(string action, int num, int heart) {
            Action = action;
            UserNumber = num;
            Heart = heart;
        }
    }
}
