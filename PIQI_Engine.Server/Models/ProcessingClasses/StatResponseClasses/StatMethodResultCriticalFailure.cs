namespace PIQI_Engine.Server.Models
{
    /// <summary>
    /// Represents the result of a critical failure for a specific PIQI SAM.
    /// Tracks counts of total, skipped, processed, passed, and failed executions.
    /// </summary>
    public class StatMethodResultCriticalFailure
    {
        #region Properties

        /// <summary>
        /// The mnemonic of the entity associated with this critical failure.
        /// </summary>
        public string EntityMnemonic { get; set; }

        /// <summary>
        /// The mnemonic of the SAM associated with this critical failure.
        /// </summary>
        public string SAMMnemonic { get; set; }

        /// <summary>
        /// Unique key for this critical failure, typically combining entity and SAM mnemonics.
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Indicates whether this SAM contributes to scoring.
        /// </summary>
        public bool IsScoring { get; set; }

        /// <summary>
        /// Indicates whether this SAM is a critical check.
        /// </summary>
        public bool IsCritical { get; set; }

        /// <summary>
        /// The scoring weight of this SAM.
        /// </summary>
        public int Weight { get; set; }

        /// <summary>
        /// Total number of SAMs executed for this critical failure.
        /// </summary>
        public int SAMTotalCount { get; set; }

        /// <summary>
        /// Number of skipped SAM executions.
        /// </summary>
        public int SAMSkippedCount { get; set; }

        /// <summary>
        /// Number of processed SAM executions (excluding skipped).
        /// </summary>
        public int SAMProcessedCount { get; set; }

        /// <summary>
        /// Number of processed SAMs that passed.
        /// </summary>
        public int SAMPassedCount { get; set; }

        /// <summary>
        /// Number of processed SAMs that failed.
        /// </summary>
        public int SAMFailedCount { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="StatMethodResultCriticalFailure"/> class.
        /// Default constructor.
        /// </summary>
        public StatMethodResultCriticalFailure() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="StatMethodResultCriticalFailure"/> class
        /// based on a <see cref="PIQISAM"/> object.
        /// </summary>
        /// <param name="piqiSam">The PIQI SAM object to create the critical failure from.</param>
        public StatMethodResultCriticalFailure(PIQISAM piqiSam)
        {
            EntityMnemonic = piqiSam.EntityMnemonic;
            SAMMnemonic = piqiSam.SAMKey;
            Key = $"{piqiSam.EntityMnemonic}|{piqiSam.SAMMnemonic}|{piqiSam.FailSAMMnemonic}";
            IsCritical = piqiSam.IsCritical;
            IsScoring = piqiSam.IsScoring;
            Weight = piqiSam.ScoringWeight;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Increments the counts for this critical failure based on the SAM processing state.
        /// </summary>
        /// <param name="state">The processing state of the SAM (Skipped, Passed, Failed).</param>
        public void Increment(SAMProcessStateEnum state)
        {
            SAMTotalCount++;

            if (state == SAMProcessStateEnum.Skipped)
            {
                SAMSkippedCount++;
            }
            else
            {
                SAMProcessedCount++;
                if (state == SAMProcessStateEnum.Passed)
                    SAMPassedCount++;
                else
                    SAMFailedCount++;
            }
        }

        #endregion
    }
}
