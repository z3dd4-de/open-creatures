using Godot;

namespace Creatures;

/// <summary>
/// Nahrungsobjekt (kleiner Kreis in der Szene).
/// Wird vom Wesen durch Überlappung gefressen.
/// Nach dem Fressen respawnt es an einer zufälligen Position.
/// </summary>
public partial class Food : Area2D
{
    [Export] public float NutritionValue = 0.6f;
    [Export] public Vector2 SpawnArea    = new Vector2(300f, 200f); // Hälfte der Ausdehnung

    // Wird aufgerufen wenn das Wesen die Nahrung berührt
    public void OnEaten()
    {
        GD.Print($"[Food] Nahrung gefressen bei {GlobalPosition}");
        Respawn();
    }

    private void Respawn()
    {
        GlobalPosition = new Vector2(
            (float)GD.RandRange(-SpawnArea.X, SpawnArea.X),
            (float)GD.RandRange(-SpawnArea.Y, SpawnArea.Y)
        );
        GD.Print($"[Food] Respawn bei {GlobalPosition}");
    }
}
