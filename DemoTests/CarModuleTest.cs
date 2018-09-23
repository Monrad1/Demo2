using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Demo;
using Demo.Modules.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Newtonsoft.Json;
using Xunit;

namespace DemoTests
{
    public class CarModuleTest
    {
        private readonly HttpClient _client;

        public CarModuleTest()
        {
            var server = new TestServer(new WebHostBuilder().UseStartup<Startup>());
            _client = server.CreateClient();
        }

        [Theory]
        [InlineData("GG53931")]
        public async Task Should_return_Car_Information(string numberPlate)
        {
            var res = await _client.GetAsync($"/{numberPlate}").ConfigureAwait(false);
            var json = await res.Content.ReadAsStringAsync().ConfigureAwait(false);
            var car = JsonConvert.DeserializeObject<Car>(json);

            Assert.Equal(HttpStatusCode.OK, res.StatusCode);
            Assert.Equal("PEUGEOT", car.Make);
        }

        [Theory]
        [InlineData("GG53931")]
        public async Task Should_return_Car_Information_post(string numberPlate)
        {
            var obj = new
            {
                NumberPlate = numberPlate,
            };

            var res = await _client.PostAsync("/", new StringContent(JsonConvert.SerializeObject(obj))).ConfigureAwait(false);
            var json = await res.Content.ReadAsStringAsync().ConfigureAwait(false);
            var car = JsonConvert.DeserializeObject<Car>(json);

            Assert.Equal(HttpStatusCode.OK, res.StatusCode);
            Assert.Equal("PEUGEOT", car.Make);
        }

        [Fact]
        public async Task Should_return_bad_request_Car_Information()
        {
            var obj = new
            {
                NumberPlate = string.Empty,
            };

            var res = await _client.PostAsync("/", new StringContent(JsonConvert.SerializeObject(obj))).ConfigureAwait(false);

            Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        }

        [Theory]
        [InlineData("GG53931")]
        public async Task Should_return_Car_Picture(string numberPlate)
        {
            var res = await _client.GetAsync($"/{numberPlate}/picture").ConfigureAwait(false);

            Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        }

        [Theory]
        [InlineData("GT53931")]
        public async Task Should_return_not_found_Car_Picture(string numberPlate)
        {
            var res = await _client.GetAsync($"/{numberPlate}/picture").ConfigureAwait(false);

            Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
        }

        [Theory]
        [InlineData("GT53931")]
        public async Task Should_return_not_found_Car_Information(string numberPlate)
        {
            var res = await _client.GetAsync($"/{numberPlate}").ConfigureAwait(false);

            Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
        }
    }
}