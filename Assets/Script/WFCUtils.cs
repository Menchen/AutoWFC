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
using V = TypedArray<int>;


namespace WFC
{
    public partial class WfcUtils<T>
    {
        [JsonProperty] public int Dimension { get; private set; }

        // [JsonProperty] public int PatternSize { get; private set; }
        [JsonProperty] public int BITSetSize { get; private set; }

        [JsonProperty] public float MutationMultiplier { get; set; }

        // public int Vol => Convert.ToInt32(Math.Pow(PatternSize, Dimension));

        public List<Pattern> Patterns;

        [JsonConverter(typeof(BitArrayConverter))]
        public BitArray MaskUsed; // Default Mask for enabled pattern

        [JsonIgnore] public Random Random;


        [JsonIgnore] public BorderBehavior BorderBehavior;

        [JsonProperty]
        public SelectPattern.SelectPatternEnum SelectPatternEnum
        {
            get => _selectPatternEnum;
            set
            {
                _selectPatternEnum = value;
                PatternFn = SelectPattern.GetPatternFn(value);
            }
        }
        
        [JsonIgnore]
        private SelectPattern.SelectPatternEnum _selectPatternEnum;

        [JsonProperty]
        public NextCell.NextCellEnum NextCellEnum
        {
            get => _nextCellEnum;
            set
            {
                _nextCellEnum = value;
                NextCellFn = NextCell.GetNextCellFn(value);
            }
        }

        [JsonIgnore]
        private NextCell.NextCellEnum _nextCellEnum;

        [JsonIgnore] public Func<Wave, Element, int> PatternFn;

        [JsonIgnore] public Func<Wave, Element> NextCellFn;

        [JsonIgnore] public Action<Wave> OnPropagate;

        public event Action<string> Logger;

        public readonly T EmptyState;

        public INeibours Neighbours;

        [JsonIgnore] public Dictionary<T, int> PatternLookUp;

        [JsonIgnore] public Dictionary<string, object> Context; // Used by external function e.g PatternFn & NextCellFn


        public static WfcUtils<T> BuildFromJson(string json)
        {
            var wfcUtils = JsonConvert.DeserializeObject<WfcUtils<T>>(json, new BitArrayConverter());
            // wfcUtils.NextCellFn ??= NextCell.GetNextCellFn(wfcUtils.NextCellEnum);
            // wfcUtils.PatternFn ??= SelectPattern.GetPatternFn(wfcUtils.SelectPatternEnum);
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
            var frequencyTable = new Dictionary<T, int>();

            var usedMask = new HashSet<T>();
            for (int i = 0; i < sizeInputLength; i++)
            {
                var index = ArrayUtils.UnRavelIndex(sizeInput, i);
                var centerValue = input[i];
                var usedFlag = true;
                if (centerValue is null || Equals(emptyState, centerValue))
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

                        if (Equals(EmptyState,value))
                        {
                            usedFlag = false;
                        }
                        else
                        {
                            listArray[j].Add(value);
                        }
                    }
                    else
                    {
                        usedFlag = false;
                    }
                }

                if (usedFlag)
                {
                    usedMask.Add(centerValue);
                }

                frequencyTable[centerValue] = frequencyTable.GetValueOrDefault(centerValue) + 1;
                lookupTable[centerValue] = listArray;
            }

            GeneratePatternFromLookUpTable(lookupTable, frequencyTable,usedMask);
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

            var frequencyTable = Patterns.ToDictionary(e => e.Value, e => e.Frequency);

            var usedMask = MaskUsed.IterateWithIndex().Where(tuple => tuple.Item1)
                .Select(tuple => Patterns[tuple.Item2].Value).ToHashSet();
            for (int i = 0; i < sizeInputLength; i++)
            {
                var index = ArrayUtils.UnRavelIndex(sizeInput, i);
                var centerValue = input[i];
                var usedFlag = true;
                if (centerValue is null || Equals(EmptyState,centerValue))
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

                        if (Equals(value,EmptyState))
                        {
                            usedFlag = false;
                        }
                        else
                        {
                            listArray[j].Add(value);
                        }
                    }
                    else
                    {
                        usedFlag = false;
                    }
                }

                if (usedFlag)
                {
                    // Only pattern with all valid neighbours are set as 'valid'
                    usedMask.Add(centerValue);
                }
                frequencyTable[centerValue] = frequencyTable.GetValueOrDefault(centerValue) + 1;
                lookupTable[centerValue] = listArray;
            }
            
            GeneratePatternFromLookUpTable(lookupTable, frequencyTable,usedMask);
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

            var frequencyTable = Patterns.ToDictionary(e => e.Value, e => e.Frequency);
            
            var usedMask = MaskUsed.IterateWithIndex().Where(tuple => tuple.Item1)
                .Select(tuple => Patterns[tuple.Item2].Value).ToHashSet();
            for (int x = 1; x < sizeInput[0]-1; x++)
            {
                for (int y = 1; y < sizeInput[1]-1; y++)
                {
                    var index = ArrayUtils.RavelIndex(sizeInput, new[] { x, y });
                    if (index is null)
                    {
                        continue;
                    }

                    usedMask.Add(input[index.Value]);
                }
            }

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

                // frequencyTable[centerValue] = frequencyTable.GetValueOrDefault(centerValue) - 1;
                lookupTable[centerValue] = listArray;
            }

            GeneratePatternFromLookUpTable(lookupTable, frequencyTable,usedMask);
        }

        private void GeneratePatternFromLookUpTable(Dictionary<T, HashSet<T>[]> lookupTable,
            Dictionary<T, int> frequencyTable,HashSet<T> usedMask)
        {
            // Remove Null/Empty state
            lookupTable = lookupTable.Where(e => !Equals(e.Key, EmptyState)).ToDictionary(e => e.Key,
                e =>
                {
                    return e.Value.Select(hashSet => hashSet.Where(pattern => !Equals(pattern, EmptyState)).ToHashSet())
                        .ToArray();
                });

            // Populate Patterns Ids
            var id = 0;
            var patternsDict = new Dictionary<T, Pattern>();
            MaskUsed = new BitArray(lookupTable.Count,false);
            BITSetSize = lookupTable.Count;
            var totalFrequency = (double)frequencyTable.Values.Aggregate((a, b) => a + b);
            foreach (var kvPair in lookupTable)
            {
                patternsDict[kvPair.Key] = new Pattern
                {
                    Id = id,
                    Value = kvPair.Key,
                    Valid = Enumerable.Range(0, Neighbours.Length).Select(_ => new BitArray(BITSetSize)).ToArray(),
                    Frequency = frequencyTable.GetValueOrDefault(kvPair.Key),
                    NormalizedFrequency = frequencyTable.GetValueOrDefault(kvPair.Key) / totalFrequency
                };

                if (usedMask.Contains(kvPair.Key))
                {
                    MaskUsed.Set(id, true);
                }
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
                Logger?.Invoke($"WFC failed at {string.Join(", ", res.Value)}");
                return false;
            }

            output = w.CurrentWave.Select(e => e.Value).ToArray();
            return true;
        }

        public static class NextCell
        {
            public enum NextCellEnum
            {
                MinState,
                MaxEntropy,
                MinEntropy,
                MinStateEntropyWeighted,
            }

            public static Func<Wave, Element> GetNextCellFn(NextCellEnum w)
            {
                switch (w)
                {
                    case NextCellEnum.MinState:
                        return MinState;
                    case NextCellEnum.MaxEntropy:
                        return MaxEntropy;
                    case NextCellEnum.MinEntropy:
                        return MinEntropy;
                    case NextCellEnum.MinStateEntropyWeighted:
                        return MinStateEntropyWeighted;
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
            
            public static Element MinStateEntropyWeighted(Wave w)
            {
                var localMinValue = double.MaxValue;
                Element minElement = null;
                var multiplierRange = w.Wfc.MutationMultiplier * 0.05f;
                var multiplierStart = 1f - multiplierRange;
                multiplierRange *= 2f;
                foreach (var element in w.CurrentWave)
                {
                    if (element.Collapsed)
                    {
                        continue;
                    }
                    var mutatedStateEntropyWeighted = element.Popcnt * element.Entropy * (w.Wfc.Random.NextDouble() * multiplierRange + multiplierStart);
                    if (mutatedStateEntropyWeighted < localMinValue)
                    {
                        minElement = element;
                        localMinValue = mutatedStateEntropyWeighted;
                    }
                }

                if (minElement is null)
                {
                    throw new InvalidOperationException("Failed to find Next Cell/Converge");
                }

                return minElement;
            }
            public static Element MinEntropy(Wave w)
            {
                var localMinValue = double.MaxValue;
                Element minElement = null;
                var multiplierRange = w.Wfc.MutationMultiplier * 0.25f;
                var multiplierStart = 1f - multiplierRange;
                multiplierRange *= 2f;
                foreach (var element in w.CurrentWave)
                {
                    if (element.Collapsed)
                    {
                        continue;
                    }
                    var mutatedEntropy = element.Entropy * (w.Wfc.Random.NextDouble() * multiplierRange + multiplierStart);
                    if (mutatedEntropy < localMinValue)
                    {
                        minElement = element;
                        localMinValue = mutatedEntropy;
                    }
                }

                if (minElement is null)
                {
                    throw new InvalidOperationException("Failed to find Next Cell/Converge");
                }

                return minElement;
            }
            public static Element MaxEntropy(Wave w)
            {
                var maxValue = double.MinValue;
                Element maxElement = null;
                var multiplierRange = w.Wfc.MutationMultiplier * 0.25f;
                var multiplierStart = 1f - multiplierRange;
                multiplierRange *= 2f;
                foreach (var element in w.CurrentWave)
                {
                    if (element.Collapsed)
                    {
                        continue;
                    }
                    var mutatedEntropy = element.Entropy * (w.Wfc.Random.NextDouble() * multiplierRange + multiplierStart);
                    if (mutatedEntropy > maxValue)
                    {
                        maxElement = element;
                        maxValue = mutatedEntropy;
                    }
                }

                if (maxElement is null)
                {
                    throw new InvalidOperationException("Failed to find Next Cell/Converge");
                }

                return maxElement;
            }
        }

        public static class SelectPattern
        {
            public enum SelectPatternEnum
            {
                PatternEntropyWeighted,
                PatternUniform,
                PatternFrequencyWeighted,
                PatternMaxEntropy,
                PatternMinEntropy,
            }

            public static Func<Wave, Element, int> GetPatternFn(SelectPatternEnum w)
            {
                switch (w)
                {
                    case SelectPatternEnum.PatternUniform:
                        return PatternUniform;
                    case SelectPatternEnum.PatternEntropyWeighted:
                        return PatternEntropyWeighted;
                    case SelectPatternEnum.PatternFrequencyWeighted:
                        return PatternFrequencyWeighted;
                    case SelectPatternEnum.PatternMaxEntropy:
                        return PatternMaxEntropy;
                    case SelectPatternEnum.PatternMinEntropy:
                        return PatternMinEntropy;
                }

                return null;
            }
            
            public static int PatternFrequencyWeighted(Wave w, Element e)
            {
                var sum = 0d;
                var multiplierRange = w.Wfc.MutationMultiplier * 0.25f;
                var multiplierStart = 1f - multiplierRange;
                multiplierRange *= 2f;
                var distributionList = new double[e.Coefficient.Count];
                for (int i = 0; i < e.Coefficient.Count; i++)
                {
                    if (!e.Coefficient[i])
                    {
                        continue;
                    }

                    var multiplier = w.Wfc.Random.NextDouble() * multiplierRange + multiplierStart;
                    var frequency = w.Wfc.Patterns[i].NormalizedFrequency * multiplier;
                    distributionList[i] = frequency;
                    sum += frequency;
                }

                // Random Double in range (0,sum)
                var r = w.Wfc.Random.NextDouble() * sum;
                var accumulator = 0d;
                for (int i = 0; i < distributionList.Length; i++)
                {
                    accumulator += distributionList[i];

                    if (accumulator >= r)
                    {
                        return i;
                    }
                }

                // throw new InvalidOperationException($"Failed to select pattern for {string.Join(",", e.Pos.Value)}");
                w.Wfc.Logger?.Invoke($"Failed to select pattern for {string.Join(",", e.Pos.Value)}");
                return -1;
            }
            public static int PatternMinEntropy(Wave w, Element e)
            {
                var multiplierRange = w.Wfc.MutationMultiplier * 0.25f;
                var multiplierStart = 1f - multiplierRange;
                multiplierRange *= 2f;
                var min = e.Coefficient.IterateWithIndex().Select(validState =>
                        (validState.Item2,
                            validState.Item1
                                ? w.Wfc.Patterns[validState.Item2].Entropy *
                                  (w.Wfc.Random.NextDouble() * multiplierRange + multiplierStart) // Mutation multiplier
                                : double.MaxValue))
                    .Aggregate((x, acc) => x.Item2 < acc.Item2 ? x : acc);

                var returnValue = e.Coefficient[min.Item1] ? min.Item1 : -1;
                if (returnValue < 0)
                {
                    w.Wfc.Logger?.Invoke($"Failed to select pattern for {string.Join(",", e.Pos.Value)}");
                }

                return returnValue;
            }

            public static int PatternMaxEntropy(Wave w, Element e)
            {
                var multiplierRange = w.Wfc.MutationMultiplier * 0.25f;
                var multiplierStart = 1f - multiplierRange;
                multiplierRange *= 2f;
                var max = e.Coefficient.IterateWithIndex().Select(validState =>
                        (validState.Item2,
                            validState.Item1
                                ? w.Wfc.Patterns[validState.Item2].Entropy *
                                  (w.Wfc.Random.NextDouble() * multiplierRange + multiplierStart) // Mutation multiplier
                                : double.MinValue))
                    .Aggregate((x, acc) => x.Item2 > acc.Item2 ? x : acc);

                var returnValue = e.Coefficient[max.Item1] ? max.Item1 : -1;
                if (returnValue < 0)
                {
                    w.Wfc.Logger?.Invoke($"Failed to select pattern for {string.Join(",", e.Pos.Value)}");
                }

                return returnValue;
            }

            public static int PatternEntropyWeighted(Wave w, Element e)
            {
                var sum = 0d;
                var multiplierRange = w.Wfc.MutationMultiplier * 0.25f;
                var multiplierStart = 1f - multiplierRange;
                multiplierRange *= 2f;
                var distributionList = new double[e.Coefficient.Count];
                for (int i = 0; i < e.Coefficient.Count; i++)
                {
                    if (!e.Coefficient[i])
                    {
                        continue;
                    }

                    var multiplier = w.Wfc.Random.NextDouble() * multiplierRange + multiplierStart;
                    var entropy = w.Wfc.Patterns[i].Entropy * multiplier;
                    distributionList[i] = entropy;
                    sum += entropy;
                }

                // Random Double in range (0,sum)
                var r = w.Wfc.Random.NextDouble() * sum;
                var accumulator = 0d;
                for (int i = 0; i < distributionList.Length; i++)
                {
                    accumulator += distributionList[i];

                    if (accumulator >= r)
                    {
                        return i;
                    }
                }

                // throw new InvalidOperationException($"Failed to select pattern for {string.Join(",", e.Pos.Value)}");
                w.Wfc.Logger?.Invoke($"Failed to select pattern for {string.Join(",", e.Pos.Value)}");
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
                w.Wfc.Logger?.Invoke($"Failed to select pattern for {string.Join(",", e.Pos.Value)}");
                return -1;
            }
        }
    }
}