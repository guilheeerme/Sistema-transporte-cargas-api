namespace Application.InputModels
{
    public class CalcTransportValuesInputModel
    {
        public string city { get; set; }
        public List<CalcTransportValuesProductsInputModel> products { get; set; }

        public class CalcTransportValuesProductsInputModel
        { 
            public int id { get; set; }
            public string name { get; set; }
            public int quantity { get; set; }
            public double weight { get; set; }
        }
    }
}
