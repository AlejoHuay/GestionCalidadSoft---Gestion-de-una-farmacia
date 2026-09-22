using ProyectoArqSoft.Domain.DTOs;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ProyectoArqSoft.Application.Helpers; // Importamos el helper de conversión

namespace ProyectoArqSoft.Application.Services
{
    public static class ComprobanteVentaPdfService
    {
        public static byte[] Generar(ComprobanteVentaPdfDto comprobante)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var logoPath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                "images",
                "vitalcare.png"
            );

            var logoBytes = File.Exists(logoPath) ? File.ReadAllBytes(logoPath) : null;

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);

                    page.Content().Column(column =>
                    {
                        column.Spacing(15);

                        column.Item().Row(row =>
                        {
                            row.ConstantItem(85).Height(85).Element(c =>
                            {
                                if (logoBytes != null)
                                    c.Image(logoBytes);
                                else
                                    c.Border(1).AlignCenter().AlignMiddle().Text("LOGO");
                            });

                            row.RelativeItem().PaddingLeft(10).Column(header =>
                            {
                                header.Spacing(3);

                                header.Item().Text("Farmacia VitalCare")
                                    .FontSize(18)
                                    .Bold()
                                    .FontColor(Colors.Green.Darken2);

                                header.Item().Text("COMPROBANTE DE VENTA")
                                    .FontSize(15)
                                    .Bold()
                                    .FontColor(Colors.Teal.Darken2);

                                header.Item().Text($"Fecha: {comprobante.Fecha:dd/MM/yyyy HH:mm:ss}")
                                    .FontSize(10);
                            });
                        });

                        column.Item().LineHorizontal(1);

                        column.Item().Border(1).Padding(10).Column(info =>
                        {
                            info.Spacing(4);
                            info.Item().Text($"CI / NIT: {comprobante.Nit}");
                            info.Item().Text($"Razón Social: {comprobante.RazonSocial}");
                            info.Item().Text($"Cajero: {comprobante.Cajero}");
                        });

                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(60);
                                columns.RelativeColumn();
                                columns.ConstantColumn(90);
                                columns.ConstantColumn(90);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Border(1).Background(Colors.Grey.Lighten3).Padding(5).AlignCenter().Text("Cant.").Bold();
                                header.Cell().Border(1).Background(Colors.Grey.Lighten3).Padding(5).AlignCenter().Text("Descripción").Bold();
                                header.Cell().Border(1).Background(Colors.Grey.Lighten3).Padding(5).AlignCenter().Text("P. Unit.").Bold();
                                header.Cell().Border(1).Background(Colors.Grey.Lighten3).Padding(5).AlignCenter().Text("Importe").Bold();
                            });

                            foreach (var item in comprobante.Detalles)
                            {
                                table.Cell().Border(1).Padding(5).AlignCenter().Text(item.Cantidad.ToString());
                                table.Cell().Border(1).Padding(5).Text(item.Descripcion);
                                table.Cell().Border(1).Padding(5).AlignRight().Text($"{item.PrecioUnitario:0.00}");
                                table.Cell().Border(1).Padding(5).AlignRight().Text($"{item.Importe:0.00}");
                            }
                        });

                        // Convertir el total a texto usando la clase helper
                        string totalLiteral = NumeroATextoConverter.ConvertirDecimalATexto(comprobante.Total);
                        int decimales = (int)Math.Round((comprobante.Total - Math.Truncate(comprobante.Total)) * 100, 0);

                        // Alineación a la izquierda del total
                        column.Item().AlignRight().Text($"TOTAL Bs.: {comprobante.Total:0.00}")
                            .Bold()
                            .FontSize(14);

                        // Aquí mostramos el total literal al lado izquierdo
                        column.Item().AlignLeft().Text($"Son {totalLiteral} {decimales:00}/100 Bolivianos")
                            .FontSize(14)
                            .FontColor(Colors.Black);

                        column.Item().PaddingTop(10).AlignCenter().Text("Gracias por su compra.")
                            .FontSize(10)
                            .FontColor(Colors.Grey.Darken1);
                    });
                });
            }).GeneratePdf();
        }
    }
}