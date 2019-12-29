using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PTZ.PhotoOrder.Models
{
    public class EncomendaRequest
    {
        public string Nome { get; set; }
        public string Email { get; set; }
        public string NumeroTelefone { get; set; }
        public string PaymentRadio { get; set; }
        public decimal Total { get; set; }
        public string[] Fotos { get; set; }
    }
}
