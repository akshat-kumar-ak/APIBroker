namespace APIBroker.Models
{
    public class ApiResponse
    {
        public string Id { get; set; }
        public string Endpoint { get; set; }

        public ApiResponse()
        {
            this.Id = string.Empty;
            this.Endpoint = string.Empty;
        }
    }
}
