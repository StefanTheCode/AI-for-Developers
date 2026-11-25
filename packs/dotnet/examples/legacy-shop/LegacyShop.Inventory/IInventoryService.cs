using System.Runtime.Serialization;
using System.ServiceModel;

namespace LegacyShop.Inventory
{
    // TRAP T23: WCF service contract with a FaultContract, hosted in IIS via .svc, basicHttpBinding from Web.config.
    [ServiceContract(Namespace = "http://legacyshop.local/inventory/2013")]
    public interface IInventoryService
    {
        [OperationContract]
        StockInfo GetStock(int productId);

        [OperationContract]
        [FaultContract(typeof(InventoryFault))]
        ReservationResult Reserve(int productId, int quantity);

        [OperationContract]
        void Release(string reservationId);
    }

    [DataContract(Namespace = "http://legacyshop.local/inventory/2013")]
    public class StockInfo
    {
        [DataMember(Order = 1)] public int ProductId { get; set; }
        [DataMember(Order = 2)] public int Available { get; set; }
        [DataMember(Order = 3)] public bool LowStock { get; set; }
    }

    [DataContract(Namespace = "http://legacyshop.local/inventory/2013")]
    public class ReservationResult
    {
        [DataMember(Order = 1)] public string ReservationId { get; set; }
        [DataMember(Order = 2)] public int Reserved { get; set; }
    }

    [DataContract(Namespace = "http://legacyshop.local/inventory/2013")]
    public class InventoryFault
    {
        [DataMember(Order = 1)] public string Code { get; set; }
        [DataMember(Order = 2)] public string Message { get; set; }
    }
}
