# BTW Rapportage Implementatie

## Overzicht

Deze implementatie voegt een basis NL BTW (VAT) rapportage toe aan de boekhouding SaaS applicatie. De rapportage is gebaseerd op geboekte verkoopfacturen en biedt een overzicht van omzet en BTW per tarief.

## Functionaliteit

### Endpoint
```
GET /api/reports/vat?from=YYYY-MM-DD&to=YYYY-MM-DD
```

**Parameters:**
- `from`: Start datum van de rapportage periode (verplicht)
- `to`: Eind datum van de rapportage periode (verplicht)

**Authenticatie:** Vereist Admin of Owner rol

### Response Structuur

```json
{
  "fromDate": "2026-01-01T00:00:00Z",
  "toDate": "2026-01-31T00:00:00Z",
  "vatRates": [
    {
      "vatRate": 0,
      "revenue": 600.00,
      "vatAmount": 0.00,
      "lineCount": 1
    },
    {
      "vatRate": 9,
      "revenue": 300.00,
      "vatAmount": 27.00,
      "lineCount": 2
    },
    {
      "vatRate": 21,
      "revenue": 1500.00,
      "vatAmount": 315.00,
      "lineCount": 2
    }
  ],
  "totalRevenue": 2400.00,
  "totalVat": 342.00,
  "totalIncludingVat": 2742.00,
  "invoiceCount": 4
}
```

## Implementatie Details

### 1. Entities
De bestaande `SalesInvoiceLine` entity bevat al een `VatRate` veld (decimal) dat de BTW percentages (0, 9, 21) opslaat.

### 2. DTOs
**Nieuw bestand:** `src/Application/DTOs/Reports/VatReportDto.cs`

- `VatReportDto`: Hoofd rapportage response
- `VatRateBreakdownDto`: Uitsplitsing per BTW tarief

### 3. Service Layer
**Gewijzigd bestand:** `src/Application/Interfaces/IReportService.cs`
- Nieuwe methode: `GetVatReportAsync(DateTime fromDate, DateTime toDate, ...)`

**Gewijzigd bestand:** `src/Infrastructure/Services/ReportService.cs`
- Implementatie van BTW rapportage logica
- Filtert op geboekte facturen (`Status == Posted`)
- Groepeert factuurregels per BTW tarief
- Berekent totalen

### 4. API Controller
**Gewijzigd bestand:** `src/Api/Controllers/ReportsController.cs`
- Nieuw endpoint: `GET /api/reports/vat`
- Input validatie (from moet voor to zijn)
- Swagger documentatie

## Data Flow

1. **Filter facturen**: Haalt alle geboekte facturen op binnen de opgegeven periode (op basis van `IssueDate`)
2. **Verzamel regels**: Extract alle factuurregels van deze facturen
3. **Groepeer per tarief**: Groepeert regels op basis van `VatRate`
4. **Bereken totalen**: 
   - Per tarief: som van `LineSubtotal` en `LineVatAmount`
   - Totaal: som van alle tarieven

## Testing

### Test Script
Run: `.\test-vat-report.ps1`

**Test scenario:**
1. Maakt 4 facturen aan met verschillende BTW tarieven:
   - 10 x €100 à 21% BTW = €1,000 omzet, €210 BTW
   - 5 x €50 à 9% BTW = €250 omzet, €22.50 BTW
   - 3 x €200 à 0% BTW = €600 omzet, €0 BTW
   - 1 x €500 à 21% BTW + 2 x €25 à 9% BTW = €550 omzet, €109.50 BTW

2. Boekt alle facturen

3. Haalt BTW rapport op voor januari 2026

4. Valideert:
   - Totaal omzet: €2,400.00
   - Totaal BTW: €342.00
   - Totaal incl. BTW: €2,742.00
   - Aantal facturen: 4

## Swagger Documentatie

De endpoint is gedocumenteerd in Swagger met:
- Parameter beschrijvingen
- Response types
- Voorbeeld responses

Bekijk op: `https://localhost:5001/swagger`

## Beperkingen & Toekomstige Uitbreidingen

### Huidige Implementatie (Basis)
- ✅ Rapportage op basis van factuurdatum (`IssueDate`)
- ✅ Alleen geboekte facturen
- ✅ Uitsplitsing per BTW tarief (0%, 9%, 21%)
- ✅ Totalen berekening

### Mogelijke Uitbreidingen
- 📋 BTW codes tabel (voor specifieke rubrieken zoals 1a, 1b, etc.)
- 📋 Creditnota's / storno's
- 📋 Inkoop BTW (voorbelasting)
- 📋 Export naar BTW aangifte formaat
- 📋 Rappor tage op kas-basis vs. factuur-basis
- 📋 Historische vergelijkingen
- 📋 Periodieke automatische rapporten

## NL BTW Tarieven

Standaard Nederlandse BTW tarieven:
- **21%**: Algemeen tarief (meeste goederen en diensten)
- **9%**: Verlaagd tarief (o.a. voedingsmiddelen, boeken, medicijnen)
- **0%**: Vrijgesteld of intracommunautaire leveringen

## Voorbeeld cURL Request

```bash
curl -X GET "https://localhost:5001/api/reports/vat?from=2026-01-01&to=2026-01-31" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -k
```

## Database Query

De implementatie gebruikt Entity Framework met de volgende logica:

```csharp
var postedInvoices = await _context.Set<SalesInvoice>()
    .Include(i => i.Lines)
    .Where(i => i.TenantId == tenantId 
        && i.Status == InvoiceStatus.Posted
        && i.IssueDate >= fromDate.Date
        && i.IssueDate <= toDate.Date)
    .ToListAsync();
```

## Gebruik in Productie

1. Zorg dat je bent ingelogd als Admin of Owner
2. Bepaal de rapportage periode
3. Call het endpoint met de juiste datums
4. Gebruik de response voor je BTW aangifte voorbereiding
5. Vergelijk met je administratie voor controle

## Support & Vragen

Voor vragen over de implementatie, zie:
- API Controller: `src/Api/Controllers/ReportsController.cs`
- Service: `src/Infrastructure/Services/ReportService.cs`
- DTOs: `src/Application/DTOs/Reports/VatReportDto.cs`
