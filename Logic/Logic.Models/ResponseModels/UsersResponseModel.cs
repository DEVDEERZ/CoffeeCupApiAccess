﻿namespace devdeer.CoffeeCupApiAccess.Logic.Models.ResponseModels
{
    using System;
    using System.Linq;

    using Newtonsoft.Json;

    using TransportModels;

    /// <summary>
    /// Represents the response of the CoffeeCup API when the users endpoint is called.
    /// </summary>
    public class UsersResponseModel : BaseResponseModel
    {
        #region properties

        /// <summary>
        /// The list of user information.
        /// </summary>
        [JsonProperty("users")]
        public UserTransportModel[] Users { get; set; }

        #endregion
    }
}