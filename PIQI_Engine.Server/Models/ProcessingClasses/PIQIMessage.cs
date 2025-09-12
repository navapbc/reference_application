using Newtonsoft.Json.Linq;
using PIQI_Engine.Server.Engines.SAMs;
using PIQI_Engine.Server.Services;

namespace PIQI_Engine.Server.Models
{
    /// <summary>
    /// Represents a PIQI message and its associated evaluation context.
    /// </summary>
    public class PIQIMessage
    {
        #region Properties

        /// <summary>
        /// The evaluation rubric used for scoring this message.
        /// </summary>
        public EvaluationRubric EvaluationRubric { get; set; }

        /// <summary>
        /// The request that initiated this PIQI message.
        /// </summary>
        public PIQIRequest PIQIRequest { get; set; }

        /// <summary>
        /// Reference data used during evaluation.
        /// </summary>
        public PIQIReferenceData RefData { get; set; }

        /// <summary>
        /// The message model that represents the structure and content of this message.
        /// </summary>
        public MessageModel MessageModel { get; set; }

        /// <summary>
        /// Dictionary of all PIQI SAMs (Scoring and Auditing Methods) associated with this message.
        /// </summary>
        public Dictionary<string, PIQISAM> SAMDict { get; set; }

        /// <summary>
        /// Statistical results generated from processing this message.
        /// </summary>
        public StatMethodResult StatMethodResult { get; set; }

        /// <summary>
        /// Formatted statistical result for this message.
        /// </summary>
        public PIQIStatResponse FormattedStatResponse { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="PIQIMessage"/> class with the specified request.
        /// </summary>
        /// <param name="piqiRequest">The PIQI request associated with this message.</param>
        public PIQIMessage(PIQIRequest piqiRequest)
        {
            PIQIRequest = piqiRequest;
            SAMDict = new Dictionary<string, PIQISAM>();
        }

        #endregion

        #region Stat Methods

        /// <summary>
        /// Generates the statistical result for this PIQI message by processing all applicable PIQI SAMs.
        /// </summary>
        /// <returns>A <see cref="StatMethodResult"/> containing aggregated scoring, pass/fail, and informational results.</returns>
        public StatMethodResult GenerateStatResponse()
        {
            // Create a new stat result object
            StatMethodResult result = new StatMethodResult();

            try
            {
                // Get the list of PIQI SAMs that are scorable (neither conditional nor dependent)
                List<PIQISAM> piqiSAMList = SAMDict.Values
                    .Where(s => !s.IsCondition && !s.IsDependency)
                    .ToList();

                // Process results for each scorable PIQI SAM
                foreach (var piqiSAM in piqiSAMList)
                {
                    result.ProcessResult(piqiSAM, RefData);
                }

                if (MessageModel?.EntityModel?.Root.Children != null)
                {
                    foreach (Entity entity in MessageModel.EntityModel.Root.Children)
                    {
                        // Create a new StatMethodResultClass object for this entity type
                        StatMethodResultClass statMethodResultClass = new StatMethodResultClass(entity.Mnemonic);

                        // Get all StatMethodResultElements that belong to this entity type
                        List<StatMethodResultElement> elementResponseList = result.ElementDict.Values
                            .Where(t => t.EntityTypeMnemonic == statMethodResultClass.EntityTypeMnemonic)
                            .ToList();

                        // Calculate aggregated stats for the class
                        statMethodResultClass.Calc(elementResponseList);

                        // Add the class stats to the result if it has any elements
                        if (statMethodResultClass.ElementCount > 0)
                        {
                            result.ClassDict.Add(statMethodResultClass.Key, statMethodResultClass);
                        }
                    }
                }
            }
            catch
            {
                throw;
            }

            // Save and return the generated statistical result
            StatMethodResult = result;
            return result;
        }

        #endregion

        #region Audit Methods

        /// <summary>
        /// Generates a JSON-formatted audit result for the current PIQI message.
        /// </summary>
        /// <returns>A JSON string representing the audit result.</returns>
        public string GenerateAuditResponse()
        {
            try
            {
                // Create root message object
                JObject msg = new JObject();

                // Add header info
                msg.Add("EntityModelMnemonic", MessageModel.Header.EntityModelMnemonic);
                msg.Add("DataProviderID", PIQIRequest.DataProviderID ?? MessageModel.Header.ProviderName);
                msg.Add("DataSourceID", PIQIRequest.DataSourceID ?? MessageModel.Header.DataSourceName);
                msg.Add("MessageID", PIQIRequest.MessageID ?? MessageModel.Header.ClientMessageID);

                // Add message-level audit info
                Audit_AddMessageInfo(msg, FormattedStatResponse);

                // Process the message model recursively
                Audit_ProcessMessageModelItem(MessageModel.RootItem, null, msg, MessageModel.EntityModel.Root);

                return msg.ToString();
            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// Recursively processes a message model item and adds audit information to the JSON node.
        /// </summary>
        /// <param name="item">The message model item to process.</param>
        /// <param name="parentItem">The optional model item parent to process (Needed for element sequence on attribute).</param>
        /// <param name="parentNode">The parent JSON node to attach audit information.</param>
        /// <param name="entity">The optional entity to process. Used in tandem with item to include processed entities without item data</param>

        private void Audit_ProcessMessageModelItem(MessageModelItem? item, MessageModelItem? parentItem, JToken parentNode, Entity? entity)
        {
            try
            {
                JObject? elementNode = null;
                string itemName = item?.Entity?.FieldName ?? item?.Entity?.Name ?? entity?.FieldName ?? entity?.Name ?? "UNKNOWN";

                if (item?.ItemType == EntityItemTypeEnum.Root)
                {
                    itemName = item.Name;
                    JObject itemNode = Utility.JSON_AddObject((JObject)parentNode, itemName);
                    if (entity?.Children != null)
                    {
                        foreach (Entity classEntity in entity.Children.OrderBy(e => e.Name))
                        {
                            var classModelItem = item.ChildDict?.Values.FirstOrDefault(c => c.Mnemonic == classEntity.Mnemonic);
                            Audit_ProcessMessageModelItem(classModelItem, null, itemNode, classEntity);
                        }
                    }
                }
                else if (entity != null && entity?.DataTypeID == EntityDataTypeEnum.CLS)
                {
                    JArray itemNode = Utility.JSON_AddArray((JObject)parentNode, itemName);
                    if (item?.ChildDict != null)
                    {
                        foreach (MessageModelItem child in item.ChildDict.Values.OrderBy(c => c.Name))
                            Audit_ProcessMessageModelItem(child, null, itemNode, entity?.Children?.FirstOrDefault());
                    }
                }
                else if (item?.ItemType == EntityItemTypeEnum.Element)
                {
                    JObject itemNode = Utility.JSON_AddObject((JArray)parentNode, itemName);
                    elementNode = itemNode;

                    if (entity?.Children != null)
                    {
                        foreach (Entity attrEntity in entity.Children.OrderBy(e => e.Name))
                        {
                            var attrModelItem = item.ChildDict?.Values.FirstOrDefault(c => c.Mnemonic == attrEntity.Mnemonic);
                            Audit_ProcessMessageModelItem(attrModelItem, item, itemNode, attrEntity);
                        }
                    }
                }
                else if (entity != null && entity?.DataTypeID > EntityDataTypeEnum.ELM)
                {
                    JObject? attrAuditItem = new JObject();
                    if (item != null)
                    {
                        JToken? attrDataItem = null;

                        // Handle leaf elements according to their data type
                        if (item?.Entity?.DataTypeID == EntityDataTypeEnum.CC)
                            attrDataItem = Utility.JSON_AddCodeableConceptObject((CodeableConcept)item.MessageData);
                        else if (item?.Entity?.DataTypeID == EntityDataTypeEnum.OBSVAL)
                            attrDataItem = Utility.JSON_AddValueObject((Value)item.MessageData);
                        else if (item?.Entity?.DataTypeID == EntityDataTypeEnum.RV)
                            attrDataItem = Utility.JSON_AddRefRangeObject((ReferenceRange)item.MessageData);
                        else
                            attrDataItem = (item?.MessageData?.Text ?? "");

                        if (attrDataItem == null) throw new Exception("Attribute information not found.");
                        attrAuditItem.Add("data", attrDataItem);
                    }

                    Audit_AddAttributeInfo(attrAuditItem, item ?? parentItem, item == null? entity : null);

                    ((JObject)parentNode).Add(itemName, attrAuditItem);
                }

                if (elementNode != null)
                    Audit_AddElementInfo(elementNode, item);
                else if (elementNode != null) throw new Exception("Element information not found.");
            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// Adds message-level audit information such as scoring summary.
        /// </summary>
        /// <param name="parent">Parent JSON node to attach the audit info.</param>
        /// <param name="statResponse">The formatted statistical result for this message.</param>
        public void Audit_AddMessageInfo(JObject parent, PIQIStatResponse statResponse)
        {
            try
            {
                JObject audit = Utility.JSON_AddObject(parent, "Audit");

                audit.Add("messageNumerator", statResponse.MessageResults.Numerator.ToString());
                audit.Add("messageDenominator", statResponse.MessageResults.Denominator.ToString());
                audit.Add("messageScore", ((float)statResponse.MessageResults.Numerator / statResponse.MessageResults.Denominator * 100).ToString("F"));
                audit.Add("messageNumeratorWeighted", statResponse.MessageResults.WeightedNumerator.ToString());
                audit.Add("messageDenominatorWeighted", statResponse.MessageResults.WeightedDenominator.ToString());
                audit.Add("messageScoreWeighted", ((float)statResponse.MessageResults.WeightedNumerator / statResponse.MessageResults.WeightedDenominator * 100).ToString("F"));
                audit.Add("messageCriticalFailureCount", statResponse.MessageResults.CriticalFailureCount.ToString());
            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// Adds element-level audit information for a specific message model item.
        /// </summary>
        /// <param name="parent">The JSON node to attach the element audit info.</param>
        /// <param name="elementItem">The message model element item to audit.</param>
        /// <returns>The audit JSON node for the element.</returns>
        private JObject? Audit_AddElementInfo(JObject parent, MessageModelItem elementItem)
        {
            JObject? auditNode = null;

            try
            {
                List<PIQISAM> elementPIQISAMList = SAMDict.Values
                    .Where(ps => ps.EntityTypeMnemonic == elementItem.ClassEntity.Mnemonic && ps.EntitySequence == elementItem.ElementSequence)
                    .ToList();

                if (elementPIQISAMList.Count > 0)
                {
                    auditNode = Utility.JSON_AddObject(parent, "elementAudit");

                    int denominator = 0;
                    int denominatorWeight = 0;
                    int numerator = 0;
                    int numeratorWeight = 0;
                    int criticalFailureCount = 0;

                    foreach (PIQISAM result in elementPIQISAMList
                                 .Where(ps => !ps.IsDependency && !ps.IsCondition)
                                 .OrderBy(ps => ps.EntityMnemonic).ThenBy(ps => ps.SAMName))
                    {
                        if (result.ProcessingState == SAMProcessStateEnum.Passed)
                        {
                            if (result.IsScoring)
                            {
                                denominator++;
                                denominatorWeight += result.ScoringWeight;
                                numerator++;
                                numeratorWeight += result.ScoringWeight;
                            }
                        }
                        else if (result.ProcessingState == SAMProcessStateEnum.Failed)
                        {
                            if (result.IsScoring)
                            {
                                denominator++;
                                denominatorWeight += result.ScoringWeight;
                                if (result.IsCritical) criticalFailureCount++;
                            }
                        }
                    }

                    double elementScore = denominator > 0 ? (double)numerator / denominator * 100 : 0;
                    double elementScoreW = denominatorWeight > 0 ? (double)numeratorWeight / denominatorWeight * 100 : 0;

                    auditNode.Add("elementScore", ((int)elementScore).ToString());
                    auditNode.Add("elementScoreWeighted", ((int)elementScoreW).ToString());
                    auditNode.Add("elementCriticalFailureCount", criticalFailureCount.ToString());
                    auditNode.Add("elementNumerator", numerator.ToString());
                    auditNode.Add("elementDenominator", denominator.ToString());
                }
            }
            catch
            {
                throw;
            }
            return auditNode;
        }

        /// <summary>
        /// Adds element-level audit information for a specific message model item.
        /// </summary>
        /// <param name="parent">The JSON node to attach the element audit info.</param>
        /// <param name="attributeItem">The message model element item to audit.</param>
        /// <returns>The audit JSON node for the element.</returns>
        private JObject? Audit_AddAttributeInfo(JObject parent, MessageModelItem attributeItem, Entity? entity)
        {
            JObject? auditNode = null;

            try
            {
                List<PIQISAM> attributePIQISAMList = null;
                if (entity == null)
                {
                    attributePIQISAMList = SAMDict.Values
                    .Where(ps => ps.EntityTypeMnemonic == attributeItem.ClassEntity.Mnemonic && ps.EntitySequence == attributeItem.ElementSequence && ps.EntityMnemonic == attributeItem.Mnemonic)
                    .ToList();
                }
                else
                {
                    attributePIQISAMList = SAMDict.Values
                    .Where(ps => ps.EntityTypeMnemonic == attributeItem.ClassEntity.Mnemonic && ps.EntitySequence == attributeItem.ElementSequence && ps.EntityMnemonic == entity.Mnemonic)
                    .ToList();
                }

                if (attributePIQISAMList.Count > 0)
                {
                    auditNode = Utility.JSON_AddObject(parent, "attributeAudit");
                    JObject attributeNode = Utility.JSON_AddObject(auditNode, "scoringData");
                    JArray assessmentNode = Utility.JSON_AddArray(auditNode, "assessmentItems");
                    JArray informationalNode = Utility.JSON_AddArray(auditNode, "InformationalItems");

                    int denominator = 0;
                    int denominatorWeight = 0;
                    int numerator = 0;
                    int numeratorWeight = 0;
                    int criticalFailureCount = 0;

                    foreach (PIQISAM result in attributePIQISAMList
                                 .Where(ps => !ps.IsDependency && !ps.IsCondition)
                                 .OrderBy(ps => ps.EntityMnemonic).ThenBy(ps => ps.SAMName))
                    {
                        JObject attrNode = result.IsScoring? Utility.JSON_AddObject(assessmentNode) : Utility.JSON_AddObject(informationalNode);
                        attrNode.Add("attributeMnemonic", result.EntityMnemonic);
                        attrNode.Add("attributeName", result.Entity?.FieldName ?? result.Entity?.Name ?? "");
                        attrNode.Add("assessment", result.EvaluationCriteriaSAMNameOverride ?? RefData.GetSAM(result.SAMMnemonic ?? "")?.Name ?? result.SAMMnemonic);
                        attrNode.Add("effect", result.IsScoring ? "Scoring" : "Informational");

                        if (result.ProcessingState == SAMProcessStateEnum.Passed)
                        {
                            attrNode.Add("status", "Passed");
                            if (result.IsScoring)
                            {
                                denominator++;
                                denominatorWeight += result.ScoringWeight;
                                numerator++;
                                numeratorWeight += result.ScoringWeight;
                            }
                            attrNode.Add("reason", "");
                        }
                        else if (result.ProcessingState == SAMProcessStateEnum.Skipped)
                        {
                            attrNode.Add("status", "Skipped");
                            attrNode.Add("reason", RefData.GetSAM(result.SkipSAMMnemonic ?? "")?.Name ?? result.SkipSAMMnemonic);
                        }
                        else if (result.ProcessingState == SAMProcessStateEnum.Failed)
                        {
                            attrNode.Add("status", "Failed");
                            attrNode.Add("reason", RefData.GetSAM(result.FailSAMMnemonic ?? "")?.Name ?? result.FailSAMMnemonic);
                            if (result.IsScoring)
                            {
                                denominator++;
                                denominatorWeight += result.ScoringWeight;
                                if (result.IsCritical) criticalFailureCount++;
                            }
                        }
                    }

                    double attributetScore = denominator > 0 ? (double)numerator / denominator * 100 : 0;
                    double attributeScoreW = denominatorWeight > 0 ? (double)numeratorWeight / denominatorWeight * 100 : 0;

                    attributeNode.Add("attributeScore", ((int)attributetScore).ToString());
                    attributeNode.Add("attributeScoreWeighted", ((int)attributeScoreW).ToString());
                    attributeNode.Add("attributeCriticalFailureCount", criticalFailureCount.ToString());
                    attributeNode.Add("attributeNumerator", numerator.ToString());
                    attributeNode.Add("attributeDenominator", denominator.ToString());
                }
            }
            catch
            {
                throw;
            }
            return auditNode;
        }

        #endregion

        #region SAMs

        /// <summary>
        /// Retrieves a PIQI SAM from the SAM dictionary based on entity, sequence, and SAM mnemonic.
        /// </summary>
        /// <param name="entityMnemonic">Entity mnemonic.</param>
        /// <param name="entitySequence">Entity sequence number.</param>
        /// <param name="criterionSequence">Criterion sequence number.</param>
        /// <param name="samMnemonic">SAM mnemonic.</param>
        /// <returns>The PIQI SAM if found; otherwise null.</returns>
        public PIQISAM? GetPIQISAM(string entityMnemonic, int entitySequence, int criterionSequence, string samMnemonic)
        {
            try
            {
                string key = $"{entityMnemonic}|{entitySequence}|{criterionSequence}|{samMnemonic}";
                return SAMDict.TryGetValue(key, out var piqiSam) ? piqiSam : null;
            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// Gets an existing PIQI SAM or creates a new one if it does not exist.
        /// </summary>
        /// <param name="entity">The entity associated with the SAM.</param>
        /// <param name="sam">The SAM definition.</param>
        /// <param name="classMnemonic">The mnemonic of the entity's class.</param>
        /// <param name="entitySequence">Sequence number.</param>
        /// <param name="evaluationCriterion">The evaluation criterion.</param>
        /// <returns>The existing or newly created PIQI SAM.</returns>
        public PIQISAM AddPIQISAM(Entity entity, SAM sam, string classMnemonic, int entitySequence, EvaluationCriterion evaluationCriterion)
        {
            try
            {
                PIQISAM? piqiSam = GetPIQISAM(entity.Mnemonic, entitySequence, evaluationCriterion.Sequence, sam.Mnemonic);
                if (piqiSam == null)
                {
                    piqiSam = new PIQISAM(entity, sam, classMnemonic, entitySequence, evaluationCriterion);
                    SAMDict.Add(piqiSam.SAMKey, piqiSam);
                }
                return piqiSam;
            }
            catch
            {
                throw;
            }
        }

        #region SAM Worker

        /// <summary>
        /// Returns the appropriate SAM worker instance for a given SAM mnemonic.
        /// </summary>
        /// <param name="mnemonic">The SAM mnemonic.</param>
        /// <param name="referenceDataService">
        /// An implementation of <see cref="SAMReferenceDataService"/> used to make FHIR API calls.
        /// </param>
        /// <returns>The SAM worker instance, or null if not found.</returns>
        public SAMBase GetSAMWorker(string mnemonic, SAMReferenceDataService referenceDataService)
        {
            try
            {
                SAM? sam = RefData.SAMList?.FirstOrDefault(x => x.Mnemonic == mnemonic);
                if (sam == null) // SAM must be in SAM list to be executed
                    throw new Exception($"{mnemonic} SAM not found in SAM list.");

                // Map mnemonics to SAM worker implementations
                return mnemonic switch
                {
                    "Attr_IsPopulated" => new SAM_AttrIsPopulated(sam, referenceDataService),
                    "Attr_IsNumeric" => new SAM_AttrIsNumeric(sam, referenceDataService),
                    "Attr_IsInteger" => new SAM_AttrIsInteger(sam, referenceDataService),
                    "Attr_IsDecimal" => new SAM_AttrIsDecimal(sam, referenceDataService),
                    "Attr_IsPositiveNumber" => new SAM_AttrIsPositiveNumber(sam, referenceDataService),
                    "Attr_IsNegativeNumber" => new SAM_AttrIsNegativeNumber(sam, referenceDataService),
                    "Attr_IsDate" => new SAM_AttrIsDate(sam, referenceDataService),
                    "Attr_IsFutureDate" => new SAM_AttrIsFutureDate(sam, referenceDataService),
                    "Attr_IsPastDate" => new SAM_AttrIsPastDate(sam, referenceDataService),
                    "Attr_IsTime" => new SAM_AttrIsTime(sam, referenceDataService),
                    "Attr_IsTimestamp" => new SAM_AttrIsTimestamp(sam, referenceDataService),
                    "Attr_IsTimestampTz" => new SAM_AttrIsTimestampTz(sam, referenceDataService),
                    "Attr_MatchesRegex" => new SAM_AttrMatchesRegex(sam, referenceDataService),
                    "Attr_InList" => new SAM_AttrIsInList(sam, referenceDataService),
                    "Attr_InExternalList" => new SAM_AttrIsInExternalList(sam, referenceDataService),
                    "Concept_HasCode" => new SAM_ConceptHasCode(sam, referenceDataService),
                    "Concept_HasCodeSystem" => new SAM_ConceptHasCodeSystem(sam, referenceDataService),
                    "Concept_HasDisplay" => new SAM_ConceptHasDisplay(sam, referenceDataService),
                    "Concept_HasRecognizedCodeSystem" => new SAM_ConceptHasRecognizedCodeSystem(sam, referenceDataService),
                    "Concept_IsComplete" => new SAM_ConceptIsComplete(sam, referenceDataService),
                    "Concept_IsValid" => new SAM_ConceptIsValid(sam, referenceDataService),
                    "Concept_IsValidMember" => new SAM_ConceptIsValidMember(sam, referenceDataService),
                    "Concept_IsConsistent" => new SAM_ConceptIsConsistent(sam, referenceDataService),
                    "Concept_IsActive" => new SAM_ConceptIsActive(sam, referenceDataService),
                    "Custom_External_Assessment" => new SAM_CustomExternalAssessment(sam, referenceDataService),
                    "ObservationValueType_InList" => new SAM_ValueTypeInList(sam, referenceDataService),
                    "ObservationValue_MatchesType" => new SAM_ValueMatchesType(sam, referenceDataService),
                    "ObservationValue_IsQualitative" => new SAM_ValueIsQualitative(sam, referenceDataService),
                    "RangeValue_IsComplete" => new SAM_RangeValueIsComplete(sam, referenceDataService),
                    "RangeValue_IsValid" => new SAM_RangeValueIsValid(sam, referenceDataService),
                    "Attr_IsCoded" => new SAM_AttrIsCoded(sam, referenceDataService),
                    "Eval_IsValid" => new SAM_EvalIsValid(sam, referenceDataService),
                    _ => new SAM_Default(sam, referenceDataService)
                };
            }
            catch
            {
                throw;
            }
        }

        #endregion

        #endregion

    }
}
