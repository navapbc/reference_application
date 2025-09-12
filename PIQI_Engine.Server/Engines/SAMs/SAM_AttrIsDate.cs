using PIQI_Engine.Server.Models;
using PIQI_Engine.Server.Services;

namespace PIQI_Engine.Server.Engines.SAMs
{
    /// <summary>
    /// SAM implementation that checks if an attribute's value contains a valid date part.
    /// </summary>
    public class SAM_AttrIsDate : SAMBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SAM_AttrIsDate"/> class.
        /// </summary>
        /// <param name="sam">The SAM object associated with this evaluator.</param>
        /// /// <param name="referenceDataService">
        /// An implementation of <see cref="SAMReferenceDataService"/> used to access reference data and make FHIR API calls.
        /// </param>
        public SAM_AttrIsDate(SAM sam, SAMReferenceDataService referenceDataService) : base(sam, referenceDataService) { }

        /// <summary>
        /// Evaluates whether the text value of a message attribute represents a valid <see cref="DateTime"/>
        /// with a valid date component (greater than <see cref="DateTime.MinValue"/>).
        /// </summary>
        /// <param name="request">
        /// The <see cref="PIQISAMRequest"/> containing the message object to evaluate. 
        /// The <c>MessageObject</c> property must be a <see cref="MessageModelItem"/> whose 
        /// <c>MessageData</c> is of type <see cref="BaseText"/>. 
        /// The <see cref="BaseText.DateTimeValue"/> method is used to parse the datetime value.
        /// </param>
        /// <returns>
        /// A <see cref="Task{PIQISAMResponse}"/> representing the asynchronous evaluation result. 
        /// The <see cref="PIQISAMResponse"/> indicates whether the datetime value has a valid date part,
        /// or contains an error message if evaluation fails.
        /// </returns>
        /// <remarks>
        /// The value is considered valid if:
        /// <list type="bullet">
        /// <item><description>It can be successfully cast to <see cref="DateTime"/>.</description></item>
        /// <item><description>The date component is greater than <see cref="DateTime.MinValue"/>.</description></item>
        /// </list>
        /// </remarks>
        /// <exception cref="Exception">
        /// Thrown if the <see cref="PIQISAMRequest.MessageObject"/> cannot be cast to <see cref="MessageModelItem"/>,
        /// if <see cref="MessageModelItem.MessageData"/> is not a <see cref="BaseText"/>, 
        /// or if the value cannot be parsed as a <see cref="DateTime"/>.
        /// </exception>
        public override async Task<PIQISAMResponse> EvaluateAsync(PIQISAMRequest request)
        {
            PIQISAMResponse result = new();
            bool passed = false;

            try
            {
                // Set the message model item
                MessageModelItem item = (MessageModelItem)request.MessageObject;

                // Evaluate the item's message data
                BaseText data = (BaseText)item.MessageData;

                // Cast to DateTime
                DateTime? dateTime = data.DateTimeValue();
                if (dateTime == null) throw new Exception("Data parameter was not a datetime. Check the SAM dependencies");

                // Check if the date part is valid
                passed = (dateTime.Value.Date > DateTime.MinValue);

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
