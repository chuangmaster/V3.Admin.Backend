using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using V3.Admin.Backend.Interfaces;
using V3.Admin.Backend.Models;

namespace V3.Admin.Backend.Controllers;

[Route("api/[controller]")]
public class AuthController : BaseApiController
{
    private readonly IAuthService authService;
    private readonly IConfiguration configuration;

    /// <summary>
    /// 建構函式，注入認證服務與組態。
    /// </summary>
    /// <param name="authService">認證服務</param>
    /// <param name="configuration">組態設定</param>
    public AuthController(IAuthService authService, IConfiguration configuration)
    {
        this.authService = authService;
        this.configuration = configuration;
    }

    /// <summary>
    /// 登入並取得 JWT Token。
    /// </summary>
    /// <param name="loginRequest">登入請求資料</param>
    /// <returns>JWT Token 或錯誤響應</returns>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest loginRequest)
    {
        try
        {
            var isValid = await authService.ValidateCredentialsAsync(
                loginRequest.Id,
                loginRequest.Pwd
            );

            if (!isValid)
            {
                return Unauthorized("帳號或密碼錯誤", ResponseCodes.InvalidCredentials);
            }

            var token = GenerateJwtToken(loginRequest.Id);
            return Success(new { token }, "登入成功");
        }
        catch (Exception)
        {
            // TODO: 可整合 Serilog 等記錄例外
            return InternalError("伺服器發生錯誤");
        }
    }

    /// <summary>
    /// 產生 JWT Token。
    /// </summary>
    /// <param name="id">使用者帳號</param>
    /// <returns>JWT Token 字串</returns>
    private string GenerateJwtToken(string id)
    {
        var jwtSettings = configuration.GetSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, id),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            // TODO: 可加入角色或權限等額外 Claims
        };

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
