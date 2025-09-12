using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PIQI_Engine.Server.Services;

namespace PIQI_Engine.Server.Models
{
    /// <summary>
    /// Represents a value entity in a message, which may include a coded type, numeric values, and text.
    /// Inherits from <see cref="CodeableConcept"/>.
    /// </summary>
    public class Value : CodeableConcept
    {
        #region Properties

        /// <summary>
        /// The coded type of this value, represented as a <see cref="CodeableConcept"/>.
        /// </summary>
        [JsonProperty(PropertyName = "type")]
        public CodeableConcept TypeCC { get; set; }

        /// <summary>
        /// The resolved <see cref="DataType"/> corresponding to <see cref="TypeCC"/>.
        /// </summary>
        [JsonIgnore]
        public DataType Type { get; set; }

        /// <summary>
        /// The primary numeric value of this instance, if applicable.
        /// </summary>
        [JsonIgnore]
        public double? ValueNumber { get; set; }

        /// <summary>
        /// The secondary numeric value of this instance, if applicable (used for ranges).
        /// </summary>
        [JsonIgnore]
        public double? ValueNumber2 { get; set; }

        /// <summary>
        /// Stores the original field value from the source, if needed for reference.
        /// </summary>
        [JsonIgnore]
        public string OriginalField { get; set; }

        /// <summary>
        /// Indicates whether this value has a coded type.
        /// </summary>
        public bool HasCodedType { get { return (TypeCC != null); } }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Value"/> class from a <see cref="JToken"/> and a list of <see cref="DataType"/>.
        /// </summary>
        /// <param name="jToken">The JSON token containing the value information.</param>
        /// <param name="dataTypeList">The list of <see cref="DataType"/> objects used to resolve the value's type.</param>
        public Value(JToken jToken, List<DataType> dataTypeList)
        {
            // Initialize coding list
            CodingList = new List<Coding>();

            // Process text node
            if (jToken.SelectToken("text") == null && jToken.SelectToken("codings") == null)
            {
                // If no structured object, use the node value as text
                Text = jToken.Value<string>();
            }
            else
            {
                // Parse structured node
                Text = Utility.GetJSONString(jToken, "text");

                JToken codeItemTokens = jToken.SelectToken("codings");
                if (codeItemTokens != null)
                {
                    foreach (JToken codeItemToken in codeItemTokens.Children())
                    {
                        Coding item = new Coding(codeItemToken);
                        CodingList.Add(item);
                    }
                }
            }

            // Process type node
            JToken typeToken = jToken.SelectToken("type");
            if (typeToken != null) TypeCC = new CodeableConcept(typeToken);

            // Resolve DataType from TypeCC
            if (TypeCC != null) Type = dataTypeList.FirstOrDefault(dt => dt.Code == TypeCC.Text);
            if (Type == null) Type = dataTypeList.FirstOrDefault(dt => dt.Code == "ST");

            // Calculate numeric values based on type
            if (Type != null)
            {
                if (this.Type.IsNumeric && !this.Type.IsRange)
                {
                    double val;
                    if (double.TryParse(Text, out val)) ValueNumber = val;
                }
                if (this.Type.IsNumeric && this.Type.IsRange)
                {
                    double val;
                    List<string> bitList = new List<string>(Text.Split(new char[] { '^' }));
                    if (bitList.Count > 0) if (double.TryParse(bitList[0], out val)) ValueNumber = val;
                    if (bitList.Count > 1) if (double.TryParse(bitList[1], out val)) ValueNumber2 = val;
                }
            }
        }

        #endregion
    }
}
