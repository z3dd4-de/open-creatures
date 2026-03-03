namespace Creatures;

/// <summary>
/// Definiert alle Input- und Output-Neuronen des Netzes als benannte Konstanten.
/// So bleibt der Index-Zugriff lesbar und erweiterbar.
///
/// Neue Sinne/Aktionen einfach hier eintragen und InputCount/OutputCount anpassen.
/// </summary>
public static class CreatureSenses
{
    // ── Inputs (Sensorik) ─────────────────────────────────────────────────────
    // Alle Werte normalisiert auf 0.0 – 1.0

    // Interne Reize (aus Biochemie)
    public const int I_Hunger      = 0;  // aktueller Hunger-Level
    public const int I_Tiredness   = 1;  // Müdigkeit
    public const int I_SexDrive    = 2;  // Sexualtrieb
    public const int I_Glucose     = 3;  // Energiereserve

    // Externe Reize (Wahrnehmung der Umwelt)
    public const int I_FoodDist    = 4;  // Distanz zur nächsten Nahrung (1=weit, 0=nah)
    public const int I_FoodDirX    = 5;  // Richtung zur Nahrung X (-1..1, normalisiert)
    public const int I_FoodDirY    = 6;  // Richtung zur Nahrung Y
    public const int I_CreatureDist= 7;  // Distanz zum nächsten anderen Wesen

    // Konstante
    public const int InputCount    = 8;

    // ── Outputs (Aktionen) ────────────────────────────────────────────────────
    // Jede Aktivierung 0..1; höchster Wert gewinnt (ArgMax)

    public const int O_MoveLeft    = 0;
    public const int O_MoveRight   = 1;
    public const int O_MoveUp      = 2;
    public const int O_MoveDown    = 3;
    public const int O_Idle        = 4;  // stehenbleiben / ausruhen
    // O_Eat entfällt – Fressen passiert automatisch bei Kontakt

    public const int OutputCount   = 5;

    // ── Hilfsmethode: lesbare Debug-Ausgabe ───────────────────────────────────

    public static string InputDebugString(float[] inputs)
    {
        if (inputs.Length < InputCount) return "[zu wenige Inputs]";
        return $"  Hunger:{inputs[I_Hunger]:F2} Tired:{inputs[I_Tiredness]:F2} " +
               $"Sex:{inputs[I_SexDrive]:F2} Gluc:{inputs[I_Glucose]:F2} | " +
               $"FoodDist:{inputs[I_FoodDist]:F2} FoodDir:({inputs[I_FoodDirX]:F2},{inputs[I_FoodDirY]:F2}) " +
               $"CreatureDist:{inputs[I_CreatureDist]:F2}";
    }

    public static string OutputDebugString(float[] outputs)
    {
        if (outputs.Length < OutputCount) return "[zu wenige Outputs]";
        string[] names = { "Left", "Right", "Up", "Down", "Idle" };
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < OutputCount; i++)
            sb.Append($"  {names[i]}:{outputs[i]:F2}");
        return sb.ToString();
    }

    public static int ArgMax(float[] outputs)
    {
        int best = 0;
        for (int i = 1; i < outputs.Length; i++)
            if (outputs[i] > outputs[best]) best = i;
        return best;
    }
}
