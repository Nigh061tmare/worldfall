using System;
using System.IO;
using System.Text;
using NeoModLoader.General;
using NeoModLoader.General.UI.Tab;
using UnityEngine;
using WorldfallExpansion.Core;

namespace WorldfallExpansion
{
    // Poderes de dios nuevos en una pestana propia (Core/Bestias.cs: Poderes.Lista). Se usan en modo dios;
    // lo que provocan (bestias, rasgos, destierros) lo ves despues en 3D con Worldfall.
    //
    //   wfx_bestia          suelta una bestia legendaria (especie con modelo 3D en Worldfall) + busqueda de caza
    //   wfx_bendicion       rasgo «Bendecido por el Obelisco» + renombre         \
    //   wfx_maldicion       rasgo «Maldito»                                       |  y se avisa a PeceraWB por
    //   wfx_juramento       rasgo «Juramentado»                                   |  intervenciones.jsonl (su
    //   wfx_destierro       sin ciudad + rasgo «Desterrado» (nunca a un rey)      |  cronica y sus afectos)
    //   wfx_discordia       en la pecera: agravio con su mejor amigo              |
    //   wfx_reconciliacion  en la pecera: paz con su rival                       /
    //
    // Firmas del binario build 719: GodPower (id, name, path_icon, rank, unselect_when_window,
    // click_action = PowerActionWithID), AssetManager.powers.add, World.world.units.spawnNewUnit,
    // NanoObject.setName, Actor.addTrait/addRenown/setCity, City.removeLeader. Botones y pestana:
    // NeoModLoader.General.PowerButtonCreator y NeoModLoader.General.UI.Tab.TabManager (NML).
    internal static class PoderesDios
    {
        static bool registrados, pestana;
        static float tIntento = -100f;
        static int intentos;

        public static void Tick()
        {
            if (!Estado.Cfg.Bool("poderes") || (registrados && pestana) || intentos > 200) return;
            float real = Time.unscaledTime;
            if (real - tIntento < 2f) return;
            tIntento = real;
            intentos++;
            if (!registrados && AssetManager.powers != null && AssetManager.powers.list.Count > 0) Registra();
            if (registrados && !pestana && CanvasMain.instance != null) Pestana();
        }

        static void Registra()
        {
            registrados = true;
            int n = 0;
            foreach (PoderDef p in Poderes.Lista)
            {
                try
                {
                    if (AssetManager.powers.has(p.Id)) { n++; continue; }
                    var g = new GodPower
                    {
                        id = p.Id,
                        name = p.Id,                     // clave de idioma = name (ver Locales/*.json)
                        path_icon = p.Icono,
                        rank = PowerRank.Rank0_free,
                        unselect_when_window = true,
                    };
                    AssetManager.powers.add(g);
                    string id = p.Id;
                    g.click_action = (tile, power) => Usa(id, tile);
                    n++;
                }
                catch (Exception e) { Estado.Fallo("poder " + p.Id, e); }
            }
            Contenido.Textos();
            Debug.Log("[WorldfallExp] poderes: " + n + "/" + Poderes.Lista.Length + " registrados");
        }

        static void Pestana()
        {
            pestana = true;
            try
            {
                Sprite icono = SpriteTextureLoader.getSprite("ui/Icons/actor_traits/iconBlessing");
                PowersTab tab = TabManager.CreateTab(Poderes.Pestana, Poderes.Pestana, Poderes.Pestana + "_description", icono);
                foreach (PoderDef p in Poderes.Lista)
                {
                    if (!AssetManager.powers.has(p.Id)) continue;
                    PowerButton b = PowerButtonCreator.CreateGodPowerButton(p.Id, SpriteTextureLoader.getSprite(p.Icono));
                    PowerButtonCreator.AddButtonToTab(b, tab);
                }
                Debug.Log("[WorldfallExp] poderes: pestana «Worldfall Expansion» creada");
            }
            catch (Exception e) { Estado.Fallo("pestana_poderes", e); }
        }

        // Efecto de cada poder al pulsar sobre el mapa. Nunca lanza excepciones al juego.
        static bool Usa(string id, WorldTile tile)
        {
            try
            {
                if (tile == null) return false;
                if (id == "wfx_bestia") return SueltaBestia(tile);
                Actor a = Busquedas.Cercano(new Vector2(tile.x, tile.y), 3f, null);
                if (a == null || !a.isAlive()) { Estado.Aviso("Toca a una persona", 2f); return false; }
                string nombre = a.getName();
                switch (id)
                {
                    case "wfx_bendicion":
                        a.addTrait("wfx_bendecido_obelisco", false); a.addRenown(5);
                        Estado.Aviso("El Obelisco bendice a " + nombre, 3f); break;
                    case "wfx_maldicion":
                        a.addTrait("wfx_maldito", false);
                        Estado.Aviso(nombre + " ha sido maldito", 3f); break;
                    case "wfx_juramento":
                        a.addTrait("wfx_juramentado", false);
                        Estado.Aviso(nombre + " jura lealtad a su reino", 3f); break;
                    case "wfx_destierro":
                        if (a.isKing()) { Estado.Aviso("Un rey no puede ser desterrado", 2f); return false; }
                        City c = a.city;
                        if (c != null && c.leader == a) c.removeLeader();
                        a.setCity(null);
                        a.addTrait("wfx_desterrado", false);
                        Estado.Aviso(nombre + " es desterrado por los dioses", 3f); break;
                    case "wfx_discordia":
                        Estado.Aviso("Los dioses siembran la discordia en el corazón de " + nombre, 3f); break;
                    case "wfx_reconciliacion":
                        Estado.Aviso("Los dioses ablandan el corazón de " + nombre, 3f); break;
                    default: return false;
                }
                Intervencion(id, Busquedas.IdDe(a), nombre);
                return true;
            }
            catch (Exception e) { Estado.Fallo("usar " + id, e); return false; }
        }

        static bool SueltaBestia(WorldTile tile)
        {
            int semilla = (tile.x * 73856093) ^ (tile.y * 19349663) ^ (int)(Time.unscaledTime * 1000f);
            BestiaDef def = Bestias.Elige(semilla);
            Actor b = World.world.units.spawnNewUnit(def.Especie, tile, true, true, 3f);
            if (b == null) { Estado.Aviso("La bestia no quiso nacer aquí", 2f); return false; }
            string nombre = Bestias.Nombre(def, semilla);
            b.setName(nombre);
            foreach (string r in Bestias.Rasgos) { try { b.addTrait(r, false); } catch (Exception e) { Estado.Fallo("rasgo_bestia " + r, e); } }
            b.addRenown(20);
            Busquedas.Sigue(b);
            Actor cerca = Busquedas.Cercano(new Vector2(tile.x, tile.y), 40f, b);
            string reino = cerca != null && cerca.kingdom != null && !cerca.kingdom.wild && cerca.kingdom.data != null ? cerca.kingdom.data.name : "";
            Busquedas.Externas(new System.Collections.Generic.List<Busqueda> { Bestias.Caza(Busquedas.IdDe(b), nombre, reino) });
            Estado.Aviso(nombre + " despierta...", 4f);
            Intervencion("wfx_bestia", Busquedas.IdDe(b), nombre);
            return true;
        }

        // Linea para PeceraWB (la lee su PuenteExpansion.cs): {"t":tipo,"id":"a123","n":"Nombre"}.
        static void Intervencion(string tipo, string id, string nombre)
        {
            string dir = Busquedas.CarpetaMundo;
            if (dir.Length == 0) return;
            try
            {
                File.AppendAllText(Path.Combine(dir, "intervenciones.jsonl"),
                    "{\"t\":\"" + Texto_.Escape(tipo) + "\",\"id\":\"" + Texto_.Escape(id) + "\",\"n\":\"" + Texto_.Escape(nombre) + "\"}\n",
                    new UTF8Encoding(false));
            }
            catch (Exception e) { Estado.Fallo("intervencion", e); }
        }
    }
}
