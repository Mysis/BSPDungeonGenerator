using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ProceduralGeneration {
    class Globals {
        public static System.Random rand = new System.Random();
    }

    [System.Serializable]
    public class IntRange {
        [UnityEngine.SerializeField]
        public int min, max;
        public IntRange(int min, int max) {
            if (min > max) throw new ArgumentOutOfRangeException("Max is less than min");
            this.min = min;
            this.max = max;
        }
        public int Length() {
            return max - min;
        }
        public int Random() {
            return Globals.rand.Next(min, max + 1);
        }
        public int[] ToArray() {
            return new int[] { min, max };
        }
        public IntRange Union(IntRange intRange, bool ignoreOutOfRange = false) {
            if ((max < intRange.min || min > intRange.max) && !ignoreOutOfRange) throw new ArgumentOutOfRangeException("Ranges do not intersect");
            return new IntRange(min < intRange.min ? min : intRange.min, max > intRange.max ? max : intRange.max);
        }
        public IntRange Intersection(IntRange intRange) {
            if (max < intRange.min || min > intRange.max) throw new ArgumentOutOfRangeException("Ranges do not intersect");
            return new IntRange(min > intRange.min ? min : intRange.min, max < intRange.max ? max : intRange.max);
        }
        public bool WithinRange(int value) {
            return value >= min && value <= max;
        }
        public override string ToString() {
            return "min: " + min + ", max: " + max;
        }
    }
}
