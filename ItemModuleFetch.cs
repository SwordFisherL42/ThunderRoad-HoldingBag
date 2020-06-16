using ThunderRoad;

namespace HoldingBag
{
    public class ItemModuleFetch : ItemModule
    {
        public int itemCategory = -1;
        public int capacity = 0;
        public bool despawnBagOnEmpty = false;
        public string[] excludedCategories = { };
        public string[] excludedItems = { };
        public bool overrideMode = false;
        public string overrideCategory;
        public string[] overrideItems = { };

        public override void OnItemLoaded(Item item)
        {
            base.OnItemLoaded(item);
            item.gameObject.AddComponent<ItemFetch>();
        }
    }
}
