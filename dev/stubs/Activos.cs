// STUBS del sistema de activos del juego (catalogos). Firmas LEIDAS de Assembly-CSharp build 719
// (ItemLibrary, ItemAsset, BaseTrait, ActorTrait, AssetLibrary, BaseStats, LocalizedTextManager).
using System.Collections.Generic;

public enum Rarity { R0_Normal, R1_Rare, R2_Epic, R3_Legendary }
public enum TraitType { Positive, Negative, Other }

public class BaseStats
{
    public float this[string pKey] { get { return 0f; } set { } }
}

public class Asset { public string id = "ASSET_ID"; }

public class BaseUnlockableAsset : Asset
{
    public string path_icon;
    public BaseStats base_stats;
    public bool needs_to_be_explored = true;
    public bool has_locales = true;
}

public class BaseAugmentationAsset : BaseUnlockableAsset
{
    public bool can_be_given = true;
    public string group_id;
}

public class ItemAsset : BaseAugmentationAsset
{
    public UnityEngine.Sprite[] gameplay_sprites;
    public int cost_coins_resources;
    public int equipment_value;
    public string translation_key;
    public ItemModAsset[] item_modifiers;
    public void setCost(int pGoldCost, string pResourceID_1 = "none", int pCostResource_1 = 0, string pResourceID_2 = "none", int pCostResource_2 = 0) { }
}

public class ItemModAsset : ItemAsset { }
public class EquipmentAsset : ItemAsset { }

public class BaseTrait<TTrait> : BaseAugmentationAsset where TTrait : BaseTrait<TTrait>
{
    public bool has_localized_id = true;
    public bool has_description_1 = true;
    public bool has_description_2 = true;
    public Rarity rarity = Rarity.R1_Rare;
}

public class ActorTrait : BaseTrait<ActorTrait> { public TraitType type = TraitType.Other; }

public class ResourceAsset : Asset { public int money_cost = 2; }

public class AssetLibrary<T> where T : Asset
{
    public List<T> list = new List<T>();
    public virtual T get(string pID) { return null; }
    public virtual bool has(string pID) { return false; }
    public virtual T add(T pAsset) { return pAsset; }
    public virtual T clone(string pNew, string pFrom) { return null; }
}

public class ItemLibrary : AssetLibrary<EquipmentAsset> { }
public class ActorTraitLibrary : AssetLibrary<ActorTrait> { }
public class ResourceLibrary : AssetLibrary<ResourceAsset> { }

public static class AssetManager
{
    public static ItemLibrary items;
    public static ActorTraitLibrary traits;
    public static ResourceLibrary resources;
    public static PowerLibrary powers;
}

public class GameLanguageAsset : Asset { }

public class LocalizedTextManager
{
    public static GameLanguageAsset current_language;
    public static UnityEngine.Font current_font { get { return null; } }
    public static bool stringExists(string pKey) { return false; }
    public static string getText(string pKey, object text = null, bool pForceEnglish = false) { return pKey; }
    public static void add(string pKey, string pTranslation, bool pReplace = false, string pFileName = "", bool pCheckForCharacters = true) { }
}
