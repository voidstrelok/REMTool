using RemTool.Shared;
namespace RemTools
{
    public partial class Utils
    {
        private readonly RemToolDataContext Bdd;

        public Utils(RemToolDataContext dbContext)
        {
            Bdd = dbContext;
        }

        private const string RegexHoja = @"[a-zA-Z]*[0-9]*\[[^\]]*.";
        private const string RegexCelda = @"[A-Z]+[0-9]+";
    }
}