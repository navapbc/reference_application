using System.Text.RegularExpressions;

namespace PIQI_Engine.Server.Models
{
    /// <summary>
    /// Represents the full PIQI statistical result returned from the engine after evaluation.
    /// Includes message-level score, class-level scores, informational evaluations, and detection results.
    /// </summary>
    public class PIQIStatResponse
    {
        #region Properties
        /// <summary>
        /// Identifier for the data provider.
        /// </summary>
        public string DataProviderID { get; set; }

        /// <summary>
        /// Identifier for the data source.
        /// </summary>
        public string DataSourceID { get; set; }

        /// <summary>
        /// Unique identifier for the message.
        /// </summary>
        public string MessageID { get; set; }

        /// <summary>
        /// Mnemonic of the evaluation rubric used.
        /// </summary>
        public string EvaluationRubric { get; set; }

        /// <summary>
        /// Timestamp of when the message was processed.
        /// </summary>
        public DateTime ProcessDate { get; set; } = DateTime.Now;

        /// <summary>
        /// Overall score for the message.
        /// </summary>
        public ScoreResult MessageResults { get; set; }

        /// <summary>
        /// Class-level score results.
        /// </summary>
        public List<DataClassScoreResult> DataClassResults { get; set; }

        /// <summary>
        /// Informational results for non-scoring SAMs.
        /// </summary>
        public List<InformationalResult> InformationalResults { get; set; }

        /// <summary>
        /// Detection results.
        /// </summary>
        public List<DetectionResult> DetectionResults { get; set; }
        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="PIQIStatResponse"/>.
        ///</summary>
        public PIQIStatResponse() { }

        /// <summary>
        /// Initializes a new PIQIStatResponse based on a StatMethodResult and the associated PIQI message.
        /// </summary>
        /// <param name="statMethodResult">The statistical result object.</param>
        /// <param name="message">The original PIQI message being evaluated.</param>
        public PIQIStatResponse(StatMethodResult statMethodResult, PIQIMessage message)
        {
            // Headers
            DataProviderID = message.MessageModel.Header.ProviderName;
            DataSourceID = message.MessageModel.Header.DataSourceName;
            MessageID = message.MessageModel.Header.ClientMessageID;
            EvaluationRubric = message.RefData.EvaluationRubric.Name ?? message.RefData.EvaluationRubric.Mnemonic;

            // Message score
            MessageResults = new ScoreResult(statMethodResult);

            // Create Lists
            DataClassResults = new List<DataClassScoreResult>();
            InformationalResults = new List<InformationalResult>();
            DetectionResults = new List<DetectionResult>();

            // Iterate through classes and add results
            foreach (var statClass in statMethodResult.ClassDict)
            {
                MessageModelItem dataClass = message.MessageModel.ClassDict.FirstOrDefault(dc => dc.Value.ClassEntity?.Mnemonic == statClass.Value?.EntityTypeMnemonic).Value;

                if (dataClass.Entity != null && statClass.Value != null)
                {
                    DataClassScoreResult dataClassStatResult = new DataClassScoreResult(statClass.Value, dataClass.ClassEntity.FieldName ?? dataClass.ClassEntity.Name);
                    DataClassResults.Add(dataClassStatResult);

                    if (statMethodResult.InformationalDict.Any(i => i.Value.EntityTypeMnemonic == dataClass.ClassEntity.Mnemonic))
                    {
                        InformationalResult informationalResult = new InformationalResult(statMethodResult, dataClass.Entity);
                        InformationalResults.Add(informationalResult);
                    }
                }
            }

            DataClassResults = DataClassResults.OrderBy(d => d.DataClassName).ToList();
        }
        #endregion
    }

    /// <summary>
    /// Represents the score result for a specific data class.
    /// </summary>
    public class DataClassScoreResult : ScoreResult
    {
        #region Properties
        /// <summary>
        /// Name of the data class.
        /// </summary>
        public string DataClassName { get; set; }

        /// <summary>
        /// Number of instances processed for this data class.
        /// </summary>
        public int InstanceCount { get; set; }
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="DataClassScoreResult"/>.
        ///</summary>
        public DataClassScoreResult() { }

        /// <summary>
        /// Initializes a new DataClassScoreResult.
        /// </summary>
        /// <param name="statMethodResultClass">The statistical class result.</param>
        /// <param name="dataClassName">The name of the data class.</param>
        public DataClassScoreResult(StatMethodResultClass statMethodResultClass, string dataClassName)
        {
            DataClassName = dataClassName;
            InstanceCount = statMethodResultClass.ElementCount;

            // Unweighted score
            Denominator = statMethodResultClass.SAMScoringProcessedCount;
            Numerator = statMethodResultClass.SAMPassCount;
            if (Denominator != 0)
                PIQIScore = (int)Math.Truncate((float)Numerator / (float)Denominator * 100);

            // Weighted score
            WeightedDenominator = statMethodResultClass.SAMWeightedDenominator;
            WeightedNumerator = statMethodResultClass.SAMWeightedNumerator;
            if (WeightedDenominator != 0)
                WeightedPIQIScore = (int)Math.Truncate((float)WeightedNumerator / (float)WeightedDenominator * 100);

            CriticalFailureCount = statMethodResultClass.CriticalFailureCount;
        }
        #endregion
    }

    /// <summary>
    /// Represents the general score result, including weighted/unweighted scores and critical failures.
    /// </summary>
    public class ScoreResult
    {
        #region Properties
        /// <summary>
        /// Gets or sets the total number of procedures evaluated.
        /// </summary>
        public int Denominator { get; set; }

        /// <summary>
        /// Gets or sets the number of procedures that passed evaluation.
        /// </summary>
        public int Numerator { get; set; }

        /// <summary>
        /// Gets or sets the unweighted PIQI score, calculated as (Numerator / Denominator) * 100.
        /// </summary>
        public int PIQIScore { get; set; }

        /// <summary>
        /// Gets or sets the number of procedures that resulted in a critical failure.
        /// </summary>
        public int CriticalFailureCount { get; set; }

        /// <summary>
        /// Gets or sets the total number of weighted procedures evaluated.
        /// </summary>
        public int WeightedDenominator { get; set; }

        /// <summary>
        /// Gets or sets the number of weighted procedures that passed evaluation.
        /// </summary>
        public int WeightedNumerator { get; set; }

        /// <summary>
        /// Gets or sets the weighted PIQI score, calculated as (WeightedNumerator / WeightedDenominator) * 100.
        /// </summary>
        public int WeightedPIQIScore { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ScoreResult"/> class with default values.
        /// </summary>
        public ScoreResult() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ScoreResult"/> class using the specified statistical method results.
        /// </summary>
        /// <param name="statMethodResult">The statistical method results used to populate the score values.</param>
        public ScoreResult(StatMethodResult statMethodResult)
        {
            Denominator = statMethodResult.ScoringProcCount;
            Numerator = statMethodResult.ScoringPassCount;
            if (Denominator != 0)
                PIQIScore = (int)Math.Truncate((float)Numerator / (float)Denominator * 100);

            WeightedDenominator = statMethodResult.WeightedProcCount;
            WeightedNumerator = statMethodResult.WeightedPassCount;
            if (WeightedDenominator != 0)
                WeightedPIQIScore = (int)Math.Truncate((float)WeightedNumerator / (float)WeightedDenominator * 100);

            CriticalFailureCount = statMethodResult.CriticalFailureCount;
        }

        #endregion
    }


    /// <summary>
    /// Represents the informational (non-scoring) results for a data class.
    /// </summary>
    public class InformationalResult
    {
        #region Properties

        /// <summary>
        /// Gets or sets the name of the data class associated with the informational results.
        /// </summary>
        public string DataClassName { get; set; }

        /// <summary>
        /// Gets or sets the collection of informational evaluations for the data class.
        /// </summary>
        public List<InformationalEvaluation> EvaluationList { get; set; }

        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="InformationalResult"/>.
        ///</summary>
        public InformationalResult() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="InformationalResult"/> class
        /// using the specified statistical method results and data class.
        /// </summary>
        /// <param name="statMethodResult">
        /// The statistical method results containing informational entries to evaluate.
        /// </param>
        /// <param name="dataClass">
        /// The data class for which the informational results are being created.
        /// </param>
        public InformationalResult(StatMethodResult statMethodResult, Entity dataClass)
        {
            DataClassName = dataClass.FieldName ?? dataClass.Name;
            EvaluationList = new List<InformationalEvaluation>();

            Dictionary<string, StatMethodResultInformational> classInformationalList =
                statMethodResult.InformationalDict
                    .Where(i => i.Value.EntityTypeMnemonic == dataClass.Mnemonic)
                    .OrderBy(i => i.Value.EntityName)
                    .ToDictionary(i => i.Key, i => i.Value);

            foreach (var informational in classInformationalList)
            {
                InformationalEvaluation informationalEvaluation = new InformationalEvaluation(informational.Value);
                EvaluationList.Add(informationalEvaluation);
            }
        }

        #endregion
    }


    /// <summary>
    /// Represents a single informational evaluation for a specific SAM (Statistical Analysis Method).
    /// </summary>
    public class InformationalEvaluation
    {
        #region Properties

        /// <summary>
        /// Gets or sets the name of the entity associated with the evaluation.
        /// </summary>
        public string EntityName { get; set; }

        /// <summary>
        /// Gets or sets the name of the evaluation (typically the SAM name).
        /// </summary>
        public string EvaluationName { get; set; }

        /// <summary>
        /// Gets or sets the total number of instances considered in the evaluation.
        /// </summary>
        public int InstanceCount { get; set; }

        /// <summary>
        /// Gets or sets the total number of instances that were processed in the evaluation.
        /// </summary>
        public int Denominator { get; set; }

        /// <summary>
        /// Gets or sets the number of instances that passed evaluation.
        /// </summary>
        public int Numerator { get; set; }

        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="InformationalEvaluation"/>.
        ///</summary>
        public InformationalEvaluation() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="InformationalEvaluation"/> class
        /// using the specified informational result values.
        /// </summary>
        /// <param name="informational">
        /// The <see cref="StatMethodResultInformational"/> object containing details
        /// about the SAM evaluation to populate this instance.
        /// </param>
        public InformationalEvaluation(StatMethodResultInformational informational)
        {
            EvaluationName = informational.SAMName;
            EntityName = informational.EntityName;
            InstanceCount = informational.SAMTotalCount;
            Numerator = informational.SAMPassedCount;
            Denominator = informational.SAMProcessedCount;
        }

        #endregion
    }


    /// <summary>
    /// Represents a set of detection results, which groups together
    /// multiple detection evaluations under a common detection set.
    /// </summary>
    public class DetectionResult
    {
        #region Properties

        /// <summary>
        /// Gets or sets the name of the detection set that these evaluations belong to.
        /// </summary>
        public string DetectionSet { get; set; }

        /// <summary>
        /// Gets or sets the collection of detection evaluations associated with this detection set.
        /// </summary>
        public List<DetectionEvaluation> Evaluations { get; set; }

        #endregion
    }

    /// <summary>
    /// Represents a single evaluation entry within a <see cref="DetectionResult"/>.
    /// </summary>
    public class DetectionEvaluation
    {
        #region Properties

        /// <summary>
        /// Gets or sets the name of the evaluation (e.g., rule or detection criteria).
        /// </summary>
        public string EvaluationName { get; set; }

        /// <summary>
        /// Gets or sets the numeric detection outcome for the evaluation.
        /// This value typically represents a count or flag indicating whether the detection was triggered.
        /// </summary>
        public int Detection { get; set; }

        #endregion
    }

}
