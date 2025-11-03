using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    public List<ItemData> inventoryItems = new List<ItemData>();

    // Метод для добавления предмета в инвентарь
    public void AddItem(Item item)
    {
        // Сохраняем данные предмета, а не сам GameObject
        inventoryItems.Add(item.GetItemData());
        Debug.Log($"Предмет {item.itemName} добавлен в инвентарь.");
    }

    /// <summary>
    /// Проверяет, есть ли предмет с указанным именем в инвентаре
    /// </summary>
    public bool HasItem(string itemName)
    {
        return inventoryItems.Exists(item => item.itemName == itemName);
    }

    /// <summary>
    /// Проверяет, есть ли кольт в инвентаре
    /// </summary>
    public bool HasColt()
    {
        return HasItem("кольт") || HasItem("Кольт") || HasItem("colt") || HasItem("Colt");
    }
}