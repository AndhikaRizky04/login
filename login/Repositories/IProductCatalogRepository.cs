using System.Collections.Generic;
using System.Threading.Tasks;
using login.Models;

namespace login.Repositories
{
    public interface IProductCatalogRepository
    {
        /// <summary>
        /// Mendapatkan semua produk untuk pengguna admin
        /// </summary>
        /// <param name="searchTerm">Kata kunci pencarian opsional</param>
        /// <returns>Kumpulan produk yang cocok dengan kriteria pencarian</returns>
        Task<IEnumerable<ProductCatalogViewModel>> GetProductsForAdminAsync(string searchTerm = null);

        /// <summary>
        /// Mendapatkan produk untuk pengguna biasa, termasuk produk PT CERES khusus
        /// </summary>
        /// <param name="username">Nama pengguna</param>
        /// <param name="searchTerm">Kata kunci pencarian opsional</param>
        /// <returns>Kumpulan produk milik pengguna dan produk khusus PT CERES</returns>
        Task<IEnumerable<ProductCatalogViewModel>> GetProductsForUserAsync(string username, string searchTerm = null);
    }
}