using PIQI_Engine.Server.Models;
using PIQI_Engine.Server.Services;

namespace PIQI_Engine.Server.Engines.SAMs
{
    public class SAM_ObsValuePlausible : SAMBase
    {
        public SAM_ObsValuePlausible(SAM sam, SAMService samService)
            : base(sam, samService) { }

        public override async Task<PIQISAMResponse> EvaluateAsync(PIQISAMRequest request)
        {
            PIQISAMResponse result = new();
            bool passed = false;

            try
            {
                // Set the message model item
                EvaluationItem evaluationItem = (EvaluationItem)request.EvaluationObject;
                MessageModelItem item = evaluationItem?.MessageItem;

                // Access the item's message data
                BaseText data = (BaseText)item.MessageData;

                // Validate the data format
                if (data is not Value)
                    throw new Exception("ObsValuePlausible expects an observation value.");

                // Cast data as observation value
                Value val = (Value)data;

                // Process required parms
                // Note: for now if we don't get the required parms we use a hard-coded list. this is a stopgap
                string valueText = "CE|CWE|CD|ST|FT|TX";

                if (request.ParmList != null)
                {
                    Tuple<string, string> arg1 = request.ParmList.Where(t => t.Item1 == "Valid Attribute List").FirstOrDefault();
                    if (arg1 != null)
                    {
                        valueText = arg1.Item2;
                    }
                }

                // Split param into list
                List<string> valuesList = Utility.Split(valueText);

                // Evaluate
                passed = valuesList != null && val.Type?.Code != null
                    && valuesList.Any(t => t.Equals(val.Type.Code, StringComparison.CurrentCultureIgnoreCase));

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
