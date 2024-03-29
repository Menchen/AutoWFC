﻿using System;
using System.Collections;
using System.Collections.Generic;
using AutoWfc.Converters;
using Newtonsoft.Json;
using V = AutoWfc.GenericUtils.TypedArray<int>;

namespace AutoWfc.Wfc
{
    public partial class WfcUtils<T>
    {
        public class Pattern
        {
            public int Id { get; set; }

            public double NormalizedFrequency { get; set; } // A.K.A Probability
            public int Frequency { get; set; }

            [JsonIgnore]
            public double Entropy => _cachedEntropy ??= -NormalizedFrequency * Math.Log(NormalizedFrequency, 2);

            [JsonIgnore] public double InverseEntropy => _cachedInverseEntropy ??= 1d / Entropy;

            // Sum of all entropy except current pattern
            public double RemainingEntropy;

            public T Value { get; set; }

            [JsonProperty(ItemConverterType = typeof(BitArrayConverter))]
            public BitArray[] Valid { get; set; }

            [JsonIgnore] private double? _cachedEntropy;
            [JsonIgnore] private double? _cachedInverseEntropy;

            [JsonIgnore] private int? _cachedHash;

            [JsonIgnore]
            public int Hash =>
                _cachedHash ??= ((IStructuralEquatable)Value).GetHashCode(EqualityComparer<T>.Default);
            // public int Hash => Value;

            public static bool operator ==(Pattern a, Pattern b)
            {
                return a?.Hash == b?.Hash;
            }

            public static bool operator !=(Pattern a, Pattern b)
            {
                return a?.Hash != b?.Hash;
            }


            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != GetType()) return false;
                return Hash == ((Pattern)obj).Hash;
            }

            public override int GetHashCode()
            {
                return Hash;
            }
        }
    }
}