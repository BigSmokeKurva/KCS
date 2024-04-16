namespace KCS.Server.Server.Controllers.Models
{
    public class ResponseModel
    {
        public string status { get; set; }
        public List<ErrorResponseModel> errors { get; set; }
    }
    public class ErrorResponseModel
    {
        public string type { get; set; }
        public string message { get; set; }
    }
}
