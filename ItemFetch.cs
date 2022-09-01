using UnityEngine;
using ThunderRoad;
using System;
using System.Collections.Generic;
//using System.Linq;

namespace HoldingBag
{
    public class ItemFetch : MonoBehaviour
    {
        protected Item item;
        protected ItemModuleFetch module;
        protected Holder holder;
        protected List<string> itemsList;
        protected List<string> parsedItemsList;
        private bool infiniteUses = false;
        private int usesRemaining = 0;
        private bool waitingForSpawn = false;

        protected void Awake()
        {
            item = this.GetComponent<Item>();
            module = item.data.GetModule<ItemModuleFetch>();
            holder = item.GetComponentInChildren<Holder>();
            holder.UnSnapped += new Holder.HolderDelegate(this.OnWeaponItemRemoved);
            
            //Trim list of ItemData IDs by sub-type. Each ItemData has its own type, defined by these enum indicies:
            //Misc = 0
            //Weapon = 1
            //Quiver = 2
            //Potion = 3
            //Prop = 4
            //Body = 5
            //Shield = 6

            //Get all ItemData IDs from the chosen category. If no valid itemCategory is set, then no trimming is done and all types are valid for return
            if (module.itemCategory >= 0 && module.itemCategory <= 6)
            {
                var categoryEnums = Enum.GetValues(typeof(ItemData.Type));
                ItemData.Type chosenCategory = (ItemData.Type)categoryEnums.GetValue(module.itemCategory);
                itemsList = Catalog.GetAllID<ItemData>().FindAll(i => Catalog.GetData<ItemData>(i, true).type.Equals(chosenCategory));
                //Only include ItemData IDs which are purchasable 
                itemsList = itemsList.FindAll(i => Catalog.GetData<ItemData>(i, true).purchasable.Equals(true));
            }
            //Otherwise, populate the list with everything as long as it is purchasable
            else
            {
                itemsList = Catalog.GetAllID<ItemData>().FindAll(i => Catalog.GetData<ItemData>(i, true).purchasable.Equals(true));
            }
            
            // If the plugin is in `overrideMode`, first fetch items from the given category (if supplied) and then add any additionally given items to the parsed list
            if (module.overrideMode)
            {
                parsedItemsList = new List<string>();
                if (!String.IsNullOrEmpty(module.overrideCategory))
                {
                    parsedItemsList = itemsList.FindAll(i => Catalog.GetData<ItemData>(i, true).category.Contains(module.overrideCategory));
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
                    parsedItemsList = parsedItemsList.FindAll(i => !Catalog.GetData<ItemData>(i, true).category.Contains(categoryName));
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

            waitingForSpawn = false;
            Debug.Log("[Fisher-HoldingBags] AWAKE HOLDING BAG: " + Time.time);
            return;
        }

        protected void Start()
        {
            // Spawn initial random item in the holder
            Debug.Log("[Fisher-HoldingBags] STARTING HOLDING BAG: "  + Time.time);
            SpawnAndSnap(GetRandomItemID(parsedItemsList), holder);
        }

        protected string GetRandomItemID(List<string> itemsList)
        {
            return itemsList[UnityEngine.Random.Range(0, itemsList.Count)];
        }

        protected void SpawnAndSnap(string spawnedItemID, Holder holder)
        {
            if (waitingForSpawn) return;
            ItemData spawnedItemData = Catalog.GetData<ItemData>(spawnedItemID, true);
            if (spawnedItemData == null) return;
            else
            {
                waitingForSpawn = true;
                spawnedItemData.SpawnAsync(thisSpawnedItem =>
                {
                    Debug.Log("[Fisher-HoldingBags] Spawning weapon: " + thisSpawnedItem.name +" with ID:" + spawnedItemID);
                    try
                    {
                        if (holder.HasSlotFree())
                        {
                            holder.Snap(thisSpawnedItem);
                            thisSpawnedItem.SetMeshLayer(GameManager.GetLayer(LayerName.FPVHide));
                            //Debug.Log("[Fisher-HoldingBags] Time: " + Time.time + " Snapped weapon: " + thisSpawnedItem.name);
                            //waitingForSpawn = false;
                        }
                        else
                        {
                            Debug.Log("[Fisher-HoldingBags] EXCEPTION Time: " + Time.time + " NO FREE SLOT FOR: " + thisSpawnedItem.name);
                        }
                        waitingForSpawn = false;
                    }
                    catch (Exception e) { Debug.Log("[Fisher-HoldingBags] " + spawnedItemID + "EXCEPTION IN SNAPPING: " + e.ToString()); }
                });
                Debug.Log("[Fisher-HoldingBags] Snapping Weapon " + spawnedItemID);
                return;
            }
        }

        protected void OnWeaponItemRemoved(Item interactiveObject)
        {
            if (waitingForSpawn) return;

            if ((!infiniteUses) && (usesRemaining <= 0))
            {
                holder.data.locked = true;
                if (module.despawnBagOnEmpty) item.Despawn();
                return;
            }
            else
            {
                //Debug.Log("[Fisher-HoldingBags] Time: " + Time.time + " Activating OnWeaponItemRemoved: " + interactiveObject.data.id);
                SpawnAndSnap(GetRandomItemID(parsedItemsList), holder);
                usesRemaining -= 1;
                return;
            }
        }

    }
}
