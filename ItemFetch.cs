using UnityEngine;
using ThunderRoad;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HoldingBag
{
    public class ItemFetch : MonoBehaviour
    {
        protected Item item;
        protected ItemModuleFetch module;
        protected ObjectHolder holder;
        protected List<string> itemsList;
        protected List<string> parsedItemsList;
        private bool infiniteUses = false;
        private int usesRemaining = 0;
        protected void Awake()
        {
            item = this.GetComponent<Item>();
            module = item.data.GetModule<ItemModuleFetch>();
            holder = item.GetComponentInChildren<ObjectHolder>();
            holder.UnSnapped += new ObjectHolder.HolderDelegate(this.OnWeaponItemRemoved);

            //Trim list of ItemPhysic IDs by sub-type. Each ItemPhysic has its own type, defined by these enum indicies:
            //Misc = 0
            //Weapon = 1
            //Quiver = 2
            //Potion = 3
            //Prop = 4
            //Body = 5
            //Shield = 6

            //Get all ItemPhysic IDs from the chosen category. If no valid itemCategory is set, then no trimming is done and all types are valid for return
            if (module.itemCategory >= 0 || module.itemCategory > 6)
            {
                var categoryEnums = Enum.GetValues(typeof(ItemPhysic.Type));
                ItemPhysic.Type chosenCategory = (ItemPhysic.Type)categoryEnums.GetValue(module.itemCategory);
                itemsList = Catalog.GetAllID<ItemPhysic>().FindAll(i => Catalog.GetData<ItemPhysic>(i, true).type.Equals(chosenCategory));
                //Only include ItemPhysic IDs which are purchasable 
                itemsList = itemsList.FindAll(i => Catalog.GetData<ItemPhysic>(i, true).purchasable.Equals(true));
            }
            //Otherwise, populate the list with everything as long as it is purchasable
            else
            {
                itemsList = Catalog.GetAllID<ItemPhysic>().FindAll(i => Catalog.GetData<ItemPhysic>(i, true).purchasable.Equals(true));
            }
            
            // If the plugin is in `overrideMode`, first fetch items from the given category (if supplied) and then add any additionally given items to the parsed list
            if (module.overrideMode)
            {
                parsedItemsList = new List<string>();
                if (!String.IsNullOrEmpty(module.overrideCategory))
                {
                    parsedItemsList = itemsList.FindAll(i => Catalog.GetData<ItemPhysic>(i, true).categoryPath.Any(j => j.Contains(module.overrideCategory)));
                }

                foreach (string itemName in module.overrideItems)
                {
                    if (!parsedItemsList.Contains(itemName) && itemsList.Contains(itemName))
                    {
                        parsedItemsList.Add(itemName);
                    }
                }
            }
            // Otherwise if not in override mode, then load all items from all categories (from the given BS master list), optionally exluding specific categories and items
            else
            {
                parsedItemsList = new List<string>(itemsList);
                foreach (string categoryName in module.excludedCategories)
                {
                    parsedItemsList = parsedItemsList.FindAll(i => !Catalog.GetData<ItemPhysic>(i, true).categoryPath.Any(j => j.Contains(categoryName)));
                }

                foreach (string itemName in module.excludedItems)
                {
                    parsedItemsList = parsedItemsList.FindAll(i => !i.Contains(itemName));
                }
            }

            // If no capacity is defined, default to infinite usages. Otherwise, set up a tracker for remaining uses
            if (module.capacity <= 0 )
            {
                infiniteUses = true;
            }
            else
            {
                usesRemaining = module.capacity - 1;
            }
            
            // Spawn initial random item in the holder
            SpawnAndSnap(GetRandomItemID(parsedItemsList), holder);
            return;
        }

        protected string GetRandomItemID(List<string> itemsList)
        {
            return itemsList[UnityEngine.Random.Range(0, itemsList.Count)];
        }

        protected void SpawnAndSnap(string spawnedItemID, ObjectHolder holder)
        {
            ItemPhysic spawnedItemData = Catalog.GetData<ItemPhysic>(spawnedItemID, true);
            //Debug.Log("[HoldingBag][Fetch][Spawn] Item Categories: " + spawnedItemData.categoryPath + ", Item Name: " + spawnedItemData.displayName);
            if (spawnedItemData == null) return;
            else
            {
                Item thisSpawnedItem = spawnedItemData.Spawn(true);
                if (!thisSpawnedItem.gameObject.activeInHierarchy) thisSpawnedItem.gameObject.SetActive(true);
                holder.Snap(thisSpawnedItem);
                return;
            }
        }

        protected void OnWeaponItemRemoved(Item interactiveObject)
        {
            if ((!infiniteUses) && (usesRemaining <= 0))
            {
                holder.data.locked = true;
                if (module.despawnBagOnEmpty) item.Despawn();
                return;
            }
            else
            {
                SpawnAndSnap(GetRandomItemID(parsedItemsList), holder);
                usesRemaining -= 1;
                return;
            }

        }

    }
}

