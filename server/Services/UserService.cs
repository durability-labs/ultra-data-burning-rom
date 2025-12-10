namespace UltraDataBurningROM.Server.Services
{
    public interface IUserService
    {
        bool IsValid(string username);
        DbUser GetUser(string username);
        void SaveUser(DbUser user);
    }

    public class UserService : IUserService
    {
        private readonly string[] knownUsers = EnvConfig.KnownUsers;
        private readonly IDatabaseService databaseService;
        private readonly IMountService mountService;

        public UserService(IDatabaseService databaseService, IMountService mountService)
        {
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
                    BucketBurnState = BucketBurnState.Open,
                };
                databaseService.Save(user);
            }
            return user;
        }

        public void SaveUser(DbUser user)
        {
            databaseService.Save(user);
        }
    }
}
