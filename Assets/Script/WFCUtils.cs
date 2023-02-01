/*
 * Created by Mengchen
 * Inspired with C++ version by jdah
 * https://gist.github.com/jdah/ad997b858513a278426f8d91317115b9
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Script.Converters;
using Script.GenericUtils;
using Unity.VisualScripting.Dependencies.NCalc;
using UnityEngine;
using Random = System.Random;
using V = TypedArray<int>;


namespace WFC
{
    public partial class WfcUtils<T>
    {
        [JsonProperty] public int Dimension { get; private set; }

        // [JsonProperty] public int PatternSize { get; private set; }
        [JsonProperty] public int BITSetSize { get; private set; }

        // public int Vol => Convert.ToInt32(Math.Pow(PatternSize, Dimension));

        public List<Pattern> Patterns;

        [JsonConverter(typeof(BitArrayConverter))]
        public BitArray MaskUsed; // Default Mask for enabled pattern

        [JsonIgnore] public Random Random;


        [JsonIgnore] public BorderBehavior BorderBehavior;

        public SelectPattern.SelectPatternEnum SelectPatternEnum;
        public NextCell.NextCellEnum NextCellEnum;

        [JsonIgnore] public Func<Wave, Element, int> PatternFn;

        [JsonIgnore] public Func<Wave, Element> NextCellFn;

        [JsonIgnore] public Action<Wave> OnPropagate;

        public readonly T EmptyState;

        public INeibours Neighbours;

        [JsonIgnore] public Dictionary<T, int> PatternLookUp;

        // [JsonProperty] public int[] PatternSizeVec { get; private set; }

        public static WfcUtils<T> BuildFromJson(string json)
        {
            var wfcUtils = JsonConvert.DeserializeObject<WfcUtils<T>>(json, new BitArrayConverter());
            wfcUtils.NextCellFn ??= NextCell.GetNextCellFn(wfcUtils.NextCellEnum);
            wfcUtils.PatternFn ??= SelectPattern.GetPatternFn(wfcUtils.SelectPatternEnum);
            wfcUtils.Random = new Random();
            wfcUtils.PatternLookUp = wfcUtils.Patterns.ToDictionary(e => e.Value, e => e.Id);
            return wfcUtils;
        }

        [JsonConstructor]
        public WfcUtils()
        {
        }

        public string SerializeToJson()
        {
            return JsonConvert.SerializeObject(this, new BitArrayConverter());
        }

        public WfcUtils(int dimension, V sizeInput, T[] input,
            BorderBehavior borderBehavior, Random random,
            INeibours neighbours, T emptyState, NextCell.NextCellEnum nextCellEnum,
            SelectPattern.SelectPatternEnum selectPatternEnum)
        {
            PatternFn = SelectPattern.GetPatternFn(selectPatternEnum);
            NextCellFn = NextCell.GetNextCellFn(nextCellEnum);
            NextCellEnum = nextCellEnum;
            SelectPatternEnum = selectPatternEnum;
            Dimension = dimension;
            // PatternSize = patternSize;
            BorderBehavior = borderBehavior;
            Random = random;
            Neighbours = neighbours;
            EmptyState = emptyState;

            // Get pattern data from input 
            var sizeInputLength = sizeInput.Value.Aggregate((a, b) => a * b);


            var lookupTable = new Dictionary<T, HashSet<T>[]>();

            for (int i = 0; i < sizeInputLength; i++)
            {
                var index = ArrayUtils.UnRavelIndex(sizeInput, i);
                var centerValue = input[i];
                if (centerValue is null)
                {
                    continue;
                }

                var listArray = lookupTable.GetValueOrDefault(centerValue) ??
                                Enumerable.Range(0, Neighbours.Length).Select(_ => new HashSet<T>()).ToArray();
                for (int j = 0; j < Neighbours.Length; j++)
                {
                    var offset = index!.Zip(Neighbours.Neighbours[j], (a, b) => a + b).ToArray();
                    if (ArrayUtils.InBounds(sizeInput, offset))
                    {
                        var pos = ArrayUtils.RavelIndex(sizeInput, offset)!.Value;
                        var value = input[pos];

                        listArray[j].Add(value);
                    }
                }

                lookupTable[centerValue] = listArray;
            }
            
            GeneratePatternFromLookUpTable(lookupTable);
        }

        public void LearnNewPattern(V sizeInput, T[] input)
        {
            // Get pattern data from input 
            var sizeInputLength = sizeInput.Value.Aggregate((a, b) => a * b);

            // Dictionary of Valid neighbours in each direction/neibours with HashSet as we don't have Id yet.
            // var lookupTable = new Dictionary<T, HashSet<T>[]>();
            var lookupTable = Patterns.ToDictionary(e => e.Value, e => e.Valid.Select(b =>
            {
                return b.IterateWithIndex().Where(tuple => tuple.Item1).Select(tuple => Patterns[tuple.Item2].Value)
                    .ToHashSet();
            }).ToArray());

            for (int i = 0; i < sizeInputLength; i++)
            {
                var index = ArrayUtils.UnRavelIndex(sizeInput, i);
                var centerValue = input[i];
                if (centerValue is null)
                {
                    continue;
                }

                var listArray = lookupTable.GetValueOrDefault(centerValue) ??
                                Enumerable.Range(0, Neighbours.Length).Select(_ => new HashSet<T>()).ToArray();
                for (int j = 0; j < Neighbours.Length; j++)
                {
                    var offset = index!.Zip(Neighbours.Neighbours[j], (a, b) => a + b).ToArray();
                    if (ArrayUtils.InBounds(sizeInput, offset))
                    {
                        var pos = ArrayUtils.RavelIndex(sizeInput, offset)!.Value;
                        var value = input[pos];

                        listArray[j].Add(value);
                    }
                }

                lookupTable[centerValue] = listArray;
            }
            GeneratePatternFromLookUpTable(lookupTable);
        }
        
        public void UnLearnPattern(V sizeInput, T[] input)
        {
            // Get pattern data from input 
            var sizeInputLength = sizeInput.Value.Aggregate((a, b) => a * b);

            // Dictionary of Valid neighbours in each direction/neibours with HashSet as we don't have Id yet.
            // var lookupTable = new Dictionary<T, HashSet<T>[]>();
            var lookupTable = Patterns.ToDictionary(e => e.Value, e => e.Valid.Select(b =>
            {
                return b.IterateWithIndex().Where(tuple => tuple.Item1).Select(tuple => Patterns[tuple.Item2].Value)
                    .ToHashSet();
            }).ToArray());

            for (int i = 0; i < sizeInputLength; i++)
            {
                var index = ArrayUtils.UnRavelIndex(sizeInput, i);
                var centerValue = input[i];
                if (centerValue is null)
                {
                    continue;
                }

                var listArray = lookupTable.GetValueOrDefault(centerValue);
                if (listArray is null)
                {
                    continue;
                }
                for (int j = 0; j < Neighbours.Length; j++)
                {
                    var offset = index!.Zip(Neighbours.Neighbours[j], (a, b) => a + b).ToArray();
                    if (ArrayUtils.InBounds(sizeInput, offset))
                    {
                        var pos = ArrayUtils.RavelIndex(sizeInput, offset)!.Value;
                        var value = input[pos];

                        listArray[j].Remove(value);
                    }
                }

                lookupTable[centerValue] = listArray;
            }

            GeneratePatternFromLookUpTable(lookupTable);

        }

        private void GeneratePatternFromLookUpTable(Dictionary<T,HashSet<T>[]>  lookupTable)
        {
            // Populate Patterns Ids
            var id = 0;
            var patternsDict = new Dictionary<T, Pattern>();
            MaskUsed = new BitArray(lookupTable.Count);
            BITSetSize = lookupTable.Count;
            foreach (var kvPair in lookupTable)
            {
                patternsDict[kvPair.Key] = new Pattern
                {
                    Id = id,
                    Value = kvPair.Key,
                    Valid = Enumerable.Range(0, Neighbours.Length).Select(_ => new BitArray(BITSetSize)).ToArray(),
                };

                MaskUsed.Set(id, true);
                id++;
            }

            // Assign valid bits
            foreach (var kvPair in lookupTable)
            {
                var myself = patternsDict[kvPair.Key];

                for (int i = 0; i < Neighbours.Length; i++)
                {
                    foreach (var neighbourValue in kvPair.Value[i])
                    {
                        var neighboursId = patternsDict[neighbourValue].Id;
                        myself.Valid[i].Set(neighboursId, true);
                    }
                }
            }


            Patterns = patternsDict.Select(e => e.Value).ToList();
            PatternLookUp = Patterns.ToDictionary(e => e.Value, e => e.Id);
        }

        public bool Collapse(V sizeOut, out T[] output, T[] preset = null)
        {
            var pattern = Patterns ?? throw new NullReferenceException("Missing Pattern");
            var nextCellFn = NextCellFn ?? throw new NullReferenceException("Missing NextCellFn");
            var selectPattern = PatternFn ?? throw new NullReferenceException("Missing PatternFn");
            var w = new Wave(this, sizeOut, preset);
            var res = w.Collapse();
            output = null;
            if (res is not null)
            {
                return false;
            }

            output = w.CurrentWave.Select(e => e.Value).ToArray();
            return true;
        }

        /**
         * Create Array of T with size Vol, A.K.A PatternSize ** N 
         * With neighbours from Input based with BorderBehavior
         */
        // private T[] DataAt(V center)
        // {
        //     var northWest = center.Value.Select(e => e - (PatternSize / 2)).ToArray();
        //
        //     if (BorderBehavior == BorderBehavior.Exclude)
        //     {
        //         var southEast = northWest.Select(e => e + PatternSize - 1).ToArray();
        //         if (!ArrayUtils.InBounds(SizeInput, northWest) || !ArrayUtils.InBounds(SizeInput, southEast))
        //         {
        //             return null;
        //         }
        //     }
        //
        //     var dst = new T[Vol];
        //     for (int i = 0; i < Vol; i++)
        //     {
        //         var offset = ArrayUtils.UnRavelIndex(PatternSizeVec, i);
        //         var pos = northWest.Zip(offset, (a, b) => a + b).ToArray();
        //         switch (BorderBehavior)
        //         {
        //             case BorderBehavior.Exclude:
        //                 dst[i] = Input[ArrayUtils.RavelIndex(SizeInput, pos)!.Value];
        //                 break;
        //             case BorderBehavior.Zero:
        //                 if (!ArrayUtils.InBounds(SizeInput, pos))
        //                 {
        //                     dst[i] = EmptyState;
        //                 }
        //
        //                 break;
        //             case BorderBehavior.Clamp:
        //                 dst[i] = Input[
        //                     ArrayUtils.RavelIndex(SizeInput,
        //                         pos.Zip(SizeInput.Value, (v, s) => Math.Clamp(v, 0, s - 1)).ToArray())!.Value];
        //                 break;
        //             case BorderBehavior.Wrap:
        //                 dst[i] = Input[
        //                     ArrayUtils.RavelIndex(SizeInput,
        //                         pos.Zip(SizeInput.Value, (v, s) => ((v % s) + s) % s).ToArray())!.Value];
        //                 break;
        //         }
        //     }
        //
        //     return dst;
        // }
        public static class NextCell
        {
            public enum NextCellEnum
            {
                MinState
            }

            public static Func<Wave, Element> GetNextCellFn(NextCellEnum w)
            {
                switch (w)
                {
                    case NextCellEnum.MinState:
                        return MinState;
                }

                return null;
            }

            public static Element MinState(Wave w)
            {
                var minValue = int.MaxValue;
                Element minElement = null;
                foreach (var element in w.CurrentWave)
                {
                    if (!element.Collapsed && element.Popcnt < minValue)
                    {
                        minElement = element;
                        minValue = element.Popcnt;
                    }
                }

                if (minElement is null)
                {
                    throw new InvalidOperationException("Failed to find Next Cell/Converge");
                }

                return minElement;
            }
        }

        public static class SelectPattern
        {
            public enum SelectPatternEnum
            {
                PatternWeighted,
                PatternUniform
            }

            public static Func<Wave, Element, int> GetPatternFn(SelectPatternEnum w)
            {
                switch (w)
                {
                    case SelectPatternEnum.PatternUniform:
                        return PatternUniform;
                    case SelectPatternEnum.PatternWeighted:
                        return PatternWeighted;
                }

                return null;
            }

            public static int PatternWeighted(Wave w, Element e)
            {
                var sum = 0f;
                var distributionList = new float[e.Coefficient.Count];
                for (int i = 0; i < e.Coefficient.Count; i++)
                {
                    if (!e.Coefficient[i])
                    {
                        continue;
                    }

                    distributionList[i] = w.Wfc.Patterns[i].Frequency;
                    sum += w.Wfc.Patterns[i].Frequency;
                }

                // Random Double in range (0,sum)
                var r = w.Wfc.Random.NextDouble() * sum;
                var accumulator = 0f;
                for (int i = 0; i < distributionList.Length; i++)
                {
                    accumulator += distributionList[i];

                    if (accumulator >= r)
                    {
                        return i;
                    }
                }

                // throw new InvalidOperationException($"Failed to select pattern for {string.Join(",", e.Pos.Value)}");
                Debug.Log($"Failed to select pattern for {string.Join(",", e.Pos.Value)}");
                return -1;
            }


            public static int PatternUniform(Wave w, Element e)
            {
                var random = w.Wfc.Random;
                var avaliable = e.Coefficient.GetCardinality();
                var r = random.Next(0, avaliable);


                var accumulator = 0;
                for (int i = 0; i < e.Coefficient.Count; i++)
                {
                    if (!e.Coefficient[i])
                    {
                        continue;
                    }

                    if (accumulator >= r)
                    {
                        return i;
                    }

                    accumulator++;
                }

                // throw new InvalidOperationException($"Failed to select pattern for {string.Join(",", e.Pos.Value)}");
                Debug.Log($"Failed to select pattern for {string.Join(",", e.Pos.Value)}");
                return -1;
            }
        }
    }
}