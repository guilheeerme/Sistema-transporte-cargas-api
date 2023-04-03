namespace Application.ViewModels
{
    public class StatisticsViewModel
    {
        public double totalCost { get; set; }
        public double totalKm { get; set; }
        public int totalTrucks { get; set; }
        public int totalProducts { get; set; }
        public double totalWeight { get; set; }
        public double averageKmCost { get; set; }
        public double averageTypeProductCost { get; set; }
        public List<CitiesRouteViewModel> citiesRoute { get; set; }

        public class CitiesRouteViewModel
        {
            public string firstCity { get; set; }
            public string lastCity { get; set; }
            public double cost { get; set; }
        }
    }
}
