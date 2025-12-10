using UltraDataBurningROM.Server.Controllers;

namespace UltraDataBurningROM.Server.Services
{
    public interface IStorageService
    {
        void Initialize();
        string Upload(string filepath);
        string PurchaseStorage(string cid, DurabilityOption option); // todo move options to this service.
    }

    public class StorageService : IStorageService
    {
        public void Initialize()
        {
            // ping nodes, all OK?
        }

        public string Upload(string filepath)
        {
            return "todo_uploadcid_" + filepath;
        }

        public string PurchaseStorage(string cid, DurabilityOption option)
        {
            return "todo_purchasecid_" + cid + option.Name;
        }
    }
}
