using PIQI_Engine.Server.Models;
using System.Net;

namespace PIQI_Engine.Server.Services
{
    /// <summary>
    /// Service for handling reference data lookups and FHIR server interactions
    /// used during SAM evaluation.
    /// </summary>
    public class SAMReferenceDataService
    {
        #region Properties

        /// <summary>
        /// Provides access to the <see cref="IFHIRClientProvider"/> service
        /// used for performing FHIR resource lookups.
        /// </summary>
        protected readonly IFHIRClientProvider _fhirClientProvider;

        public HttpClient Client { get; }

        /// <summary>
        /// Gets or sets the reference data used for PIQI processing.
        /// </summary>
        /// <value>
        /// A <see cref="PIQIReferenceData"/> instance containing lookup values, code systems,
        /// and other contextual information needed for SAM evaluation; or <c>null</c> if not assigned.
        /// </value>
        public PIQIReferenceData? ReferenceData { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SAMReferenceDataService"/> class
        /// with the specified <see cref="IFHIRClientProvider"/>.
        /// </summary>
        /// <param name="fhirClientProvider">
        /// An implementation of <see cref="IFHIRClientProvider"/> used to make FHIR API calls.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="fhirClientProvider"/> is <c>null</c>.
        /// </exception>
        public SAMReferenceDataService(IFHIRClientProvider fhirClientProvider, HttpClient client)
        {
            _fhirClientProvider = fhirClientProvider ?? throw new ArgumentNullException(nameof(fhirClientProvider));
            Client = client;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Updates a list of <see cref="Coding"/> objects with recognized code system information
        /// based on the current <see cref="ReferenceData"/>.
        /// </summary>
        /// <param name="codingList">
        /// The list of <see cref="Coding"/> objects to be updated. Each coding's 
        /// <see cref="Coding.HasRecognizedCodeSystem"/> and <see cref="Coding.RecognizedCodeSystem"/> 
        /// properties will be set if a recognized code system is found.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// Thrown if <see cref="ReferenceData"/> is <c>null</c> or invalid.
        /// </exception>
        public void SetRecognizedCodeSystems(List<Coding> codingList)
        {
            try
            {
                if (ReferenceData == null)
                    throw new InvalidOperationException("Missing or invalid reference data for recognized code systems check.");

                // Update each coding with recognized code system information
                foreach (Coding coding in codingList)
                {
                    var recognizedCodeSystem = coding.CodeSystemList?.FirstOrDefault(cs => ReferenceData.GetCodeSystem(cs) != null);
                    coding.HasRecognizedCodeSystem = recognizedCodeSystem != null;
                    coding.RecognizedCodeSystem = recognizedCodeSystem;
                }
            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// Updates the <see cref="Coding.IsInteroperable"/> property for each <see cref="Coding"/>
        /// in the given list based on the specified interoperability code systems.
        /// </summary>
        /// <param name="codingList">
        /// The list of <see cref="Coding"/> objects to update. Each coding must have
        /// <see cref="Coding.RecognizedCodeSystem"/> set to be checked for interoperability.
        /// </param>
        /// <param name="systemsList">
        /// The list of code system identifiers considered interoperable.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// Thrown if <see cref="ReferenceData"/> is <c>null</c> or invalid.
        /// </exception>
        public void UpdateInteroperability(List<Coding> codingList, List<string> systemsList)
        {
            try
            {
                if (ReferenceData == null)
                    throw new InvalidOperationException("Missing or invalid reference data for interoperability check.");

                // Update all codings against the list
                foreach (Coding coding in codingList)
                {
                    coding.IsInteroperable = coding.RecognizedCodeSystem != null ?
                        systemsList.Where(s => ReferenceData.GetCodeSystem(s) == ReferenceData.GetCodeSystem(coding.RecognizedCodeSystem)).Any()
                        : false;
                }
            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// Populates the <see cref="Coding.ReferenceDisplayList"/> for each <see cref="Coding"/> 
        /// in the given <see cref="CodeableConcept"/> using the FHIR $lookup operation
        /// if it has not already been called.
        /// </summary>
        /// <param name="codeableConcept">
        /// The <see cref="CodeableConcept"/> containing a list of <see cref="Coding"/> objects 
        /// for which the reference display values will be populated.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation. 
        /// The method completes when all codings have their reference display lists populated.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the <see cref="ReferenceData"/> property is <c>null</c> or invalid.
        /// </exception>
        /// <exception cref="Exception">
        /// Thrown if the FHIR $lookup call returns an unexpected status code other than <see cref="HttpStatusCode.BadRequest"/>.
        /// </exception>
        public async Task CallFHIRServer(CodeableConcept codeableConcept)
        {
            try
            {
                if (ReferenceData == null)
                    throw new InvalidOperationException("Missing or invalid reference data for FHIR server check.");

                // Update each coding with recognized code system information
                SetRecognizedCodeSystems(codeableConcept.CodingList);

                // Check each coding code against their code system lists
                foreach (Coding coding in codeableConcept.CodingList)
                {
                    // Get all code system list identifiers based on the coding's recognized code system
                    var codeSystemList = ReferenceData.GetCodeSystem(coding.RecognizedCodeSystem).CodeSystemIdentifiers;

                    foreach (string codeSystem in codeSystemList)
                    {
                        // Make API request to check code against code system
                        var response = await _fhirClientProvider.LookupCodeAsync(coding.CodeValue, codeSystem);

                        if (response.IsSuccessStatusCode)
                        {
                            coding.LookupResponse = response;
                            coding.IsValid = true;
                            coding.SetReferenceDisplayList();
                            coding.SetStatus();
                            break;
                        }
                        else if (response.StatusCode != HttpStatusCode.BadRequest)
                        {
                            throw new Exception($"Unexpected status code from FHIR client provider: {response.StatusCode}");
                        }
                    }
                }

                codeableConcept.FHIRServerCalled = true;
            } 
            catch
            {
                throw;
            }
        }

        #endregion
    }
}
