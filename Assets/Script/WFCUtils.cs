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
using Script;
using Script.GenericUtils;
using Unity.Mathematics;
using UnityEngine;
using Random = System.Random;
using V = TypedArray<int>;



namespace WFC
{
    public partial class WfcUtils<T>
    {
        public int N { get; private set; }
        public int PatternSize { get; private set; }
        public int BITSetSize { get; private set; }

        public int Vol => Convert.ToInt32(Math.Pow(PatternSize, N));

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

        public WfcUtils(int n, int s, int d, V sizeInput, T[] input, Func<Wave, Element, int> patternFn,
            Func<Wave, Element> nextCellFn, Action<Wave> onPropagate, BorderBehavior borderBehavior, Random random,
            int flags, INeibours<object> neighbours, T emptyState)
        {
            this.N = n;
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

            // get pattern data from input 
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
            var patternSizeVec = Enumerable.Repeat(PatternSize, PatternSize).ToArray();
            for (int i = 0; i < Vol; i++)
            {
                var offset = ArrayUtils.UnRavelIndex(patternSizeVec, i)!.Value;
                var pos = northWest.Zip(offset.Value, (a,b) => a + b).ToArray();
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