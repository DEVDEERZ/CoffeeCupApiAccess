﻿namespace devdeer.CoffeeCupApiAccess.Logic.Core
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;

    using Models.ResponseModels;
    using Models.TransportModels;

    using Newtonsoft.Json;

    /// <summary>
    /// Allows data retrieval from the CoffeeCup API.
    /// </summary>
    public class Repository
    {
        #region member vars

        private readonly HttpClient _client;

        #endregion

        #region constants

        private static DateTime? _barerTokenInvalidationTime;

        private static string _bearerToken;

        private static string _refreshToken;

        #endregion

        #region constructors and destructors

        public Repository(HttpClient client)
        {
            _client = client;
        }

        #endregion

        #region methods

        /// <summary>
        /// Retrieves the list of client information from the CoffeeCup API.
        /// </summary>
        /// <param name="requestData">The contextual data from the API which is needed by the repository.</param>
        /// <returns>The list of client information.</returns>
        public async Task<IEnumerable<ClientTransportModel>> GetClientsAsync(RequestDataModel requestData)
        {
            var apiResult = await GetCoffeeCupApiResultAsync<CoffeeCupClientsResponseModel>(requestData, "clients");
            return apiResult.Clients.OrderBy(p => p.Name);
        }

        /// <summary>
        /// Retrieves the list of project information from the CoffeeCup API.
        /// </summary>
        /// <param name="requestData">The contextual data from the API which is needed by the repository.</param>
        /// <returns>The list of project information.</returns>
        public async Task<IEnumerable<ProjectTransportModel>> GetProjectsAsync(RequestDataModel requestData)
        {
            var apiResult = await GetCoffeeCupApiResultAsync<CoffeeCupProjectsResponseModel>(requestData, "projects");
            return apiResult.Projects.OrderBy(p => p.Name);
        }

        /// <summary>
        /// Retrieves the list of time entries from the CoffeeCup API.
        /// </summary>
        /// <param name="requestData">The contextual data from the API which is needed by the repository.</param>
        /// <returns>The list of time entries.</returns>
        public async Task<IEnumerable<TimeEntryTransportModel>> GetTimeEntriesAsync(RequestDataModel requestData)
        {
            var apiResult = await GetCoffeeCupApiResultAsync<CoffeeCupTimeEntriesResponseModel>(requestData, "timeEntries");
            return apiResult.TimeEntries.OrderBy(p => p.Day).ThenBy(p => p.StartTime).ThenBy(p => p.UserId);
        }

        /// <summary>
        /// Retrieves the list of time entries from the CoffeeCup API.
        /// </summary>
        /// <param name="requestData">The contextual data from the API which is needed by the repository.</param>
        /// <param name="day">The day for which to retrieve the entries.</param>
        /// <returns>The list of time entries.</returns>
        public async Task<IEnumerable<TimeEntryTransportModel>> GetTimeEntriesByDayAsync(RequestDataModel requestData, DateTime day)
        {
            var relativeUrl = string.Format(CultureInfo.InvariantCulture, @"timeEntries?where={{""day"":{{"">="": ""{0}""}}}}", day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            var apiResult = await GetCoffeeCupApiResultAsync<CoffeeCupTimeEntriesResponseModel>(requestData, relativeUrl);
            return apiResult.TimeEntries.OrderBy(p => p.Day).ThenBy(p => p.StartTime).ThenBy(p => p.UserId);
        }

        /// <summary>
        /// Retrieves the list of time entries from the CoffeeCup API.
        /// </summary>
        /// <param name="requestData">The contextual data from the API which is needed by the repository.</param>
        /// <param name="from">The starting day for which to retrieve the entries.</param>
        /// <param name="to">The ending day for which to retrieve the entries.</param>
        /// <returns>The list of time entries.</returns>
        public async Task<IEnumerable<TimeEntryTransportModel>> GetTimeEntriesByDayRangeAsync(RequestDataModel requestData, DateTime from, DateTime to)
        {
            var relativeUrl = string.Format(
                CultureInfo.InvariantCulture,
                @"timeEntries?where={{""day"":{{"">="": ""{0}""}},{{""<="": ""{1}""}}}}",
                from.ToString("YYYY-MM-dd", CultureInfo.InvariantCulture),
                to.ToString("YYYY-MM-dd", CultureInfo.InvariantCulture));
            var apiResult = await GetCoffeeCupApiResultAsync<CoffeeCupTimeEntriesResponseModel>(requestData, relativeUrl);
            return apiResult.TimeEntries.OrderBy(p => p.Day).ThenBy(p => p.StartTime).ThenBy(p => p.UserId);
        }

        /// <summary>
        /// Retrieves the list of complete user information from the CoffeeCup API.
        /// </summary>
        /// <param name="onlyValid">If set to <c>true</c> only currently valid users will be loaded.</param>
        /// <param name="requestData">The contextual data from the API which is needed by the repository.</param>
        /// <returns>The list of user information.</returns>
        public async Task<IEnumerable<UserTransportModel>> GetUsersAsync(RequestDataModel requestData, bool onlyValid = true)
        {
            var apiResult = await GetCoffeeCupApiResultAsync<UsersResponseModel>(requestData, "users");
            var result = apiResult.Users.OrderBy(u => u.Lastname).ThenBy(u => u.Firstname);
            return onlyValid ? result.Where(r => r.ShowInPlanner) : result;
        }

        /// <summary>
        /// Retrieves the list of simplified user information from the CoffeeCup API.
        /// </summary>
        /// <param name="onlyValid">If set to <c>true</c> only currently valid users will be loaded.</param>
        /// <param name="requestData">The contextual data from the API which is needed by the repository.</param>
        /// <returns>The list of user information.</returns>
        public async Task<IEnumerable<SimpleUserTransportModel>> GetUsersSimpleAsync(RequestDataModel requestData, bool onlyValid = true)
        {
            var apiResult = await GetCoffeeCupApiResultAsync<UsersResponseModel>(requestData, "users");
            var result = apiResult.Users.Select(u => u.ToSimple()).OrderBy(u => u.Lastname).ThenBy(u => u.Firstname);
            return onlyValid ? result.Where(r => r.IsCurrentlyValid) : result;
        }

        /// <summary>
        /// Retrieves the currently valid or an updated bearer token from the CoffeeCup API.
        /// </summary>
        /// <param name="requestData">The contextual data from the API which is needed by the repository.</param>
        /// <returns>The a string of the pattern "Bearer {token}" if the operation succeeded.</returns>
        private async Task<string> GetBearerTokenAsync(RequestDataModel requestData)
        {
            var coffeeCupUser = requestData.CoffeeCupUsername;
            var coffeeCupPass = requestData.CoffeeCupPassword;
            if (string.IsNullOrEmpty(coffeeCupUser) || string.IsNullOrEmpty(coffeeCupPass))
            {
                throw new InvalidOperationException("Invalid CoffeeCup credentials.");
            }
            if (_bearerToken == null || _barerTokenInvalidationTime.HasValue && _barerTokenInvalidationTime.Value < DateTime.Now)
            {
                // Either we've never received a bearer token or it is invalidated now.                                
                var dict = new Dictionary<string, string>
                {
                    { "grant_type", "password" },
                    { "username", coffeeCupUser },
                    { "password", coffeeCupPass },
                    { "client_id", requestData.CoffeeCupApiClientId },
                    { "client_secret", requestData.CoffeeCupApiClientSecret }
                };
                var body = new FormUrlEncodedContent(dict);
                try
                {
                    var response = await _client.PostAsync("oauth2/token", body);
                    var result = await response.Content.ReadAsStringAsync();
                    var tokenResult = JsonConvert.DeserializeObject<AuthorizationResponseModel>(result);
                    // store token, refresh and invalidation timestamp locally
                    _bearerToken = tokenResult.AccessToken;
                    _refreshToken = tokenResult.RefreshToken;
                    _barerTokenInvalidationTime = DateTime.Now.AddSeconds(tokenResult.Expiration);
                }
                catch (Exception ex)
                {
                    Trace.TraceError(ex.Message);
                }
            }
            return $"Bearer {_bearerToken}";
        }

        /// <summary>
        /// Retrieves a result from the CoffeeCup API and tries to convert it to <typeparamref name="TResult" />.
        /// </summary>
        /// <typeparam name="TResult">The type the result is expected to be.</typeparam>
        /// <param name="relativeUrl">The relative URL to the CoffeeCup API without the configured base address.</param>
        /// <param name="requestData">The contextual data from the API which is needed by the repository.</param>
        /// <returns>The result.</returns>
        private async Task<TResult> GetCoffeeCupApiResultAsync<TResult>(RequestDataModel requestData, string relativeUrl)
        {
            if (_barerTokenInvalidationTime <= DateTime.Now)
            {
                // we have to refresh the token
                var ok = await RefreshBearerTokenAsync(requestData);
                if (!ok)
                {
                    throw new ApplicationException("Could not refresh authentication token.");
                }
            }
            var bearer = await GetBearerTokenAsync(requestData);
            if (string.IsNullOrEmpty(bearer))
            {
                throw new InvalidOperationException("Could not retrieve bearer token.");
            }
            _client.DefaultRequestHeaders.Add("Authorization", bearer);
            try
            {
                var response = await _client.GetAsync($"{requestData.CoffeeCupApiVersion}/{relativeUrl}");
                if (!response.IsSuccessStatusCode)
                {
                    // something went wrong
                    if (response.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        throw new ApplicationException("Could not read data from CoffeeCup-API due to authorization issues.");
                    }
                    throw new ApplicationException($"Could not read data from CoffeeCup-API due to response code {response.StatusCode}.");
                }
                var result = await response.Content.ReadAsStringAsync();
                var converted = JsonConvert.DeserializeObject<TResult>(result);
                return converted;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        /// <summary>
        /// Retrieves the currently valid or an updated bearer token from the CoffeeCup API.
        /// </summary>
        /// <param name="requestData">The contextual data from the API which is needed by the repository.</param>
        /// <returns>The a string of the pattern "Bearer {token}" if the operation succeeded.</returns>
        private async Task<bool> RefreshBearerTokenAsync(RequestDataModel requestData)
        {
            try
            {
                var dict = new Dictionary<string, string>
                {
                    { "grant_type", "refresh_token" },
                    { "refresh_token", _refreshToken },
                    { "client_id", requestData.CoffeeCupApiClientId },
                    { "client_secret", requestData.CoffeeCupApiClientSecret }
                };
                var body = new FormUrlEncodedContent(dict);
                var response = await _client.PostAsync("oauth2/token", body);
                var result = await response.Content.ReadAsStringAsync();
                var tokenResult = JsonConvert.DeserializeObject<AuthorizationResponseModel>(result);
                // store token, refresh and invalidation timestamp locally
                _bearerToken = tokenResult.AccessToken;
                _refreshToken = tokenResult.RefreshToken;
                _barerTokenInvalidationTime = DateTime.Now.AddSeconds(tokenResult.Expiration);
                return true;
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.Message);
                return false;
            }
        }

        #endregion
    }
}