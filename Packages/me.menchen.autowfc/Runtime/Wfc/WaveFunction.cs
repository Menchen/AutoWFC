using System;
using System.Collections.Generic;
using System.Linq;
using AutoWfc.Extensions;

namespace AutoWfc.Wfc
{
    public partial class WfcUtils<T>
    {
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

                    var mutatedStateEntropyWeighted = element.Popcnt * element.Entropy *
                                                      (w.Wfc.Rng.NextDouble() * multiplierRange + multiplierStart);
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

                    var mutatedEntropy =
                        element.Entropy * (w.Wfc.Rng.NextDouble() * multiplierRange + multiplierStart);
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

                    var mutatedEntropy =
                        element.Entropy * (w.Wfc.Rng.NextDouble() * multiplierRange + multiplierStart);
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
                PatternMaxRemainingEntropy,
                PatternMinRemainingEntropy,
                PatternEntropyInverseWeighted,
                PatternEntropyHalfHalfWeighted,
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
                    case SelectPatternEnum.PatternMaxRemainingEntropy:
                        return PatternMaxRemainingEntropy;
                    case SelectPatternEnum.PatternMinRemainingEntropy:
                        return PatternMinRemainingEntropy;
                    case SelectPatternEnum.PatternEntropyInverseWeighted:
                        return PatternEntropyInverseWeighted;
                    case SelectPatternEnum.PatternEntropyHalfHalfWeighted:
                        return PatternEntropyHalfHalfWeighted;
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

                    var multiplier = w.Wfc.Rng.NextDouble() * multiplierRange + multiplierStart;
                    var frequency = w.Wfc.Patterns[i].NormalizedFrequency * multiplier;
                    distributionList[i] = frequency;
                    sum += frequency;
                }

                // Random Double in range (0,sum)
                var r = w.Wfc.Rng.NextDouble() * sum;
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
                                  (w.Wfc.Rng.NextDouble() * multiplierRange + multiplierStart) // Mutation multiplier
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
                                  (w.Wfc.Rng.NextDouble() * multiplierRange + multiplierStart) // Mutation multiplier
                                : double.MinValue))
                    .Aggregate((x, acc) => x.Item2 > acc.Item2 ? x : acc);

                var returnValue = e.Coefficient[max.Item1] ? max.Item1 : -1;
                if (returnValue < 0)
                {
                    w.Wfc.Logger?.Invoke($"Failed to select pattern for {string.Join(",", e.Pos.Value)}");
                }

                return returnValue;
            }
            
            public static int PatternMaxRemainingEntropy(Wave w, Element e)
            {
                var multiplierRange = w.Wfc.MutationMultiplier * 0.25f;
                var multiplierStart = 1f - multiplierRange;
                multiplierRange *= 2f;
                var max = e.Coefficient.IterateWithIndex().Select(validState =>
                        (validState.Item2,
                            validState.Item1
                                ? w.Wfc.Patterns[validState.Item2].RemainingEntropy *
                                  (w.Wfc.Rng.NextDouble() * multiplierRange + multiplierStart) // Mutation multiplier
                                : double.MinValue))
                    .Aggregate((x, acc) => x.Item2 > acc.Item2 ? x : acc);

                var returnValue = e.Coefficient[max.Item1] ? max.Item1 : -1;
                if (returnValue < 0)
                {
                    w.Wfc.Logger?.Invoke($"Failed to select pattern for {string.Join(",", e.Pos.Value)}");
                }

                return returnValue;
            }
            
            public static int PatternMinRemainingEntropy(Wave w, Element e)
            {
                var multiplierRange = w.Wfc.MutationMultiplier * 0.25f;
                var multiplierStart = 1f - multiplierRange;
                multiplierRange *= 2f;
                var max = e.Coefficient.IterateWithIndex().Select(validState =>
                        (validState.Item2,
                            validState.Item1
                                ? w.Wfc.Patterns[validState.Item2].RemainingEntropy *
                                  (w.Wfc.Rng.NextDouble() * multiplierRange + multiplierStart) // Mutation multiplier
                                : double.MaxValue))
                    .Aggregate((x, acc) => x.Item2 < acc.Item2 ? x : acc);

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

                    var multiplier = w.Wfc.Rng.NextDouble() * multiplierRange + multiplierStart;
                    var entropy = w.Wfc.Patterns[i].Entropy * multiplier;
                    distributionList[i] = entropy;
                    sum += entropy;
                }

                // Random Double in range (0,sum)
                var r = w.Wfc.Rng.NextDouble() * sum;
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

            public static int PatternEntropyInverseWeighted(Wave w, Element e)
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

                    var multiplier = w.Wfc.Rng.NextDouble() * multiplierRange + multiplierStart;
                    var entropy = w.Wfc.Patterns[i].InverseEntropy * multiplier;
                    distributionList[i] = entropy;
                    sum += entropy;
                }

                // Random Double in range (0,sum)
                var r = w.Wfc.Rng.NextDouble() * sum;
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

            public static int PatternEntropyHalfHalfWeighted(Wave w, Element e)
            {
                var currentState = (int)w.Wfc.Context.GetValueOrDefault("PatternEntropyHalfHalfWeighted_NextState", 0);
                w.Wfc.Context["PatternEntropyHalfHalfWeighted_NextState"] = ++currentState % 2;
                return currentState <= 0 ? PatternEntropyWeighted(w, e) : PatternEntropyInverseWeighted(w, e);
            }

            public static int PatternUniform(Wave w, Element e)
            {
                var random = w.Wfc.Rng;
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