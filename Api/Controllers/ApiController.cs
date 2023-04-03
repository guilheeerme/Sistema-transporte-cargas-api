using Application.InputModels;
using Application.Interfaces;
using Application.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    // Controller, possiu todas as chamadas disponíveis na api, ou seja, todos os métodos que podem ser consultados
    // Cada método do controller chama um método do appService, que vai retornar o resultado esperado
    [Route("api/")]
    [ApiController]
    public class ApiController : ControllerBase
    {
        private readonly IApiAppService _ApiAppService;

        public ApiController(IApiAppService ApiAppService)
        {
            _ApiAppService = ApiAppService;
        }

        // Get para consultar dados
        [HttpGet("GetCities")]
        public async Task<ActionResult<List<string>>> GetCitiesAsync()
        {
            var cities = await _ApiAppService.GetCitiesAsync();
            return Ok(cities);
        }

        // Post para consultar os valores da rota desejada, informando um body com os dados na request
        [HttpPost("CalcRoute")]
        public async Task<ActionResult<CalcRouteViewModel>> CalcRouteAsync([FromBody] CalcRouteInputModel inputModel)
        {
            var calcRouteViewModel = await _ApiAppService.CalcRouteAsync(inputModel);
            return Ok(calcRouteViewModel);
        }

        // Get para consultar os produtos
        [HttpGet("GetProducts")]
        public async Task<ActionResult<List<ProductViewModel>>> GetProductsAsync()
        {
            var products = await _ApiAppService.GetProductsAsync();
            return Ok(products);
        }

        // Post para cadastrar os dados do transporte e para consultar os valores do transporte, informando um body com os dados da request.
        [HttpPost("CalcTransportValues")]
        public async Task<ActionResult<CalcTransportValuesViewModel>> CalcTransportValuesAsync([FromBody] CalcTransportValuesInputModel[] input)
        {
            var calcTransportValues = await _ApiAppService.CalcTransportValuesAsync(input);
            return Ok(calcTransportValues);
        }

        // Get para pegar os dados dos transportes já realizados
        [HttpGet("GetStatistics")]
        public async Task<ActionResult<List<StatisticsViewModel>>> GetStatisticsAsync()
        { 
            var statistics = await _ApiAppService.GetStatisticsAsync();
            return Ok(statistics);
        }
    }
}
