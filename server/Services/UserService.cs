namespace UltraDataBurningROM.Server.Services
{
    public interface IUserService
    {
        bool IsValid(string username);
        DbUser GetUser(string username);
    }

    public class UserService : IUserService
    {
        private readonly string[] knownUsers = Array.Empty<string>();
        private readonly IDatabaseService databaseService;
        private readonly IMountService mountService;

        public UserService(IDatabaseService databaseService, IMountService mountService)
        {
            var envVar = Environment.GetEnvironmentVariable("BROM_USERNAMES");
            if (!string.IsNullOrEmpty(envVar))
            {
                knownUsers = envVar.Split(";", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            }

            this.databaseService = databaseService;
            this.mountService = mountService;
        }

        public bool IsValid(string username)
        {
            return knownUsers.Contains(username);
        }

        public DbUser GetUser(string username)
        {
            var user = databaseService.Get<DbUser>(username);
            if (user == null)
            {
                user = new DbUser
                {
                    Id = username,
                    Username = username,
                    BucketMountId = mountService.CreateNewBucketMount().Id,
                    BucketBurnState = 0,
                };
                databaseService.Save(user);
            }
            return user;
        }
    }
}
