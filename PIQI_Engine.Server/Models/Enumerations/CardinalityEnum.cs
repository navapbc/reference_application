namespace PIQI_Engine.Server.Models
{
    /// <summary>
    /// Specifies the cardinality rules for classes in a model.
    /// </summary>
    public enum CardinalityEnum
    {
        /// <summary>
        /// Exactly one instance is required.
        /// </summary>
        One = 1,

        /// <summary>
        /// Zero or more instances are allowed.
        /// </summary>
        ZeroToMany = 2,

        /// <summary>
        /// One or more instances are required.
        /// </summary>
        OneToMany = 3
    }
}
