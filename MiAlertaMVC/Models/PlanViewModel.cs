using System;
using System.Collections.Generic;
using System.Numerics;
using Newtonsoft.Json;
namespace MiAlertaMVC.Models
{
    public class PlanViewModel
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("userId")]
        public int UserId { get; set; }

        [JsonProperty("planId")]
        public string PlanId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("currency")]
        public string Currency { get; set; }

        [JsonProperty("amount")]
        public string Amount { get; set; }

        [JsonProperty("interval")]
        public int Interval { get; set; }

        [JsonProperty("interval_count")]
        public int IntervalCount { get; set; }

        [JsonProperty("created")]
        public DateTime Created { get; set; }

        [JsonProperty("periods_number")]
        public int PeriodsNumber { get; set; }

        [JsonProperty("trial_period_days")]
        public int TrialPeriodDays { get; set; }

        [JsonProperty("days_until_due")]
        public int DaysUntilDue { get; set; }

        [JsonProperty("urlCallback")]
        public string UrlCallback { get; set; }

        [JsonProperty("charges_retries_number")]
        public int ChargesRetriesNumber { get; set; }

        [JsonProperty("currency_convert_option")]
        public int CurrencyConvertOption { get; set; }

        [JsonProperty("status")]
        public int Status { get; set; }

        [JsonProperty("public")]
        public int Public { get; set; }

        [JsonProperty("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonProperty("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }
    public class PlanResponse
    {
        [JsonProperty("total")]
        public int Total { get; set; }

        [JsonProperty("hasMore")]
        public bool HasMore { get; set; }

        [JsonProperty("data")]
        public List<PlanViewModel> Data { get; set; }
    }
}
