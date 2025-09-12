namespace PIQI_Engine.Server.Models
{
    /// <summary>
    /// Represents a simple category with a name and mnemonic.
    /// </summary>
    public class Category
    {
        /// <summary>
        /// The display name of the category.
        /// </summary>
        public string Name { get; set; } = null!;

        /// <summary>
        /// A unique mnemonic identifier for the category.
        /// </summary>
        public string Mnemonic { get; set; } = null!;
    }
}
