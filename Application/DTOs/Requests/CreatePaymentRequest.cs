using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Requests
{
    public class CreatePaymentRequest
    {
        public Guid PackageId { get; set; }
        public float Amount { get; set; }
    }
}
