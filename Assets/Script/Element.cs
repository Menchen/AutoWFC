using System;
using System.Collections;
using Newtonsoft.Json;
using Script.Converters;
using V = TypedArray<int>;
namespace WFC
{
    public partial class WfcUtils<T>
    {
        public class Element
        {
            public V Pos { get; set; }
            [JsonConverter(typeof(BitArrayConverter))]
            public BitArray Coefficient { get; set; }

            public int Popcnt { get; set; }

            public T Value;

            public double SumWeights { get; set; }
            public double SumWeightsLogWeights { get; set; }
            public double Entropy { get; set; }

            // public Element(Wave w, BitArray mask)
            // {
            //     Coefficient = new BitArray(mask);
            //     Popcnt = mask.GetCardinality();
            //     for (int i = 0; i < Coefficient.Count; i++)
            //     {
            //         if (!Coefficient[i])
            //         {
            //             continue;
            //         }
            //
            //         var weight = w.Wfc.Patterns[i].Frequency;
            //         this.SumWeights += weight;
            //         this.SumWeightsLogWeights += weight * Math.Log(weight);
            //     }
            // }
            
            public void Initialize(Wave w, BitArray mask)
            {
                Coefficient = new BitArray(mask);
                Popcnt = mask.GetCardinality();
                for (int i = 0; i < Coefficient.Count; i++)
                {
                    if (!Coefficient[i])
                    {
                        continue;
                    }

                    var weight = w.Wfc.Patterns[i].Frequency;
                    this.SumWeights += weight;
                    this.SumWeightsLogWeights += weight * Math.Log(weight);
                }
            }

            public bool Apply(Wave w, BitArray mask)
            {
                var diff = new BitArray(this.Coefficient).And(mask);
                if (diff.GetCardinality() == 0)
                {
                    return true;
                }

                this.Coefficient.And(mask);

                for (int i = 0; i < Coefficient.Count; i++)
                {
                    if (!Coefficient[i])
                    {
                        continue;
                    }

                    var weight = w.Wfc.Patterns[i].Frequency;
                    this.SumWeights -= weight;
                    this.SumWeightsLogWeights -= weight * Math.Log(weight);
                }

                // this.Entropy = Math.Log(this.SumWeights) - (this.SumWeightsLogWeights / this.SumWeights);
                this.Popcnt = this.Coefficient.GetCardinality();
                this.Entropy = Popcnt;
                return this.Popcnt != 0;
            }

            public bool Collapse(int n, T value)
            {
                if (!this.Coefficient[n])
                {
                    return false;
                }

                this.Value = value;
                this.Coefficient.SetAll(false);
                this.Coefficient.Set(n, true);
                this.Popcnt = 1;
                this.Entropy = 0.0d;
                this.SumWeights = 0.0d;
                this.SumWeightsLogWeights = 0.0d;
                return true;
            }

            public bool Collapsed => this.Value is not null;

        }
    }
}