using System;
using UnityEngine;

namespace AutoWfc.Extensions
{
    public static class Vector3IntExtension
    {
        public static BoundsInt BoundsIntFrom2Points(this Vector3Int a, Vector3Int b)
        {
            var xMin = Math.Min(a.x, b.x);
            var xMax = Math.Max(a.x, b.x);
            var yMin = Math.Min(a.y, b.y);
            var yMax = Math.Max(a.y, b.y);
            var zMin = Math.Min(a.z, b.z);
            var zMax = Math.Max(a.z, b.z);

            return new BoundsInt(xMin, yMin, zMin, xMax - xMin+1, yMax - yMin+1, zMax - zMin+1);
        }
    }
}