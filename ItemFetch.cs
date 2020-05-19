using UnityEngine;
using BS;
using System;
using System.Collections.Generic;

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

            // Get BS "master list" first, which will be further parsed into the final items list.
            //Misc = 0,
            //Apparel = 1,
            //Weapon = 2,
            //Quiver = 3,
            //Potion = 4,
            //Prop = 5,
            //Body = 6,
            //Shield = 7
            var categoryEnums = Enum.GetValues(typeof(ItemData.Category));
            ItemData.Category chosenCategory = (ItemData.Category)categoryEnums.GetValue(module.itemCategory);
            
            // Trim itemsList to just purchaseable items, unless explicitly overridden in module
            if (module.onlyPurchaseable)
            {
                itemsList = Catalog.current.GetAllItemID(chosenCategory).FindAll(i => Catalog.current.GetData<ItemData>(i, true).purchasable.Equals(true));
            }
            else
            {
                itemsList = Catalog.current.GetAllItemID(chosenCategory);
            }

            // If in `overrideMode`, first fetch items from the given category (if supplied) and then add any additionally given items to the parsed list
            if (module.overrideMode)
            {
                parsedItemsList = new List<string>();
                if (!String.IsNullOrEmpty(module.overrideCategory))
                {
                    parsedItemsList = itemsList.FindAll(i => Catalog.current.GetData<ItemData>(i, true).storageCategory.Contains(module.overrideCategory));
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
                    parsedItemsList = parsedItemsList.FindAll(i => !Catalog.current.GetData<ItemData>(i, true).storageCategory.Contains(categoryName));
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
            ItemData spawnedItemData = Catalog.current.GetData<ItemData>(spawnedItemID, true);
            Debug.Log("[HoldingBag][Fetch][Spawn] Item Category: " + spawnedItemData.storageCategory + ", Item Name: " + spawnedItemData.displayName);
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

