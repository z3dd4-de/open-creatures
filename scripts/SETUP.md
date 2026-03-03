# Creatures – Biochemie Testszene

## Dateien

| Datei | Zweck |
|---|---|
| `ChemID.cs` | Enum aller Chemikalien |
| `BiochemState.cs` | Datenhaltung (Konzentrationen) |
| `ChemicalReaction.cs` | Reaktionslogik (Massenwirkungsgesetz) |
| `Biochemistry.cs` | Haupt-Engine: Triebe, Schlaf, Metabolismus |
| `Creature.cs` | Godot-Node des Wesens |
| `Food.cs` | Nahrungsobjekt |

Alle Dateien in denselben Ordner deines Godot-Projekts legen (z.B. `res://src/creatures/`).

---

## Szenen-Setup in Godot

### 1. Food-Szene (`Food.tscn`)

```
Area2D  ← Script: Food.cs
  ├── Sprite2D / ColorRect  (kleiner Kreis, z.B. grüne Farbe)
  └── CollisionShape2D      (CircleShape2D, Radius 8)
```

**Wichtig:** Im Inspector von Area2D:
- `Monitoring = true`
- `Monitorable = true`

---

### 2. Creature-Szene (`Creature.tscn`)

```
CharacterBody2D  ← Script: Creature.cs
  ├── Sprite2D / ColorRect  (Rechteck, z.B. blaue Farbe)
  ├── CollisionShape2D      (RectangleShape2D, z.B. 32×32)
  ├── Label (Name: "StatusLabel")   ← optional, zeigt Live-Status
  └── Area2D (Name: "DetectionArea")  ← erkennt Nahrung
        └── CollisionShape2D (CircleShape2D, Radius 40)
```

**Wichtig:** Im Inspector der DetectionArea:
- `Monitoring = true`
- `Monitorable = false`

---

### 3. Hauptszene (`Main.tscn`)

```
Node2D
  ├── Creature   (instanziiert)
  ├── Food       (3–5 Instanzen verteilt)
  └── Camera2D   (optional)
```

---

## Was du im Output-Log siehst

Alle 3 Sekunden erscheint ein Block wie:

```
[Biochem]
  Glucose       : 0.721
  NutrientStore : 0.489
  Hunger        : 0.134
  Tiredness     : 0.312
  SexDrive      : 0.087
  IsAsleep      : 0.000
  Reward        : 0.000
  Punishment    : 0.000
```

Beim Fressen:
```
[Biochem] Gegessen! Glucose: 0.89, Hunger: 0.05
[Food] Respawn bei (123, -87)
```

Beim Einschlafen/Aufwachen:
```
[Biochem] Das Wesen schläft ein (Tiredness: 0.85)
[Biochem] Das Wesen wacht auf.
```

---

## Nächste Erweiterungsschritte

1. **Mehrere Wesen** – Szene mehrfach instanziieren
2. **Fortpflanzung** – wenn `SexDrive > 0.8` und zwei Wesen sich berühren
3. **Genom** – die Raten (`HungerRate`, `TirednessRate` etc.) ins Genom auslagern
4. **Nervensystem** – Input-Neuronen aus `BiochemState` befüllen, Outputs als Aktionen

---

## Biochemie-Übersicht

```
Zeit ──► Tiredness (steigt immer wenn wach)
Zeit ──► Hunger (steigt wenn Glucose sinkt)
         │
         ▼
Glucose sinkt ──► NutrientStore wird mobilisiert
                  │
                  ▼
             Glucose steigt wieder (Notreserve)

Tiredness > 0.85 ──► IsAsleep = 1
IsAsleep = 1     ──► Tiredness fällt schnell
Tiredness < 0.15 ──► IsAsleep = 0

Glucose hoch + nicht müde + wach ──► SexDrive steigt
```
