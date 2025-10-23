namespace V3.Admin.Backend.Models
{
    public class BaseResponseModel<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public T Data { get; set; }
    }

    public class BaseResponseModel : BaseResponseModel<object>
    {
    }
}