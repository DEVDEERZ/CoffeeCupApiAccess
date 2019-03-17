﻿namespace devdeer.CoffeeCupApiAccess.Logic.Core.ResponseModels
{
    using System;
    using System.Linq;

    using Newtonsoft.Json;

    using TransportModels;

    /// <summary>
    /// Represents the response of the CoffeeCup API when the clients endpoint is called.
    /// </summary>
    public class CoffeeCupClientsResponseModel
    {
        #region properties

        /// <summary>
        /// The list of client information.
        /// </summary>
        [JsonProperty("clients")]
        public CoffeeCupClientTransportModel[] Clients { get; set; }

        #endregion
    }
}