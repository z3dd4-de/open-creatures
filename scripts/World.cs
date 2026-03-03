using Godot;

namespace Creatures;

/// <summary>
/// Haupt-Szenen-Node (Node2D).
/// Verwaltet die Welt: Grenzen, Nahrung spawnen, Kamera, Debug-HUD.
///
/// Szenen-Setup:
///   World (Node2D)  ← Script: World.cs
///     ├── Creature         (instanziiert, Name: "Creature")
///     ├── FoodContainer    (Node2D, hält alle Food-Instanzen)
///     ├── WallTop          (StaticBody2D + CollisionShape2D)
///     ├── WallBottom       (StaticBody2D + CollisionShape2D)
///     ├── WallLeft         (StaticBody2D + CollisionShape2D)
///     ├── WallRight        (StaticBody2D + CollisionShape2D)
///     └── HUD (CanvasLayer)
///           └── StatusLabel (Label, oben links)
/// </summary>
public partial class World : Node2D
{
    // ── Exports ───────────────────────────────────────────────────────────────
    [Export] public PackedScene FoodScene;           // Food.tscn hier reinziehen
    [Export] public int         FoodCount     = 5;
    [Export] public Vector2     WorldHalfSize = new Vector2(320f, 200f); // Weltgröße (Hälfte)

    // ── Interne Referenzen ────────────────────────────────────────────────────
    private Creature _creature;
    private Node2D   _foodContainer;
    private Label    _statusLabel;

    public override void _Ready()
    {
        _creature      = GetNode<Creature>("Creature");
        _foodContainer = GetNodeOrNull<Node2D>("FoodContainer") ?? this;
        _statusLabel   = GetNodeOrNull<Label>("HUD/StatusLabel");

        // Signals verbinden (für Biochemie-Updates)
        //Signals.Instance.BiochemChanged += UpdateLabel;

        SpawnFood();

        GD.Print($"[World] Welt bereit. Größe: {WorldHalfSize * 2}");
    }

    public override void _Process(double delta)
    {
        UpdateHUD();
    }

    private void UpdateLabel(string text)
    {
        _statusLabel.Text = text;
    }

    // ── Nahrung spawnen ───────────────────────────────────────────────────────

    private void SpawnFood()
    {
        if (FoodScene == null)
        {
            GD.PrintErr("[World] FoodScene nicht gesetzt! Bitte Food.tscn im Inspector zuweisen.");
            return;
        }

        for (int i = 0; i < FoodCount; i++)
        {
            var food = FoodScene.Instantiate<Food>();
            food.SpawnArea = WorldHalfSize * 0.9f; // etwas Rand lassen
            _foodContainer.AddChild(food);

            // Zufällige Startposition
            food.GlobalPosition = new Vector2(
                (float)GD.RandRange(-WorldHalfSize.X * 0.9f, WorldHalfSize.X * 0.9f),
                (float)GD.RandRange(-WorldHalfSize.Y * 0.9f, WorldHalfSize.Y * 0.9f)
            );
        }

        GD.Print($"[World] {FoodCount} Nahrungsobjekte gespawnt.");
    }

    // ── HUD ───────────────────────────────────────────────────────────────────

    private void UpdateHUD()
    {
        if (_statusLabel == null || _creature == null) return;
        _statusLabel.Text = _creature.Biochem.State.StatusLine();
    }

    // ── Weltgrenzen (falls keine StaticBody2D-Wände in der Szene) ─────────────
    // Alternativ: einfach 4 StaticBody2D-Nodes in der Szene platzieren.
    // Diese Methode zeichnet nur eine Debug-Visualisierung der Grenzen.

    public override void _Draw()
    {
        var rect = new Rect2(-WorldHalfSize, WorldHalfSize * 2f);
        DrawRect(rect, new Color(0.3f, 0.6f, 0.3f, 0.3f), filled: false, width: 2f);
    }
}