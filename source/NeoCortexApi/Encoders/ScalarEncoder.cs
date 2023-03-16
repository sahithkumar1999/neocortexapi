// Copyright (c) Damir Dobric. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
using NeoCortexApi.Entities;
using NeoCortexApi.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace NeoCortexApi.Encoders
{


    /// <summary>
    /// Defines the <see cref="ScalarEncoderExperimental" />
    /// </summary>
    public class ScalarEncoder : EncoderBase
    {
        /// <summary>
        /// Gets a value indicating whether IsDelta
        /// </summary>
        public override bool IsDelta => throw new NotImplementedException();

        /// <summary>
        /// Gets the Width
        /// </summary>
        public override int Width => throw new NotImplementedException();

        /// <summary>
        /// Initializes a new instance of the <see cref="ScalarEncoderExperimental"/> class.
        /// </summary>
        public ScalarEncoder()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ScalarEncoderExperimental"/> class.
        /// </summary>
        /// <param name="encoderSettings">The encoderSettings<see cref="Dictionary{string, object}"/></param>
        public ScalarEncoder(Dictionary<string, object> encoderSettings)
        {
            this.Initialize(encoderSettings);
        }

        /// <summary>
        /// The AfterInitialize
        /// </summary>
        public override void AfterInitialize()
        {
            if (W % 2 == 0)
            {
                throw new ArgumentException("W must be an odd number (to eliminate centering difficulty)");
            }

            HalfWidth = (W - 1) / 2;

            // For non-periodic inputs, padding is the number of bits "outside" the range,
            // on each side. I.e. the representation of minval is centered on some bit, and
            // there are "padding" bits to the left of that centered bit; similarly with
            // bits to the right of the center bit of maxval
            Padding = Periodic ? 0 : HalfWidth;

            if (double.NaN != MinVal && double.NaN != MaxVal)
            {
                if (MinVal >= MaxVal)
                {
                    throw new ArgumentException("maxVal must be > minVal");
                }

                RangeInternal = MaxVal - MinVal;
            }

            // There are three different ways of thinking about the representation. Handle
            // each case here.
            InitEncoder(W, MinVal, MaxVal, N, Radius, Resolution);

            //nInternal represents the output area excluding the possible padding on each side
            NInternal = N - 2 * Padding;

            if (Name == null)
            {
                if ((MinVal % ((int)MinVal)) > 0 ||
                    (MaxVal % ((int)MaxVal)) > 0)
                {
                    Name = "[" + MinVal + ":" + MaxVal + "]";
                }
                else
                {
                    Name = "[" + (int)MinVal + ":" + (int)MaxVal + "]";
                }
            }

            //Checks for likely mistakes in encoder settings
            if (IsRealCortexModel)
            {
                if (W < 21 || W <= 2)
                {
                    throw new ArgumentException(
                        "Number of bits in the SDR (%d) must be greater than 2, and recommended >= 21 (use forced=True to override)");
                }
            }
        }


        protected void InitEncoder(int w, double minVal, double maxVal, int n, double radius, double resolution)
        {
            if (N != 0)
            {
                if (double.NaN != minVal && double.NaN != maxVal)
                {
                    if (!Periodic)
                    {
                        Resolution = RangeInternal / (N - W);
                    }
                    else
                    {
                        Resolution = RangeInternal / N;
                    }

                    Radius = W * Resolution;

                    if (Periodic)
                    {
                        Range = RangeInternal;
                    }
                    else
                    {
                        Range = RangeInternal + Resolution;
                    }
                }
            }
            else
            {
                if (radius != 0)
                {
                    Resolution = Radius / w;
                }
                else if (resolution != 0)
                {
                    Radius = Resolution * w;
                }
                else
                {
                    throw new ArgumentException(
                        "One of n, radius, resolution must be specified for a ScalarEncoder");
                }

                if (Periodic)
                {
                    Range = RangeInternal;
                }
                else
                {
                    Range = RangeInternal + Resolution;
                }

                double nFloat = w * (Range / Radius) + 2 * Padding;
                N = (int)(nFloat);
            }
        }


        /// <summary>
        /// Gets the index of the first non-zero bit.
        /// </summary>
        /// <param name="input"></param>
        /// <returns>Null in a case of an error.</returns>
        /// <exception cref="ArgumentException"></exception>
        protected int? GetFirstOnBit(double input)
        {
            if (input == double.NaN)
            {
                return null;
            }
            else
            {
                if (input < MinVal)
                {
                    if (ClipInput && !Periodic)
                    {
                        Debug.WriteLine("Clipped input " + Name + "=" + input + " to minval " + MinVal);

                        input = MinVal;
                    }
                    else
                    {
                        throw new ArgumentException($"Input ({input}) less than range ({MinVal} - {MaxVal}");
                    }
                }
            }

            if (Periodic)
            {
                if (input >= MaxVal)
                {
                    throw new ArgumentException($"Input ({input}) greater than periodic range ({MinVal} - {MaxVal}");
                }
            }
            else
            {
                if (input > MaxVal)
                {
                    if (ClipInput)
                    {

                        Debug.WriteLine($"Clipped input {Name} = {input} to maxval MaxVal");
                        input = MaxVal;
                    }
                    else
                    {
                        throw new ArgumentException($"Input ({input}) greater than periodic range ({MinVal} - {MaxVal}");
                    }
                }
            }

            int centerbin;
            if (Periodic)
            {
                centerbin = (int)((input - MinVal) * NInternal / Range + Padding);
            }
            else
            {
                centerbin = ((int)(((input - MinVal) + Resolution / 2) / Resolution)) + Padding;
            }

            return centerbin - HalfWidth;
        }


        /// <summary>
        /// Gets the bucket index of the given value.
        /// </summary>
        /// <param name="inputData">The data to be encoded. Must be of type double.</param>
        /// <param name="bucketIndex">The bucket index.</param>
        /// <returns></returns>

        public int? GetBucketIndex(decimal inputData)
        {
            if ((double)inputData < MinVal || (double)inputData > MaxVal)
            {
                return null;
            }

            decimal fraction = (decimal)(((double)inputData - MinVal) / (MaxVal - MinVal));

            if (Periodic)
            {
                fraction = fraction - Math.Floor(fraction);
            }

            int bucketIndex = (int)Math.Floor(fraction * N);

            if (bucketIndex == N)
            {
                bucketIndex = 0;
            }

            // For periodic encoders, the center of the first bucket is considered equal to the center of the last bucket
            if (Periodic && bucketIndex == 0 && Math.Abs((double)inputData - MaxVal) <= 0.0000000000000000000000000001)
            {
                bucketIndex = N - 1;
            }

            // Check if the input value is within the radius of the bucket
            if (Radius >= 0)
            {
                decimal bucketWidth = ((decimal)MaxVal - (decimal)MinVal) / (decimal)N;
                decimal bucketCenter = (bucketWidth * bucketIndex) + (bucketWidth / 2) + (decimal)MinVal;

                if (Math.Abs((decimal)inputData - bucketCenter) > (decimal)Radius * bucketWidth)
                {
                    return null;
                }
            }

            return bucketIndex;

        }


        public (Dictionary<string, (List<double[]>, string)>, List<string>) Decode(double[] encoded, string parentFieldName = "")
        {
            // For now, we simply assume any top-down output greater than 0
            // is ON. Eventually, we will probably want to incorporate the strength
            // of each top-down output.
            var tmpOutput = encoded.Take(N).Select(x => x > 0).ToArray();
            if (!tmpOutput.Any())
            {
                return (new Dictionary<string, (List<double[]>, string)>(), new List<string>());
            }

            // ------------------------------------------------------------------------
            // First, assume the input pool is not sampled 100%, and fill in the
            // "holes" in the encoded representation (which are likely to be present
            // if this is a coincidence that was learned by the SP).

            // Search for portions of the output that have "holes"
            var maxZerosInARow = HalfWidth;
            for (var i = 0; i < maxZerosInARow; i++)
            {
                var searchStr = new double[i + 3];
                searchStr[0] = 1;
                searchStr[^1] = 1;
                for (var j = 1; j < i + 2; j++)
                {
                    searchStr[j] = 0;
                }
                var subLen = searchStr.Length;

                // Does this search string appear in the output?
                if (Periodic)
                {
                    for (var j = 0; j < N; j++)
                    {
                        var outputIndices = Enumerable.Range(j, subLen).Select(k => k % N).ToArray();
                        if (searchStr.SequenceEqual(outputIndices.Select(k => Convert.ToBoolean(tmpOutput[k]))))
                        {
                            foreach (var index in outputIndices)
                            {
                                tmpOutput[index] = 1;
                            }
                        }


                    }
                }
                else
                {
                    for (var j = 0; j < N - subLen + 1; j++)
                    {
                        if (searchStr.SequenceEqual(tmpOutput.Skip(j).Take(subLen)))
                        {
                            for (var k = j; k < j + subLen; k++)
                            {
                                tmpOutput[k] = true;
                            }
                        }
                    }
                }
            }

            if (Verbosity >= 2)
            {
                Console.WriteLine($"raw output: {string.Join(", ", encoded.Take(N))}");
                Console.WriteLine($"filtered output: {string.Join(", ", tmpOutput)}");
            }

            // ------------------------------------------------------------------------
            // Find each run of 1's.
            var nz = tmpOutput.Select((x, i) => new { Value = x, Index = i }).Where(x => x.Value).Select(x => x.Index).ToArray();
            var runs = new List<(int startIdx, int runLength)>(); // will be tuples of (startIdx, runLength)
            var run = (nz[0], 1);
            for (var i = 1; i < nz.Length; i++)
            {
                if (nz[i] == run.startIdx + run.runLength)
                {
                    run.runLength++;
                }
                else
                {
                    runs.Add(run);
                    run = (nz[i], 1);
                }
            }
            runs.Add(run);

            // If we have a periodic encoder, merge the first and last run if they
            // both go all the way to the edges
            if (Periodic && runs.Count > 1)
            {
                if (runs[0].Item1 == 0 && runs[^1].Item1 + runs[^1].Item2 == N)
                {
                    runs[^1].Item2 += runs[0].Item2;
                    runs.RemoveAt(0);
                }
            }

            // ------------------------------------------------------------------------
            // Now, for each group of 1's, determine the "left" and "right" edges, where
            // the "left" edge is inset by halfwidth and the "right" edge is inset by
            // halfwidth.
            // For a group of width w or less, the "left" and "right" edge are both at
            // the center position of the group.
            List<Tuple<double, double>> ranges = new List<Tuple<double, double>>();
            foreach (var run in runs)
            {
                var (start, runLen) = run;
                double left, right;
                if (runLen <= W)
                {
                    left = right = start + runLen / 2.0;
                }
                else
                {
                    left = start + HalfWidth;
                    right = start + runLen - 1 - HalfWidth;
                }
                // Convert to input space.
                double inMin, inMax;
                if (!Periodic)
                {
                    inMin = (left - Padding) * Resolution + MinVal;
                    inMax = (right - Padding) * Resolution + MinVal;
                }
                else
                {
                    inMin = (left - Padding) * Range / nInternal + MinVal;
                    inMax = (right - Padding) * Range / nInternal + MinVal;
                }

                // Handle wrap-around if periodic
                if (Periodic)
                {
                    if (inMin >= MaxVal)
                    {
                        inMin -= Range;
                        inMax -= Range;
                    }
                }

                // Clip low end
                if (inMin < MinVal)
                {
                    inMin = MinVal;
                }
                if (inMax < MinVal)
                {   
                    inMax = MinVal;
                }

                // If we have a periodic encoder, and the max is past the edge, break into
                // 2 separate ranges
                if (Periodic && inMax >= MaxVal)
                {
                    ranges.Add(new Tuple<double, double>(inMin, MaxVal));
                    ranges.Add(new Tuple<double, double>(MinVal, inMax - Range));
                }
                else
                {
                    if (inMax > MaxVal)
                    {
                        inMax = MaxVal;
                    }
                    if (inMin > MaxVal)
                    {
                        inMin = MaxVal;
                    }
                    ranges.Add(new Tuple<double, double>(inMin, inMax));
                }
            }

            var desc = GenerateRangeDescription(ranges);
            // Return result
            if (!string.IsNullOrEmpty(parentFieldName))
            {
                fieldName = $"{parentFieldName}.{Name}";
            }
            else
            {
                fieldName = Name;
            }
            return (new Dictionary<string, Tuple<List<Tuple<double, double>>, string>>()
            {
            { fieldName, new Tuple<List<Tuple<double, double>>, string>(ranges, desc) }
            }, new List<string> { fieldName });
        }


        /*
                public int? GetBucketIndex(object inputData)
                {
                    double input = Convert.ToDouble(inputData, CultureInfo.InvariantCulture);
                    if (input == double.NaN)
                    {
                        return null;
                    }

                    int? bucketVal = GetFirstOnBit(input);

                    return bucketVal;
                }
               */

            /// <summary>
            /// The Encode
            /// </summary>
            /// <param name="inputData">The inputData<see cref="object"/></param>
            /// <returns>The <see cref="int[]"/></returns>
        public override int[] Encode(object inputData)
        {
            int[] output = null;

            double input = Convert.ToDouble(inputData, CultureInfo.InvariantCulture);
            if (input == double.NaN)
            {
                return output;
            }

            int? bucketVal = GetFirstOnBit(input);
            if (bucketVal != null)
            {
                output = new int[N];

                int bucketIdx = bucketVal.Value;
                //Arrays.fill(output, 0);
                int minbin = bucketIdx;
                int maxbin = minbin + 2 * HalfWidth;
                if (Periodic)
                {
                    if (maxbin >= N)
                    {
                        int bottombins = maxbin - N + 1;
                        int[] range = ArrayUtils.Range(0, bottombins);
                        ArrayUtils.SetIndexesTo(output, range, 1);
                        maxbin = N - 1;
                    }
                    if (minbin < 0)
                    {
                        int topbins = -minbin;
                        ArrayUtils.SetIndexesTo(output, ArrayUtils.Range(N - topbins, N), 1);
                        minbin = 0;
                    }
                }

                ArrayUtils.SetIndexesTo(output, ArrayUtils.Range(minbin, maxbin + 1), 1);
            }

            // Output 1-D array of same length resulted in parameter N    
            return output;
        }
            







        /// <summary>
        /// This method enables running in the network.
        /// </summary>
        /// <param name="inputData"></param>
        /// <param name="learn"></param>
        /// <returns></returns>
        public int[] Compute(object inputData, bool learn)
        {
            return Encode(inputData);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns>The <see cref="List{T}"/></returns>
        public override List<T> GetBucketValues<T>()
        {
            throw new NotImplementedException();
        }

        //public static object Deserialize<T>(StreamReader sr, string name)
        //{
        //    var excludeMembers = new List<string> { nameof(ScalarEncoder.Properties) };
        //    return HtmSerializer2.DeserializeObject<T>(sr, name, excludeMembers);
        //}
    }
}