using Newtonsoft.Json;

namespace PIQI_Engine.Server.Models
{
    /// <summary>
    /// Represents an individual PIQI model with metadata and possible sub-models.
    /// </summary>
    public class Model
    {
        /// <summary>
        /// The name of the model.
        /// </summary>
        public string Name { get; set; } = null!;

        /// <summary>
        /// The version number of the model.
        /// </summary>
        public double Version { get; set; }

        /// <summary>
        /// The optional unique mnemonic for the model.
        /// </summary>
        public string? Mnemonic { get; set; }

        /// <summary>
        /// An optional description for the model.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Enum describing the type of model.
        /// </summary>
        [JsonProperty(PropertyName = "modelTypeId")]
        public ModelTypeEnum ModelType { get; set; }

        /// <summary>
        /// The mnemonic of the root entity associated with this model.
        /// </summary>
        public string RootEntityMnemonic { get; set; } = null!;

        /// <summary>
        /// The name of the root entity associated with this model.
        /// </summary>
        public string RootEntityName { get; set; } = null!;

        /// <summary>
        /// The field name of the root entity associated with this model.
        /// </summary>
        public string RootEntityFieldName { get; set; } = null!;

        /// <summary>
        /// Optional mnemonic of a base model if this model extends another model.
        /// </summary>
        public string? BaseModelMnemonic { get; set; }

        /// <summary>
        /// Last date the model was published.
        /// </summary>
        public DateTime? LastPublishedDate { get; set; }

        /// <summary>
        /// Last date the model was published.
        /// </summary>
        public bool IsDeprecated { get; set; }

        /// <summary>
        /// A list of class entities under this model.
        /// </summary>
        public List<Entity> ModelDataClasses { get; set; } = new List<Entity>();
    }
}
