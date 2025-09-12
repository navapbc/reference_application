using PIQI_Engine.Server.Models;
using PIQI_Engine.Server.Services;

namespace PIQI_Engine.Server.Engines.SAMs
{
    /// <summary>
    /// SAM that checks whether a given <see cref="Value"/>'s type is included in a provided list of allowed types.
    /// </summary>
    public class SAM_ValueTypeInList : SAMBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SAM_ValueTypeInList"/> class.
        /// </summary>
        /// <param name="sam">The parent SAM object.</param>
        /// <param name="referenceDataService">
        /// An implementation of <see cref="SAMReferenceDataService"/> used to access reference data and make FHIR API calls.
        /// </param>
        public SAM_ValueTypeInList(SAM sam, SAMReferenceDataService referenceDataService) : base(sam, referenceDataService) { }

        /// <summary>
        /// Evaluates whether the <see cref="Value"/> contained in the provided request 
        /// matches one of the allowed type codes.
        /// </summary>
        /// <param name="request">
        /// The <see cref="PIQISAMRequest"/> containing:
        /// <list type="bullet">
        ///   <item>The <see cref="PIQISAMRequest.MessageObject"/>, expected to be a <see cref="MessageModelItem"/> whose <see cref="MessageModelItem.MessageData"/> is a <see cref="Value"/>.</item>
        ///   <item>Optional entries in <see cref="PIQISAMRequest.ParmList"/>, where one parameter contains the delimited string of allowed type codes.</item>
        /// </list>
        /// </param>
        /// <returns>
        /// A <see cref="Task{PIQISAMResponse}"/> representing the asynchronous operation. 
        /// The response indicates success if the <see cref="Value"/> type is included in the allowed list; otherwise, it indicates failure.
        /// </returns>
        public override async Task<PIQISAMResponse> EvaluateAsync(PIQISAMRequest request)
        {
            PIQISAMResponse result = new();
            bool passed = false;

            try
            {
                // Set the message model item
                MessageModelItem item = (MessageModelItem)request.MessageObject;

                // Access the item's message data
                BaseText data = (BaseText)item.MessageData;

                // Validate the data type
                if (data is not Value val)
                    throw new Exception("ValueTypeInList expects an observation value.");

                // Get allowed types parameter
                if (request.ParmList == null) throw new Exception("Parameter list was not supplied");
                Tuple<string, string> arg1 = request.ParmList.Where(t => t.Item1 == "Value Type List").FirstOrDefault();
                if (arg1 == null) throw new Exception("[Value Type List] parameter not found");
                string setMnemonic = arg1.Item2;
                string valueText = data.Text;

                // Split parameter into a list
                List<string> valuesList = Utility.Split(setMnemonic);

                // Evaluate
                passed = (val.Type != null
                          && valuesList.Any(t => t.Equals(val.Type.Code, StringComparison.CurrentCultureIgnoreCase)));

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
