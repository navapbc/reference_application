using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace PIQI_Engine.Server.Models
{
    /// <summary>
    /// Represents a parameter used in evaluation criteria.
    /// </summary>
    public class EvaluationCriteriaParameter
    {
        /// <summary>
        /// The value of the parameter. Can be null.
        /// </summary>
        public string? ParameterValue { get; set; }

        /// <summary>
        /// The ID representing the type of the parameter.
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public SAMParameterTypeEnum ParameterType { get; set; }

        /// <summary>
        /// The name of the parameter.
        /// </summary>
        public string ParameterName { get; set; } = null!;
    }
}
