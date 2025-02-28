using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace SparseBitsets
{
    [DebuggerDisplay("Runs = {Runs.Count}")]
    public class SparseBitset
    {

        public List<Run> Runs { get; internal set; }

        private Dictionary<long, ulong> _bitFields = new Dictionary<long, ulong>();

        public SparseBitset()
        {
            Runs = new List<Run>();
        }

        public SparseBitset(IEnumerable<Run> runs)
        {
            Runs = runs.ToList();
        }

        public void Add(ulong bit)
        {
            long key = (long)(bit / 64);
            var bitPosition = bit % 64;

            if (!_bitFields.TryGetValue(key, out ulong value))
            {
                _bitFields.Add(key, ((ulong)1 << (int)bitPosition));
            }
            else
            {
                _bitFields[key] = value | ((ulong)1 << (int)bitPosition);
            }
        }

        public void Remove(ulong bit)
        {
            long key = (long)(bit / 64);
            var bitPosition = bit % 64;
            if (_bitFields.TryGetValue(key, out ulong value))
            {
                var updatedValue = value & ~((ulong)1 << (int)bitPosition);
                if (updatedValue == 0)
                {
                    _bitFields.Remove(key);
                }
                else
                {
                    _bitFields[key] = updatedValue;
                }
            }
        }


        //public static List<Run> Merge(List<Run> runs)
        //{
        //    return runs;

        //    //if (runs.Count > 1)
        //    //{
        //    //    var reoptimzed = new List<Run>();
        //    //    var i = 0;
        //    //    var current = runs[i];
        //    //    i++;
        //    //    while (i < runs.Count)
        //    //    {
        //    //        if (runs[i].Start - current.End < 16)
        //    //        {
        //    //            current.End = runs[i].End;
        //    //            var length = current.End - current.Start + 1;
        //    //            var newArray = new ulong[length];
        //    //            Array.Copy(current.Values, newArray, current.Values.Length);
        //    //            Array.Copy(runs[i].Values, 0, newArray, length - runs[i].Values.Length, runs[i].Values.Length);
        //    //            current.Values = newArray;
        //    //        }
        //    //        else
        //    //        {
        //    //            reoptimzed.Add(current);
        //    //            current = runs[i];
        //    //        }
        //    //        i++;
        //    //    }
        //    //    reoptimzed.Add(current);
        //    //    return reoptimzed;
        //    //}

        //    //return runs;
        //}


        public void Unpack()
        {
            _bitFields = new Dictionary<long, ulong>();

            foreach (var run in Runs)
            {
                for (var i = 0; i < run.End - run.Start + 1; i++)
                {
                    _bitFields.Add(run.Start + i, run.Values[i]);
                }
            }
        }

        public void Pack()
        {
            Runs = _bitFields.OrderBy(y => y.Key)
                .Select((y, i) => new { Value = y, Group = y.Key - i })
                .GroupBy(y => y.Group)
                .Select(b =>
                    new Run
                    {
                        Start = b.Min(c => c.Value.Key),
                        End = b.Max(c => c.Value.Key),
                        Values = b.Select(c => c.Value.Value).ToArray()
                    }
                )
                .ToList();

            _bitFields.Clear();
        }


        public long GetPopCount()
        {
            long nSize = 0;
            //unchecked
            //{
            //if (this.IsOptimized)
            //{
            foreach (var run in this.Runs)
            {
                var i = 0;
                while (i < run.Values.Length)
                {
                    if (run.Values[i] == ulong.MaxValue)
                    {
                        nSize += 64;
                    }
                    else if (run.Values[i] > 0)
                    {
#if NETCOREAPP3_1 && USE_INTRINSICS
                        // NOTE: This does not seem to yield a significant increase 
                        // in speed
                        nSize += (long)Popcnt.X64.PopCount(run.Values[i]);
#else
                        nSize += BitFieldHelpers.CountSetBitsFast(run.Values[i]);
#endif
                    }
                    i++;
                }
            }
            //                }
            //                else
            //                {
            //                    foreach (var bitWord in this)
            //                    {
            //                        if (bitWord.Value == ulong.MaxValue)
            //                        {
            //                            nSize += 64;
            //                        }
            //                        else if (bitWord.Value > 0)
            //                        {
            //#if NETCOREAPP3_0
            //                    // NOTE: This does not seem to yield a significant increase 
            //                    // in speed
            //                    nSize += (long)Popcnt.X64.PopCount((ulong)bitWord.Value);
            //#else
            //                            nSize += BitFieldHelpers.CountSetBitsFast(bitWord.Value);
            //#endif
            //                        }
            //                    }
            //                }

            //}

            return nSize;
        }

        public SparseBitset And(SparseBitset b)
        {
            SparseBitset result;
            var bb = (SparseBitset)b;
            // Where the bitset keys do overlap, bits should be an AND of the bits in A and B
            //if (this.IsOptimized && bb.IsOptimized)
            //{
            result = SparseBitsetOptimzedOperators.And(this, bb);
            //}
            //else
            //{
            //    result = new SparseBitset();
            //    foreach (var bitWord in this)
            //    {
            //        if (bb.TryGetValue(bitWord.Key, out ulong value))
            //        {
            //            result.Add(bitWord.Key, bitWord.Value & value);
            //        }
            //    }
            //}

            // Where the bitset keys do not overlap, bits will automatically be zero
            // so there is no need to check the 

            return result;
        }

        public SparseBitset Or(SparseBitset b)
        {
            SparseBitset result;
            var bb = (SparseBitset)b;
            // Where the bitset keys do overlap, bits should be an AND of the bits in A and B
            //if (this.IsOptimized && bb.IsOptimized)
            //{
            result = SparseBitsetOptimzedOperators.Or(this, bb);
            //}
            //else
            //{
            //    result = new SparseBitset();

            //    foreach (var bitWord in this)
            //    {
            //        if (bb.TryGetValue(bitWord.Key, out ulong value))
            //        {
            //            result.Add(bitWord.Key, bitWord.Value & value);
            //        }
            //    }

            //    foreach (var bitWord in bb.Where(x => !this.ContainsKey(x.Key)))
            //    {
            //        this.Add(bitWord.Key, bitWord.Value);
            //    }
            //}

            // Where the bitset keys do not overlap, bits will automatically be zero
            // so there is no need to check the 

            return result;
        }

        public SparseBitset AndNot(SparseBitset b, SparseBitset fullBitset)
        {
            SparseBitset result;

            var bb = (SparseBitset)b;
            var fb = (SparseBitset)fullBitset;

            //if (IsOptimized && this.IsOptimized)
            //{
            result = SparseBitsetOptimzedOperators.And(this, SparseBitsetOptimzedOperators.Not(bb, fb));
            //}
            //else
            //{
            //    result = new SparseBitset();
            //    foreach (var bitWord in fb.Where(x => !bb.ContainsKey(x.Key)))
            //    {
            //        result.Add(bitWord.Key, bitWord.Value);
            //    }

            //    // We need to AND against the fullBitset valuepair in case we flipped a bit to 1 in a position
            //    // that doesn't exist in the full bitset

            //    foreach (var bitWord in this.Where(x => bb.ContainsKey(x.Key)))
            //    {
            //        result.Add(bitWord.Key, bitWord.Value & ~bb[bitWord.Key] & fb[bitWord.Key]);
            //    }
            //}


            return result;
        }

        public SparseBitset Not(SparseBitset fullBitset)
        {
            SparseBitset result;

            var fb = (SparseBitset)fullBitset;


            //if (this.IsOptimized && fb.IsOptimized)
            //{
            result = SparseBitsetOptimzedOperators.Not(this, fb);
            //}
            //else
            //{
            //    result = new SparseBitset();
            //    // Because the bitsets are sparse, the whole range of bitsets is not stored.
            //    // This makes it impossible to perform a correct NOT without the full bitset

            //    // First, we need to copy the bit fields that are in the fullbitset but are not in
            //    // the source bitset. This is equivalent to flipping the non-existant bits in the source
            //    // bitset that exist in the range off the full bitset.

            //    foreach (var bitWord in fb.Where(x => !this.ContainsKey(x.Key)))
            //    {
            //        result.Add(bitWord.Key, bitWord.Value);
            //    }

            //    // Next we copy in the inverse of the bit fields from our source, but also AND
            //    // each bitfield with the corresponding field in the full bitset to remove invalid bits

            //    foreach (var bitWord in this)
            //    {
            //        result.Add(bitWord.Key, ~bitWord.Value & fb[bitWord.Key]);
            //    }


            //}

            return result;
        }

        //public static SparseBitset Not(this SparseBitset a)
        //{
        //    var result = new SparseBitset();

        //    foreach (var bitWord in a)
        //    {
        //        result.Add(bitWord.Key, ~bitWord.Value);
        //    }

        //    return result;
        //}

        /// <summary>
        /// Returns a list of values that are contained in the bitset
        /// </summary>
        /// <param name="bitSet"></param>
        /// <returns></returns>
        public IEnumerable<long> GetValues()
        {
            //if (IsOptimized)
            //{
            foreach (var run in Runs)
            {
                var start = run.Start;
                var runLength = run.End - run.Start + 1;

                for (var j = 0; j < runLength; j++)
                {
                    var value = run.Values[j];
                    const int length = 64;
                    for (var i = 0; i < length; i++)
                    {
                        if ((value & 1) == 1)
                            yield return (run.Start + j) * 64 + i;
                        value >>= 1;
                    }
                }
            }
            //}
            //else
            //{
            //    foreach (var keyValuePair in this)
            //    {
            //        var value = keyValuePair.Value;
            //        const int length = 64;
            //        for (var i = 0; i < length; i++)
            //        {
            //            if ((value & 1) == 1)
            //                yield return keyValuePair.Key * 64 + i;
            //            value >>= 1;
            //        }
            //    }
            //}
        }
    }

}