using System;
using Godot;

namespace Creatures;

/// <summary>
/// Das Genom eines Wesens – ein flaches float-Array.
///
/// Layout (Segmente):
///   [0 .. NeuralWeightCount-1]               = NN-Gewichte
///   [NeuralWeightCount .. +BiochemGeneCount]  = Biochemie-Parameter
///
/// Biochemie-Gene (der Reihe nach):
///   0: HungerRate
///   1: TirednessRate
///   2: SexDriveRate
///   3: SleepThreshold
///   4: WakeThreshold
/// </summary>
public class Genome
{
    public const int BiochemGeneCount = 5;

    public const int G_HungerRate     = 0;
    public const int G_TirednessRate  = 1;
    public const int G_SexDriveRate   = 2;
    public const int G_SleepThreshold = 3;
    public const int G_WakeThreshold  = 4;

    public float[] Genes        { get; private set; }
    public int     NeuralOffset  { get; }
    public int     BiochemOffset { get; }
    public int     TotalLength   { get; }

    public Genome(int neuralWeightCount)
    {
        NeuralOffset  = 0;
        BiochemOffset = neuralWeightCount;
        TotalLength   = neuralWeightCount + BiochemGeneCount;
        Genes         = new float[TotalLength];
    }

    // Erstellt ein zufaelliges Genom und initialisiert gleichzeitig das NN
    public void Randomize(NeuralNetwork nn, Random rng = null)
    {
        rng ??= new Random();

        nn.RandomizeWeights(rng);
        var nnWeights = nn.ExportWeights();
        Array.Copy(nnWeights, 0, Genes, NeuralOffset, nnWeights.Length);

        SetBiochem(G_HungerRate,     RandomAround(rng, 0.008f, 0.003f));
        SetBiochem(G_TirednessRate,  RandomAround(rng, 0.005f, 0.002f));
        SetBiochem(G_SexDriveRate,   RandomAround(rng, 0.004f, 0.002f));
        SetBiochem(G_SleepThreshold, RandomAround(rng, 0.85f,  0.05f));
        SetBiochem(G_WakeThreshold,  RandomAround(rng, 0.15f,  0.05f));
    }

    // Uniform Crossover: jedes Gen zufaellig von Elternteil A oder B
    public static Genome Crossover(Genome a, Genome b, Random rng = null)
    {
        rng ??= new Random();
        var child = new Genome(a.BiochemOffset);
        for (int i = 0; i < a.TotalLength; i++)
            child.Genes[i] = rng.NextDouble() > 0.5 ? a.Genes[i] : b.Genes[i];
        return child;
    }

    // Mutation: kleine zufaellige Veraenderung einzelner Gene
    public Genome Mutate(float mutationRate = 0.02f, float mutationStrength = 0.1f, Random rng = null)
    {
        rng ??= new Random();
        var child = Clone();
        for (int i = 0; i < child.TotalLength; i++)
        {
            if (rng.NextDouble() < mutationRate)
                child.Genes[i] += (float)(rng.NextDouble() * 2 - 1) * mutationStrength;
        }
        return child;
    }

    public Genome Clone()
    {
        var copy = new Genome(BiochemOffset);
        Array.Copy(Genes, copy.Genes, TotalLength);
        return copy;
    }

    // Schreibt NN-Gewichte aus dem Genom ins Netz
    public void ApplyToNetwork(NeuralNetwork nn)
    {
        var nnGenes = new float[nn.WeightCount];
        Array.Copy(Genes, NeuralOffset, nnGenes, 0, nn.WeightCount);
        nn.LoadWeights(nnGenes);
    }

    // Schreibt Biochemie-Gene in die Biochemie-Instanz
    public void ApplyToBiochem(Biochemistry biochem)
    {
        biochem.HungerRate     = Math.Clamp(GetBiochem(G_HungerRate),    0.001f, 0.05f);
        biochem.TirednessRate  = Math.Clamp(GetBiochem(G_TirednessRate),  0.001f, 0.03f);
        biochem.SexDriveRate   = Math.Clamp(GetBiochem(G_SexDriveRate),   0.001f, 0.03f);
        biochem.SleepThreshold = Math.Clamp(GetBiochem(G_SleepThreshold), 0.5f,   0.99f);
        biochem.WakeThreshold  = Math.Clamp(GetBiochem(G_WakeThreshold),  0.01f,  0.4f);
    }

    public float GetBiochem(int geneIndex)         => Genes[BiochemOffset + geneIndex];
    public void  SetBiochem(int geneIndex, float v) => Genes[BiochemOffset + geneIndex] = v;

    public string BiochemDebugString() =>
        $"  HungerRate:{GetBiochem(G_HungerRate):F4} " +
        $"TirednessRate:{GetBiochem(G_TirednessRate):F4} " +
        $"SexDriveRate:{GetBiochem(G_SexDriveRate):F4}\n" +
        $"  SleepThreshold:{GetBiochem(G_SleepThreshold):F3} " +
        $"WakeThreshold:{GetBiochem(G_WakeThreshold):F3}";

    private static float RandomAround(Random rng, float center, float spread)
        => center + (float)(rng.NextDouble() * 2 - 1) * spread;
}
