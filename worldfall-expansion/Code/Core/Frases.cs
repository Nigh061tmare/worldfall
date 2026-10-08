using System.Collections.Generic;

namespace WorldfallExpansion.Core
{
    // Frases nuevas para los recuerdos que cuentan los PNJ de Worldfall. Worldfall (Dialogue.GameLine) lee
    // las claves del juego «happiness_dialog_<tipo>_<n>» seguidas desde n=0 hasta 23 y elige una al azar;
    // la muestra entre comillas antes del momento: «"¡No!" Cuando perdí a mi amor, hace 2 años.».
    // El adaptador (Frases.cs) anade estas a partir del primer n libre. Tipos = ids de HappinessLibrary.
    public static class Frases
    {
        public const int MaxPorTipo = 24;

        public static string Clave(string tipo, int n) { return "happiness_dialog_" + tipo + "_" + n; }

        static readonly Dictionary<string, string[][]> datos = new Dictionary<string, string[][]>
        {
            { "death_lover", new[] {
                new[] { "¿Por qué tú y no yo?", "Why you and not me?" },
                new[] { "Prometiste que envejeceríamos juntos...", "You promised we'd grow old together..." },
                new[] { "La casa se ha quedado tan fría.", "The house has gone so cold." } } },
            { "death_child", new[] {
                new[] { "Ningún padre debería enterrar a un hijo.", "No parent should bury a child." },
                new[] { "Era tan pequeño... tan pequeño.", "So small... so small." },
                new[] { "Le cantaba cada noche. ¿Ahora a quién?", "I sang to them every night. Who now?" } } },
            { "death_family_member", new[] {
                new[] { "La mesa tiene un sitio vacío.", "There's an empty seat at the table." },
                new[] { "Nos queda su nombre. Lo diremos siempre.", "We still have the name. We'll always say it." },
                new[] { "La familia se hace más pequeña cada invierno.", "The family grows smaller every winter." } } },
            { "death_best_friend", new[] {
                new[] { "¿Quién se va a reír ahora de mis chistes?", "Who's going to laugh at my jokes now?" },
                new[] { "Le debía una cerveza. Se la debo para siempre.", "I owed them an ale. I owe it forever now." },
                new[] { "Fuimos hermanos sin serlo.", "We were siblings without being so." } } },
            { "got_robbed", new[] {
                new[] { "¡Mis ahorros! ¡Toda una vida!", "My savings! A whole life!" },
                new[] { "Como encuentre al ladrón...", "If I ever find the thief..." },
                new[] { "Ya no se puede confiar en nadie.", "You can't trust anyone anymore." } } },
            { "lost_fight", new[] {
                new[] { "La próxima vez no tendrá tanta suerte.", "Next time they won't be so lucky." },
                new[] { "Me ha dolido más el orgullo que los golpes.", "My pride hurt more than the blows." },
                new[] { "Vale, vale... me rindo.", "Fine, fine... I yield." } } },
            { "got_caught", new[] {
                new[] { "¡No es lo que parece!", "It's not what it looks like!" },
                new[] { "Alguien me ha delatado. Lo sé.", "Someone sold me out. I know it." },
                new[] { "Pagaré. Pero no olvidaré.", "I'll pay. But I won't forget." } } },
            { "just_killed", new[] {
                new[] { "Era él o yo.", "It was them or me." },
                new[] { "Me tiemblan las manos. No dejan de temblar.", "My hands are shaking. They won't stop." },
                new[] { "Que los dioses me perdonen.", "May the gods forgive me." } } },
            { "become_king", new[] {
                new[] { "La corona pesa más de lo que parecía.", "The crown is heavier than it looked." },
                new[] { "Juro cuidar de este reino como de mis hijos.", "I swear to care for this realm like my own children." },
                new[] { "¡Larga vida al rey! ...¿Ese soy yo?", "Long live the king! ...Is that me?" } } },
            { "become_leader", new[] {
                new[] { "Ahora todos me miran a mí.", "Now everyone looks to me." },
                new[] { "Esta ciudad merece algo mejor. Se lo daré.", "This town deserves better. I'll give it that." },
                new[] { "Que nadie pase hambre mientras yo mande.", "No one goes hungry while I lead." } } },
            { "just_won_war", new[] {
                new[] { "¡Victoria! ¡Que suenen los cuernos!", "Victory! Sound the horns!" },
                new[] { "Hemos ganado. Pero cuántos no han vuelto...", "We won. But so many didn't come back..." },
                new[] { "Que sus hijos canten nuestro nombre.", "Let their children sing our name." } } },
            { "just_lost_war", new[] {
                new[] { "Hemos perdido. Todo por nada.", "We lost. All for nothing." },
                new[] { "Volveremos a levantarnos. Siempre lo hacemos.", "We'll rise again. We always do." },
                new[] { "Malditos sean ellos y su rey.", "Curse them and their king." } } },
            { "just_made_peace", new[] {
                new[] { "Por fin podremos dormir tranquilos.", "At last we can sleep in peace." },
                new[] { "La paz es frágil. Cuidémosla.", "Peace is fragile. Let's guard it." },
                new[] { "Hoy nadie muere. Hoy es un buen día.", "No one dies today. Today is a good day." } } },
            { "was_conquered", new[] {
                new[] { "Ahora ondea otra bandera sobre nuestras casas.", "Now another banner flies over our homes." },
                new[] { "Nos quitaron la ciudad, no el corazón.", "They took our town, not our heart." },
                new[] { "Agacha la cabeza y sobrevive.", "Keep your head down and survive." } } },
            { "kingdom_fell_apart", new[] {
                new[] { "El reino se ha roto como un plato viejo.", "The realm broke like an old plate." },
                new[] { "¿A quién servimos ahora?", "Who do we serve now?" },
                new[] { "Lo vi venir. Nadie me escuchó.", "I saw it coming. No one listened." } } },
            { "just_started_war", new[] {
                new[] { "¡A las armas! ¡Es la guerra!", "To arms! It's war!" },
                new[] { "Otra guerra. Otra vez nuestros hijos.", "Another war. Our children again." },
                new[] { "Que corra su sangre y no la nuestra.", "Let their blood run, not ours." } } },
            { "just_rebelled", new[] {
                new[] { "¡Ya no somos sus esclavos!", "We're no longer their slaves!" },
                new[] { "Libres. Suena raro decirlo.", "Free. It feels strange to say it." },
                new[] { "Hoy nace algo nuevo.", "Today something new is born." } } },
            { "fallen_in_love", new[] {
                new[] { "Cuando me mira, se me olvida todo.", "When they look at me, I forget everything." },
                new[] { "Creo que estoy perdido. Y me da igual.", "I think I'm lost. And I don't care." },
                new[] { "¿Esto es lo que cantan los bardos?", "Is this what the bards sing about?" } } },
            { "just_had_child", new[] {
                new[] { "¡Mira qué manitas!", "Look at those tiny hands!" },
                new[] { "Te protegeré de todo. De todo.", "I'll protect you from everything. Everything." },
                new[] { "Tiene tus ojos. Y mi mal genio.", "They have your eyes. And my temper." } } },
            { "wrote_book", new[] {
                new[] { "Por fin está terminado.", "It's finally finished." },
                new[] { "Cuando yo no esté, esto quedará.", "When I'm gone, this will remain." },
                new[] { "Ojalá alguien lo lea.", "I hope someone reads it." } } },
            { "just_became_adult", new[] {
                new[] { "¡Ya soy mayor! Que tiemble el mundo.", "I'm grown! Let the world tremble." },
                new[] { "Ahora me toca a mí cargar con todo.", "Now it's my turn to carry everything." },
                new[] { "Echaré de menos ser pequeño.", "I'll miss being little." } } },
            { "just_found_house", new[] {
                new[] { "Un techo propio. Qué lujo.", "A roof of my own. What luxury." },
                new[] { "Aquí echaré raíces.", "Here I'll put down roots." },
                new[] { "Por fin algo que es mío.", "At last something that's mine." } } },
            { "just_lost_house", new[] {
                new[] { "Todo lo que tenía estaba ahí dentro.", "Everything I had was in there." },
                new[] { "Dormiré bajo las estrellas. Como al principio.", "I'll sleep under the stars. Like at the start." },
                new[] { "Una casa se reconstruye. Una vida, no.", "A house can be rebuilt. A life cannot." } } },
            { "just_made_friend", new[] {
                new[] { "¡Por fin alguien que me entiende!", "At last someone who gets me!" },
                new[] { "Brindemos por esta amistad.", "Let's drink to this friendship." },
                new[] { "Contigo el camino se hace corto.", "With you the road feels short." } } },
            { "just_injured", new[] {
                new[] { "¡Ay! Eso dejará marca.", "Ouch! That'll leave a mark." },
                new[] { "Sigo en pie. Por poco.", "Still standing. Barely." },
                new[] { "La sangre es mía, ¿verdad?", "That blood is mine, isn't it?" } } },
            { "just_cursed", new[] {
                new[] { "Algo frío se me ha metido dentro.", "Something cold got inside me." },
                new[] { "Desde hoy todo me sale torcido.", "From today everything goes crooked for me." },
                new[] { "¿Quién me odia tanto?", "Who hates me this much?" } } },
            { "had_nightmare", new[] {
                new[] { "Soñé que la tierra se tragaba la ciudad.", "I dreamt the earth swallowed the town." },
                new[] { "Había ojos en la oscuridad. Muchos ojos.", "There were eyes in the dark. So many eyes." },
                new[] { "No quiero volver a dormir.", "I don't want to sleep again." } } },
            { "lost_crown", new[] {
                new[] { "Ayer era rey. Hoy no soy nadie.", "Yesterday a king. Today nobody." },
                new[] { "Volveré a por lo que es mío.", "I'll come back for what's mine." },
                new[] { "Al menos ya no me duele la cabeza.", "At least my head doesn't ache anymore." } } },
            { "conquered_city", new[] {
                new[] { "¡La ciudad es nuestra!", "The city is ours!" },
                new[] { "Que nadie toque a los niños.", "Let no one touch the children." },
                new[] { "Ahora tendremos que gobernarla. Eso es más difícil.", "Now we have to rule it. That's harder." } } },
            { "lost_city", new[] {
                new[] { "La perdimos. Calle a calle.", "We lost it. Street by street." },
                new[] { "Juro que la recuperaremos.", "I swear we'll take it back." },
                new[] { "Mis vecinos... ¿dónde están mis vecinos?", "My neighbours... where are my neighbours?" } } },
            { "lost_capital", new[] {
                new[] { "Ha caído la capital. Ha caído el corazón.", "The capital has fallen. The heart has fallen." },
                new[] { "Sin capital no hay reino.", "No capital, no realm." },
                new[] { "Esto no acaba aquí.", "This doesn't end here." } } },
            { "just_felt_the_divine", new[] {
                new[] { "Alguien allá arriba me ha mirado.", "Someone up there looked at me." },
                new[] { "Lo he sentido. No estamos solos.", "I felt it. We are not alone." },
                new[] { "Una luz... y luego paz.", "A light... and then peace." } } },
            { "become_alpha", new[] {
                new[] { "*aúlla con fuerza*", "*howls loudly*" },
                new[] { "*la manada baja la cabeza*", "*the pack lowers its head*" },
                new[] { "*gruñe, satisfecho*", "*growls, satisfied*" } } },
        };

        public static IEnumerable<string> Tipos { get { return datos.Keys; } }

        public static List<string> De(string tipo, bool espanol)
        {
            var l = new List<string>();
            string[][] f;
            if (datos.TryGetValue(tipo, out f)) foreach (string[] par in f) l.Add(espanol ? par[0] : par[1]);
            return l;
        }

        // Claves y textos a anadir para un tipo, a partir del primer indice libre. existe(clave) consulta el juego.
        public static List<KeyValuePair<string, string>> Plan(string tipo, bool espanol, System.Func<string, bool> existe)
        {
            var r = new List<KeyValuePair<string, string>>();
            int n = 0;
            while (n < MaxPorTipo && existe(Clave(tipo, n))) n++;
            foreach (string s in De(tipo, espanol))
            {
                if (n >= MaxPorTipo) break;
                r.Add(new KeyValuePair<string, string>(Clave(tipo, n++), s));
            }
            return r;
        }
    }
}
