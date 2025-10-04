using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradingMicroservices.Common.Constants
{
    public static class Messaging
    {
        public static class Exchanges
        {
            // Message entity names
            public const string PriceUpdated = "price-updated";
            public const string OrderExecuted = "order-executed";
        }

        public static class Headers
        {
            // HTTP/message correlation header
            public const string CorrelationId = "X-Correlation-Id";
            public const string UserRef = "X-User-Ref";
        }

        public static class LogProperties
        {
            public const string CorrelationId = "CorrelationId";
            public const string UserRef = "UserRef";
        }
    }
}
