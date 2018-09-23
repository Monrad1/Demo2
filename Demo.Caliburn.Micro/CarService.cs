using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Caliburn.Micro;
using Demo.Caliburn.Micro.Models;
using Demo.Caliburn.Micro.Models.Interfaces;
using Newtonsoft.Json;
using RestSharp;
using Sekoia.Caching;

namespace Demo.Caliburn.Micro
{
    public class CarService : ICarService
    {
        private readonly IElementCache<CarDto, string> _carCache;
        private readonly IRestClient _client = new RestClient("http://localhost:64772");
        private readonly IElementCache<ImageDto, string> _imageCache;

        public CarService()
        {
            var carBuilder = CacheBuilder.For<CarDto>()
                .WithIdExtractor(i => i.NumberPlate)
                .WithElementRetriever(i => FetchCarFromServer(i))
                .WithElementMaxAge(TimeSpan.FromMinutes(12));

            var imageBuilder = CacheBuilder.For<ImageDto>()
                .WithIdExtractor(i => i.NumberPlate)
                .WithElementRetriever(i => FetchImageFromServer(i))
                .WithElementMaxAge(TimeSpan.FromMinutes(12));

            _imageCache = imageBuilder.ElementCache;
            _carCache = carBuilder.ElementCache;
        }

        public IObservable<ICar> GetCarByNumberPlate(string numberPlate)
        {
            return _carCache.Get(numberPlate).Select(Car.Create);
        }

        public IObservable<BitmapImage> GetCarImageByNumberPlate(string numberPlate)
        {
            return _imageCache.Get(numberPlate).Select(i => ToImage(i.ImageAsBytes));
        }

        private IObservable<ImageDto> FetchImageFromServer(string numberPlate)
        {
            var request = new RestRequest($"/{numberPlate}/picture");
            var o = Task.FromResult(_client.DownloadData(request)).ToObservable();

            return o.Select(res => new ImageDto {ImageAsBytes = res, NumberPlate = numberPlate});
        }

        private IObservable<CarDto> FetchCarFromServer(string numberPlate)
        {
            //var fixture = new Fixture();
            //return Task.FromResult(fixture.Create<CarDto>()).ToObservable();
            var request = new RestRequest($"/{numberPlate}");
            var o = _client.ExecuteGetTaskAsync(request).ToObservable();

            return o.Select(res =>
            {
                if (res.StatusCode == HttpStatusCode.NotFound)
                    throw new CarNotFoundException();

                if (res.StatusCode != HttpStatusCode.OK)
                    throw new Exception();

                var dto = JsonConvert.DeserializeObject<CarDto>(res.Content);
                dto.NumberPlate = numberPlate;
                return dto;
            });
        }

        private static BitmapImage ToImage(byte[] array)
        {
            if (!array?.Any() ?? true)
                array = File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Assets/dummyCar.png"));
            Func<BitmapImage> imageFunc = null;
            Execute.OnUIThread(() =>
            {
                var image = new BitmapImage();
                image.BeginInit();
                image.StreamSource = new MemoryStream(array);
                image.EndInit();
                imageFunc = ()=> image;
            });

            return imageFunc();
        }

        private class Car : ICar, IEquatable<ICar>
        {
            private readonly CarDto _dto;

            private Car(CarDto dto)
            {
                _dto = dto ?? throw new ArgumentNullException(nameof(dto));
            }

            public string NumberPlate => _dto.NumberPlate;
            public string Make => _dto.Make;
            public string Model => _dto.Model;

            public bool Equals(ICar other)
            {
                return ToString().Equals(other?.ToString());
            }

            public static ICar Create(CarDto dto)
            {
                return new Car(dto);
            }

            public override string ToString()
            {
                return NumberPlate;
            }

            public override bool Equals(object obj)
            {
                return Equals((Car) obj);
            }

            public override int GetHashCode()
            {
                return ToString().GetHashCode();
            }
        }
    }
}