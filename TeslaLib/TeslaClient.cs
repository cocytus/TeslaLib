using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using TeslaLib.Models;
using System;

namespace TeslaLib
{
    public class TeslaClient
    {
        public string Email { get; }
        public string TeslaClientID { get; }
        public string TeslaClientSecret { get; }
        public string AccessToken { get; private set; }
        public LoginToken LoginToken { get; private set; }
        public RestClient Client { get; set; }

        public const string BaseUrl = "https://owner-api.teslamotors.com/api/1";
        public const string Version = "1.1.0";
        public const string UserAgent = "Mozilla/5.0 (Linux; Android 9.0.0; VS985 4G Build/LRX21Y; wv) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/58.0.3029.83 Mobile Safari/537.36";
        public const string TeslaUserAgent = "TeslaApp/3.4.4-350/fad4a582e/android/9.0.0";

        public TeslaClient(string email, string teslaClientId, string teslaClientSecret)
        {
            Email = email;
            TeslaClientID = teslaClientId;
            TeslaClientSecret = teslaClientSecret;

            Client = new RestClient(BaseUrl);
            Client.Authenticator = new TeslaAuthenticator();
        }

        public void LoginUsingCache(string password)
        {
            var token = LoginTokenCache.GetToken(Email);
            if (token != null)
            {
                SetToken(token);
            }
            else
            {
                token = GetLoginToken(password);
                SetToken(token);
                LoginTokenCache.AddToken(Email, token);
            }
        }

        public void Login(string password) => SetToken(GetLoginToken(password));

        private LoginToken GetLoginToken(string password)
        {
            var loginClient = new RestClient("https://owner-api.teslamotors.com/oauth");
            var request = new RestRequest("token")
            {
                RequestFormat = DataFormat.Json
            };

            request.AddBody(new
            {
                grant_type = "password",
                client_id = TeslaClientID,
                client_secret = TeslaClientSecret,
                email = Email,
                password
            });

            var response = loginClient.Post<LoginToken>(request);
            var token = response.Data;
            return token;
        }

        internal void SetToken(LoginToken token)
        {
            var auth = Client.Authenticator as TeslaAuthenticator;
            LoginToken = token;
            auth.Token = token.AccessToken;
            AccessToken = token.AccessToken;
        }

        public bool HasValidToken
        {
            get
            {
                return LoginToken != null && LoginToken.ExpiresAt > DateTime.UtcNow;
            }
        }

        public void ClearLoginTokenCache() => LoginTokenCache.ClearCache();

        public List<TeslaVehicle> LoadVehicles()
        {
            var request = new RestRequest("vehicles");
            var response = Client.Get(request);

            var json = JObject.Parse(response.Content)["response"];
            var vehicles = JsonConvert.DeserializeObject<List<TeslaVehicle>>(json.ToString());

            foreach (var vehicle in vehicles)
            {
                vehicle.Client = Client;
            }

            return vehicles;
        }
    }
}
