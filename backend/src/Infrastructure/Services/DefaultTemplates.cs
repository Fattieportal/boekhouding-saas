namespace Boekhouding.Infrastructure.Services;

/// <summary>
/// Default invoice templates
/// </summary>
public static class DefaultTemplates
{
    public static string DefaultHtmlTemplate => @"
<!DOCTYPE html>
<html lang=""nl"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Invoice {{ Invoice.InvoiceNumber }}</title>
</head>
<body>
    <div class=""invoice-container"">
        <!-- Header -->
        <div class=""invoice-header"">
            <div class=""company-info"">
                {{if Branding.LogoUrl}}
                <img src=""{{ Branding.LogoUrl }}"" alt=""Company Logo"" class=""logo"" />
                {{end}}
            </div>
            <div class=""invoice-title"">
                <h1>FACTUUR</h1>
                <p class=""invoice-number"">{{ Invoice.InvoiceNumber }}</p>
            </div>
        </div>

        <!-- Invoice Details & Customer Info -->
        <div class=""info-section"">
            <div class=""customer-info"">
                <h3>Aan:</h3>
                <p><strong>{{ Contact.DisplayName }}</strong></p>
                {{if Contact.AddressLine1}}<p>{{ Contact.AddressLine1 }}</p>{{end}}
                {{if Contact.AddressLine2}}<p>{{ Contact.AddressLine2 }}</p>{{end}}
                {{if Contact.PostalCode}}<p>{{ Contact.PostalCode }} {{ Contact.City }}</p>{{end}}
                {{if Contact.Country}}<p>{{ Contact.Country }}</p>{{end}}
                {{if Contact.Email}}<p>Email: {{ Contact.Email }}</p>{{end}}
                {{if Contact.Phone}}<p>Tel: {{ Contact.Phone }}</p>{{end}}
            </div>
            <div class=""invoice-details"">
                <table class=""details-table"">
                    <tr>
                        <td><strong>Factuurdatum:</strong></td>
                        <td>{{ Invoice.IssueDate }}</td>
                    </tr>
                    <tr>
                        <td><strong>Vervaldatum:</strong></td>
                        <td>{{ Invoice.DueDate }}</td>
                    </tr>
                    <tr>
                        <td><strong>Factuurnummer:</strong></td>
                        <td>{{ Invoice.InvoiceNumber }}</td>
                    </tr>
                </table>
            </div>
        </div>

        <!-- Line Items -->
        <div class=""line-items"">
            <table class=""items-table"">
                <thead>
                    <tr>
                        <th class=""text-left"">#</th>
                        <th class=""text-left"">Omschrijving</th>
                        <th class=""text-right"">Aantal</th>
                        <th class=""text-right"">Prijs</th>
                        <th class=""text-right"">BTW %</th>
                        <th class=""text-right"">Totaal</th>
                    </tr>
                </thead>
                <tbody>
                    {{for line in Lines}}
                    <tr>
                        <td>{{ line.LineNumber }}</td>
                        <td>{{ line.Description }}</td>
                        <td class=""text-right"">{{ line.Quantity }}</td>
                        <td class=""text-right"">{{ Invoice.Currency }} {{ line.UnitPrice }}</td>
                        <td class=""text-right"">{{ line.VatRate }}%</td>
                        <td class=""text-right"">{{ Invoice.Currency }} {{ line.LineTotal }}</td>
                    </tr>
                    {{end}}
                </tbody>
            </table>
        </div>

        <!-- Totals -->
        <div class=""totals-section"">
            <table class=""totals-table"">
                <tr>
                    <td class=""total-label"">Subtotaal (excl. BTW):</td>
                    <td class=""total-amount"">{{ Invoice.Currency }} {{ Invoice.Subtotal }}</td>
                </tr>
                <tr>
                    <td class=""total-label"">BTW:</td>
                    <td class=""total-amount"">{{ Invoice.Currency }} {{ Invoice.VatTotal }}</td>
                </tr>
                <tr class=""grand-total"">
                    <td class=""total-label""><strong>Totaal (incl. BTW):</strong></td>
                    <td class=""total-amount""><strong>{{ Invoice.Currency }} {{ Invoice.Total }}</strong></td>
                </tr>
            </table>
        </div>

        <!-- Notes -->
        {{if Invoice.Notes}}
        <div class=""notes-section"">
            <h3>Opmerkingen:</h3>
            <p>{{ Invoice.Notes }}</p>
        </div>
        {{end}}

        <!-- Footer -->
        <div class=""invoice-footer"">
            {{if Branding.FooterText}}
            <p>{{ Branding.FooterText }}</p>
            {{else}}
            <p>Bedankt voor uw opdracht!</p>
            {{end}}
        </div>
    </div>
</body>
</html>";

    public static string DefaultCssTemplate => @"
* {
    margin: 0;
    padding: 0;
    box-sizing: border-box;
}

body {
    font-family: 'Arial', 'Helvetica', sans-serif;
    font-size: 10pt;
    line-height: 1.6;
    color: #333;
}

.invoice-container {
    max-width: 800px;
    margin: 0 auto;
    padding: 20px;
}

/* Header */
.invoice-header {
    display: flex;
    justify-content: space-between;
    align-items: flex-start;
    margin-bottom: 40px;
    padding-bottom: 20px;
    border-bottom: 3px solid #0066cc;
}

.logo {
    max-width: 200px;
    max-height: 80px;
}

.invoice-title h1 {
    font-size: 32pt;
    color: #0066cc;
    margin: 0;
}

.invoice-number {
    font-size: 12pt;
    color: #666;
    margin-top: 5px;
}

/* Info Section */
.info-section {
    display: flex;
    justify-content: space-between;
    margin-bottom: 30px;
}

.customer-info, .invoice-details {
    width: 48%;
}

.customer-info h3, .invoice-details h3 {
    font-size: 11pt;
    margin-bottom: 10px;
    color: #0066cc;
}

.customer-info p {
    margin: 3px 0;
}

.details-table {
    width: 100%;
}

.details-table td {
    padding: 3px 0;
}

.details-table td:first-child {
    width: 50%;
}

/* Line Items */
.line-items {
    margin-bottom: 30px;
}

.items-table {
    width: 100%;
    border-collapse: collapse;
}

.items-table thead {
    background-color: #0066cc;
    color: white;
}

.items-table th {
    padding: 10px;
    text-align: left;
    font-weight: bold;
    font-size: 10pt;
}

.items-table tbody tr {
    border-bottom: 1px solid #ddd;
}

.items-table tbody tr:nth-child(even) {
    background-color: #f9f9f9;
}

.items-table td {
    padding: 10px;
    font-size: 10pt;
}

.text-left {
    text-align: left;
}

.text-right {
    text-align: right;
}

/* Totals */
.totals-section {
    margin-bottom: 30px;
    display: flex;
    justify-content: flex-end;
}

.totals-table {
    width: 350px;
}

.totals-table tr {
    border-bottom: 1px solid #ddd;
}

.total-label {
    padding: 8px 10px;
    text-align: right;
}

.total-amount {
    padding: 8px 10px;
    text-align: right;
    font-weight: bold;
}

.grand-total {
    background-color: #f0f0f0;
    border-top: 2px solid #0066cc;
}

.grand-total .total-amount {
    color: #0066cc;
    font-size: 12pt;
}

/* Notes */
.notes-section {
    margin-bottom: 30px;
    padding: 15px;
    background-color: #f9f9f9;
    border-left: 4px solid #0066cc;
}

.notes-section h3 {
    font-size: 11pt;
    margin-bottom: 10px;
    color: #0066cc;
}

/* Footer */
.invoice-footer {
    text-align: center;
    padding-top: 20px;
    border-top: 1px solid #ddd;
    color: #666;
    font-size: 9pt;
}

.invoice-footer p {
    margin: 5px 0;
}

@media print {
    .invoice-container {
        padding: 0;
    }
}";
}
