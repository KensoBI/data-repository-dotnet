using Kenso.Domain;

namespace Kenso.Data.Repository
{
    public interface IPartRepository
    {
        Task<long> Upsert(Part part, long? modelId, string source);
        Task AddModelPartMapping(long partId, long modelId);
    }
}
