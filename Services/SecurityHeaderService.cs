using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace CrudApi.Services
{
    public class SecurityHeaderService
    {
        private readonly IConfiguration _configuration;
        private readonly string _secretKey;  

        public SecurityHeaderService( IConfiguration configuration)
        {
            _configuration = configuration;
            _secretKey = _configuration["ApiSettings:SecretKey"];
        }

        public bool VerifySignature(string method, string rawUrl, string body, string clientId, string timeStamp, string signature)
        {
            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(timeStamp) || string.IsNullOrEmpty(signature))
            {
                return false;
            }

            if (DateTime.UtcNow.Subtract(DateTime.ParseExact(timeStamp, "yyyyMMddHHmmss", null)).TotalMinutes > 5)
            {
                return false;
            }

            var computedSignature = GenerateSignature(method, rawUrl, clientId, timeStamp, body);
            return computedSignature == signature;
        }


        public string GenerateSignature(string method, string rawUrl, string clientId, string timeStamp, string body)
        {
            string strToSign = $"{method}:{rawUrl}:{clientId}:{timeStamp}:{body}";
            using (var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(_secretKey)))
            {
                var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(strToSign));
                return Convert.ToBase64String(hash);
            }
        }
    }
}