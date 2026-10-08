// STUBS de compilacion: NO es el juego. Declaran SOLO las firmas verificadas de la build 719
// (docs/ARQUITECTURA.md §4). Si el codigo del mod usa algo que no esta aqui, la comprobacion falla:
// asi se cumple la regla «si no esta verificado, no se usa». Para anadir una firma, verificala antes
// en un mod de la build 719 y anotala en docs/ARQUITECTURA.md.
using System.Collections.Generic;

public class BaseSimObject { }

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
    public IEnumerable<Actor> getChildren(bool pOnlyAlive) { return new List<Actor>(); }
    public void setCity(City pCity) { }
    public void addRenown(int pAmount) { }
}

public class City
{
    public Actor leader;
    public void removeLeader() { }
}

public class KingdomData { public string name; }

public class Kingdom
{
    public KingdomData data;
    public bool wild { get { return false; } }
    public Actor king;
}

public class ActorManager { public List<Actor> getSimpleList() { return new List<Actor>(); } }

public class MapStats { public string name; public long id; }

public class World
{
    public static World world;
    public ActorManager units;
    public MapStats map_stats;
    public double getCurWorldTime() { return 0; }
}

public static class WorldTip
{
    public static void showNow(string pText, bool pTranslate, string pPosition, float pTime) { }
}

public static class DiplomacyManager
{
    public static void startWar(Kingdom pAttacker, Kingdom pDefender) { }
}
