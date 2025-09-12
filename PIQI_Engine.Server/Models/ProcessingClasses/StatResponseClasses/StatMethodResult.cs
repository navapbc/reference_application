namespace PIQI_Engine.Server.Models
{
    /// <summary>
    /// Holds statistical results for a SAM (Scoring and Assessment Method) execution.
    /// Tracks counts for scoring, weighted scoring, informational, skipped, failed, and critical failures.
    /// </summary>
    public class StatMethodResult
    {
        #region Properties

        /// <summary> Total number of scoring items processed. </summary>
        public int ScoringTotalCount { get; set; }

        /// <summary> Number of scoring items skipped. </summary>
        public int ScoringSkipCount { get; set; }

        /// <summary> Number of scoring items processed. </summary>
        public int ScoringProcCount { get; set; }

        /// <summary> Number of scoring items passed. </summary>
        public int ScoringPassCount { get; set; }

        /// <summary> Number of scoring items failed. </summary>
        public int ScoringFailCount { get; set; }

        /// <summary> Number of critical failures encountered. </summary>
        public int CriticalFailureCount { get; set; }

        /// <summary> Total number of weighted scoring items. </summary>
        public int WeightedTotalCount { get; set; }

        /// <summary> Number of weighted scoring items skipped. </summary>
        public int WeightedSkipCount { get; set; }

        /// <summary> Number of weighted scoring items processed. </summary>
        public int WeightedProcCount { get; set; }

        /// <summary> Number of weighted scoring items passed. </summary>
        public int WeightedPassCount { get; set; }

        /// <summary> Number of weighted scoring items failed. </summary>
        public int WeightedFailCount { get; set; }

        /// <summary> Total number of informational items. </summary>
        public int InfoTotalCount { get; set; }

        /// <summary> Number of informational items skipped. </summary>
        public int InfoSkipCount { get; set; }

        /// <summary> Number of informational items processed. </summary>
        public int InfoProcCount { get; set; }

        /// <summary> Number of informational items passed. </summary>
        public int InfoPassCount { get; set; }

        /// <summary> Number of informational items failed. </summary>
        public int InfoFailCount { get; set; }

        /// <summary> Dictionary of classes with their statistical results, keyed by entity type mnemonic. </summary>
        public Dictionary<string, StatMethodResultClass> ClassDict { get; set; }

        /// <summary> Dictionary of elements with their statistical results, keyed by entity type mnemonic and sequence. </summary>
        public Dictionary<string, StatMethodResultElement> ElementDict { get; set; }

        /// <summary> Dictionary of critical failures, keyed by EntityMnemonic|SAMMnemonic|FailSAMMnemonic. </summary>
        public Dictionary<string, StatMethodResultCriticalFailure> CritcalFailureDict { get; set; }

        /// <summary> Dictionary of informational results, keyed by EntityMnemonic|SAMMnemonic. </summary>
        public Dictionary<string, StatMethodResultInformational> InformationalDict { get; set; }

        /// <summary> Dictionary of skipped SAMs, keyed by EntityMnemonic|SAMMnemonic|SkipSAMMnemonic. </summary>
        public Dictionary<string, StatMethodResultSkip> SkipDict { get; set; }

        /// <summary> Dictionary of failed SAMs, keyed by EntityMnemonic|SAMMnemonic|FailSAMMnemonic. </summary>
        public Dictionary<string, StatMethodResultFail> FailDict { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of <see cref="StatMethodResult"/> with empty dictionaries.
        /// </summary>
        public StatMethodResult()
        {
            ClassDict = new Dictionary<string, StatMethodResultClass>();
            ElementDict = new Dictionary<string, StatMethodResultElement>();
            CritcalFailureDict = new Dictionary<string, StatMethodResultCriticalFailure>();
            InformationalDict = new Dictionary<string, StatMethodResultInformational>();
            SkipDict = new Dictionary<string, StatMethodResultSkip>();
            FailDict = new Dictionary<string, StatMethodResultFail>();
        }

        #endregion

        #region Methods

        #region Get Methods

        /// <summary> Retrieves a class result by its PIQI SAM entity type mnemonic. </summary>
        public StatMethodResultClass GetClass(PIQISAM piqiSam)
        {
            string key = $"{piqiSam.EntityTypeMnemonic}";
            return ClassDict.ContainsKey(key) ? ClassDict[key] : null;
        }

        /// <summary> Retrieves an element result by its PIQI SAM entity type mnemonic and sequence. </summary>
        public StatMethodResultElement GetElement(PIQISAM piqiSam)
        {
            string key = $"{piqiSam.EntityTypeMnemonic}.{piqiSam.EntitySequence}";
            return ElementDict.ContainsKey(key) ? ElementDict[key] : null;
        }

        /// <summary> Retrieves an element result using entity type mnemonic and sequence. </summary>
        public StatMethodResultElement GetElement(string entityTypeMnemonic, int sequence)
        {
            string key = $"{entityTypeMnemonic}.{sequence}";
            return ElementDict.ContainsKey(key) ? ElementDict[key] : null;
        }

        /// <summary> Retrieves a critical failure result by PIQI SAM keys. </summary>
        public StatMethodResultCriticalFailure GetCriticalFailure(PIQISAM piqiSam)
        {
            string key = $"{piqiSam.EntityMnemonic}|{piqiSam.SAMMnemonic}|{piqiSam.FailSAMMnemonic}";
            return CritcalFailureDict.ContainsKey(key) ? CritcalFailureDict[key] : null;
        }

        /// <summary> Retrieves an informational result by PIQI SAM keys. </summary>
        public StatMethodResultInformational GetInformational(PIQISAM piqiSam)
        {
            string key = $"{piqiSam.EntityMnemonic}|{piqiSam.SAMMnemonic}";
            return InformationalDict.ContainsKey(key) ? InformationalDict[key] : null;
        }

        /// <summary> Retrieves a skipped SAM result by PIQI SAM keys. </summary>
        public StatMethodResultSkip GetSkip(PIQISAM piqiSam)
        {
            string key = $"{piqiSam.EntityMnemonic}|{piqiSam.SAMMnemonic}|{piqiSam.SkipSAMMnemonic}";
            return SkipDict.ContainsKey(key) ? SkipDict[key] : null;
        }

        /// <summary> Retrieves a failed SAM result by PIQI SAM keys. </summary>
        public StatMethodResultFail GetFail(PIQISAM piqiSam)
        {
            string key = $"{piqiSam.EntityMnemonic}|{piqiSam.SAMMnemonic}|{piqiSam.FailSAMMnemonic}";
            return FailDict.ContainsKey(key) ? FailDict[key] : null;
        }

        #endregion

        #region Increment Methods

        /// <summary>
        /// Increments the total count for the given PIQI SAM. 
        /// Updates scoring totals if it is a scoring SAM; otherwise updates informational totals.
        /// Weighted total is incremented by the scoring weight for scoring SAMs.
        /// </summary>
        /// <param name="piqiSam">The PIQI SAM to evaluate.</param>
        public void IncrementTotal(PIQISAM piqiSam)
        {
            if (piqiSam.IsScoring)
            {
                ScoringTotalCount++;
                WeightedTotalCount += piqiSam.ScoringWeight | 0;
            }
            else
            {
                InfoTotalCount++;
            }
        }

        /// <summary>
        /// Increments the skipped count for the given PIQI SAM.
        /// Updates scoring skipped count if it is a scoring SAM; otherwise updates informational skipped count.
        /// Weighted skipped is incremented by the scoring weight for scoring SAMs.
        /// </summary>
        /// <param name="piqiSam">The PIQI SAM to evaluate.</param>
        public void IncrementSkipped(PIQISAM piqiSam)
        {
            if (piqiSam.IsScoring)
            {
                ScoringSkipCount++;
                WeightedSkipCount += piqiSam.ScoringWeight | 0;
            }
            else
            {
                InfoSkipCount++;
            }
        }

        /// <summary>
        /// Increments the processed count for the given PIQI SAM.
        /// Updates scoring processed count if it is a scoring SAM; otherwise updates informational processed count.
        /// Weighted processed is incremented by the scoring weight for scoring SAMs.
        /// </summary>
        /// <param name="piqiSam">The PIQI SAM to evaluate.</param>
        public void IncrementProcessed(PIQISAM piqiSam)
        {
            if (piqiSam.IsScoring)
            {
                ScoringProcCount++;
                WeightedProcCount += piqiSam.ScoringWeight | 0;
            }
            else
            {
                InfoProcCount++;
            }
        }

        /// <summary>
        /// Increments the passed count for the given PIQI SAM.
        /// Updates scoring passed count if it is a scoring SAM; otherwise updates informational passed count.
        /// Weighted passed is incremented by the scoring weight for scoring SAMs.
        /// </summary>
        /// <param name="piqiSam">The PIQI SAM to evaluate.</param>
        public void IncrementPassed(PIQISAM piqiSam)
        {
            if (piqiSam.IsScoring)
            {
                ScoringPassCount++;
                WeightedPassCount += piqiSam.ScoringWeight | 0;
            }
            else
            {
                InfoPassCount++;
            }
        }

        /// <summary>
        /// Increments the failed count for the given PIQI SAM.
        /// Updates scoring failed count if it is a scoring SAM; otherwise updates informational failed count.
        /// Weighted failed is incremented by the scoring weight for scoring SAMs.
        /// If the SAM is critical, also increments the critical failure count.
        /// </summary>
        /// <param name="piqiSam">The PIQI SAM to evaluate.</param>
        public void IncrementFailed(PIQISAM piqiSam)
        {
            if (piqiSam.IsScoring)
            {
                ScoringFailCount++;
                WeightedFailCount += piqiSam.ScoringWeight | 0;
                if (piqiSam.IsCritical)
                {
                    CriticalFailureCount++;
                }
            }
            else
            {
                InfoFailCount++;
            }
        }

        #endregion


        /// <summary>
        /// Processes a single PIQI SAM result and updates the corresponding scoring and informational statistics
        /// in this <see cref="StatMethodResult"/> instance.
        /// </summary>
        /// <param name="piqiSam">The PIQI SAM result to process.</param>
        /// <param name="referenceData">The reference data used for evaluation (not used in current logic, but available for future use).</param>
        /// <remarks>
        /// - Conditional and dependent PIQI SAMs are ignored.
        /// - Updates totals, processed, skipped, passed, failed, and weighted counts.
        /// - Logs scoring, skipped, fail, critical failure, and informational SAMs in the corresponding dictionaries.
        /// </remarks>
        public void ProcessResult(PIQISAM piqiSam, PIQIReferenceData referenceData)
        {
            // Ignore conditional and dependent PIQI SAMs
            if (piqiSam.IsCondition || piqiSam.IsDependency) return;

            // Increment the total if the PIQI SAM is scoring
            IncrementTotal(piqiSam);

            // If the failure is informational, log it in the informationalDict
            if (!piqiSam.IsScoring)
            {
                StatMethodResultInformational informational = GetInformational(piqiSam);
                if (informational == null)
                {
                    informational = new StatMethodResultInformational(piqiSam);
                    InformationalDict.Add(informational.Key, informational);
                }
                informational.Increment(piqiSam.ProcessingState);
            }

            // Log class records for all scoring PIQI SAMs. This is used for auditing.
            StatMethodResultElement piqiElement = GetElement(piqiSam);
            if (piqiElement == null)
            {
                piqiElement = new StatMethodResultElement(piqiSam.EntityTypeMnemonic, piqiSam.EntitySequence);
                ElementDict.Add(piqiElement.Key, piqiElement);
            }
            piqiElement.Increment(piqiSam);

            // Check if the PIQI SAM was skipped
            if (piqiSam.ProcessingState == SAMProcessStateEnum.Skipped)
            {
                IncrementSkipped(piqiSam);

                StatMethodResultSkip skip = GetSkip(piqiSam);
                if (skip == null)
                {
                    skip = new StatMethodResultSkip(piqiSam.EntityMnemonic, piqiSam.SAMMnemonic, piqiSam.SkipSAMMnemonic, true);
                    SkipDict.Add(skip.Key, skip);
                }
                skip.SkipCount++;
            }
            else
            {
                // Processed (passed or failed)
                IncrementProcessed(piqiSam);

                if (piqiSam.ProcessingState == SAMProcessStateEnum.Passed)
                {
                    IncrementPassed(piqiSam);
                }
                else
                {
                    IncrementFailed(piqiSam);

                    StatMethodResultFail fail = GetFail(piqiSam);
                    if (fail == null)
                    {
                        fail = new StatMethodResultFail(piqiSam.EntityMnemonic, piqiSam.SAMMnemonic, piqiSam.FailSAMMnemonic, piqiSam.IsScoring, piqiSam.IsCritical);
                        FailDict.Add(fail.Key, fail);
                    }
                    fail.FailCount++;

                    if (piqiSam.IsCritical)
                    {
                        StatMethodResultCriticalFailure criticalFailure = GetCriticalFailure(piqiSam);
                        if (criticalFailure == null)
                        {
                            criticalFailure = new StatMethodResultCriticalFailure(piqiSam);
                            CritcalFailureDict.Add(criticalFailure.Key, criticalFailure);
                        }
                        criticalFailure.Increment(piqiSam.ProcessingState);
                    }
                }
            }
        }
        #endregion
    }
}
