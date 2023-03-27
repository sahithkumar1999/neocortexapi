// Copyright (c) Damir Dobric. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
using NeoCortexApi.Entities;
using NeoCortexApi.Utility;
using NeoCortexEntities.NeuroVisualizer;
using Newtonsoft.Json.Linq;
using System;
using System.Buffers.Text;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace NeoCortexApi.Encoders
{


    /// <summary>
    /// Defines the <see cref="ScalarEncoderExperimental" />
    /// </summary>
    public class ScalarEncoder : EncoderBase
    {
        private int v1;
        private int v2;
        private int v3;
        private bool v4;

        /// <summary>
        /// Gets a value indicating whether IsDelta
        /// </summary>
        public override bool IsDelta => throw new NotImplementedException();

        /// <summary>
        /// Gets the Width
        /// </summary>
        public override int Width => throw new NotImplementedException();

        public int NumBits { get; private set; }
        public double PeriodicRadius { get; private set; }
        public double BucketWidth { get; private set; }
        public int NumBuckets { get; private set; }
        public double[] Centers { get; private set; }


        public ScalarEncoder(double minValue, double maxValue, int numBits, double period = 0, double periodicRadius = 0)
        {
            BucketWidth = (maxValue - minValue) / (numBits - (period > 0 ? 2 : 1));
            this.NumBuckets = numBits - (period > 0 ? 1 : 0);
            this.NumBits = numBits;
            this.PeriodicRadius = periodicRadius;

            if (period > 0)
            {
                // Calculate the centers for a periodic encoder
                this.Centers = new double[this.NumBuckets];
                double halfWidth = this.BucketWidth / 2.0;
                double periodOffset = period / 2.0;
                for (int i = 0; i < this.NumBuckets; i++)
                {
                    double center = minValue + halfWidth + i * this.BucketWidth;
                    this.Centers[i] = ((center + periodOffset) % period) - periodOffset;
                }
            }
            else
            {
                // Calculate the centers for a non-periodic encoder
                this.Centers = new double[this.NumBuckets];
                double halfWidth = this.BucketWidth / 2.0;
                for (int i = 0; i < this.NumBuckets; i++)
                {
                    this.Centers[i] = minValue + halfWidth + i * this.BucketWidth;
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ScalarEncoderExperimental"/> class.
        /// </summary>
        /// <param name="encoderSettings">The encoderSettings<see cref="Dictionary{string, object}"/></param>
        public ScalarEncoder(Dictionary<string, object> encoderSettings)
        {
            this.Initialize(encoderSettings);
        }

        public ScalarEncoder(int v1, int v2, int v3, bool v4)
        {
            this.v1 = v1;
            this.v2 = v2;
            this.v3 = v3;
            this.v4 = v4;
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

        /// <summary>
        /// This method initializes the encoder with the given parameters, such as w, minVal, maxVal, n, radius, and resolution.
        ///It checks if the encoder is already initialized and if minVal and maxVal are valid.
        ///If N is not set, it calculates N based on the given parameters and sets the Range and Resolution values accordingly.
        /// </summary>
        /// <param name="w"></param>
        /// <param name="minVal"></param>
        /// <param name="maxVal"></param>
        /// <param name="n"></param>
        /// <param name="radius"></param>
        /// <param name="resolution"></param>
        /// <exception cref="ArgumentException"></exception>
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
        /// This method decodes an array of outputs based on the provided parameters, and returns an array of decoded inputs.
        ///The decoding process involves identifying the runs of 1s in the output array, and mapping those runs to ranges of input 
        ///values based on the specified minimum and maximum values.
        ///If the periodic parameter is set to true, the decoded input array is checked for periodicity and adjusted if necessary.
        /// </summary>
        /// <param name="output"></param>
        /// <param name="minVal"></param>
        /// <param name="maxVal"></param>
        /// <param name="n"></param>
        /// <param name="w"></param>
        /// <param name="periodic"></param>
        /// <returns></returns>
        public static int[] decode(int[] output, int minVal, int maxVal, int n, double w, bool periodic)
        {
            List<int[]> runs = new List<int[]>();
            int start = -1;
            int prev = 0;
            int count = 0;
            for (int i = 0; i < output.Length; i++)
            {
                if (output[i] == 0)
                {
                    if (start != -1)
                    {
                        runs.Add(new int[] { start, prev, count });
                        start = -1;
                        count = 0;
                    }
                }
                else
                {
                    if (start == -1)
                    {
                        start = i;
                    }
                    prev = i;
                    count++;
                }
            }
            if (start != -1)
            {
                runs.Add(new int[] { start, prev, count });
            }
            if (periodic && runs.Count > 1)
            {
                int[] first = runs[0];
                int[] last = runs[runs.Count - 1];
                if (first[0] == 0 && last[1] == output.Length - 1)
                {
                    first[1] = last[1];
                    first[2] += last[2];
                    runs.RemoveAt(runs.Count - 1);
                }
            }
            List<int> input = new List<int>();
            foreach (int[] run in runs)
            {
                int left = (int)Math.Floor(run[0] + 0.5 * (run[2] - w));
                int right = (int)Math.Floor(run[1] - 0.5 * (run[2] - w));
                if (left < 0 && periodic)
                {
                    left += output.Length;
                    right += output.Length;
                }
                for (int i = left; i <= right; i++)
                {
                    int val = (int)Math.Round(map(i, 0, output.Length - 1, minVal, maxVal));
                    if (periodic)
                    {
                        val = wrap(val, minVal, maxVal);
                    }
                    if (val >= minVal && val <= maxVal)
                    {
                        input.Add(val);
                    }
                }
            }
            input.Sort();
            if (periodic && input.Count > 0)
            {
                int max = input[input.Count - 1];
                if (max > maxVal)
                {
                    List<int> input2 = new List<int>();
                    foreach (int val in input)
                    {
                        if (val <= maxVal)
                        {
                            input2.Add(val);
                        }
                    }
                    input = input2;
                    input2 = new List<int>();
                    foreach (int val in input)
                    {
                        if (val >= minVal)
                        {
                            input2.Add(val);
                        }
                    }
                    input = input2;
                }
            }
            return input.ToArray();
        }




        /// <summary>
        /// This method encodes the input into an array of active bits using a scalar encoder
        /// It takes into account both periodic and non-periodic encoders
        /// The active bits are set based on the bucket index calculated for the input value
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        // EncodeIntoArray method
        public int[] EncodeIntoArray(double input)
        {
            int[] activeBits = new int[this.NumBits];

            if (this.PeriodicRadius > 0)
            {
                // Calculate the bucket index for a periodic encoder
                int bucketIndex = -1;
                for (int i = 0; i < this.NumBuckets; i++)
                {
                    if (Math.Abs(input - this.Centers[i]) <= this.PeriodicRadius)
                    {
                        bucketIndex = i;
                        break;
                    }
                }

                // Set active bits
                if (bucketIndex != -1)
                {
                    int startBit = bucketIndex * (this.NumBits / this.NumBuckets);
                    int endBit = startBit + (this.NumBits / this.NumBuckets) - 1;
                    if (endBit < activeBits.Length) // Check if endBit is within the bounds of the array
                    {
                        for (int i = startBit; i <= endBit; i++)
                        {
                            activeBits[i] = 1;
                        }
                    }
                }
            }
            else
            {
                // Calculate the bucket index for a non-periodic encoder
                int bucketIndex = (int)Math.Floor((input - (this.Centers[0] - this.BucketWidth / 2.0)) / this.BucketWidth);

                // Set active bits
                if (bucketIndex >= 0 && bucketIndex < this.NumBuckets)
                {
                    int startBit = bucketIndex;
                    int endBit = startBit + (this.NumBits / this.NumBuckets) - 1;
                    if (endBit < activeBits.Length) // Check if endBit is within the bounds of the array
                    {
                        for (int i = startBit; i <= endBit; i++)
                        {
                            activeBits[i] = 1;
                        }
                    }
                }
            }
            return activeBits;
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


        public int[] GetBucketInfo(double input)
        {
            // Clip input to range
            if (input < MinVal)
            {
                input = MinVal;
            }
            else if (input > MaxVal)
            {
                input = MaxVal;
            }

            // Calculate bucket index
            double bucketWidth = (MaxVal - MinVal) / N;
            int bucketIndex = (int)((input - MinVal) / bucketWidth);

            // Calculate bucket center
            double bucketCenter = MinVal + (bucketIndex + 0.5) * bucketWidth;

            // Calculate bucket bounds
            double bucketStart = MinVal + bucketIndex * bucketWidth;
            double bucketEnd = MinVal + (bucketIndex + 1) * bucketWidth;

            // Handle periodic encoding
            if (Periodic)
            {
                // Wrap bucket index
                if (bucketIndex < 0)
                {
                    bucketIndex += N;
                }
                else if (bucketIndex >= N)
                {
                    bucketIndex -= N;
                }

                // Calculate distance to nearest edge
                double distToStart = input - bucketStart;
                double distToEnd = bucketEnd - input;

                if (distToStart < 0)
                {
                    distToStart += MaxVal - MinVal;
                }
                if (distToEnd < 0)
                {
                    distToEnd += MaxVal - MinVal;
                }

                // Choose the closest edge as bucket center
                if (distToStart < distToEnd)
                {
                    bucketCenter = bucketStart;
                }
                else
                {
                    bucketCenter = bucketEnd;
                }
            }

            return new int[] { bucketIndex, (int)Math.Round(bucketCenter), (int)Math.Round(bucketStart), (int)Math.Round(bucketEnd) };
        }

        /// <summary>
        /// This method takes a list of ranges and returns a string that describes them.
        ///It iterates through the list of ranges and constructs the string by appending each range's start and end values.
        ///If the start and end values of a range are the same, it only appends the start value to the string.
        /// </summary>
        /// <param name="ranges"></param>
        /// <returns></returns>
        public string GenerateRangeDescription(List<Tuple<double, double>> ranges)
        {
            var desc = "";
            var numRanges = ranges.Count;
            for (var i = 0; i < numRanges; i++)
            {
                if (ranges[i].Item1 != ranges[i].Item2)
                {
                    desc += $"{ranges[i].Item1:F2}-{ranges[i].Item2:F2}";
                }
                else
                {
                    desc += $"{ranges[i].Item1:F2}";
                }

                if (i < numRanges - 1)
                {
                    desc += ", ";
                }
            }

            return desc;
        }

        private string DecodedToStr(Tuple<Dictionary<string, Tuple<List<int>, string>>, List<string>> tuple)
        {
            throw new NotImplementedException();
        }

        private void PPrint(double[] output)
        {
            throw new NotImplementedException();
        }

        private Tuple<Dictionary<string, Tuple<List<int>, string>>, List<string>> Decode(double[] output)
        {
            throw new NotImplementedException();
        }


        /// <summary>
        /// This method encodes an input value using the Scalar Encoder algorithm and returns an integer array as the output.
        ///The input value is first converted to a double using the CultureInfo.InvariantCulture format.
        ///The method checks if the input value is NaN and returns null if it is, otherwise it proceeds with encoding the value 
        ///into an integer array using the Scalar Encoder algorithm.
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
        /// This method calculates the closeness score between two sets of scalar values, expValues and actValues.
        ///The method takes an optional boolean parameter 'fractional', which if set to true, calculates the closeness 
        ///score as a fraction of the possible range of values.
        ///The method returns an array containing the calculated closeness score.
        /// </summary>
        /// <param name="expValues"></param>
        /// <param name="actValues"></param>
        /// <param name="fractional"></param>
        /// <returns></returns>
        public double[] ClosenessScores(double[] expValues, double[] actValues, bool fractional = true)
        {
            double expValue = expValues[0];
            double actValue = actValues[0];
            double err;

            if (Periodic)
            {
                expValue = expValue % MaxVal;
                actValue = actValue % MaxVal;
                err = Math.Min(Math.Abs(expValue - actValue), MaxVal - Math.Abs(expValue - actValue));
            }
            else
            {
                err = Math.Abs(expValue - actValue);
            }

            double closeness;
            if (fractional)
            {
                double range = (MaxVal - MinVal) + (ClipInput ? 0 : (2 * (MaxVal - MinVal) / (N - 1)));
                double pctErr = err / range;
                pctErr = Math.Min(1.0, pctErr);
                closeness = 1.0 - pctErr;
            }
            else
            {
                closeness = err;
            }

            return new double[] { closeness };
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


        /// <summary>
        /// This method calculates the lower and upper bounds of a given input value in a range of values.
        ///It throws an exception if the input value is outside the encoder's range or is not a valid number.
        ///It also prints the bucket width, bucket index, bucket lower bound, and bucket upper bound to the console.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public double[] GetBucketValues(double input)
        {
            // Check for edge cases
            if (double.IsNaN(input) || double.IsInfinity(input))
            {
                throw new ArgumentException("Input value is not a valid number.");
            }
            if (input < this.MinVal || input >= this.MaxVal)
            {
                throw new ArgumentException("Input value is outside of the encoder's range.");
            }
            NumBuckets = 100;
            // Calculate the width of each bucket
            double bucketWidth = (this.MaxVal - this.MinVal) / (double)this.NumBuckets;
            if (double.IsInfinity(bucketWidth) || double.IsNaN(bucketWidth) || bucketWidth <= 0.0)
            {
                throw new InvalidOperationException("Bucket width is not valid.");
            }

            Console.WriteLine("bucketWidth: " + bucketWidth);

            // Calculate the index of the bucket that the input falls into
            int bucketIndex = (int)((input - this.MinVal) / bucketWidth);
            Console.WriteLine("bucketIndex: " + bucketIndex);

            // Calculate the lower and upper bounds of the bucket
            double bucketLowerBound = bucketIndex * bucketWidth + this.MinVal;
            Console.WriteLine("bucketLowerBound: " + bucketLowerBound);

            double bucketUpperBound = (bucketIndex + 1) * bucketWidth + this.MinVal;
            Console.WriteLine("bucketUpperBound: " + bucketUpperBound);

            // Return the bucket values
            return new double[] { bucketLowerBound, bucketUpperBound };
        }
    }
}