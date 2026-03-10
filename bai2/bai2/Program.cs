using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bai2
{
    class Item 
    {
        public int id 
        {
            get; set; } 
        public string name 
        { 
            get; set; } 
        public string type 
        { 
            get; set; } 
        public double price 
        {
            get; set; } 
    }
    internal class Program
    {
        static List<Item> LocItemTheoGia(List<Item> inventory, double price) 
        { 
            return inventory.Where(item => item.price > price).ToList(); 
        }
        static List<Item> GetItemsByType(List<Item> inventory, string type)
        {
            return inventory.Where(item => item.type == type).ToList();
        }
        static List<Item> SapXepTheoGia(List<Item> inventory, string type) 
        { 
            if (type == "des") 
            { 
                return inventory.OrderByDescending(item => item.price).ToList(); 
            } 
            else 
            { 
                return inventory.OrderBy(item => item.price).ToList(); 
            } 
        }
        static List<Item> GetMostExpensiveItem(List<Item> inventory)
        {
            double maxPrice = inventory.Max(item => item.price); 
            return inventory.Where(item => item.price == maxPrice).ToList();
        }
        static double GetAveragePrice(List<Item> inventory) 
        { 
            return inventory.Average(item => item.price); 
        }
        static Dictionary<string, List<Item>> GroupItemTheoType(List<Item> inventory)
        {
            return inventory
                .GroupBy(item => item.type)
                .ToDictionary(group => group.Key, group => group.ToList());
        }
        static void Main(string[] args)
        {
            List<Item> inventory = new List<Item>();
            inventory.Add(new Item {id = 1, name = "Sword", type = "Weapon", price = 90});
            inventory.Add(new Item { id = 2, name = "Axe", type = "Weapon", price = 120 });
            inventory.Add(new Item { id = 3, name = "Shield", type = "Weapon", price = 70 });
            inventory.Add(new Item { id = 4, name = "Fire Armor", type = "Armor", price = 200 });
            inventory.Add(new Item { id = 5, name = "Helmet", type = "Armor", price = 200 });
            inventory.Add(new Item { id = 6, name = "Bow", type = "Weapon", price = 180 });
            inventory.Add(new Item { id = 7, name = "Mana Potion", type = "Potions", price = 30 });
            inventory.Add(new Item { id = 8, name = "Ice Armor", type = "Armor", price = 200 });
            inventory.Add(new Item { id = 9, name = "Health Potion", type = "Potions", price = 30 });
            Console.WriteLine("\nDanh sach item trong inventory:");
            foreach (var item in inventory)
            {
                Console.WriteLine($"{item.id} - {item.name} - {item.type} - {item.price}");
            }
            Console.WriteLine("\nDanh sach weapon trong inventory:");
            List<Item> inventory1 = GetItemsByType(inventory, "Weapon");
            foreach (var item in inventory1)
            {
                Console.WriteLine($"{item.id} - {item.name} - {item.type} - {item.price}");
            }
            inventory1 = LocItemTheoGia(inventory, 100);
            Console.WriteLine("\nDanh sach item co gia > 100 trong inventory:");
            foreach (var item in inventory1)
            {
                Console.WriteLine($"{item.id} - {item.name} - {item.type} - {item.price}");
            }
            inventory1 = SapXepTheoGia(inventory, "asc");
            Console.WriteLine("\nDanh sach item sap xep theo gia tang dan trong inventory:");
            foreach (var item in inventory1)
            {
                Console.WriteLine($"{item.id} - {item.name} - {item.type} - {item.price}");
            }
            Console.WriteLine("\nDanh sach group item theo type.:");
            var groups = GroupItemTheoType(inventory);

            foreach (var group in groups)
            {
                Console.WriteLine(group.Key);
                foreach (var item in group.Value)
                {
                    Console.WriteLine($"{item.id} - {item.name} - {item.price}");
                }
            }
        }
    }
}
