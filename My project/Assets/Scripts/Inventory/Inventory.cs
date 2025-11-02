using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    public List<Item> inventoryItems = new List<Item>();
    
    // Метод для добавления предмета в инвентарь
    public void AddItem(Item item)
    {
        inventoryItems.Add(item);
        Debug.Log($"Предмет {item.itemName} добавлен в инвентарь.");
    }
}
