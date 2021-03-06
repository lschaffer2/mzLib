﻿using Chemistry;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlashLFQ
{
    public class ChromatographicPeak
    {
        public double Intensity;
        public readonly bool IsMbrFeature;
        public readonly SpectraFileInfo RawFileInfo;
        public List<IsotopicEnvelope> IsotopicEnvelopes;
        public double SplitRT;

        public ChromatographicPeak(Identification id, bool isMbrFeature, SpectraFileInfo fileInfo)
        {
            SplitRT = 0;
            NumChargeStatesObserved = 0;
            MassError = double.NaN;
            NumIdentificationsByBaseSeq = 1;
            NumIdentificationsByFullSeq = 1;
            Identifications = new List<Identification>() { id };
            IsotopicEnvelopes = new List<IsotopicEnvelope>();
            IsMbrFeature = isMbrFeature;
            RawFileInfo = fileInfo;
        }

        public IsotopicEnvelope Apex { get; private set; }
        public List<Identification> Identifications { get; private set; }
        public int NumChargeStatesObserved { get; private set; }
        public int NumIdentificationsByBaseSeq { get; private set; }
        public int NumIdentificationsByFullSeq { get; private set; }
        public double MassError { get; private set; }

        public static string TabSeparatedHeader
        {
            get
            {
                var sb = new StringBuilder();
                sb.Append("File Name" + "\t");
                sb.Append("Base Sequence" + "\t");
                sb.Append("Full Sequence" + "\t");
                sb.Append("Protein Group" + "\t");
                sb.Append("Peptide Monoisotopic Mass" + "\t");
                sb.Append("MS2 Retention Time" + "\t");
                sb.Append("Precursor Charge" + "\t");
                sb.Append("Theoretical MZ" + "\t");
                sb.Append("Peak intensity" + "\t");
                sb.Append("Peak RT Start" + "\t");
                sb.Append("Peak RT Apex" + "\t");
                sb.Append("Peak RT End" + "\t");
                sb.Append("Peak MZ" + "\t");
                sb.Append("Peak Charge" + "\t");
                sb.Append("Num Charge States Observed" + "\t");
                sb.Append("Peak Detection Type" + "\t");
                sb.Append("PSMs Mapped" + "\t");
                sb.Append("Base Sequences Mapped" + "\t");
                sb.Append("Full Sequences Mapped" + "\t");
                sb.Append("Peak Split Valley RT" + "\t");
                sb.Append("Peak Apex Mass Error (ppm)");
                return sb.ToString();
            }
        }

        public void CalculateIntensityForThisFeature(bool integrate)
        {
            if (IsotopicEnvelopes.Any())
            {
                double maxIntensity = IsotopicEnvelopes.Max(p => p.Intensity);
                Apex = IsotopicEnvelopes.Where(p => p.Intensity == maxIntensity).First();

                if (integrate)
                {
                    Intensity = IsotopicEnvelopes.Sum(p => p.Intensity);
                }
                else
                {
                    Intensity = Apex.Intensity;
                }

                MassError = Identifications.Min(p => ((ClassExtensions.ToMass(Apex.IndexedPeak.Mz, Apex.ChargeState) - p.monoisotopicMass) / p.monoisotopicMass) * 1e6);
                NumChargeStatesObserved = IsotopicEnvelopes.Select(p => p.ChargeState).Distinct().Count();
            }
            else
            {
                Intensity = 0;
                MassError = double.NaN;
                NumChargeStatesObserved = 0;
                Apex = null;
            }
        }

        public void MergeFeatureWith(IEnumerable<ChromatographicPeak> otherFeatures, bool integrate)
        {
            var thisFeaturesPeaks = this.IsotopicEnvelopes.Select(p => p.IndexedPeak);

            foreach (var otherFeature in otherFeatures)
            {
                if (otherFeature != this)
                {
                    this.Identifications = this.Identifications.Union(otherFeature.Identifications).Distinct().ToList();
                    ResolveIdentifications();
                    this.IsotopicEnvelopes.AddRange(otherFeature.IsotopicEnvelopes.Where(p => !thisFeaturesPeaks.Contains(p.IndexedPeak)));
                    otherFeature.Intensity = -1;
                }
            }
            this.CalculateIntensityForThisFeature(integrate);
        }

        public void ResolveIdentifications()
        {
            this.NumIdentificationsByBaseSeq = Identifications.Select(v => v.BaseSequence).Distinct().Count();
            this.NumIdentificationsByFullSeq = Identifications.Select(v => v.ModifiedSequence).Distinct().Count();
        }

        override public string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(RawFileInfo.FilenameWithoutExtension + "\t");
            sb.Append(string.Join("|", Identifications.Select(p => p.BaseSequence).Distinct()) + '\t');
            sb.Append(string.Join("|", Identifications.Select(p => p.ModifiedSequence).Distinct()) + '\t');

            var t = Identifications.SelectMany(p => p.proteinGroups.Select(v => v.ProteinGroupName)).Distinct().OrderBy(p => p);
            if (t.Any())
                sb.Append(string.Join(";", t) + '\t');
            else
                sb.Append("" + '\t');

            sb.Append("" + Identifications.First().monoisotopicMass + '\t');
            if (!IsMbrFeature)
                sb.Append("" + Identifications.First().ms2RetentionTimeInMinutes + '\t');
            else
                sb.Append("" + '\t');
            sb.Append("" + Identifications.First().precursorChargeState + '\t');
            sb.Append("" + ClassExtensions.ToMz(Identifications.First().monoisotopicMass, Identifications.First().precursorChargeState) + '\t');
            sb.Append("" + Intensity + "\t");

            if (Apex != null)
            {
                sb.Append("" + IsotopicEnvelopes.Select(p => p.RetentionTime).Min() + "\t");
                sb.Append("" + Apex.RetentionTime + "\t");
                sb.Append("" + IsotopicEnvelopes.Select(p => p.RetentionTime).Max() + "\t");

                sb.Append("" + Apex.IndexedPeak.Mz + "\t");
                sb.Append("" + Apex.ChargeState + "\t");
            }
            else
            {
                sb.Append("" + "-" + "\t");
                sb.Append("" + "-" + "\t");
                sb.Append("" + "-" + "\t");

                sb.Append("" + "-" + "\t");
                sb.Append("" + "-" + "\t");
            }

            sb.Append("" + NumChargeStatesObserved + "\t");

            if (IsMbrFeature)
                sb.Append("" + "MBR" + "\t");
            else
                sb.Append("" + "MSMS" + "\t");

            sb.Append("" + Identifications.Count + "\t");
            sb.Append("" + NumIdentificationsByBaseSeq + "\t");
            sb.Append("" + NumIdentificationsByFullSeq + "\t");
            sb.Append("" + SplitRT + "\t");
            sb.Append("" + MassError);

            return sb.ToString();
        }
    }
}