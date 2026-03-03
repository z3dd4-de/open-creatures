namespace Creatures;

/// <summary>
/// Eine chemische Reaktion: verbraucht Edukte, produziert Produkt.
/// rate wird durch das Genom gesteuert (später).
/// </summary>
public class ChemicalReaction
{
    public ChemID Reactant1  { get; init; }
    public ChemID? Reactant2 { get; init; }  // optional: 2. Edukt
    public ChemID Product    { get; init; }
    public float  Rate       { get; init; }  // Einheiten pro Sekunde

    /// <summary>
    /// Führt die Reaktion für einen Zeitschritt durch.
    /// Gibt true zurück wenn die Reaktion stattgefunden hat.
    /// </summary>
    public bool Apply(BiochemState state, float delta)
    {
        float r1 = state.Get(Reactant1);

        // Brauchen wir ein 2. Edukt?
        if (Reactant2.HasValue)
        {
            float r2 = state.Get(Reactant2.Value);
            if (r1 < 0.01f || r2 < 0.01f) return false;

            float amount = Rate * delta * r1 * r2; // Massenwirkungsgesetz
            state.Add(Reactant1, -amount);
            state.Add(Reactant2.Value, -amount);
            state.Add(Product, amount);
        }
        else
        {
            if (r1 < 0.01f) return false;

            float amount = Rate * delta * r1;
            state.Add(Reactant1, -amount);
            state.Add(Product, amount);
        }

        return true;
    }
}
