namespace PIQI_Engine.Server.Models
{
    /// <summary>
    /// Represents the response from evaluating a single PIQI Scoring and Assessment Method (SAM).
    /// Encapsulates the evaluation result state, any associated error messages, 
    /// and convenience flags for checking outcome.
    /// </summary>
    public class PIQISAMResponse
    {
        #region Properties

        /// <summary>
        /// Gets or sets the result state of the SAM evaluation.
        /// </summary>
        /// <remarks>
        /// This value reflects whether the SAM succeeded, failed, was skipped, or errored.
        /// </remarks>
        public SAMResultStateEnum ResultState { get; set; }        // Int cast of eSAM_RESULT_STATE

        /// <summary>
        /// Gets or sets the error message associated with the SAM evaluation.
        /// </summary>
        /// <remarks>
        /// This property is only populated when <see cref="ResultState"/> is <see cref="SAMResultStateEnum.ERRORED"/>.
        /// </remarks>
        public string? ErrorMessage { get; set; }    // Only populated if ResultState = ERRORED

        /// <summary>
        /// Gets a value indicating whether the SAM evaluation succeeded.
        /// </summary>
        public bool Succeeded { get { return ResultState == SAMResultStateEnum.SUCCEEDED; } }

        /// <summary>
        /// Gets a value indicating whether the SAM evaluation failed.
        /// </summary>
        public bool Failed { get { return ResultState == SAMResultStateEnum.FAILED; } }

        /// <summary>
        /// Gets a value indicating whether the SAM evaluation was skipped.
        /// </summary>
        public bool Skipped { get { return ResultState == SAMResultStateEnum.SKIPPED; } }

        /// <summary>
        /// Gets a value indicating whether the SAM evaluation encountered an error.
        /// </summary>
        public bool Errored { get { return ResultState == SAMResultStateEnum.ERRORED; } }

        #endregion

        #region Methods

        /// <summary>
        /// Marks the SAM evaluation as succeeded.
        /// </summary>
        public void Succeed()
        {
            ResultState = SAMResultStateEnum.SUCCEEDED;
        }

        /// <summary>
        /// Marks the SAM evaluation as failed.
        /// </summary>
        public void Fail()
        {
            ResultState = SAMResultStateEnum.FAILED;
        }

        /// <summary>
        /// Marks the SAM evaluation as skipped.
        /// </summary>
        public void Skip()
        {
            ResultState = SAMResultStateEnum.SKIPPED;
        }

        /// <summary>
        /// Marks the SAM evaluation as errored and assigns the provided error message.
        /// </summary>
        /// <param name="errorMessage">The error message describing why the SAM evaluation failed.</param>
        public void Error(string errorMessage)
        {
            ResultState = SAMResultStateEnum.ERRORED;
            ErrorMessage = errorMessage;
        }

        /// <summary>
        /// Marks the SAM evaluation as either succeeded or failed based on the provided value.
        /// </summary>
        /// <param name="succeeded">
        /// <c>true</c> to mark the SAM evaluation as succeeded; 
        /// <c>false</c> to mark it as failed.
        /// </param>
        public void Done(bool succeeded)
        {
            ResultState = (succeeded ? SAMResultStateEnum.SUCCEEDED : SAMResultStateEnum.FAILED);
        }

        #endregion
    }
}
