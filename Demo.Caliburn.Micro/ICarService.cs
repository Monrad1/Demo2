using System;
using System.Windows.Media.Imaging;
using Demo.Caliburn.Micro.Models.Interfaces;

namespace Demo.Caliburn.Micro
{
    public interface ICarService
    {
        IObservable<ICar> GetCarByNumberPlate(string numberPlate);
        IObservable<BitmapImage> GetCarImageByNumberPlate(string numberPlate);
    }
}