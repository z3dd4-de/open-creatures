using Godot;
using System;

namespace Creatures;

/// <summary>
/// Das Wesen. Verbindet Biochemie, Genom und neuronales Netz.
/// 
/// NEU:
/// - Wanderkennung mit Abprallen
/// - Trieb-Override: Hunger/Müdigkeit übersteuern das NN direkt
/// - Einfaches Hebb'sches Reinforcement: Reward/Punishment stärkt/schwächt
///   die zuletzt ausgeführte Aktion
/// </summary>
public partial class Creature : CharacterBody2D
{
    public  Biochemistry  Biochem { get; } = new();
    public  Genome        Genome  { get; private set; }
    private NeuralNetwork _brain;

    [Export] public float MoveSpeed        = 80f;
    [Export] public float SensorRadius     = 200f;
    [Export] public bool  DebugBrainOutput = true;

    // Weltgrenzen – werden von World.cs gesetzt
    public Vector2 WorldHalfSize = new Vector2(320f, 200f);

    private AnimatedSprite2D _sprite;
    private Food             _nearestFood;
    private Creature         _nearestCreature;
    private int              _currentAction  = CreatureSenses.O_Idle;
    private float[]          _lastInputs     = new float[CreatureSenses.InputCount];
    private float            _debugTimer     = 0f;

    // Wandabprall-Zustand
    private Vector2 _wallBounceDir   = Vector2.Zero;
    private float   _wallBounceTimer = 0f;
    private const float WallBounceSeconds = 1.2f;
    private const float WallMargin        = 20f;

    // Lernrate für Reinforcement
    private const float LearningRate = 0.05f;

    public override void _Ready()
    {
        _sprite = GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");

        var detection = GetNodeOrNull<Area2D>("DetectionArea");
        if (detection != null)
        {
            detection.AreaEntered += OnAreaEntered;
            detection.AreaExited  += OnAreaExited;
        }

        _brain = new NeuralNetwork(
            inputCount:  CreatureSenses.InputCount,
            hiddenCount: 8,
            outputCount: CreatureSenses.OutputCount
        );

        Genome = new Genome(_brain.WeightCount);
        Genome.Randomize(_brain);
        Genome.ApplyToBiochem(Biochem);

        // Eindeutigen Namen generieren und als Label anzeigen
        //string creatureName = Name; // Godot setzt den Node-Namen automatisch (z.B. "Creature", "Creature2", "Creature3")
        //if (GetNodeOrNull<Label>("NameLabel") is Label nameLabel)
        //    nameLabel.Text = creatureName;

        GD.Print($"[Creature] Initialisiert. Genom-Länge: {Genome.TotalLength} Gene");
        GD.Print($"[Creature] Biochemie:\n{Genome.BiochemDebugString()}");
    }

    public void RefreshNameLabel()
    {
        if (GetNodeOrNull<Label>("NameLabel") is Label nameLabel)
            nameLabel.Text = Name;
    }

    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;
        Biochem.Tick(dt);

        // Reinforcement: Reward/Punishment aus Biochemie auf zuletzt gewählte Aktion anwenden
        ApplyReinforcement();

        bool asleep = Biochem.State.Get(ChemID.IsAsleep) > 0.5f;

        if (!asleep)
        {
            // Wandabprall hat Priorität
            if (_wallBounceTimer > 0f)
            {
                _wallBounceTimer -= dt;
                Velocity = _wallBounceDir * MoveSpeed;
            }
            else
            {
                // Trieb-Override prüfen – starke biologische Reize übersteuern das NN
                int drivenAction = GetDriveOverride();

                if (drivenAction >= 0)
                {
                    // Biologischer Trieb ist stark genug
                    _currentAction = drivenAction;
                }
                else
                {
                    // Normaler NN-Entscheid
                    _lastInputs    = GatherInputs();
                    float[] outputs = _brain.Forward(_lastInputs);
                    _currentAction  = CreatureSenses.ArgMax(outputs);

                    if (DebugBrainOutput)
                    {
                        _debugTimer += dt;
                        if (_debugTimer > 2f)
                        {
                            _debugTimer = 0f;
                            GD.Print($"[Brain] In:  {CreatureSenses.InputDebugString(_lastInputs)}");
                            GD.Print($"[Brain] Out: {CreatureSenses.OutputDebugString(outputs)} -> {ActionName(_currentAction)}");
                        }
                    }
                }

                ExecuteAction(_currentAction);
                CheckWallBounce();
            }
        }
        else
        {
            Velocity       = Vector2.Zero;
            _currentAction = CreatureSenses.O_Idle;
        }

        MoveAndSlide();
        UpdateAnimation(asleep);
    }

    // ── Trieb-Override ────────────────────────────────────────────────────────
    // Wenn ein Trieb sehr stark ist, wird das NN umgangen.
    // Das sorgt dafür, dass das Wesen auch ohne Training sinnvoll reagiert
    // UND gibt dem NN ein klares Lernsignal (Reward nach Fressen).

    private int GetDriveOverride()
    {
        float hunger = Biochem.State.Get(ChemID.Hunger);

        if (hunger > 0.65f && _nearestFood != null && IsInstanceValid(_nearestFood))
        {
            Vector2 toFood = _nearestFood.GlobalPosition - GlobalPosition;
            return DirectionToAction(toFood);
        }

        // NEU: Hungrig aber keine Nahrung in Sicht → zufällig suchen
        if (hunger > 0.65f)
        {
            // Alle 2 Sekunden neue Zufallsrichtung
            _searchTimer -= (float)GetProcessDeltaTime();
            if (_searchTimer <= 0f)
            {
                _searchDir   = (int)(GD.Randi() % 4); // 0-3 = Links/Rechts/Hoch/Runter
                _searchTimer = 2f;
            }
            return _searchDir;
        }

        return -1;
    }

    private float _searchTimer = 0f;
    private int   _searchDir   = CreatureSenses.O_MoveRight;

    // Wandelt einen Richtungsvektor in die nächste Himmelsrichtungs-Aktion um
    private static int DirectionToAction(Vector2 dir)
    {
        if (MathF.Abs(dir.X) > MathF.Abs(dir.Y))
            return dir.X > 0 ? CreatureSenses.O_MoveRight : CreatureSenses.O_MoveLeft;
        else
            return dir.Y > 0 ? CreatureSenses.O_MoveDown : CreatureSenses.O_MoveUp;
    }

    // ── Wanderkennung ─────────────────────────────────────────────────────────

    private void CheckWallBounce()
    {
        Vector2 pos  = GlobalPosition;
        Vector2 newDir = Vector2.Zero;

        if      (pos.X < -WorldHalfSize.X + WallMargin) newDir.X =  1f;
        else if (pos.X >  WorldHalfSize.X - WallMargin) newDir.X = -1f;

        if      (pos.Y < -WorldHalfSize.Y + WallMargin) newDir.Y =  1f;
        else if (pos.Y >  WorldHalfSize.Y - WallMargin) newDir.Y = -1f;

        if (newDir != Vector2.Zero)
        {
            _wallBounceDir   = newDir.Normalized();
            _wallBounceTimer = WallBounceSeconds;
            // Kleiner Schmerz-Reiz damit das NN lernt, Wände zu meiden
            Biochem.OnHurt(0.05f);
        }
    }

    // ── Reinforcement Learning (Hebb'sch) ─────────────────────────────────────
    // Reward/Punishment aus der Biochemie verstärken/schwächen die Output-Gewichte
    // der zuletzt gewählten Aktion. Simpel, aber biologisch motiviert.

    private void ApplyReinforcement()
    {
        float reward     = Biochem.State.Get(ChemID.Reward);
        float punishment = Biochem.State.Get(ChemID.Punishment);
        float signal     = (reward - punishment) * LearningRate;

        if (MathF.Abs(signal) < 0.0001f) return;

        // Nur Output-Schicht anpassen (einfachste Form)
        _brain.ReinforceLastAction(_currentAction, signal);
    }

    // ── Sensorik ──────────────────────────────────────────────────────────────

    private float[] GatherInputs()
    {
        var inputs = new float[CreatureSenses.InputCount];

        inputs[CreatureSenses.I_Hunger]    = Biochem.State.Get(ChemID.Hunger);
        inputs[CreatureSenses.I_Tiredness] = Biochem.State.Get(ChemID.Tiredness);
        inputs[CreatureSenses.I_SexDrive]  = Biochem.State.Get(ChemID.SexDrive);
        inputs[CreatureSenses.I_Glucose]   = Biochem.State.Get(ChemID.Glucose);

        if (_nearestFood != null && IsInstanceValid(_nearestFood))
        {
            Vector2 toFood = _nearestFood.GlobalPosition - GlobalPosition;
            float dist     = toFood.Length();
            inputs[CreatureSenses.I_FoodDist] = 1f - Math.Clamp(dist / SensorRadius, 0f, 1f);
            Vector2 dir    = dist > 0.01f ? toFood.Normalized() : Vector2.Zero;
            inputs[CreatureSenses.I_FoodDirX] = dir.X * 0.5f + 0.5f;
            inputs[CreatureSenses.I_FoodDirY] = dir.Y * 0.5f + 0.5f;
        }
        else
        {
            inputs[CreatureSenses.I_FoodDist] = 0f;
            inputs[CreatureSenses.I_FoodDirX] = 0.5f;
            inputs[CreatureSenses.I_FoodDirY] = 0.5f;
        }

        if (_nearestCreature != null && IsInstanceValid(_nearestCreature))
        {
            float dist = GlobalPosition.DistanceTo(_nearestCreature.GlobalPosition);
            inputs[CreatureSenses.I_CreatureDist] = 1f - Math.Clamp(dist / SensorRadius, 0f, 1f);
        }

        return inputs;
    }

    // ── Aktionsausführung ─────────────────────────────────────────────────────

    private void ExecuteAction(int action)
    {
        float hunger    = Biochem.State.Get(ChemID.Hunger);
        float speedMult = Mathf.Lerp(0.6f, 1.4f, hunger);
        float speed     = MoveSpeed * speedMult;

        Velocity = action switch
        {
            CreatureSenses.O_MoveLeft  => new Vector2(-speed, 0),
            CreatureSenses.O_MoveRight => new Vector2( speed, 0),
            CreatureSenses.O_MoveUp    => new Vector2(0, -speed),
            CreatureSenses.O_MoveDown  => new Vector2(0,  speed),
            _                          => Vector2.Zero,
        };
    }

    // ── Animation ─────────────────────────────────────────────────────────────

    private void UpdateAnimation(bool asleep)
    {
        if (_sprite == null) return;

        if (asleep)
        {
            _sprite.Play("sleep");
            return;
        }

        bool moving = Velocity.LengthSquared() > 1f;
        _sprite.Play(moving ? "run" : "idle");

        if      (Velocity.X > 0.1f)  _sprite.FlipH = true;
        else if (Velocity.X < -0.1f) _sprite.FlipH = false;
    }

    // ── Kollisions-Events ─────────────────────────────────────────────────────

    private void OnAreaEntered(Area2D area)
    {
        if (area is Food food)
        {
            _nearestFood = food;
            // Fressen: immer wenn hungrig (Trieb-Override sorgt dafür, 
            // dass wir zur Nahrung laufen) oder direkt daneben
            if (Biochem.State.Get(ChemID.Hunger) > 0.2f)
            {
                Biochem.OnEat(food.NutritionValue);
                food.OnEaten();
            }
        }
        else if (area.GetParent() is Creature other && other != this)
        {
            _nearestCreature = other;
        }
    }

    private void OnAreaExited(Area2D area)
    {
        if (area is Food food && _nearestFood == food)
            _nearestFood = null;
        else if (area.GetParent() is Creature other && _nearestCreature == other)
            _nearestCreature = null;
    }

    // ── Reproduktion ─────────────────────────────────────────────────────────

    public Genome CreateOffspringGenome(Creature partner)
        => Genome.Crossover(this.Genome, partner.Genome)
                 .Mutate(mutationRate: 0.02f, mutationStrength: 0.08f);

    public void Initialize(Genome genome)
    {
        _brain ??= new NeuralNetwork(CreatureSenses.InputCount, 8, CreatureSenses.OutputCount);
        Genome = genome;
        genome.ApplyToNetwork(_brain);
        genome.ApplyToBiochem(Biochem);

        // Name jetzt korrekt, da World ihn vor Initialize() gesetzt hat
        if (GetNodeOrNull<Label>("NameLabel") is Label nameLabel)
            nameLabel.Text = Name;
    }

    // ── Hilfsmethoden ─────────────────────────────────────────────────────────

    private static string ActionName(int action) => action switch
    {
        CreatureSenses.O_MoveLeft  => "Links",
        CreatureSenses.O_MoveRight => "Rechts",
        CreatureSenses.O_MoveUp    => "Hoch",
        CreatureSenses.O_MoveDown  => "Runter",
        _                          => "Idle"
    };
}