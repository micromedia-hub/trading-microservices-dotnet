using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Windows.Forms;

namespace Tools.TokenGenerator
{
    /// <summary>
    /// Sample usage: --sub demo-user-1 --scope api.read --hours 8
    /// </summary>
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            const string Audience = "trading-api";
            const string SigningKey = "super-secret-test-key-change-me-32+chars";
            const int Hours = 8;
            string userRef = $"demo-user-{Random.Shared.Next(0, 11)}";
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, userRef)
            };
            var token = new JwtSecurityToken(
                issuer: null,
                audience: Audience,
                claims: claims,
                notBefore: DateTime.UtcNow.AddMinutes(-1),
                expires: DateTime.UtcNow.AddHours(Hours),
                signingCredentials: credentials);
            string jwt = new JwtSecurityTokenHandler().WriteToken(token);
            Console.WriteLine(jwt);
            try
            {
                Clipboard.SetText(jwt);
                Console.WriteLine("Token copied to clipboard.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Could not copy token to clipboard: {ex.Message}");
            }
        }
    }
}
