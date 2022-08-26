/*
 * Credit:
 * Original C++ by jdah
 * C# Port by Mengchen
 * https://gist.github.com/jdah/ad997b858513a278426f8d91317115b9
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Script.GenericUtils;
using Random = System.Random;
using V = TypedArray<int>;



namespace WFC
{
    public partial class WfcUtils<T>
    {
        public int Dimension { get; private set; }
        public int PatternSize { get; private set; }
        public int BITSetSize { get; private set; }

        public int Vol => Convert.ToInt32(Math.Pow(PatternSize, Dimension));

        public V SizeInput;
        
        public readonly T[] Input;

        public List<Pattern> Patterns;

        public BitArray MaskUsed;

        public Random Random;

        public int Flags;

        public BorderBehavior BorderBehavior;

        public Func<Wave, Element, int> PatternFn;
        public Func<Wave, Element> NextCellFn;
        public Action<Wave> OnPropagate;

        public readonly T EmptyState;
        public INeibours<object> Neighbours;
        public readonly int[] PatternSizeVec;

        public WfcUtils(int dimension, int patternSize, int bitSetSize, V sizeInput, T[] input, Func<Wave, Element, int> patternFn,
            Func<Wave, Element> nextCellFn, Action<Wave> onPropagate, BorderBehavior borderBehavior, Random random,
            int flags, INeibours<object> neighbours, T emptyState)
        {
            Dimension = dimension;
            PatternSize = patternSize;
            BITSetSize = bitSetSize;
            SizeInput = sizeInput;
            Input = input;
            PatternFn = patternFn;
            NextCellFn = nextCellFn;
            OnPropagate = onPropagate;
            BorderBehavior = borderBehavior;
            Random = random;
            Flags = flags;
            Neighbours = neighbours;
            EmptyState = emptyState;
            
            
            PatternSizeVec = Enumerable.Repeat(PatternSize, PatternSize).ToArray();


            Patterns = new List<Pattern>();
            
            // get pattern data from input 
            var sizeInputLength = SizeInput.Value.Aggregate((a,b)=>a*b);
            for (int i = 0; i < sizeInputLength; i++)
            {
                var data = DataAt(ArrayUtils.UnRavelIndex(SizeInput, i));
                if (data is not null)
                {
                    Patterns.Add(new Pattern(data,Neighbours.Length,BITSetSize));
                }
            }

            // Compute freq hashmap
            var patternHashToFreq = new Dictionary<int, int>();
            foreach (var pattern in Patterns)
            {
                patternHashToFreq[pattern.Hash]++;
            }

            // Assign non normalized freq
            foreach (var pattern in Patterns)
            {
                pattern.Frequency = patternHashToFreq[pattern.Hash];
            }

            Patterns = Patterns.GroupBy(e => e.Hash).Select(e => e.First()).ToList();

            // TODO Add rotation, reflections
            
            // Compute total sum of freq
            float freqTotal = 0f;
            foreach (var pattern in Patterns)
            {
                freqTotal += pattern.Frequency;
            }
            
            // Normalize final freq
            foreach (var pattern in Patterns)
            {
                pattern.Frequency /= freqTotal;
            }
            
            // Calculate mask & Id
            MaskUsed = new BitArray(BITSetSize);

            for (int i = 0; i < Patterns.Count; i++)
            {
                Patterns[i].Id = i;
                MaskUsed.Set(i,true);
            }

            // Calculate valid pattern for each possible neighnours
            foreach (var p in Patterns)
            {
                foreach (var q in Patterns)
                {
                    for (int i = 0; i < Neighbours.Length; i++)
                    {
                        var neighbour = Neighbours.Neighbours[i];
                        var valid = true;
                        for (int s = 0; s < Vol; s++)
                        {
                            var offsetQ = ArrayUtils.UnRavelIndex(PatternSizeVec, s);

                            var offsetP = neighbour.Zip(offsetQ, (a, b) => a + b).ToArray();

                            if (!ArrayUtils.InBounds(PatternSizeVec,offsetP))
                            {
                                break;
                            }
                            
                            var vecP = p.Data[ArrayUtils.RavelIndex(PatternSizeVec, offsetP)!.Value];
                            var vecQ = p.Data[ArrayUtils.RavelIndex(PatternSizeVec, offsetQ)!.Value];

                            if (!EqualityComparer<T>.Default.Equals(vecP,vecQ))
                            {
                                valid = false;
                                break;
                            }
                        }

                        if (valid)
                        {
                            p.Valid[i].Set(q.Id,true);
                        }
                        
                    }

                }
            }
        }

        public bool Collapse(V sizeOut, out T[] output, T[] preset = null)
        {
            var w = new Wave(this, sizeOut, preset);
            var res = w.Collapse();
            output = null;
            if (res is not null)
            {
                return false;
            }

            output = w.wave.Select(e=>e.Value).ToArray();
            return true;
        }

        /**
         * Create Array of T with size Vol, A.K.A PatternSize ** N 
         * With neighbours from Input based with BorderBehavior
         */
        private T[] DataAt(V center)
        {
            var northWest = center.Value.Select(e => e - (PatternSize / 2)).ToArray();

            if (BorderBehavior == BorderBehavior.EXCLUDE)
            {
                var southEast = northWest.Select(e => e + PatternSize - 1).ToArray();
                if (!ArrayUtils.InBounds(SizeInput,northWest) || !ArrayUtils.InBounds(SizeInput,southEast))
                {
                    return null;
                }
            }

            var dst = new T[Vol];
            for (int i = 0; i < Vol; i++)
            {
                var offset = ArrayUtils.UnRavelIndex(PatternSizeVec, i);
                var pos = northWest.Zip(offset, (a,b) => a + b).ToArray();
                switch (BorderBehavior)
                {
                    case BorderBehavior.EXCLUDE:
                        dst[i] = Input[ArrayUtils.RavelIndex(SizeInput, pos)!.Value];
                        break;
                    case BorderBehavior.ZERO:
                        if (!ArrayUtils.InBounds(SizeInput,pos))
                        {
                            dst[i] = EmptyState;
                        }
                        break;
                    case BorderBehavior.CLAMP:
                        dst[i] = Input[ArrayUtils.RavelIndex(SizeInput, pos.Zip(SizeInput.Value,(v,s)=>Math.Clamp(v,0,s-1)).ToArray())!.Value];
                        break;
                    case BorderBehavior.WRAP:
                        dst[i] = Input[ArrayUtils.RavelIndex(SizeInput, pos.Zip(SizeInput.Value,(v,s)=>((v%s)+s)%s).ToArray())!.Value];
                        break;
                }
            }
            return dst;

        }
        
    }
}