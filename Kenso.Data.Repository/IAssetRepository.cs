using Kenso.Domain;

namespace Kenso.Data.Repository
{
    public interface IAssetRepository
    {
        Task<long> Upsert(Asset asset, string source);
        Task<long> Insert(Asset asset, string source);
        Task<long> Update(Asset asset, string source);
    }
}
