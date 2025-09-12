using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PIQI_Engine.Server.Engines.SAMs;
using PIQI_Engine.Server.Models;
using PIQI_Engine.Server.Services;
using System.Data;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace PIQI_Engine.Server.Engines
{
    /// <summary>
    /// Engine responsible for processing, scoring, and auditing PIQI messages.
    /// Provides access to configuration, logging, caching, and reference data services.
    /// </summary>
    public class PIQIEngine
    {
        /// <summary>
        /// Application configuration used to access settings and options.
        /// </summary>
        protected readonly IConfiguration _Configuration;

        /// <summary>
        /// Logger used to record information, warnings, and errors during PIQI processing.
        /// </summary>
        protected readonly ILogger<PIQIEngine> _Logger;

        /// <summary>
        /// Caching service for storing and retrieving files used during processing.
        /// </summary>
        protected readonly FileCacheService _Cache;

        /// <summary>
        /// Service used in the SAMs to access reference data such as code systems.
        /// </summary>
        protected readonly SAMReferenceDataService _SAMReferenceDataService;

        /// <summary>
        /// Engine for managing reference data, such as lookups and domain-specific information.
        /// </summary>
        private ReferenceDataEngine _ReferenceDataEngine;

        /// <summary>
        /// Lock object used to synchronize access to the cache to prevent race conditions.
        /// </summary>
        private static object _CacheLock = new object();

        /// <summary>
        /// Initializes a new instance of the <see cref="PIQIEngine"/> class.
        /// </summary>
        /// <param name="configuration">The application configuration.</param>
        /// <param name="logger">The logger instance.</param>
        /// <param name="cache">The file cache service.</param>
        /// <param name="referenceDataService">The reference data service used in the SAMs.</param>
        /// <param name="referenceDataEngine">The engine used for handling reference data.</param>
        public PIQIEngine(
            IConfiguration configuration,
            ILogger<PIQIEngine> logger,
            FileCacheService cache,
            SAMReferenceDataService referenceDataService,
            ReferenceDataEngine referenceDataEngine
            )
        {
            _Configuration = configuration;
            _Logger = logger;
            _Cache = cache;
            _SAMReferenceDataService = referenceDataService;
            _ReferenceDataEngine = referenceDataEngine;
        }

        #region Main

        /// <summary>
        /// Processes a PIQI request and generates a result, optionally in audit mode.
        /// </summary>
        /// <param name="piqiRequest">The request object containing the PIQI message details.</param>
        /// <param name="auditMode">Indicates whether the request should generate an audited result.</param>
        /// <returns>A <see cref="PIQIResponse"/> containing the processed message, formatted statistics, and optionally the audited message.</returns>
        /// <exception cref="Exception">Thrown if the message processing fails, reference data cannot be loaded, or validation fails.</exception>
        public async Task<PIQIResponse> PiqiRequestAsync(PIQIRequest piqiRequest, bool auditMode)
        {
            // Create final result object
            PIQIResponse result = new PIQIResponse();

            try
            {
                var stopwatch = Stopwatch.StartNew();

                // Create message object
                PIQIMessage message = new PIQIMessage(piqiRequest);

                // Load the message header
                MessageModel headerResult = MessageModelBuilder.LoadHeader(piqiRequest);
                if (headerResult == null) throw new Exception("Failed to parse message header.");
                message.MessageModel = headerResult;

                //Load reference data: Code system dictionary, Sam list, Entity list, Entity Type list, Criteria list, Value list, and Data Type list
                PIQIReferenceData refDataResult = _ReferenceDataEngine.LoadRefData(piqiRequest.EvaluationRubricMnemonic);
                if (refDataResult == null) throw new Exception("Failed to load reference data.");
                message.RefData = refDataResult;
                _SAMReferenceDataService.ReferenceData = refDataResult;

                // Explicitly set Data Type List, Entity Model, and root information to be used when loading messageData content
                message.MessageModel.DataTypeList = refDataResult.DataTypeList;
                message.MessageModel.EntityModel = refDataResult.EntityModel;
                message.MessageModel.RootEntityFieldName = refDataResult.Model.RootEntityFieldName;
                message.MessageModel.RootEntityName = refDataResult.Model.RootEntityName;

                // Get evaluation rubric from evaluation mnemonic and apply it to the message
                EvaluationRubric evaluationRubric = message.RefData.EvaluationRubric;
                if (evaluationRubric == null) throw new Exception("evaluation rubric mnemonic invalid.");

                // Validate the input entity model version mnemonic against the evaluation
                ValidateEntityModelVersionMnemonic(evaluationRubric, piqiRequest.PIQIModelMnemonic);
                message.EvaluationRubric = evaluationRubric;

                // Load the message content  
                MessageModelBuilder.LoadContent(message.MessageModel);

                // Process the message
                await ProcessMessageAsync(message);

                // Generate stats
                StatMethodResult statResponse = message.GenerateStatResponse();

                // Format the information from the stat response into a PIQI stat response
                PIQIStatResponse formattedStatResponse = new PIQIStatResponse(statResponse, message);
                message.FormattedStatResponse = formattedStatResponse;

                // Generate audit message
                if (auditMode)
                {
                    string auditResponse = message.GenerateAuditResponse();
                    result.AuditedMessage = auditResponse;
                }

                stopwatch.Stop();
                // Set result succeeded
                result.Succeed(formattedStatResponse, stopwatch);
            }
            catch (Exception ex)
            {
                // Fail result with exception message
                _Logger.LogError(ex, "PiqiRequestAsync: " + ex.Message);
                result.Fail(ex);
            }
            return result;
        }

        // Iterates through the evaluation rubric and processes each criterion
        private async Task ProcessMessageAsync(PIQIMessage message)
        {
            try
            {
                // Sort evaluation criteria by sequence
                List<EvaluationCriterion> orderedEvaluationCriteria = message.EvaluationRubric.Criteria.OrderBy(ec => ec.Sequence).ToList();

                // Cycle through evaluation criteria
                foreach (EvaluationCriterion evaluationCriterion in orderedEvaluationCriteria)
                {
                    // Get the entity and class key for the entity mnemonic in the evaluation criterion
                    Entity? criteriaEntity = message.RefData.GetEntity(evaluationCriterion.Entity);
                    Entity? criteriaClassEntity = message.RefData.GetEntityClass(evaluationCriterion.Entity);
                    string? criteriaElementMnemonic = criteriaClassEntity?.Children?.FirstOrDefault()?.Mnemonic;

                    if (criteriaEntity == null || criteriaClassEntity == null || criteriaElementMnemonic == null) throw new Exception("Failed to load entity from evaluation criterion.");
                    string classKey = $"{message.RefData.Model.RootEntityMnemonic}|{criteriaClassEntity.Mnemonic}";
                    
                    // Get the relevant entities based on the datatype
                    switch (criteriaEntity.DataTypeID)
                    {
                        case EntityDataTypeEnum.ROOT:
                            throw new Exception("Not implemented - model entity in criteria");
                        case EntityDataTypeEnum.CLS:
                            throw new Exception("Not implemented - class entity in criteria");
                        case EntityDataTypeEnum.ELM:
                            throw new Exception("Not implemented - element entity in criteria");
                        default:
                            // Get the total number of atrtributes to iterate through
                            int attributeTotal = message.MessageModel.ClassDict.TryGetValue(classKey, out MessageModelItem? classValue) ? classValue.ChildDict.Count : 0;

                            //For each element in the given class, process the SAM on the message model item representing the attribute
                            for (int entitySequence = 1; entitySequence <= attributeTotal; entitySequence++)
                            {
                                // Get attribute key from classKey, parent mnemonic, sequence, and criteria entity mnemonic and use it to get the message model item from AttrDict
                                string attributeKey = $"{classKey}|{criteriaElementMnemonic}.{entitySequence}|{evaluationCriterion.Entity}";
                                MessageModelItem messageModelItem = message.MessageModel.AttrDict.TryGetValue(attributeKey, out MessageModelItem? attrValue) ? attrValue : null;
                                
                                // Now that we have the message model item, process the criteria
                                PIQISAM processResult = await ProcessCriteriaSAMAsync(message, messageModelItem, evaluationCriterion, entitySequence);
                                if (processResult == null)
                                    throw new Exception("Evaluation criterion processing failed.");
                            }
                            break;
                    }
                }
            }
            catch
            {
                throw;
            }
        }
        #endregion

        #region Validation
        private void ValidateEntityModelVersionMnemonic(EvaluationRubric evaluationRubric, string messagePIQIModelMnemonic)
        {
            try
            {
                if (messagePIQIModelMnemonic == null || string.IsNullOrWhiteSpace(messagePIQIModelMnemonic)) throw new Exception("Missing model version mnemonic.");
                var pattern = @"^(.*?)_V(\d+)(?:_(.*))?$";
                string? messageModelMnemonic, messageVersion, messageExtension = null;
                string? evaluationModelMnemonic, evaluationVersion, evaluationExtension = null;

                // Split the message model mnemonic into parts
                var messageMatch = Regex.Match(messagePIQIModelMnemonic, pattern);
                if (messageMatch.Success)
                {
                    messageModelMnemonic = messageMatch.Groups[1].Value; // "model_x_y_z"
                    messageVersion = messageMatch.Groups[2].Value; // "123"
                    messageExtension = messageMatch.Groups[3].Success ? messageMatch.Groups[3].Value : null; // optional
                }
                else throw new Exception("Invalid message model.");

                // Split the evaluation model mnemonic into parts
                var evalMatch = Regex.Match(evaluationRubric.Model.Mnemonic ?? "", pattern);
                if (evalMatch.Success)
                {
                    evaluationModelMnemonic = evalMatch.Groups[1].Value; // "model_x_y_z"
                    evaluationVersion = evalMatch.Groups[2].Value; // "123"
                    evaluationExtension = evalMatch.Groups[3].Success ? evalMatch.Groups[3].Value : null; // optional
                }
                else throw new Exception("Invalid evaluation model.");

                // Verify the models match
                if (!messageModelMnemonic.Equals(evaluationModelMnemonic, StringComparison.OrdinalIgnoreCase))
                    throw new Exception("Message model mnemonic does not match the evaluation rubric.");
            }
            catch
            {
                throw;
            }
        }
        #endregion

        #region SAM Processing
        //  Validates the SAM parameters, triggers the conditional SAM and its dependencies, then triggers the evaluation criteria SAM and its dependencies
        private async Task<PIQISAM> ProcessCriteriaSAMAsync(PIQIMessage message, MessageModelItem messageModelItem, EvaluationCriterion evaluationCriterion, int entitySequence)
        {
            try
            {
                // Get entity from evaluation criterion
                Entity? entity = message.RefData.GetEntity(evaluationCriterion.Entity);
                if (entity == null)
                    throw new Exception($"Criteria entity{(evaluationCriterion.Entity != null ? " (" + evaluationCriterion.Entity + ")" : "")} missing from entity reference list or invalid.");
                string? classMnemonic = message.RefData.GetEntityClass(evaluationCriterion.Entity)?.Mnemonic;
                if (classMnemonic == null)
                {
                    _Logger.Log(LogLevel.Warning, $"Invalid class entity for evaluation criteria: {evaluationCriterion.Description}.");
                    classMnemonic = entity.Mnemonic.Split('.')[0];
                }
                // Get SAM from evaluation criterion
                SAM? criteriaSAM = message.RefData.GetSAM(evaluationCriterion.SAMMnemonic);
                if (criteriaSAM == null)
                    throw new Exception($"Criteria SAM{(evaluationCriterion.SAMMnemonic != null ? " (" + evaluationCriterion.SAMMnemonic + ")" : "")} missing from SAM reference list or invalid.");

                // Create PIQISAM item for processing
                PIQISAM processingPIQISAM = message.AddPIQISAM(entity, criteriaSAM, classMnemonic, entitySequence, evaluationCriterion);
                // Check if evaluation in the evaluation criteria is valid, skip if not
                SAM? evalValidSam = message.RefData.GetSAM("Eval_IsValid");
                if (evalValidSam == null)
                    throw new Exception($"Evaluation validity SAM (Eval_IsValid missing) from SAM reference list or invalid.");

                if (!EvalIsValid(evaluationCriterion, criteriaSAM))
                {
                    _Logger.Log(LogLevel.Warning, $"Invalid evaluation criteria: {evaluationCriterion.Description}");
                    processingPIQISAM.Skip(evalValidSam.Mnemonic);
                }
                // Check for a conditional SAM if processing state is still pending. If conditional SAM exists, process it prior to running the SAM in the evaluation criteria
                if (processingPIQISAM.ProcessingState == SAMProcessStateEnum.Pending && evaluationCriterion.ConditionalSAM != null)
                {
                    // Get class mnemonic to create PIQI SAM
                    string? conditionalClassMnemonic = message.RefData.GetEntityClass(processingPIQISAM.Entity.Mnemonic)?.Mnemonic;
                    if (conditionalClassMnemonic == null)
                    {
                        _Logger.Log(LogLevel.Warning, $"Invalid conditional class entity for evaluation criteria: {evaluationCriterion.Description}.");
                        conditionalClassMnemonic = processingPIQISAM.Entity.Mnemonic.Split('.')[0];
                    }

                    // Get conditional SAM from evaluation criterion
                    PIQISAM conditionalProcessingSAM = new PIQISAM(processingPIQISAM.Entity, criteriaSAM, conditionalClassMnemonic, processingPIQISAM.EntitySequence);

                    // Process conditional SAM and its prerequisites
                    await ProcessSAMAsync(message, conditionalProcessingSAM, messageModelItem, evaluationCriterion.ConditionalSAM, true);

                    // Skip current criteria SAM if conditional SAM fails
                    if (conditionalProcessingSAM.ProcessingState == SAMProcessStateEnum.Failed)
                    {
                        processingPIQISAM.Skip(evaluationCriterion.ConditionalSAM);
                    }
                }
                // Check if processing state is still pending. Process this SAM if so
                if (processingPIQISAM.ProcessingState == SAMProcessStateEnum.Pending)
                {
                    await ProcessSAMAsync(message, processingPIQISAM, messageModelItem, criteriaSAM.Mnemonic, false);
                }

                // No exceptions means the method succeeded
                return processingPIQISAM;
            }
            catch
            {
                throw;
            }
        }


        // Used to manage prerequisite SAMs for either the evaluation criteria SAM or the conditional SAM
        // Takes in either the criteria's prerequisite SAM or the conditional SAM 
        private async Task ProcessSAMAsync(PIQIMessage message, PIQISAM initialProcessingPIQISAM, MessageModelItem messageModelItem, string initalSAMMNemonic, bool isConditional)
        {
            try
            {
                // Stack of SAMs used to process the prerequisite SAMs in order
                Stack<SAM> dependencySAMStack = new Stack<SAM>();
                var dependencySAMMnemonic = initalSAMMNemonic;

                // Get the chain of prerequisite SAMs needed for the SAM in evaluationCriterion
                while (dependencySAMMnemonic != null)
                {
                    // Get the SAM matching the prerequisite mnemonic and add it to the stack of SAMS
                    SAM dependencySAM = message.RefData.GetSAM(dependencySAMMnemonic);
                    if (dependencySAM == null) throw new Exception($"Dependency SAM {dependencySAMMnemonic} not found.");

                    // Push the Dependency sam onto the stack to process later
                    dependencySAMStack.Push(dependencySAM);

                    // Set prerequisiteSAMMnemonic to the prerequisiteSAM's prerequisite SAM 
                    dependencySAMMnemonic = dependencySAM.PrerequisiteSAMMnemonic;
                }
                // Process the SAMs in the prerequisiteStack in order 
                while (dependencySAMStack.Count > 0)
                {
                    // Get SAM and PIQISAM for processing
                    SAM processingSAM = dependencySAMStack.Pop();

                    // Criteria parameter
                    List<EvaluationCriteriaParameter>? evaluationCriteriaParameters = null;
                    string? evaluationCriteriaProcessingURL = null;
                    string? dataMnemonic = null;
                    // Check for parameters (only necesary if the SAM is not dependent)
                    if (messageModelItem != null && processingSAM.Parameters != null && processingSAM.Parameters.Count > 0 && processingSAM.Mnemonic == initalSAMMNemonic)
                    {
                        // Get the evaluation criteria and evaluation criteria parameters
                        EvaluationCriterion? evaluationCriterion = message.RefData.GetEvaluationCriterion(message.EvaluationRubric, messageModelItem.Mnemonic, processingSAM.Mnemonic, initialProcessingPIQISAM.CriterionSequence, isConditional);
                        if (evaluationCriterion == null) throw new Exception("Missing or invalid evaluation criterion.");
                        evaluationCriteriaParameters = isConditional ? evaluationCriterion.ConditionalSAMParameters?.ToList() : evaluationCriterion.SAMParameters?.ToList();
                        evaluationCriteriaProcessingURL = evaluationCriterion?.ProcessingURL;

                        // Get the dataMnemonic specified in the param, if necessary
                        if (processingSAM.Mnemonic == "attr_is_in_value_list")
                            dataMnemonic = evaluationCriteriaParameters?.FirstOrDefault()?.ParameterValue;
                    }
                    else if (processingSAM.Mnemonic == "attr_is_uom")
                        dataMnemonic = "UCUM";

                    // If we're using value data, ensure the appropriate data is loaded
                    if (!string.IsNullOrEmpty(dataMnemonic) && message.RefData.GetValueList(dataMnemonic) == null)
                        throw new Exception("Failed to load value data for [" + dataMnemonic + "]");
                    // Get the executable SAM
                    SAMBase samWorker = message.GetSAMWorker(processingSAM.Mnemonic, _SAMReferenceDataService);

                    // If we got the default sam, log that
                    if (samWorker.SAMObject.Mnemonic == "default")
                        _Logger.Log(LogLevel.Warning, "SAM [" + processingSAM.Mnemonic + "] wasn't found in the cache. Executing default SAM instead.");

                    PIQISAMRequest? samRequest = new PIQISAMRequest();
                    PIQISAMResponse? samResult = null;

                    // Create SAM request object
                    samRequest.MessageObject = messageModelItem;
                    if (evaluationCriteriaParameters != null && evaluationCriteriaParameters.Count > 0)
                    {
                        // Add processing URL as a parameter
                        if (evaluationCriteriaProcessingURL != null)
                            samRequest.AddParameter("Processing URL", evaluationCriteriaProcessingURL);
                        for (int i = 0; i < evaluationCriteriaParameters.Count; i++)
                        {
                            EvaluationCriteriaParameter ecp = evaluationCriteriaParameters[i];
                            SAMParameter? samParameter = samWorker.SAMObject.Parameters?.Where(t => t.Name == ecp.ParameterName).FirstOrDefault();
                            if (samParameter == null || samParameter.Name == null || ecp.ParameterValue == null)
                                _Logger.Log(LogLevel.Warning, $"Invalid SAM parameter object: {samParameter?.Name}, {ecp.ParameterValue}");
                            else
                            {
                                if (samParameter.ParameterType == SAMParameterTypeEnum.Object)
                                {
                                    var parameterValues = JsonConvert.DeserializeObject<JObject>(ecp.ParameterValue);
                                    if (parameterValues == null) throw new Exception($"Invalid or missing parameter value(s): {ecp.ParameterName}");

                                    // Loop through object and add each property as a new parameter
                                    foreach (var property in parameterValues.Properties())
                                    {
                                        if (property == null || property.Name == null || property.Value == null)
                                            throw new Exception($"Invalid property in {ecp.ParameterName} criteria parameter object");
                                        samRequest.AddParameter(property.Name, property.Value.ToString());
                                    }
                                }
                                else
                                    samRequest.AddParameter(samParameter.Name, ecp.ParameterValue);
                            }
                        }
                    }

                    //Execute the SAM
                    samResult = await samWorker.EvaluateAsync(samRequest);

                    // Validate that we ran successfully
                    if (samResult == null || samResult.ResultState == SAMResultStateEnum.ERRORED)
                        throw new Exception($"{processingSAM.Mnemonic} failed to process {(samResult != null ? ": " + samResult?.ErrorMessage : "")}");

                    // Fail the criteria SAM if it or one of its dependencies fails
                    if (samResult.Failed)
                    {
                        initialProcessingPIQISAM.Fail(processingSAM.Mnemonic);
                        break;
                    }

                }

                // No exceptions means the method succeeded
                if (initialProcessingPIQISAM.ProcessingState != SAMProcessStateEnum.Failed) initialProcessingPIQISAM.Pass();
            }
            catch
            {
                throw;
            }
        }

        private bool EvalIsValid(EvaluationCriterion evaluationCriterion, SAM sam)
        {
            try
            {
                // Default state is passed
                bool passed = true;

                // If there are parameters, go through each SAM parameter, find the matching evaluation criteria parameter and ensure it's populated
                if (sam.Parameters != null)
                {
                    foreach (SAMParameter samParameter in sam.Parameters)
                    {
                        // If there are no parameters in the criterion, the evaluation is invalid
                        if (evaluationCriterion.SAMParameters != null)
                        {
                            EvaluationCriteriaParameter? evaluationCriteriaParameter = evaluationCriterion.SAMParameters.FirstOrDefault(ecsp => ecsp.ParameterName == samParameter.Name);
                            if (evaluationCriteriaParameter == null || string.IsNullOrEmpty(evaluationCriteriaParameter.ParameterValue)) passed = false;
                        }
                        else passed = false;
                    }
                }

                return passed;
            }
            catch
            {
                throw;
            }
        }
        #endregion
    }
}
