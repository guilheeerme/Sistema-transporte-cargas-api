namespace Models.Models
{
    public class TransportModel
    {
        public double totalCost {  get; set; }
        public double totalKm { get; set; }
        public int totalTrucks { get; set; }
        public int totalProducts { get; set; }
        public double totalWeight { get; set; }
        public double averageKmCost { get; set; }
        public double averageTypeProductCost { get; set; }
        public double totalPriceTrucks { get; set; }
        public List<CitiesRouteModel> citiesRoutes { get; set; }
    }
}
