﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AutoWfc.Extensions;
using AutoWfc.GenericUtils;
using V = AutoWfc.GenericUtils.TypedArray<int>;

namespace AutoWfc.Wfc
{
    public partial class WfcUtils<T>
    {
        public class Wave
        {
            public readonly WfcUtils<T> Wfc;

            public V SizeWave;

            public int SizeWaveLength => SizeWave.Value.Aggregate(((a, b) => a * b));

            public Element[] CurrentWave;

            public readonly T[] Preset;

            public int NumCollapsed;

            public Wave(WfcUtils<T> wfc, V sizeWave, T[] preset)
            {
                var sizeWaveLength = sizeWave.Value.Aggregate((acc, value) => acc * value);
                if (preset is not null && sizeWaveLength != preset.Length)
                {
                    throw new ArgumentException($"SizeWave{sizeWaveLength} & preset{preset.Length} is not same length",
                        nameof(preset));
                }

                Wfc = wfc;
                SizeWave = sizeWave;
                Preset = preset;
            }

            public V? Collapse(Element e, int? n = null)
            {
                if (e.Popcnt <= 0 )
                {
                    throw new ZeroElementCoefficientException();
                }
                n ??= e.Popcnt <= 1
                    ? e.Coefficient.FindFirstIndex()
                    : e.Coefficient.FindRandomIndex(e.Popcnt, Wfc.Rng);

                var p = Wfc.Patterns[n.Value];
                if (!e.Collapse(n.Value, p.Value))
                {
                    Wfc.Logger?.Invoke($"Failed to collapse {string.Join(", ",e.Pos)}");
                    return e.Pos;
                }

                NumCollapsed++;
                return null;
            }

            public V? Observe(Element e)
            {
                var n = Wfc.PatternFn(this, e);
                if (n < 0)
                {
                    Wfc.Logger?.Invoke($"Failed to observe {string.Join(", ",e.Pos)}");
                    return e.Pos;
                }

                return Collapse(e, n);
            }

            public V? Propagate(Element toPropagate)
            {
                Stack<Element> es = new Stack<Element>();
                es.Push(toPropagate);

                V? contradiction = null;

                while (es.Count > 0)
                {
                    var e = es.Pop();

                    Element[] neighbors = new Element[Wfc.Neighbours.Length];

                    for (int i = 0; i < Wfc.Neighbours.Length; i++)
                    {
                        neighbors[i] = null;

                        var offset = Wfc.Neighbours.Neighbours[i].Zip(e.Pos.Value, (a, b) => a + b).ToArray();
                        var outputIndex = ArrayUtils.InBounds(SizeWave, offset)
                            ? ArrayUtils.RavelIndex(SizeWave, offset)
                            : null;
                        if (outputIndex is not null)
                        {
                            var neighbourElement = CurrentWave[outputIndex.Value];
                            if (neighbourElement.Collapsed)
                            {
                                continue;
                            }

                            neighbors[i] = neighbourElement;
                        }
                    }

                    // Calculate superpatterns valid neighbours
                    var neighbourPatterns = Enumerable.Range(0, Wfc.Neighbours.Length)
                        .Select(_ => new BitArray(Wfc.BITSetSize)).ToArray();
                    for (int i = 0; i < e.Coefficient.Length; i++)
                    {
                        if (!e.Coefficient[i])
                        {
                            continue;
                        }

                        var pattern = Wfc.Patterns[i];
                        for (int j = 0; j < Wfc.Neighbours.Length; j++)
                        {
                            if (neighbors[j] is not null)
                            {
                                neighbourPatterns[j].Or(pattern.Valid[j]);
                            }
                        }
                    }

                    // apply superpatterns
                    for (int i = 0; i < Wfc.Neighbours.Length; i++)
                    {
                        if (neighbors[i] is null)
                        {
                            continue;
                        }

                        var neighbourElement = neighbors[i];
                        var oldCount = neighbourElement.Popcnt;
                        neighbourElement.Apply(this, neighbourPatterns[i]);

                        var newCount = neighbourElement.Popcnt;
                        if (newCount != oldCount)
                        {
                            if (newCount == 0)
                            {
                                // zero popcount = contradiction
                                Collapse(neighbourElement, 0);
                                contradiction = neighbourElement.Pos;
                                Wfc.Logger?.Invoke($"Failed to converge (0 state) at {string.Join(", ",contradiction)}");
                            }
                            else if (newCount == 1)
                            {
                                var res = Collapse(neighbourElement);
                                if (res is not null)
                                {
                                    return res;
                                }
                            }

                            // propagate changed value
                            es.Push(neighbourElement);
                        }
                    }
                }

                if (contradiction is not null)
                {
                    return contradiction;
                }

                Wfc.OnPropagate?.Invoke(this);

                return null;
            }

            // Collapse the wave
            public TypedArray<int>? Collapse()
            {
                // Initialize the wave, all patterns are valid for each element
                CurrentWave = new Element[SizeWaveLength];

                for (int i = 0; i < CurrentWave.Length; i++)
                {
                    // wave[i] = new Element(this, this.Wfc.MaskUsed);
                    CurrentWave[i] = new Element
                    {
                        Pos = ArrayUtils.UnRavelIndex(SizeWave, i)
                    };
                    CurrentWave[i].Initialize(this, Wfc.MaskUsed);
                }

                // Load preset value if present
                if (Preset is not null)
                {
                    // Use shuffled index to introduce randomness
                    var shuffledIndex = Enumerable.Range(0, Preset.Length).ToList();
                    shuffledIndex.ShuffleInPlace(Wfc.Rng);

                    foreach (var i in shuffledIndex)
                    {
                        if (Equals(Preset[i], Wfc.EmptyState) || Preset[i] is null || CurrentWave[i].Collapsed)
                            continue;
                        if (!Wfc.PatternLookUp.TryGetValue(Preset[i],out var patternId))
                        {
                            Wfc.Logger($"Tile [{Preset[i]}] not found in pattern, try learning it before WFC?");
                            throw new InvalidOperationException($"Tile {Preset[i]} found in preset but not in Pattern.");
                        }

                        // Checking if patternId is still valid for current element because it can be invalidated by neighbours propagation
                        if (!CurrentWave[i].Coefficient[patternId])
                        {
                            continue;
                        }
                        var err = Collapse(CurrentWave[i], patternId);
                        if (err is null)
                        {
                            Propagate(CurrentWave[i]);
                        }
                    }
                }

                while (NumCollapsed < CurrentWave.Length)
                {
                    var e = Wfc.NextCellFn(this);

                    // Observe/Collapse local node
                    var observeError = Observe(e);
                    if (observeError is not null)
                    {
                        return observeError;
                    }

                    // Propagate constrains to neighbours
                    var propagateError = Propagate(e);
                    if (propagateError is not null)
                    {
                        return propagateError;
                    }
                }

                return null;
            }
        }
    }

    public class ZeroElementCoefficientException : Exception
    {
        
    }
    
}