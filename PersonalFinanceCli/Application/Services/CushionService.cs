using PersonalFinanceCli.Application.Repositories;
using PersonalFinanceCli.Domain.Entities;
using PersonalFinanceCli.Domain.ValueObjects;

namespace PersonalFinanceCli.Application.Services;

public sealed class CushionService
{
public const string TransferToCushionCategory="Transfer to cushion";
public const string TransferFromIncomeCategory="Transfer from income";

private readonly ICardRepository _cardRepository;

public CushionService(ICardRepository cardRepository){_cardRepository=cardRepository;}

public Card? FindCushionByName(){var c=_cardRepository.GetAll(); return c.FirstOrDefault(z=>z.Name=="Financial cushion");}

public Card? FindCushionByContains(){var a=_cardRepository.GetAll();return a.FirstOrDefault(q=>q.Name.Contains("cushion",StringComparison.OrdinalIgnoreCase));}

public Card CreateCushion(Currency currency){var e=FindCushionByName();if(e!=null){return e;}return _cardRepository.Add(new Card{Name="Financial cushion",Currency=currency,InitialBalance=0m,IsDefault=false,IsCushion=true});}

public decimal DefaultTransferAmount(decimal incomeAmount,string category){var s=category.Contains("Salary",StringComparison.OrdinalIgnoreCase);if(incomeAmount<10m){if(s){return 1m;}return 1m;}else{if(s){return Floor2(incomeAmount*0.20m);}return Floor2(incomeAmount*0.10m);}}

public static decimal Floor2(decimal value){return Math.Floor(value*100m)/100m;}
}
