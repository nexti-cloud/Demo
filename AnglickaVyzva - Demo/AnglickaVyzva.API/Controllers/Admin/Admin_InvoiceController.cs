using AnglickaVyzva.API.Data;
using AnglickaVyzva.API.Entities;
using AnglickaVyzva.API.Helpers;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnglickaVyzva.API.Controllers.Admin
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class Admin_InvoiceController : BaseController
    {
        public Admin_InvoiceController(IConfiguration config, IMapper mapper, DataContext dbContext, IWebHostEnvironment env) : base(config, mapper, dbContext, env)
        {
        }

        public class GetList_Model
        {

        }
        [HttpPost("getList")]
        public async Task<IActionResult> GetList(GetList_Model model)
        {
            AdminHelper.CheckAuthorization(await GetLoggedUser());

            var invoices = await InvoiceRepo.All.ToListAsync();
            invoices = invoices.OrderByDescending(x => x.Id).ToList();

            return Ok(new
            {
                invoices,
            });
        }

        public class SetExported_Model
        {
            public List<int> InvoicesIds { get; set; }
        }
        [HttpPost("setExported")]
        public async Task<IActionResult> SetExported(SetExported_Model model)
        {
            AdminHelper.CheckAuthorization(await GetLoggedUser());

            var invoices = await InvoiceRepo.All.Where(x=>model.InvoicesIds.Contains(x.Id)).ToListAsync();

            foreach(var invoice in invoices)
            {
                if(invoice.IsExported != true)
                {
                    invoice.IsExported = true;
                    invoice.ExportedDate = DateTime.Now;
                }
            }

            await SaveAll();

            return Ok();
        }

        public class SetNotExported_Model
        {
            public List<int> InvoicesIds { get; set; }
        }
        [HttpPost("setNotExported")]
        public async Task<IActionResult> SetNotExported(SetExported_Model model)
        {
            AdminHelper.CheckAuthorization(await GetLoggedUser());

            var invoices = await InvoiceRepo.All.Where(x => model.InvoicesIds.Contains(x.Id)).ToListAsync();

            foreach (var invoice in invoices)
            {
                if (invoice.IsExported == true)
                {
                    invoice.IsExported = false;
                    invoice.ExportedDate = null;
                }
            }

            await SaveAll();

            return Ok();
        }

        //public class Discard_Model
        //{
        //    public int InvoiceId { get; set; }
        //    public string DiscardReason { get; set; }
        //}
        //public async Task<IActionResult> Discard(Discard_Model model)
        //{
        //    AdminHelper.CheckAuthorization(await GetLoggedUser());

        //    if (string.IsNullOrWhiteSpace(model.DiscardReason))
        //    {
        //        return BadRequest("Chybí důvod zkartování");
        //    }

        //    var invoice = await InvoiceRepo.All.FirstAsync(x => x.Id == model.InvoiceId);

        //    if (invoice.IsDiscarted == true)
        //    {
        //        return BadRequest("Faktura je již zkartovaná");
        //    }

        //    invoice.IsDiscarted = true;
        //    invoice.DiscardReason = model.DiscardReason;

        //    await SaveAll();

        //    return Ok();
        //}

        [HttpPost("exportToPohoda")]
        public async Task<IActionResult> ExportToPohoda()
        {
            AdminHelper.CheckAuthorization(await GetLoggedUser());

            // --- POZOR PRIDAT PODMINKU NA TO, JESTLI UZ JSOU VYEXPORTOVANE

            var unexportedInvoices = await InvoiceRepo.All.Where(x=>x.Id >= 72).ToListAsync();

            var xml = generateXmlHeader(OrderController.ProviderICO);


            foreach(var invoice in unexportedInvoices)
            {
                xml += generateXmlItemForInvoice(invoice);
            }

            xml += generateXmlFooter();

            return Ok(new
            {
                xml
            });

            //return File(Encoding.UTF8.GetBytes(xml), "application/octet-stream");
        }

        private string generateXmlHeader(string providerIco)
        {
            string created = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");

            return @$"<dat:dataPack
                    xmlns:dat=""http://www.stormware.cz/schema/version_2/data.xsd"" 
                    xmlns:inv=""http://www.stormware.cz/schema/version_2/invoice.xsd"" 
                    xmlns:typ=""http://www.stormware.cz/schema/version_2/type.xsd""
                    id=""import-{created}"" ico=""{providerIco}"" 
                    application=""zpracovani-faktur-app"" 
                    version=""2.0"" 
                    note=""Import faktur"">
            ";
        }

        private string generateXmlItemForInvoice(Invoice invoice)
        {
            var createdDate = invoice.CreatedDate.ToString("yyyy-MM-dd");
            var dueDate = invoice.DueDate.ToString("yyyy-MM-dd");

            return @$"
<dat:dataPackItem id=""{invoice.Number}"" version=""2.0"">

        <inv:invoice version=""2.0"">

            <inv:invoiceHeader>

                <inv:invoiceType>issuedInvoice</inv:invoiceType>

                <inv:number>

                    <typ:numberRequested checkDuplicity=""true"">{invoice.Number}</typ:numberRequested>

                </inv:number>

                <inv:classificationVAT>

                    {generateClasificationVatLine(invoice)}

                </inv:classificationVAT>

                <inv:symVar>{invoice.VS}</inv:symVar>

                <inv:date>{createdDate}</inv:date>

                <inv:dateTax>{dueDate}</inv:dateTax>

                <inv:dateDue>{dueDate}</inv:dateDue>

                <inv:text>{invoice.Name}</inv:text>

                <inv:partnerIdentity>

                    <typ:address>

                        <typ:company>{invoice.ClientName}</typ:company>

                             <typ:city>{invoice.ClientCity}</typ:city>

                             <typ:street>{invoice.ClientStreet}</typ:street>

                             <typ:zip>{invoice.ClientPostalCode}</typ:zip>

                             {this.genereateClientIcoLine(invoice)}

                             {this.generateClientDicLine(invoice)}

                             <typ:email>{invoice.ClientEmail}</typ:email>

                    </typ:address>

                </inv:partnerIdentity>

                <inv:paymentType>

                    <typ:paymentType>draft</typ:paymentType>

                </inv:paymentType>

                <inv:accounting>

                    <typ:ids>2Pslužb</typ:ids>

                </inv:accounting>

                <inv:account>

                    <typ:ids>RB</typ:ids>

                </inv:account>

            </inv:invoiceHeader>

            <inv:invoiceSummary>

                <inv:roundingDocument>math2one</inv:roundingDocument>

                <inv:homeCurrency>

                    {generatePriceLine(invoice)}

                </inv:homeCurrency>

            </inv:invoiceSummary>

        </inv:invoice>

    </dat:dataPackItem>
";
        }

        private string genereateClientIcoLine(Invoice invoice)
        {
            if (string.IsNullOrWhiteSpace(invoice.ClientICO))
            {
                return "";
            }
            else
            {
                return $"<typ:ico>{invoice.ClientICO}</typ:ico>";
            }
        }

        private string generateClientDicLine(Invoice invoice)
        {
            if (string.IsNullOrWhiteSpace(invoice.ClientDIC))
            {
                return "";
            }
            else
            {
                return $"<typ:dic>{invoice.ClientDIC}</typ:dic>";
            }
        }

        private string generatePriceLine(Invoice invoice)
        {
            if(invoice.ProviderVatPayer)
            {
                return $"<typ:priceHighSum>{invoice.PriceWithVat.ToString().Replace(",",".")}</typ:priceHighSum>";
            }
            else
            {
                return $"<typ:priceNone>{invoice.PriceWithVat.ToString().Replace(",", ".")}</typ:priceNone>";
            }
        }

        /// <summary>
        /// Osoba registrovana k dani (FB reklama)
        /// </summary>
        /// <param name="invoice"></param>
        /// <returns></returns>
        private string generateClasificationVatLine(Invoice invoice)
        {
            if (invoice.ProviderVatPayer)
            {
                return $"<typ:classificationVATType>inland</typ:classificationVATType>";
            }
            else
            {
                return $"<typ:classificationVATType>nonSubsume</typ:classificationVATType>";
            }
        }

        private string generateXmlFooter()
        {
            return "</dat:dataPack>";
        }
    }
}
