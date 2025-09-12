using Newtonsoft.Json;

namespace PIQI_Engine.Server.Models
{
    /// <summary>
    /// Represents the root container for evaluation profiles.
    /// </summary>
    public class ReferenceDataProfileRoot
    {
        /// <summary>
        /// A collection of evaluation profiles.
        /// </summary>
        [JsonProperty("EvaluationProfileLibrary")]
        public List<ReferenceDataProfile>? EvaluationProfiles { get; set; }

        /// <summary>
        /// A collection of model profiles.
        /// </summary>
        [JsonProperty("ModelLibrary")]
        public List<ReferenceDataProfile>? ModelProfiles { get; set; }
    }

    /// <summary>
    /// Represents an individual evaluation profile with basic metadata.
    /// </summary>
    public class ReferenceDataProfile
    {
        /// <summary>
        /// The name of the evaluation profile.
        /// </summary>
        public string Name { get; set; } = null!;

        /// <summary>
        /// The unique mnemonic of the evaluation profile.
        /// </summary>
        public string Mnemonic { get; set; } = null!;

        /// <summary>
        /// The file path where the evaluation profile is stored.
        /// </summary>
        public string FilePath { get; set; } = null!;
    }
}
