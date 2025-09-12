namespace PIQI_Engine.Server.Models
{
    /// <summary>
    /// Represents an informational PIQI SAM result that is not scoring.
    /// Tracks entity, SAM identifiers, and execution counts.
    /// </summary>
    public class StatMethodResultInformational
    {
        #region Properties

        /// <summary>
        /// The mnemonic of the entity type associated with this informational SAM.
        /// </summary>
        public string EntityTypeMnemonic { get; set; }

        /// <summary>
        /// The name of the entity.
        /// </summary>
        public string EntityName { get; set; }

        /// <summary>
        /// The mnemonic of the SAM.
        /// </summary>
        public string SAMMnemonic { get; set; }

        /// <summary>
        /// The name of the SAM.
        /// </summary>
        public string SAMName { get; set; }

        /// <summary>
        /// Unique key combining entity and SAM mnemonics.
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Indicates if the SAM is critical.
        /// </summary>
        public bool IsCritical { get; set; }

        /// <summary>
        /// Weight associated with the SAM.
        /// </summary>
        public int Weight { get; set; }

        /// <summary>
        /// Total number of informational SAM executions.
        /// </summary>
        public int SAMTotalCount { get; set; }

        /// <summary>
        /// Number of skipped informational SAM executions.
        /// </summary>
        public int SAMSkippedCount { get; set; }

        /// <summary>
        /// Number of processed informational SAM executions.
        /// </summary>
        public int SAMProcessedCount { get; set; }

        /// <summary>
        /// Number of informational SAM executions that passed.
        /// </summary>
        public int SAMPassedCount { get; set; }

        /// <summary>
        /// Number of informational SAM executions that failed.
        /// </summary>
        public int SAMFailedCount { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Default constructor for <see cref="StatMethodResultInformational"/>.
        /// </summary>
        public StatMethodResultInformational() { }

        /// <summary>
        /// Initializes a new instance of <see cref="StatMethodResultInformational"/> based on a PIQISAM.
        /// </summary>
        /// <param name="piqiSam">The PIQI SAM to initialize from.</param>
        public StatMethodResultInformational(PIQISAM piqiSam)
        {
            EntityTypeMnemonic = piqiSam.EntityTypeMnemonic;
            EntityName = piqiSam.Entity.Name;
            SAMMnemonic = piqiSam.SAMKey;
            SAMName = piqiSam.EvaluationCriteriaSAMNameOverride ?? piqiSam.SAMName;
            Key = $"{piqiSam.EntityMnemonic}|{piqiSam.SAMMnemonic}";
            IsCritical = piqiSam.IsCritical;
            Weight = piqiSam.ScoringWeight;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Increments the SAM execution counts based on the processing state.
        /// </summary>
        /// <param name="state">The processing state of the SAM.</param>
        public void Increment(SAMProcessStateEnum state)
        {
            SAMTotalCount++;
            if (state == SAMProcessStateEnum.Skipped)
                SAMSkippedCount++;
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
