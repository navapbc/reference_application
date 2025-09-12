namespace PIQI_Engine.Server.Models
{
    /// <summary>
    /// Defines the different types of SAM (Scoring and Matching) parameters
    /// that can be used for evaluation or configuration.
    /// </summary>
    public enum SAMParameterTypeEnum
    {
        /// <summary>
        /// A comma-separated value (CSV) list of items.
        /// </summary>
        CSV = 1,

        /// <summary>
        /// A regular expression (Regex) pattern.
        /// </summary>
        Regex = 2,

        /// <summary>
        /// A single value or item.
        /// </summary>
        Single = 3,

        /// <summary>
        /// A single object with structured data.
        /// </summary>
        Object = 4,

        /// <summary>
        /// A collection of structured objects.
        /// </summary>
        Objects = 5
    }
}