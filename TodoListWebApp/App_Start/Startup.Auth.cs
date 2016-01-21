﻿using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Authentication;
using Microsoft.AspNet.Authentication.Cookies;
using Microsoft.AspNet.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Threading.Tasks;
using TodoListWebApp.Utils;
using Microsoft.IdentityModel.Protocols;

namespace TodoListWebApp
{
    public partial class Startup
    {
        public static string Authority = String.Empty;
        public static string ClientId = String.Empty;
        public static string AppKey = String.Empty;
        public static string TodoListResourceId = String.Empty;
        public static string TodoListBaseAddress = String.Empty;
        public static string GraphResourceId = String.Empty;

        public void ConfigureAuth(IApplicationBuilder app)
        {
            // Populate AzureAd Configuration Values
            Authority = String.Format(Configuration.Get("AzureAd:AadInstance"), Configuration.Get("AzureAd:Tenant"));
            ClientId = Configuration.Get("AzureAd:ClientId");
            AppKey = Configuration.Get("AzureAd:AppKey");
            TodoListResourceId = Configuration.Get("AzureAd:TodoListResourceId");
            TodoListBaseAddress = Configuration.Get("AzureAd:TodoListBaseAddress");
            GraphResourceId = Configuration.Get("AzureAd:GraphResourceId");

            // Configure the Session Middleware, Used for Storing Tokens
            app.UseSession();

            // Configure OpenId Connect Authentication Middleware
            app.UseCookieAuthentication(options => { });
            app.UseOpenIdConnectAuthentication(options =>
            {
                options.ClientId = Configuration.Get("AzureAd:ClientId");
                options.Authority = Authority;
                options.PostLogoutRedirectUri = Configuration.Get("AzureAd:PostLogoutRedirectUri");
                options.Notifications = new OpenIdConnectAuthenticationNotifications
                {
                    AuthorizationCodeReceived = OnAuthorizationCodeReceived,
                    AuthenticationFailed = OnAuthenticationFailed
                };
            });
        }

        private Task OnAuthenticationFailed(AuthenticationFailedNotification<OpenIdConnectMessage, OpenIdConnectAuthenticationOptions> notification)
        {
            notification.HandleResponse();
            notification.Response.Redirect("/Home/Error?message=" + notification.Exception.Message);
            return Task.FromResult(0);
        }

        public async Task OnAuthorizationCodeReceived(AuthorizationCodeReceivedNotification notification)
        {
            // Acquire a Token for the Graph API and cache it.  In the TodoListController, we'll use the cache to acquire a token to the Todo List API
            string userObjectId = notification.AuthenticationTicket.Principal.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier").Value;
            ClientCredential clientCred = new ClientCredential(ClientId, AppKey);
            AuthenticationContext authContext = new AuthenticationContext(Authority, new NaiveSessionCache(userObjectId, notification.HttpContext.Session));
            AuthenticationResult authResult = await authContext.AcquireTokenByAuthorizationCodeAsync(
                notification.Code, new Uri(notification.RedirectUri), clientCred, Startup.GraphResourceId);
        }
    }
}