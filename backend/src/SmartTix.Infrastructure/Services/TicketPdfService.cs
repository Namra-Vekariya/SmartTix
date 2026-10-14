using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SmartTix.Application.Interfaces.Services;
using SmartTix.Domain.Entities;

namespace SmartTix.Infrastructure.Services;

public sealed class TicketPdfService : ITicketPdfService
{
    public byte[] GenerateTicketPdf(Booking booking)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A5);
                page.Margin(24);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header()
                    .Background("#6c63ff")
                    .Padding(14)
                    .Column(column =>
                    {
                        column.Item().Text("SmartTix").FontSize(22).FontColor("#ffffff").Bold();
                        column.Item().Text("E-Ticket").FontSize(9).FontColor("#e0e0ff");
                        column.Item()
                            .PaddingTop(8)
                            .Text(booking.BookingReference)
                            .FontSize(15)
                            .FontColor("#ffffff")
                            .Bold();
                    });

                page.Content().PaddingVertical(16).Column(column =>
                {
                    column.Item().Text(booking.Event.Name).FontSize(16).Bold();
                    column.Item()
                        .PaddingTop(2)
                        .Text(booking.Event.ArtistName)
                        .FontSize(11)
                        .FontColor("#555555");

                    column.Item()
                        .PaddingTop(10)
                        .Text($"Venue: {booking.Event.VenueName}, {booking.Event.VenueCity}");
                    column.Item()
                        .PaddingTop(2)
                        .Text($"Date: {booking.Event.EventDate:dddd, MMMM dd yyyy 'at' hh:mm tt} UTC");

                    if (booking.Event.DoorsOpenTime is not null)
                    {
                        column.Item()
                            .PaddingTop(2)
                            .Text($"Doors open: {booking.Event.DoorsOpenTime:hh:mm tt} UTC");
                    }

                    column.Item().PaddingTop(14).Text("Seats").FontSize(12).Bold();

                    column.Item().PaddingTop(4).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(2);
                        });

                        table.Header(header =>
                        {
                            HeaderCell(header.Cell(), "Seat");
                            HeaderCell(header.Cell(), "Row");
                            HeaderCell(header.Cell(), "No.");
                            HeaderCell(header.Cell(), "Category");
                            HeaderCell(header.Cell(), "Price");
                        });

                        foreach (var bookingSeat in booking.BookingSeats)
                        {
                            var seat = bookingSeat.Seat;

                            table.Cell().BorderBottom(1).BorderColor("#eeeeee").Padding(6)
                                .Text(seat.SeatCode);
                            table.Cell().BorderBottom(1).BorderColor("#eeeeee").Padding(6)
                                .Text(seat.RowLabel);
                            table.Cell().BorderBottom(1).BorderColor("#eeeeee").Padding(6)
                                .Text(seat.SeatNumber.ToString());
                            table.Cell().BorderBottom(1).BorderColor("#eeeeee").Padding(6)
                                .Text(seat.SeatCategory.Name);
                            table.Cell().BorderBottom(1).BorderColor("#eeeeee").Padding(6)
                                .Text($"₹{bookingSeat.PriceAtBooking:N0}");
                        }
                    });

                    column.Item().PaddingTop(10).Text(text =>
                    {
                        text.AlignRight();
                        text.Span($"Total Paid: ₹{booking.TotalAmount:N0}")
                            .FontSize(13)
                            .Bold();
                    });

                    column.Item().PaddingTop(14)
                        .Text($"Booked by: {booking.BookerName ?? "—"}");

                    if (!string.IsNullOrWhiteSpace(booking.BookerEmail))
                    {
                        column.Item().PaddingTop(2)
                            .Text(booking.BookerEmail)
                            .FontColor("#555555");
                    }
                });

                page.Footer().AlignCenter().PaddingTop(8).Text(text =>
                {
                    text.Span("SmartTix — AI-Powered Concert Ticketing. Present this ticket at the venue entrance.")
                        .FontSize(8)
                        .FontColor("#999999");
                });
            });
        }).GeneratePdf();
    }

    private static void HeaderCell(IContainer cell, string label)
    {
        cell.Background("#6c63ff").Padding(6).Text(label)
            .FontColor("#ffffff")
            .Bold()
            .FontSize(9);
    }
}
