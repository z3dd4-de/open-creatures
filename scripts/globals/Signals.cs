using Godot;
namespace Creatures;

public partial class Signals : Node
{
    [Signal]
    public delegate void BiochemChangedEventHandler(string myString);

    public static Signals Instance { get; private set; }

    public override void _Ready()
    {
        Instance = this;
        GD.Print("[Signals] Signal-Handler bereit.");
    } 
}
