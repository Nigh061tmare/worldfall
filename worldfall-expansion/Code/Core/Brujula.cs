using System;

namespace WorldfallExpansion.Core
{
    // Rumbo y giro hacia un objetivo, en coordenadas del mapa (x al este, y al norte: el mismo convenio
    // que Worldfall usa en su Quests.Compass). El yaw de Worldfall va en radianes y su «delante» es
    // (cos yaw, sin yaw), segun su GpuRenderer.
    public static class Brujula
    {
        static readonly string[] Puntos = { "este", "noreste", "norte", "noroeste", "oeste", "suroeste", "sur", "sureste" };

        public static string Rumbo(double dx, double dy)
        {
            if (Math.Abs(dx) < 1e-6 && Math.Abs(dy) < 1e-6) return "aqui";
            double g = (Math.Atan2(dy, dx) * 180 / Math.PI + 360) % 360;
            return Puntos[(int)Math.Round(g / 45) % 8];
        }

        // Angulo relativo en grados (-180..180]: positivo = a la izquierda (sentido antihorario).
        public static double Relativo(double dx, double dy, double yaw)
        {
            double r = (Math.Atan2(dy, dx) - yaw) * 180 / Math.PI;
            while (r <= -180) r += 360;
            while (r > 180) r -= 360;
            return r;
        }

        public static string Giro(double dx, double dy, double yaw, bool invertir)
        {
            double r = Relativo(dx, dy, yaw);
            if (invertir) r = -r;
            double a = Math.Abs(r);
            if (a <= 20) return "justo delante";
            if (a >= 150) return "a tu espalda";
            string lado = r > 0 ? "izquierda" : "derecha";
            return a < 70 ? "delante a la " + lado : (a <= 110 ? "a tu " + lado : "detras a la " + lado);
        }

        public static string Distancia(double d)
        {
            if (d < 3) return "aqui mismo";
            if (d < 1000) return ((int)Math.Round(d)) + " casillas";
            return (Math.Round(d / 100) / 10).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + " mil casillas";
        }

        // Linea de HUD: "42 casillas al noreste · delante a la izquierda". conYaw=false en modo dios.
        public static string Linea(double dx, double dy, bool conYaw, double yaw, bool invertir)
        {
            double d = Math.Sqrt(dx * dx + dy * dy);
            if (d < 3) return "aqui mismo";
            string s = Distancia(d) + " al " + Rumbo(dx, dy);
            if (conYaw) s += " · " + Giro(dx, dy, yaw, invertir);
            return s;
        }
    }
}
