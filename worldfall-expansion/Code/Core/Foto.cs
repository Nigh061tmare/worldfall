using System.Collections.Generic;

namespace WorldfallExpansion.Core
{
    // Foto del mundo en un instante: SOLO datos planos (strings, bools), sin tipos del juego.
    // El adaptador (Code/Busquedas.cs) la rellena leyendo World.world; el Core compara dos fotos.
    // Claves: unidades "a<data.id>"; reinos y ciudades con una clave estable por objeto que asigna el adaptador.
    public sealed class FotoUnidad
    {
        public string Id = "", Nombre = "";
        public bool Adulto, EsRey;
        public string Reino;          // clave del reino (null si no tiene o es salvaje)
        public string ReinoNombre = "";
        public string Ciudad;         // clave de la ciudad (null si no tiene)
        public string Amante;         // id de la pareja (null si no tiene)
        // Hijos vivos (ids). Solo se rellena para reyes y lideres de ciudad: getChildren no es gratis.
        public List<string> Hijos = new List<string>();
    }

    public sealed class FotoReino
    {
        public string Clave = "", Nombre = "";
        public string Rey;            // id del rey vivo (null si el trono esta vacio)
    }

    public sealed class FotoCiudad
    {
        public string Clave = "";
        public string Lider;          // id del lider vivo (null si no tiene)
        public string Reino;          // clave del reino del lider, si se conoce
    }

    public sealed class FotoMundo
    {
        public double Tiempo;         // World.world.getCurWorldTime()
        public readonly Dictionary<string, FotoUnidad> Unidades = new Dictionary<string, FotoUnidad>();
        public readonly Dictionary<string, FotoReino> Reinos = new Dictionary<string, FotoReino>();
        public readonly Dictionary<string, FotoCiudad> Ciudades = new Dictionary<string, FotoCiudad>();

        public FotoUnidad Unidad(string id)
        {
            FotoUnidad u;
            return id != null && Unidades.TryGetValue(id, out u) ? u : null;
        }

        public FotoReino Reino(string clave)
        {
            FotoReino r;
            return clave != null && Reinos.TryGetValue(clave, out r) ? r : null;
        }

        public FotoCiudad Ciudad(string clave)
        {
            FotoCiudad c;
            return clave != null && Ciudades.TryGetValue(clave, out c) ? c : null;
        }

        public string NombreReino(string clave)
        {
            FotoReino r = Reino(clave);
            return r != null && r.Nombre.Length > 0 ? r.Nombre : "tierras sin reino";
        }
    }
}
