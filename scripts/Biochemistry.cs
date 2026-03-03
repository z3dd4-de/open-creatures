using System.Collections.Generic;
using Godot;

namespace Creatures;

/// <summary>
/// Die Biochemie-Engine des Wesens.
/// Verwaltet Reaktionen, natürlichen Zerfall, Trieb-Aufbau und Schlaf-Logik.
///
/// Aufbau der Reaktionskette (vereinfacht):
///
///   Zeit  ──────────────► Tiredness
///   Zeit  ──────────────► Hunger (langsam)
///   Glucose  ──────────► NutrientStore (Speicherung)
///   Tiredness+IsAsleep  → Tiredness sinkt (Erholung)
///   Glucose sinkt       → Hunger steigt (indirekt via Reaktion)
///   Energie hoch+ausgeruht → SexDrive steigt
///
/// </summary>
public class Biochemistry
{
    [Signal]
    public delegate void BiochemEventHandler(string myString);
    public BiochemState State { get; } = new();

    // Wie oft (in Sekunden) wir einen Debug-Print machen
    private float _debugTimer = 0f;
    public float DebugInterval = 3f;

    // Konfigurierbare Raten (später aus Genom)
    public float HungerRate     = 0.008f;  // wie schnell Hunger steigt
    public float TirednessRate  = 0.005f;  // wie schnell Müdigkeit steigt
    public float SexDriveRate   = 0.004f;  // Aufbaurate Sexualtrieb
    public float DecayRate      = 0.003f;  // allg. Zerfall von Reward/Punishment
    public float SleepHealRate  = 0.04f;   // wie schnell Schlaf Müdigkeit abbaut

    // Schwellwerte
    public float SleepThreshold = 0.85f;   // ab hier schläft das Wesen ein
    public float WakeThreshold  = 0.15f;   // darunter wacht es auf

    private readonly List<ChemicalReaction> _reactions;

    public Biochemistry()
    {
        // Startwerte
        State.Set(ChemID.Glucose, 0.8f);
        State.Set(ChemID.NutrientStore, 0.5f);
        State.Set(ChemID.Hunger, 0.1f);

        // Reaktionen definieren
        _reactions = new List<ChemicalReaction>
        {
            // Glucose wird in Nährstoffspeicher umgewandelt (wenn viel Glucose da)
            new() {
                Reactant1 = ChemID.Glucose,
                Product   = ChemID.NutrientStore,
                Rate      = 0.05f
            },
            // Nährstoffspeicher wird mobilisiert wenn Glucose knapp
            // (wird manuell in Tick() behandelt, da bedingungsabhängig)
        };
    }

    /// <summary>Hauptupdate, wird jeden _PhysicsProcess-Frame aufgerufen.</summary>
    public void Tick(float delta)
    {
        HandleGlucoseMetabolism(delta);
        HandleHunger(delta);
        HandleTiredness(delta);
        HandleSleepCycle(delta);
        HandleSexDrive(delta);
        ApplyReactions(delta);
        ApplyDecay(delta);

        // Debug-Ausgabe
        _debugTimer += delta;
        if (_debugTimer >= DebugInterval)
        {
            _debugTimer = 0f;
            GD.Print($"[Biochem]\n{State.DebugString()}");
            //Signals.Instance.EmitSignal(nameof(Signals.BiochemChangedEventHandler), State.DebugString());
        }
    }

    // ── Metabolismus ─────────────────────────────────────────────────────────

    private void HandleGlucoseMetabolism(float delta)
    {
        // Glucose sinkt passiv (Grundumsatz) – weniger im Schlaf
        float isAsleep  = State.Get(ChemID.IsAsleep);
        float burnRate  = Mathf.Lerp(0.006f, 0.002f, isAsleep); // wach vs. schlafend
        State.Add(ChemID.Glucose, -burnRate * delta);

        // Falls Glucose knapp: Nährstoffspeicher mobilisieren
        if (State.Get(ChemID.Glucose) < 0.2f && State.Get(ChemID.NutrientStore) > 0.05f)
        {
            float mobilize = 0.03f * delta;
            State.Add(ChemID.NutrientStore, -mobilize);
            State.Add(ChemID.Glucose, mobilize * 0.8f); // etwas Verlust
        }
    }

    private void HandleHunger(float delta)
    {
        // Hunger steigt wenn Glucose niedrig
        float glucose      = State.Get(ChemID.Glucose);
        float hungerSignal = Mathf.Clamp(1f - glucose * 2f, 0f, 1f); // viel Hunger bei wenig Glucose
        State.Add(ChemID.Hunger, hungerSignal * HungerRate * delta);

        // Hunger sinkt leicht wenn Glucose hoch
        if (glucose > 0.7f)
            State.Add(ChemID.Hunger, -HungerRate * 0.5f * delta);
    }

    private void HandleTiredness(float delta)
    {
        // Müdigkeit steigt nur wenn wach
        float isAsleep = State.Get(ChemID.IsAsleep);
        if (isAsleep < 0.5f)
            State.Add(ChemID.Tiredness, TirednessRate * delta);
    }

    private void HandleSleepCycle(float delta)
    {
        float tired    = State.Get(ChemID.Tiredness);
        float isAsleep = State.Get(ChemID.IsAsleep);

        if (isAsleep < 0.5f)
        {
            // Einschlafen wenn sehr müde
            if (tired >= SleepThreshold)
            {
                State.Set(ChemID.IsAsleep, 1f);
                GD.Print("[Biochem] Das Wesen schläft ein (Tiredness: " + tired.ToString("F2") + ")");
            }
        }
        else
        {
            // Schläft: Müdigkeit wird abgebaut
            State.Add(ChemID.Tiredness, -SleepHealRate * delta);

            // Aufwachen wenn ausgeruht
            if (tired <= WakeThreshold)
            {
                State.Set(ChemID.IsAsleep, 0f);
                GD.Print("[Biochem] Das Wesen wacht auf.");
            }
        }
    }

    private void HandleSexDrive(float delta)
    {
        // Sexualtrieb steigt nur wenn ausgeruht UND gut ernährt
        float tired    = State.Get(ChemID.Tiredness);
        float glucose  = State.Get(ChemID.Glucose);
        float isAsleep = State.Get(ChemID.IsAsleep);

        bool wellFed   = glucose > 0.5f;
        bool notTired  = tired < 0.4f;
        bool awake     = isAsleep < 0.5f;

        if (wellFed && notTired && awake)
            State.Add(ChemID.SexDrive, SexDriveRate * delta);
        else
            State.Add(ChemID.SexDrive, -SexDriveRate * 0.5f * delta); // langsam abfallen
    }

    private void ApplyReactions(float delta)
    {
        foreach (var reaction in _reactions)
            reaction.Apply(State, delta);
    }

    private void ApplyDecay(float delta)
    {
        // Reward/Punishment klingen von selbst ab
        State.Add(ChemID.Reward,     -DecayRate * delta);
        State.Add(ChemID.Punishment, -DecayRate * delta);
    }

    // ── Externe Ereignisse ────────────────────────────────────────────────────

    /// <summary>Wesen frisst Nahrung: Glucose und NutrientStore steigen.</summary>
    public void OnEat(float nutritionValue = 0.4f)
    {
        State.Add(ChemID.Glucose,       nutritionValue * 0.6f);
        State.Add(ChemID.NutrientStore, nutritionValue * 0.4f);
        State.Add(ChemID.Hunger,       -nutritionValue * 0.5f); // Hunger sinkt sofort etwas
        State.Add(ChemID.Reward,        0.3f); // positives Feedback
        GD.Print($"[Biochem] Gegessen! Glucose: {State.Get(ChemID.Glucose):F2}, Hunger: {State.Get(ChemID.Hunger):F2}");
    }

    /// <summary>Wesen wird verletzt.</summary>
    public void OnHurt(float painAmount = 0.3f)
    {
        State.Add(ChemID.Punishment, painAmount);
        GD.Print($"[Biochem] Schmerz! Punishment: {State.Get(ChemID.Punishment):F2}");
    }
}
