using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Media.Imaging;
using Caliburn.Micro;
using Demo.Caliburn.Micro.Models.Interfaces;

namespace Demo.Caliburn.Micro.ViewModels
{
    public class ShellViewModel : Screen
    {
        private readonly SerialDisposable _carServiceDisposable = new SerialDisposable();
        private readonly SerialDisposable _searchServiceDisposable = new SerialDisposable();
        private readonly ICarService _carService;
        private readonly ISearchService _searchService;

        private BindableCollection<ISavedSearch> _savedSearches = new BindableCollection<ISavedSearch>();
        public IReadOnlyList<ISavedSearch> SavedSearches
        {
            get => _savedSearches;
            private set => Set(ref _savedSearches, new BindableCollection<ISavedSearch>(value));
        }

        public IReadOnlyList<string> SavedSearchesAsString => _savedSearches.Select(i => i.NumberPlate).ToList();

        private ICar _car;
        public ICar Car
        {
            get => _car;
            private set => Set(ref _car, value);
        }

        private BitmapImage _image;
        public BitmapImage Image
        {
            get => _image;
            private set => Set(ref _image, value);
        }

        private bool _isLoadingCar;
        public bool IsLoadingCar
        {
            get => _isLoadingCar;
            private set => Set(ref _isLoadingCar, value);
        }

        private bool _isLoadingSavedSearch;
        public bool IsLoadingSavedSearch
        {
            get => _isLoadingSavedSearch;
            private set => Set(ref _isLoadingSavedSearch, value);
        }

        private string _currentSearch;
        public string CurrentSearch
        {
            get => _currentSearch;
            set
            {
                Set(ref _currentSearch, value);
                Search(value);
            }
        }

        [Obsolete("For designtime")]
        public ShellViewModel()
        {
            
        }

        public ShellViewModel(ICarService carService, ISearchService searchService)
        {
            _searchService = searchService ?? throw new ArgumentNullException(nameof(searchService));
            _carService = carService ?? throw new ArgumentNullException(nameof(carService));
        }

        protected override void OnActivate()
        {
            IsLoadingSavedSearch = true;
            _searchServiceDisposable.Disposable = _searchService.GetSavedSearchs()
                                                                .ObserveOn(TaskPoolScheduler.Default)
                                                                .SubscribeOn(TaskPoolScheduler.Default)
                                                                .Select(instances => instances.Where(instance => instance?.NumberPlate != null))
                                                                .DistinctUntilChanged(instances => string.Join("|", instances.Select(instance => instance.NumberPlate)))
                                                                .Select(instances => instances.OrderBy(instance => instance.NumberPlate))
                                                                .Subscribe(instances =>
                                                                          {
                                                                              var missing = instances.Where(instance => !_savedSearches.Contains(instance));
                                                                              var removed = _savedSearches.Where(saved => !instances.Contains(saved));

                                                                              _savedSearches.IsNotifying = false;
                                                                              _savedSearches.AddRange(missing);
                                                                              _savedSearches.RemoveRange(removed);
                                                                              _savedSearches.IsNotifying = true;
                                                                              _savedSearches.Refresh();
                                                                              NotifyOfPropertyChange(()=> SavedSearchesAsString);
                                                                              IsLoadingSavedSearch = false;
                                                                          }, OnError);
        }

        protected override void OnDeactivate(bool close)
        {
            base.OnDeactivate(close);

            _carServiceDisposable.Dispose();
            _searchServiceDisposable.Dispose();
        }

        public void SetCurrentSearch(ISavedSearch ss)
        {
            CurrentSearch = ss.NumberPlate;
        }

        public void Search(string numberPlate)
        {
            IsLoadingCar = true;
            _carServiceDisposable.Disposable = _carService.GetCarByNumberPlate(numberPlate)
                                                          .CombineLatest(_carService.GetCarImageByNumberPlate(numberPlate), (first, second) => (car:first, image:second))
                                                          .ObserveOn(TaskPoolScheduler.Default)
                                                          .SubscribeOn(TaskPoolScheduler.Default)
                                                          .DistinctUntilChanged(instance => instance.car.ToString())
                                                          .Throttle(TimeSpan.FromSeconds(5))
                                                          .Subscribe(async instance =>
                                                          {
                                                                        Car = instance.car;
                                                                        await Execute.OnUIThreadAsync(()=>Image = instance.image).ConfigureAwait(false);
                                                                        await _searchService.GetEmpty(numberPlate).Save().ConfigureAwait(false);
                                                                        IsLoadingCar = false;
                                                          }, OnError);
        }

        private void OnError(Exception exception)
        {
            IsLoadingCar = false;
            IsLoadingSavedSearch = false;
        }
    }
}
