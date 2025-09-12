namespace PIQI_Engine.Server.Services
{
    /// <summary>
    /// Contract for a provider that supplies a configured FHIR <see cref="HttpClient"/>.
    /// </summary>
    public interface IFHIRClientProvider
    {
        /// <summary>
        /// Gets the configured <see cref="HttpClient"/> instance for FHIR server communication.
        /// </summary>
        HttpClient Client { get; }

        /// <summary>
        /// Performs a FHIR CodeSystem $lookup operation for a given code and code system.
        /// </summary>
        /// <param name="code">The code value to look up.</param>
        /// <param name="system">The code system URI.</param>
        /// <param name="properties">Optional properties to include in the lookup.</param>
        /// <returns>The HTTP response from the FHIR server.</returns>
        Task<HttpResponseMessage> LookupCodeAsync(string code, string system, params string[] properties);

    }

    /// <summary>
    /// Provides an HTTP client configured for interacting with a FHIR server.
    /// </summary>
    public class FHIRClientProvider : IFHIRClientProvider
    {
        /// <summary>
        /// Gets the configured <see cref="HttpClient"/> instance for FHIR interactions.
        /// </summary>
        public HttpClient Client { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FHIRClientProvider"/> class.
        /// </summary>
        /// <param name="client">The <see cref="HttpClient"/> instance provided by dependency injection.</param>
        public FHIRClientProvider(HttpClient client)
        {
            Client = client;
        }

        public Task<HttpResponseMessage> LookupCodeAsync(string code, string system, params string[] properties)
        {
            try
            {
                if (properties == null || properties.Length == 0)
                    properties = new[] { "display", "designations", "status" };

                // Build query string
                var query = $"CodeSystem/$lookup?code={Uri.EscapeDataString(code)}&system={Uri.EscapeDataString(system)}";
                if (properties != null && properties.Length > 0)
                {
                    foreach (var prop in properties)
                        query += $"&property={Uri.EscapeDataString(prop)}";
                }

                return Client.GetAsync(query);
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}
