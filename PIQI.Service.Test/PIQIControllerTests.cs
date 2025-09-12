using Microsoft.AspNetCore.Mvc.Testing;
using Newtonsoft.Json;
using PIQI.Service.WebTesting.Rest;
using PIQI_Engine.Server.Models;
using System.Net;

namespace PIQI.Service.Test;

public class PIQIControllerTests : RestClient, IClassFixture<WebApplicationFactory<PIQI_Engine.Server.Program>>
{
    private readonly HttpClient _client;
    public PIQIControllerTests(WebApplicationFactory<PIQI_Engine.Server.Program> factory) : base(factory.CreateClient())
    {
        var application = new PIQIEngineService();
        _client = application.CreateClient();
    }

    [Theory]
    [InlineData("/PIQI/ScoreMessage")]
    public async Task ScoresMessage_ReturnsExpectedResponse(string endpoint)
    {
        // Arrange
        var piqiRequest = new PIQIRequest {
            DataProviderID = "TestProvider",
            DataSourceID = "TestSource",
            PIQIModelMnemonic = "PAT_CLINICAL_V1",
            EvaluationRubricMnemonic = "USCDI_V3",
            MessageID = "Msg001",
            MessageData = File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData/Test1_PIQI.json"))
        };
        var result = new PIQIResponse();
        var requestContent = new StringContent(JsonConvert.SerializeObject(piqiRequest),System.Text.Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync(endpoint, requestContent);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var contentType = response.Content.Headers.ContentType.MediaType;
        if (contentType == "text/plain" || contentType == "application/json")
        {
            var responseBody = await response.Content.ReadAsStringAsync();
            Console.WriteLine("responseBody: " + responseBody);
            result = JsonConvert.DeserializeObject<PIQIResponse>(responseBody);

            Assert.NotNull(result);
        }

        // You can't directly compare objects here without setting up the expectedResponse properly
        Assert.Equal(result.Succeeded, true);
        Assert.Equal(result.ScoringData.MessageResults.PIQIScore, 75);
        Assert.Equal(result.ScoringData.MessageResults.Numerator, 3);
        Assert.Equal(result.ScoringData.MessageResults.Denominator, 4);
        Assert.Equal(result.ScoringData.MessageResults.CriticalFailureCount, 0);
        Assert.Equal(result.ScoringData.MessageResults.WeightedPIQIScore, 75);
        Assert.Equal(result.ScoringData.MessageResults.WeightedNumerator, 3);
        Assert.Equal(result.ScoringData.MessageResults.WeightedDenominator, 4);
    }

    [Theory]
    [InlineData("/PIQI/ScoreAuditMessage")]
    public async Task ScoreAuditMessage_ReturnsExpectedResponse(string endpoint)
    {
        // Arrange
        var piqiRequest = new PIQIRequest
        {
            DataProviderID = "TestProvider",
            DataSourceID = "TestSource",
            PIQIModelMnemonic = "PAT_CLINICAL_V1",
            EvaluationRubricMnemonic = "USCDI_V3",
            MessageID = "Msg001",
            MessageData = File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData/Test1_PIQI.json"))
        };
        var result = new PIQIResponse();
        var requestContent = new StringContent(JsonConvert.SerializeObject(piqiRequest),System.Text.Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync(endpoint, requestContent);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var contentType = response.Content.Headers.ContentType.MediaType;
        if (contentType == "text/plain" || contentType == "application/json")
        {
            var responseBody = await response.Content.ReadAsStringAsync();
            result = JsonConvert.DeserializeObject<PIQIResponse>(responseBody);

            Assert.NotNull(result);
        }

        // You can't directly compare objects here without setting up the expectedResponse properly
        Assert.NotNull(result.AuditedMessage);
        Assert.Equal(result.Succeeded, true);
        Assert.Equal(result.ScoringData.MessageResults.PIQIScore, 75);
        Assert.Equal(result.ScoringData.MessageResults.Numerator, 3);
        Assert.Equal(result.ScoringData.MessageResults.Denominator, 4);
        Assert.Equal(result.ScoringData.MessageResults.CriticalFailureCount, 0);
        Assert.Equal(result.ScoringData.MessageResults.WeightedPIQIScore, 75);
        Assert.Equal(result.ScoringData.MessageResults.WeightedNumerator, 3);
        Assert.Equal(result.ScoringData.MessageResults.WeightedDenominator, 4);
    }

}