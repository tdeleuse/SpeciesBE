using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using SpeciesBE.Services;

namespace SpeciesBE.Tests;

public class SpeciesApiServiceTests
{
    private class TestHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;
        public TestHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) => _responder = responder;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(_responder(request));
    }

    [Fact]
    public async Task SearchSpecies_ReturnsMappedSpecies()
    {
        var json = "{\"results\":[{\"id\":123,\"name\":\"Vulpes vulpes\",\"preferred_common_name\":\"Renard roux\",\"rank\":\"species\",\"default_photo\":{\"medium_url\":\"https://example.com/med.jpg\",\"square_url\":\"https://example.com/sq.jpg\",\"original_url\":\"https://example.com/orig.jpg\"}}]}";

        var handler = new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        });

        var http = new HttpClient(handler);
        var svc = new SpeciesApiService(http);

        var result = await svc.SearchSpecies("fox");

        Assert.NotNull(result);
        Assert.Single(result);
        var s = result[0];
        Assert.Equal(123, s.Id);
        Assert.Equal("Vulpes vulpes", s.ScientificName);
        Assert.Equal("Renard roux", s.CommonName);
        Assert.Equal("species", s.Rank);
        Assert.Equal("https://example.com/med.jpg", s.PhotoUrl);
    }

    [Fact]
    public async Task GetTaxonDetails_ReturnsDetails()
    {
        var json = "{\"results\":[{\"id\":456,\"name\":\"Canis lupus\",\"preferred_common_name\":\"Loup\",\"rank\":\"species\",\"default_photo\":{\"medium_url\":\"https://example.com/wolf_med.jpg\"},\"wikipedia_summary\":\"Résumé\",\"wikipedia_url\":\"https://fr.wikipedia.org/wiki/Canis_lupus\",\"ancestor_ids\":[1,2,3]}]}";

        var handler = new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        });

        var http = new HttpClient(handler);
        var svc = new SpeciesApiService(http);

        var details = await svc.GetTaxonDetails(456);

        Assert.NotNull(details);
        Assert.Equal(456, details.Id);
        Assert.Equal("Canis lupus", details.ScientificName);
        Assert.Equal("Loup", details.CommonName);
        Assert.Equal("https://example.com/wolf_med.jpg", details.PhotoUrl);
        Assert.Equal("Résumé", details.WikipediaSummary);
        Assert.Equal("https://fr.wikipedia.org/wiki/Canis_lupus", details.WikipediaUrl);
        Assert.Equal(3, details.AncestorIds.Count);
    }
}
