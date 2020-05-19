using BS;

namespace HoldingBag
{
    public class ItemModuleFetch : ItemModule
    {
        public int itemCategory = 2;
        public int capacity = 0;
        public bool despawnBagOnEmpty = false;
        public bool onlyPurchaseable = true;
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
