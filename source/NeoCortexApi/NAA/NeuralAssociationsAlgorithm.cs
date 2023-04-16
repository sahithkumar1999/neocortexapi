﻿using NeoCortexApi.Entities;
using NeoCortexApi.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Security;
using System.Text;
using System.Threading;
using static NeoCortexApi.TemporalMemory;



namespace NeoCortexApi
{
    /// <summary>
    /// See PhD Chapter Neural Associations Algorithm.
    /// </summary>
    public class NeuralAssociationAlgorithm
    {
        /// <summary>
        /// Every area has its own cycle results. Cellst that are acrive from the point of view of area1 might be different than 
        /// from the point of view of area2.
        /// </summary>
        //private Dictionary<string, ComputeCycle> _cycleResults;

        private int _iteration;

        private CorticalArea area;

        private HtmConfig _cfg;

        private Random _rnd;

        /// <summary>
        /// Stores each cycle's most recent activity
        /// </summary>
        public SegmentActivity LastActivity { get; set; }

        /// <summary>
        /// Get active segments.
        /// </summary>
        protected List<ApicalDendrite> ActiveApicalSegments
        {
            get
            {
                return GetSegmentsOfActiveCells(_cfg.ActivationThreshold, int.MaxValue);
            }
        }

        /// <summary>
        /// Get matching segments.
        /// </summary>
        protected List<ApicalDendrite> MatchingApicalSegments
        {
            get
            {
                return GetSegmentsOfActiveCells(_cfg.MinThreshold, _cfg.ActivationThreshold);
            }
        }

        /// <summary>
        /// Get segments that are neither matching nor active.
        /// </summary>
        protected List<ApicalDendrite> InactiveApicalSegments
        {
            get
            {
                return GetSegmentsOfActiveCells(0, _cfg.MinThreshold);
            }
        }


        ///// <summary>
        ///// Gets active segments of active cells in the computing (this) area.
        ///// </summary>
        //public List<ApicalDendrite> ActiveApicalSegmentsOfActiveCells
        //{
        //    get
        //    {
        //        var indiciesOfActCells = this.area.ActiveCells.Select(c => c.Index);
        //        return ActiveApicalSegments.Where(s => indiciesOfActCells.Contains(s.ParentCell.Index)).ToList();
        //    }
        //}


        ///// <summary>
        ///// Gets active segments of matching cells in the computing (this) area.
        ///// </summary>
        //public List<ApicalDendrite> MatchingApicalSegmentsOfActiveCells
        //{
        //    get
        //    {
        //        var indiciesOfActCells = this.area.ActiveCells.Select(c => c.Index);
        //        return MatchingApicalSegments.Where(s => indiciesOfActCells.Contains(s.ParentCell.Index)).ToList();
        //    }
        //}

        ///// <summary>
        ///// Gets incative segments of active cells in the computing (this) area.
        ///// </summary>
        //public List<ApicalDendrite> InactiveApicalSegmentsOfActiveCells
        //{
        //    get
        //    {
        //        var indiciesOfActCells = this.area.ActiveCells.Select(c => c.Index);
        //        return InactiveApicalSegments.Where(s => indiciesOfActCells.Contains(s.ParentCell.Index)).ToList();
        //    }
        //}


        private List<ApicalDendrite> GetSegmentsOfActiveCells(int thresholdMin, int thresholdMax)
        {
            List<ApicalDendrite> matchSegs = new List<ApicalDendrite>();

            foreach (var cell in this.area.ActiveCells)
            {
                foreach (var seg in cell.ApicalDendrites)
                {
                    if (seg.NumConnectedSynapses >= thresholdMin && seg.NumConnectedSynapses < thresholdMax)
                        matchSegs.Add(seg);
                }
            }

            return matchSegs;
        }

        public NeuralAssociationAlgorithm(HtmConfig cfg, CorticalArea area, Random random = null)
        {
            this._cfg = cfg;
            this.area = area;
            if (random == null)
            {
                this._rnd = new Random();
            }
            else
                _rnd = random;
        }

        public ComputeCycle Compute(CorticalArea associatedArea, bool learn)
        {
            return Compute(new CorticalArea[] { associatedArea }, learn);
        }

        public ComputeCycle Compute(CorticalArea[] associatedAreas, bool learn)
        {
            foreach (var area in associatedAreas)
            {
                //if (!_cycleResults.ContainsKey(area.Name))
                //    _cycleResults.Add(area.Name, new ComputeCycle());

                ActivateCells(area, learn: learn);

                _iteration++;
            }

            return null;
        }

        protected virtual void ActivateCells(CorticalArea associatedArea, bool learn)
        {
            ComputeCycle newComputeCycle = new ComputeCycle
            {
                ActivColumnIndicies = null,
            };

            double permanenceIncrement = this._cfg.PermanenceIncrement;
            double permanenceDecrement = this._cfg.PermanenceDecrement;

            AdaptActiveSegments(associatedArea.ActiveCells, learn, this._cfg.PermanenceIncrement, this._cfg.PermanenceDecrement);

            // In HTM instead of associatedArea.ActiveCells, WinnerCells are used.
            // Because there is curretnly no temporal dependency in the NAA
            AdaptMatchingSegments(associatedArea.ActiveCells, learn, this._cfg.PermanenceIncrement, this._cfg.PermanenceDecrement);

            AdaptIncativeSegments(associatedArea, learn, this._cfg.PermanenceIncrement, this._cfg.PermanenceDecrement);

        }



        /// <summary>
        /// TM activated segments on the column in the previous cycle. This method locates such segments and 
        /// adapts them and return owner cells of active segments.
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="activeSegments">Active segments as calculated (activated) in the previous step.</param>
        /// <param name="matchingSegments"></param>
        /// <param name="associatingCells">Cells active in the current cycle.</param>
        /// <param name="prevWinnerCells"></param>
        /// <param name="permanenceIncrement"></param>
        /// <param name="permanenceDecrement"></param>
        /// <param name="learn"></param>
        /// <returns>Cells which owns active column segments as calculated in the previous step.</returns>
        protected void AdaptActiveSegments(ICollection<Cell> associatingCells, bool learn, double permanenceIncrement, double permanenceDecrement)
        {
            // List of cells that owns active segments. These cells will be activated in this cycle.
            // In previous cycle they are depolarized.
            List<Cell> cellsOwnersOfActiveSegments = new List<Cell>();

            foreach (Segment segment in ActiveApicalSegments)
            {
                if (!cellsOwnersOfActiveSegments.Contains(segment.ParentCell))
                {
                    cellsOwnersOfActiveSegments.Add(segment.ParentCell);
                }

                if (learn)
                {
                    AdaptSegment(segment, associatingCells, permanenceIncrement, permanenceDecrement);

                    //
                    // Even if the segment is active, new synapses can be added that connect previously active cells with the segment.
                    int nGrowDesired = this._cfg.MaxNewSynapseCount - segment.Synapses.Count;

                    if (nGrowDesired > 0)
                    {
                        // Create new synapses on the segment from winner (pre-synaptic cells) cells.
                        GrowSynapses(associatingCells, segment, this._cfg.InitialPermanence,
                            nGrowDesired, this._cfg.MaxSynapsesPerSegment, this._cfg.Random);
                    }
                    else
                    {
                        // Segment has already maximum number of synapses.
                        // for debugging.
                    }
                }
            }
        }

        private void AdaptMatchingSegments(ICollection<Cell> associatingCells, bool learn, double permanenceIncrement, double permanenceDecrement)
        {
            // List of cells that owns active segments. These cells will be activated in this cycle.
            // In previous cycle they are depolarized.
            List<Cell> cellsOwnersOfActiveSegments = new List<Cell>();

            //
            // Matching segments result from number of potential synapses. These are segments with number of potential
            // synapses permanence higher than some minimum threshold value.
            // Potential synapses are synapses from presynaptc cells connected to the active cell.
            // In other words, synapse permanence between presynaptic cell and the active cell defines a statistical prediction that active cell will become the active in the next cycle.
            // Bursting will create new segments if there are no matching segments until some matching segments appear. 
            // Once that happen, segment adoption will start.
            // If some matching segments exist, bursting will grab the segment with most potential synapses and adapt it.
           // foreach (var matchSeg in MatchingApicalSegmentsOfActiveCells)
            {
                Segment maxPotentialSeg = HtmCompute.GetSegmentWithHighesPotential(MatchingApicalSegments.ToArray());

                if (maxPotentialSeg == null)
                    return;

                if (learn)
                {
                    AdaptSegment(maxPotentialSeg, associatingCells, permanenceIncrement, permanenceDecrement);

                    int nGrowDesired = this._cfg.MaxNewSynapseCount - this.LastActivity.PotentialSynapses[maxPotentialSeg.SegmentIndex];

                    if (nGrowDesired > 0)
                    {
                        GrowSynapses(associatingCells, maxPotentialSeg, this._cfg.InitialPermanence, nGrowDesired, this._cfg.MaxSynapsesPerSegment, _rnd);
                    }
                }
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="associatedArea"></param>
        /// <param name="learn"></param>
        /// <param name="permanenceIncrement"></param>
        /// <param name="permanenceDecrement"></param>
        /// <remarks>PHD ref: Algorithm 12 - Line 19-26.</remarks>
        private void AdaptIncativeSegments(CorticalArea associatedArea, bool learn, double permanenceIncrement, double permanenceDecrement)
        {
            // Lookup the cell with the lowest number of synapses in the area.
            var leastUsedPotentialCell = HtmCompute.GetLeastUsedCell(this.area.ActiveCells, _rnd);

            //foreach (var inactiveSeg in InactiveApicalSegmentsOfActiveCells)
            {
                if (learn)
                {
                    // This is why we substract number of winner cells from the MaxNewSynapseCount.
                    int nGrowExact = Math.Min(this._cfg.MaxNewSynapseCount, associatedArea.ActiveCells.Count);

                    if (nGrowExact > 0)
                    {
                        Segment newSegment;

                        //
                        // We will create distal segments if associating cells are from the same area.
                        // For all cells out of this area apical segments will be created.
                        if (leastUsedPotentialCell.ParentAreaName == associatedArea.ActiveCells.First().ParentAreaName)
                            newSegment = CreateDistalSegment(leastUsedPotentialCell);
                        else
                            newSegment = CreateApicalSegment(leastUsedPotentialCell);

                        GrowSynapses(associatedArea.ActiveCells, newSegment, this._cfg.InitialPermanence, nGrowExact, this._cfg.MaxSynapsesPerSegment, _rnd);
                    }
                }
            }
        }



        //protected BurstingResult BurstArea(CorticalArea area, List<Segment> matchingSegments,
        // ICollection<Cell> prevActiveCells, ICollection<Cell> prevWinnerCells, double permanenceIncrement, double permanenceDecrement,
        //     Random random, bool learn)
        //{
        //    IList<Cell> cells = area.Cells;
        //    Cell leastUsedOrMaxPotentialCell = null;

        //    //
        //    // Matching segments result from number of potential synapses. These are segments with number of potential
        //    // synapses permanence higher than some minimum threshold value.
        //    // Potential synapses are synapses from presynaptc cells connected to the active cell.
        //    // In other words, synapse permanence between presynaptic cell and the active cell defines a statistical prediction that active cell will become the active in the next cycle.
        //    // Bursting will create new segments if there are no matching segments until some matching segments appear. 
        //    // Once that happen, segment adoption will start.
        //    // If some matching segments exist, bursting will grab the segment with most potential synapses and adapt it.
        //    if (matchingSegments != null && matchingSegments.Count > 0)
        //    {
        //        // Debug.Write($"B.({matchingSegments.Count})");

        //        Segment maxPotentialSeg = HtmCompute.GetSegmentwithHighesPotential(matchingSegments, prevActiveCells, this.LastActivity.PotentialSynapses);

        //        leastUsedOrMaxPotentialCell = maxPotentialSeg.ParentCell;

        //        if (learn)
        //        {
        //            AdaptSegment(maxPotentialSeg, prevActiveCells, permanenceIncrement, permanenceDecrement);

        //            int nGrowDesired = this._cfg.MaxNewSynapseCount - this.LastActivity.PotentialSynapses[maxPotentialSeg.SegmentIndex];

        //            if (nGrowDesired > 0)
        //            {
        //                GrowSynapses(prevWinnerCells, maxPotentialSeg, this._cfg.InitialPermanence, nGrowDesired, this._cfg.MaxSynapsesPerSegment, random);
        //            }
        //        }
        //    }
        //    else
        //    {
        //        // Debug.Write("B.0");

        //        leastUsedOrMaxPotentialCell = HtmCompute.GetLeastUsedCell(cells, random);
        //        if (learn)
        //        {
        //            // This can be optimized. Right now, we assume that every winner cell has a single synaptic connection to the segment.
        //            // This is why we substract number of cells from the MaxNewSynapseCount.
        //            int nGrowExact = Math.Min(this._cfg.MaxNewSynapseCount, prevWinnerCells.Count);
        //            if (nGrowExact > 0)
        //            {
        //                Segment newSegment;
        //                if (leastUsedOrMaxPotentialCell.ParentAreaName == prevWinnerCells.First().ParentAreaName)
        //                    newSegment = CreateDistalSegment(leastUsedOrMaxPotentialCell);
        //                else
        //                    newSegment = CreateDistalSegment(leastUsedOrMaxPotentialCell);//apical

        //                GrowSynapses(prevWinnerCells, newSegment, this._cfg.InitialPermanence, nGrowExact, this._cfg.MaxSynapsesPerSegment, random);
        //            }
        //        }
        //    }

        //    return new BurstingResult(cells, leastUsedOrMaxPotentialCell);
        //}


        /// <summary>
        /// Used internally to return the least recently activated segment on the specified cell
        /// </summary>
        /// <param name="cell">cell to search for segments on.</param>
        /// <returns>the least recently activated segment on the specified cell.</returns>
        private static Segment GetLeastRecentlyUsedSegment(Segment[] segments)
        {

            Segment minSegment = null;
            long minIteration = long.MaxValue;

            foreach (Segment dd in segments)
            {
                if (dd.LastUsedIteration < minIteration)
                {
                    minSegment = dd;
                    minIteration = dd.LastUsedIteration;
                }
            }

            return minSegment;
        }


        /// <summary>
        /// Adds a new <see cref="Segment"/> segment on the specified <see cref="Cell"/>, or reuses an existing one.
        /// </summary>
        /// <param name="segmentParentCell">the Cell to which a segment is added.</param>
        /// <returns>the newly created segment or a reused segment.</returns>
        public ApicalDendrite CreateApicalSegment(Cell segmentParentCell)
        {
            //
            // If there are more segments than maximal allowed number of segments per cell,
            // least used segments will be destroyed.
            while (segmentParentCell.ApicalDendrites.Count >= this._cfg.MaxSegmentsPerCell)
            {
                var lruSegment = GetLeastRecentlyUsedSegment(segmentParentCell.ApicalDendrites.ToArray());
                KillSegment<ApicalDendrite>(lruSegment as ApicalDendrite, segmentParentCell.ApicalDendrites);
            }

            int index = segmentParentCell.DistalDendrites.Count;
            ApicalDendrite segment = new ApicalDendrite(segmentParentCell, index, _iteration, index, this._cfg.SynPermConnected, -1 /* For proximal segments only.*/);
            segmentParentCell.ApicalDendrites.Add(segment);

            return segment;
        }

        public DistalDendrite CreateDistalSegment(Cell segmentParentCell)
        {
            //
            // If there are more segments than maximal allowed number of segments per cell,
            // least used segments will be destroyed.
            while (segmentParentCell.DistalDendrites.Count >= this._cfg.MaxSegmentsPerCell)
            {
                var lruSegment = GetLeastRecentlyUsedSegment(segmentParentCell.DistalDendrites.ToArray());
                KillSegment<DistalDendrite>(lruSegment as DistalDendrite, segmentParentCell.DistalDendrites);
            }

            int index = segmentParentCell.DistalDendrites.Count;
            DistalDendrite segment = new DistalDendrite(segmentParentCell, index, _iteration, index, this._cfg.SynPermConnected, -1 /* For proximal segments only.*/);
            segmentParentCell.DistalDendrites.Add(segment);

            return segment;

        }

        /// <summary>
        /// Increments the permanence of the segment's synapse if the synapse's presynaptic cell was active in the previous cycle.
        /// If it was not active, then it will decrement the permanence value. 
        /// If the permamence is below EPSILON, synapse is destroyed.
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="segment">The segment to adapt.</param>
        /// <param name="prevActiveCells">List of active cells in the current cycle (calculated in the previous cycle).</param>
        /// <param name="permanenceIncrement"></param>
        /// <param name="permanenceDecrement"></param>
        public void AdaptSegment(Segment segment, ICollection<Cell> prevActiveCells,
            double permanenceIncrement, double permanenceDecrement)
        {

            // Destroying a synapse modifies the set that we're iterating through.
            List<Synapse> synapsesToDestroy = new List<Synapse>();

            foreach (Synapse presynapticCellSynapse in segment.Synapses)
            {
                double permanence = presynapticCellSynapse.Permanence;

                //
                // If synapse's presynaptic cell was active in the previous cycle then streng it.
                if (prevActiveCells.Contains(presynapticCellSynapse.GetPresynapticCell()))
                {
                    permanence += permanenceIncrement;
                }
                else
                {
                    permanence -= permanenceDecrement;
                }

                // Keep permanence within min/max bounds
                permanence = permanence < 0 ? 0 : permanence > 1.0 ? 1.0 : permanence;

                // Use this to examine issues caused by subtle floating point differences
                // be careful to set the scale (1 below) to the max significant digits right of the decimal point
                // between the permanenceIncrement and initialPermanence
                //
                // permanence = new BigDecimal(permanence).setScale(1, RoundingMode.HALF_UP).doubleValue(); 

                if (permanence < HtmConfig.EPSILON)
                {
                    synapsesToDestroy.Add(presynapticCellSynapse);
                }
                else
                {
                    presynapticCellSynapse.Permanence = permanence;
                }
            }

            foreach (Synapse syn in synapsesToDestroy)
            {
                segment.KillSynapse(syn);
            }

            if (segment.Synapses.Count == 0)
            {
                KillSegment(segment);
            }
        }


        /// <summary>
        /// Destroys the specified <see cref="Synapse"/> in specific <see cref="Segment"/> segment and in the source cell.
        /// Every synapse instance is stored at two places: The source cell (receptor synapse) and the segment.
        /// </summary>
        /// <param name="synapse">the Synapse to destroy</param>
        /// <param name="segment"></param>
        private static void DestroySynapse(Synapse synapse, Segment segment)
        {
            // lock ("synapses")
            {
                synapse.SourceCell.ReceptorSynapses.Remove(synapse);

                segment.Synapses.Remove(synapse);
            }
        }

        private void KillSegment(Segment segment)
        {
            if (segment.GetType() == typeof(ApicalDendrite))
                KillSegment<ApicalDendrite>(segment as ApicalDendrite, segment.ParentCell.ApicalDendrites);
            if (segment.GetType() == typeof(DistalDendrite))
                KillSegment<DistalDendrite>(segment as DistalDendrite, segment.ParentCell.DistalDendrites);
            else
                throw new ArgumentException($"Unsuproted segment type: {segment.GetType().Name}");
        }

        /// <summary>
        /// Destroys a segment <see cref="Segment"/>
        /// </summary>
        /// <param name="segment">the segment to destroy</param>
        private void KillSegment<TSeg>(TSeg segment, List<TSeg> segments) where TSeg : Segment
        {
            lock ("segmentindex")
            {
                // Remove the synapses from all data structures outside this Segment.
                //DD List<Synapse> synapses = GetSynapses(segment);
                List<Synapse> synapses = segment.Synapses;
                int len = synapses.Count;


                //getSynapses(segment).stream().forEach(s->removeSynapseFromPresynapticMap(s));
                //DD foreach (var s in GetSynapses(segment))
                foreach (var s in segment.Synapses)
                {
                    DestroySynapse(s, segment);
                }

                //m_NumSynapses -= len;


                segments.Remove(segment);
            }
        }



        /// <summary>
        /// Creates nDesiredNewSynapes synapses on the segment passed in if possible, choosing random cells from the previous winner cells that are
        /// not already on the segment.
        /// </summary>
        /// <param name="associatingCells">Winner cells in `t-1`</param>
        /// <param name="segment">Segment to grow synapses on. </param>
        /// <param name="initialPermanence">Initial permanence of a new synapse.</param>
        /// <param name="nDesiredNewSynapses">Desired number of synapses to grow</param>
        /// <param name="random"><see cref="TemporalMemory"/> object used to generate random numbers</param>
        /// <remarks>
        /// <b>Notes:</b> The process of writing the last value into the index in the array that was most recently changed is to ensure the same results that 
        /// we get in the c++ implementation using iter_swap with vectors.
        /// </remarks>
        public static void GrowSynapses(ICollection<Cell> associatingCells, Segment segment,
            double initialPermanence, int nDesiredNewSynapses, int maxSynapsesPerSegment, Random random)
        {

            List<Cell> removingCandidates = new List<Cell>(associatingCells);
            removingCandidates = removingCandidates.OrderBy(c => c).ToList();

            //
            // Enumarates all synapses in a segment and remove winner-cells from
            // list of removingCandidates if they are presynaptic winners cells.
            // So, we will create synapses only from cells, which do not already have synaptic connection to the segment.          
            foreach (Synapse synapse in segment.Synapses)
            {
                int index = removingCandidates.IndexOf(synapse.SourceCell);
                if (index != -1)
                {
                    removingCandidates.RemoveAt(index); ;
                }
            }

            int candidatesLength = removingCandidates.Count;

            // We take here eather wanted growing number of desired synapes or num of candidates
            // if too many growing synapses requested.
            int numMissingSynapses = nDesiredNewSynapses < candidatesLength ? nDesiredNewSynapses : candidatesLength;

            //
            // Finally we randomly create new synapses. 
            for (int i = 0; i < numMissingSynapses; i++)
            {
                int rndIndex = random.Next(removingCandidates.Count);
                CreateSynapse(segment, removingCandidates[rndIndex], initialPermanence, maxSynapsesPerSegment);
                removingCandidates.RemoveAt(rndIndex);
            }
        }

        /// <summary>
        /// Creates a new synapse on a segment.
        /// </summary>
        /// <param name="segment">the <see cref="Segment"/> segment to which a <see cref="Synapse"/> is being created.</param>
        /// <param name="presynapticCell">the source <see cref="Cell"/>.</param>
        /// <param name="permanence">the initial permanence.</param>
        /// <returns>the created <see cref="Synapse"/>.</returns>
        public static Synapse CreateSynapse(Segment segment, Cell presynapticCell, double permanence, int maxSynapsesPerSegment)
        {
            while (segment.Synapses.Count >= maxSynapsesPerSegment)
            {
                DestroySynapse(segment.GetMinPermanenceSynapse(), segment);
            }

            //lock ("synapses")
            {
                Synapse synapse = null;

                segment.Synapses.Add(synapse = new Synapse(presynapticCell, segment.SegmentIndex, segment.Synapses.Count, permanence));

                presynapticCell.ReceptorSynapses.Add(synapse);

                return synapse;
            }
        }       
    }
}
