using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Constants
{
    public static class Headers
    {
        // Заглавие за корелация на заявки/събития (по избор).
        // Traceability: един и същи CorrelationId минава през Gateway - Order - брокер - Portfolio.
        public const string CorrelationId = "X-Correlation-Id";
    }
}
