using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace PIQI_Engine.Server.Models
{
    /// <summary>
    /// Represents a parameter used by a SAM (Scoring and Analysis Module).
    /// </summary>
    public class SAMParameter
    {
        /// <summary>
        /// The name of the parameter.
        /// </summary>
        public string Name { get; set; } = null!;

        /// <summary>
        /// The type ID of the parameter.
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public SAMParameterTypeEnum ParameterType { get; set; }

        /// <summary>
        /// The data type ID of the parameter.
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public EntityDataTypeEnum DataType { get; set; }
    }
}
