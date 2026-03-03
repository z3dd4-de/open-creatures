namespace Creatures;

/// <summary>
/// Eindeutige IDs für alle Chemikalien im Blutkreislauf.
/// Neue Chemikalien einfach hier eintragen.
/// </summary>
public enum ChemID
{
    // Nährstoffe
    Glucose       = 0,   // Energie aus Nahrung
    NutrientStore = 1,   // Langzeitspeicher

    // Triebe (steigen kontinuierlich, werden durch Aktionen abgebaut)
    Hunger        = 10,  // steigt wenn Glucose sinkt
    Tiredness     = 11,  // steigt mit der Zeit → Schlafdrang
    SexDrive      = 12,  // steigt wenn Energie hoch & ausgeruht

    // Zustands-Chemikalien
    IsAsleep      = 20,  // > 0.5 = schläft gerade
    Reward        = 30,  // positives Feedback (für späteres Lernen)
    Punishment    = 31,  // negatives Feedback
}
