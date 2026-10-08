// STUBS de compilacion: NO es el juego. Declaran SOLO las firmas verificadas de la build 719
// (docs/ARQUITECTURA.md §4). Si el codigo del mod usa algo que no esta aqui, la comprobacion falla:
// asi se cumple la regla «si no esta verificado, no se usa». Para anadir una firma, verificala antes
// en un mod de la build 719 y anotala en docs/ARQUITECTURA.md.
using System.Collections.Generic;

// Verificado en Assembly-CSharp (build 719): BaseSimObject.current_position (Vector2), current_tile.
public class WorldTile { public readonly int x; public readonly int y; }

public class NanoObject { public void setName(string pName, bool pTrack = true) { } }

public class BaseSimObject : NanoObject
{
    public WorldTile current_tile;
    public UnityEngine.Vector2 current_position;
}

public class ActorData { public long id; }

public class Actor : BaseSimObject
{
    public ActorData data;
    public Actor lover;
    public City city;
    public Kingdom kingdom;
    public bool isAlive() { return true; }
    public bool isSapient() { return true; }
    public bool isAdult() { return true; }
    public bool isKing() { return false; }
    public string getName() { return ""; }
    public bool hasLover() { return false; }
    public bool hasFamily() { return false; }
    public IEnumerable<Actor> getChildren(bool pOnlyCurrentFamily = true) { return new List<Actor>(); }
    public void setCity(City pCity) { }
    public void addRenown(int pValue) { }
    public bool addTrait(string pTraitID, bool pRemoveOpposites = false) { return false; }
    public bool hasTrait(string pTraitID) { return false; }
    public bool isCityLeader() { return false; }
    public void joinCity(City pCity) { }
}

public class City
{
    public Actor leader;
    public void removeLeader() { }
    // internal en el juego (se usa por reflexion): Kingdom makeOwnKingdom(Actor, bool pRebellion, bool pFellApart)
}

public class KingdomData { public string name; }

public class Kingdom
{
    public KingdomData data;
    public bool wild;
    public Actor king;
    public City capital;
    public bool isInWarWith(Kingdom pKingdom) { return false; }
    public int countCities() { return 0; }
    public bool hasAlliance() { return false; }
    public Alliance getAlliance() { return null; }
}
public class Alliance { }

public class ActorManager
{
    public List<Actor> getSimpleList() { return new List<Actor>(); }
    public Actor spawnNewUnit(string pActorAssetID, WorldTile pTile, bool pSpawnSound = false, bool pMiracleSpawn = false, float pSpawnHeight = 6f, object pSubspecies = null, bool pGiveOwnerlessItems = false, bool pAdultAge = false) { return null; }
}

// Verificado en Assembly-CSharp (build 719): World es estatica y World.world es un MapBox.
// MapBox.map_stats es INTERNAL: solo se lee por reflexion (Traverse), por eso no esta aqui.
public static class World { public static MapBox world; }

public class MapBox
{
    public ActorManager units;
    public DiplomacyManager diplomacy;
    public double getCurWorldTime() { return 0; }
}

public static class WorldTip
{
    public static void showNow(string pText, bool pTranslate = true, string pPosition = "center", float pTime = 3f, string pColor = "#F3961F") { }
}

// DiplomacyManager.startWar es INTERNAL en la build 719 (4 parametros): solo por Harmony/reflexion.
public class DiplomacyManager { }
public class WarTypeAsset : Asset { }
public static class WarTypeLibrary { public static WarTypeAsset normal; public static WarTypeAsset rebellion; }

// Ventana de unidad (PeceraWB/UnidadUi.cs). Verificado: UnitWindow.name_input (NameInput), SelectedUnit.unit.
public class NameInput : UnityEngine.MonoBehaviour { }
public class UnitWindow : UnityEngine.MonoBehaviour { public NameInput name_input; }
public static class SelectedUnit { public static Actor unit { get { return null; } } }

// Poderes de dios (PowerLibrary, GodPower, PowerActionWithID) y UI de la barra de poderes. Build 719.
public enum PowerRank { Rank0_free, Rank1_common, Rank2_normal, Rank3_good, Rank4_awesome }
public delegate bool PowerActionWithID(WorldTile pTile, string pPowerID);
public class GodPower : Asset
{
    public string name = "DEFAULT NAME";
    public string path_icon;
    public PowerRank rank;
    public bool unselect_when_window;
    public PowerActionWithID click_action;
}
public class PowerLibrary : AssetLibrary<GodPower> { }
public class PowersTab : UnityEngine.MonoBehaviour { }
public class PowerButton : UnityEngine.MonoBehaviour { }
public class CanvasMain : UnityEngine.MonoBehaviour { public static CanvasMain instance; }
public static class SpriteTextureLoader { public static UnityEngine.Sprite getSprite(string pPath) { return null; } }
