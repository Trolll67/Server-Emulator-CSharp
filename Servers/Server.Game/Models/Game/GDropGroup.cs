using Database.DataModel.Enums;
using Database.DataModel.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.Models.Game
{
    public class GDropGroup : DropGroup
    {
        public GDropGroup()
            : base()
        {

        }
        public DropGroupTypeEnum DropType { get; set; }
        // TODO: the drop amplification windows of the group are not modeled yet
    }
}
