using Application.InputModels;
using Application.Interfaces;
using Application.ViewModels;
using Microsoft.Extensions.Caching.Memory;
using Models.Models;
using System.Net.WebSockets;
using static Application.ViewModels.StatisticsViewModel;

namespace Application.Services
{
    public class ApiAppService : IApiAppService
    {
        // OBS: para armazenamento do dados foi utilizado a memória em cache

        // para possibilitar o armazenamento em cache:
        private IMemoryCache _cache;
        private MemoryCacheEntryOptions _cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromSeconds(60))
                    .SetAbsoluteExpiration(TimeSpan.FromSeconds(3600))
                    .SetPriority(CacheItemPriority.Normal)
                    .SetSize(1024);

        private const string _csvDataCache = "csvDataCache";
        private const string _productsCache = "productsCache";
        private const string _transportCache = "_transportCache";

        private List<List<string>> _csvData;
        private List<string> _trucks = new List<string>();

        public ApiAppService(IMemoryCache cache) 
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        public async Task<List<string>> GetCitiesAsync()
        {
            // extrai os dados do csv
            await TryGetCsvFromCacheAsync();

            // retorna as cidades disponíveis no csv
            return _csvData.First();
        }

        public async Task<List<ProductViewModel>> GetProductsAsync()
        {
            // Tenta pegar produtos cadastrados, faz o cadastro caso não tenha produtos cadastrados
            if (!_cache.TryGetValue(_productsCache, out List<ProductViewModel> productsData))
            {
                await InsertDefaultProductsAsync();
            }

            // retorna os produtos cadastrados
            return _cache.Get<List<ProductViewModel>>(_productsCache);
        }

        public async Task<CalcRouteViewModel> CalcRouteAsync(CalcRouteInputModel input) 
        {
            // extrai os dados do csv
            await TryGetCsvFromCacheAsync();

            // monta a viewModel de retorno
            var calcRouteViewModel = new CalcRouteViewModel();
            // consulta a quantidade de Kms entre a cidade inicial e final
            calcRouteViewModel.quantityKm = await GetKmBetweenCitiesAsync(input.firstCity, input.lastCity);
            // consulta o custo total com o tipo de caminhão selecionado
            calcRouteViewModel.totalCost = await GetCostBetweenCitiesAsync(calcRouteViewModel.quantityKm, await ConvertToPriceAsync(input.truckType));
            //retorna a viewModel
            return calcRouteViewModel;
        }

        private async Task<int> GetKmBetweenCitiesAsync(string firstCity, string lastCity)
        {
            // pega o nome da cidade de saída
            var posFirstCity = _csvData.First().FindIndex(x => x == firstCity);
            // pega o nome da cidade destino
            var posLastCity = _csvData.First().FindIndex(x => x == lastCity);

            // retorna os Kms entre as duas cidades
            return int.Parse(_csvData[posFirstCity + 1][posLastCity]);
        }

        private async Task<double> GetCostBetweenCitiesAsync(int quantityKm, double costKm)
        {
            // retorna o calculo de km x custo por km
            return Math.Round(quantityKm * costKm, 2);
        }

        public async Task<CalcTransportValuesViewModel> CalcTransportValuesAsync(CalcTransportValuesInputModel[] cityAndProducts)
        {
            // OBS: "transport" é referente ao cálculo geral, ou seja, de tudo que envolve o transporte dos produtos de um ponto inicial até um ponto final, podendo haver outros pontos entre o inicial e final

            // extrai os dados do csv
            await TryGetCsvFromCacheAsync();

            // cria a classe para armazenar os dados
            var transport = new TransportModel();
            transport.citiesRoutes = new List<CitiesRouteModel>();

            // para cada trecho entre uma cidade e outra
            for (var i = 0; i < cityAndProducts.Length - 1; i++)
            {
                // pega o nome da cidade de saída
                var firstCity = cityAndProducts[i].city;
                // pega o nome da cidade destino
                var lastCity = cityAndProducts[i + 1].city;
                // calcula a distancia entre as duas
                var distance = await GetKmBetweenCitiesAsync(firstCity, lastCity);

                // adiciona os valores para armazenamento
                transport.citiesRoutes.Add(new CitiesRouteModel()
                {
                    firstCity = firstCity,
                    lastCity = lastCity,
                    distance = distance
                });

                // adiciona a distancia entre as duas cidades no total de km do transporte
                transport.totalKm += distance;
                // adiciona o número de produtos no total de produtos do transporte

                // para cada cidade com produto para serem entregues, calcula o peso dos produtos e adiciona no total de peso de produtos do transporte
                foreach (var product in cityAndProducts[i+1].products)
                {
                    transport.totalProducts += product.quantity;
                    transport.totalWeight += product.quantity * product.weight;
                }
            }

            // calcula e adiciona o valor total do transporte
            transport.totalCost = await GetTotalCostAndAddTruckAsync(transport.totalKm, transport.totalWeight);
            // adiciona o total de caminhões necessários no transporte
            transport.totalTrucks = _trucks.Count();
            // calcula e adiciona o valor médio do km;
            transport.averageKmCost = transport.totalCost / transport.totalKm;

            foreach (var truck in _trucks)
            {
                transport.totalPriceTrucks += await ConvertToPriceAsync(truck);
            }


            // pega os transportes já feitos, adiciona o transporte novo na lista e salva
            var existentTransports = _cache.Get<List<TransportModel>>(_transportCache);
            if (existentTransports == null)
            {
                existentTransports = new List<TransportModel>();
            }
            existentTransports.Add(transport);

            _cache.Set(_transportCache, existentTransports, _cacheEntryOptions);

            // adiciona as infos necessárias na view model de retorno
            var calcTransportValuesViewModel = new CalcTransportValuesViewModel();
            calcTransportValuesViewModel.totalDistance = transport.totalKm;
            calcTransportValuesViewModel.totalCost = transport.totalCost;
            calcTransportValuesViewModel.trucks = _trucks;

            return calcTransportValuesViewModel;
        }

        public async Task<List<StatisticsViewModel>> GetStatisticsAsync()
        {
            // pega os dados dos transportes armazenados e retorna em uma viewModel
            var statisticsViewModel = new List<StatisticsViewModel>();
            var transports = _cache.Get<List<TransportModel>>(_transportCache);

            if (transports != null)
            { 
                foreach (var transport in transports)
                {
                    var transportStatistics = new StatisticsViewModel();
                    transportStatistics.totalCost = transport.totalCost;
                    transportStatistics.totalKm = transport.totalKm;
                    transportStatistics.totalTrucks = transport.totalTrucks;
                    transportStatistics.totalProducts = transport.totalProducts;
                    transportStatistics.totalWeight = transport.totalWeight;
                    transportStatistics.averageKmCost = transport.averageKmCost;
                    transportStatistics.citiesRoute = new List<CitiesRouteViewModel>();

                    foreach (var citiesRoutes in transport.citiesRoutes)
                    {
                        transportStatistics.citiesRoute.Add(new CitiesRouteViewModel()
                        {
                            firstCity = citiesRoutes.firstCity,
                            lastCity = citiesRoutes.lastCity,
                            cost = citiesRoutes.distance * transport.totalPriceTrucks
                        });
                    }

                    statisticsViewModel.Add(transportStatistics);
                }
            }

            return statisticsViewModel;
        }

        private async Task<double> GetTotalCostAndAddTruckAsync(double totalDistance, double totalWeight)
        {
            const string smalltruck = "Pequeno";
            const string mediumTruck = "Médio";
            const string bigTruck = "Grande";

            var totalCost = 0.0;
            var truck = "";
            var remainingKgValue = totalWeight;

            // Os valores 8, 2 e 1 são para determinar qual caminhão é mias vantajoso
            // Se valorToneladas for maior que 8 é mais vantajoso utilizar caminhões grandes
            // Se valorToneladas for igual ou menor que 8, é mais vantajoso utilizar caminhões médios
            // Se valorToneladas for igual ou menor que 2, é mais vantajoso utilizar caminhões pequenos
            // Ex: 4 caminhões médios transportam 8 toneladas e custam mais barato que um caminhão grande que transporta 10

            while (remainingKgValue > 0)
            {
                // calcula o total de toneladas no total de peso dos produtos
                var tonsValue = remainingKgValue / 1000;

                // Se valorToneladas for igual ou menor que 8, é mais vantajoso utilizar com caminhões médios
                if (tonsValue <= 8)
                {
                    // Se valorToneladas for igual ou menor que 2, é mais vantajoso utilizar caminhões pequenos
                    if (tonsValue <= 2)
                    {
                        // Se valorToneladas for igual ou menor que 1, quer dizer que tem 1t ou menos
                        if (tonsValue <= 1)
                        {
                            // Zera o peso restante
                            truck = smalltruck;
                            remainingKgValue = 0;
                        }
                        // Se valorToneladas for maior que 1, quer dizer que tem 1 ou mais toneladas
                        else
                        {
                            // Calcula o peso restante
                            remainingKgValue = remainingKgValue - 1000;
                            truck = smalltruck;
                        }
                    }
                    // Se valorToneladas for igual ou maior que 2, quer dizer que tem 2 ou mais toneladas
                    else
                    {
                        // Calcula o peso restante
                        remainingKgValue = remainingKgValue - 4000;
                        truck = mediumTruck;
                    }
                }
                // Se valorToneladas for igual ou maior que 8 é mais vantajoso utilizar caminhões grandes
                else
                {
                    // Calcula o peso restante
                    remainingKgValue = remainingKgValue - 10000;
                    truck = bigTruck;
                }
                // Adiciona o caminhão na lista
                _trucks.Add(truck);
                // Calcula e adiciona o valor da viagem com caminhão grande ao custo total
                totalCost += totalDistance * await ConvertToPriceAsync(truck);
            }

            return Math.Round(totalCost, 2);
        }

        #region Extra

        private async Task TryGetCsvFromCacheAsync()
        {
            // tenta consultados os dados do csv em cache
            if (!_cache.TryGetValue(_csvDataCache, out List<List<string>> csvData))
            {
                // se não encontrar, faz a leitura do csv e armazena os dados
                await GetDataFromCsvAsync();
            }

            // consulta os dados armazenados
            _csvData = _cache.Get<List<List<string>>>(_csvDataCache);
        }

        private async Task GetDataFromCsvAsync()
        {
            // pega os dados do csv disponível no diretório informado, com o nome de arquivo informado, e salva em memória
            var csv = new List<List<string>>();
            using (var reader = new StreamReader($@"C:/Users/giregu/Desktop/resolucaoExercicio/DNIT-Distancias.csv"))
            {
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    List<string> lineValues = line.Split(';').ToList();
                    csv.Add(lineValues);
                }
            }
            _cache.Set(_csvDataCache, csv, _cacheEntryOptions);
        }

        private async Task<double> ConvertToPriceAsync(string truckType)
        { 
            // retorna o preço por km do caminhão informado de acordo com o nome do caminhão
            switch (truckType)
            {
                case "Pequeno":
                    return 4.87;
                case "Médio":
                    return 11.92;
                case "Grande":
                    return 27.44;
                default:
                    return 0.0;
            }
        }

        private async Task InsertDefaultProductsAsync()
        {
            // adiciona alguns produtos default na memória em cache
            var products = new List<ProductViewModel>();

            var product1 = new ProductViewModel();
            product1.id = 1;
            product1.name = "Celular";
            product1.weight = 0.5;
            products.Add(product1);

            var product2 = new ProductViewModel();
            product2.id = 2;
            product2.name = "Geladeira";
            product2.weight = 60.0;
            products.Add(product2);

            var product3 = new ProductViewModel();
            product3.id = 3;
            product3.name = "Freezer";
            product3.weight = 100.0;
            products.Add(product3);

            var product4 = new ProductViewModel();
            product4.id = 4;
            product4.name = "Lavadora de roupas";
            product4.weight = 120;
            products.Add(product4);

            _cache.Set(_productsCache, products, _cacheEntryOptions);
        }

        #endregion
    }
}
