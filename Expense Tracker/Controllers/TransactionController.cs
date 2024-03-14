﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Expense_Tracker.Models;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.IO;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.IO.Image;

namespace Expense_Tracker.Controllers
{
    public class TransactionController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TransactionController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Transaction
        //public async Task<IActionResult> Index()
        //{
        //    var applicationDbContext = _context.Transactions.Include(t => t.Category);
        //    return View(await applicationDbContext.ToListAsync());
        //}

        public async Task<IActionResult> Index(string? categoria, DateTime? date)
        {
            var categories = await _context.Categories.ToListAsync();
            ViewBag.Categories = new SelectList(categories, "Title", "Title");

            var applicationDbContext = _context.Transactions.Include(t => t.Category);
            if(categoria != null)
            {
                var applicationDbContextCategoria = _context.Transactions.Include(t => t.Category).Where(t => t.Category.Title == categoria);
                return View(await applicationDbContextCategoria.ToListAsync());
            }
            if(date != null)
            {
                var applicationDbContextDate = _context.Transactions.Include(t => t.Category).Where(t => t.Date == date);
                return View(await applicationDbContextDate.ToListAsync());
            }
            else
            {
                return View(await applicationDbContext.ToListAsync());
            }
        }

        public IActionResult AddOrEdit(int id = 0)
        {
            PopulateCategories();
            if (id == 0)
                return View(new Transaction());
            else
                return View(_context.Transactions.Find(id));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddOrEdit([Bind("TransactionId,CategoryId,Amount,Note,Date")] Transaction transaction)
        {
            if (ModelState.IsValid)
            {
                if (transaction.TransactionId == 0)
                    _context.Add(transaction);
                else
                    _context.Update(transaction);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            PopulateCategories();
            return View(transaction);
        }
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Transactions == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Transactions'  is null.");
            }
            var transaction = await _context.Transactions.FindAsync(id);
            if (transaction != null)
            {
                _context.Transactions.Remove(transaction);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }


        [NonAction]
        public void PopulateCategories()
        {
            var CategoryCollection = _context.Categories.ToList();
            Category DefaultCategory = new Category() { CategoryId = 0, Title = "Choose a Category" };
            CategoryCollection.Insert(0, DefaultCategory);
            ViewBag.Categories = CategoryCollection;
        }

        public async Task<IActionResult> GenerarPDF()
        {
            var transactions = await _context.Transactions.Include(t => t.Category).ToListAsync();


            // Crear un nuevo documento PDF en una ubicación temporal
            string rutaTempPDF = Path.GetTempFileName() + ".pdf";

            using (PdfDocument pdfDocument = new PdfDocument(new PdfWriter(rutaTempPDF)))
            {
                using (Document document = new Document(pdfDocument))
                {
                    document.Add(new Paragraph("Resumen de Transacciones"));

                    // Crear la tabla para mostrar la lista de objetos
                    iText.Layout.Element.Table table = new iText.Layout.Element.Table(5);
                    // 5 columnas
                    table.SetWidth(UnitValue.CreatePercentValue(100)); // Ancho de la tabla al 100% del documento

                    // Añadir las celdas de encabezado a la tabla
                    table.AddHeaderCell("ID Transacción");
                    table.AddHeaderCell("Categoría");
                    table.AddHeaderCell("Monto");
                    table.AddHeaderCell("Nota");
                    table.AddHeaderCell("Fecha");


                    foreach (Transaction transaction in transactions)
                    {
                        table.AddCell(transaction.TransactionId.ToString());
                        table.AddCell(transaction.Category.Title);
                        table.AddCell(transaction.FormattedAmount.ToString());
                        if (transaction.Note == null)
                            table.AddCell("Sin Nota");
                        else
                            table.AddCell(transaction.Note);
                        table.AddCell(transaction.Date.ToString("dd/MM/yyyy"));

                    }

                    // Añadir la tabla al documento
                    document.Add(table);
                }
            }

            // Leer el archivo PDF como un arreglo de bytes
            byte[] fileBytes = System.IO.File.ReadAllBytes(rutaTempPDF);

            // Eliminar el archivo temporal
            System.IO.File.Delete(rutaTempPDF);

            // Descargar el archivo PDF
            return new FileStreamResult(new MemoryStream(fileBytes), "application/pdf")
            {
                FileDownloadName = "ReporteProductos.pdf"
            };
        }

    }
}
