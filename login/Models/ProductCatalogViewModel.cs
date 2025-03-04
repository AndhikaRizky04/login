using System;

namespace login.Models
{
    public class ProductCatalogViewModel
    {
        // Properties sesuai dengan mapping kolom di CUST_TRACKING
        public string NoJob { get; set; }     // NO_JOB
        public string NamaJob { get; set; }   // NAMA_JOB
        public string Nama { get; set; }      // NAMA (digunakan sebagai Customer)
        public string PoInt { get; set; }     // PO_INT
        public string NoPo { get; set; }      // NO_PO
        public decimal Oplaag { get; set; }   // Total produksi
        public decimal Terkirim { get; set; }
        public decimal WipPot { get; set; }
        public decimal WipBrt { get; set; }
        public decimal WipLam { get; set; }

        // Property untuk mengecek apakah produk adalah PT CERES
        public bool IsCeresProduct => Nama == "PT CERES";

        // Menghitung progress sesuai dengan fungsi calculateProgress
        public decimal CalculateProgress()
        {
            try
            {
                if (Oplaag <= 0) return 0;

                decimal totalWip = WipPot + WipBrt + WipLam;
                decimal wipProgress = Math.Min(50, (totalWip / Oplaag) * 50);
                decimal deliveryProgress = Math.Min(50, (Terkirim / Oplaag) * 50);

                return Math.Min(100, wipProgress + deliveryProgress);
            }
            catch
            {
                // Jika ada error dalam perhitungan, return 0
                return 0;
            }
        }

        // Get status progress
        public string GetProgressStatus()
        {
            decimal progress = CalculateProgress();

            if (progress < 25) return "Baru Dimulai";
            if (progress < 50) return "Sedang Proses";
            if (progress < 75) return "Hampir Selesai";
            return "Selesai";
        }

        // Mendapatkan class warna untuk progress bar
        public string GetProgressColorClass()
        {
            decimal progress = CalculateProgress();

            if (progress < 25) return "bg-danger";
            if (progress < 50) return "bg-warning";
            if (progress < 75) return "bg-info";
            return "bg-success";
        }
    }
}