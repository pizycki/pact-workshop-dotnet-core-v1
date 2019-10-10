using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Consumer;
using FluentAssertions;
using NUnit.Framework;
using PactNet;
using PactNet.Mocks.MockHttpService;
using PactNet.Mocks.MockHttpService.Models;

namespace tests
{
    public class ConsumerPactClassFixture
    {
        public IPactBuilder PactBuilder { get; private set; }
        public IMockProviderService MockProviderService { get; private set; }

        public int MockServerPort => 9222;

        public string MockProviderServiceBaseUri => $"http://localhost:{MockServerPort}";

        public void Setup()
        {
            var pactConfig = new PactConfig
            {
                SpecificationVersion = "2.0.0",
                PactDir = @"..\..\..\..\..\pacts",
                LogDir = @".\pact_logs"
            };

            PactBuilder = new PactBuilder(pactConfig)
                .ServiceConsumer("Consumer") // Define the name of our Consumer project (Consumer) which will be used in other Pact Test projects. 
                .HasPactWith("Provider"); // Define the relationships our Consumer project has with others. In this case, just one called "Provider" this name will map to the same name used in the Provider Project Pact tests.

            MockProviderService = PactBuilder.MockService(MockServerPort);
        }

        public void TearDown()
        {
            // This will save the pact file once finished.
            PactBuilder.Build();
        }
    }

    public class Tests
    {
        private ConsumerPactClassFixture _consumerPactFixture;
        private IMockProviderService _mockProviderService;
        private string _mockProviderServiceBaseUri;

        [OneTimeSetUp]
        public void Setup()
        {
            _consumerPactFixture = new ConsumerPactClassFixture();
            _consumerPactFixture.Setup();
            _mockProviderService = _consumerPactFixture.MockProviderService;
            _mockProviderService.ClearInteractions(); //NOTE: Clears any previously registered interactions before the test is run
            _mockProviderServiceBaseUri = _consumerPactFixture.MockProviderServiceBaseUri;
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            // This will save the pact file once finished.
            _consumerPactFixture.TearDown();
        }

        [Test]
        public async Task ItHandlesInvalidDateParam()
        {
            // Arange
            var invalidRequestMessage = "validDateTime is not a date or time";
            _mockProviderService.Given("There is data")
                .UponReceiving("A invalid GET request for Date Validation with invalid date parameter")
                .With(new ProviderServiceRequest
                {
                    Method = HttpVerb.Get,
                    Path = "/api/provider",
                    Query = "validDateTime=lolz"
                })
                .WillRespondWith(new ProviderServiceResponse
                {
                    Status = 400,
                    Headers = new Dictionary<string, object>
                    {
                        {"Content-Type", "application/json; charset=utf-8"}
                    },
                    Body = new
                    {
                        message = invalidRequestMessage
                    }
                });

            // Act
            HttpResponseMessage result = await ConsumerApiClient.ValidateDateTimeUsingProviderApi("lolz", _mockProviderServiceBaseUri);
            string resultBodyText = await result.Content.ReadAsStringAsync();

            // Assert
            resultBodyText.Should().Contain(invalidRequestMessage);
        }
    }
}