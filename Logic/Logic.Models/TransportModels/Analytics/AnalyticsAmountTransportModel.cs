﻿namespace devdeer.CoffeeCupApiAccess.Logic.Models.TransportModels.Analytics
{
    using System;
    using System.Linq;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Contains information about amounts in analytics data.
    /// </summary>
    public class AnalyticsAmountTransportModel
    {
        #region properties

        [JsonPropertyName("billed")]
        public int Billed { get; set; }

        [JsonPropertyName("budget")]
        public int Budget { get; set; }

        [JsonPropertyName("outOfBudget")]
        public float OutOfBudget { get; set; }

        [JsonPropertyName("spent")]
        public float Spent { get; set; }

        #endregion
    }
}