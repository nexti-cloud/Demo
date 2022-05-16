using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AnglickaVyzva.API.Entities
{
    public class Invoice : BaseEntity
    {
        public Guid Guid { get; set; }

        public int OrderId { get; set; }

        public long Number { get; set; }
        public string VS { get; set; }
        public string Name { get; set; }
        public decimal PriceWithVat { get; set; }
        public decimal PriceWithoutVat { get; set; }
        public decimal VatRate { get; set; }
        public decimal Vat { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime PostingDate { get; set; }

        public string ClientName { get; set; }
        public string ClientStreet { get; set; }
        public string ClientPostalCode { get; set; }
        public string ClientCity { get; set; }
        public string ClientPhone { get; set; }
        public string ClientEmail { get; set; }
        public string ClientICO { get; set; }
        public string ClientDIC { get; set; }

        public bool ProviderVatPayer { get; set; }
        public string ProviderName { get; set; }
        public string ProviderStreet { get; set; }
        public string ProviderPostalCode { get; set; }
        public string ProviderCity { get; set; }
        public string ProviderICO { get; set; }
        public string ProviderDIC { get; set; }
        public string ProviderIBAN { get; set; }
        public string ProviderPhone { get; set; }
        public string ProviderEmail { get; set; }
        public string ProviderBank { get; set; }
        public string ProviderBankCode { get; set; }
        public string ProviderAccountNumber { get; set; }
        public string ProviderFileReference { get; set; }

        //public bool? IsDiscarted { get; set; }
        //public string DiscardReason { get; set; }

        public bool? IsExported { get; set; }
        public DateTime? ExportedDate { get; set; }
    }
}
