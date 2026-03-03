using System.Collections.Generic;
using System.Text;
using Godot;

namespace Creatures;

/// <summary>
/// Hält alle Chemikalien-Konzentrationen (0.0 – 1.0).
/// Reines Daten-Objekt, keine Logik.
/// </summary>
public class BiochemState
{
    private readonly Dictionary<ChemID, float> _chems = new();

    public float Get(ChemID id)
        => _chems.TryGetValue(id, out float v) ? v : 0f;

    public void Set(ChemID id, float value)
        => _chems[id] = System.Math.Clamp(value, 0f, 1f);

    public void Add(ChemID id, float delta)
        => Set(id, Get(id) + delta);

    /// <summary>Übersichtliche Debug-Ausgabe aller Chemikalien.</summary>
    public string DebugString()
    {
        var sb = new StringBuilder();
        foreach (var kv in _chems)
            sb.Append($"  {kv.Key,-14}: {kv.Value:F3}\n");
        return sb.ToString();
    }

    /// <summary>Gibt einen kurzen Status-String zurück (für HUD).</summary>
    public string StatusLine()
    {
        return $"Hunger:{Get(ChemID.Hunger):F2} " +
               $"Tired:{Get(ChemID.Tiredness):F2} " +
               $"Sex:{Get(ChemID.SexDrive):F2} " +
               $"Glucose:{Get(ChemID.Glucose):F2} " +
               $"Sleep:{(Get(ChemID.IsAsleep) > 0.5f ? "ZZZ" : "awake")}";
    }
}
