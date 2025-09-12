using PIQI_Engine.Server.Models;
using PIQI_Engine.Server.Services;

namespace PIQI_Engine.Server.Engines.SAMs
{
    /// <summary>
    /// SAM implementation that checks if an attribute's value exists in an external value list.
    /// </summary>
    public class SAM_AttrIsInExternalList : SAMBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SAM_AttrIsInExternalList"/> class.
        /// </summary>
        /// <param name="sam">The SAM object associated with this evaluator.</param>
        /// /// <param name="referenceDataService">
        /// An implementation of <see cref="SAMReferenceDataService"/> used to access reference data and make FHIR API calls.
        /// </param>
        public SAM_AttrIsInExternalList(SAM sam, SAMReferenceDataService referenceDataService) : base(sam, referenceDataService) { }

        /// <summary>
        /// Evaluates whether the text value of a message attribute exists in a specified external value list.
        /// </summary>
        /// <param name="request">
        /// The <see cref="PIQISAMRequest"/> containing the message object and parameters for evaluation. 
        /// The <c>MessageObject</c> property must be a <see cref="MessageModelItem"/> whose 
        /// <c>MessageData</c> is of type <see cref="BaseText"/>. 
        /// The <c>ParameterList</c> must include a tuple with <c>Item1</c> equal to "Code System List"
        /// and <c>Item2</c> containing the value list mnemonic to check against.
        /// </param>
        /// <returns>
        /// A <see cref="Task{PIQISAMResponse}"/> representing the asynchronous evaluation result. 
        /// The <see cref="PIQISAMResponse"/> indicates whether the text value exists in the specified value list,
        /// or contains an error message if evaluation fails.
        /// </returns>
        /// <remarks>
        /// The method performs the following checks:
        /// <list type="bullet">
        /// <item><description>Validates that the parameter list contains the "Code System List" entry.</description></item>
        /// <item><description>Ensures the <see cref="_SAMReferenceDataService.ReferenceData"/> and its <c>ValueList</c> are available.</description></item>
        /// <item><description>Verifies that the specified value list mnemonic exists in the reference data.</description></item>
        /// <item><description>Performs a case-insensitive comparison of the attribute's text against both <c>DataCode</c> and <c>DataText</c> entries in the value list.</description></item>
        /// </list>
        /// </remarks>
        /// <exception cref="Exception">
        /// Thrown if:
        /// <list type="bullet">
        /// <item>The parameter list is null or does not contain "Code System List".</item>
        /// <item>The reference data or value list is missing.</item>
        /// <item>The specified value list mnemonic does not exist in the reference data.</item>
        /// </list>
        /// </exception>
        public override async Task<PIQISAMResponse> EvaluateAsync(PIQISAMRequest request)
        {
            PIQISAMResponse result = new();
            bool passed = false;

            try
            {
                // Set the message model item
                MessageModelItem item = (MessageModelItem)request.MessageObject;

                // Access the attribute's message data
                BaseText data = (BaseText)item.MessageData;

                // Get the ValueListMnemonic parameter
                if (request.ParmList == null) throw new Exception("Parameter list was not supplied");
                Tuple<string, string> arg1 = request.ParmList.Where(t => t.Item1 == "Code System List").FirstOrDefault();
                if (arg1 == null) throw new Exception("[Code System List] parameter not found");
                string setMnemonic = arg1.Item2;

                // Verify _SAMReferenceDataService.ReferenceData is not null
                if (_SAMReferenceDataService.ReferenceData == null || _SAMReferenceDataService.ReferenceData.ValueList == null) throw new Exception("Missing or invalid reference data for SAM_AttrIsInExternalList");

                // Verify that the value list exists in the reference data
                if (!_SAMReferenceDataService.ReferenceData.ValueList.Any(v => v.Mnemonic == setMnemonic))
                    throw new Exception("Value data [" + setMnemonic + "] not in RefData. Check processing engine.");

                // Retrieve the value list (case-insensitive match)
                ValueList value = _SAMReferenceDataService.ReferenceData.ValueList
                    .FirstOrDefault(v => v.Mnemonic.Equals(setMnemonic, StringComparison.OrdinalIgnoreCase));

                // Check if the attribute's text exists in the code list or text entries
                passed = value.CodeList.Any(c =>
                    c.DataCode.Equals(data.Text, StringComparison.OrdinalIgnoreCase) ||
                    c.DataText.Equals(data.Text, StringComparison.OrdinalIgnoreCase)
                );

                // Update result
                result.Done(passed);
            }
            catch (Exception ex)
            {
                result.Error(ex.Message);
            }
            return result;
        }
    }
}
