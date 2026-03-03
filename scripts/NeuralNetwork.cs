using System;
using Godot;

namespace Creatures;

/// <summary>
/// Einfaches Feed-Forward-Netz mit einer Hidden-Layer.
/// Alle Gewichte liegen in einem flachen float-Array → direkt aus Genom lesbar.
///
/// Topologie (konfigurierbar):
///   InputCount  → HiddenCount → OutputCount
///
/// Gewichts-Layout im Array (row-major):
///   [0 .. InputCount*HiddenCount - 1]               = W1 (Input→Hidden)
///   [InputCount*HiddenCount .. +HiddenCount]        = B1 (Bias Hidden)
///   [.. +HiddenCount*OutputCount]                   = W2 (Hidden→Output)
///   [.. +OutputCount]                               = B2 (Bias Output)
/// </summary>
public class NeuralNetwork
{
    public int InputCount  { get; }
    public int HiddenCount { get; }
    public int OutputCount { get; }

    /// <summary>Gesamtanzahl Gewichte – genau so viele Gene braucht das Genom.</summary>
    public int WeightCount => InputCount * HiddenCount + HiddenCount
                            + HiddenCount * OutputCount + OutputCount;

    // Gewichts-Matrizen (row = Ziel-Neuron, col = Quell-Neuron)
    private readonly float[,] _w1; // [hidden, input]
    private readonly float[]  _b1; // [hidden]
    private readonly float[,] _w2; // [output, hidden]
    private readonly float[]  _b2; // [output]

    // Letzter Hidden-State (für späteres Hebbian-Learning)
    public float[] LastHidden { get; private set; }
    public float[] LastOutput { get; private set; }

    // ── Konstruktoren ─────────────────────────────────────────────────────────

    public NeuralNetwork(int inputCount, int hiddenCount, int outputCount)
    {
        InputCount  = inputCount;
        HiddenCount = hiddenCount;
        OutputCount = outputCount;

        _w1 = new float[hiddenCount, inputCount];
        _b1 = new float[hiddenCount];
        _w2 = new float[outputCount, hiddenCount];
        _b2 = new float[outputCount];

        LastHidden = new float[hiddenCount];
        LastOutput = new float[outputCount];
    }

    // ── Gewichte laden ────────────────────────────────────────────────────────

    /// <summary>
    /// Lädt alle Gewichte aus einem flachen Array (z.B. direkt aus dem Genom).
    /// </summary>
    public void LoadWeights(float[] genes)
    {
        if (genes.Length != WeightCount)
            throw new ArgumentException($"Erwarte {WeightCount} Gene, bekam {genes.Length}");

        int i = 0;

        for (int h = 0; h < HiddenCount; h++)
            for (int inp = 0; inp < InputCount; inp++)
                _w1[h, inp] = genes[i++];

        for (int h = 0; h < HiddenCount; h++)
            _b1[h] = genes[i++];

        for (int o = 0; o < OutputCount; o++)
            for (int h = 0; h < HiddenCount; h++)
                _w2[o, h] = genes[i++];

        for (int o = 0; o < OutputCount; o++)
            _b2[o] = genes[i++];
    }

    /// <summary>Initialisiert alle Gewichte zufällig (Xavier-Initialisierung).</summary>
    public void RandomizeWeights(Random rng = null)
    {
        rng ??= new Random();
        float scale1 = MathF.Sqrt(2f / InputCount);
        float scale2 = MathF.Sqrt(2f / HiddenCount);

        for (int h = 0; h < HiddenCount; h++)
        {
            for (int inp = 0; inp < InputCount; inp++)
                _w1[h, inp] = (float)(rng.NextDouble() * 2 - 1) * scale1;
            _b1[h] = 0f;
        }

        for (int o = 0; o < OutputCount; o++)
        {
            for (int h = 0; h < HiddenCount; h++)
                _w2[o, h] = (float)(rng.NextDouble() * 2 - 1) * scale2;
            _b2[o] = 0f;
        }
    }

    /// <summary>Exportiert alle Gewichte in ein flaches Array (für Genom-Speicherung).</summary>
    public float[] ExportWeights()
    {
        var genes = new float[WeightCount];
        int i = 0;

        for (int h = 0; h < HiddenCount; h++)
            for (int inp = 0; inp < InputCount; inp++)
                genes[i++] = _w1[h, inp];

        for (int h = 0; h < HiddenCount; h++)
            genes[i++] = _b1[h];

        for (int o = 0; o < OutputCount; o++)
            for (int h = 0; h < HiddenCount; h++)
                genes[i++] = _w2[o, h];

        for (int o = 0; o < OutputCount; o++)
            genes[i++] = _b2[o];

        return genes;
    }

    // ── Forward Pass ──────────────────────────────────────────────────────────

    public float[] Forward(float[] inputs)
    {
        if (inputs.Length != InputCount)
            throw new ArgumentException($"Erwarte {InputCount} Inputs, bekam {inputs.Length}");

        // Hidden Layer: ReLU
        for (int h = 0; h < HiddenCount; h++)
        {
            float sum = _b1[h];
            for (int inp = 0; inp < InputCount; inp++)
                sum += _w1[h, inp] * inputs[inp];
            LastHidden[h] = MathF.Max(0f, sum); // ReLU
        }

        // Output Layer: Sigmoid (0..1, passt gut für Aktivierungsniveaus)
        for (int o = 0; o < OutputCount; o++)
        {
            float sum = _b2[o];
            for (int h = 0; h < HiddenCount; h++)
                sum += _w2[o, h] * LastHidden[h];
            LastOutput[o] = Sigmoid(sum);
        }

        return LastOutput;
    }

    private static float Sigmoid(float x) => 1f / (1f + MathF.Exp(-x));

    // ── Reinforcement ─────────────────────────────────────────────────────────

    /// <summary>
    /// Verstärkt/schwächt die Verbindungen zur zuletzt gewählten Aktion.
    /// signal > 0 = Reward (Aktion wahrscheinlicher machen)
    /// signal < 0 = Punishment (Aktion unwahrscheinlicher machen)
    /// </summary>
    public void ReinforceLastAction(int actionIndex, float signal)
    {
        if (actionIndex < 0 || actionIndex >= OutputCount) return;

        // Gewichte Hidden→Output für diese Aktion anpassen
        for (int h = 0; h < HiddenCount; h++)
            _w2[actionIndex, h] = Math.Clamp(
                _w2[actionIndex, h] + signal * LastHidden[h],
                -3f, 3f  // Gewichte begrenzen damit sie nicht explodieren
            );

        _b2[actionIndex] = Math.Clamp(_b2[actionIndex] + signal * 0.1f, -3f, 3f);
    }
}