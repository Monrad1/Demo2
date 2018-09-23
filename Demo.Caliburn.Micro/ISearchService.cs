using System;
using System.Collections.Generic;
using Demo.Caliburn.Micro.Models.Interfaces;

namespace Demo.Caliburn.Micro
{
    public interface ISearchService
    {
        IObservable<IReadOnlyList<ISavedSearch>> GetSavedSearchs();
        ISavedSearch GetEmpty(string numberPlate);
    }
}