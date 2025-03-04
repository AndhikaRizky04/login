using System;
using System.Collections.Generic;

namespace login.Models
{
    public class ProductTracking
    {
        public string NO_JOB { get; set; }
        public string NAMA_JOB { get; set; }
        public string NAMA { get; set; }
        public string NO_PO { get; set; }
        public string PO_INT { get; set; }
        public decimal OPLAAG { get; set; }
        public decimal TERKIRIM { get; set; }

        // WIP fields
        public decimal WIP_POT { get; set; }
        public decimal WIP_BRT { get; set; }
        public decimal WIP_TMR { get; set; }
        public decimal WIP_CEL { get; set; }
        public decimal WIP_VAR { get; set; }
        public decimal WIP_LAM { get; set; }
        public decimal WIP_FOI { get; set; }
        public decimal WIP_PON { get; set; }
        public decimal WIP_LIP { get; set; }
        public decimal WIP_CBT { get; set; }
        public decimal WIP_PET { get; set; }
        public decimal WIP_FNA { get; set; }
        public decimal WIP_FND { get; set; }
        public decimal WIP_FNB { get; set; }

        // Calculate the progress percentage
        public decimal CalculateProgress()
        {
            if (OPLAAG <= 0) return 0;

            // WIP fields for calculation
            var wipFields = new[]
            {
                WIP_POT, WIP_BRT, WIP_TMR, WIP_CEL, WIP_VAR, WIP_LAM,
                WIP_FOI, WIP_PON, WIP_LIP, WIP_CBT, WIP_PET,
                WIP_FNA, WIP_FND, WIP_FNB
            };

            // Calculate WIP contribution (50% of total progress)
            decimal totalWip = 0;
            foreach (var field in wipFields)
            {
                totalWip += field;
            }

            // WIP contributes up to 50% of progress
            decimal wipProgress = Math.Min(50, (totalWip / OPLAAG) * 50);

            // Calculate delivery contribution (50% of total progress)
            decimal deliveryProgress = Math.Min(50, (TERKIRIM / OPLAAG) * 50);

            // Total progress is sum of WIP and delivery progress
            return Math.Min(100, wipProgress + deliveryProgress);
        }

        // Get color for progress visualization
        public static string GetProgressColor(decimal value)
        {
            if (value == 0) return "#6c757d";
            if (value < 25) return "#dc3545";
            if (value < 50) return "#ffc107";
            if (value < 75) return "#17a2b8";
            return "#28a745";
        }
    }

    // Tracking step description for UI
    public class TrackingStep
    {
        public string Key { get; set; }
        public string Label { get; set; }
    }
}