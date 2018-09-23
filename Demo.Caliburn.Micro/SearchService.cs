using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using Caliburn.Micro;
using Demo.Caliburn.Micro.Models;
using Demo.Caliburn.Micro.Models.Interfaces;
using Newtonsoft.Json;
using Sekoia.Caching;

namespace Demo.Caliburn.Micro
{
    public class SearchService : ISearchService
    {
        private readonly IElementCache<SearchDto, string> _serachCache;
        private readonly IArrayCache<SearchDto, string> _serachsCache;

        public SearchService()
        {
            var searchBuilder = CacheBuilder.For<SearchDto>()
                                            .WithIdExtractor(i => i?.NumberPlate ?? "")
                                            .WithElementRetriever(i => FetchSerach(i))
                                            .WithElementMaxAge(TimeSpan.FromMinutes(1))
                                            .WithArrayMaxAge(TimeSpan.FromMinutes(1));

            _serachCache = searchBuilder.ElementCache;
            _serachsCache = searchBuilder.GetArrayCache<string>(FetchSerachs);
        }

        public IObservable<IReadOnlyList<ISavedSearch>> GetSavedSearchs()
        {
            return _serachsCache.Get("SEARCH").Select(i => i.Select(ii => Search.Create(ii, this)).ToList());
        }

        public ISavedSearch GetEmpty(string numberPlate)
        {
            return Search.Create(new SearchDto { NumberPlate = numberPlate }, this);
        }

        private IObservable<SearchDto> FetchSerachs(string key)
        {
            var savedSearchsAsJson = Properties.Settings.Default.SavedSearchs;
            var savedSearchs = JsonConvert.DeserializeObject<IReadOnlyList<SearchDto>>(savedSearchsAsJson);

            return Task.FromResult(savedSearchs ?? Enumerable.Empty<SearchDto>()
                       .Where(x => x?.NumberPlate != null)
                       .ToList())
                       .ToObservable()
                       .SelectMany(x => x);
        }

        private IObservable<SearchDto> FetchSerach(string numberPlate)
        {
            var savedSearchsAsJson = Properties.Settings.Default.SavedSearchs;
            var savedSearch = JsonConvert.DeserializeObject<IReadOnlyList<SearchDto>>(savedSearchsAsJson).FirstOrDefault(x => x.NumberPlate.Equals(numberPlate));

            if (savedSearch == null)
                throw new SearchNotFoundException();

            return Task.FromResult(savedSearch).ToObservable();
        }

        private class Search : PropertyChangedBase, ISavedSearch
        {
            private readonly SearchDto _dto;
            private readonly SearchService _parent;
            private bool _isLoading;

            public string NumberPlate => _dto.NumberPlate;

            public bool IsLoading
            {
                get => _isLoading;
                private set => Set(ref _isLoading, value);
            }

            private Search(SearchDto dto, SearchService parent)
            {
                _dto = dto ?? throw new ArgumentNullException(nameof(dto));
                _parent = parent ?? throw new ArgumentNullException(nameof(parent));
            }
            
            public Task Save()
            {
                IsLoading = true;
                var savedSearchsAsJson = Properties.Settings.Default.SavedSearchs;
                var savedSearchs = JsonConvert.DeserializeObject<List<SearchDto>>(savedSearchsAsJson);
                savedSearchs = savedSearchs ?? new List<SearchDto>();
                var dto = new SearchDto {NumberPlate = NumberPlate};
                if (!savedSearchs.Contains(dto))
                    savedSearchs.Add(dto);

                Properties.Settings.Default.SavedSearchs = JsonConvert.SerializeObject(savedSearchs);
                Properties.Settings.Default.Save();
                _parent._serachsCache.RefreshAll();
                return Task.CompletedTask;
            }

            public Task Delete()
            {
                IsLoading = true;
                var savedSearchsAsJson = Properties.Settings.Default.SavedSearchs;
                var savedSearchs = JsonConvert.DeserializeObject<List<SearchDto>>(savedSearchsAsJson);
                savedSearchs = savedSearchs ?? new List<SearchDto>();
                savedSearchs.Remove(new SearchDto { NumberPlate = NumberPlate });

                Properties.Settings.Default.SavedSearchs = JsonConvert.SerializeObject(savedSearchs);
                Properties.Settings.Default.Save();
                _parent._serachsCache.RefreshAll();
                return Task.CompletedTask;
            }
            
            public static ISavedSearch Create(SearchDto dto, SearchService parent)
            {
                return new Search(dto, parent);
            }
        }
    }
}
