using UnityEngine;

[System.Serializable]
public class Item : MonoBehaviour
{
    public string itemName;
    public Sprite itemIcon;
    public InteractionType interactionType = InteractionType.Pickup;
    public float holdDuration = 2f; // Время удержания для дверей и перетаскиваемых предметов

    // Создаёт копию предмета (только данные)
    public ItemData GetItemData()
    {
        return new ItemData
        {
            itemName = this.itemName,
            itemIcon = this.itemIcon,
            interactionType = this.interactionType
        };
    }
}

// Класс только для данных (не привязан к GameObject)
[System.Serializable]
public class ItemData
{
    public string itemName;
    public Sprite itemIcon;
    public InteractionType interactionType;
}

