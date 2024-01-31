namespace Kenso.Data.Repository
{
    public class DatabaseOptions
    {
        public string? ConnectionString { get; set; }
        public bool UpdateFromSource { get; set; } = true;
    }
}
