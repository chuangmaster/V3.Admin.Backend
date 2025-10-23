using System;
using Microsoft.AspNetCore.Mvc;
using V3.Admin.Backend.Models;

namespace V3.Admin.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CommonController : ControllerBase
    {
        [HttpGet]
        public BaseResponseModel Get()
        {
            return new BaseResponseModel
            {
                Success = true,
                Message = "This is a common response.",
                Data = new { Timestamp = DateTime.UtcNow },
            };
        }
    }
}
