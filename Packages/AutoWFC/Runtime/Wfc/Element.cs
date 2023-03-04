using System.Collections;
using Newtonsoft.Json;
using AutoWfc.Runtime.Converters;
using AutoWfc.Runtime.Extensions;
using V = TypedArray<int>;
namespace AutoWfc.Wfc
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

                    SumWeights += w.Wfc.Patterns[i].NormalizedFrequency;
                    Entropy += w.Wfc.Patterns[i].Entropy;
                }
            }

            /**
             * Apply mask to a element.
             */
            public void Apply(Wave w, BitArray mask)
            {
                var diff = new BitArray(Coefficient).Xor(mask);
                if (diff.GetCardinality() == 0)
                {
                    // Mask is same as Coefficient, nothing to do.
                    return;
                }

                // Get Bit's that was invalidated by our mask
                diff.And(Coefficient);

                for (int i = 0; i < diff.Count; i++)
                {
                    if (!diff[i])
                    {
                        continue;
                    }

                    SumWeights -= w.Wfc.Patterns[i].NormalizedFrequency;
                    Entropy -= w.Wfc.Patterns[i].Entropy;
                }

                Coefficient.And(mask);
                Popcnt = Coefficient.GetCardinality();
            }

            public bool Collapse(int n, T value)
            {
                if (!Coefficient[n])
                {
                    return false;
                }

                Value = value;
                Coefficient.SetAll(false);
                Coefficient.Set(n, true);
                Popcnt = 1;
                SumWeights = 0.0d;
                Entropy = 0.0d;
                return true;
            }

            public bool Collapsed => Value is not null;

        }
    }
}