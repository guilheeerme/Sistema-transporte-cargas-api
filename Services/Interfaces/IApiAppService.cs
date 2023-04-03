using Application.InputModels;
using Application.ViewModels;

namespace Application.Interfaces
{
    public interface IApiAppService
    {
        Task<List<string>> GetCitiesAsync();
        Task<CalcRouteViewModel> CalcRouteAsync(CalcRouteInputModel input);
        Task<List<ProductViewModel>> GetProductsAsync();
        Task<CalcTransportValuesViewModel> CalcTransportValuesAsync(CalcTransportValuesInputModel[] input);
        Task<List<StatisticsViewModel>> GetStatisticsAsync();
    }
}
