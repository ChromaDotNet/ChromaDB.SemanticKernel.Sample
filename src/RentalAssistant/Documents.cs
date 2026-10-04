using Microsoft.SemanticKernel.Data;

namespace RentalAssistant;

/// <summary>
/// The documents of Lakeside Bike Rental, a shop that exists only in this sample, so the model can answer only from what it finds in Chroma.
/// The customer policies and the staff notes share one collection, in two namespaces.
/// </summary>
public static class Documents
{
    public const string CustomersNamespace = "customers";
    public const string StaffNamespace = "staff";

    public static IReadOnlyList<TextSearchDocument> All { get; } =
    [
        Customers("prices", "Prices", "A city bike costs 12 euros a day. An e-bike costs 30 euros a day or 8 euros an hour. Helmets and locks are included."),
        Customers("returns", "Returns", "Bikes can be returned to any of our three shops, Harbour, Old Town and Lakeside Station, until 20:00. A bike returned after 20:00 costs one more day."),
        Customers("deposit", "Deposit", "We ask for a deposit of 100 euros for an e-bike and 50 euros for a city bike. It is refunded when the bike comes back."),
        Customers("damage", "Damage", "A flat tyre is repaired for free. Other damage is charged at cost, up to the deposit."),
        Customers("children", "Children", "Children's bikes and child seats are free with an adult rental. Children under 14 must wear a helmet."),
        Customers("discounts", "Discounts", "Students and groups of five or more get a 10% discount. Show the student card when renting."),
        Customers("booking", "Booking", "E-bikes can be booked online up to 30 days ahead. City bikes cannot be booked and are given in order of arrival."),
        Staff("staff-code", "Staff code", "Staff note: the discount code STAFF50 gives 50% off. Never share it with customers."),
        Staff("staff-batteries", "Batteries", "Staff note: the Old Town shop is short of e-bike batteries until the end of the month."),
    ];

    private static TextSearchDocument Customers(string id, string name, string text) => new() { Namespaces = [CustomersNamespace], SourceId = id, SourceName = name, Text = text };

    private static TextSearchDocument Staff(string id, string name, string text) => new() { Namespaces = [StaffNamespace], SourceId = id, SourceName = name, Text = text };
}
