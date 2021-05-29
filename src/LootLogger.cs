using System;
using System.Collections.Generic;
using System.Text;

namespace SUNLootChecker
{
    public class Player
    {
        public string Name { get; set; }
        public List<Loot> Loots { get; set; }
    }

    public class Loot
    {
        public DateTime PickupTime { get; set; }
        public string ItemName { get; set; }
        public int Quantity { get; set; }
    }
}
