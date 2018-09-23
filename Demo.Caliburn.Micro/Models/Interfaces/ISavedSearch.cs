using System.Threading.Tasks;

namespace Demo.Caliburn.Micro.Models.Interfaces
{
    public interface ISavedSearch
    {
        string NumberPlate { get; }
        bool IsLoading { get; }

        Task Save();
        Task Delete();
    }
}