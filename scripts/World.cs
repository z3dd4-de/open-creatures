using Godot;
using System.Collections.Generic;

namespace Creatures;

public partial class World : Node2D
{
    [Export] public PackedScene CreatureScene;
    [Export] public PackedScene FoodScene;
    [Export] public int         FoodCount            = 8;
    [Export] public int         MaxCreatures         = 10;
    [Export] public Vector2     WorldHalfSize        = new Vector2(320f, 200f);
    [Export] public float       ReproductionDrive    = 0.75f;
    [Export] public float       ReproductionDistance = 60f;
    [Export] public float       ReproductionCooldown = 15f;

    private readonly List<Creature>              _creatures   = new();
    private readonly Dictionary<Creature, float> _repCooldown = new();
    private Node2D _foodContainer;
    private Label  _statusLabel;

    public override void _Ready()
    {
        _foodContainer = GetNodeOrNull<Node2D>("FoodContainer") ?? this;
        _statusLabel   = GetNodeOrNull<Label>("HUD/StatusLabel");

        foreach (var child in GetChildren())
            if (child is Creature c) RegisterCreature(c);

        SpawnFood();
        GD.Print($"[World] Bereit. {_creatures.Count} Wesen, Weltgroesse {WorldHalfSize * 2}");
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        UpdateCooldowns(dt);
        CheckReproduction();
        UpdateHUD();
        // In _Process(), temporär:
        foreach (var c in _creatures)
            if (c.Biochem.State.Get(ChemID.Glucose) < 0.2f)
                GD.Print($"[World] {c.Name} hat kritisch wenig Glucose: {c.Biochem.State.Get(ChemID.Glucose):F2}");
    }

    private void RegisterCreature(Creature c)
    {
        c.WorldHalfSize = WorldHalfSize;
        c.Name = $"Katze {_creatures.Count + 1}";
        _creatures.Add(c);
        c.RefreshNameLabel();
        _repCooldown[c] = 0f;
        GD.Print($"[World] Wesen registriert: {c.Name}");
    }

    private void SpawnFood()
    {
        if (FoodScene == null) { GD.PrintErr("[World] FoodScene nicht gesetzt!"); return; }
        for (int i = 0; i < FoodCount; i++) AddFood();
        GD.Print($"[World] {FoodCount} Nahrungsobjekte gespawnt.");
    }

    private void AddFood()
    {
        var food = FoodScene.Instantiate<Food>();
        food.SpawnArea = WorldHalfSize * 0.9f;
        _foodContainer.AddChild(food);
        food.GlobalPosition = RandomPos();
    }

    private void UpdateCooldowns(float dt)
    {
        foreach (var c in _creatures)
            if (_repCooldown.TryGetValue(c, out float cd) && cd > 0f)
                _repCooldown[c] = cd - dt;
    }

    private void CheckReproduction()
    {
        if (_creatures.Count >= MaxCreatures) return;
        for (int i = 0; i < _creatures.Count; i++)
            for (int j = i + 1; j < _creatures.Count; j++)
                if (CanReproduce(_creatures[i], _creatures[j]))
                {
                    Reproduce(_creatures[i], _creatures[j]);
                    return;
                }
    }

    private bool CanReproduce(Creature a, Creature b)
    {
        if (_repCooldown.GetValueOrDefault(a) > 0f) return false;
        if (_repCooldown.GetValueOrDefault(b) > 0f) return false;
        if (a.Biochem.State.Get(ChemID.SexDrive) < ReproductionDrive) return false;
        if (b.Biochem.State.Get(ChemID.SexDrive) < ReproductionDrive) return false;
        if (a.Biochem.State.Get(ChemID.IsAsleep) > 0.5f) return false;
        if (b.Biochem.State.Get(ChemID.IsAsleep) > 0.5f) return false;
        return a.GlobalPosition.DistanceTo(b.GlobalPosition) <= ReproductionDistance;
    }

    private void Reproduce(Creature a, Creature b)
    {
        if (CreatureScene == null) { GD.PrintErr("[World] CreatureScene fehlt!"); return; }

        var childGenome = a.CreateOffspringGenome(b);
        var child = CreatureScene.Instantiate<Creature>();
        AddChild(child);
        child.GlobalPosition = (a.GlobalPosition + b.GlobalPosition) / 2f;
        child.Initialize(childGenome);
        RegisterCreature(child);

        _repCooldown[a] = ReproductionCooldown;
        _repCooldown[b] = ReproductionCooldown;
        a.Biochem.State.Set(ChemID.SexDrive, 0f);
        b.Biochem.State.Set(ChemID.SexDrive, 0f);

        GD.Print($"[World] Geburt! {a.Name} + {b.Name} = Kind #{_creatures.Count}");
        GD.Print($"[World] Kind-Biochemie:\n{childGenome.BiochemDebugString()}");
    }

    private void UpdateHUD()
    {
        if (_statusLabel == null) return;
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Wesen: {_creatures.Count}/{MaxCreatures}");
        foreach (var c in _creatures)
        {
            if (!IsInstanceValid(c)) continue;
            sb.AppendLine($"\n[{c.Name}]");
            sb.AppendLine(c.Biochem.State.StatusLine());
            float cd = _repCooldown.GetValueOrDefault(c);
            if (cd > 0f) sb.AppendLine($"  Paarungs-CD: {cd:F0}s");
        }
        _statusLabel.Text = sb.ToString();
    }

    public override void _Draw()
    {
        DrawRect(new Rect2(-WorldHalfSize, WorldHalfSize * 2f),
                 new Color(0.3f, 0.6f, 0.3f, 0.4f), filled: false, width: 2f);
    }

    private Vector2 RandomPos() => new(
        (float)GD.RandRange(-WorldHalfSize.X * 0.9f, WorldHalfSize.X * 0.9f),
        (float)GD.RandRange(-WorldHalfSize.Y * 0.9f, WorldHalfSize.Y * 0.9f)
    );
}
