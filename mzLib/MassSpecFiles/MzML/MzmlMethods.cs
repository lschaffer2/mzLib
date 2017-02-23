﻿using MassSpectrometry;
using MzLibUtil;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml.Serialization;

namespace IO.MzML
{
    public static class MzmlMethods
    {

        #region Internal Fields

        internal static readonly XmlSerializer _indexedSerializer = new XmlSerializer(typeof(Generated.indexedmzML));

        #endregion Internal Fields

        #region Private Fields

        private static readonly Dictionary<DissociationType, string> DissociationTypeAccessions = new Dictionary<DissociationType, string>{
            {DissociationType.HCD, "MS:1000422"},
            {DissociationType.CID, "MS:1000133"},
            {DissociationType.Unknown, "MS:1000044"}};

        private static readonly Dictionary<DissociationType, string> DissociationTypeNames = new Dictionary<DissociationType, string>{
            {DissociationType.HCD, "beam-type collision-induced dissociation"},
            {DissociationType.CID, "collision-induced dissociation"},
            {DissociationType.Unknown, "dissociation method"}};

        private static readonly Dictionary<bool, string> CentroidAccessions = new Dictionary<bool, string>{
            {true, "MS:1000127"},
            {false, "MS:1000128"}};

        private static readonly Dictionary<bool, string> CentroidNames = new Dictionary<bool, string>{
            {true, "centroid spectrum"},
            {false, "profile spectrum"}};

        private static readonly Dictionary<Polarity, string> PolarityAccessions = new Dictionary<Polarity, string>{
            {Polarity.Negative, "MS:1000129"},
            {Polarity.Positive, "MS:1000130"}};

        private static readonly Dictionary<Polarity, string> PolarityNames = new Dictionary<Polarity, string>{
            {Polarity.Negative, "negative scan"},
            {Polarity.Positive, "positive scan"}};
        #endregion Private Fields

        #region Public Methods

        public static void CreateAndWriteMyIndexedMZmlwithCalibratedSpectra(IMsDataFile<IMsDataScan<IMzSpectrum<IMzPeak>>> myMsDataFile, string outputFile)
        {
            Generated.indexedmzML _indexedmzMLConnection = new Generated.indexedmzML();
            _indexedmzMLConnection.mzML = new Generated.mzMLType();
            _indexedmzMLConnection.mzML.version = "1";

            _indexedmzMLConnection.mzML.cvList = new Generated.CVListType();
            _indexedmzMLConnection.mzML.cvList.count = "1";
            _indexedmzMLConnection.mzML.cvList.cv = new Generated.CVType[1];
            _indexedmzMLConnection.mzML.cvList.cv[0] = new Generated.CVType();
            _indexedmzMLConnection.mzML.cvList.cv[0].URI = @"https://raw.githubusercontent.com/HUPO-PSI/psi-ms-CV/master/psi-ms.obo";
            _indexedmzMLConnection.mzML.cvList.cv[0].fullName = "Proteomics Standards Initiative Mass Spectrometry Ontology";
            _indexedmzMLConnection.mzML.cvList.cv[0].id = "MS";

            _indexedmzMLConnection.mzML.fileDescription = new Generated.FileDescriptionType();
            _indexedmzMLConnection.mzML.fileDescription.fileContent = new Generated.ParamGroupType();
            _indexedmzMLConnection.mzML.fileDescription.fileContent.cvParam = new Generated.CVParamType[2];
            _indexedmzMLConnection.mzML.fileDescription.fileContent.cvParam[0] = new Generated.CVParamType();
            _indexedmzMLConnection.mzML.fileDescription.fileContent.cvParam[0].accession = "MS:1000579"; // MS1 Data
            _indexedmzMLConnection.mzML.fileDescription.fileContent.cvParam[1] = new Generated.CVParamType();
            _indexedmzMLConnection.mzML.fileDescription.fileContent.cvParam[1].accession = "MS:1000580"; // MSn Data

            _indexedmzMLConnection.mzML.softwareList = new Generated.SoftwareListType();
            _indexedmzMLConnection.mzML.softwareList.count = "1";

            _indexedmzMLConnection.mzML.softwareList.software = new Generated.SoftwareType[1];

            // TODO: add the raw file fields
            _indexedmzMLConnection.mzML.softwareList.software[0] = new Generated.SoftwareType();
            _indexedmzMLConnection.mzML.softwareList.software[0].id = "mzLib";
            _indexedmzMLConnection.mzML.softwareList.software[0].version = "1";
            _indexedmzMLConnection.mzML.softwareList.software[0].cvParam = new Generated.CVParamType[1];
            _indexedmzMLConnection.mzML.softwareList.software[0].cvParam[0] = new Generated.CVParamType();
            _indexedmzMLConnection.mzML.softwareList.software[0].cvParam[0].accession = "MS:1000799";
            _indexedmzMLConnection.mzML.softwareList.software[0].cvParam[0].value = "mzLib";

            // Leaving empty. Can't figure out the configurations.
            // ToDo: read instrumentConfigurationList from mzML file
            _indexedmzMLConnection.mzML.instrumentConfigurationList = new Generated.InstrumentConfigurationListType();

            _indexedmzMLConnection.mzML.dataProcessingList = new Generated.DataProcessingListType();
            // Only writing mine! Might have had some other data processing (but not if it is a raw file)
            // ToDo: read dataProcessingList from mzML file
            _indexedmzMLConnection.mzML.dataProcessingList.count = "1";
            _indexedmzMLConnection.mzML.dataProcessingList.dataProcessing = new Generated.DataProcessingType[1];
            _indexedmzMLConnection.mzML.dataProcessingList.dataProcessing[0] = new Generated.DataProcessingType();
            _indexedmzMLConnection.mzML.dataProcessingList.dataProcessing[0].id = "mzLibProcessing";

            _indexedmzMLConnection.mzML.run = new Generated.RunType();

            // ToDo: Finish the chromatogram writing!
            _indexedmzMLConnection.mzML.run.chromatogramList = new Generated.ChromatogramListType();
            _indexedmzMLConnection.mzML.run.chromatogramList.count = "1";
            _indexedmzMLConnection.mzML.run.chromatogramList.chromatogram = new Generated.ChromatogramType[1];
            _indexedmzMLConnection.mzML.run.chromatogramList.chromatogram[0] = new Generated.ChromatogramType();

            _indexedmzMLConnection.mzML.run.spectrumList = new Generated.SpectrumListType();
            _indexedmzMLConnection.mzML.run.spectrumList.count = (myMsDataFile.NumSpectra).ToString(CultureInfo.InvariantCulture);
            _indexedmzMLConnection.mzML.run.spectrumList.defaultDataProcessingRef = "mzLibProcessing";
            _indexedmzMLConnection.mzML.run.spectrumList.spectrum = new Generated.SpectrumType[myMsDataFile.NumSpectra];

            // Loop over all spectra
            for (int i = 1; i <= myMsDataFile.NumSpectra; i++)
            {
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1] = new Generated.SpectrumType();

                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].defaultArrayLength = myMsDataFile.GetOneBasedScan(i).MassSpectrum.Size;

                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].index = i.ToString(CultureInfo.InvariantCulture);
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].id = myMsDataFile.GetOneBasedScan(i).OneBasedScanNumber.ToString();

                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].cvParam = new Generated.CVParamType[8];

                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].cvParam[0] = new Generated.CVParamType();

                if (myMsDataFile.GetOneBasedScan(i).MsnOrder == 1)
                {
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].cvParam[0].accession = "MS:1000579";
                }
                else if (myMsDataFile.GetOneBasedScan(i) is IMsDataScanWithPrecursor<IMzSpectrum<IMzPeak>>)
                {
                    var scanWithPrecursor = myMsDataFile.GetOneBasedScan(i) as IMsDataScanWithPrecursor<IMzSpectrum<IMzPeak>>;
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].cvParam[0].accession = "MS:1000580";

                    // So needs a precursor!
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].precursorList = new Generated.PrecursorListType();
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].precursorList.count = 1.ToString();
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].precursorList.precursor = new Generated.PrecursorType[1];
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].precursorList.precursor[0] = new Generated.PrecursorType();
                    string precursorID = scanWithPrecursor.OneBasedPrecursorScanNumber.ToString();
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].precursorList.precursor[0].spectrumRef = precursorID;
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].precursorList.precursor[0].selectedIonList = new Generated.SelectedIonListType();
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].precursorList.precursor[0].selectedIonList.count = 1.ToString();
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].precursorList.precursor[0].selectedIonList.selectedIon = new Generated.ParamGroupType[1];
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].precursorList.precursor[0].selectedIonList.selectedIon[0] = new Generated.ParamGroupType();
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].precursorList.precursor[0].selectedIonList.selectedIon[0].cvParam = new Generated.CVParamType[3];
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].precursorList.precursor[0].selectedIonList.selectedIon[0].cvParam[0] = new Generated.CVParamType();
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].precursorList.precursor[0].selectedIonList.selectedIon[0].cvParam[0].name = "selected ion m/z";

                    // Selected ion MZ
                    if (scanWithPrecursor.SelectedIonGuessMonoisotopicMZ.HasValue)
                    {
                        _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].precursorList.precursor[0].selectedIonList.selectedIon[0].cvParam[0] = new Generated.CVParamType();
                        _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].precursorList.precursor[0].selectedIonList.selectedIon[0].cvParam[0].name = "selected ion m/z";
                        _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].precursorList.precursor[0].selectedIonList.selectedIon[0].cvParam[0].value = scanWithPrecursor.SelectedIonGuessMonoisotopicMZ.Value.ToString(CultureInfo.InvariantCulture);
                        _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].precursorList.precursor[0].selectedIonList.selectedIon[0].cvParam[0].accession = "MS:1000744";
                    }

                    // Charge State
                    if (scanWithPrecursor.SelectedIonGuessChargeStateGuess.HasValue)
                    {
                        _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].precursorList.precursor[0].selectedIonList.selectedIon[0].cvParam[1] = new Generated.CVParamType();
                        _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].precursorList.precursor[0].selectedIonList.selectedIon[0].cvParam[1].name = "charge state";
                        _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].precursorList.precursor[0].selectedIonList.selectedIon[0].cvParam[1].value = scanWithPrecursor.SelectedIonGuessChargeStateGuess.Value.ToString(CultureInfo.InvariantCulture);
                        _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].precursorList.precursor[0].selectedIonList.selectedIon[0].cvParam[1].accession = "MS:1000041";
                    }

                    // Selected ion intensity
                    if (scanWithPrecursor.SelectedIonGuessIntensity.HasValue)
                    {
                        _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].precursorList.precursor[0].selectedIonList.selectedIon[0].cvParam[2] = new Generated.CVParamType();
                        _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].precursorList.precursor[0].selectedIonList.selectedIon[0].cvParam[2].name = "peak intensity";
                        double selectedIonGuesssMonoisotopicIntensity = scanWithPrecursor.SelectedIonGuessIntensity.Value;
                        _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].precursorList.precursor[0].selectedIonList.selectedIon[0].cvParam[2].value = selectedIonGuesssMonoisotopicIntensity.ToString(CultureInfo.InvariantCulture);
                        _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].precursorList.precursor[0].selectedIonList.selectedIon[0].cvParam[2].accession = "MS:1000042";
                    }

                    MzRange isolationRange = scanWithPrecursor.IsolationRange;
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].precursorList.precursor[0].isolationWindow = new Generated.ParamGroupType();
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].precursorList.precursor[0].isolationWindow.cvParam = new Generated.CVParamType[3];
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].precursorList.precursor[0].isolationWindow.cvParam[0] = new Generated.CVParamType();
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].precursorList.precursor[0].isolationWindow.cvParam[0].accession = "MS:1000827";
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].precursorList.precursor[0].isolationWindow.cvParam[0].name = "isolation window target m/z";
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].precursorList.precursor[0].isolationWindow.cvParam[0].value = isolationRange.Mean.ToString(CultureInfo.InvariantCulture);
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].precursorList.precursor[0].isolationWindow.cvParam[1] = new Generated.CVParamType();
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].precursorList.precursor[0].isolationWindow.cvParam[1].accession = "MS:1000828";
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].precursorList.precursor[0].isolationWindow.cvParam[1].name = "isolation window lower offset";
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].precursorList.precursor[0].isolationWindow.cvParam[1].value = (isolationRange.Width / 2).ToString(CultureInfo.InvariantCulture);
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].precursorList.precursor[0].isolationWindow.cvParam[2] = new Generated.CVParamType();
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].precursorList.precursor[0].isolationWindow.cvParam[2].accession = "MS:1000829";
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].precursorList.precursor[0].isolationWindow.cvParam[2].name = "isolation window upper offset";
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].precursorList.precursor[0].isolationWindow.cvParam[2].value = (isolationRange.Width / 2).ToString(CultureInfo.InvariantCulture);

                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].precursorList.precursor[0].activation = new Generated.ParamGroupType();
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].precursorList.precursor[0].activation.cvParam = new Generated.CVParamType[1];
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].precursorList.precursor[0].activation.cvParam[0] = new Generated.CVParamType();

                    DissociationType dissociationType = scanWithPrecursor.DissociationType;

                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].precursorList.precursor[0].activation.cvParam[0].accession = DissociationTypeAccessions[dissociationType];
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].precursorList.precursor[0].activation.cvParam[0].name = DissociationTypeNames[dissociationType];
                }

                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].cvParam[1] = new Generated.CVParamType();
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].cvParam[1].name = "ms level";
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].cvParam[1].accession = "MS:1000511";
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].cvParam[1].value = myMsDataFile.GetOneBasedScan(i).MsnOrder.ToString(CultureInfo.InvariantCulture);

                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].cvParam[2] = new Generated.CVParamType();
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].cvParam[2].name = CentroidNames[myMsDataFile.GetOneBasedScan(i).IsCentroid];
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].cvParam[2].accession = CentroidAccessions[myMsDataFile.GetOneBasedScan(i).IsCentroid];

                string polarityName;
                string polarityAccession;
                if (PolarityNames.TryGetValue(myMsDataFile.GetOneBasedScan(i).Polarity, out polarityName) && PolarityAccessions.TryGetValue(myMsDataFile.GetOneBasedScan(i).Polarity, out polarityAccession))
                {
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].cvParam[3] = new Generated.CVParamType();
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].cvParam[3].name = polarityName;
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].cvParam[3].accession = polarityAccession;
                }
                // Spectrum title
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].cvParam[4] = new Generated.CVParamType();
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].cvParam[4].name = "spectrum title";
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].cvParam[4].accession = "MS:1000796";
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].cvParam[4].value = myMsDataFile.GetOneBasedScan(i).OneBasedScanNumber.ToString();

                if ((myMsDataFile.GetOneBasedScan(i).MassSpectrum.Size) > 0)
                {
                    // Lowest observed mz
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].cvParam[5] = new Generated.CVParamType();
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].cvParam[5].name = "lowest observed m/z";
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].cvParam[5].accession = "MS:1000528";
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].cvParam[5].value = myMsDataFile.GetOneBasedScan(i).MassSpectrum.FirstX.ToString(CultureInfo.InvariantCulture);

                    // Highest observed mz
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].cvParam[6] = new Generated.CVParamType();
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].cvParam[6].name = "highest observed m/z";
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].cvParam[6].accession = "MS:1000527";
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].cvParam[6].value = myMsDataFile.GetOneBasedScan(i).MassSpectrum.LastX.ToString(CultureInfo.InvariantCulture);
                }

                // Total ion current
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].cvParam[7] = new Generated.CVParamType();
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].cvParam[7].name = "total ion current";
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].cvParam[7].accession = "MS:1000285";
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].cvParam[7].value = myMsDataFile.GetOneBasedScan(i).TotalIonCurrent.ToString(CultureInfo.InvariantCulture);

                // Retention time
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].scanList = new Generated.ScanListType();
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].scanList.count = "1";
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].scanList.scan = new Generated.ScanType[1];
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].scanList.scan[0] = new Generated.ScanType();
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].scanList.scan[0].cvParam = new Generated.CVParamType[3];
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].scanList.scan[0].cvParam[0] = new Generated.CVParamType();
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].scanList.scan[0].cvParam[0].name = "scan start time";
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].scanList.scan[0].cvParam[0].accession = "MS:1000016";
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].scanList.scan[0].cvParam[0].value = myMsDataFile.GetOneBasedScan(i).RetentionTime.ToString(CultureInfo.InvariantCulture);
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].scanList.scan[0].cvParam[0].unitCvRef = "UO";
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].scanList.scan[0].cvParam[0].unitAccession = "UO:0000031";
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].scanList.scan[0].cvParam[0].unitName = "minute";
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].scanList.scan[0].cvParam[1] = new Generated.CVParamType();
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].scanList.scan[0].cvParam[1].name = "filter string";
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].scanList.scan[0].cvParam[1].accession = "MS:1000512";
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].scanList.scan[0].cvParam[1].value = myMsDataFile.GetOneBasedScan(i).ScanFilter;
                if (myMsDataFile.GetOneBasedScan(i).InjectionTime.HasValue)
                {
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].scanList.scan[0].cvParam[2] = new Generated.CVParamType();
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].scanList.scan[0].cvParam[2].name = "ion injection time";
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].scanList.scan[0].cvParam[2].accession = "MS:1000927";
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].scanList.scan[0].cvParam[2].value = myMsDataFile.GetOneBasedScan(i).InjectionTime.Value.ToString(CultureInfo.InvariantCulture);
                }
                if (myMsDataFile.GetOneBasedScan(i) is IMsDataScanWithPrecursor<IMzSpectrum<IMzPeak>>)
                {
                    var scanWithPrecursor = myMsDataFile.GetOneBasedScan(i) as IMsDataScanWithPrecursor<IMzSpectrum<IMzPeak>>;
                    if (scanWithPrecursor.SelectedIonGuessMonoisotopicMZ.HasValue)
                    {
                        _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].scanList.scan[0].userParam = new Generated.UserParamType[1];
                        _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].scanList.scan[0].userParam[0] = new Generated.UserParamType();
                        _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].scanList.scan[0].userParam[0].name = "[mzLib]Monoisotopic M/Z:";
                        _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].scanList.scan[0].userParam[0].value = scanWithPrecursor.SelectedIonGuessMonoisotopicMZ.Value.ToString(CultureInfo.InvariantCulture);
                    }
                }

                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].scanList.scan[0].scanWindowList = new Generated.ScanWindowListType();
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].scanList.scan[0].scanWindowList.count = 1;
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].scanList.scan[0].scanWindowList.scanWindow = new Generated.ParamGroupType[1];
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].scanList.scan[0].scanWindowList.scanWindow[0] = new Generated.ParamGroupType();
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].scanList.scan[0].scanWindowList.scanWindow[0].cvParam = new Generated.CVParamType[2];
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].scanList.scan[0].scanWindowList.scanWindow[0].cvParam[0] = new Generated.CVParamType();
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].scanList.scan[0].scanWindowList.scanWindow[0].cvParam[0].name = "scan window lower limit";
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].scanList.scan[0].scanWindowList.scanWindow[0].cvParam[0].accession = "MS:1000501";
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].scanList.scan[0].scanWindowList.scanWindow[0].cvParam[0].value = myMsDataFile.GetOneBasedScan(i).ScanWindowRange.Minimum.ToString(CultureInfo.InvariantCulture);
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].scanList.scan[0].scanWindowList.scanWindow[0].cvParam[1] = new Generated.CVParamType();
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].scanList.scan[0].scanWindowList.scanWindow[0].cvParam[1].name = "scan window upper limit";
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].scanList.scan[0].scanWindowList.scanWindow[0].cvParam[1].accession = "MS:1000500";
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].scanList.scan[0].scanWindowList.scanWindow[0].cvParam[1].value = myMsDataFile.GetOneBasedScan(i).ScanWindowRange.Maximum.ToString(CultureInfo.InvariantCulture);

                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList = new Generated.BinaryDataArrayListType();

                // ONLY WRITING M/Z AND INTENSITY DATA, NOT THE CHARGE! (but can add charge info later)
                // CHARGE (and other stuff) CAN BE IMPORTANT IN ML APPLICATIONS!!!!!
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.count = 2.ToString();
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray = new Generated.BinaryDataArrayType[5];

                // M/Z Data
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[0] = new Generated.BinaryDataArrayType();
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[0].binary = myMsDataFile.GetOneBasedScan(i).MassSpectrum.Get64BitXarray();
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[0].encodedLength = (4 * Math.Ceiling(((double)_indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[0].binary.Length / 3))).ToString(CultureInfo.InvariantCulture);
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[0].cvParam = new Generated.CVParamType[2];
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[0].cvParam[0] = new Generated.CVParamType();
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[0].cvParam[0].accession = "MS:1000514";
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[0].cvParam[0].name = "m/z array";
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[0].cvParam[1] = new Generated.CVParamType();
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[0].cvParam[1].accession = "MS:1000523";
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[0].cvParam[1].name = "64-bit float";

                // Intensity Data
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[1] = new Generated.BinaryDataArrayType();
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[1].binary = myMsDataFile.GetOneBasedScan(i).MassSpectrum.Get64BitYarray();
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[1].encodedLength = (4 * Math.Ceiling(((double)_indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[1].binary.Length / 3))).ToString(CultureInfo.InvariantCulture);
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[1].cvParam = new Generated.CVParamType[2];
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[1].cvParam[0] = new Generated.CVParamType();
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[1].cvParam[0].accession = "MS:1000515";
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[1].cvParam[0].name = "intensity array";
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[1].cvParam[1] = new Generated.CVParamType();
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[1].cvParam[1].accession = "MS:1000523";
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[1].cvParam[1].name = "64-bit float";

                if (myMsDataFile.GetOneBasedScan(i).NoiseData != null)
                {
                    // mass
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[2] = new Generated.BinaryDataArrayType();
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[2].binary = myMsDataFile.GetOneBasedScan(i).Get64BitNoiseDataMass();
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[2].encodedLength = (4 * Math.Ceiling(((double)_indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[2].binary.Length / 3))).ToString(CultureInfo.InvariantCulture);
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[2].cvParam = new Generated.CVParamType[2];
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[2].cvParam[0] = new Generated.CVParamType();
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[2].cvParam[0].accession = "MS:1000786";
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[2].cvParam[0].name = "non-standard data array";
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[2].cvParam[1] = new Generated.CVParamType();
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[2].cvParam[1].accession = "MS:1000523";
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[2].cvParam[1].name = "64-bit float";
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[2].userParam = new Generated.UserParamType[1];
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[2].userParam[0] = new Generated.UserParamType();
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[2].userParam[0].name = "custom mass array from noise data";

                    // noise
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[3] = new Generated.BinaryDataArrayType();
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[3].binary = myMsDataFile.GetOneBasedScan(i).Get64BitNoiseDataNoise();
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[3].encodedLength = (4 * Math.Ceiling(((double)_indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[3].binary.Length / 3))).ToString(CultureInfo.InvariantCulture);
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[3].cvParam = new Generated.CVParamType[2];
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[3].cvParam[0] = new Generated.CVParamType();
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[3].cvParam[0].accession = "MS:1000786";
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[3].cvParam[0].name = "non-standard data array";
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[3].cvParam[1] = new Generated.CVParamType();
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[3].cvParam[1].accession = "MS:1000523";
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[3].cvParam[1].name = "64-bit float";
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[3].userParam = new Generated.UserParamType[1];
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[3].userParam[0] = new Generated.UserParamType();
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[3].userParam[0].name = "custom noise array from noise data";

                    // baseline
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[4] = new Generated.BinaryDataArrayType();
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[4].binary = myMsDataFile.GetOneBasedScan(i).Get64BitNoiseDataBaseline();
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[4].encodedLength = (4 * Math.Ceiling(((double)_indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[4].binary.Length / 3))).ToString(CultureInfo.InvariantCulture);
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[4].cvParam = new Generated.CVParamType[2];
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[4].cvParam[0] = new Generated.CVParamType();
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[4].cvParam[0].accession = "MS:1000786";
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[4].cvParam[0].name = "non-standard data array";
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[4].cvParam[1] = new Generated.CVParamType();
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[4].cvParam[1].accession = "MS:1000523";
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[4].cvParam[1].name = "64-bit float";
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[4].userParam = new Generated.UserParamType[1];
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[4].userParam[0] = new Generated.UserParamType();
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[4].userParam[0].name = "custom baseline array from noise data";
                }
            }

            Write(outputFile, _indexedmzMLConnection);
        }

        #endregion Public Methods

        #region Private Methods

        private static void Write(string filePath, Generated.indexedmzML _indexedmzMLConnection)
        {
            using (TextWriter writer = new StreamWriter(filePath))
            {
                _indexedSerializer.Serialize(writer, _indexedmzMLConnection);
            }
        }

        #endregion Private Methods

    }
}