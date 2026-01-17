using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class RefreshToken
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Token { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }   // thời điểm cấp token
        public DateTime InitialLoginAt { get; set; }  // thời điểm login ban đầu
        public bool IsRevoked { get; set; }

        public Guid AccountId { get; set; }
        public Account? Account { get; set; }
    }

}
