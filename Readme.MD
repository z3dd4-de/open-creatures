# CreatureSim – Biochemie-Modul

## Dateien
| Datei | Zweck |
|---|---|
| `ChemicalIds.cs` | Konstanten für alle Chemikalien + Namen |
| `Biochemistry.cs` | Kernlogik: Zerfall, Hunger, Schlaf, Sexualtrieb |
| `Food.cs` | Nahrungsobjekt (Area2D + DrawCircleNode) |
| `Creature.cs` | Der Agent (CharacterBody2D mit Biochemie) |
| `World.cs` | Hauptszene, spawnt Creature + Nahrung |

## Godot-Setup (minimale Schritte)

### 1. Namespace / Projekt
Alle Dateien in dein Godot-Projekt kopieren (z.B. `res://Scripts/`).  
Das Projekt braucht eine `*.csproj`-Datei – leg ein C#-Projekt an.

### 2. Szene aufbauen
```
World (Node2D)
  └── Camera2D (zentriert auf 0,0)
```
- World.cs als Script an `World` hängen
- Die Szene als Hauptszene setzen

Die Creature- und Food-Nodes werden **zur Laufzeit** vom World-Script erstellt
(kein PackedScene nötig für den Start).

### 3. Falls du PackedScenes verwenden willst
- Neue Szene: `CharacterBody2D` → Script `Creature.cs`
- Neue Szene: `Area2D` → Script `Food.cs`  
- Beide Szenen in die Export-Felder von World ziehen

### 4. Starten & beobachten
Ausgabe im Godot-Output-Tab (alle 3 Sekunden):
```
── Biochemie t=3.0s ──
  Glucose      [################....] 0.812
  Fat          [######..............] 0.298
  Hunger       [####................] 0.188
  Tiredness    [##..................] 0.087
  SexDrive     [....................] 0.000
  Awake        [####################] 1.000
  Age          [....................] 0.005
  Pain         [....................] 0.000
  Reward       [....................] 0.000
  Zustand: 🚶 WANDERT
```

## Biochemie-Logik

```
Glucose  ──sinkt mit Zeit──→  treibt Hunger an
                               Creature sucht Nahrung
                               Nahrung essen → +Glucose +Reward

Tiredness ──steigt immer──→  ab 0.9: einschlafen (Awake=0)
                               während Schlaf: Tiredness sinkt
                               ab 0.1: aufwachen (Awake=1)

Age ──steigt mit Zeit──→  ab 0.15: SexDrive beginnt zu steigen
                               (später: Paarungsverhalten)

Fat ──Puffer──→  wird aus überschüssiger Glucose aufgebaut
                  wird mobilisiert wenn Glucose sehr niedrig
```

## Nächste Schritte
1. **Nervensystem** – einfaches Feed-Forward-NN, Input = Chemikalien, Output = Aktionen
2. **Genom** – NN-Gewichte im Genom kodieren, Mutation + Crossover
3. **Fortpflanzung** – zwei Wesen paaren sich wenn SexDrive hoch
4. **Mehr Reize** – Schmerz durch andere Wesen, Temperatur der Umgebung