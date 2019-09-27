using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Owin;
using Microsoft.Owin.Extensions;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OpenIdConnect;
using Owin;

[assembly: OwinStartup(typeof(WebApplication3.Startup))]

namespace WebApplication3
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AuthenticationType = "Cookies",
                CookieHttpOnly = true
            });

            app.UseOpenIdConnectAuthentication(new OpenIdConnectAuthenticationOptions
            {
                AuthenticationType = "oidc",
                SignInAsAuthenticationType = "Cookies",
                Authority = "http://localhost:5000/",
                RedirectUri = "http://localhost:5000/signin-oidc",
                ClientId = "mvc",
                ClientSecret = "secret",
                ResponseType = "code id_token",
                RequireHttpsMetadata = false,
                Scope = "api1 offine_access",
                UseTokenLifetime = false
            });
            app.UseStageMarker(PipelineStage.Authenticate);
            ConfigureAuth(app);
        }
    }
}
