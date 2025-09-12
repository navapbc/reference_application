namespace PIQI_Engine.Server.Models
{
    /// <summary>
    /// Represents the current state of a processing method or task.
    /// </summary>
    public enum ProcessStateEnum
    {
        /// <summary>
        /// The process has not yet started.
        /// </summary>
        Pending = 0,

        /// <summary>
        /// The process is currently in progress.
        /// </summary>
        Processing = 1,

        /// <summary>
        /// The process completed successfully.
        /// </summary>
        Succeeded = 2,

        /// <summary>
        /// The process failed during execution.
        /// </summary>
        Failed = 3
    }
}
