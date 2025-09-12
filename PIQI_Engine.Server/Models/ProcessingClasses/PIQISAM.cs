namespace PIQI_Engine.Server.Models
{
    /// <summary>
    /// Represents a single SAM (Statistical Assessment Metric) item for processing within the PIQI engine.
    /// Includes fields for processing state, skipped/failed mnemonics, scoring, and conditional/dependent relationships.
    /// </summary>
    public class PIQISAM
    {
        #region Properties

        /// <summary>
        /// Unique key for the SAM combining entity mnemonic, sequence, and SAM mnemonic.
        /// </summary>
        public string SAMKey { get; set; }

        /// <summary>
        /// The mnemonic identifier for the entity associated with this SAM.
        /// </summary>
        public string EntityMnemonic { get; set; }

        /// <summary>
        /// The mnemonic identifier for the type of entity associated with this SAM.
        /// </summary>
        public string EntityTypeMnemonic { get; set; }

        /// <summary>
        /// The mnemonic identifier for this SAM.
        /// </summary>
        public string SAMMnemonic { get; set; }

        /// <summary>
        /// The mnemonic of the evaluation criterion associated with this SAM (optional).
        /// </summary>
        public string? EvaluationCriteriaMnemonic { get; set; }

        /// <summary>
        /// Optional name override for the criteria used in the audit.
        /// </summary>
        public string? EvaluationCriteriaSAMNameOverride { get; set; }

        /// <summary>
        /// The human-readable name of this SAM.
        /// </summary>
        public string SAMName { get; set; }

        /// <summary>
        /// Sequence number marks which entity this is within its parent.
        /// </summary>
        public int EntitySequence { get; set; } = 0;

        /// <summary>
        /// Sequence number marks which evaluation criterion this uses.
        /// </summary>
        public int CriterionSequence { get; set; } = 0;

        /// <summary>
        /// Reference to the entity object associated with this SAM.
        /// </summary>
        public Entity Entity { get; set; }

        /// <summary>
        /// Indicates whether this SAM is a conditional SAM.
        /// </summary>
        public bool IsCondition { get; set; }

        /// <summary>
        /// Indicates whether this SAM depends on another SAM.
        /// </summary>
        public bool IsDependency { get; set; }

        /// <summary>
        /// Reference to the conditional SAM item, if any.
        /// </summary>
        public PIQISAM? ConditionalItem { get; set; }

        /// <summary>
        /// Reference to the dependent SAM item, if any.
        /// </summary>
        public PIQISAM? DependentItem { get; set; }

        /// <summary>
        /// Current processing state of this SAM (Pending, Passed, Failed, Skipped).
        /// </summary>
        public SAMProcessStateEnum ProcessingState { get; set; }

        /// <summary>
        /// Mnemonic for the SAM that caused this SAM to be skipped, if any.
        /// </summary>
        public string? SkipSAMMnemonic { get; set; }

        /// <summary>
        /// Mnemonic for the SAM that caused this SAM to fail, if any.
        /// </summary>
        public string? FailSAMMnemonic { get; set; }

        /// <summary>
        /// Indicates whether this SAM contributes to the scoring.
        /// </summary>
        public bool IsScoring { get; set; } = false;

        /// <summary>
        /// Indicates whether this SAM represents a critical failure.
        /// </summary>
        public bool IsCritical { get; set; } = false;

        /// <summary>
        /// Weight assigned to this SAM for scoring purposes.
        /// </summary>
        public int ScoringWeight { get; set; } = 0; 

        #endregion

        #region Constructors

        /// <summary>
        /// Default constructor.
        /// </summary>
        public PIQISAM() { }

        /// <summary>
        /// Initializes a new PIQISAM item with associated entity, SAM, sequence, and optional evaluation criterion.
        /// </summary>
        /// <param name="entity">The entity associated with this SAM.</param>
        /// <param name="sam">The SAM definition object.</param>
        /// <param name="classMnemonic">The mnemonic of the entity's class.</param>
        /// <param name="sequence">Sequence number of the SAM within the entity.</param>
        /// <param name="evaluationCriterion">Optional evaluation criterion providing scoring details.</param>
        public PIQISAM(Entity entity, SAM sam, string classMnemonic, int sequence, EvaluationCriterion? evaluationCriterion = null)
        {
            try
            {
                EntityMnemonic = entity.Mnemonic;
                EntityTypeMnemonic = classMnemonic;
                Entity = entity;
                SAMMnemonic = sam.Mnemonic;
                EvaluationCriteriaMnemonic = evaluationCriterion?.SAMMnemonic;
                SAMName = sam.Name;
                ProcessingState = SAMProcessStateEnum.Pending;
                EntitySequence = sequence;

                if (evaluationCriterion != null)
                {
                    EvaluationCriteriaSAMNameOverride = evaluationCriterion.SamNameOverride;
                    CriterionSequence = evaluationCriterion.Sequence;
                    IsScoring = evaluationCriterion.ScoringEffect == ScoringEffectEnum.Scoring;
                    IsCritical = evaluationCriterion.CriticalityIndicator;
                    ScoringWeight = evaluationCriterion.ScoringWeight;
                }

                // Create a dictionary key from entity, sequence, and SAM
                SAMKey = $"{EntityMnemonic}|{EntitySequence}|{CriterionSequence}|{SAMMnemonic}";
            } catch
            {
                throw;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Marks this SAM as passed.
        /// </summary>
        public void Pass()
        {
            ProcessingState = SAMProcessStateEnum.Passed;
        }

        /// <summary>
        /// Marks this SAM as failed and records the mnemonic of the SAM that caused the failure.
        /// </summary>
        /// <param name="failSAMMnemonic">The mnemonic of the SAM causing the failure.</param>
        public void Fail(string failSAMMnemonic)
        {
            ProcessingState = SAMProcessStateEnum.Failed;
            FailSAMMnemonic = failSAMMnemonic;
        }

        /// <summary>
        /// Marks this SAM as skipped and records the mnemonic of the SAM that caused it to be skipped.
        /// </summary>
        /// <param name="skipSAMMnemonic">The mnemonic of the SAM causing this skip.</param>
        public void Skip(string skipSAMMnemonic)
        {
            ProcessingState = SAMProcessStateEnum.Skipped;
            SkipSAMMnemonic = skipSAMMnemonic;
        }

        #endregion
    }
}
