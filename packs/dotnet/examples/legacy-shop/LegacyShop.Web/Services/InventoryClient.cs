using System;
using System.Runtime.Serialization;
using System.ServiceModel;

namespace LegacyShop.Web.Services
{
    // Client-side copy of the contract, the way "Add Service Reference" / svcutil generates it.
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

    /// <summary>
    /// TRAP T24: a static ChannelFactory built from the "InventoryEndpoint" client endpoint in Web.config.
    /// .NET 10 has no system.serviceModel config section - endpoints must be built in code.
    /// </summary>
    public static class InventoryClient
    {
        private static readonly ChannelFactory<IInventoryService> Factory =
            new ChannelFactory<IInventoryService>("InventoryEndpoint");

        public static StockInfo GetStock(int productId)
        {
            return Call(c => c.GetStock(productId));
        }

        public static ReservationResult Reserve(int productId, int quantity)
        {
            return Call(c => c.Reserve(productId, quantity));
        }

        private static T Call<T>(Func<IInventoryService, T> action)
        {
            var channel = Factory.CreateChannel();
            var communication = (ICommunicationObject)channel;
            try
            {
                var result = action(channel);
                communication.Close();
                return result;
            }
            catch
            {
                communication.Abort();
                throw;
            }
        }
    }
}
