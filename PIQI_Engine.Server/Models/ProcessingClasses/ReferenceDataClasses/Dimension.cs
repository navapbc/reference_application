namespace PIQI_Engine.Server.Models
{
    /// <summary>
    /// Represents a dimension used for categorization or classification within the system.
    /// </summary>
    public class Dimension
    {
        /// <summary>
        /// The human-readable name of the dimension.
        /// </summary>
        public string Name { get; set; } = null!;

        /// <summary>
        /// The unique mnemonic identifier for the dimension.
        /// </summary>
        public string Mnemonic { get; set; } = null!;

        /// <summary>
        /// Optional description providing additional details about the dimension.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// The type of application this dimension is associated with.
        /// </summary>
        public int ApplicationTypeID { get; set; }

        /// <summary>
        /// The category to which this dimension belongs.
        /// </summary>
        public Category Category { get; set; }
    }
}
