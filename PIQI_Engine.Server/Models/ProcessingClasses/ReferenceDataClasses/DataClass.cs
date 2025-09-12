namespace PIQI_Engine.Server.Models
{
    /// <summary>
    /// Represents a data class within the entity model.
    /// A data class groups related entities and provides a mnemonic identifier.
    /// </summary>
    public class DataClass
    {
        /// <summary>
        /// The human-readable name of the data class.
        /// </summary>
        public string Name { get; set; } = null!;

        /// <summary>
        /// The mnemonic or short identifier for the data class.
        /// </summary>
        public string Mnemonic { get; set; } = null!;
    }
}
