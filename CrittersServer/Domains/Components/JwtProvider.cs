﻿using CritterServer.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CritterServer.Domains.Components
{
    public class JwtProvider : IJwtProvider
    {
        private string SecretKey;
        private SymmetricSecurityKey SigningKey;
        private TokenValidationParameters tokenValidationOptions;

        public JwtProvider(string secretKey, TokenValidationParameters tokenValidationOptions)
        {
            this.SecretKey = secretKey;
            this.SigningKey = new SymmetricSecurityKey(Convert.FromBase64String(SecretKey));
            this.tokenValidationOptions = tokenValidationOptions;
        }

        public SymmetricSecurityKey GetSigningKey()
        {
            return SigningKey;
        }

        public string GenerateToken(User user)
        {
            SecurityTokenDescriptor std = new SecurityTokenDescriptor()
            {
                Subject = new ClaimsIdentity(new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.UserName)
                }),
                Expires = DateTime.UtcNow.AddDays(14),
                SigningCredentials = new SigningCredentials(SigningKey, SecurityAlgorithms.HmacSha384Signature),
                IssuedAt = DateTime.UtcNow,
                Issuer = "critters!",
            };
            JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();
            SecurityToken jwt = tokenHandler.CreateToken(std);
            return tokenHandler.WriteToken(jwt);
        }

        public bool ValidateToken(string jwtString)
        {
            if (!string.IsNullOrEmpty(jwtString))
            {
                if(CrackJwt(jwtString) != null)
                {
                    return true;
                }
            }
            return false;
        }

        public ClaimsPrincipal CrackJwt(string jwtString)
        {
            SecurityToken jwt;
            JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();

            try
            {
                return tokenHandler.ValidateToken(jwtString, tokenValidationOptions, out jwt);
            }
            catch (Exception ex)
            {
                return null;
                //TODO log this out, but don't error to user
            }
        }
    }

    public interface IJwtProvider
    {
        string GenerateToken(User user);
        bool ValidateToken(string jwtString);
        ClaimsPrincipal CrackJwt(string jwtString);
        SymmetricSecurityKey GetSigningKey();
    }

    public static class JwtExtensions
    {
        public static IServiceCollection AddJwt(this IServiceCollection services, IConfiguration config)
        {
            services.AddSingleton<Microsoft.IdentityModel.Tokens.TokenValidationParameters>(sp => {
                return new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                {
                    IssuerSigningKey = new SymmetricSecurityKey(Convert.FromBase64String(config.GetValue<string>("JwtSigningKey"))),
                    ValidIssuer = "critters!",
                    ValidateAudience = false,
                    ValidateActor = false,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    RequireExpirationTime = true
                };
            });
            services.AddSingleton<IJwtProvider, JwtProvider>(sp => 
                new JwtProvider(config.GetValue<string>("JwtSigningKey"), services.BuildServiceProvider().GetService<TokenValidationParameters>()));

            services.AddAuthentication(Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(c => c.TokenValidationParameters = services.BuildServiceProvider().GetService<TokenValidationParameters>());
            return services;
        }
    }
}
