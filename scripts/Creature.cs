using Godot;

namespace Creatures;

/// <summary>
/// Das Wesen selbst (Sprite2D / CharacterBody2D Rechteck).
///
/// Szenen-Setup in Godot:
///   Creature (CharacterBody2D)
///     └── Sprite2D          (einfaches Rechteck-Bild oder ColorRect)
///     └── CollisionShape2D  (RectangleShape2D)
///     └── DetectionArea (Area2D)
///           └── CollisionShape2D (CircleShape2D, Radius ~40)
///
/// Das Wesen bewegt sich zufällig und frisst Nahrung wenn es sie berührt.
/// Ist es schläfrig, bleibt es stehen.
/// </summary>
public partial class Creature : CharacterBody2D
{
    // ── Biochemie ─────────────────────────────────────────────────────────────
    public Biochemistry Biochem { get; } = new();

    // ── Bewegung ──────────────────────────────────────────────────────────────
    [Export] public float MoveSpeed     = 80f;
    [Export] public float DirectionChangeInterval = 2f;
    [Export] public AnimatedSprite2D Animation;

    private Vector2 _moveDir            = Vector2.Zero;
    private Orientation orientation = Orientation.LEFT;
    private float   _directionTimer     = 0f;

    // ── Debug-Label ───────────────────────────────────────────────────────────
    // Optional: Label-Node als Kind anhängen für Live-Status
    private Label _statusLabel;

    public override void _Ready()
    {
        // Status-Label suchen (optional, muss nicht existieren)
        _statusLabel = GetNodeOrNull<Label>("StatusLabel");

        // Detections-Area verbinden (falls vorhanden)
        var detection = GetNodeOrNull<Area2D>("DetectionArea");
        if (detection != null)
            detection.AreaEntered += OnAreaEntered;

        GD.Print("[Creature] Wesen initialisiert.");
    }

    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;

        // 1. Biochemie updaten
        Biochem.Tick(dt);

        // 2. Bewegen (nur wenn wach)
        bool asleep = Biochem.State.Get(ChemID.IsAsleep) > 0.5f;
        if (!asleep)
        {
            HandleMovement(dt);
        }
        else
        {
            Velocity = Vector2.Zero;
            PlayAnimation("sleep");
        }

        MoveAndSlide();

        // 3. Statusanzeige updaten
        if (_statusLabel != null)
            _statusLabel.Text = Biochem.State.StatusLine();
    }

    // ── Bewegungslogik ────────────────────────────────────────────────────────

    private void HandleMovement(float delta)
    {
        _directionTimer -= delta;
        if (_directionTimer <= 0f)
        {
            // Neue Zufallsrichtung wählen
            float angle   = (float)GD.RandRange(0, Mathf.Tau);
            _moveDir      = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            
            if (_moveDir.X < 0)
            {
                orientation = Orientation.LEFT;
                PlayAnimation("run");
            }
            else
            {
                orientation = Orientation.RIGHT;
                PlayAnimation("run");
            }
            _directionTimer = (float)GD.RandRange(1.0, DirectionChangeInterval);
        }

        // Hunger beeinflusst Bewegungsgeschwindigkeit (hungrig = aktiver)
        float hunger    = Biochem.State.Get(ChemID.Hunger);
        float speedMult = Mathf.Lerp(0.5f, 1.5f, hunger);
        Velocity        = _moveDir * MoveSpeed * speedMult;
    }

    private void PlayAnimation(string name)
    {
        string suffix = "_left";
        if (orientation == Orientation.LEFT)
        {
            Animation.FlipH = false;
            Animation.Play(name + suffix);
        }
        else
        {
            Animation.FlipH = true;
            Animation.Play(name + suffix);
        }
    }

    // ── Kollisionserkennung ───────────────────────────────────────────────────

    private void OnAreaEntered(Area2D area)
    {
        // Ist es Nahrung?
        if (area is Food food)
        {
            Biochem.OnEat(food.NutritionValue);
            food.OnEaten();
        }
    }
}
