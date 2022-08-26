using System.Collections;
using System.Collections.Generic;
using System.Linq;
using V = TypedArray<int>;
namespace WFC
{
    public partial class WfcUtils<T>
    {
        
        public class Wave
        {
            public readonly WfcUtils<T> Wfc;

            public V SizeWave;

            public int SizeWaveLength => SizeWave.Value.Aggregate(((a, b) => a * b));

            public Element[] wave;

            public readonly T[] Preset;

            public int NumCollapsed = 0;

            public Wave(WfcUtils<T> wfc, V sizeWave, T[] preset)
            {
                Wfc = wfc;
                SizeWave = sizeWave;
                Preset = preset;
            }

            public V? Collapse(Element e, int? n = null)
            {
                n ??= e.Coefficient.FindFirstIndex(true);

                var p = this.Wfc.Patterns[n.Value];
                if (!e.Collapse(n.Value, p.Value))
                {
                    return e.Pos;
                }

                this.NumCollapsed++;
                return null;
            }

            public V? Observe(Element e)
            {
                var n = this.Wfc.PatternFn(this, e);
                return this.Collapse(e, n);
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

                        // ReSharper disable once CoVariantArrayConversion
                        if (Wfc.Neighbours.TryGet(wave, e.Pos, Wfc.Neighbours.Neighbours[i],
                                out var output))
                        {
                            var neighbourElement = output as Element;
                            if (neighbourElement!.Collapsed)
                            {
                                continue;
                            }

                            neighbors[i] = neighbourElement;
                        }
                    }

                    // Calculate superpatterns valid neighbours
                    var neighbourPatterns = Enumerable.Range(0, Wfc.Neighbours.Length)
                        .Select(_ => new BitArray(Wfc.Neighbours.Length)).ToArray();
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
                                this.Collapse(neighbourElement, 0);
                                contradiction = neighbourElement.Pos;
                            }
                            else if (newCount == 1)
                            {
                                var res = this.Collapse(neighbourElement);
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

                if (this.Wfc.OnPropagate is not null)
                {
                    this.Wfc.OnPropagate(this);
                }

                return null;
            }

            // Collapse the wave
            public TypedArray<int>? Collapse()
            {
                // Initialize the wave, all patterns are valid for each element
                this.wave = new Element[this.SizeWaveLength];

                for (int i = 0; i < this.wave.Length; i++)
                {
                    wave[i] = new Element(this, this.Wfc.MaskUsed);
                    wave[i].Pos = ArrayUtils.UnRavelIndex(SizeWave, i);
                }

                // Load preset value if present
                if (Preset is not null)
                {
                    for (int i = 0; i < Preset.Length; i++)
                    {
                        if (Preset[i] is not null)
                        {
                            wave[i].Value = Preset[i];
                        }
                    }
                }

                while (NumCollapsed != this.wave.Length)
                {
                    var e = Wfc.NextCellFn(this);

                    var observeError = Observe(e);
                    if (observeError is not null)
                    {
                        return observeError;
                    }

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
}