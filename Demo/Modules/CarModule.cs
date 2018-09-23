using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Demo.Carter;
using Demo.Carter.Attributes;
using Demo.Modules.Models;
using RestSharp;

namespace Demo.Modules
{
    public class CarModule : CarterModuleBase
    {
        [Get("/{numberPlate}")]
        [Post("/")]
        public async Task<Response<Car>> GetCarInformationAsync(CarRequest request)
        {
            var carResponse = await GetCarAsync(request.NumberPlate).ConfigureAwait(false);

            if (carResponse.StatusCode == HttpStatusCode.NotFound)
                return ResponseBuilder.NotFound;

            if (carResponse.StatusCode != HttpStatusCode.OK)
                return ResponseBuilder.WithStatusCode(HttpStatusCode.InternalServerError)
                    .WithErrorBody("Unable to find car");

            return ResponseBuilder.Ok.WithBody(carResponse.Data);
        }

        [Get("/{numberPlate}/picture")]
        public async Task<Response<Func<Stream>>> GetCarPicture(string numberPlate)
        {
            var carResponse = await GetCarAsync(numberPlate).ConfigureAwait(false);

            if (carResponse.StatusCode == HttpStatusCode.NotFound)
                return ResponseBuilder.NotFound.ToResponse();

            if (carResponse.StatusCode != HttpStatusCode.OK)
                return ResponseBuilder.WithStatusCode(HttpStatusCode.InternalServerError)
                    .WithErrorBody("Unable to find car")
                    .ToResponse();
            var variant = carResponse.Data.Variant.Replace(" ", "%20");
            var url = $"https://www.google.com/search?q={carResponse.Data.Make}%20{carResponse.Data.Model}%20{variant}&tbm=isch";
            var client = new RestClient(url);
            var request = new RestRequest();
            var pictureSite = await Task.FromResult(client.DownloadData(request)).ConfigureAwait(false);
            var pictureUrl = GetUrls(Encoding.UTF8.GetString(pictureSite)).FirstOrDefault();

            if (pictureUrl == null)
                return ResponseBuilder.NotFound;

            client = new RestClient(pictureUrl);
            var picture = await Task.FromResult(client.DownloadData(request)).ConfigureAwait(false);

            var stream = new MemoryStream(picture);
            return ResponseBuilder.Ok.WithStreamBody(() => stream);
        }

        private static IReadOnlyList<string> GetUrls(string html)
        {
            var urls = new List<string>();
            var ndx = html.IndexOf("class=\"images_table\"", StringComparison.Ordinal);
            ndx = html.IndexOf("<img", ndx, StringComparison.Ordinal);

            while (ndx >= 0)
            {
                ndx = html.IndexOf("src=\"", ndx, StringComparison.Ordinal);
                ndx = ndx + 5;
                var ndx2 = html.IndexOf("\"", ndx, StringComparison.Ordinal);
                var url = html.Substring(ndx, ndx2 - ndx);
                urls.Add(url);
                ndx = html.IndexOf("<img", ndx, StringComparison.Ordinal);
            }
            return urls;
        }

        private async Task<IRestResponse<Car>> GetCarAsync(string numberPlate)
        {
            var client = new RestClient($"https://v1.motorapi.dk/vehicles/{numberPlate}");
            var request = new RestRequest {RequestFormat = DataFormat.Json};
            request.AddHeader("X-AUTH-TOKEN", "pzqzk8vrokklo4pce5aunrquauh0n97d");
            return await client.ExecuteTaskAsync<Car>(request);
        }
    }
}